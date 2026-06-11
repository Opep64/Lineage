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
    float eatRateEnergyCostPerSecond = 0f,
    float gutCapacityEnergyCostPerSecond = 0f,
    float digestionRateEnergyCostPerSecond = 0f,
    float biteStrengthEnergyCostPerSecond = 0f,
    float damageResistanceEnergyCostPerSecond = 0f,
    float plantSpecializationEnergyCostPerSecond = 0f,
    float memoryEnergyCostPerSecond = 0f,
    float rtNeatHiddenNodeEnergyCostPerSecond = MetabolismSystem.DefaultRtNeatHiddenNodeEnergyCostPerSecond,
    float rtNeatEnabledConnectionEnergyCostPerSecond = MetabolismSystem.DefaultRtNeatEnabledConnectionEnergyCostPerSecond,
    BiomePressureProfile? biomeBasalCostProfile = null,
    float thermalMismatchBasalCostMultiplier = 0f) : ISimulationSystem
{
    public const float DefaultRtNeatHiddenNodeEnergyCostPerSecond = 0f;
    public const float DefaultRtNeatEnabledConnectionEnergyCostPerSecond = 0f;
    private const float VisionAngleUpkeepExponent = 3f;

    private readonly BiomePressureProfile _biomeBasalCostProfile =
        BiomePressureProfile.Validate(biomeBasalCostProfile ?? BiomePressureProfile.Neutral, nameof(biomeBasalCostProfile));
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
    private readonly float _gutCapacityEnergyCostPerSecond =
        ValidateCost(gutCapacityEnergyCostPerSecond, nameof(gutCapacityEnergyCostPerSecond));
    private readonly float _digestionRateEnergyCostPerSecond =
        ValidateCost(digestionRateEnergyCostPerSecond, nameof(digestionRateEnergyCostPerSecond));
    private readonly float _biteStrengthEnergyCostPerSecond =
        ValidateCost(biteStrengthEnergyCostPerSecond, nameof(biteStrengthEnergyCostPerSecond));
    private readonly float _damageResistanceEnergyCostPerSecond =
        ValidateCost(damageResistanceEnergyCostPerSecond, nameof(damageResistanceEnergyCostPerSecond));
    private readonly float _plantSpecializationEnergyCostPerSecond =
        ValidateCost(plantSpecializationEnergyCostPerSecond, nameof(plantSpecializationEnergyCostPerSecond));
    private readonly float _memoryEnergyCostPerSecond =
        ValidateCost(memoryEnergyCostPerSecond, nameof(memoryEnergyCostPerSecond));
    private readonly float _rtNeatHiddenNodeEnergyCostPerSecond =
        ValidateCost(rtNeatHiddenNodeEnergyCostPerSecond, nameof(rtNeatHiddenNodeEnergyCostPerSecond));
    private readonly float _rtNeatEnabledConnectionEnergyCostPerSecond =
        ValidateCost(rtNeatEnabledConnectionEnergyCostPerSecond, nameof(rtNeatEnabledConnectionEnergyCostPerSecond));
    private readonly float _thermalMismatchBasalCostMultiplier =
        ValidateCost(thermalMismatchBasalCostMultiplier, nameof(thermalMismatchBasalCostMultiplier));
    private BrainTopologyUpkeepCacheEntry?[] _brainTopologyUpkeepCache = [];

    public void Update(WorldState state, float deltaSeconds)
    {
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);

            creature.LastEnergyLedger = default;
            creature.AgeSeconds += deltaSeconds;
            creature.SecondsSinceLastMeal += deltaSeconds;
            creature.ReproductionCooldownSeconds = Math.Max(
                0f,
                creature.ReproductionCooldownSeconds
                    - deltaSeconds * CreatureMetabolism.CooldownRecoveryMultiplier(genome));
            var effectiveBodyRadius = CreatureGrowth.EffectiveBodyRadius(creature, genome);
            var effectiveSenseRadius = CreatureGrowth.EffectiveSenseRadius(creature, genome);
            var effectiveVisionAngle = CreatureGrowth.EffectiveVisionAngleRadians(creature, genome);
            var effectiveDamageResistance = CreatureGrowth.EffectiveDamageResistance(creature, genome);
            var bodyUpkeep = BaselineScaledQuadraticUpkeep(
                effectiveBodyRadius,
                CreatureGenome.Baseline.BodyRadius,
                _bodyRadiusEnergyCostPerSecond);
            var speedUpkeep = CreatureGrowth.EffectiveMaxSpeed(creature, genome) * _maxSpeedEnergyCostPerSecond;
            var turnUpkeep = CreatureGrowth.EffectiveMaxTurnRadiansPerSecond(creature, genome) * _turnRateEnergyCostPerSecond;
            var senseUpkeep = BaselineScaledQuadraticUpkeep(
                effectiveSenseRadius,
                CreatureGenome.Baseline.SenseRadius,
                _senseRadiusEnergyCostPerSecond);
            var visionUpkeep = BaselineScaledPowerUpkeep(
                effectiveVisionAngle,
                CreatureGenome.Baseline.VisionAngleRadians,
                _visionAngleEnergyCostPerSecond,
                VisionAngleUpkeepExponent);
            var eatRateUpkeep = CreatureGrowth.EffectiveEatCaloriesPerSecond(creature, genome) * _eatRateEnergyCostPerSecond;
            var gutCapacityUpkeep = CreatureGrowth.EffectiveGutCapacityCalories(creature, genome) * _gutCapacityEnergyCostPerSecond;
            var digestionUpkeep = CreatureGrowth.EffectiveDigestionCaloriesPerSecond(creature, genome) * _digestionRateEnergyCostPerSecond;
            var biteStrengthUpkeep = CreatureGrowth.EffectiveBiteStrength(creature, genome) * _biteStrengthEnergyCostPerSecond;
            var damageResistanceUpkeep = BaselineScaledQuadraticUpkeep(
                effectiveDamageResistance,
                CreatureGenome.Baseline.DamageResistance,
                _damageResistanceEnergyCostPerSecond);
            var plantSpecializationUpkeep = CreatureDigestion.PlantSpecializationUpkeepFactor(genome) * _plantSpecializationEnergyCostPerSecond;
            var memoryUpkeep = Math.Clamp(creature.MemoryVector.Length, 0f, 1f) * _memoryEnergyCostPerSecond;
            var brainUpkeep = BrainTopologyUpkeep(state, creature.BrainId);
            var biomeBasalCostMultiplier = _biomeBasalCostProfile.For(state.Biomes.GetKindAt(creature.Position));
            var thermalMismatch = _thermalMismatchBasalCostMultiplier > 0f
                ? CreatureThermal.ThermalMismatch(state.GetTemperatureAt(creature.Position), genome)
                : 0f;
            var thermalBasalCostMultiplier = 1f + thermalMismatch * _thermalMismatchBasalCostMultiplier;
            var basalUpkeep = genome.BasalEnergyPerSecond
                * CreatureMetabolism.BasalCostMultiplier(genome)
                * biomeBasalCostMultiplier
                * thermalBasalCostMultiplier;

            var ledger = new CreatureEnergyLedger
            {
                BasalCalories = basalUpkeep * deltaSeconds,
                BodyUpkeepCalories = bodyUpkeep * deltaSeconds,
                SpeedUpkeepCalories = speedUpkeep * deltaSeconds,
                TurnUpkeepCalories = turnUpkeep * deltaSeconds,
                SenseUpkeepCalories = senseUpkeep * deltaSeconds,
                VisionUpkeepCalories = visionUpkeep * deltaSeconds,
                EatRateUpkeepCalories = eatRateUpkeep * deltaSeconds,
                GutCapacityUpkeepCalories = gutCapacityUpkeep * deltaSeconds,
                DigestionUpkeepCalories = digestionUpkeep * deltaSeconds,
                BiteStrengthUpkeepCalories = biteStrengthUpkeep * deltaSeconds,
                DamageResistanceUpkeepCalories = damageResistanceUpkeep * deltaSeconds,
                PlantSpecializationUpkeepCalories = plantSpecializationUpkeep * deltaSeconds,
                MemoryUpkeepCalories = memoryUpkeep * deltaSeconds,
                BrainUpkeepCalories = brainUpkeep * deltaSeconds
            };
            creature.LastEnergyLedger = ledger;
            creature.Energy -= ledger.BasalCalories + ledger.TraitUpkeepCalories();

            state.Creatures[i] = creature;
        }
    }

    private float BrainTopologyUpkeep(WorldState state, int brainId)
    {
        if (brainId < 0
            || !state.TryGetBrain(brainId, out var brain)
            || brain?.RtNeat is not { } rtNeat)
        {
            return 0f;
        }

        EnsureBrainTopologyUpkeepCacheCapacity(brainId);
        var cacheEntry = _brainTopologyUpkeepCache[brainId];
        if (cacheEntry is not null && ReferenceEquals(cacheEntry.Brain, rtNeat))
        {
            return cacheEntry.Upkeep;
        }

        var upkeep = rtNeat.HiddenNodeCount * _rtNeatHiddenNodeEnergyCostPerSecond
            + rtNeat.EnabledConnectionCount * _rtNeatEnabledConnectionEnergyCostPerSecond;
        _brainTopologyUpkeepCache[brainId] = new BrainTopologyUpkeepCacheEntry(rtNeat, upkeep);
        return upkeep;
    }

    private void EnsureBrainTopologyUpkeepCacheCapacity(int brainId)
    {
        if (_brainTopologyUpkeepCache.Length > brainId)
        {
            return;
        }

        Array.Resize(ref _brainTopologyUpkeepCache, Math.Max(brainId + 1, _brainTopologyUpkeepCache.Length * 2));
    }

    private static float ValidateCost(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Energy cost must be finite and non-negative.");
    }

    private static float BaselineScaledQuadraticUpkeep(float value, float baselineValue, float costPerBaselineUnit)
    {
        if (costPerBaselineUnit <= 0f || value <= 0f)
        {
            return 0f;
        }

        var baseline = Math.Max(0.000001f, baselineValue);
        return value * value / baseline * costPerBaselineUnit;
    }

    private static float BaselineScaledPowerUpkeep(
        float value,
        float baselineValue,
        float costPerBaselineUnit,
        float exponent)
    {
        if (costPerBaselineUnit <= 0f || value <= 0f)
        {
            return 0f;
        }

        var baseline = Math.Max(0.000001f, baselineValue);
        var ratio = Math.Max(0.000001f, value / baseline);
        return baseline * MathF.Pow(ratio, exponent) * costPerBaselineUnit;
    }
}

internal sealed class BrainTopologyUpkeepCacheEntry(RtNeatBrainGenome brain, float upkeep)
{
    public RtNeatBrainGenome Brain { get; } = brain;

    public float Upkeep { get; } = upkeep;
}
