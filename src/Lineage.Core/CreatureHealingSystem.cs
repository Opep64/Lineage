namespace Lineage.Core;

/// <summary>
/// Restores health slowly after a creature has avoided damage for a while.
/// </summary>
public sealed class CreatureHealingSystem(
    float healingDelaySeconds = CreatureHealingSystem.DefaultHealingDelaySeconds,
    float healingHealthFractionPerSecond = CreatureHealingSystem.DefaultHealingHealthFractionPerSecond,
    float healingEnergyCostPerHealth = CreatureHealingSystem.DefaultHealingEnergyCostPerHealth,
    float healingMinimumEnergy = CreatureHealingSystem.DefaultHealingMinimumEnergy) : ISimulationSystem
{
    public const float DefaultHealingDelaySeconds = 25f;
    public const float DefaultHealingHealthFractionPerSecond = 0.006f;
    public const float DefaultHealingEnergyCostPerHealth = 3f;
    public const float DefaultHealingMinimumEnergy = 30f;

    private readonly float _healingDelaySeconds =
        ValidateNonNegative(healingDelaySeconds, nameof(healingDelaySeconds));
    private readonly float _healingHealthFractionPerSecond =
        ValidateNonNegative(healingHealthFractionPerSecond, nameof(healingHealthFractionPerSecond));
    private readonly float _healingEnergyCostPerHealth =
        ValidateNonNegative(healingEnergyCostPerHealth, nameof(healingEnergyCostPerHealth));
    private readonly float _healingMinimumEnergy =
        ValidateNonNegative(healingMinimumEnergy, nameof(healingMinimumEnergy));

    public void Update(WorldState state, float deltaSeconds)
    {
        if (deltaSeconds <= 0f)
        {
            return;
        }

        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            creature.LastHealingReceived = 0f;
            creature.LastHealingEnergySpent = 0f;

            if (creature.Health <= 0f || creature.Energy <= 0f)
            {
                state.Creatures[i] = creature;
                continue;
            }

            if (_healingHealthFractionPerSecond <= 0f)
            {
                state.Creatures[i] = creature;
                continue;
            }

            if (creature.LastAttackDamageTaken > 0f
                || creature.LastCreatureCollisionDamageTaken > 0f
                || creature.LastRottenMeatDamage > 0f)
            {
                creature.SecondsSinceLastDamage = 0f;
                state.Creatures[i] = creature;
                continue;
            }

            if (!float.IsFinite(creature.SecondsSinceLastDamage) || creature.SecondsSinceLastDamage < 0f)
            {
                creature.SecondsSinceLastDamage = 0f;
            }

            creature.SecondsSinceLastDamage += deltaSeconds;
            if (creature.SecondsSinceLastDamage < _healingDelaySeconds
                || creature.Energy <= _healingMinimumEnergy)
            {
                state.Creatures[i] = creature;
                continue;
            }

            var maxHealth = OffspringDevelopment.JuvenileGrowthScale(creature.BirthInvestmentRatio);
            var missingHealth = maxHealth - creature.Health;
            if (missingHealth <= 0f)
            {
                state.Creatures[i] = creature;
                continue;
            }

            var healingByRate = _healingHealthFractionPerSecond
                * CreatureMetabolism.HealingRateMultiplier(genome)
                * maxHealth
                * deltaSeconds;
            var healingByEnergy = _healingEnergyCostPerHealth > 0f
                ? MathF.Max(0f, (creature.Energy - _healingMinimumEnergy) / _healingEnergyCostPerHealth)
                : float.PositiveInfinity;
            var healing = MathF.Min(missingHealth, MathF.Min(healingByRate, healingByEnergy));
            if (healing > 0f)
            {
                var energySpent = _healingEnergyCostPerHealth > 0f
                    ? healing * _healingEnergyCostPerHealth
                    : 0f;
                creature.Health += healing;
                creature.Energy -= energySpent;
                creature.LastHealingReceived = healing;
                creature.LastHealingEnergySpent = energySpent;
                var ledger = creature.LastEnergyLedger;
                ledger.HealingCalories += energySpent;
                creature.LastEnergyLedger = ledger;
            }

            state.Creatures[i] = creature;
        }
    }

    private static float ValidateNonNegative(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Healing settings must be finite and non-negative.");
    }
}
