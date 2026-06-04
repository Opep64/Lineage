namespace Lineage.Core;

/// <summary>
/// Removes creatures that have exhausted their survival state.
/// </summary>
public sealed class DeathSystem : ISimulationSystem
{
    private const float FreshKillCreditSeconds = 20f;

    private readonly float _meatCaloriesPerBodyRadius;
    private readonly float _meatEnergyFraction;
    private readonly float _meatDecayCaloriesPerSecond;

    public DeathSystem(
        float meatCaloriesPerBodyRadius = 4f,
        float meatEnergyFraction = 0.35f,
        float meatDecayCaloriesPerSecond = 0.03f)
    {
        _meatCaloriesPerBodyRadius = ValidateNonNegative(meatCaloriesPerBodyRadius, nameof(meatCaloriesPerBodyRadius));
        _meatEnergyFraction = ValidateProbability(meatEnergyFraction, nameof(meatEnergyFraction));
        _meatDecayCaloriesPerSecond = ValidateNonNegative(meatDecayCaloriesPerSecond, nameof(meatDecayCaloriesPerSecond));
    }

    public void Update(WorldState state, float deltaSeconds)
    {
        var writeIndex = 0;

        for (var readIndex = 0; readIndex < state.Creatures.Count; readIndex++)
        {
            var creature = state.Creatures[readIndex];
            var genome = state.GetGenome(creature.GenomeId);
            var oldAgeDeathProbability = CreatureMetabolism.OldAgeDeathProbability(creature, genome, deltaSeconds);
            var diedOfOldAge = oldAgeDeathProbability >= 1f
                || (oldAgeDeathProbability > 0f && state.Random.NextSingle() < oldAgeDeathProbability);
            if (creature.Energy <= 0f || creature.Health <= 0f || diedOfOldAge)
            {
                var reason = GetDeathReason(creature, diedOfOldAge);
                SpawnMeatResource(state, creature, reason);
                state.MarkCreatureDead(
                    creature.Id,
                    reason,
                    state.Biomes.GetKindAt(creature.Position),
                    creature.Position,
                    creature.MaxXReached,
                    reason == CreatureDeathReason.Injury
                        ? creature.LastDamagingCreatureId
                        : default);
                continue;
            }

            if (writeIndex != readIndex)
            {
                state.Creatures[writeIndex] = creature;
            }

            writeIndex++;
        }

        if (writeIndex < state.Creatures.Count)
        {
            state.Creatures.RemoveRange(writeIndex, state.Creatures.Count - writeIndex);
        }
    }

    private void SpawnMeatResource(WorldState state, CreatureState creature, CreatureDeathReason reason)
    {
        var genome = state.GetGenome(creature.GenomeId);
        var bodyRadius = CreatureGrowth.EffectiveBodyRadius(creature, genome);
        var calories = bodyRadius * _meatCaloriesPerBodyRadius
            + (Math.Max(0f, creature.Energy) + Math.Max(0f, creature.FatCalories)) * _meatEnergyFraction;
        if (calories <= 0f)
        {
            return;
        }

        state.SpawnResourcePatch(new ResourcePatchState
        {
            Kind = ResourceKind.Meat,
            Position = creature.Position,
            Radius = Math.Clamp(bodyRadius * 0.85f, 1.5f, 12f),
            Calories = calories,
            MaxCalories = calories,
            RegrowthCaloriesPerSecond = 0f,
            DecayCaloriesPerSecond = _meatDecayCaloriesPerSecond,
            MeatAgeSeconds = 0f,
            FreshKillAttackerId = reason == CreatureDeathReason.Injury
                ? creature.LastDamagingCreatureId
                : default,
            FreshKillPreyId = reason == CreatureDeathReason.Injury && creature.LastDamagingCreatureId != default
                ? creature.Id
                : default,
            FreshKillSecondsRemaining = reason == CreatureDeathReason.Injury && creature.LastDamagingCreatureId != default
                ? FreshKillCreditSeconds
                : 0f
        });
    }

    private static CreatureDeathReason GetDeathReason(CreatureState creature, bool diedOfOldAge)
    {
        if (creature.Energy <= 0f)
        {
            return CreatureDeathReason.Starvation;
        }

        if (creature.Health <= 0f)
        {
            return creature.LastAttackDamageTaken > 0f
                ? CreatureDeathReason.Injury
                : creature.LastRottenMeatDamage > 0f
                    ? CreatureDeathReason.RottenMeat
                    : CreatureDeathReason.Injury;
        }

        if (diedOfOldAge)
        {
            return CreatureDeathReason.OldAge;
        }

        return CreatureDeathReason.Unknown;
    }

    private static float ValidateNonNegative(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Death meat setting must be finite and non-negative.");
    }

    private static float ValidateProbability(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f && value <= 1f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Death meat energy fraction must be finite and between 0 and 1.");
    }
}
