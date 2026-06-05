using System.Text.Json.Serialization;

namespace Lineage.Core;

/// <summary>
/// Reproducible setup parameters for a simulation run.
/// </summary>
///
/// <remarks>
/// Scenarios keep world construction out of viewers and CLI tools. The first version
/// is deliberately compact: one seeded forager lineage, calorie resources, and the
/// selectable core pipeline.
/// </remarks>
public sealed record SimulationScenario
{
    public const ulong DefaultSeed = 20260519UL;
    public const float ResourceDensityAreaUnits = 1_000_000f;
    public const int DefaultStatsSnapshotIntervalTicks = 300;

    public string Name { get; init; } = "Balanced Foraging";

    public ulong Seed { get; init; } = DefaultSeed;

    public SimulationPipelineKind PipelineKind { get; init; } = SimulationPipelineKind.Neural;

    public BrainArchitectureKind BrainArchitectureKind { get; init; } = BrainArchitectureKind.HybridNeural;

    public InitialBrainKind InitialBrainKind { get; init; } = InitialBrainKind.SectorForager;

    public int BrainHiddenNodeCount { get; init; } = NeuralBrainSchema.DefaultHiddenNodeCount;

    /// <summary>
    /// Legacy JSON migration field. New scenarios should use <see cref="InitialBrainKind"/>.
    /// </summary>
    [JsonIgnore]
    public bool RandomizeInitialBrainWeights { get; init; }

    public bool EnableBiomes { get; init; } = true;

    public BiomeMapKind BiomeMapKind { get; init; } = BiomeMapKind.NaturalClimate;

    public bool EnableTemperature { get; init; } = true;

    public string? WorldMapPath { get; init; }

    public string? ManualBiomeMapPath { get; init; }

    public bool EnableObstacles { get; init; }

    public ObstacleMapKind ObstacleMapKind { get; init; } = ObstacleMapKind.None;

    public string? ManualObstacleMapPath { get; init; }

    public float ObstacleCellSize { get; init; } = 128f;

    public float WorldWidth { get; init; } = 4_000f;

    public float WorldHeight { get; init; } = 4_000f;

    public float BiomeCellSize { get; init; } = 100f;

    public float ResourceVoidBorderWidth { get; init; } = 160f;

    public float FixedDeltaSeconds { get; init; } = 1f / 30f;

    public float SpatialCellSize { get; init; } = 64f;

    public int WorldSenseIntervalTicks { get; init; } = CreatureSensingSystem.DefaultWorldSenseIntervalTicks;

    public float CloseSenseRefreshProximity { get; init; } = CreatureSensingSystem.DefaultCloseSenseRefreshProximity;

    public int CloseSenseRefreshMinimumTicks { get; init; } = CreatureSensingSystem.DefaultCloseSenseRefreshMinimumTicks;

    public float PlantPayoffTraceHalfLifeSeconds { get; init; } = CreatureSensingSystem.DefaultPlantPayoffTraceHalfLifeSeconds;

    public int SensingThreadCount { get; init; } = CreatureSensingSystem.DefaultSensingThreadCount;

    public bool EnableSectorVision { get; init; } = true;

    public bool ReuseNeuralActionsOnSkippedWorldSenses { get; init; }

    public int NeuralControllerThreadCount { get; init; } = NeuralControllerSystem.DefaultNeuralControllerThreadCount;

    public int StatsSnapshotIntervalTicks { get; init; } = DefaultStatsSnapshotIntervalTicks;

    public bool EnableExtinctPayloadPruning { get; init; }

    public int ExtinctPayloadPruneIntervalTicks { get; init; } = 1_000;

    public int InitialCreatureCount { get; init; } = 80;

    public InitialCreatureSpawnRegion InitialCreatureSpawnRegion { get; init; } = InitialCreatureSpawnRegion.Uniform;

    public float InitialBodyRadius { get; init; } = CreatureGenome.Baseline.BodyRadius;

    public float InitialMaxSpeed { get; init; } = CreatureGenome.Baseline.MaxSpeed;

    public float InitialMaxTurnRadiansPerSecond { get; init; } = CreatureGenome.Baseline.MaxTurnRadiansPerSecond;

    public float InitialSenseRadius { get; init; } = CreatureGenome.Baseline.SenseRadius;

    public SpeciesScenarioSeed[] SpeciesSeeds { get; init; } = [];

    public float InitialResourcesPerMillionArea { get; init; } = 28.75f;

    public float InitialCreatureEnergyMin { get; init; } = 45f;

    public float InitialCreatureEnergyMax { get; init; } = 85f;

    public float ResourceRadiusMin { get; init; } = 3f;

    public float ResourceRadiusMax { get; init; } = 8f;

    public float ResourceCaloriesMin { get; init; } = 55f;

    public float ResourceCaloriesMax { get; init; } = 120f;

    public float ResourceMaxCalories { get; init; } = 120f;

    public float ResourceRegrowthMin { get; init; } = 0.35f;

    public float ResourceRegrowthMax { get; init; } = 1.8f;

    public float GenericPlantWeight { get; init; } = 1f;

    public float TenderPlantWeight { get; init; }

    public float RichPlantWeight { get; init; }

    public float ToughPlantWeight { get; init; }

    public bool EnablePlantTypeHabitatAffinity { get; init; }

    public bool RelocateDepletedResources { get; init; } = true;

    public float PlantRespawnDelaySecondsMin { get; init; } = 0f;

    public float PlantRespawnDelaySecondsMax { get; init; } = 0f;

    public float ResourceClusterStrength { get; init; } = 0.2f;

    public float ResourceClusterRadius { get; init; } = 360f;

    public float PlantLocalDispersalChance { get; init; } = 0.35f;

    public float PlantLocalDispersalRadius { get; init; } = 440f;

    public bool EnableLocalFertility { get; init; } = true;

    public float LocalFertilityCellSize { get; init; } = 500f;

    public float LocalFertilityMinimumMultiplier { get; init; } = 0.35f;

    public float LocalFertilityRecoveryPerSecond { get; init; } = 0.0003f;

    public float LocalFertilityDepletionPerPlant { get; init; } = 0.12f;

    public float LocalFertilityNeighborDepletionShare { get; init; } = 0.55f;

    public bool EnableSeasons { get; init; }

    public float SeasonLengthSeconds { get; init; } = 900f;

    public float SeasonFertilityAmplitude { get; init; } = 0.3f;

    public float SeasonPhaseOffsetSeconds { get; init; } = 0f;

    public SeasonPhaseMode SeasonPhaseMode { get; init; } = SeasonPhaseMode.Global;

    public float BarrenBiomeSeasonalAmplitudeMultiplier { get; init; } = 0.5f;

    public float SparseBiomeSeasonalAmplitudeMultiplier { get; init; } = 0.85f;

    public float GrasslandBiomeSeasonalAmplitudeMultiplier { get; init; } = 1f;

    public float RichBiomeSeasonalAmplitudeMultiplier { get; init; } = 1.2f;

    public float ForestBiomeSeasonalAmplitudeMultiplier { get; init; } = 1.05f;

    public float WetlandBiomeSeasonalAmplitudeMultiplier { get; init; } = 1.15f;

    public float TundraBiomeSeasonalAmplitudeMultiplier { get; init; } = 0.65f;

    public float HighlandBiomeSeasonalAmplitudeMultiplier { get; init; } = 0.8f;

    public float BarrenBiomeMovementCostMultiplier { get; init; } = 1.4f;

    public float SparseBiomeMovementCostMultiplier { get; init; } = 1.12f;

    public float GrasslandBiomeMovementCostMultiplier { get; init; } = 1f;

    public float RichBiomeMovementCostMultiplier { get; init; } = 0.92f;

    public float ForestBiomeMovementCostMultiplier { get; init; } = 1.08f;

    public float WetlandBiomeMovementCostMultiplier { get; init; } = 1.35f;

    public float TundraBiomeMovementCostMultiplier { get; init; } = 1.15f;

    public float HighlandBiomeMovementCostMultiplier { get; init; } = 1.22f;

    public float BarrenBiomeSpeedMultiplier { get; init; } = 0.55f;

    public float SparseBiomeSpeedMultiplier { get; init; } = 0.8f;

    public float GrasslandBiomeSpeedMultiplier { get; init; } = 1f;

    public float RichBiomeSpeedMultiplier { get; init; } = 1.02f;

    public float ForestBiomeSpeedMultiplier { get; init; } = 0.82f;

    public float WetlandBiomeSpeedMultiplier { get; init; } = 0.68f;

    public float TundraBiomeSpeedMultiplier { get; init; } = 1f;

    public float HighlandBiomeSpeedMultiplier { get; init; } = 1f;

    public float BarrenBiomeVisionRangeMultiplier { get; init; } = 1.08f;

    public float SparseBiomeVisionRangeMultiplier { get; init; } = 0.95f;

    public float GrasslandBiomeVisionRangeMultiplier { get; init; } = 1f;

    public float RichBiomeVisionRangeMultiplier { get; init; } = 1.05f;

    public float ForestBiomeVisionRangeMultiplier { get; init; } = 0.6f;

    public float WetlandBiomeVisionRangeMultiplier { get; init; } = 0.82f;

    public float TundraBiomeVisionRangeMultiplier { get; init; } = 1f;

    public float HighlandBiomeVisionRangeMultiplier { get; init; } = 1f;

    public float BarrenBiomeBasalCostMultiplier { get; init; } = 1.2f;

    public float SparseBiomeBasalCostMultiplier { get; init; } = 1.06f;

    public float GrasslandBiomeBasalCostMultiplier { get; init; } = 1f;

    public float RichBiomeBasalCostMultiplier { get; init; } = 0.96f;

    public float ForestBiomeBasalCostMultiplier { get; init; } = 0.88f;

    public float WetlandBiomeBasalCostMultiplier { get; init; } = 1.08f;

    public float TundraBiomeBasalCostMultiplier { get; init; } = 1.14f;

    public float HighlandBiomeBasalCostMultiplier { get; init; } = 1.08f;

    public float BasalEnergyPerSecond { get; init; } = 0.18f;

    public float MetabolicPace { get; init; } = CreatureMetabolism.DefaultPace;

    public float ThermalMismatchBasalCostMultiplier { get; init; }

    public float BodyRadiusEnergyCostPerSecond { get; init; } = 0.04f;

    public float MaxSpeedEnergyCostPerSecond { get; init; } = 0.006f;

    public float TurnRateEnergyCostPerSecond { get; init; } = 0.03f;

    public float SenseRadiusEnergyCostPerSecond { get; init; } = 0.0008f;

    public float VisionAngleRadians { get; init; } = MathF.PI * 2f / 3f;

    public float VisionAngleEnergyCostPerSecond { get; init; } = 0.02f;

    public float EatRateEnergyCostPerSecond { get; init; } = 0.006f;

    public float GutCapacityEnergyCostPerSecond { get; init; } = 0.0008f;

    public float DigestionRateEnergyCostPerSecond { get; init; } = 0.014f;

    public float BiteStrengthEnergyCostPerSecond { get; init; } = 0.04f;

    public float DamageResistanceEnergyCostPerSecond { get; init; } = 0.03f;

    public float PlantSpecializationEnergyCostPerSecond { get; init; } = 0.025f;

    public float MemoryEnergyCostPerSecond { get; init; } = 0.01f;

    public float RtNeatHiddenNodeEnergyCostPerSecond { get; init; } =
        MetabolismSystem.DefaultRtNeatHiddenNodeEnergyCostPerSecond;

    public float RtNeatEnabledConnectionEnergyCostPerSecond { get; init; } =
        MetabolismSystem.DefaultRtNeatEnabledConnectionEnergyCostPerSecond;

    public float MemoryDecayPerSecond { get; init; } = NeuralControllerSystem.DefaultMemoryDecayPerSecond;

    public float MemoryWriteRatePerSecond { get; init; } = NeuralControllerSystem.DefaultMemoryWriteRatePerSecond;

    public float EggEnergyCostPerSecond { get; init; } = 0f;

    public float EggEnvironmentalDamagePerSecond { get; init; } = 0.08f;

    public float MovementEnergyPerSecond { get; init; } = 0.28f;

    public float MovementSpeedCostExponent { get; init; } = 1.6f;

    public float EatCaloriesPerSecond { get; init; } = 18f;

    public float GutCapacityCalories { get; init; } = 55f;

    public float DigestionCaloriesPerSecond { get; init; } = 5f;

    public float FatStorageCapacityCalories { get; init; } = CreatureGenome.Baseline.FatStorageCapacityCalories;

    public float FatStorageEfficiency { get; init; } = CreatureGenome.Baseline.FatStorageEfficiency;

    public float FatDepositEnergyRatio { get; init; } = FatStorageSystem.DefaultDepositEnergyRatio;

    public float FatWithdrawEnergyRatio { get; init; } = FatStorageSystem.DefaultWithdrawEnergyRatio;

    public float FatTransferCapacitySharePerSecond { get; init; } = FatStorageSystem.DefaultTransferCapacitySharePerSecond;

    public float ReproductionEnergyThreshold { get; init; } = 84f;

    public float OffspringEnergyInvestment { get; init; } = 28f;

    public float EggProductionEnergyPerSecond { get; init; } = 3f;

    public float EggIncubationSeconds { get; init; } = 18f;

    public float MaturityAgeSeconds { get; init; } = 60f;

    public float ReproductionCooldownSeconds { get; init; } = 7f;

    public float MaxLifeExpectancySeconds { get; init; } = CreatureGenome.Baseline.MaxLifeExpectancySeconds;

    public bool RequireReproductionIntent { get; init; } = true;

    public float ReproductivePrimeAgeSeconds { get; init; } = 240f;

    public float ReproductiveSenescenceAgeSeconds { get; init; } = 900f;

    public float SenescentFertilityMultiplier { get; init; } = 0.18f;

    public float CrowdingFertilityPenalty { get; init; } = 0.65f;

    public float DietaryAdaptation { get; init; } = 0.1f;

    public float CarrionAdaptation { get; init; } = 0f;

    public float TenderPlantAdaptation { get; init; } = 0f;

    public float RichPlantAdaptation { get; init; } = 0f;

    public float ToughPlantAdaptation { get; init; } = 0f;

    public float ThermalOptimum { get; init; } = CreatureGenome.Baseline.ThermalOptimum;

    public float ThermalTolerance { get; init; } = CreatureGenome.Baseline.ThermalTolerance;

    public float BiteStrength { get; init; } = 0.55f;

    public float DamageResistance { get; init; } = 1f;

    public float DeathMeatCaloriesPerBodyRadius { get; init; } = 4f;

    public float DeathMeatEnergyFraction { get; init; } = 0.35f;

    public float MeatDecayCaloriesPerSecond { get; init; } = 0.03f;

    public float RottenMeatDamagePerRawKcal { get; init; } = 0.004f;

    public float MeatScentRangeMultiplier { get; init; } = 2f;

    public float MeatScentCaloriesForFullStrength { get; init; } = 60f;

    public float MeatScentDensitySaturation { get; init; } = 1f;

    public bool EnableSmallPrey { get; init; }

    public float SmallPreyPerMillionArea { get; init; }

    public float SmallPreyMaxSpawnsPerSecond { get; init; } = 1f;

    public float SmallPreyRadius { get; init; } = 2f;

    public float SmallPreyCalories { get; init; } = 16f;

    public float SmallPreyHealth { get; init; } = 0.18f;

    public float SmallPreyMaxSpeed { get; init; } = 7f;

    public float SmallPreyWanderIntervalSecondsMin { get; init; } = 0.6f;

    public float SmallPreyWanderIntervalSecondsMax { get; init; } = 2.2f;

    public float BarrenBiomeSmallPreySpawnWeight { get; init; } = 0f;

    public float SparseBiomeSmallPreySpawnWeight { get; init; } = 0.25f;

    public float GrasslandBiomeSmallPreySpawnWeight { get; init; } = 0.35f;

    public float RichBiomeSmallPreySpawnWeight { get; init; } = 0.9f;

    public float ForestBiomeSmallPreySpawnWeight { get; init; } = 1.4f;

    public float WetlandBiomeSmallPreySpawnWeight { get; init; } = 1.6f;

    public float TundraBiomeSmallPreySpawnWeight { get; init; } = 0f;

    public float HighlandBiomeSmallPreySpawnWeight { get; init; } = 0.2f;

    public float SoundRangeMultiplier { get; init; } = CreatureSensingSystem.DefaultSoundRangeMultiplier;

    public float SoundDensitySaturation { get; init; } = CreatureSensingSystem.DefaultSoundDensitySaturation;

    public float BiteDamagePerSecond { get; init; } = 0.18f;

    public float BiteEnergyCostPerSecond { get; init; } = 0.15f;

    public float BiteRangePadding { get; init; } = 1f;

    public bool EnableCreatureCollision { get; init; } = true;

    public float CreatureCollisionSafeImpactSpeed { get; init; } = CreatureCollisionSystem.DefaultSafeImpactSpeed;

    public float CreatureCollisionDamageScale { get; init; } = CreatureCollisionSystem.DefaultDamageScale;

    public int CreatureCollisionSeparationIterations { get; init; } =
        CreatureCollisionSystem.DefaultSeparationIterations;

    public bool EnableInjuryMemory { get; init; } = true;

    public float InjuryMemoryHalfLifeSeconds { get; init; } = InjuryMemorySystem.DefaultHalfLifeSeconds;

    public float InjuryMemoryDamageSignalScale { get; init; } = InjuryMemorySystem.DefaultDamageSignalScale;

    public float HealingDelaySeconds { get; init; } = CreatureHealingSystem.DefaultHealingDelaySeconds;

    public float HealingHealthFractionPerSecond { get; init; } =
        CreatureHealingSystem.DefaultHealingHealthFractionPerSecond;

    public float HealingEnergyCostPerHealth { get; init; } = CreatureHealingSystem.DefaultHealingEnergyCostPerHealth;

    public float HealingMinimumEnergy { get; init; } = CreatureHealingSystem.DefaultHealingMinimumEnergy;

    public float MutationStrength { get; init; } = 0.06f;

    public float TraitMutationRate { get; init; } = 0.2f;

    public float BrainMutationRate { get; init; } = 0.08f;

    public SimulationScenario Validated()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Scenario name cannot be empty.");
        }

        EnsurePositive(WorldWidth, nameof(WorldWidth));
        EnsurePositive(WorldHeight, nameof(WorldHeight));
        EnsureEnumDefined(BrainArchitectureKind, nameof(BrainArchitectureKind));
        EnsureEnumDefined(InitialBrainKind, nameof(InitialBrainKind));
        EnsureEnumDefined(BiomeMapKind, nameof(BiomeMapKind));
        if (EnableBiomes
            && BiomeMapKind == BiomeMapKind.Manual
            && string.IsNullOrWhiteSpace(ManualBiomeMapPath)
            && string.IsNullOrWhiteSpace(WorldMapPath))
        {
            throw new InvalidOperationException("Manual biome maps require manualBiomeMapPath or worldMapPath.");
        }

        EnsureEnumDefined(ObstacleMapKind, nameof(ObstacleMapKind));
        if (EnableObstacles
            && ObstacleMapKind == ObstacleMapKind.Manual
            && string.IsNullOrWhiteSpace(ManualObstacleMapPath)
            && string.IsNullOrWhiteSpace(WorldMapPath))
        {
            throw new InvalidOperationException("Manual obstacle maps require manualObstacleMapPath or worldMapPath.");
        }

        EnsureHiddenNodeCount(BrainHiddenNodeCount, nameof(BrainHiddenNodeCount));
        EnsurePositive(BiomeCellSize, nameof(BiomeCellSize));
        EnsurePositive(ObstacleCellSize, nameof(ObstacleCellSize));
        EnsureNonNegative(ResourceVoidBorderWidth, nameof(ResourceVoidBorderWidth));
        EnsurePositive(FixedDeltaSeconds, nameof(FixedDeltaSeconds));
        EnsurePositive(SpatialCellSize, nameof(SpatialCellSize));
        EnsurePositive(WorldSenseIntervalTicks, nameof(WorldSenseIntervalTicks));
        EnsureRange(CloseSenseRefreshProximity, 0f, 1f, nameof(CloseSenseRefreshProximity));
        EnsurePositive(CloseSenseRefreshMinimumTicks, nameof(CloseSenseRefreshMinimumTicks));
        EnsurePositive(PlantPayoffTraceHalfLifeSeconds, nameof(PlantPayoffTraceHalfLifeSeconds));
        EnsurePositive(SensingThreadCount, nameof(SensingThreadCount));
        EnsurePositive(NeuralControllerThreadCount, nameof(NeuralControllerThreadCount));
        EnsurePositive(StatsSnapshotIntervalTicks, nameof(StatsSnapshotIntervalTicks));
        EnsurePositive(ExtinctPayloadPruneIntervalTicks, nameof(ExtinctPayloadPruneIntervalTicks));
        EnsureNonNegative(InitialCreatureCount, nameof(InitialCreatureCount));
        EnsureEnumDefined(InitialCreatureSpawnRegion, nameof(InitialCreatureSpawnRegion));
        EnsurePositive(InitialBodyRadius, nameof(InitialBodyRadius));
        EnsurePositive(InitialMaxSpeed, nameof(InitialMaxSpeed));
        EnsurePositive(InitialMaxTurnRadiansPerSecond, nameof(InitialMaxTurnRadiansPerSecond));
        EnsurePositive(InitialSenseRadius, nameof(InitialSenseRadius));
        var speciesSeeds = (SpeciesSeeds ?? []).Select(seed =>
        {
            if (seed is null)
            {
                throw new InvalidOperationException("Species seed entries cannot be null.");
            }

            return seed.Validated();
        }).ToArray();

        EnsureNonNegative(InitialResourcesPerMillionArea, nameof(InitialResourcesPerMillionArea));
        EnsureRange(InitialCreatureEnergyMin, InitialCreatureEnergyMax, nameof(InitialCreatureEnergyMin), nameof(InitialCreatureEnergyMax));
        EnsureRange(ResourceRadiusMin, ResourceRadiusMax, nameof(ResourceRadiusMin), nameof(ResourceRadiusMax));
        EnsureRange(ResourceCaloriesMin, ResourceCaloriesMax, nameof(ResourceCaloriesMin), nameof(ResourceCaloriesMax));
        EnsurePositive(ResourceMaxCalories, nameof(ResourceMaxCalories));
        EnsureRange(ResourceRegrowthMin, ResourceRegrowthMax, nameof(ResourceRegrowthMin), nameof(ResourceRegrowthMax));
        EnsureNonNegative(GenericPlantWeight, nameof(GenericPlantWeight));
        EnsureNonNegative(TenderPlantWeight, nameof(TenderPlantWeight));
        EnsureNonNegative(RichPlantWeight, nameof(RichPlantWeight));
        EnsureNonNegative(ToughPlantWeight, nameof(ToughPlantWeight));
        if (GenericPlantWeight + TenderPlantWeight + RichPlantWeight + ToughPlantWeight <= 0f)
        {
            throw new InvalidOperationException("At least one plant type weight must be positive.");
        }

        EnsureNonNegativeRange(PlantRespawnDelaySecondsMin, PlantRespawnDelaySecondsMax, nameof(PlantRespawnDelaySecondsMin), nameof(PlantRespawnDelaySecondsMax));
        EnsureProbability(ResourceClusterStrength, nameof(ResourceClusterStrength));
        EnsurePositive(ResourceClusterRadius, nameof(ResourceClusterRadius));
        EnsureProbability(PlantLocalDispersalChance, nameof(PlantLocalDispersalChance));
        EnsureNonNegative(PlantLocalDispersalRadius, nameof(PlantLocalDispersalRadius));
        if (PlantLocalDispersalChance > 0f && PlantLocalDispersalRadius <= 0f)
        {
            throw new InvalidOperationException("Plant local dispersal radius must be positive when local dispersal is enabled.");
        }

        EnsurePositive(LocalFertilityCellSize, nameof(LocalFertilityCellSize));
        EnsureRange(LocalFertilityMinimumMultiplier, 0.05f, 1f, nameof(LocalFertilityMinimumMultiplier));
        EnsureNonNegative(LocalFertilityRecoveryPerSecond, nameof(LocalFertilityRecoveryPerSecond));
        EnsureRange(LocalFertilityDepletionPerPlant, 0f, 1f, nameof(LocalFertilityDepletionPerPlant));
        EnsureProbability(LocalFertilityNeighborDepletionShare, nameof(LocalFertilityNeighborDepletionShare));

        EnsurePositive(SeasonLengthSeconds, nameof(SeasonLengthSeconds));
        EnsureRange(SeasonFertilityAmplitude, 0f, 0.95f, nameof(SeasonFertilityAmplitude));
        EnsureFinite(SeasonPhaseOffsetSeconds, nameof(SeasonPhaseOffsetSeconds));
        EnsureEnumDefined(SeasonPhaseMode, nameof(SeasonPhaseMode));
        EnsureRange(BarrenBiomeSeasonalAmplitudeMultiplier, 0f, 2f, nameof(BarrenBiomeSeasonalAmplitudeMultiplier));
        EnsureRange(SparseBiomeSeasonalAmplitudeMultiplier, 0f, 2f, nameof(SparseBiomeSeasonalAmplitudeMultiplier));
        EnsureRange(GrasslandBiomeSeasonalAmplitudeMultiplier, 0f, 2f, nameof(GrasslandBiomeSeasonalAmplitudeMultiplier));
        EnsureRange(RichBiomeSeasonalAmplitudeMultiplier, 0f, 2f, nameof(RichBiomeSeasonalAmplitudeMultiplier));
        EnsureRange(ForestBiomeSeasonalAmplitudeMultiplier, 0f, 2f, nameof(ForestBiomeSeasonalAmplitudeMultiplier));
        EnsureRange(WetlandBiomeSeasonalAmplitudeMultiplier, 0f, 2f, nameof(WetlandBiomeSeasonalAmplitudeMultiplier));
        EnsureRange(TundraBiomeSeasonalAmplitudeMultiplier, 0f, 2f, nameof(TundraBiomeSeasonalAmplitudeMultiplier));
        EnsureRange(HighlandBiomeSeasonalAmplitudeMultiplier, 0f, 2f, nameof(HighlandBiomeSeasonalAmplitudeMultiplier));
        _ = BiomePressureProfile.Validate(CreateBiomeMovementCostProfile(), "BiomeMovementCostProfile");
        _ = BiomePressureProfile.Validate(CreateBiomeSpeedProfile(), "BiomeSpeedProfile");
        _ = BiomePressureProfile.Validate(CreateBiomeVisionRangeProfile(), "BiomeVisionRangeProfile");
        _ = BiomePressureProfile.Validate(CreateBiomeBasalCostProfile(), "BiomeBasalCostProfile");
        _ = BiomePressureProfile.Validate(CreateBiomeSeasonalAmplitudeProfile(), "BiomeSeasonalAmplitudeProfile");
        EnsureNonNegative(BasalEnergyPerSecond, nameof(BasalEnergyPerSecond));
        EnsureRange(MetabolicPace, CreatureMetabolism.MinimumPace, CreatureMetabolism.MaximumPace, nameof(MetabolicPace));
        EnsureNonNegative(ThermalMismatchBasalCostMultiplier, nameof(ThermalMismatchBasalCostMultiplier));
        EnsureNonNegative(BodyRadiusEnergyCostPerSecond, nameof(BodyRadiusEnergyCostPerSecond));
        EnsureNonNegative(MaxSpeedEnergyCostPerSecond, nameof(MaxSpeedEnergyCostPerSecond));
        EnsureNonNegative(TurnRateEnergyCostPerSecond, nameof(TurnRateEnergyCostPerSecond));
        EnsureNonNegative(SenseRadiusEnergyCostPerSecond, nameof(SenseRadiusEnergyCostPerSecond));
        EnsureRange(VisionAngleRadians, MathF.PI / 12f, MathF.Tau, nameof(VisionAngleRadians));
        EnsureNonNegative(VisionAngleEnergyCostPerSecond, nameof(VisionAngleEnergyCostPerSecond));
        EnsureNonNegative(EatRateEnergyCostPerSecond, nameof(EatRateEnergyCostPerSecond));
        EnsureNonNegative(GutCapacityEnergyCostPerSecond, nameof(GutCapacityEnergyCostPerSecond));
        EnsureNonNegative(DigestionRateEnergyCostPerSecond, nameof(DigestionRateEnergyCostPerSecond));
        EnsureNonNegative(BiteStrengthEnergyCostPerSecond, nameof(BiteStrengthEnergyCostPerSecond));
        EnsureNonNegative(DamageResistanceEnergyCostPerSecond, nameof(DamageResistanceEnergyCostPerSecond));
        EnsureNonNegative(PlantSpecializationEnergyCostPerSecond, nameof(PlantSpecializationEnergyCostPerSecond));
        EnsureNonNegative(MemoryEnergyCostPerSecond, nameof(MemoryEnergyCostPerSecond));
        EnsureNonNegative(RtNeatHiddenNodeEnergyCostPerSecond, nameof(RtNeatHiddenNodeEnergyCostPerSecond));
        EnsureNonNegative(RtNeatEnabledConnectionEnergyCostPerSecond, nameof(RtNeatEnabledConnectionEnergyCostPerSecond));
        EnsureNonNegative(MemoryDecayPerSecond, nameof(MemoryDecayPerSecond));
        EnsureNonNegative(MemoryWriteRatePerSecond, nameof(MemoryWriteRatePerSecond));
        EnsureNonNegative(EggEnergyCostPerSecond, nameof(EggEnergyCostPerSecond));
        EnsureNonNegative(EggEnvironmentalDamagePerSecond, nameof(EggEnvironmentalDamagePerSecond));
        EnsureNonNegative(MovementEnergyPerSecond, nameof(MovementEnergyPerSecond));
        EnsurePositive(MovementSpeedCostExponent, nameof(MovementSpeedCostExponent));
        EnsurePositive(EatCaloriesPerSecond, nameof(EatCaloriesPerSecond));
        EnsurePositive(GutCapacityCalories, nameof(GutCapacityCalories));
        EnsurePositive(DigestionCaloriesPerSecond, nameof(DigestionCaloriesPerSecond));
        EnsureNonNegative(FatStorageCapacityCalories, nameof(FatStorageCapacityCalories));
        EnsureRange(FatStorageEfficiency, 0.05f, 1f, nameof(FatStorageEfficiency));
        EnsureNonNegative(FatDepositEnergyRatio, nameof(FatDepositEnergyRatio));
        EnsureNonNegative(FatWithdrawEnergyRatio, nameof(FatWithdrawEnergyRatio));
        EnsureNonNegative(FatTransferCapacitySharePerSecond, nameof(FatTransferCapacitySharePerSecond));
        EnsurePositive(ReproductionEnergyThreshold, nameof(ReproductionEnergyThreshold));
        EnsurePositive(OffspringEnergyInvestment, nameof(OffspringEnergyInvestment));
        EnsurePositive(EggProductionEnergyPerSecond, nameof(EggProductionEnergyPerSecond));
        EnsureNonNegative(EggIncubationSeconds, nameof(EggIncubationSeconds));
        EnsureNonNegative(MaturityAgeSeconds, nameof(MaturityAgeSeconds));
        EnsureNonNegative(ReproductionCooldownSeconds, nameof(ReproductionCooldownSeconds));
        EnsurePositive(MaxLifeExpectancySeconds, nameof(MaxLifeExpectancySeconds));
        EnsureNonNegative(ReproductivePrimeAgeSeconds, nameof(ReproductivePrimeAgeSeconds));
        EnsureNonNegative(ReproductiveSenescenceAgeSeconds, nameof(ReproductiveSenescenceAgeSeconds));
        EnsureRange(SenescentFertilityMultiplier, 0f, 1f, nameof(SenescentFertilityMultiplier));
        EnsureRange(CrowdingFertilityPenalty, 0f, 1f, nameof(CrowdingFertilityPenalty));
        EnsureProbability(DietaryAdaptation, nameof(DietaryAdaptation));
        EnsureProbability(CarrionAdaptation, nameof(CarrionAdaptation));
        EnsureProbability(TenderPlantAdaptation, nameof(TenderPlantAdaptation));
        EnsureProbability(RichPlantAdaptation, nameof(RichPlantAdaptation));
        EnsureProbability(ToughPlantAdaptation, nameof(ToughPlantAdaptation));
        EnsureRange(ThermalOptimum, CreatureThermal.MinimumOptimum, CreatureThermal.MaximumOptimum, nameof(ThermalOptimum));
        EnsureRange(ThermalTolerance, CreatureThermal.MinimumTolerance, CreatureThermal.MaximumTolerance, nameof(ThermalTolerance));
        EnsurePositive(BiteStrength, nameof(BiteStrength));
        EnsurePositive(DamageResistance, nameof(DamageResistance));
        EnsureNonNegative(DeathMeatCaloriesPerBodyRadius, nameof(DeathMeatCaloriesPerBodyRadius));
        EnsureProbability(DeathMeatEnergyFraction, nameof(DeathMeatEnergyFraction));
        EnsureNonNegative(MeatDecayCaloriesPerSecond, nameof(MeatDecayCaloriesPerSecond));
        EnsureNonNegative(RottenMeatDamagePerRawKcal, nameof(RottenMeatDamagePerRawKcal));
        EnsureRange(MeatScentRangeMultiplier, 1f, 10f, nameof(MeatScentRangeMultiplier));
        EnsurePositive(MeatScentCaloriesForFullStrength, nameof(MeatScentCaloriesForFullStrength));
        EnsurePositive(MeatScentDensitySaturation, nameof(MeatScentDensitySaturation));
        EnsureNonNegative(SmallPreyPerMillionArea, nameof(SmallPreyPerMillionArea));
        EnsureNonNegative(SmallPreyMaxSpawnsPerSecond, nameof(SmallPreyMaxSpawnsPerSecond));
        EnsurePositive(SmallPreyRadius, nameof(SmallPreyRadius));
        EnsurePositive(SmallPreyCalories, nameof(SmallPreyCalories));
        EnsurePositive(SmallPreyHealth, nameof(SmallPreyHealth));
        EnsureNonNegative(SmallPreyMaxSpeed, nameof(SmallPreyMaxSpeed));
        EnsurePositive(SmallPreyWanderIntervalSecondsMin, nameof(SmallPreyWanderIntervalSecondsMin));
        EnsurePositive(SmallPreyWanderIntervalSecondsMax, nameof(SmallPreyWanderIntervalSecondsMax));
        if (SmallPreyWanderIntervalSecondsMax < SmallPreyWanderIntervalSecondsMin)
        {
            throw new InvalidOperationException("Small prey max wander interval must be at least the min interval.");
        }
        EnsureNonNegative(BarrenBiomeSmallPreySpawnWeight, nameof(BarrenBiomeSmallPreySpawnWeight));
        EnsureNonNegative(SparseBiomeSmallPreySpawnWeight, nameof(SparseBiomeSmallPreySpawnWeight));
        EnsureNonNegative(GrasslandBiomeSmallPreySpawnWeight, nameof(GrasslandBiomeSmallPreySpawnWeight));
        EnsureNonNegative(RichBiomeSmallPreySpawnWeight, nameof(RichBiomeSmallPreySpawnWeight));
        EnsureNonNegative(ForestBiomeSmallPreySpawnWeight, nameof(ForestBiomeSmallPreySpawnWeight));
        EnsureNonNegative(WetlandBiomeSmallPreySpawnWeight, nameof(WetlandBiomeSmallPreySpawnWeight));
        EnsureNonNegative(TundraBiomeSmallPreySpawnWeight, nameof(TundraBiomeSmallPreySpawnWeight));
        EnsureNonNegative(HighlandBiomeSmallPreySpawnWeight, nameof(HighlandBiomeSmallPreySpawnWeight));
        _ = BiomePressureProfile.Validate(CreateSmallPreySpawnWeightProfile(), "SmallPreySpawnWeightProfile");
        EnsureRange(SoundRangeMultiplier, 1f, 10f, nameof(SoundRangeMultiplier));
        EnsurePositive(SoundDensitySaturation, nameof(SoundDensitySaturation));
        EnsureNonNegative(BiteDamagePerSecond, nameof(BiteDamagePerSecond));
        EnsureNonNegative(BiteEnergyCostPerSecond, nameof(BiteEnergyCostPerSecond));
        EnsureNonNegative(BiteRangePadding, nameof(BiteRangePadding));
        EnsureNonNegative(CreatureCollisionSafeImpactSpeed, nameof(CreatureCollisionSafeImpactSpeed));
        EnsureNonNegative(CreatureCollisionDamageScale, nameof(CreatureCollisionDamageScale));
        EnsurePositive(CreatureCollisionSeparationIterations, nameof(CreatureCollisionSeparationIterations));
        EnsurePositive(InjuryMemoryHalfLifeSeconds, nameof(InjuryMemoryHalfLifeSeconds));
        EnsureNonNegative(InjuryMemoryDamageSignalScale, nameof(InjuryMemoryDamageSignalScale));
        EnsureNonNegative(HealingDelaySeconds, nameof(HealingDelaySeconds));
        EnsureNonNegative(HealingHealthFractionPerSecond, nameof(HealingHealthFractionPerSecond));
        EnsureNonNegative(HealingEnergyCostPerHealth, nameof(HealingEnergyCostPerHealth));
        EnsureNonNegative(HealingMinimumEnergy, nameof(HealingMinimumEnergy));
        EnsureNonNegative(MutationStrength, nameof(MutationStrength));
        EnsureProbability(TraitMutationRate, nameof(TraitMutationRate));
        EnsureProbability(BrainMutationRate, nameof(BrainMutationRate));

        if (ResourceCaloriesMax > ResourceMaxCalories)
        {
            throw new InvalidOperationException("Resource calories max cannot exceed resource max calories.");
        }

        if (ResourceVoidBorderWidth * 2f >= Math.Min(WorldWidth, WorldHeight))
        {
            throw new InvalidOperationException("Resource void border width must leave a positive resource-spawn area.");
        }

        if (ReproductionEnergyThreshold < OffspringEnergyInvestment)
        {
            throw new InvalidOperationException("Reproduction threshold must be at least the offspring investment.");
        }

        if (ReproductiveSenescenceAgeSeconds < ReproductivePrimeAgeSeconds)
        {
            throw new InvalidOperationException("Reproductive senescence age must be greater than or equal to prime age.");
        }

        var normalizedWorldMapPath = string.IsNullOrWhiteSpace(WorldMapPath)
            ? null
            : WorldMapPath.Trim();

        return this with
        {
            BrainHiddenNodeCount = BrainFactory.ResolveHiddenNodeCount(BrainArchitectureKind, BrainHiddenNodeCount),
            WorldMapPath = normalizedWorldMapPath,
            ManualBiomeMapPath = normalizedWorldMapPath is not null || string.IsNullOrWhiteSpace(ManualBiomeMapPath)
                ? null
                : ManualBiomeMapPath.Trim(),
            ManualObstacleMapPath = normalizedWorldMapPath is not null || string.IsNullOrWhiteSpace(ManualObstacleMapPath)
                ? null
                : ManualObstacleMapPath.Trim(),
            SpeciesSeeds = speciesSeeds
        };
    }

    public BiomePressureProfile CreateBiomeMovementCostProfile()
    {
        return new BiomePressureProfile(
            BarrenBiomeMovementCostMultiplier,
            SparseBiomeMovementCostMultiplier,
            GrasslandBiomeMovementCostMultiplier,
            RichBiomeMovementCostMultiplier,
            ForestBiomeMovementCostMultiplier,
            WetlandBiomeMovementCostMultiplier,
            TundraBiomeMovementCostMultiplier,
            HighlandBiomeMovementCostMultiplier);
    }

    public BiomePressureProfile CreateBiomeBasalCostProfile()
    {
        return new BiomePressureProfile(
            BarrenBiomeBasalCostMultiplier,
            SparseBiomeBasalCostMultiplier,
            GrasslandBiomeBasalCostMultiplier,
            RichBiomeBasalCostMultiplier,
            ForestBiomeBasalCostMultiplier,
            WetlandBiomeBasalCostMultiplier,
            TundraBiomeBasalCostMultiplier,
            HighlandBiomeBasalCostMultiplier);
    }

    public BiomePressureProfile CreateBiomeSpeedProfile()
    {
        return new BiomePressureProfile(
            BarrenBiomeSpeedMultiplier,
            SparseBiomeSpeedMultiplier,
            GrasslandBiomeSpeedMultiplier,
            RichBiomeSpeedMultiplier,
            ForestBiomeSpeedMultiplier,
            WetlandBiomeSpeedMultiplier,
            TundraBiomeSpeedMultiplier,
            HighlandBiomeSpeedMultiplier);
    }

    public BiomePressureProfile CreateBiomeVisionRangeProfile()
    {
        return new BiomePressureProfile(
            BarrenBiomeVisionRangeMultiplier,
            SparseBiomeVisionRangeMultiplier,
            GrasslandBiomeVisionRangeMultiplier,
            RichBiomeVisionRangeMultiplier,
            ForestBiomeVisionRangeMultiplier,
            WetlandBiomeVisionRangeMultiplier,
            TundraBiomeVisionRangeMultiplier,
            HighlandBiomeVisionRangeMultiplier);
    }

    public BiomePressureProfile CreateBiomeSeasonalAmplitudeProfile()
    {
        return new BiomePressureProfile(
            BarrenBiomeSeasonalAmplitudeMultiplier,
            SparseBiomeSeasonalAmplitudeMultiplier,
            GrasslandBiomeSeasonalAmplitudeMultiplier,
            RichBiomeSeasonalAmplitudeMultiplier,
            ForestBiomeSeasonalAmplitudeMultiplier,
            WetlandBiomeSeasonalAmplitudeMultiplier,
            TundraBiomeSeasonalAmplitudeMultiplier,
            HighlandBiomeSeasonalAmplitudeMultiplier);
    }

    public BiomePressureProfile CreateSmallPreySpawnWeightProfile()
    {
        return new BiomePressureProfile(
            BarrenBiomeSmallPreySpawnWeight,
            SparseBiomeSmallPreySpawnWeight,
            GrasslandBiomeSmallPreySpawnWeight,
            RichBiomeSmallPreySpawnWeight,
            ForestBiomeSmallPreySpawnWeight,
            WetlandBiomeSmallPreySpawnWeight,
            TundraBiomeSmallPreySpawnWeight,
            HighlandBiomeSmallPreySpawnWeight);
    }

    public int CalculateInitialResourceCount()
    {
        return CalculateResourceCount(WorldWidth, WorldHeight, InitialResourcesPerMillionArea);
    }

    public PlantResourceKind SamplePlantResourceKind(DeterministicRandom random, BiomeKind? biomeKind = null)
    {
        var genericWeight = GenericPlantWeight;
        var tenderWeight = TenderPlantWeight;
        var richWeight = RichPlantWeight;
        var toughWeight = ToughPlantWeight;
        if (EnablePlantTypeHabitatAffinity && biomeKind is { } biome)
        {
            genericWeight *= PlantResourceTraits.HabitatAffinityMultiplier(PlantResourceKind.Generic, biome);
            tenderWeight *= PlantResourceTraits.HabitatAffinityMultiplier(PlantResourceKind.Tender, biome);
            richWeight *= PlantResourceTraits.HabitatAffinityMultiplier(PlantResourceKind.Rich, biome);
            toughWeight *= PlantResourceTraits.HabitatAffinityMultiplier(PlantResourceKind.Tough, biome);
        }

        var totalWeight = genericWeight + tenderWeight + richWeight + toughWeight;
        if (totalWeight <= 0f)
        {
            return PlantResourceKind.Generic;
        }

        var roll = random.NextSingle(0f, totalWeight);
        if (roll < genericWeight)
        {
            return PlantResourceKind.Generic;
        }

        roll -= genericWeight;
        if (roll < tenderWeight)
        {
            return PlantResourceKind.Tender;
        }

        roll -= tenderWeight;
        if (roll < richWeight)
        {
            return PlantResourceKind.Rich;
        }

        return PlantResourceKind.Tough;
    }

    public bool HasEnabledSpeciesSeeds()
    {
        return EnabledSpeciesSeeds().Any();
    }

    public IEnumerable<SpeciesScenarioSeed> EnabledSpeciesSeeds()
    {
        return (SpeciesSeeds ?? [])
            .Where(seed => seed is not null)
            .Select(seed => seed.Validated())
            .Where(seed => seed.Enabled);
    }

    public static int CalculateResourceCount(float worldWidth, float worldHeight, float resourcesPerMillionArea)
    {
        EnsurePositive(worldWidth, nameof(worldWidth));
        EnsurePositive(worldHeight, nameof(worldHeight));
        EnsureNonNegative(resourcesPerMillionArea, nameof(resourcesPerMillionArea));

        var worldArea = worldWidth * worldHeight;
        return Math.Max(0, (int)MathF.Round(resourcesPerMillionArea * worldArea / ResourceDensityAreaUnits));
    }

    public static float CalculateResourcesPerMillionArea(int resourceCount, float worldWidth, float worldHeight)
    {
        EnsureNonNegative(resourceCount, nameof(resourceCount));
        EnsurePositive(worldWidth, nameof(worldWidth));
        EnsurePositive(worldHeight, nameof(worldHeight));

        return resourceCount / (worldWidth * worldHeight) * ResourceDensityAreaUnits;
    }

    private static void EnsurePositive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0f)
        {
            throw new InvalidOperationException($"{name} must be finite and positive.");
        }
    }

    private static void EnsurePositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException($"{name} must be positive.");
        }
    }

    private static void EnsureNonNegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f)
        {
            throw new InvalidOperationException($"{name} must be finite and non-negative.");
        }
    }

    private static void EnsureNonNegative(int value, string name)
    {
        if (value < 0)
        {
            throw new InvalidOperationException($"{name} must be non-negative.");
        }
    }

    private static void EnsureHiddenNodeCount(int value, string name)
    {
        if (value < 0 || value > NeuralBrainSchema.MaxHiddenNodeCount)
        {
            throw new InvalidOperationException(
                $"{name} must be between 0 and {NeuralBrainSchema.MaxHiddenNodeCount}.");
        }
    }

    private static void EnsureRange(float min, float max, string minName, string maxName)
    {
        EnsureNonNegative(min, minName);
        EnsurePositive(max, maxName);

        if (max < min)
        {
            throw new InvalidOperationException($"{maxName} must be greater than or equal to {minName}.");
        }
    }

    private static void EnsureNonNegativeRange(float min, float max, string minName, string maxName)
    {
        EnsureNonNegative(min, minName);
        EnsureNonNegative(max, maxName);

        if (max < min)
        {
            throw new InvalidOperationException($"{maxName} must be greater than or equal to {minName}.");
        }
    }

    private static void EnsureRange(float value, float inclusiveMin, float inclusiveMax, string name)
    {
        if (!float.IsFinite(value) || value < inclusiveMin || value > inclusiveMax)
        {
            throw new InvalidOperationException($"{name} must be finite and between {inclusiveMin} and {inclusiveMax}.");
        }
    }

    private static void EnsureFinite(float value, string name)
    {
        if (!float.IsFinite(value))
        {
            throw new InvalidOperationException($"{name} must be finite.");
        }
    }

    private static void EnsureProbability(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f || value > 1f)
        {
            throw new InvalidOperationException($"{name} must be finite and between 0 and 1.");
        }
    }

    private static void EnsureEnumDefined<TEnum>(TEnum value, string name)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new InvalidOperationException($"{name} must be a defined {typeof(TEnum).Name} value.");
        }
    }
}
