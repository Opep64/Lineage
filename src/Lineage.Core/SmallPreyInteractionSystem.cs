namespace Lineage.Core;

/// <summary>
/// Resolves creature grab and bite actions against lightweight small prey.
/// </summary>
public sealed class SmallPreyInteractionSystem(
    UniformSpatialIndex spatialIndex,
    float biteDamagePerSecond = 0.18f,
    float biteEnergyCostPerSecond = 0.15f,
    float biteRangePadding = 1f,
    float grabRangePadding = 3f,
    float meatDecayCaloriesPerSecond = 0.03f) : ISimulationSystem
{
    private const float FreshKillCreditSeconds = 20f;

    private readonly UniformSpatialIndex _spatialIndex = spatialIndex;
    private readonly float _biteDamagePerSecond = ValidateNonNegative(biteDamagePerSecond, nameof(biteDamagePerSecond));
    private readonly float _biteEnergyCostPerSecond = ValidateNonNegative(biteEnergyCostPerSecond, nameof(biteEnergyCostPerSecond));
    private readonly float _biteRangePadding = ValidateNonNegative(biteRangePadding, nameof(biteRangePadding));
    private readonly float _grabRangePadding = ValidateNonNegative(grabRangePadding, nameof(grabRangePadding));
    private readonly float _meatDecayCaloriesPerSecond = ValidateNonNegative(meatDecayCaloriesPerSecond, nameof(meatDecayCaloriesPerSecond));
    private readonly List<int> _preyCandidates = [];
    private readonly IndexStampSet _seenPreyCandidates = new();

    public void Update(WorldState state, float deltaSeconds)
    {
        ResetHoldState(state);

        if (state.SmallPrey.Count == 0)
        {
            return;
        }

        for (var creatureIndex = 0; creatureIndex < state.Creatures.Count; creatureIndex++)
        {
            var creature = state.Creatures[creatureIndex];
            var genome = state.GetGenome(creature.GenomeId);
            var bodyRadius = CreatureGrowth.EffectiveBodyRadius(creature, genome);
            var queryRadius = bodyRadius + MathF.Max(_biteRangePadding, _grabRangePadding);
            _spatialIndex.AddSmallPreyCandidatesWithCalories(
                state,
                creature.Position,
                queryRadius,
                minimumCalories: 0f,
                _preyCandidates,
                _seenPreyCandidates);

            var contact = FindNearestContact(state, creature, bodyRadius);
            if (contact.PreyIndex < 0)
            {
                state.Creatures[creatureIndex] = creature;
                continue;
            }

            if (creature.Actions.WantsGrab)
            {
                GrabSmallPrey(state, contact.PreyIndex, ref creature);
            }

            if (creature.Actions.WantsAttack && IsInBiteRangeAndArc(state.SmallPrey[contact.PreyIndex], creature, bodyRadius))
            {
                BiteSmallPrey(state, contact.PreyIndex, ref creature, genome, deltaSeconds);
            }

            state.Creatures[creatureIndex] = creature;
        }
    }

    private SmallPreyContact FindNearestContact(WorldState state, CreatureState creature, float bodyRadius)
    {
        var best = SmallPreyContact.None;
        var bestEdgeDistance = float.PositiveInfinity;
        var bestDistanceSquared = float.PositiveInfinity;

        foreach (var preyIndex in _preyCandidates)
        {
            var prey = state.SmallPrey[preyIndex];
            if (prey.Calories <= 0f || prey.Health <= 0f)
            {
                continue;
            }

            var toPrey = prey.Position - creature.Position;
            var centerDistanceSquared = toPrey.LengthSquared;
            var centerDistance = MathF.Sqrt(centerDistanceSquared);
            var edgeDistance = Math.Max(0f, centerDistance - bodyRadius - prey.Radius);
            if (edgeDistance > MathF.Max(_biteRangePadding, _grabRangePadding))
            {
                continue;
            }

            if (edgeDistance < bestEdgeDistance
                || (Math.Abs(edgeDistance - bestEdgeDistance) <= 0.0001f && centerDistanceSquared < bestDistanceSquared))
            {
                best = new SmallPreyContact(preyIndex, prey.Id);
                bestEdgeDistance = edgeDistance;
                bestDistanceSquared = centerDistanceSquared;
            }
        }

        return best;
    }

    private void GrabSmallPrey(WorldState state, int preyIndex, ref CreatureState creature)
    {
        var prey = state.SmallPrey[preyIndex];
        var grabPressure = Math.Clamp(creature.Actions.GrabOutput, 0f, 1f);
        if (grabPressure <= prey.GrabPressure)
        {
            return;
        }

        prey.HeldByCreatureId = creature.Id;
        prey.GrabPressure = grabPressure;
        prey.Velocity = SimVector2.Zero;
        state.SmallPrey[preyIndex] = prey;

        creature.HeldSmallPreyId = prey.Id;
        if (creature.HeldCreatureId == default)
        {
            creature.GrabStrength = grabPressure;
        }
    }

    private void BiteSmallPrey(
        WorldState state,
        int preyIndex,
        ref CreatureState creature,
        CreatureGenome genome,
        float deltaSeconds)
    {
        var biteCost = CreatureCombat.BiteEnergyCostPerSecond(creature, genome, _biteEnergyCostPerSecond)
            * deltaSeconds;
        if (biteCost > 0f && creature.Energy <= biteCost)
        {
            return;
        }

        var damage = CreatureCombat.BiteDamagePerSecond(creature, genome, _biteDamagePerSecond) * deltaSeconds;
        if (damage <= 0f)
        {
            return;
        }

        var prey = state.SmallPrey[preyIndex];
        prey.Health = Math.Max(0f, prey.Health - damage);
        creature.Energy -= biteCost;
        var ledger = creature.LastEnergyLedger;
        ledger.AttackCalories += biteCost;
        creature.LastEnergyLedger = ledger;
        creature.LastAttackDamageDealt += damage;
        state.Stats.RecordAttackDamage(state.Bounds, prey.Position, damage);

        if (prey.Health <= 0f)
        {
            SpawnSmallPreyMeat(state, prey, creature.Id);
            prey.Calories = 0f;
            prey.Health = 0f;
            state.Stats.RecordSmallPreyKilled();
        }

        state.SmallPrey[preyIndex] = prey;
    }

    private bool IsInBiteRangeAndArc(SmallPreyState prey, CreatureState creature, float bodyRadius)
    {
        var toPrey = prey.Position - creature.Position;
        var centerDistance = toPrey.Length;
        var edgeDistance = Math.Max(0f, centerDistance - bodyRadius - prey.Radius);
        if (edgeDistance > _biteRangePadding)
        {
            return false;
        }

        if (centerDistance <= 0.0001f)
        {
            return true;
        }

        var forward = SimVector2.FromAngle(creature.HeadingRadians);
        return SimVector2.Dot(toPrey / centerDistance, forward) >= 0f;
    }

    private void SpawnSmallPreyMeat(WorldState state, SmallPreyState prey, EntityId attackerId)
    {
        if (prey.Calories <= 0f)
        {
            return;
        }

        state.SpawnResourcePatch(new ResourcePatchState
        {
            Kind = ResourceKind.Meat,
            Position = prey.Position,
            Radius = Math.Clamp(prey.Radius * 0.9f, 1f, 6f),
            Calories = prey.Calories,
            MaxCalories = prey.Calories,
            DecayCaloriesPerSecond = _meatDecayCaloriesPerSecond,
            MeatAgeSeconds = 0f,
            FreshKillAttackerId = attackerId,
            FreshKillPreyId = prey.Id,
            FreshKillSecondsRemaining = FreshKillCreditSeconds
        });
    }

    private static void ResetHoldState(WorldState state)
    {
        for (var i = 0; i < state.SmallPrey.Count; i++)
        {
            var prey = state.SmallPrey[i];
            prey.HeldByCreatureId = default;
            prey.GrabPressure = 0f;
            state.SmallPrey[i] = prey;
        }

        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            creature.HeldSmallPreyId = default;
            state.Creatures[i] = creature;
        }
    }

    private static float ValidateNonNegative(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Small prey interaction setting must be finite and non-negative.");
    }

    private readonly record struct SmallPreyContact(int PreyIndex, EntityId PreyId)
    {
        public static SmallPreyContact None { get; } = new(-1, default);
    }
}
