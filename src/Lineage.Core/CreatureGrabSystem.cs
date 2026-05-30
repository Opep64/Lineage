namespace Lineage.Core;

/// <summary>
/// Resolves creature-on-creature grab intent into a cheap movement-impeding hold.
/// </summary>
public sealed class CreatureGrabSystem(
    float holdRangePadding = CreatureGrabSystem.DefaultHoldRangePadding,
    float maximumMovementPenalty = CreatureGrabSystem.DefaultMaximumMovementPenalty) : ISimulationSystem
{
    public const float DefaultHoldRangePadding = 3f;
    public const float DefaultMaximumMovementPenalty = 0.85f;

    private readonly float _holdRangePadding = ValidateNonNegative(holdRangePadding, nameof(holdRangePadding));
    private readonly float _maximumMovementPenalty = ValidateUnit(maximumMovementPenalty, nameof(maximumMovementPenalty));
    private readonly Dictionary<EntityId, int> _creatureIndexById = [];
    private int[] _claimGrabberIndices = [];
    private float[] _claimPressures = [];
    private SimVector2[] _claimDirections = [];

    public void Update(WorldState state, float deltaSeconds)
    {
        var creatureCount = state.Creatures.Count;
        EnsureCapacity(creatureCount);
        BuildCreatureIndex(state);
        ResetIncomingGrabState(state);
        Array.Fill(_claimGrabberIndices, -1, 0, creatureCount);
        Array.Fill(_claimPressures, 0f, 0, creatureCount);
        Array.Fill(_claimDirections, SimVector2.Zero, 0, creatureCount);

        for (var grabberIndex = 0; grabberIndex < creatureCount; grabberIndex++)
        {
            var grabber = state.Creatures[grabberIndex];
            var targetIndex = ResolveTargetIndex(state, grabber, grabberIndex);
            if (targetIndex < 0)
            {
                grabber.HeldCreatureId = default;
                grabber.GrabStrength = 0f;
                state.Creatures[grabberIndex] = grabber;
                continue;
            }

            var target = state.Creatures[targetIndex];
            var grabberGenome = state.GetGenome(grabber.GenomeId);
            var targetGenome = state.GetGenome(target.GenomeId);
            var grabPressure = CalculateGrabPressure(grabber, target, grabberGenome, targetGenome);
            if (grabPressure <= 0f || ShouldBreakHold(grabber, target, grabberGenome, targetGenome, grabPressure))
            {
                grabber.HeldCreatureId = default;
                grabber.GrabStrength = 0f;
                state.Creatures[grabberIndex] = grabber;
                continue;
            }

            grabber.HeldCreatureId = target.Id;
            grabber.GrabStrength = grabPressure;
            state.Creatures[grabberIndex] = grabber;
            ClaimTarget(state, grabberIndex, targetIndex, grabPressure);
        }

        ApplyClaims(state);
        ClearRejectedHolds(state);
    }

    public static float MovementMultiplierForGrabPressure(float grabPressure, float maximumMovementPenalty = DefaultMaximumMovementPenalty)
    {
        var penalty = ValidateUnit(maximumMovementPenalty, nameof(maximumMovementPenalty))
            * Math.Clamp(grabPressure, 0f, 1f);
        return Math.Clamp(1f - penalty, 0f, 1f);
    }

    private int ResolveTargetIndex(WorldState state, CreatureState grabber, int grabberIndex)
    {
        if (!grabber.Actions.WantsGrab)
        {
            return -1;
        }

        if (TryResolveHeldTarget(state, grabber, grabberIndex, out var heldTargetIndex))
        {
            return heldTargetIndex;
        }

        if (!grabber.IsTouchingCreature
            || grabber.CreatureContactId == default
            || grabber.CreatureContactId == grabber.Id
            || !_creatureIndexById.TryGetValue(grabber.CreatureContactId, out var contactIndex))
        {
            return -1;
        }

        return contactIndex == grabberIndex ? -1 : contactIndex;
    }

    private bool TryResolveHeldTarget(
        WorldState state,
        CreatureState grabber,
        int grabberIndex,
        out int heldTargetIndex)
    {
        heldTargetIndex = -1;
        if (grabber.HeldCreatureId == default
            || grabber.HeldCreatureId == grabber.Id
            || !_creatureIndexById.TryGetValue(grabber.HeldCreatureId, out heldTargetIndex)
            || heldTargetIndex == grabberIndex)
        {
            return false;
        }

        var target = state.Creatures[heldTargetIndex];
        var grabberRadius = CreatureGrowth.EffectiveBodyRadius(grabber, state.GetGenome(grabber.GenomeId));
        var targetRadius = CreatureGrowth.EffectiveBodyRadius(target, state.GetGenome(target.GenomeId));
        var maxDistance = grabberRadius + targetRadius + _holdRangePadding;
        return SimVector2.DistanceSquared(grabber.Position, target.Position) <= maxDistance * maxDistance;
    }

    private static float CalculateGrabPressure(
        CreatureState grabber,
        CreatureState target,
        CreatureGenome grabberGenome,
        CreatureGenome targetGenome)
    {
        var output = Math.Clamp(grabber.Actions.GrabOutput, 0f, 1f);
        if (output <= 0f)
        {
            return 0f;
        }

        var grabberRadius = MathF.Max(0.001f, CreatureGrowth.EffectiveBodyRadius(grabber, grabberGenome));
        var targetRadius = MathF.Max(0.001f, CreatureGrowth.EffectiveBodyRadius(target, targetGenome));
        var sizeFactor = MathF.Sqrt(grabberRadius / targetRadius);
        return Math.Clamp(output * sizeFactor, 0f, 1f);
    }

    private static bool ShouldBreakHold(
        CreatureState grabber,
        CreatureState target,
        CreatureGenome grabberGenome,
        CreatureGenome targetGenome,
        float grabPressure)
    {
        var targetRadius = MathF.Max(0.001f, CreatureGrowth.EffectiveBodyRadius(target, targetGenome));
        var grabberRadius = MathF.Max(0.001f, CreatureGrowth.EffectiveBodyRadius(grabber, grabberGenome));
        var targetEffort = Math.Clamp(target.Actions.MoveForward, 0f, 1f)
            + Math.Abs(target.Actions.Turn) * 0.35f;
        var sizeAdvantage = MathF.Sqrt(targetRadius / grabberRadius);
        var escapeStrain = targetEffort * sizeAdvantage;
        return escapeStrain > grabPressure + 0.65f;
    }

    private void ClaimTarget(WorldState state, int grabberIndex, int targetIndex, float grabPressure)
    {
        if (grabPressure <= _claimPressures[targetIndex])
        {
            return;
        }

        var grabber = state.Creatures[grabberIndex];
        var target = state.Creatures[targetIndex];
        _claimGrabberIndices[targetIndex] = grabberIndex;
        _claimPressures[targetIndex] = grabPressure;
        _claimDirections[targetIndex] = (grabber.Position - target.Position).Normalized();
    }

    private void ApplyClaims(WorldState state)
    {
        for (var targetIndex = 0; targetIndex < state.Creatures.Count; targetIndex++)
        {
            var grabberIndex = _claimGrabberIndices[targetIndex];
            if (grabberIndex < 0)
            {
                continue;
            }

            var target = state.Creatures[targetIndex];
            target.GrabbedByCreatureId = state.Creatures[grabberIndex].Id;
            target.GrabPressure = Math.Clamp(_claimPressures[targetIndex], 0f, 1f);
            target.GrabDirection = _claimDirections[targetIndex];
            state.Creatures[targetIndex] = target;
        }
    }

    private void ClearRejectedHolds(WorldState state)
    {
        for (var grabberIndex = 0; grabberIndex < state.Creatures.Count; grabberIndex++)
        {
            var grabber = state.Creatures[grabberIndex];
            if (grabber.HeldCreatureId == default)
            {
                continue;
            }

            if (!_creatureIndexById.TryGetValue(grabber.HeldCreatureId, out var targetIndex)
                || state.Creatures[targetIndex].GrabbedByCreatureId != grabber.Id)
            {
                grabber.HeldCreatureId = default;
                grabber.GrabStrength = 0f;
                state.Creatures[grabberIndex] = grabber;
            }
        }
    }

    private void ResetIncomingGrabState(WorldState state)
    {
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            creature.GrabbedByCreatureId = default;
            creature.GrabPressure = 0f;
            creature.GrabDirection = SimVector2.Zero;
            state.Creatures[i] = creature;
        }
    }

    private void BuildCreatureIndex(WorldState state)
    {
        _creatureIndexById.Clear();
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            _creatureIndexById[state.Creatures[i].Id] = i;
        }
    }

    private void EnsureCapacity(int creatureCount)
    {
        if (_claimGrabberIndices.Length >= creatureCount)
        {
            return;
        }

        var capacity = Math.Max(creatureCount, _claimGrabberIndices.Length * 2);
        _claimGrabberIndices = new int[capacity];
        _claimPressures = new float[capacity];
        _claimDirections = new SimVector2[capacity];
    }

    private static float ValidateNonNegative(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Value must be finite and non-negative.");
    }

    private static float ValidateUnit(float value, string name)
    {
        return float.IsFinite(value) && value is >= 0f and <= 1f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Value must be finite and between 0 and 1.");
    }
}
