namespace Lineage.Core;

/// <summary>
/// Resolves close-range biting between living creatures.
/// </summary>
public sealed class CreatureAttackSystem(
    UniformSpatialIndex spatialIndex,
    float biteDamagePerSecond = 0.25f,
    float biteEnergyCostPerSecond = 0.12f,
    float biteRangePadding = 1f,
    bool requireAttackIntent = true) : ISimulationSystem
{
    private readonly List<int> _creatureCandidates = [];
    private readonly List<float> _damageByCreature = [];
    private readonly List<EntityId> _damageSourceByCreature = [];
    private readonly float _biteDamagePerSecond = ValidateNonNegative(biteDamagePerSecond, nameof(biteDamagePerSecond));
    private readonly float _biteEnergyCostPerSecond = ValidateNonNegative(biteEnergyCostPerSecond, nameof(biteEnergyCostPerSecond));
    private readonly float _biteRangePadding = ValidateNonNegative(biteRangePadding, nameof(biteRangePadding));

    public void Update(WorldState state, float deltaSeconds)
    {
        EnsureDamageBufferSize(state.Creatures.Count);

        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var attacker = state.Creatures[i];
            var attackerGenome = state.GetGenome(attacker.GenomeId);
            var attackerRadius = CreatureGrowth.EffectiveBodyRadius(attacker, attackerGenome);
            spatialIndex.AddCreatureCandidates(
                state,
                attacker.Position,
                attackerRadius + _biteRangePadding + 12f,
                _creatureCandidates);

            attacker.IsTouchingCreature = false;
            attacker.CreatureContactId = default;
            attacker.CreatureContactEdgeDistance = 0f;
            attacker.LastAttackDamageDealt = 0f;
            attacker.LastAttackDamageTaken = 0f;

            var contact = FindBestBiteContact(state, i, attacker, attackerRadius);
            if (contact.TargetIndex < 0)
            {
                state.Creatures[i] = attacker;
                continue;
            }

            attacker.IsTouchingCreature = true;
            attacker.CreatureContactId = contact.TargetId;
            attacker.CreatureContactEdgeDistance = contact.EdgeDistance;

            if (requireAttackIntent && !attacker.Actions.WantsAttack)
            {
                state.Creatures[i] = attacker;
                continue;
            }

            var biteCost = CreatureCombat.BiteEnergyCostPerSecond(attacker, attackerGenome, _biteEnergyCostPerSecond)
                * deltaSeconds;
            if (biteCost > 0f && attacker.Energy <= biteCost)
            {
                state.Creatures[i] = attacker;
                continue;
            }

            var target = state.Creatures[contact.TargetIndex];
            var targetGenome = state.GetGenome(target.GenomeId);
            var damage = CreatureCombat.ApplyDamageResistance(
                CreatureCombat.BiteDamagePerSecond(attacker, attackerGenome, _biteDamagePerSecond) * deltaSeconds,
                target,
                targetGenome);
            if (damage <= 0f)
            {
                state.Creatures[i] = attacker;
                continue;
            }

            attacker.Energy -= biteCost;
            attacker.LastAttackDamageDealt = damage;
            state.Stats.RecordAttackDamage(state.Bounds, target.Position, damage);
            _damageByCreature[contact.TargetIndex] += damage;
            _damageSourceByCreature[contact.TargetIndex] = attacker.Id;
            state.Creatures[i] = attacker;
        }

        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var damage = _damageByCreature[i];
            if (damage <= 0f)
            {
                continue;
            }

            var creature = state.Creatures[i];
            creature.Health = Math.Max(0f, creature.Health - damage);
            creature.LastAttackDamageTaken = damage;
            if (_damageSourceByCreature[i] != default)
            {
                creature.LastDamagingCreatureId = _damageSourceByCreature[i];
            }

            state.Creatures[i] = creature;
        }
    }

    private CreatureContact FindBestBiteContact(
        WorldState state,
        int attackerIndex,
        CreatureState attacker,
        float attackerRadius)
    {
        var best = CreatureContact.None;
        var bestEdgeDistance = float.PositiveInfinity;
        var bestDistanceSquared = float.PositiveInfinity;
        var forward = SimVector2.FromAngle(attacker.HeadingRadians);

        foreach (var targetIndex in _creatureCandidates)
        {
            if (targetIndex == attackerIndex)
            {
                continue;
            }

            var target = state.Creatures[targetIndex];
            if (target.Id == attacker.Id || target.Health <= 0f || target.Energy <= 0f)
            {
                continue;
            }

            var targetGenome = state.GetGenome(target.GenomeId);
            var targetRadius = CreatureGrowth.EffectiveBodyRadius(target, targetGenome);
            var toTarget = target.Position - attacker.Position;
            var centerDistance = toTarget.Length;
            var edgeDistance = Math.Max(0f, centerDistance - attackerRadius - targetRadius);
            if (edgeDistance > _biteRangePadding || !IsInBiteArc(toTarget, centerDistance, forward))
            {
                continue;
            }

            var distanceSquared = centerDistance * centerDistance;
            if (edgeDistance < bestEdgeDistance
                || (Math.Abs(edgeDistance - bestEdgeDistance) <= 0.0001f && distanceSquared < bestDistanceSquared))
            {
                best = new CreatureContact(targetIndex, target.Id, edgeDistance);
                bestEdgeDistance = edgeDistance;
                bestDistanceSquared = distanceSquared;
            }
        }

        return best;
    }

    private void EnsureDamageBufferSize(int count)
    {
        if (_damageByCreature.Count > count)
        {
            _damageByCreature.RemoveRange(count, _damageByCreature.Count - count);
        }

        if (_damageSourceByCreature.Count > count)
        {
            _damageSourceByCreature.RemoveRange(count, _damageSourceByCreature.Count - count);
        }

        while (_damageByCreature.Count < count)
        {
            _damageByCreature.Add(0f);
        }

        while (_damageSourceByCreature.Count < count)
        {
            _damageSourceByCreature.Add(default);
        }

        for (var i = 0; i < count; i++)
        {
            _damageByCreature[i] = 0f;
            _damageSourceByCreature[i] = default;
        }
    }

    private static bool IsInBiteArc(SimVector2 toTarget, float centerDistance, SimVector2 forward)
    {
        if (centerDistance <= 0.0001f)
        {
            return true;
        }

        return SimVector2.Dot(toTarget / centerDistance, forward) >= 0f;
    }

    private static float ValidateNonNegative(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Creature attack setting must be finite and non-negative.");
    }

    private readonly record struct CreatureContact(int TargetIndex, EntityId TargetId, float EdgeDistance)
    {
        public static CreatureContact None { get; } = new(-1, default, 0f);
    }
}
