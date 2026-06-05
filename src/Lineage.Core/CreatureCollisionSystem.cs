namespace Lineage.Core;

/// <summary>
/// Resolves living-creature body collisions with deterministic circle separation
/// and optional high-speed impact damage.
/// </summary>
public sealed class CreatureCollisionSystem(
    bool enabled = true,
    float safeImpactSpeed = CreatureCollisionSystem.DefaultSafeImpactSpeed,
    float damageScale = CreatureCollisionSystem.DefaultDamageScale,
    int separationIterations = CreatureCollisionSystem.DefaultSeparationIterations,
    float minimumCellSize = CreatureCollisionSystem.DefaultMinimumCellSize) : ISimulationSystem
{
    public const float DefaultSafeImpactSpeed = 18f;
    public const float DefaultDamageScale = 0.00025f;
    public const int DefaultSeparationIterations = 2;
    public const float DefaultMinimumCellSize = 16f;

    private const float MinimumDistance = 0.0001f;
    private const float MinimumMass = 0.01f;
    private const float MinimumSweepMotion = 0.0001f;
    private const float RestingContactClearance = 0.25f;
    private const float RestingSeparationHysteresis = 0.05f;
    private const float TangentialUnstickShare = 0.04f;
    private const float MaxTangentialUnstick = 0.35f;

    private readonly bool _enabled = enabled;
    private readonly float _safeImpactSpeed = ValidateNonNegative(safeImpactSpeed, nameof(safeImpactSpeed));
    private readonly float _damageScale = ValidateNonNegative(damageScale, nameof(damageScale));
    private readonly int _separationIterations = ValidatePositive(separationIterations, nameof(separationIterations));
    private readonly float _minimumCellSize = ValidatePositive(minimumCellSize, nameof(minimumCellSize));
    private readonly Dictionary<long, List<int>> _cells = [];
    private readonly List<long> _activeCellKeys = [];
    private readonly List<List<int>> _cellPool = [];

    private float[] _bodyRadii = [];
    private float[] _bodyMasses = [];
    private float[] _damageTaken = [];
    private float[] _damageDealt = [];
    private float[] _sourceDamage = [];
    private EntityId[] _damageSources = [];

    private readonly record struct SweptContact(SimVector2 Normal, SimVector2 FirstPosition, SimVector2 SecondPosition);

    public void Update(WorldState state, float deltaSeconds)
    {
        var creatureCount = state.Creatures.Count;
        ResetLastCollisionState(state);
        if (!_enabled || creatureCount < 2)
        {
            return;
        }

        EnsureBuffers(creatureCount);
        var maxRadius = CacheBodyTraits(state, out var maxMovementDistance);
        var cellSize = Math.Max(_minimumCellSize, maxRadius * 2f + maxMovementDistance);

        for (var iteration = 0; iteration < _separationIterations; iteration++)
        {
            RebuildCollisionGrid(state, cellSize);
            var recordContact = iteration == 0;
            ResolveActiveCellPairs(state, recordContact);
        }

        ApplyAccumulatedDamage(state);
    }

    private void ResolveActiveCellPairs(WorldState state, bool recordContact)
    {
        for (var i = 0; i < _activeCellKeys.Count; i++)
        {
            var key = _activeCellKeys[i];
            if (!_cells.TryGetValue(key, out var cell) || cell.Count == 0)
            {
                continue;
            }

            ResolvePairsInCell(state, cell, recordContact);

            var cellX = CellX(key);
            var cellY = CellY(key);
            ResolvePairsAcrossCells(state, cell, cellX + 1, cellY, recordContact);
            ResolvePairsAcrossCells(state, cell, cellX - 1, cellY + 1, recordContact);
            ResolvePairsAcrossCells(state, cell, cellX, cellY + 1, recordContact);
            ResolvePairsAcrossCells(state, cell, cellX + 1, cellY + 1, recordContact);
        }
    }

    private void ResolvePairsInCell(WorldState state, List<int> cell, bool recordContact)
    {
        for (var i = 0; i < cell.Count; i++)
        {
            for (var j = i + 1; j < cell.Count; j++)
            {
                ResolvePair(state, cell[i], cell[j], recordContact);
            }
        }
    }

    private void ResolvePairsAcrossCells(
        WorldState state,
        List<int> cell,
        int neighborX,
        int neighborY,
        bool recordContact)
    {
        if (!_cells.TryGetValue(CellKey(neighborX, neighborY), out var neighbor) || neighbor.Count == 0)
        {
            return;
        }

        for (var i = 0; i < cell.Count; i++)
        {
            for (var j = 0; j < neighbor.Count; j++)
            {
                ResolvePair(state, cell[i], neighbor[j], recordContact);
            }
        }
    }

    private void ResolvePair(WorldState state, int firstIndex, int secondIndex, bool recordContact)
    {
        if (firstIndex == secondIndex)
        {
            return;
        }

        var first = state.Creatures[firstIndex];
        var second = state.Creatures[secondIndex];
        if (first.Id == second.Id
            || first.Health <= 0f
            || second.Health <= 0f
            || first.Energy <= 0f
            || second.Energy <= 0f)
        {
            return;
        }

        var minimumDistance = _bodyRadii[firstIndex] + _bodyRadii[secondIndex];
        var restingDistance = minimumDistance + RestingContactClearance;
        var restingSeparationDistance = restingDistance + RestingSeparationHysteresis;
        var delta = second.Position - first.Position;
        var distanceSquared = delta.LengthSquared;
        var restingDistanceSquared = restingDistance * restingDistance;
        var normal = SimVector2.Zero;
        if (distanceSquared < restingDistanceSquared)
        {
            var distance = MathF.Sqrt(Math.Max(0f, distanceSquared));
            normal = distance > MinimumDistance
                ? delta / distance
                : DeterministicNormal(firstIndex, secondIndex);
            SeparateOverlap(
                state,
                ref first,
                ref second,
                normal,
                restingSeparationDistance,
                distance,
                firstIndex,
                secondIndex);
        }
        else if (TryGetSweptContact(first, second, minimumDistance, firstIndex, secondIndex, out var sweptContact))
        {
            normal = sweptContact.Normal;
            first.Position = state.Bounds.Clamp(sweptContact.FirstPosition);
            second.Position = state.Bounds.Clamp(sweptContact.SecondPosition);
            var resolvedDelta = second.Position - first.Position;
            var resolvedDistance = resolvedDelta.Length;
            if (resolvedDistance < minimumDistance)
            {
                var resolvedNormal = resolvedDistance > MinimumDistance
                    ? resolvedDelta / resolvedDistance
                    : normal;
                SeparateOverlap(
                    state,
                    ref first,
                    ref second,
                    resolvedNormal,
                    restingSeparationDistance,
                    resolvedDistance,
                    firstIndex,
                    secondIndex);
                normal = resolvedNormal;
            }
        }
        else
        {
            return;
        }

        first.LastMovementBlocked = true;
        second.LastMovementBlocked = true;
        first.MaxXReached = Math.Max(first.MaxXReached, first.Position.X);
        second.MaxXReached = Math.Max(second.MaxXReached, second.Position.X);

        var firstMass = _bodyMasses[firstIndex];
        var secondMass = _bodyMasses[secondIndex];
        var totalMass = Math.Max(MinimumMass, firstMass + secondMass);
        var relativeVelocity = second.Velocity - first.Velocity;
        var closingSpeed = Math.Max(0f, -SimVector2.Dot(relativeVelocity, normal));
        if (closingSpeed > 0f)
        {
            first.LastCreatureCollisionImpactSpeed = Math.Max(first.LastCreatureCollisionImpactSpeed, closingSpeed);
            second.LastCreatureCollisionImpactSpeed = Math.Max(second.LastCreatureCollisionImpactSpeed, closingSpeed);
            ApplyTangentialUnstick(state, ref first, ref second, normal, relativeVelocity, closingSpeed, minimumDistance);
            first.MaxXReached = Math.Max(first.MaxXReached, first.Position.X);
            second.MaxXReached = Math.Max(second.MaxXReached, second.Position.X);
            RemoveClosingVelocity(ref first, ref second, normal, firstMass, secondMass, totalMass);
        }

        if (recordContact)
        {
            first.LastCreatureCollisionCount++;
            second.LastCreatureCollisionCount++;
            if (_damageScale > 0f)
            {
                AccumulateImpactDamage(state, firstIndex, secondIndex, first, second, closingSpeed);
            }
        }

        state.Creatures[firstIndex] = first;
        state.Creatures[secondIndex] = second;
    }

    private void SeparateOverlap(
        WorldState state,
        ref CreatureState first,
        ref CreatureState second,
        SimVector2 normal,
        float minimumDistance,
        float distance,
        int firstIndex,
        int secondIndex)
    {
        var overlap = Math.Max(0f, minimumDistance - distance);
        var firstMass = _bodyMasses[firstIndex];
        var secondMass = _bodyMasses[secondIndex];
        var totalMass = Math.Max(MinimumMass, firstMass + secondMass);
        var firstMoveShare = secondMass / totalMass;
        var secondMoveShare = firstMass / totalMass;

        first.Position = state.Bounds.Clamp(first.Position - normal * (overlap * firstMoveShare));
        second.Position = state.Bounds.Clamp(second.Position + normal * (overlap * secondMoveShare));
        first.MaxXReached = Math.Max(first.MaxXReached, first.Position.X);
        second.MaxXReached = Math.Max(second.MaxXReached, second.Position.X);
        first.LastMovementBlocked = true;
        second.LastMovementBlocked = true;
    }

    private static bool TryGetSweptContact(
        CreatureState first,
        CreatureState second,
        float minimumDistance,
        int firstIndex,
        int secondIndex,
        out SweptContact contact)
    {
        contact = default;
        if (!first.PreviousPosition.IsFinite || !second.PreviousPosition.IsFinite)
        {
            return false;
        }

        var relativeStart = second.PreviousPosition - first.PreviousPosition;
        var relativeEnd = second.Position - first.Position;
        var relativeMovement = relativeEnd - relativeStart;
        var motionSquared = relativeMovement.LengthSquared;
        if (motionSquared <= MinimumSweepMotion * MinimumSweepMotion)
        {
            return false;
        }

        var minimumDistanceSquared = minimumDistance * minimumDistance;
        var startDistanceSquared = relativeStart.LengthSquared;
        if (startDistanceSquared <= minimumDistanceSquared)
        {
            return false;
        }

        var b = 2f * SimVector2.Dot(relativeStart, relativeMovement);
        if (b >= 0f)
        {
            return false;
        }

        var c = startDistanceSquared - minimumDistanceSquared;
        var discriminant = b * b - 4f * motionSquared * c;
        if (discriminant < 0f)
        {
            return false;
        }

        var hitTime = (-b - MathF.Sqrt(discriminant)) / (2f * motionSquared);
        if (hitTime < 0f || hitTime > 1f)
        {
            return false;
        }

        var firstContact = Lerp(first.PreviousPosition, first.Position, hitTime);
        var secondContact = Lerp(second.PreviousPosition, second.Position, hitTime);
        var contactDelta = secondContact - firstContact;
        var normal = contactDelta.LengthSquared > MinimumDistance * MinimumDistance
            ? contactDelta.Normalized()
            : DeterministicNormal(firstIndex, secondIndex);

        var firstRemainder = first.Position - firstContact;
        var secondRemainder = second.Position - secondContact;
        var firstSlide = RemoveInwardRemainder(firstRemainder, normal, isFirst: true);
        var secondSlide = RemoveInwardRemainder(secondRemainder, normal, isFirst: false);
        contact = new SweptContact(normal, firstContact + firstSlide, secondContact + secondSlide);
        return true;
    }

    private static SimVector2 RemoveInwardRemainder(SimVector2 remainder, SimVector2 normal, bool isFirst)
    {
        var normalMotion = SimVector2.Dot(remainder, normal);
        if (isFirst)
        {
            return normalMotion > 0f ? remainder - normal * normalMotion : remainder;
        }

        return normalMotion < 0f ? remainder - normal * normalMotion : remainder;
    }

    private static SimVector2 Lerp(SimVector2 start, SimVector2 end, float amount)
    {
        return start + (end - start) * amount;
    }

    private static void ApplyTangentialUnstick(
        WorldState state,
        ref CreatureState first,
        ref CreatureState second,
        SimVector2 normal,
        SimVector2 relativeVelocity,
        float closingSpeed,
        float minimumDistance)
    {
        var tangent = new SimVector2(-normal.Y, normal.X);
        var tangentialSpeed = MathF.Abs(SimVector2.Dot(relativeVelocity, tangent));
        if (tangentialSpeed > closingSpeed * 0.15f + 0.001f)
        {
            return;
        }

        var bias = MathF.Min(MaxTangentialUnstick, minimumDistance * TangentialUnstickShare);
        if (bias <= 0f)
        {
            return;
        }

        var sign = first.Id.Value <= second.Id.Value ? 1f : -1f;
        first.Position = state.Bounds.Clamp(first.Position + tangent * (bias * sign));
        second.Position = state.Bounds.Clamp(second.Position - tangent * (bias * sign));
    }

    private void AccumulateImpactDamage(
        WorldState state,
        int firstIndex,
        int secondIndex,
        CreatureState first,
        CreatureState second,
        float closingSpeed)
    {
        var excessSpeed = closingSpeed - _safeImpactSpeed;
        if (excessSpeed <= 0f)
        {
            return;
        }

        var baseDamage = _damageScale * excessSpeed * excessSpeed;
        var firstDamage = ImpactDamageFor(baseDamage, _bodyMasses[secondIndex], _bodyMasses[firstIndex]);
        var secondDamage = ImpactDamageFor(baseDamage, _bodyMasses[firstIndex], _bodyMasses[secondIndex]);
        if (firstDamage > 0f)
        {
            firstDamage = ApplyDamageResistance(state, first, firstDamage);
            _damageTaken[firstIndex] += firstDamage;
            _damageDealt[secondIndex] += firstDamage;
            if (firstDamage > _sourceDamage[firstIndex])
            {
                _sourceDamage[firstIndex] = firstDamage;
                _damageSources[firstIndex] = second.Id;
            }
        }

        if (secondDamage > 0f)
        {
            secondDamage = ApplyDamageResistance(state, second, secondDamage);
            _damageTaken[secondIndex] += secondDamage;
            _damageDealt[firstIndex] += secondDamage;
            if (secondDamage > _sourceDamage[secondIndex])
            {
                _sourceDamage[secondIndex] = secondDamage;
                _damageSources[secondIndex] = first.Id;
            }
        }
    }

    private void ApplyAccumulatedDamage(WorldState state)
    {
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var damageTaken = _damageTaken[i];
            creature.LastCreatureCollisionDamageTaken = damageTaken;
            creature.LastCreatureCollisionDamageDealt = _damageDealt[i];
            if (damageTaken > 0f)
            {
                creature.Health = Math.Max(0f, creature.Health - damageTaken);
                if (_damageSources[i] != default)
                {
                    creature.LastDamagingCreatureId = _damageSources[i];
                }
            }

            state.Creatures[i] = creature;
        }
    }

    private static float ImpactDamageFor(float baseDamage, float incomingMass, float targetMass)
    {
        var massPressure = MathF.Sqrt(Math.Max(MinimumMass, incomingMass) / Math.Max(MinimumMass, targetMass));
        return baseDamage * massPressure;
    }

    private static float ApplyDamageResistance(WorldState state, CreatureState target, float incomingDamage)
    {
        var genome = state.GetGenome(target.GenomeId);
        return CreatureCombat.ApplyDamageResistance(incomingDamage, target, genome);
    }

    private static void RemoveClosingVelocity(
        ref CreatureState first,
        ref CreatureState second,
        SimVector2 normal,
        float firstMass,
        float secondMass,
        float totalMass)
    {
        var firstNormalVelocity = SimVector2.Dot(first.Velocity, normal);
        var secondNormalVelocity = SimVector2.Dot(second.Velocity, normal);
        var sharedNormalVelocity = (firstNormalVelocity * firstMass + secondNormalVelocity * secondMass) / totalMass;
        first.Velocity += normal * (sharedNormalVelocity - firstNormalVelocity);
        second.Velocity += normal * (sharedNormalVelocity - secondNormalVelocity);
    }

    private void ResetLastCollisionState(WorldState state)
    {
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            creature.LastCreatureCollisionCount = 0;
            creature.LastCreatureCollisionImpactSpeed = 0f;
            creature.LastCreatureCollisionDamageDealt = 0f;
            creature.LastCreatureCollisionDamageTaken = 0f;
            state.Creatures[i] = creature;
        }
    }

    private float CacheBodyTraits(WorldState state, out float maxMovementDistance)
    {
        var maxRadius = 0f;
        maxMovementDistance = 0f;
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            var radius = Math.Max(0.001f, CreatureGrowth.EffectiveBodyRadius(creature, genome));
            _bodyRadii[i] = radius;
            _bodyMasses[i] = Math.Max(MinimumMass, radius * radius);
            maxRadius = Math.Max(maxRadius, radius);
            maxMovementDistance = Math.Max(
                maxMovementDistance,
                SimVector2.Distance(creature.PreviousPosition, creature.Position));
            _damageTaken[i] = 0f;
            _damageDealt[i] = 0f;
            _sourceDamage[i] = 0f;
            _damageSources[i] = default;
        }

        return maxRadius;
    }

    private void RebuildCollisionGrid(WorldState state, float cellSize)
    {
        ReleaseActiveCells();
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            if (creature.Health <= 0f || creature.Energy <= 0f)
            {
                continue;
            }

            var cellX = (int)MathF.Floor(creature.Position.X / cellSize);
            var cellY = (int)MathF.Floor(creature.Position.Y / cellSize);
            var key = CellKey(cellX, cellY);
            if (!_cells.TryGetValue(key, out var cell))
            {
                cell = RentCell();
                _cells.Add(key, cell);
                _activeCellKeys.Add(key);
            }

            cell.Add(i);
        }
    }

    private void ReleaseActiveCells()
    {
        for (var i = 0; i < _activeCellKeys.Count; i++)
        {
            var key = _activeCellKeys[i];
            if (_cells.Remove(key, out var cell))
            {
                cell.Clear();
                _cellPool.Add(cell);
            }
        }

        _activeCellKeys.Clear();
    }

    private List<int> RentCell()
    {
        if (_cellPool.Count == 0)
        {
            return [];
        }

        var index = _cellPool.Count - 1;
        var cell = _cellPool[index];
        _cellPool.RemoveAt(index);
        return cell;
    }

    private void EnsureBuffers(int creatureCount)
    {
        if (_bodyRadii.Length >= creatureCount)
        {
            return;
        }

        var capacity = Math.Max(creatureCount, _bodyRadii.Length * 2);
        if (capacity <= 0)
        {
            capacity = creatureCount;
        }

        Array.Resize(ref _bodyRadii, capacity);
        Array.Resize(ref _bodyMasses, capacity);
        Array.Resize(ref _damageTaken, capacity);
        Array.Resize(ref _damageDealt, capacity);
        Array.Resize(ref _sourceDamage, capacity);
        Array.Resize(ref _damageSources, capacity);
    }

    private static SimVector2 DeterministicNormal(int firstIndex, int secondIndex)
    {
        var angle = (firstIndex * 73856093 ^ secondIndex * 19349663) * 0.000001f;
        return SimVector2.FromAngle(angle);
    }

    private static long CellKey(int x, int y)
    {
        return ((long)x << 32) | (uint)y;
    }

    private static int CellX(long key)
    {
        return (int)(key >> 32);
    }

    private static int CellY(long key)
    {
        return (int)(key & 0xffffffff);
    }

    private static float ValidateNonNegative(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Creature collision setting must be finite and non-negative.");
    }

    private static int ValidatePositive(int value, string name)
    {
        return value > 0
            ? value
            : throw new ArgumentOutOfRangeException(name, "Creature collision setting must be positive.");
    }

    private static float ValidatePositive(float value, string name)
    {
        return float.IsFinite(value) && value > 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Creature collision setting must be finite and positive.");
    }
}
