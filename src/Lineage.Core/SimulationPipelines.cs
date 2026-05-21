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
        float eggEnergyCostPerSecond = 0f,
        float eggEnvironmentalDamagePerSecond = 0f,
        float deathMeatCaloriesPerBodyRadius = 4f,
        float deathMeatEnergyFraction = 0.35f,
        float meatDecayCaloriesPerSecond = 0.03f,
        float biteDamagePerSecond = 0.25f,
        float biteEnergyCostPerSecond = 0.12f,
        float biteRangePadding = 1f,
        bool relocateDepletedResources = true)
    {
        var spatialIndex = new UniformSpatialIndex(spatialCellSize);

        return
        [
            new ResourceRegrowthSystem(relocateDepletedResources),
            new MetabolismSystem(
                bodyRadiusEnergyCostPerSecond,
                maxSpeedEnergyCostPerSecond,
                turnRateEnergyCostPerSecond,
                senseRadiusEnergyCostPerSecond,
                visionAngleEnergyCostPerSecond,
                eatRateEnergyCostPerSecond),
            new SpatialIndexRebuildSystem(spatialIndex),
            new SimpleForagingSystem(spatialIndex),
            new MovementSystem(),
            new SpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex),
            new ReproductionSystem(),
            new EggEnvironmentalDamageSystem(eggEnvironmentalDamagePerSecond),
            new EggSystem(eggEnergyCostPerSecond),
            new DeathSystem(deathMeatCaloriesPerBodyRadius, deathMeatEnergyFraction, meatDecayCaloriesPerSecond),
            new StatsRecordingSystem(statsSnapshotIntervalTicks)
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
        float eggEnergyCostPerSecond = 0f,
        float eggEnvironmentalDamagePerSecond = 0f,
        float deathMeatCaloriesPerBodyRadius = 4f,
        float deathMeatEnergyFraction = 0.35f,
        float meatDecayCaloriesPerSecond = 0.03f,
        float biteDamagePerSecond = 0.25f,
        float biteEnergyCostPerSecond = 0.12f,
        float biteRangePadding = 1f,
        bool relocateDepletedResources = true)
    {
        var spatialIndex = new UniformSpatialIndex(spatialCellSize);

        return
        [
            new ResourceRegrowthSystem(relocateDepletedResources),
            new MetabolismSystem(
                bodyRadiusEnergyCostPerSecond,
                maxSpeedEnergyCostPerSecond,
                turnRateEnergyCostPerSecond,
                senseRadiusEnergyCostPerSecond,
                visionAngleEnergyCostPerSecond,
                eatRateEnergyCostPerSecond),
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex),
            new NeuralControllerSystem(),
            new MovementSystem(),
            new SpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex, requireEatIntent: true),
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
            new StatsRecordingSystem(statsSnapshotIntervalTicks)
        ];
    }
}
