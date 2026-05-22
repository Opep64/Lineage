namespace Lineage.Core;

/// <summary>
/// Factory methods for common simulation system pipelines.
/// </summary>
public static class SimulationPipelines
{
    public static ISimulationSystem[] CreateMinimalLifeLoop(
        float spatialCellSize = 64f,
        int statsSnapshotIntervalTicks = 1,
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
        float eggEnergyCostPerSecond = 0f,
        float eggEnvironmentalDamagePerSecond = 0f,
        float deathMeatCaloriesPerBodyRadius = 4f,
        float deathMeatEnergyFraction = 0.35f,
        float meatDecayCaloriesPerSecond = 0.03f,
        float meatScentRangeMultiplier = 2f,
        float meatScentCaloriesForFullStrength = 60f,
        float meatScentDensitySaturation = 1f,
        float biteDamagePerSecond = 0.25f,
        float biteEnergyCostPerSecond = 0.12f,
        float biteRangePadding = 1f,
        bool relocateDepletedResources = true,
        float resourceClusterStrength = 0f,
        float resourceClusterRadius = 180f,
        BiomePressureProfile? biomeMovementCostProfile = null,
        BiomePressureProfile? biomeBasalCostProfile = null,
        BiomePressureProfile? biomeSpeedProfile = null,
        float movementSpeedCostExponent = 1f)
    {
        var spatialIndex = new UniformSpatialIndex(spatialCellSize);

        return
        [
            new ResourceRegrowthSystem(relocateDepletedResources, resourceClusterStrength, resourceClusterRadius),
            new MetabolismSystem(
                bodyRadiusEnergyCostPerSecond,
                maxSpeedEnergyCostPerSecond,
                turnRateEnergyCostPerSecond,
                senseRadiusEnergyCostPerSecond,
                visionAngleEnergyCostPerSecond,
                eatRateEnergyCostPerSecond,
                gutCapacityEnergyCostPerSecond,
                digestionRateEnergyCostPerSecond,
                biteStrengthEnergyCostPerSecond,
                damageResistanceEnergyCostPerSecond,
                biomeBasalCostProfile),
            new SpatialIndexRebuildSystem(spatialIndex),
            new SimpleForagingSystem(spatialIndex),
            new MovementSystem(biomeMovementCostProfile, biomeSpeedProfile, movementSpeedCostExponent),
            new CreatureSpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex),
            new DigestionSystem(),
            new ReproductionSystem(),
            new EggEnvironmentalDamageSystem(eggEnvironmentalDamagePerSecond),
            new EggSystem(eggEnergyCostPerSecond),
            new DeathSystem(deathMeatCaloriesPerBodyRadius, deathMeatEnergyFraction, meatDecayCaloriesPerSecond),
            new StatsRecordingSystem(statsSnapshotIntervalTicks, biomeMovementCostProfile, biomeBasalCostProfile, biomeSpeedProfile)
        ];
    }

    public static ISimulationSystem[] CreateNeuralLifeLoop(
        float spatialCellSize = 64f,
        int statsSnapshotIntervalTicks = 1,
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
        float eggEnergyCostPerSecond = 0f,
        float eggEnvironmentalDamagePerSecond = 0f,
        float deathMeatCaloriesPerBodyRadius = 4f,
        float deathMeatEnergyFraction = 0.35f,
        float meatDecayCaloriesPerSecond = 0.03f,
        float meatScentRangeMultiplier = 2f,
        float meatScentCaloriesForFullStrength = 60f,
        float meatScentDensitySaturation = 1f,
        float biteDamagePerSecond = 0.25f,
        float biteEnergyCostPerSecond = 0.12f,
        float biteRangePadding = 1f,
        bool relocateDepletedResources = true,
        float resourceClusterStrength = 0f,
        float resourceClusterRadius = 180f,
        BiomePressureProfile? biomeMovementCostProfile = null,
        BiomePressureProfile? biomeBasalCostProfile = null,
        BiomePressureProfile? biomeSpeedProfile = null,
        float movementSpeedCostExponent = 1f)
    {
        var spatialIndex = new UniformSpatialIndex(spatialCellSize);

        return
        [
            new ResourceRegrowthSystem(relocateDepletedResources, resourceClusterStrength, resourceClusterRadius),
            new MetabolismSystem(
                bodyRadiusEnergyCostPerSecond,
                maxSpeedEnergyCostPerSecond,
                turnRateEnergyCostPerSecond,
                senseRadiusEnergyCostPerSecond,
                visionAngleEnergyCostPerSecond,
                eatRateEnergyCostPerSecond,
                gutCapacityEnergyCostPerSecond,
                digestionRateEnergyCostPerSecond,
                biteStrengthEnergyCostPerSecond,
                damageResistanceEnergyCostPerSecond,
                biomeBasalCostProfile),
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(
                spatialIndex,
                meatScentRangeMultiplier,
                meatScentCaloriesForFullStrength,
                meatScentDensitySaturation,
                biomeSpeedProfile),
            new NeuralControllerSystem(),
            new MovementSystem(biomeMovementCostProfile, biomeSpeedProfile, movementSpeedCostExponent),
            new CreatureSpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex, requireEatIntent: true),
            new DigestionSystem(),
            new ReproductionSystem(requireReproductionIntent: true),
            new EggEnvironmentalDamageSystem(eggEnvironmentalDamagePerSecond),
            new EggSystem(eggEnergyCostPerSecond),
            new CreatureAttackSystem(
                spatialIndex,
                biteDamagePerSecond,
                biteEnergyCostPerSecond,
                biteRangePadding,
                requireAttackIntent: true),
            new DeathSystem(deathMeatCaloriesPerBodyRadius, deathMeatEnergyFraction, meatDecayCaloriesPerSecond),
            new StatsRecordingSystem(statsSnapshotIntervalTicks, biomeMovementCostProfile, biomeBasalCostProfile, biomeSpeedProfile)
        ];
    }
}
