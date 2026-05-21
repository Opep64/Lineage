namespace Lineage.Core;

/// <summary>
/// Applies baseline energy upkeep, trait upkeep, and age/cooldown progression.
/// </summary>
public sealed class MetabolismSystem(
    float bodyRadiusEnergyCostPerSecond = 0f,
    float maxSpeedEnergyCostPerSecond = 0f,
    float turnRateEnergyCostPerSecond = 0f,
    float senseRadiusEnergyCostPerSecond = 0f,
    float visionAngleEnergyCostPerSecond = 0f,
    float eatRateEnergyCostPerSecond = 0f) : ISimulationSystem
{
    private readonly float _bodyRadiusEnergyCostPerSecond =
        ValidateCost(bodyRadiusEnergyCostPerSecond, nameof(bodyRadiusEnergyCostPerSecond));
    private readonly float _maxSpeedEnergyCostPerSecond =
        ValidateCost(maxSpeedEnergyCostPerSecond, nameof(maxSpeedEnergyCostPerSecond));
    private readonly float _turnRateEnergyCostPerSecond =
        ValidateCost(turnRateEnergyCostPerSecond, nameof(turnRateEnergyCostPerSecond));
    private readonly float _senseRadiusEnergyCostPerSecond =
        ValidateCost(senseRadiusEnergyCostPerSecond, nameof(senseRadiusEnergyCostPerSecond));
    private readonly float _visionAngleEnergyCostPerSecond =
        ValidateCost(visionAngleEnergyCostPerSecond, nameof(visionAngleEnergyCostPerSecond));
    private readonly float _eatRateEnergyCostPerSecond =
        ValidateCost(eatRateEnergyCostPerSecond, nameof(eatRateEnergyCostPerSecond));

    public void Update(WorldState state, float deltaSeconds)
    {
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);

            creature.AgeSeconds += deltaSeconds;
            creature.SecondsSinceLastMeal += deltaSeconds;
            creature.ReproductionCooldownSeconds = Math.Max(
                0f,
                creature.ReproductionCooldownSeconds - deltaSeconds);
            var traitUpkeep =
                CreatureGrowth.EffectiveBodyRadius(creature, genome) * _bodyRadiusEnergyCostPerSecond
                + CreatureGrowth.EffectiveMaxSpeed(creature, genome) * _maxSpeedEnergyCostPerSecond
                + CreatureGrowth.EffectiveMaxTurnRadiansPerSecond(creature, genome) * _turnRateEnergyCostPerSecond
                + CreatureGrowth.EffectiveSenseRadius(creature, genome) * _senseRadiusEnergyCostPerSecond
                + CreatureGrowth.EffectiveVisionAngleRadians(creature, genome) * _visionAngleEnergyCostPerSecond
                + CreatureGrowth.EffectiveEatCaloriesPerSecond(creature, genome) * _eatRateEnergyCostPerSecond;
            creature.Energy -= (genome.BasalEnergyPerSecond + traitUpkeep) * deltaSeconds;

            state.Creatures[i] = creature;
        }
    }

    private static float ValidateCost(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Trait energy cost must be finite and non-negative.");
    }
}
