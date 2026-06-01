using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lineage.Core;

var tests = new (string Name, Action Body)[]
{
    ("Simulation clock advances by fixed steps", SimulationClockAdvancesByFixedSteps),
    ("Simulation profiler records system timings", SimulationProfilerRecordsSystemTimings),
    ("Simulation profiler can be paused", SimulationProfilerCanBePaused),
    ("Sensing profiler records candidate counts", SensingProfilerRecordsCandidateCounts),
    ("Creature sensing time slices world queries", CreatureSensingTimeSlicesWorldQueries),
    ("Creature sensing parallel path matches single-threaded path", CreatureSensingParallelPathMatchesSingleThreadedPath),
    ("Creature sensing throttles proximity close refreshes", CreatureSensingThrottlesProximityCloseRefreshes),
    ("Creature sensing keeps contact close refresh immediate", CreatureSensingKeepsContactCloseRefreshImmediate),
    ("DeterministicRandom repeats sequences from the same seed", RandomRepeatsFromSameSeed),
    ("System pipeline produces repeatable world changes", SystemPipelineIsRepeatable),
    ("Movement records search distance", MovementRecordsSearchDistance),
    ("Movement blocks obstacle collisions", MovementBlocksObstacleCollisions),
    ("Movement slides along obstacle edges", MovementSlidesAlongObstacleEdges),
    ("Movement cost follows biome multiplier", MovementCostFollowsBiomeMultiplier),
    ("Movement speed follows biome multiplier", MovementSpeedFollowsBiomeMultiplier),
    ("Movement speed cost is nonlinear", MovementSpeedCostIsNonlinear),
    ("Invalid configuration is rejected", InvalidConfigurationIsRejected),
    ("Resource regrowth is capped", ResourceRegrowthIsCapped),
    ("Seasonal fertility scales plant regrowth", SeasonalFertilityScalesPlantRegrowth),
    ("Seasonal fertility scales plant dormancy", SeasonalFertilityScalesPlantDormancy),
    ("Opposed seasonal fertility alternates world halves", OpposedSeasonalFertilityAlternatesWorldHalves),
    ("Biome seasonal response scales plant regrowth", BiomeSeasonalResponseScalesPlantRegrowth),
    ("Local fertility recovers after plant depletion", LocalFertilityRecoversAfterPlantDepletion),
    ("Local fertility slows plant dormancy", LocalFertilitySlowsPlantDormancy),
    ("Depleted resources can relocate before regrowing", DepletedResourcesCanRelocateBeforeRegrowing),
    ("Depleted plants can disperse locally", DepletedPlantsCanDisperseLocally),
    ("Depleted plants keep habitat biome when relocating", DepletedPlantsKeepHabitatBiomeWhenRelocating),
    ("Dormant plants hold when habitat placement unavailable", DormantPlantsHoldWhenHabitatPlacementUnavailable),
    ("Depleted plants enter dormancy before respawning", DepletedPlantsEnterDormancyBeforeRespawning),
    ("Dormant plants are absent from the spatial index", DormantPlantsAreAbsentFromSpatialIndex),
    ("Meat resources decay and disappear", MeatResourcesDecayAndDisappear),
    ("Fresh-kill resource credit expires", FreshKillResourceCreditExpires),
    ("Meat freshness reduces digested energy", MeatFreshnessReducesDigestedEnergy),
    ("Rotten meat damage scales with carrion adaptation", RottenMeatDamageScalesWithCarrionAdaptation),
    ("Rotten meat health deaths are counted", RottenMeatHealthDeathsAreCounted),
    ("Eating transfers resource calories into creature energy", EatingTransfersCalories),
    ("Eating fills gut before digestion", EatingFillsGutBeforeDigestion),
    ("Gut capacity limits additional eating", GutCapacityLimitsAdditionalEating),
    ("Plant type controls eating transfer rate", PlantTypeControlsEatingTransferRate),
    ("Plant type controls digestion payoff", PlantTypeControlsDigestionPayoff),
    ("Plant adaptation controls digestion payoff", PlantAdaptationControlsDigestionPayoff),
    ("Plant adaptation penalizes mismatched plant types", PlantAdaptationPenalizesMismatchedPlantTypes),
    ("Eating requires body contact with a resource", EatingRequiresBodyContact),
    ("Dietary adaptation controls digested calories", DietaryAdaptationControlsDigestedCalories),
    ("Carrion adaptation trades fresh and stale meat digestion", CarrionAdaptationTradesFreshAndStaleMeatDigestion),
    ("Eating transfers egg energy as meat nutrition", EatingTransfersEggEnergyAsMeatNutrition),
    ("Egg predation deaths are counted", EggPredationDeathsAreCounted),
    ("Starving creatures are removed", StarvingCreaturesAreRemoved),
    ("Dead creatures leave meat resources", DeadCreaturesLeaveMeatResources),
    ("Metabolism can charge body-size upkeep", MetabolismChargesBodySizeUpkeep),
    ("Metabolism can charge trait upkeep", MetabolismChargesTraitUpkeep),
    ("Metabolism charges plant specialization upkeep", MetabolismChargesPlantSpecializationUpkeep),
    ("Metabolism basal cost follows biome multiplier", MetabolismBasalCostFollowsBiomeMultiplier),
    ("Fat storage preserves egg reserve priority", FatStoragePreservesEggReservePriority),
    ("Fat storage releases before starvation death", FatStorageReleasesBeforeStarvationDeath),
    ("Reproduction builds egg reserve before laying", ReproductionBuildsEggReserveBeforeLaying),
    ("Reproduction fertility declines with age", ReproductionFertilityDeclinesWithAge),
    ("Reproduction fertility declines with crowding", ReproductionFertilityDeclinesWithCrowding),
    ("Reproduction lays mutated eggs", ReproductionCreatesOffspring),
    ("Eggs hatch into offspring", EggsHatchIntoOffspring),
    ("Egg environmental damage follows void and biome pressure", EggEnvironmentalDamageFollowsVoidAndBiomePressure),
    ("Juvenile creatures cannot reproduce before maturity", JuvenileCreaturesCannotReproduceBeforeMaturity),
    ("Juvenile growth scales effective traits", JuvenileGrowthScalesEffectiveTraits),
    ("Offspring investment scales juvenile growth", OffspringInvestmentScalesJuvenileGrowth),
    ("Minimal life loop is repeatable", MinimalLifeLoopIsRepeatable),
    ("Creature sensing reports local food direction", CreatureSensingReportsFoodDirection),
    ("Creature sensing splits plant and meat cues", CreatureSensingSplitsPlantAndMeatCues),
    ("Creature sensing reports plant quality cues", CreatureSensingReportsPlantQualityCues),
    ("Creature sensing reports plant preference bridge", CreatureSensingReportsPlantPreferenceBridge),
    ("Creature sensing reports visible creature cues", CreatureSensingReportsVisibleCreatureCues),
    ("Creature sensing smells similar creatures beyond vision", CreatureSensingSmellsSimilarCreaturesBeyondVision),
    ("Creature sensing separates predator prey similarity", CreatureSensingSeparatesPredatorPreySimilarity),
    ("Creature sensing hears intentional sound beyond vision", CreatureSensingHearsIntentionalSoundBeyondVision),
    ("Creature sensing smells meat beyond vision", CreatureSensingSmellsMeatBeyondVision),
    ("Creature sensing reports rotten meat cues", CreatureSensingReportsRottenMeatCues),
    ("Creature sensing reports local terrain drag", CreatureSensingReportsLocalTerrainDrag),
    ("Creature sensing reports habitat quality", CreatureSensingReportsHabitatQuality),
    ("Creature sensing applies biome vision range penalty", CreatureSensingAppliesBiomeVisionRangePenalty),
    ("Creature sensing reports local obstacles", CreatureSensingReportsLocalObstacles),
    ("Creature sensing reports memory direction", CreatureSensingReportsMemoryDirection),
    ("Creature sensing reports egg reserve readiness", CreatureSensingReportsEggReserveReadiness),
    ("Creature sensing reports reproductive context", CreatureSensingReportsReproductiveContext),
    ("Creature vision cone hides food behind it", CreatureVisionConeHidesFoodBehindIt),
    ("Creature sector vision buckets visible categories", CreatureSectorVisionBucketsVisibleCategories),
    ("Brain IO registry describes the dense adapter contract", BrainIoRegistryDescribesDenseAdapterContract),
    ("Legacy neural adapter maps grouped brain inputs", LegacyNeuralAdapterMapsGroupedBrainInputs),
    ("Neural controller turns senses into actions", NeuralControllerTurnsSensesIntoActions),
    ("Neural controller reuses actions on skipped world senses", NeuralControllerReusesActionsOnSkippedWorldSenses),
    ("Neural controller forces decisions on stale contact", NeuralControllerForcesDecisionsOnStaleContact),
    ("Neural controller parallel path matches single-threaded path", NeuralControllerParallelPathMatchesSingleThreadedPath),
    ("Neural controller consumes sector vision inputs", NeuralControllerConsumesSectorVisionInputs),
    ("Neural controller writes spatial memory", NeuralControllerWritesSpatialMemory),
    ("Neural controller honors memory tuning", NeuralControllerHonorsMemoryTuning),
    ("Forager predator turns creature proximity into attack intent", ForagerPredatorTurnsCreatureProximityIntoAttackIntent),
    ("Forager predator turns creature contact into attack intent", ForagerPredatorTurnsCreatureContactIntoAttackIntent),
    ("Forager predator suppresses similar creature contact attack", ForagerPredatorSuppressesSimilarCreatureContactAttack),
    ("Forager predator steers away from similar creature scent", ForagerPredatorSteersAwayFromSimilarCreatureScent),
    ("Sector forager starter follows sector plant cues", SectorForagerStarterFollowsSectorPlantCues),
    ("Scavenger starter follows sector meat cues", ScavengerStarterFollowsSectorMeatCues),
    ("Predator starter follows sector creature cues", PredatorStarterFollowsSectorCreatureCues),
    ("Opportunistic forager samples meat on contact", OpportunisticForagerSamplesMeatOnContact),
    ("Seed forager slows down near food", SeedForagerSlowsDownNearFood),
    ("Behavior assay summarizes seed forager responses", BehaviorAssaySummarizesSeedForagerResponses),
    ("Behavior assay reports plant choice probes", BehaviorAssayReportsPlantChoiceProbes),
    ("Behavior assay reports plant preference bridge probes", BehaviorAssayReportsPlantPreferenceBridgeProbes),
    ("Behavior assay detects fresh meat preference", BehaviorAssayDetectsFreshMeatPreference),
    ("Behavior assay detects rotten scent avoidance", BehaviorAssayDetectsRottenScentAvoidance),
    ("Brain input diagnostics summarize freshness wiring", BrainInputDiagnosticsSummarizeFreshnessWiring),
    ("Species brain input diagnostics summarize cluster wiring", SpeciesBrainInputDiagnosticsSummarizeClusterWiring),
    ("Explorer forager keeps searching without food cues", ExplorerForagerKeepsSearchingWithoutFoodCues),
    ("Behavior assay summarizes terrain response", BehaviorAssaySummarizesTerrainResponse),
    ("Behavior assay summarizes lateral terrain response", BehaviorAssaySummarizesLateralTerrainResponse),
    ("Scavenger forager starter brain follows carrion cues", ScavengerForagerStarterBrainFollowsCarrionCues),
    ("Freshness-aware scavenger starter brain avoids rot cues", FreshnessAwareScavengerStarterBrainAvoidsRotCues),
    ("Forager predator starter brain hunts creature cues", ForagerPredatorStarterBrainHuntsCreatureCues),
    ("Meat-oriented starters eat meat on contact", MeatOrientedStartersEatMeatOnContact),
    ("Neural brain migrates reproductive context inputs", NeuralBrainMigratesReproductiveContextInputs),
    ("Neural brain migrates memory inputs and outputs", NeuralBrainMigratesMemoryInputsAndOutputs),
    ("Neural brain migrates rotten meat sensing inputs", NeuralBrainMigratesRottenMeatSensingInputs),
    ("Neural brain migrates obstacle sensing inputs", NeuralBrainMigratesObstacleSensingInputs),
    ("Neural brain migrates sector vision inputs", NeuralBrainMigratesSectorVisionInputs),
    ("Neural brain migrates nearest aggregate schema", NeuralBrainMigratesNearestAggregateSchema),
    ("Neural brain migrates food contact input", NeuralBrainMigratesFoodContactInput),
    ("Neural brain migrates health ratio input", NeuralBrainMigratesHealthRatioInput),
    ("Neural brain migrates creature sector motion inputs", NeuralBrainMigratesCreatureSectorMotionInputs),
    ("Neural brain migrates creature contact input", NeuralBrainMigratesCreatureContactInput),
    ("Neural brain migrates plant quality inputs", NeuralBrainMigratesPlantQualityInputs),
    ("Neural brain migrates recent food energy yield input", NeuralBrainMigratesRecentFoodEnergyYieldInput),
    ("Neural brain migrates sector plant quality inputs", NeuralBrainMigratesSectorPlantQualityInputs),
    ("Neural brain migrates typed plant energy yield inputs", NeuralBrainMigratesTypedPlantEnergyYieldInputs),
    ("Neural brain migrates plant payoff trace inputs", NeuralBrainMigratesPlantPayoffTraceInputs),
    ("Neural brain migrates plant preference bridge inputs", NeuralBrainMigratesPlantPreferenceBridgeInputs),
    ("Neural brain migrates creature similarity inputs", NeuralBrainMigratesCreatureSimilarityInputs),
    ("Neural brain migrates habitat quality inputs", NeuralBrainMigratesHabitatQualityInputs),
    ("Neural brain migrates grab output and inputs", NeuralBrainMigratesGrabOutputAndInputs),
    ("Neural brain migrates sound output and inputs", NeuralBrainMigratesSoundOutputAndInputs),
    ("Neural brain migrates fat inputs", NeuralBrainMigratesFatInputs),
    ("Neural brain supports hidden nodes", NeuralBrainSupportsHiddenNodes),
    ("Brain factory describes hybrid neural architecture", BrainFactoryDescribesHybridNeuralArchitecture),
    ("Brain factory preserves hybrid starter brains", BrainFactoryPreservesHybridStarterBrains),
    ("Brain factory mutates hybrid neural brains", BrainFactoryMutatesHybridNeuralBrains),
    ("Brain factory supports hidden-layer neural architecture", BrainFactorySupportsHiddenLayerNeuralArchitecture),
    ("Brain factory supports rtNEAT graph architecture", BrainFactorySupportsRtNeatGraphArchitecture),
    ("World state tracks brain architecture metadata", WorldStateTracksBrainArchitectureMetadata),
    ("Lineage behavior assays summarize top founder strategies", LineageBehaviorAssaysSummarizeTopFounderStrategies),
    ("Creature attack damages contact targets", CreatureAttackDamagesContactTargets),
    ("Creature grab slows contact targets", CreatureGrabSlowsContactTargets),
    ("Creature attack deaths become injury meat", CreatureAttackDeathsBecomeInjuryMeat),
    ("Sparse mutation rates gate genome and brain changes", SparseMutationRatesGateGenomeAndBrainChanges),
    ("World mutation policy overrides inherited genome mutation settings", WorldMutationPolicyOverridesInheritedGenomeMutationSettings),
    ("Intent-gated eating requires eat output", IntentGatedEatingRequiresEatOutput),
    ("Intent-gated reproduction mutates brain", IntentGatedReproductionMutatesBrain),
    ("Neural life loop is repeatable", NeuralLifeLoopIsRepeatable),
    ("Spawned creatures create lineage records", SpawnedCreaturesCreateLineageRecords),
    ("Offspring lineage records parent and generation", OffspringLineageRecordsParentAndGeneration),
    ("Death system marks lineage death reason", DeathSystemMarksLineageDeathReason),
    ("Spatial heatmaps record lifecycle and interaction events", SpatialHeatmapsRecordLifecycleAndInteractionEvents),
    ("World state prunes extinct payloads", WorldStatePrunesExtinctPayloads),
    ("World state keeps survivor ancestor payloads", WorldStateKeepsSurvivorAncestorPayloads),
    ("Pruned simulation snapshots restore continuation", PrunedSimulationSnapshotsRestoreContinuation),
    ("Extinct payload pruning system runs in pipeline", ExtinctPayloadPruningSystemRunsInPipeline),
    ("Stats recording captures aggregate snapshot", StatsRecordingCapturesAggregateSnapshot),
    ("Stats recording ignores extinct brain payloads", StatsRecordingIgnoresExtinctBrainPayloads),
    ("Stats recording reports rtNEAT topology telemetry", StatsRecordingReportsRtNeatTopologyTelemetry),
    ("Stats recording reports biome pressure telemetry", StatsRecordingReportsBiomePressureTelemetry),
    ("Stats recording reports biome death causes", StatsRecordingReportsBiomeDeathCauses),
    ("Stats recording reports lifespan summary", StatsRecordingReportsLifespanSummary),
    ("Stats recording honors sample interval", StatsRecordingHonorsSampleInterval),
    ("Scenario factory seeds requested world", ScenarioFactorySeedsRequestedWorld),
    ("Scenario factory seeds requested plant type mix", ScenarioFactorySeedsRequestedPlantTypeMix),
    ("Plant type habitat affinity biases biome sampling", PlantTypeHabitatAffinityBiasesBiomeSampling),
    ("Scenario factory honors initial spawn region", ScenarioFactoryHonorsInitialSpawnRegion),
    ("Scenario resource density scales with world area", ScenarioResourceDensityScalesWithWorldArea),
    ("Scenario resource clustering creates local food patches", ScenarioResourceClusteringCreatesLocalFoodPatches),
    ("Generated small biome maps contain visible variety", GeneratedSmallBiomeMapsContainVisibleVariety),
    ("Natural climate biome maps create broad five-biome regions", NaturalClimateBiomeMapsCreateBroadFiveBiomeRegions),
    ("Natural climate single-cell maps stay neutral", NaturalClimateSingleCellMapsStayNeutral),
    ("Banded biome maps create broad regions", BandedBiomeMapsCreateBroadRegions),
    ("Edge band biome maps create productive ends", EdgeBandBiomeMapsCreateProductiveEnds),
    ("Edge ladder biome maps keep poor centers crossable", EdgeLadderBiomeMapsKeepPoorCentersCrossable),
    ("Edge corridor biome maps create harsh crossings", EdgeCorridorBiomeMapsCreateHarshCrossings),
    ("Obstacle maps create barriers and scattered rocks", ObstacleMapsCreateBarriersAndScatteredRocks),
    ("Biome map samples resources by density", BiomeMapSamplesResourcesByDensity),
    ("Resource void border excludes plant growth", ResourceVoidBorderExcludesPlantGrowth),
    ("Resource void clipped biome cells are skipped during sampling", ResourceVoidClippedBiomeCellsAreSkippedDuringSampling),
    ("Manual biome map JSON round trips", ManualBiomeMapJsonRoundTrips),
    ("Manual obstacle map JSON round trips", ManualObstacleMapJsonRoundTrips),
    ("World map artifact JSON round trips", WorldMapArtifactJsonRoundTrips),
    ("Creature-only spatial rebuild preserves static entities", CreatureOnlySpatialRebuildPreservesStaticEntities),
    ("Persistent spatial rebuild removes decayed resources", PersistentSpatialRebuildRemovesDecayedResources),
    ("Persistent spatial rebuild removes hatched eggs", PersistentSpatialRebuildRemovesHatchedEggs),
    ("Scenario factory creates deterministic biomes", ScenarioFactoryCreatesDeterministicBiomes),
    ("Scenario factory honors biome map kind", ScenarioFactoryHonorsBiomeMapKind),
    ("Scenario factory honors manual biome map path", ScenarioFactoryHonorsManualBiomeMapPath),
    ("Scenario factory honors manual obstacle map path", ScenarioFactoryHonorsManualObstacleMapPath),
    ("Scenario factory honors world map path", ScenarioFactoryHonorsWorldMapPath),
    ("Scenario factory honors natural climate biome map kind", ScenarioFactoryHonorsNaturalClimateBiomeMapKind),
    ("Scenario factory honors obstacle map kind", ScenarioFactoryHonorsObstacleMapKind),
    ("Scenario factory supports initial brain kinds", ScenarioFactorySupportsInitialBrainKinds),
    ("Scenario factory honors reproduction intent toggle", ScenarioFactoryHonorsReproductionIntentToggle),
    ("Brain profile JSON round trips neural controllers", BrainProfileJsonRoundTripsNeuralControllers),
    ("Brain profile JSON round trips rtNEAT graph controllers", BrainProfileJsonRoundTripsRtNeatGraphControllers),
    ("Brain profile compatibility reports schema status", BrainProfileCompatibilityReportsSchemaStatus),
    ("Species profile JSON round trips representative genomes and brains", SpeciesProfileJsonRoundTripsRepresentativeGenomesAndBrains),
    ("Species profile injection creates founder creatures", SpeciesProfileInjectionCreatesFounderCreatures),
    ("Species profile injection honors quadrant spawn region", SpeciesProfileInjectionHonorsQuadrantSpawnRegion),
    ("Species profile injection can override brain kind", SpeciesProfileInjectionCanOverrideBrainKind),
    ("Species profile injection can randomize brains per founder", SpeciesProfileInjectionCanRandomizeBrainsPerFounder),
    ("Species clustering groups injected profile founders", SpeciesClusteringGroupsInjectedProfileFounders),
    ("Species clustering splits distinct brains", SpeciesClusteringSplitsDistinctBrains),
    ("Species clustering separates starter ecotypes", SpeciesClusteringSeparatesStarterEcotypes),
    ("Species clustering handles non-neural creatures", SpeciesClusteringHandlesNonNeuralCreatures),
    ("Species cluster history tracks snapshots", SpeciesClusterHistoryTracksSnapshots),
    ("Species behavior change highlights notable shifts", SpeciesBehaviorChangeHighlightsNotableShifts),
    ("Scenario species roster injects profile founders", ScenarioSpeciesRosterInjectsProfileFounders),
    ("Scenario species roster label names injection groups", ScenarioSpeciesRosterLabelNamesInjectionGroups),
    ("Scenario species roster injects brain overrides", ScenarioSpeciesRosterInjectsBrainOverrides),
    ("Scenario species roster injects brain profile paths", ScenarioSpeciesRosterInjectsBrainProfilePaths),
    ("Roster lineage summaries group injected profile descendants", RosterLineageSummariesGroupInjectedProfileDescendants),
    ("Simulation snapshots restore exact continuation", SimulationSnapshotsRestoreExactContinuation),
    ("Simulation snapshot capture can sample stats history", SimulationSnapshotCaptureCanSampleStatsHistory),
    ("Scenario pressure knobs seed starting genome", ScenarioPressureKnobsSeedStartingGenome),
    ("Scenario metadata describes editable JSON fields", ScenarioMetadataDescribesEditableJsonFields),
    ("Scenario JSON migrates legacy resource count", ScenarioJsonMigratesLegacyResourceCount),
    ("Biome JSON migrates legacy names", BiomeJsonMigratesLegacyNames),
    ("Scenario JSON round trips", ScenarioJsonRoundTrips)
};

var failures = 0;

foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {test.Name}");
        Console.Error.WriteLine(ex);
    }
}

if (failures > 0)
{
    Console.Error.WriteLine($"{failures} test(s) failed.");
    Environment.ExitCode = 1;
    return;
}

Console.WriteLine($"{tests.Length} test(s) passed.");

static void SimulationClockAdvancesByFixedSteps()
{
    var config = new SimulationConfig { FixedDeltaSeconds = 0.25f };
    var simulation = new Simulation(config, seed: 42);

    simulation.RunSteps(4);

    AssertEqual(4L, simulation.State.Tick, "Tick count");
    AssertClose(1.0, simulation.State.ElapsedSeconds, 0.000001, "Elapsed seconds");
}

static void SimulationProfilerRecordsSystemTimings()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.25f },
        seed: 42,
        systems: [new ProfilingNoOpSystem(), new ProfilingNoOpSystem()]);
    simulation.Profile = new SimulationProfile();

    simulation.RunSteps(3);

    AssertEqual(3L, simulation.Profile.ProfiledSteps, "Profiled steps");
    AssertEqual(2, simulation.Profile.Systems.Count, "Profiled system count");
    AssertEqual("ProfilingNoOpSystem", simulation.Profile.Systems[0].SystemName, "Profiled system name");
    AssertEqual(3L, simulation.Profile.Systems[0].CallCount, "First system call count");
    AssertEqual(3L, simulation.Profile.Systems[1].CallCount, "Second system call count");
    AssertTrue(simulation.Profile.TotalMilliseconds >= 0.0, "Profiled time should be non-negative");
}

static void SimulationProfilerCanBePaused()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.25f },
        seed: 42,
        systems: [new ProfilingNoOpSystem()]);
    simulation.Profile = new SimulationProfile { IsActive = false };

    simulation.RunSteps(2);
    AssertEqual(0L, simulation.Profile.ProfiledSteps, "Paused profiled steps");
    AssertEqual(0, simulation.Profile.Systems.Count, "Paused profile system count");

    simulation.Profile.IsActive = true;
    simulation.RunSteps(1);

    AssertEqual(1L, simulation.Profile.ProfiledSteps, "Resumed profiled steps");
    AssertEqual(1, simulation.Profile.Systems.Count, "Resumed profile system count");
    AssertEqual(1L, simulation.Profile.Systems[0].CallCount, "Resumed profile call count");
}

static void SensingProfilerRecordsCandidateCounts()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 7,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex)
        ]);
    simulation.Profile = new SimulationProfile();

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 100f,
        VisionAngleRadians = MathF.Tau,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        Position = new SimVector2(30f, 20f),
        Radius = 1f,
        Calories = 20f,
        MaxCalories = 20f,
        RegrowthCaloriesPerSecond = 0f
    });
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Meat,
        Position = new SimVector2(40f, 20f),
        Radius = 1f,
        Calories = 20f,
        MaxCalories = 20f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();

    var sensing = simulation.Profile.Sensing;
    AssertEqual(1L, sensing.CreaturesSensed, "Sensed creature count");
    AssertEqual(1L, sensing.TraitCacheCreatures, "Trait cache creature count");
    AssertEqual(1L, sensing.WorldSenseRefreshes, "World sense refresh count");
    AssertEqual(1L, sensing.WorldSenseForcedRefreshes, "World sense forced refresh count");
    AssertEqual(0L, sensing.WorldSenseSkippedUpdates, "World sense skipped count");
    AssertTrue(sensing.CreatureSetupMilliseconds >= 0.0, "Creature setup time should be non-negative");
    AssertTrue(sensing.InternalStateMilliseconds >= 0.0, "Internal state time should be non-negative");
    AssertEqual(1L, sensing.ResourceQueries, "Resource query count");
    AssertEqual(2L, sensing.ResourceCandidates, "Resource candidate count");
    AssertEqual(1L, sensing.PlantResourceQueries, "Plant resource query count");
    AssertEqual(1L, sensing.PlantResourceQueryCandidates, "Plant resource query candidate count");
    AssertEqual(1L, sensing.MeatResourceQueries, "Meat resource query count");
    AssertEqual(1L, sensing.MeatResourceQueryCandidates, "Meat resource query candidate count");
    AssertEqual(1L, sensing.PlantCandidates, "Plant candidate count");
    AssertEqual(1L, sensing.MeatResourceCandidates, "Meat candidate count");
    AssertEqual(1L, sensing.VisiblePlantCandidates, "Visible plant count");
    AssertEqual(1L, sensing.VisibleMeatResourceCandidates, "Visible meat count");
    AssertEqual(1L, sensing.CreatureQueries, "Creature query count");
    AssertEqual(1L, sensing.CreatureCandidates, "Raw creature candidate count includes self");
    AssertEqual(0L, sensing.VisibleCreatureCandidates, "Visible creature count excludes self");
    AssertTrue(sensing.CreatureCellsVisited > 0, "Creature query should record visited cells");
    AssertTrue(sensing.CreatureNonEmptyCellsVisited > 0, "Creature query should record non-empty cells");
    AssertEqual(1L, sensing.CreatureSelfRejectedCandidates, "Self creature reject count");
    AssertTrue(sensing.TerrainSenseMilliseconds >= 0.0, "Terrain sensing time should be non-negative");
    AssertEqual(1L, sensing.ObstacleSenseSamples, "Obstacle sense sample count");
    AssertTrue(sensing.ObstacleSenseMilliseconds >= 0.0, "Obstacle sensing time should be non-negative");
    AssertTrue(sensing.MemorySenseMilliseconds >= 0.0, "Memory sensing time should be non-negative");
    AssertTrue(sensing.SenseFinalizationMilliseconds >= 0.0, "Sense finalization time should be non-negative");
    AssertTrue(sensing.TotalMeasuredMilliseconds >= 0.0, "Measured sensing time should be non-negative");
}

static void CreatureSensingTimeSlicesWorldQueries()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 8,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(
                spatialIndex,
                worldSenseIntervalTicks: 4,
                closeSenseRefreshProximity: 0.7f)
        ]);
    simulation.Profile = new SimulationProfile();

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 100f,
        VisionAngleRadians = MathF.Tau,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        Position = new SimVector2(90f, 20f),
        Radius = 1f,
        Calories = 20f,
        MaxCalories = 20f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();
    simulation.Step();
    simulation.Step();
    simulation.Step();

    var sensing = simulation.Profile.Sensing;
    AssertEqual(4L, sensing.CreaturesSensed, "Time-sliced sensed creature count");
    AssertEqual(1L, sensing.WorldSenseForcedRefreshes, "Initial world sense refresh should be forced");
    AssertEqual(1L, sensing.WorldSenseScheduledRefreshes, "One later world sense refresh should be scheduled");
    AssertEqual(0L, sensing.WorldSenseCloseRefreshes, "Distant food should not trigger close refresh");
    AssertEqual(2L, sensing.WorldSenseSkippedUpdates, "Two world sense updates should be skipped");
    AssertEqual(2L, sensing.ResourceQueries, "Resource query count should follow world sense refreshes");

    simulation.Profile = new SimulationProfile();
    var closeCreature = simulation.State.Creatures[0];
    var closeSenses = closeCreature.Senses;
    closeSenses.FoodDetected = true;
    closeSenses.FoodProximity = 0.95f;
    closeCreature.Senses = closeSenses;
    simulation.State.Creatures[0] = closeCreature;

    simulation.Step();

    var closeSensing = simulation.Profile.Sensing;
    AssertEqual(1L, closeSensing.WorldSenseCloseRefreshes, "Close food should force a world sense refresh");
    AssertEqual(0L, closeSensing.WorldSenseSkippedUpdates, "Close food should not skip world sensing");
    AssertEqual(1L, closeSensing.ResourceQueries, "Close refresh should run resource query");
}

static void CreatureSensingParallelPathMatchesSingleThreadedPath()
{
    var singleThreaded = CreateCreatureSensingParallelProbe(sensingThreadCount: 1);
    var parallel = CreateCreatureSensingParallelProbe(sensingThreadCount: 4);

    singleThreaded.Step();
    parallel.Step();

    AssertEqual(singleThreaded.State.Creatures.Count, parallel.State.Creatures.Count, "Parallel sensing creature count");
    for (var i = 0; i < singleThreaded.State.Creatures.Count; i++)
    {
        var expected = singleThreaded.State.Creatures[i];
        var actual = parallel.State.Creatures[i];
        AssertEqual(
            JsonSerializer.Serialize(expected.Senses),
            JsonSerializer.Serialize(actual.Senses),
            $"Parallel sensing senses {i}");
        AssertClose(expected.TenderPlantPayoffTrace, actual.TenderPlantPayoffTrace, 0.000001, $"Parallel sensing tender trace {i}");
        AssertClose(expected.RichPlantPayoffTrace, actual.RichPlantPayoffTrace, 0.000001, $"Parallel sensing rich trace {i}");
        AssertClose(expected.ToughPlantPayoffTrace, actual.ToughPlantPayoffTrace, 0.000001, $"Parallel sensing tough trace {i}");
    }
}

static Simulation CreateCreatureSensingParallelProbe(int sensingThreadCount)
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 32f);
    var simulation = new Simulation(
        new SimulationConfig { WorldWidth = 600f, WorldHeight = 600f, FixedDeltaSeconds = 0.1f },
        seed: 82,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(
                spatialIndex,
                worldSenseIntervalTicks: 1,
                enableSectorVision: true,
                sensingThreadCount: sensingThreadCount)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 5f,
        MaxSpeed = 18f,
        SenseRadius = 120f,
        VisionAngleRadians = MathF.Tau * 0.75f,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f,
        TenderPlantAdaptation = 0.2f,
        RichPlantAdaptation = 0.1f,
        ToughPlantAdaptation = 0.05f
    });

    var positions = new[]
    {
        new SimVector2(100f, 100f),
        new SimVector2(150f, 115f),
        new SimVector2(220f, 125f),
        new SimVector2(130f, 210f),
        new SimVector2(260f, 230f),
        new SimVector2(320f, 180f),
        new SimVector2(360f, 260f),
        new SimVector2(420f, 220f),
        new SimVector2(460f, 320f),
        new SimVector2(180f, 330f),
        new SimVector2(280f, 360f),
        new SimVector2(380f, 390f)
    };
    for (var i = 0; i < positions.Length; i++)
    {
        simulation.State.SpawnCreature(genomeId, positions[i], energy: 45f + i * 3f);
        var creature = simulation.State.Creatures[^1];
        creature.HeadingRadians = i * 0.43f;
        creature.Velocity = SimVector2.FromAngle(creature.HeadingRadians) * (2f + i);
        creature.MemoryVector = new SimVector2((i % 4) * 0.1f, (i % 3) * -0.08f);
        creature.LastPlantCaloriesEaten = i % 2 == 0 ? 4f : 0f;
        creature.LastTenderPlantDigestedEnergy = i % 3 == 0 ? 2f : 0f;
        creature.LastRichPlantDigestedEnergy = i % 4 == 0 ? 3f : 0f;
        creature.LastToughPlantDigestedEnergy = i % 5 == 0 ? 1.5f : 0f;
        simulation.State.Creatures[^1] = creature;
    }

    var parentId = simulation.State.Creatures[0].Id;
    simulation.State.SpawnEgg(genomeId, brainId: -1, parentId, new SimVector2(175f, 130f), energy: 28f, incubationSeconds: 30f, generation: 1);
    simulation.State.SpawnEgg(genomeId, brainId: -1, parentId, new SimVector2(340f, 245f), energy: 32f, incubationSeconds: 30f, generation: 1);

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        PlantKind = PlantResourceKind.Generic,
        Position = new SimVector2(120f, 105f),
        Radius = 4f,
        Calories = 70f,
        MaxCalories = 80f,
        RegrowthCaloriesPerSecond = 0f
    });
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        PlantKind = PlantResourceKind.Rich,
        Position = new SimVector2(245f, 140f),
        Radius = 6f,
        Calories = 95f,
        MaxCalories = 110f,
        RegrowthCaloriesPerSecond = 0f
    });
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Meat,
        Position = new SimVector2(300f, 210f),
        Radius = 5f,
        Calories = 55f,
        MaxCalories = 80f,
        MeatAgeSeconds = 8f,
        RegrowthCaloriesPerSecond = 0f
    });
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Meat,
        Position = new SimVector2(405f, 235f),
        Radius = 5f,
        Calories = 45f,
        MaxCalories = 80f,
        MeatAgeSeconds = 70f,
        RegrowthCaloriesPerSecond = 0f
    });

    return simulation;
}

static void CreatureSensingThrottlesProximityCloseRefreshes()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 81,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(
                spatialIndex,
                worldSenseIntervalTicks: 10,
                closeSenseRefreshProximity: 0.7f,
                closeSenseRefreshMinimumTicks: 3)
        ]);
    simulation.Profile = new SimulationProfile();

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 100f,
        VisionAngleRadians = MathF.Tau,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        Position = new SimVector2(30f, 20f),
        Radius = 1f,
        Calories = 20f,
        MaxCalories = 20f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();
    AssertEqual(1L, simulation.Profile.Sensing.WorldSenseForcedRefreshes, "Initial world sense refresh should be forced");

    simulation.Profile = new SimulationProfile();
    var creature = simulation.State.Creatures[0];
    creature.Energy = 75f;
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    var firstSkippedSensing = simulation.Profile.Sensing;
    AssertEqual(1L, firstSkippedSensing.WorldSenseSkippedUpdates, "Fresh close proximity should wait for the close minimum");
    AssertEqual(0L, firstSkippedSensing.WorldSenseCloseRefreshes, "Close proximity should not refresh before the minimum age");
    AssertEqual(0L, firstSkippedSensing.ResourceQueries, "Skipped proximity refresh should avoid resource queries");
    AssertClose(0.75f, simulation.State.Creatures[0].Senses.EnergyRatio, 0.000001, "Skipped world sense should still refresh internal energy");

    simulation.Step();
    simulation.Step();

    var sensing = simulation.Profile.Sensing;
    AssertEqual(2L, sensing.WorldSenseSkippedUpdates, "Two proximity refreshes should be throttled");
    AssertEqual(1L, sensing.WorldSenseCloseRefreshes, "Close proximity should refresh once the minimum age is reached");
    AssertEqual(1L, sensing.ResourceQueries, "Only the delayed close refresh should run resource queries");
}

static void CreatureSensingKeepsContactCloseRefreshImmediate()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 82,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(
                spatialIndex,
                worldSenseIntervalTicks: 10,
                closeSenseRefreshProximity: 0.7f,
                closeSenseRefreshMinimumTicks: 3)
        ]);
    simulation.Profile = new SimulationProfile();

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 100f,
        VisionAngleRadians = MathF.Tau,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f);
    simulation.Step();

    simulation.Profile = new SimulationProfile();
    var creature = simulation.State.Creatures[0];
    creature.IsTouchingFood = true;
    creature.FoodContactKind = FoodContactKind.Resource;
    creature.FoodContactResourceKind = ResourceKind.Plant;
    creature.FoodContactPlantKind = PlantResourceKind.Rich;
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    var sensing = simulation.Profile.Sensing;
    var senses = simulation.State.Creatures[0].Senses;
    AssertEqual(1L, sensing.WorldSenseCloseRefreshes, "Direct food contact should bypass the close refresh minimum");
    AssertEqual(0L, sensing.WorldSenseSkippedUpdates, "Direct food contact should not be skipped");
    AssertClose(1f, senses.FoodContact, 0.000001, "Food contact should stay fresh");
    AssertClose(1f, senses.PlantFoodContact, 0.000001, "Plant contact should stay fresh");
    AssertClose(PlantResourceTraits.EnergyQualitySense(PlantResourceKind.Rich), senses.PlantFoodContactEnergyQuality, 0.000001, "Plant contact quality should stay fresh");
}

static void RandomRepeatsFromSameSeed()
{
    var first = new DeterministicRandom(123456789);
    var second = new DeterministicRandom(123456789);

    for (var i = 0; i < 16; i++)
    {
        AssertEqual(first.NextUInt64(), second.NextUInt64(), $"Random value {i}");
    }
}

static void SystemPipelineIsRepeatable()
{
    var first = CreateProbeSimulation();
    var second = CreateProbeSimulation();

    first.RunSteps(10);
    second.RunSteps(10);

    AssertEqual(first.State.Creatures.Count, second.State.Creatures.Count, "Creature count");

    for (var i = 0; i < first.State.Creatures.Count; i++)
    {
        var firstCreature = first.State.Creatures[i];
        var secondCreature = second.State.Creatures[i];

        AssertEqual(firstCreature.Id, secondCreature.Id, $"Creature {i} id");
        AssertClose(firstCreature.Position.X, secondCreature.Position.X, 0.000001, $"Creature {i} x");
        AssertClose(firstCreature.Position.Y, secondCreature.Position.Y, 0.000001, $"Creature {i} y");
        AssertClose(firstCreature.AgeSeconds, secondCreature.AgeSeconds, 0.000001, $"Creature {i} age");
    }
}

static Simulation CreateProbeSimulation()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 0.1f
        },
        seed: 8675309,
        systems: [new ProbeMovementSystem()]);

    simulation.State.Creatures.Add(new CreatureState
    {
        Id = simulation.State.CreateEntityId(),
        Position = new SimVector2(50f, 50f),
        Health = 1f,
        Energy = 1f
    });

    return simulation;
}

static void MovementRecordsSearchDistance()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 0.5f
        },
        seed: 2,
        systems: [new MovementSystem()]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 20f);
    var creature = simulation.State.Creatures[0];
    creature.DesiredVelocity = new SimVector2(12f, 0f);
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    var moved = simulation.State.Creatures[0];
    AssertClose(6f, moved.LastDistanceTraveled, 0.000001, "Last movement distance");
    AssertClose(6f, moved.DistanceSinceLastMeal, 0.000001, "Distance since meal accumulates");
    AssertClose(26f, moved.Position.X, 0.000001, "Movement x position");
    AssertClose(26f, moved.MaxXReached, 0.000001, "Personal max x is updated");
    AssertClose(26f, simulation.State.Stats.MaxCreatureXReached, 0.000001, "Run max x is updated");
}

static void MovementBlocksObstacleCollisions()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 1f
        },
        seed: 220,
        systems: [new MovementSystem()]);
    var cells = new bool[100];
    cells[2 * 10 + 3] = true;
    simulation.State.SetObstacles(ObstacleMap.CreateFromCells(
        simulation.State.Bounds,
        cellSize: 10f,
        cellCountX: 10,
        cellCountY: 10,
        cells));

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = 20f,
        BodyRadius = 1f,
        MovementEnergyPerSecond = 0f,
        MaturityAgeSeconds = 0f
    });
    simulation.State.SpawnCreature(genomeId, new SimVector2(25f, 25f), energy: 20f);
    var creature = simulation.State.Creatures[0];
    creature.DesiredVelocity = new SimVector2(10f, 0f);
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    var moved = simulation.State.Creatures[0];
    AssertClose(25f, moved.Position.X, 0.000001, "Blocked movement x");
    AssertClose(25f, moved.Position.Y, 0.000001, "Blocked movement y");
    AssertClose(0f, moved.LastDistanceTraveled, 0.000001, "Blocked movement distance");
    AssertTrue(moved.LastMovementBlocked, "Creature should record obstacle contact");
}

static void MovementSlidesAlongObstacleEdges()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 1f
        },
        seed: 221,
        systems: [new MovementSystem()]);
    var cells = new bool[100];
    cells[3 * 10 + 3] = true;
    simulation.State.SetObstacles(ObstacleMap.CreateFromCells(
        simulation.State.Bounds,
        cellSize: 10f,
        cellCountX: 10,
        cellCountY: 10,
        cells));

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = 20f,
        BodyRadius = 1f,
        MovementEnergyPerSecond = 0f,
        MaturityAgeSeconds = 0f
    });
    simulation.State.SpawnCreature(genomeId, new SimVector2(25f, 25f), energy: 20f);
    var creature = simulation.State.Creatures[0];
    creature.DesiredVelocity = new SimVector2(10f, 10f);
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    var moved = simulation.State.Creatures[0];
    AssertClose(35f, moved.Position.X, 0.000001, "Sliding movement x");
    AssertClose(25f, moved.Position.Y, 0.000001, "Sliding movement y");
    AssertClose(10f, moved.LastDistanceTraveled, 0.000001, "Sliding movement distance");
    AssertTrue(moved.LastMovementBlocked, "Sliding should still record a blocked component");
}

static void MovementCostFollowsBiomeMultiplier()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 1f
        },
        seed: 22,
        systems: [new MovementSystem(new BiomePressureProfile(1f, 1f, 1.75f, 1f))]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = CreatureGenome.Baseline.MaxSpeed,
        MovementEnergyPerSecond = 2f,
        MaturityAgeSeconds = 0f
    });
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 20f);
    var creature = simulation.State.Creatures[0];
    creature.DesiredVelocity = new SimVector2(CreatureGenome.Baseline.MaxSpeed, 0f);
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    AssertClose(16.5f, simulation.State.Creatures[0].Energy, 0.000001, "Movement biome cost");
}

static void MovementSpeedFollowsBiomeMultiplier()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 1f
        },
        seed: 24,
        systems: [new MovementSystem(biomeSpeedProfile: new BiomePressureProfile(1f, 1f, 0.5f, 1f))]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = 24f,
        MaturityAgeSeconds = 0f
    });
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 20f);
    var creature = simulation.State.Creatures[0];
    creature.DesiredVelocity = new SimVector2(12f, 0f);
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    var moved = simulation.State.Creatures[0];
    AssertClose(26f, moved.Position.X, 0.000001, "Biome speed x position");
    AssertClose(6f, moved.Velocity.Length, 0.000001, "Biome speed actual velocity");
    AssertClose(6f, moved.LastDistanceTraveled, 0.000001, "Biome speed distance");
}

static void MovementSpeedCostIsNonlinear()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 1f
        },
        seed: 23,
        systems: [new MovementSystem(movementSpeedCostExponent: 2f)]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = CreatureGenome.Baseline.MaxSpeed * 2f,
        MovementEnergyPerSecond = 1f,
        MaturityAgeSeconds = 0f
    });
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 40f), energy: 10f);

    var slowCreature = simulation.State.Creatures[0];
    slowCreature.DesiredVelocity = new SimVector2(CreatureGenome.Baseline.MaxSpeed * 0.5f, 0f);
    simulation.State.Creatures[0] = slowCreature;

    var fastCreature = simulation.State.Creatures[1];
    fastCreature.DesiredVelocity = new SimVector2(CreatureGenome.Baseline.MaxSpeed * 2f, 0f);
    simulation.State.Creatures[1] = fastCreature;

    simulation.Step();

    AssertClose(9.75f, simulation.State.Creatures[0].Energy, 0.000001, "Slow movement nonlinear cost");
    AssertClose(6f, simulation.State.Creatures[1].Energy, 0.000001, "Fast movement nonlinear cost");
}

static void InvalidConfigurationIsRejected()
{
    AssertThrows<InvalidOperationException>(
        () => new Simulation(new SimulationConfig { WorldWidth = 0f }, seed: 1),
        "Zero-width world");

    AssertThrows<InvalidOperationException>(
        () => new Simulation(new SimulationConfig { FixedDeltaSeconds = float.NaN }, seed: 1),
        "NaN fixed delta");
}

static void ResourceRegrowthIsCapped()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 1,
        systems: [new ResourceRegrowthSystem()]);

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(10f, 10f),
        Radius = 2f,
        Calories = 8f,
        MaxCalories = 10f,
        RegrowthCaloriesPerSecond = 5f
    });

    simulation.Step();

    AssertClose(10f, simulation.State.Resources[0].Calories, 0.000001, "Capped calories");
}

static void SeasonalFertilityScalesPlantRegrowth()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 2,
        systems:
        [
            new ResourceRegrowthSystem(
                enableSeasons: true,
                seasonLengthSeconds: 4f,
                seasonFertilityAmplitude: 0.5f,
                seasonPhaseOffsetSeconds: 1f)
        ]);

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        Position = new SimVector2(10f, 10f),
        Radius = 2f,
        Calories = 0f,
        MaxCalories = 100f,
        RegrowthCaloriesPerSecond = 10f
    });

    simulation.Step();

    AssertClose(15f, simulation.State.Resources[0].Calories, 0.000001, "Peak-season regrowth calories");
}

static void SeasonalFertilityScalesPlantDormancy()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 3,
        systems:
        [
            new ResourceRegrowthSystem(
                enableSeasons: true,
                seasonLengthSeconds: 4f,
                seasonFertilityAmplitude: 0.5f,
                seasonPhaseOffsetSeconds: 1f)
        ]);

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        Position = new SimVector2(10f, 10f),
        Radius = 2f,
        Calories = 0f,
        MaxCalories = 100f,
        RegrowthCaloriesPerSecond = 10f,
        RespawnSecondsRemaining = 3f
    });

    simulation.Step();

    AssertClose(1.5f, simulation.State.Resources[0].RespawnSecondsRemaining, 0.000001, "Peak-season dormancy countdown");
    AssertClose(0f, simulation.State.Resources[0].Calories, 0.000001, "Dormant plant remains inedible");
}

static void OpposedSeasonalFertilityAlternatesWorldHalves()
{
    var bounds = new WorldBounds(100f, 50f);
    var left = SeasonalFertility.CalculateAt(
        enabled: true,
        elapsedSeconds: 0,
        seasonLengthSeconds: 4f,
        fertilityAmplitude: 0.5f,
        phaseOffsetSeconds: 1f,
        SeasonPhaseMode.HorizontalOpposed,
        bounds,
        new SimVector2(10f, 25f));
    var right = SeasonalFertility.CalculateAt(
        enabled: true,
        elapsedSeconds: 0,
        seasonLengthSeconds: 4f,
        fertilityAmplitude: 0.5f,
        phaseOffsetSeconds: 1f,
        SeasonPhaseMode.HorizontalOpposed,
        bounds,
        new SimVector2(90f, 25f));

    AssertClose(1.5f, left.FertilityMultiplier, 0.000001, "Left half should be in summer");
    AssertClose(0.5f, right.FertilityMultiplier, 0.000001, "Right half should be in winter");
}

static void BiomeSeasonalResponseScalesPlantRegrowth()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 4,
        systems:
        [
            new ResourceRegrowthSystem(
                enableSeasons: true,
                seasonLengthSeconds: 4f,
                seasonFertilityAmplitude: 0.5f,
                seasonPhaseOffsetSeconds: 1f,
                biomeSeasonalAmplitudeProfile: new BiomePressureProfile(1f, 1f, 0.5f, 1f))
        ]);

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        Position = new SimVector2(10f, 10f),
        Radius = 2f,
        Calories = 0f,
        MaxCalories = 100f,
        RegrowthCaloriesPerSecond = 10f
    });

    simulation.Step();

    AssertClose(12.5f, simulation.State.Resources[0].Calories, 0.000001, "Grassland seasonal response regrowth calories");
}

static void LocalFertilityRecoversAfterPlantDepletion()
{
    var map = LocalFertilityMap.Create(
        new WorldBounds(100f, 100f),
        cellSize: 50f,
        minimumMultiplier: 0.4f,
        recoveryPerSecond: 0.1f,
        depletionPerPlant: 0.25f,
        neighborDepletionShare: 0.5f);

    map.ApplyPlantDepletion(new SimVector2(25f, 25f));

    AssertClose(0.75f, map.GetMultiplierAt(new SimVector2(25f, 25f)), 0.000001, "Center fertility depletion");
    AssertClose(0.875f, map.GetMultiplierAt(new SimVector2(75f, 25f)), 0.000001, "Neighbor fertility depletion");
    AssertClose(0.9375f, map.GetMultiplierAt(new SimVector2(75f, 75f)), 0.000001, "Diagonal fertility depletion");
    AssertClose(1f, map.Summarize().DepletedCellShare, 0.000001, "All cells should be recovering after corner depletion");

    map.Recover(1f);

    AssertClose(0.85f, map.GetMultiplierAt(new SimVector2(25f, 25f)), 0.000001, "Center fertility recovery");
    AssertClose(0.975f, map.GetMultiplierAt(new SimVector2(75f, 25f)), 0.000001, "Neighbor fertility recovery");
    AssertClose(1f, map.GetMultiplierAt(new SimVector2(75f, 75f)), 0.000001, "Diagonal fertility should cap at full recovery");
}

static void LocalFertilitySlowsPlantDormancy()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 1f
        },
        seed: 122,
        systems:
        [
            new ResourceRegrowthSystem(
                plantRespawnDelaySecondsMin: 1f,
                plantRespawnDelaySecondsMax: 1f,
                plantRespawnCaloriesMin: 4f,
                plantRespawnCaloriesMax: 4f)
        ]);
    simulation.State.SetLocalFertility(LocalFertilityMap.Create(
        simulation.State.Bounds,
        cellSize: 50f,
        minimumMultiplier: 0.5f,
        recoveryPerSecond: 0f,
        depletionPerPlant: 0.5f,
        neighborDepletionShare: 0f));

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        Position = new SimVector2(25f, 25f),
        Radius = 2f,
        Calories = 0f,
        MaxCalories = 10f,
        RegrowthCaloriesPerSecond = 5f
    });

    simulation.Step();
    AssertClose(0.5f, simulation.State.LocalFertility.GetMultiplierAt(new SimVector2(25f, 25f)), 0.000001, "Depleted local fertility");
    AssertClose(1f, simulation.State.Resources[0].RespawnSecondsRemaining, 0.000001, "Dormancy starts at full sampled duration");

    simulation.Step();
    AssertClose(0f, simulation.State.Resources[0].Calories, 0.000001, "Low fertility keeps plant dormant");
    AssertClose(0.5f, simulation.State.Resources[0].RespawnSecondsRemaining, 0.000001, "Dormancy countdown is slowed by local fertility");

    simulation.Step();
    AssertClose(4f, simulation.State.Resources[0].Calories, 0.000001, "Plant eventually respawns once slowed countdown completes");
}

static void DepletedResourcesCanRelocateBeforeRegrowing()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 1f
        },
        seed: 21,
        systems: [new ResourceRegrowthSystem(relocateDepletedResources: true)]);

    var initialPosition = new SimVector2(10f, 10f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = initialPosition,
        Radius = 2f,
        Calories = 0f,
        MaxCalories = 10f,
        RegrowthCaloriesPerSecond = 5f
    });

    simulation.Step();

    var resource = simulation.State.Resources[0];
    AssertTrue(resource.Position != initialPosition, "Depleted resource should relocate");
    AssertTrue(simulation.State.Bounds.Contains(resource.Position), "Relocated resource should remain inside bounds");
    AssertClose(5f, resource.Calories, 0.000001, "Relocated resource calories after regrowth");
}

static void DepletedPlantsCanDisperseLocally()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 500f,
            WorldHeight = 500f,
            FixedDeltaSeconds = 1f
        },
        seed: 210,
        systems:
        [
            new ResourceRegrowthSystem(
                relocateDepletedResources: true,
                resourceClusterStrength: 0f,
                plantLocalDispersalChance: 1f,
                plantLocalDispersalRadius: 25f)
        ]);

    var initialPosition = new SimVector2(250f, 250f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = initialPosition,
        Radius = 2f,
        Calories = 0f,
        MaxCalories = 10f,
        RegrowthCaloriesPerSecond = 5f
    });

    simulation.Step();

    var resource = simulation.State.Resources[0];
    AssertTrue(simulation.State.Bounds.Contains(resource.Position), "Locally dispersed resource should remain inside bounds");
    AssertTrue(
        SimVector2.Distance(initialPosition, resource.Position) <= 25.0001f,
        "Locally dispersed resource should stay near the depleted plant");
    AssertClose(5f, resource.Calories, 0.000001, "Locally dispersed resource calories after regrowth");
    AssertEqual(1, simulation.State.Stats.PlantDepletionCount, "Local dispersal depletion count");
    AssertEqual(1, simulation.State.Stats.PlantLocalDispersalCount, "Local dispersal relocation count");
    AssertEqual(0, simulation.State.Stats.PlantClusterRelocationCount, "Local dispersal cluster fallback count");
    AssertEqual(0, simulation.State.Stats.PlantGlobalRelocationCount, "Local dispersal global fallback count");
}

static void DepletedPlantsKeepHabitatBiomeWhenRelocating()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 200f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 1f
        },
        seed: 211,
        systems:
        [
            new ResourceRegrowthSystem(
                relocateDepletedResources: true,
                resourceClusterStrength: 0f,
                plantLocalDispersalChance: 0f)
        ]);
    simulation.State.SetBiomes(BiomeMap.CreateFromCells(
        simulation.State.Bounds,
        cellSize: 100f,
        cellCountX: 2,
        cellCountY: 1,
        [BiomeKind.Grassland, BiomeKind.Rich]));

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        Position = new SimVector2(150f, 50f),
        Radius = 2f,
        Calories = 0f,
        MaxCalories = 10f,
        RegrowthCaloriesPerSecond = 5f
    });

    simulation.Step();

    var resource = simulation.State.Resources[0];
    AssertEqual(BiomeKind.Rich, resource.HabitatBiomeKind, "Plant habitat biome should be captured at spawn");
    AssertEqual(BiomeKind.Rich, simulation.State.Biomes.GetKindAt(resource.Position), "Relocated plant should remain in its habitat biome");
    AssertClose(5f, resource.Calories, 0.000001, "Habitat-constrained relocation calories after regrowth");
}

static void DormantPlantsHoldWhenHabitatPlacementUnavailable()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 200f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 1f
        },
        seed: 212,
        systems:
        [
            new ResourceRegrowthSystem(
                relocateDepletedResources: true,
                resourceClusterStrength: 0f,
                plantLocalDispersalChance: 0f,
                plantRespawnDelaySecondsMin: 2f,
                plantRespawnDelaySecondsMax: 2f,
                plantRespawnCaloriesMin: 4f,
                plantRespawnCaloriesMax: 4f)
        ]);
    simulation.State.SetBiomes(BiomeMap.CreateFromCells(
        simulation.State.Bounds,
        cellSize: 100f,
        cellCountX: 2,
        cellCountY: 1,
        [BiomeKind.Rich, BiomeKind.Grassland]));
    simulation.State.SetObstacles(ObstacleMap.CreateFromCells(
        simulation.State.Bounds,
        cellSize: 100f,
        cellCountX: 2,
        cellCountY: 1,
        [true, false]));

    var initialPosition = new SimVector2(50f, 50f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        Position = initialPosition,
        Radius = 2f,
        Calories = 0f,
        MaxCalories = 10f,
        RegrowthCaloriesPerSecond = 5f
    });

    simulation.Step();
    AssertClose(0f, simulation.State.Resources[0].Calories, 0.000001, "Unavailable habitat plant should enter dormancy");
    AssertClose(2f, simulation.State.Resources[0].RespawnSecondsRemaining, 0.000001, "Unavailable habitat dormant timer");
    AssertEqual(0, simulation.State.Stats.PlantGlobalRelocationCount, "Unavailable habitat should not count as global relocation");

    simulation.Step();
    simulation.Step();

    var resource = simulation.State.Resources[0];
    AssertTrue(resource.Position == initialPosition, "Unavailable habitat plant should hold its last compatible position");
    AssertClose(0f, resource.Calories, 0.000001, "Unavailable habitat plant should remain dormant");
    AssertClose(1f, resource.RespawnSecondsRemaining, 0.000001, "Unavailable habitat plant should retry later");
    AssertClose(2f, resource.RespawnSecondsTotal, 0.000001, "Unavailable habitat plant should keep original dormancy duration");
    AssertEqual(0, simulation.State.Stats.PlantDormancyCompletedCount, "Unavailable habitat should not complete dormancy");
}

static void DepletedPlantsEnterDormancyBeforeRespawning()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 1f
        },
        seed: 22,
        systems:
        [
            new ResourceRegrowthSystem(
                relocateDepletedResources: true,
                plantRespawnDelaySecondsMin: 2f,
                plantRespawnDelaySecondsMax: 2f,
                plantRespawnCaloriesMin: 4f,
                plantRespawnCaloriesMax: 4f)
        ]);

    var initialPosition = new SimVector2(10f, 10f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        Position = initialPosition,
        Radius = 2f,
        Calories = 0f,
        MaxCalories = 10f,
        RegrowthCaloriesPerSecond = 5f
    });

    simulation.Step();

    var dormant = simulation.State.Resources[0];
    AssertTrue(dormant.Position != initialPosition, "Dormant plant should choose a new respawn position");
    AssertClose(0f, dormant.Calories, 0.000001, "Dormant plant should stay inedible");
    AssertClose(2f, dormant.RespawnSecondsRemaining, 0.000001, "Dormant plant timer");
    AssertClose(2f, dormant.RespawnSecondsTotal, 0.000001, "Dormant plant original timer");
    AssertEqual(1, simulation.State.Stats.PlantDepletionCount, "Dormant plant depletion count");
    AssertEqual(1, simulation.State.Stats.PlantGlobalRelocationCount, "Dormant plant global relocation count");
    AssertEqual(1, simulation.State.Stats.PlantDormancyStartedCount, "Dormancy started count");
    AssertClose(2f, simulation.State.Stats.AveragePlantDormancyScheduledSeconds, 0.000001, "Dormancy scheduled duration");

    simulation.Step();
    AssertClose(0f, simulation.State.Resources[0].Calories, 0.000001, "Plant should remain dormant before timer completes");
    AssertClose(1f, simulation.State.Resources[0].RespawnSecondsRemaining, 0.000001, "Dormant plant countdown");
    AssertEqual(0, simulation.State.Stats.PlantDormancyCompletedCount, "Dormancy should not complete early");

    simulation.Step();
    AssertClose(4f, simulation.State.Resources[0].Calories, 0.000001, "Plant should respawn with sampled calories");
    AssertClose(0f, simulation.State.Resources[0].RespawnSecondsRemaining, 0.000001, "Respawn timer clears");
    AssertClose(0f, simulation.State.Resources[0].RespawnSecondsTotal, 0.000001, "Respawn total timer clears");
    AssertEqual(1, simulation.State.Stats.PlantDormancyCompletedCount, "Dormancy completed count");
    AssertClose(2f, simulation.State.Stats.AveragePlantDormancyCompletedSeconds, 0.000001, "Dormancy completed duration");
}

static void DormantPlantsAreAbsentFromSpatialIndex()
{
    var index = new UniformSpatialIndex(10f);
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 1f
        },
        seed: 23,
        systems:
        [
            new ResourceRegrowthSystem(
                plantRespawnDelaySecondsMin: 2f,
                plantRespawnDelaySecondsMax: 2f,
                plantRespawnCaloriesMin: 6f,
                plantRespawnCaloriesMax: 6f),
            new SpatialIndexRebuildSystem(index)
        ]);

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        Position = new SimVector2(50f, 50f),
        Radius = 2f,
        Calories = 0f,
        MaxCalories = 10f,
        RegrowthCaloriesPerSecond = 5f
    });

    simulation.Step();
    AssertEqual(-1, index.FindNearestResourceWithCalories(simulation.State, new SimVector2(50f, 50f), 5f), "Dormant plant should not be indexed");

    simulation.Step();
    AssertEqual(-1, index.FindNearestResourceWithCalories(simulation.State, new SimVector2(50f, 50f), 5f), "Dormant plant should remain absent while timer runs");

    simulation.Step();
    AssertEqual(0, index.FindNearestResourceWithCalories(simulation.State, new SimVector2(50f, 50f), 5f), "Respawned plant should return to the spatial index");
}

static void MeatResourcesDecayAndDisappear()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 23,
        systems: [new ResourceRegrowthSystem()]);

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Meat,
        Position = new SimVector2(10f, 10f),
        Radius = 2f,
        Calories = 1f,
        MaxCalories = 1f,
        DecayCaloriesPerSecond = 2f
    });

    simulation.Step();

    AssertEqual(0, simulation.State.Resources.Count, "Decayed meat resource count");
}

static void FreshKillResourceCreditExpires()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 230,
        systems: [new ResourceRegrowthSystem()]);

    var attackerId = new EntityId(7);
    var preyId = new EntityId(8);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Meat,
        Position = new SimVector2(10f, 10f),
        Radius = 2f,
        Calories = 10f,
        MaxCalories = 10f,
        DecayCaloriesPerSecond = 0f,
        FreshKillAttackerId = attackerId,
        FreshKillPreyId = preyId,
        FreshKillSecondsRemaining = 0.5f
    });

    simulation.Step();

    var meat = simulation.State.Resources[0];
    AssertClose(0f, meat.FreshKillSecondsRemaining, 0.000001, "Fresh-kill timer expires");
    AssertEqual(default(EntityId), meat.FreshKillAttackerId, "Expired fresh-kill attacker clears");
    AssertEqual(default(EntityId), meat.FreshKillPreyId, "Expired fresh-kill prey clears");
    AssertClose(1f, meat.MeatAgeSeconds, 0.000001, "Meat age advances");
}

static void MeatFreshnessReducesDigestedEnergy()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 231,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex),
            new DigestionSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        EatCaloriesPerSecond = 10f,
        DigestionCaloriesPerSecond = 10f,
        DietaryAdaptation = 1f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Meat,
        Position = new SimVector2(22f, 20f),
        Radius = 2f,
        Calories = 30f,
        MaxCalories = 30f,
        DecayCaloriesPerSecond = 0f,
        MeatAgeSeconds = MeatQuality.StaleAgeSeconds
    });

    simulation.Step();

    var creature = simulation.State.Creatures[0];
    var expectedEnergy = 10f * MeatQuality.MinimumFreshness;
    AssertClose(10f + expectedEnergy, creature.Energy, 0.000001, "Stale meat releases reduced energy");
    AssertClose(10f, creature.LastCarcassCaloriesEaten, 0.000001, "Stale carcass raw calories eaten");
    AssertClose(0f, creature.LastFreshMeatCaloriesEaten, 0.000001, "No fresh meat recorded");
    AssertClose(10f, creature.LastStaleMeatCaloriesEaten, 0.000001, "Stale meat recorded");
    AssertClose(expectedEnergy, creature.LastMeatDigestedEnergy, 0.000001, "Stale meat digested energy");
    AssertClose(0f, creature.GutMeatCalories, 0.000001, "Meat gut emptied");
    AssertClose(0f, creature.GutMeatQualityCalories, 0.000001, "Meat quality gut emptied");
}

static void RottenMeatDamageScalesWithCarrionAdaptation()
{
    var freshSpecialist = RunRottenMeatDamageProbe(carrionAdaptation: 0f, meatAgeSeconds: 0f);
    var staleSpecialist = RunRottenMeatDamageProbe(carrionAdaptation: 0f, meatAgeSeconds: MeatQuality.StaleAgeSeconds);
    var carrionSpecialist = RunRottenMeatDamageProbe(carrionAdaptation: 1f, meatAgeSeconds: MeatQuality.StaleAgeSeconds);

    AssertClose(0f, freshSpecialist.LastRottenMeatDamage, 0.000001, "Fresh meat should not damage health");
    AssertClose(0.04f, staleSpecialist.LastRottenMeatDamage, 0.000001, "Fully stale meat damage");
    AssertClose(0.004f, carrionSpecialist.LastRottenMeatDamage, 0.000001, "Carrion adaptation protects against most stale meat damage");
    AssertTrue(
        carrionSpecialist.Health > staleSpecialist.Health,
        "Carrion adaptation should preserve more health from rotten meat");
}

static CreatureState RunRottenMeatDamageProbe(float carrionAdaptation, float meatAgeSeconds)
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 232,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex),
            new DigestionSystem(rottenMeatDamagePerRawKcal: 0.004f)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        EatCaloriesPerSecond = 10f,
        DigestionCaloriesPerSecond = 10f,
        DietaryAdaptation = 1f,
        CarrionAdaptation = carrionAdaptation,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Meat,
        Position = new SimVector2(22f, 20f),
        Radius = 2f,
        Calories = 30f,
        MaxCalories = 30f,
        DecayCaloriesPerSecond = 0f,
        MeatAgeSeconds = meatAgeSeconds
    });

    simulation.Step();
    return simulation.State.Creatures[0];
}

static void RottenMeatHealthDeathsAreCounted()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 233,
        systems:
        [
            new DigestionSystem(rottenMeatDamagePerRawKcal: 0.004f),
            new DeathSystem(meatCaloriesPerBodyRadius: 4f, meatEnergyFraction: 0.35f, meatDecayCaloriesPerSecond: 0.03f)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        DietaryAdaptation = 1f,
        DigestionCaloriesPerSecond = 10f,
        MaturityAgeSeconds = 0f
    });
    var creatureId = simulation.State.SpawnCreature(
        genomeId,
        new SimVector2(20f, 20f),
        energy: 10f,
        health: 0.02f);
    var creature = simulation.State.Creatures[0];
    creature.GutMeatCalories = 10f;
    creature.GutMeatQualityCalories = 10f * MeatQuality.MinimumFreshness;
    creature.LastDamagingCreatureId = new EntityId(999);
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    AssertEqual(0, simulation.State.Creatures.Count, "Rotten meat victim removed");
    AssertEqual(1, simulation.State.Stats.CreatureDeathCount, "Rotten death total count");
    AssertEqual(1, simulation.State.Stats.RottenMeatDeathCount, "Rotten death count");
    AssertEqual(0, simulation.State.Stats.InjuryDeathCount, "Rotten death should not count as injury");
    AssertTrue(simulation.State.TryGetLineageRecord(creatureId, out var record), "Rotten death lineage lookup");
    AssertEqual(CreatureDeathReason.RottenMeat, record.DeathReason, "Rotten death reason");
    AssertEqual(1, simulation.State.Resources.Count, "Rotten death meat resource");
    AssertEqual(default(EntityId), simulation.State.Resources[0].FreshKillAttackerId, "Rotten death should not credit a stale attacker");
}

static void EatingTransfersCalories()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 2,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex),
            new DigestionSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        EatCaloriesPerSecond = 12f,
        DigestionCaloriesPerSecond = 12f,
        DietaryAdaptation = 0f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);
    var hungryCreature = simulation.State.Creatures[0];
    hungryCreature.SecondsSinceLastMeal = 6f;
    hungryCreature.DistanceSinceLastMeal = 42f;
    simulation.State.Creatures[0] = hungryCreature;
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(22f, 20f),
        Radius = 2f,
        Calories = 30f,
        MaxCalories = 30f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();

    AssertClose(22f, simulation.State.Creatures[0].Energy, 0.000001, "Creature energy after eating");
    AssertTrue(simulation.State.Creatures[0].IsTouchingFood, "Creature should be touching food");
    AssertEqual(simulation.State.Resources[0].Id, simulation.State.Creatures[0].FoodContactResourceId, "Touched resource id");
    AssertClose(0f, simulation.State.Creatures[0].FoodContactEdgeDistance, 0.000001, "Touched resource edge distance");
    AssertClose(30f, simulation.State.Creatures[0].FoodContactCalories, 0.000001, "Touched resource calories before eating");
    AssertClose(12f, simulation.State.Creatures[0].LastCaloriesEaten, 0.000001, "Calories eaten last tick");
    AssertClose(12f, simulation.State.Creatures[0].LastPlantCaloriesEaten, 0.000001, "Plant calories eaten last tick");
    AssertClose(0f, simulation.State.Creatures[0].LastCarcassCaloriesEaten, 0.000001, "Carcass calories eaten last tick");
    AssertClose(0f, simulation.State.Creatures[0].LastEggCaloriesEaten, 0.000001, "Egg calories eaten last tick");
    AssertClose(12f, simulation.State.Creatures[0].LastCaloriesDigested, 0.000001, "Calories digested last tick");
    AssertClose(12f, simulation.State.Creatures[0].LastPlantDigestedEnergy, 0.000001, "Plant energy digested last tick");
    AssertClose(0f, simulation.State.Creatures[0].LastMeatDigestedEnergy, 0.000001, "Meat energy digested last tick");
    AssertClose(0f, simulation.State.Creatures[0].GutPlantCalories, 0.000001, "Gut plant calories after digestion");
    AssertClose(0f, simulation.State.Creatures[0].SecondsSinceLastMeal, 0.000001, "Meal timer resets after eating");
    AssertClose(0f, simulation.State.Creatures[0].DistanceSinceLastMeal, 0.000001, "Meal distance resets after eating");
    AssertClose(18f, simulation.State.Resources[0].Calories, 0.000001, "Resource calories after eating");
}

static void EatingFillsGutBeforeDigestion()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 21,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        EatCaloriesPerSecond = 12f,
        GutCapacityCalories = 20f,
        DietaryAdaptation = 0f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(22f, 20f),
        Radius = 2f,
        Calories = 30f,
        MaxCalories = 30f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();

    AssertClose(10f, simulation.State.Creatures[0].Energy, 0.000001, "Energy should not change before digestion");
    AssertClose(12f, simulation.State.Creatures[0].GutPlantCalories, 0.000001, "Plant gut calories after eating");
    AssertClose(0f, simulation.State.Creatures[0].GutMeatCalories, 0.000001, "Meat gut calories after eating");
    AssertClose(12f, simulation.State.Creatures[0].LastCaloriesEaten, 0.000001, "Raw calories eaten last tick");
    AssertClose(12f, simulation.State.Creatures[0].LastPlantCaloriesEaten, 0.000001, "Plant source calories eaten last tick");
    AssertClose(0f, simulation.State.Creatures[0].LastCaloriesDigested, 0.000001, "No calories digested without digestion system");
    AssertClose(18f, simulation.State.Resources[0].Calories, 0.000001, "Resource calories after eating into gut");
}

static void GutCapacityLimitsAdditionalEating()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 23,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        EatCaloriesPerSecond = 20f,
        GutCapacityCalories = 10f,
        DietaryAdaptation = 0f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);
    var creature = simulation.State.Creatures[0];
    creature.GutMeatCalories = 8f;
    simulation.State.Creatures[0] = creature;
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(22f, 20f),
        Radius = 2f,
        Calories = 30f,
        MaxCalories = 30f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();

    AssertClose(2f, simulation.State.Creatures[0].LastCaloriesEaten, 0.000001, "Only free gut capacity can be eaten");
    AssertClose(2f, simulation.State.Creatures[0].GutPlantCalories, 0.000001, "Plant gut calories are capacity-limited");
    AssertClose(8f, simulation.State.Creatures[0].GutMeatCalories, 0.000001, "Existing meat gut calories remain");
    AssertClose(28f, simulation.State.Resources[0].Calories, 0.000001, "Resource loses only capacity-limited calories");
}

static void PlantTypeControlsEatingTransferRate()
{
    AssertClose(15f, RunPlantTypeEatingProbe(PlantResourceKind.Tender), 0.000001, "Tender plant eating transfer");
    AssertClose(10f, RunPlantTypeEatingProbe(PlantResourceKind.Generic), 0.000001, "Generic plant eating transfer");
    AssertClose(6.5f, RunPlantTypeEatingProbe(PlantResourceKind.Rich), 0.000001, "Rich plant eating transfer");
    AssertClose(4.5f, RunPlantTypeEatingProbe(PlantResourceKind.Tough), 0.000001, "Tough plant eating transfer");
}

static float RunPlantTypeEatingProbe(PlantResourceKind plantKind)
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 231,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        EatCaloriesPerSecond = 10f,
        GutCapacityCalories = 30f,
        DietaryAdaptation = 0f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        PlantKind = plantKind,
        Position = new SimVector2(22f, 20f),
        Radius = 2f,
        Calories = 30f,
        MaxCalories = 30f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();

    return simulation.State.Creatures[0].LastPlantCaloriesEaten;
}

static void PlantTypeControlsDigestionPayoff()
{
    AssertClose(10f, RunPlantTypeDigestionProbe(PlantResourceKind.Generic), 0.000001, "Generic plant digestion");
    AssertClose(8.5f, RunPlantTypeDigestionProbe(PlantResourceKind.Tender), 0.000001, "Tender plant digestion");
    AssertClose(10.5f, RunPlantTypeDigestionProbe(PlantResourceKind.Rich), 0.000001, "Rich plant digestion");
    AssertClose(5f, RunPlantTypeDigestionProbe(PlantResourceKind.Tough), 0.000001, "Tough plant digestion");
}

static void PlantAdaptationControlsDigestionPayoff()
{
    AssertClose(
        13.175f,
        RunPlantTypeDigestionProbe(PlantResourceKind.Tender, tenderPlantAdaptation: 1f),
        0.000001,
        "Tender adaptation improves tender plant digestion");
    AssertClose(
        21f,
        RunPlantTypeDigestionProbe(PlantResourceKind.Rich, richPlantAdaptation: 1f),
        0.00001,
        "Rich adaptation improves rich plant digestion");
    AssertClose(
        10f,
        RunPlantTypeDigestionProbe(PlantResourceKind.Tough, toughPlantAdaptation: 1f),
        0.000001,
        "Tough adaptation improves tough plant digestion");
}

static void PlantAdaptationPenalizesMismatchedPlantTypes()
{
    AssertClose(
        9.2f,
        RunPlantTypeDigestionProbe(PlantResourceKind.Generic, richPlantAdaptation: 1f),
        0.000001,
        "Rich adaptation mildly reduces generic plant digestion");
    AssertClose(
        6.375f,
        RunPlantTypeDigestionProbe(PlantResourceKind.Tender, richPlantAdaptation: 1f),
        0.000001,
        "Rich adaptation reduces tender plant digestion");
    AssertClose(
        3.75f,
        RunPlantTypeDigestionProbe(PlantResourceKind.Tough, richPlantAdaptation: 1f),
        0.000001,
        "Rich adaptation reduces tough plant digestion");
}

static float RunPlantTypeDigestionProbe(
    PlantResourceKind plantKind,
    float tenderPlantAdaptation = 0f,
    float richPlantAdaptation = 0f,
    float toughPlantAdaptation = 0f)
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 232,
        systems:
        [
            new DigestionSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        DigestionCaloriesPerSecond = 10f,
        GutCapacityCalories = 30f,
        DietaryAdaptation = 0f,
        TenderPlantAdaptation = tenderPlantAdaptation,
        RichPlantAdaptation = richPlantAdaptation,
        ToughPlantAdaptation = toughPlantAdaptation,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);
    var creature = simulation.State.Creatures[0];
    creature.GutPlantCalories = 10f;
    switch (plantKind)
    {
        case PlantResourceKind.Tender:
            creature.GutTenderPlantCalories = 10f;
            break;
        case PlantResourceKind.Rich:
            creature.GutRichPlantCalories = 10f;
            break;
        case PlantResourceKind.Tough:
            creature.GutToughPlantCalories = 10f;
            break;
    }

    simulation.State.Creatures[0] = creature;

    simulation.Step();

    return simulation.State.Creatures[0].LastPlantDigestedEnergy;
}

static void EatingRequiresBodyContact()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 22,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex),
            new DigestionSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        EatCaloriesPerSecond = 12f,
        DigestionCaloriesPerSecond = 12f,
        DietaryAdaptation = 0f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(25.5f, 20f),
        Radius = 2f,
        Calories = 30f,
        MaxCalories = 30f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();

    AssertClose(10f, simulation.State.Creatures[0].Energy, 0.000001, "Energy outside body contact");
    AssertTrue(!simulation.State.Creatures[0].IsTouchingFood, "Creature should not be touching food");
    AssertEqual(default(EntityId), simulation.State.Creatures[0].FoodContactResourceId, "No touched resource id");
    AssertClose(0f, simulation.State.Creatures[0].LastCaloriesEaten, 0.000001, "Calories eaten outside body contact");
    AssertClose(30f, simulation.State.Resources[0].Calories, 0.000001, "Resource outside body contact");

    var creature = simulation.State.Creatures[0];
    creature.Position = new SimVector2(20.5f, 20f);
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    AssertClose(22f, simulation.State.Creatures[0].Energy, 0.000001, "Energy inside body contact");
    AssertTrue(simulation.State.Creatures[0].IsTouchingFood, "Creature should be touching food");
    AssertEqual(simulation.State.Resources[0].Id, simulation.State.Creatures[0].FoodContactResourceId, "Touched resource id");
    AssertClose(3f, simulation.State.Creatures[0].FoodContactEdgeDistance, 0.000001, "Touched resource edge distance");
    AssertClose(30f, simulation.State.Creatures[0].FoodContactCalories, 0.000001, "Touched resource calories before eating");
    AssertClose(12f, simulation.State.Creatures[0].LastCaloriesEaten, 0.000001, "Calories eaten inside body contact");
    AssertClose(12f, simulation.State.Creatures[0].LastCaloriesDigested, 0.000001, "Calories digested inside body contact");
    AssertClose(18f, simulation.State.Resources[0].Calories, 0.000001, "Resource inside body contact");
}

static void DietaryAdaptationControlsDigestedCalories()
{
    AssertClose(10f, RunEatingProbe(ResourceKind.Plant, dietaryAdaptation: 0f), 0.000001, "Plant specialist plant calories");
    AssertClose(2f, RunEatingProbe(ResourceKind.Meat, dietaryAdaptation: 0f), 0.000001, "Plant specialist meat calories");
    AssertClose(2f, RunEatingProbe(ResourceKind.Plant, dietaryAdaptation: 1f), 0.000001, "Meat specialist plant calories");
    AssertClose(10f, RunEatingProbe(ResourceKind.Meat, dietaryAdaptation: 1f), 0.000001, "Meat specialist meat calories");
    AssertClose(6f, RunEatingProbe(ResourceKind.Plant, dietaryAdaptation: 0.5f), 0.000001, "Omnivore plant calories");
    AssertClose(6f, RunEatingProbe(ResourceKind.Meat, dietaryAdaptation: 0.5f), 0.000001, "Omnivore meat calories");
}

static float RunEatingProbe(ResourceKind resourceKind, float dietaryAdaptation)
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 24,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex),
            new DigestionSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        EatCaloriesPerSecond = 10f,
        DigestionCaloriesPerSecond = 10f,
        DietaryAdaptation = dietaryAdaptation,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 1f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = resourceKind,
        Position = new SimVector2(22f, 20f),
        Radius = 2f,
        Calories = 30f,
        MaxCalories = 30f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();

    AssertClose(20f, simulation.State.Resources[0].Calories, 0.000001, "Raw resource calories removed");
    if (resourceKind == ResourceKind.Meat)
    {
        AssertClose(10f, simulation.State.Creatures[0].LastCarcassCaloriesEaten, 0.000001, "Carcass source calories eaten");
        AssertClose(0f, simulation.State.Creatures[0].LastPlantCaloriesEaten, 0.000001, "No plant source calories eaten");
    }
    else
    {
        AssertClose(10f, simulation.State.Creatures[0].LastPlantCaloriesEaten, 0.000001, "Plant source calories eaten");
        AssertClose(0f, simulation.State.Creatures[0].LastCarcassCaloriesEaten, 0.000001, "No carcass source calories eaten");
    }

    return simulation.State.Creatures[0].Energy - 1f;
}

static void CarrionAdaptationTradesFreshAndStaleMeatDigestion()
{
    var freshSpecialist = CreatureGenome.Baseline with
    {
        DietaryAdaptation = 1f,
        CarrionAdaptation = 0f
    };
    var carrionSpecialist = freshSpecialist with { CarrionAdaptation = 1f };

    AssertClose(1f, CreatureDigestion.FreshMeatEnergyEfficiency(freshSpecialist), 0.000001, "Fresh specialist fresh meat");
    AssertClose(MeatQuality.MinimumFreshness, CreatureDigestion.StaleMeatEnergyEfficiency(freshSpecialist), 0.000001, "Fresh specialist stale meat");
    AssertClose(0.75f, CreatureDigestion.FreshMeatEnergyEfficiency(carrionSpecialist), 0.000001, "Carrion specialist fresh meat penalty");
    AssertClose(0.9025f, CreatureDigestion.StaleMeatEnergyEfficiency(carrionSpecialist), 0.000001, "Carrion specialist stale meat recovery");
    AssertTrue(
        CreatureDigestion.FreshMeatEnergyEfficiency(carrionSpecialist) < CreatureDigestion.FreshMeatEnergyEfficiency(freshSpecialist),
        "Carrion specialization should cost fresh meat efficiency");
    AssertTrue(
        CreatureDigestion.StaleMeatEnergyEfficiency(carrionSpecialist) > CreatureDigestion.StaleMeatEnergyEfficiency(freshSpecialist),
        "Carrion specialization should improve stale meat efficiency");
}

static void EatingTransfersEggEnergyAsMeatNutrition()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 26,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex),
            new DigestionSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        EatCaloriesPerSecond = 10f,
        DigestionCaloriesPerSecond = 10f,
        DietaryAdaptation = 1f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);
    var predatorBeforeEating = simulation.State.Creatures[0];
    predatorBeforeEating.DistanceSinceLastMeal = 18f;
    simulation.State.Creatures[0] = predatorBeforeEating;
    var parentId = simulation.State.SpawnCreature(genomeId, new SimVector2(80f, 80f), energy: 10f);
    var eggId = simulation.State.SpawnEgg(
        genomeId: genomeId,
        brainId: -1,
        parentId: parentId,
        position: new SimVector2(25f, 20f),
        energy: 20f,
        incubationSeconds: 100f,
        generation: 1);

    simulation.Step();

    var predator = simulation.State.Creatures[0];
    AssertClose(20f, predator.Energy, 0.000001, "Predator energy after eating egg");
    AssertTrue(predator.IsTouchingFood, "Predator should be touching egg food");
    AssertEqual(FoodContactKind.Egg, predator.FoodContactKind, "Touched food kind");
    AssertEqual(eggId, predator.FoodContactResourceId, "Touched egg id");
    AssertClose(20f, predator.FoodContactCalories, 0.000001, "Touched egg energy before eating");
    AssertClose(10f, predator.LastCaloriesEaten, 0.000001, "Egg calories eaten last tick");
    AssertClose(0f, predator.LastPlantCaloriesEaten, 0.000001, "Plant calories eaten while eating egg");
    AssertClose(0f, predator.LastCarcassCaloriesEaten, 0.000001, "Carcass calories eaten while eating egg");
    AssertClose(10f, predator.LastEggCaloriesEaten, 0.000001, "Egg source calories eaten last tick");
    AssertClose(10f, predator.LastCaloriesDigested, 0.000001, "Egg calories digested last tick");
    AssertClose(0f, predator.LastPlantDigestedEnergy, 0.000001, "Plant energy digested while eating egg");
    AssertClose(10f, predator.LastMeatDigestedEnergy, 0.000001, "Meat energy digested from egg");
    AssertClose(0f, predator.SecondsSinceLastMeal, 0.000001, "Meal timer resets after egg");
    AssertClose(0f, predator.DistanceSinceLastMeal, 0.000001, "Meal distance resets after egg");
    AssertClose(10f, simulation.State.Eggs[0].Energy, 0.000001, "Egg energy after partial predation");
    AssertEqual(EggDeathReason.Unknown, simulation.State.Eggs[0].PendingDeathReason, "Partially eaten egg remains viable");
}

static void EggPredationDeathsAreCounted()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 27,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex),
            new DigestionSystem(),
            new EggSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        EatCaloriesPerSecond = 20f,
        DietaryAdaptation = 1f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);
    var parentId = simulation.State.SpawnCreature(genomeId, new SimVector2(80f, 80f), energy: 10f);
    simulation.State.SpawnEgg(
        genomeId: genomeId,
        brainId: -1,
        parentId: parentId,
        position: new SimVector2(25f, 20f),
        energy: 5f,
        incubationSeconds: 100f,
        generation: 1);

    simulation.Step();

    AssertEqual(0, simulation.State.Eggs.Count, "Eaten egg should be removed");
    AssertEqual(1, simulation.State.Stats.EggDeathCount, "Eaten egg should count as egg death");
    AssertEqual(1, simulation.State.Stats.EggPredationDeathCount, "Eaten egg should count as predation death");
    AssertClose(15f, simulation.State.Creatures[0].Energy, 0.000001, "Predator energy after finishing egg");
}

static void StarvingCreaturesAreRemoved()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 3,
        systems:
        [
            new MetabolismSystem(),
            new DeathSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BasalEnergyPerSecond = 10f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 5f);

    simulation.Step();

    AssertEqual(0, simulation.State.Creatures.Count, "Living creature count");
}

static void DeadCreaturesLeaveMeatResources()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 25,
        systems: [new DeathSystem(meatCaloriesPerBodyRadius: 4f, meatEnergyFraction: 0.5f, meatDecayCaloriesPerSecond: 0.25f)]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 4f,
        MaturityAgeSeconds = 0f
    });
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 6f);
    var creature = simulation.State.Creatures[0];
    creature.Health = 0f;
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    AssertEqual(0, simulation.State.Creatures.Count, "Living creature count");
    AssertEqual(1, simulation.State.Resources.Count, "Meat resource count");
    var meat = simulation.State.Resources[0];
    AssertEqual(ResourceKind.Meat, meat.Kind, "Meat resource kind");
    AssertClose(20f, meat.Position.X, 0.000001, "Meat x");
    AssertClose(20f, meat.Position.Y, 0.000001, "Meat y");
    AssertClose(19f, meat.Calories, 0.000001, "Meat calories");
    AssertClose(19f, meat.MaxCalories, 0.000001, "Meat max calories");
    AssertClose(0f, meat.RegrowthCaloriesPerSecond, 0.000001, "Meat regrowth");
    AssertClose(0.25f, meat.DecayCaloriesPerSecond, 0.000001, "Meat decay");
    AssertEqual(default(EntityId), meat.FreshKillAttackerId, "Starvation meat should not credit an attacker");
    AssertEqual(default(EntityId), meat.FreshKillPreyId, "Starvation meat should not credit fresh-kill prey");
    AssertClose(0f, meat.FreshKillSecondsRemaining, 0.000001, "Starvation meat fresh-kill timer");
}

static void MetabolismChargesBodySizeUpkeep()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 31,
        systems: [new MetabolismSystem(bodyRadiusEnergyCostPerSecond: 0.5f)]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 4f,
        BasalEnergyPerSecond = 1f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);

    simulation.Step();

    AssertClose(7f, simulation.State.Creatures[0].Energy, 0.000001, "Energy after basal and body-size upkeep");
}

static void MetabolismChargesTraitUpkeep()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 131,
        systems:
        [
            new MetabolismSystem(
                maxSpeedEnergyCostPerSecond: 0.1f,
                turnRateEnergyCostPerSecond: 0.2f,
                senseRadiusEnergyCostPerSecond: 0.01f,
                eatRateEnergyCostPerSecond: 0.3f,
                gutCapacityEnergyCostPerSecond: 0.01f,
                digestionRateEnergyCostPerSecond: 0.02f,
                biteStrengthEnergyCostPerSecond: 0.5f,
                damageResistanceEnergyCostPerSecond: 0.25f)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BasalEnergyPerSecond = 1f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 20f);

    simulation.Step();

    AssertClose(8.525f, simulation.State.Creatures[0].Energy, 0.000001, "Energy after trait upkeep");
}

static void MetabolismChargesPlantSpecializationUpkeep()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 133,
        systems:
        [
            new MetabolismSystem(plantSpecializationEnergyCostPerSecond: 1f)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BasalEnergyPerSecond = 1f,
        TenderPlantAdaptation = 0.5f,
        RichPlantAdaptation = 0.25f,
        ToughPlantAdaptation = 1f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);

    simulation.Step();

    AssertClose(7.6875f, simulation.State.Creatures[0].Energy, 0.000001, "Energy after plant specialization upkeep");
}

static void MetabolismBasalCostFollowsBiomeMultiplier()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 132,
        systems: [new MetabolismSystem(biomeBasalCostProfile: new BiomePressureProfile(1f, 1f, 1.5f, 1f))]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BasalEnergyPerSecond = 2f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);

    simulation.Step();

    AssertClose(7f, simulation.State.Creatures[0].Energy, 0.000001, "Biome basal energy");
}

static void FatStoragePreservesEggReservePriority()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 1004,
        systems:
        [
            new ReproductionSystem(),
            new FatStorageSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        ReproductionEnergyThreshold = 100f,
        OffspringEnergyInvestment = 30f,
        EggProductionEnergyPerSecond = 10f,
        MaturityAgeSeconds = 0f,
        ReproductionCooldownSeconds = 0f,
        FatStorageCapacityCalories = 50f,
        FatStorageEfficiency = 1f,
        MutationStrength = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 140f);

    simulation.Step();

    var creature = simulation.State.Creatures[0];
    AssertEqual(0, simulation.State.Eggs.Count, "Fat storage should not lay an egg early");
    AssertClose(10f, creature.ReproductiveEnergy, 0.000001, "Egg reserve keeps first claim on surplus energy");
    AssertClose(5f, creature.FatCalories, 0.000001, "Fat stores only energy remaining above the deposit target");
    AssertClose(125f, creature.Energy, 0.000001, "Parent energy lands on fat deposit target after egg reserve transfer");
    AssertClose(5f, creature.LastFatStoredCalories, 0.000001, "Fat storage telemetry records stored reserve");
}

static void FatStorageReleasesBeforeStarvationDeath()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 1005,
        systems:
        [
            new FatStorageSystem(),
            new DeathSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f,
        FatStorageCapacityCalories = 50f,
        FatStorageEfficiency = 1f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 1f);
    var creature = simulation.State.Creatures[0];
    creature.Energy = -5f;
    creature.FatCalories = 40f;
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    AssertEqual(1, simulation.State.Creatures.Count, "Fat reserve should rescue a starving creature before death removal");
    creature = simulation.State.Creatures[0];
    AssertClose(17.5f, creature.Energy, 0.000001, "Released fat restores usable energy");
    AssertClose(17.5f, creature.FatCalories, 0.000001, "Released fat is removed from reserve");
    AssertClose(22.5f, creature.LastFatReleasedCalories, 0.000001, "Fat release telemetry records usable energy");
}

static void ReproductionBuildsEggReserveBeforeLaying()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 104,
        systems: [new ReproductionSystem()]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        ReproductionEnergyThreshold = 50f,
        OffspringEnergyInvestment = 20f,
        EggProductionEnergyPerSecond = 5f,
        MaturityAgeSeconds = 0f,
        ReproductionCooldownSeconds = 0f,
        MutationStrength = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 80f);

    simulation.Step();

    AssertEqual(0, simulation.State.Eggs.Count, "Egg count while reserve is building");
    AssertClose(75f, simulation.State.Creatures[0].Energy, 0.000001, "Parent energy after reserve transfer");
    AssertClose(5f, simulation.State.Creatures[0].ReproductiveEnergy, 0.000001, "Egg reserve after one transfer");

    simulation.Step();
    simulation.Step();
    simulation.Step();

    AssertEqual(1, simulation.State.Eggs.Count, "Egg count after reserve completes");
    AssertClose(60f, simulation.State.Creatures[0].Energy, 0.000001, "Parent energy after egg is laid");
    AssertClose(0f, simulation.State.Creatures[0].ReproductiveEnergy, 0.000001, "Egg reserve after laying");
    AssertClose(20f, simulation.State.Eggs[0].Energy, 0.000001, "Egg energy from reserve");
}

static void ReproductionFertilityDeclinesWithAge()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 501,
        systems:
        [
            new ReproductionSystem(
                reproductivePrimeAgeSeconds: 10f,
                reproductiveSenescenceAgeSeconds: 20f,
                senescentFertilityMultiplier: 0.25f,
                crowdingFertilityPenalty: 0f)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        ReproductionEnergyThreshold = 50f,
        OffspringEnergyInvestment = 20f,
        EggProductionEnergyPerSecond = 8f,
        MaturityAgeSeconds = 0f,
        ReproductionCooldownSeconds = 0f,
        MutationStrength = 0.01f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 100f);
    var oldAdult = simulation.State.Creatures[0];
    oldAdult.AgeSeconds = 25f;
    simulation.State.Creatures[0] = oldAdult;

    simulation.Step();

    AssertClose(98f, simulation.State.Creatures[0].Energy, 0.000001, "Old adult energy after reduced transfer");
    AssertClose(2f, simulation.State.Creatures[0].ReproductiveEnergy, 0.000001, "Old adult egg reserve transfer");
}

static void ReproductionFertilityDeclinesWithCrowding()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 502,
        systems:
        [
            new ReproductionSystem(
                reproductivePrimeAgeSeconds: 0f,
                reproductiveSenescenceAgeSeconds: 100f,
                senescentFertilityMultiplier: 1f,
                crowdingFertilityPenalty: 0.5f)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        ReproductionEnergyThreshold = 50f,
        OffspringEnergyInvestment = 20f,
        EggProductionEnergyPerSecond = 8f,
        MaturityAgeSeconds = 0f,
        ReproductionCooldownSeconds = 0f,
        MutationStrength = 0.01f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 100f);
    var crowdedAdult = simulation.State.Creatures[0];
    crowdedAdult.Senses = new CreatureSenseState { VisibleCreatureDensity = 1f };
    simulation.State.Creatures[0] = crowdedAdult;

    simulation.Step();

    AssertClose(96f, simulation.State.Creatures[0].Energy, 0.000001, "Crowded adult energy after reduced transfer");
    AssertClose(4f, simulation.State.Creatures[0].ReproductiveEnergy, 0.000001, "Crowded adult egg reserve transfer");
}

static void ReproductionCreatesOffspring()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 4,
        systems: [new ReproductionSystem()]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        ReproductionEnergyThreshold = 50f,
        OffspringEnergyInvestment = 20f,
        EggProductionEnergyPerSecond = 20f,
        MaturityAgeSeconds = 30f,
        ReproductionCooldownSeconds = 5f,
        MutationStrength = 0.01f
    });

    var parentId = simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 80f);
    var parent = simulation.State.Creatures[0];
    parent.AgeSeconds = 30f;
    simulation.State.Creatures[0] = parent;

    simulation.Step();

    AssertEqual(1, simulation.State.Creatures.Count, "Creature count after laying egg");
    AssertEqual(1, simulation.State.Eggs.Count, "Egg count after reproduction");
    AssertEqual(2, simulation.State.Genomes.Count, "Genome count after reproduction");
    AssertClose(60f, simulation.State.Creatures[0].Energy, 0.000001, "Parent energy after reproduction");
    AssertClose(0f, simulation.State.Creatures[0].ReproductiveEnergy, 0.000001, "Parent egg reserve after reproduction");
    AssertClose(20f, simulation.State.Eggs[0].Energy, 0.000001, "Egg energy after reproduction");
    AssertClose(20f / CreatureGenome.Baseline.OffspringEnergyInvestment, simulation.State.Eggs[0].InvestmentRatio, 0.000001, "Egg investment ratio");
    AssertClose(MathF.Sqrt(20f / CreatureGenome.Baseline.OffspringEnergyInvestment), simulation.State.Eggs[0].MaxHealth, 0.000001, "Egg max health");
    AssertClose(simulation.State.Eggs[0].MaxHealth, simulation.State.Eggs[0].Health, 0.000001, "Egg health");
    AssertEqual(parentId, simulation.State.Eggs[0].ParentId, "Egg parent id");
    AssertEqual(1, simulation.State.Eggs[0].Generation, "Egg generation");
    AssertEqual(1, simulation.State.Stats.EggLaidCount, "Egg laid count");
    AssertEqual(1, simulation.State.Stats.CreatureBirthCount, "Creature births should not include unhatched eggs");
}

static void EggsHatchIntoOffspring()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 404,
        systems: [new EggSystem()]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateZero());
    var parentId = simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);
    simulation.State.SpawnEgg(
        genomeId,
        brainId,
        parentId,
        new SimVector2(30f, 20f),
        energy: 12f,
        incubationSeconds: 1f,
        generation: 1);

    simulation.Step();

    AssertEqual(2, simulation.State.Creatures.Count, "Creature count after hatching");
    AssertEqual(0, simulation.State.Eggs.Count, "Egg count after hatching");
    AssertEqual(1, simulation.State.Stats.EggLaidCount, "Egg laid count");
    AssertEqual(1, simulation.State.Stats.EggHatchedCount, "Egg hatch count");
    AssertEqual(2, simulation.State.Stats.CreatureBirthCount, "Creature birth count after hatch");
    AssertEqual(parentId, simulation.State.Creatures[1].ParentId, "Hatchling parent id");
    AssertEqual(1, simulation.State.Creatures[1].Generation, "Hatchling generation");
    AssertClose(12f, simulation.State.Creatures[1].Energy, 0.000001, "Hatchling energy");
    AssertClose(12f / CreatureGenome.Baseline.OffspringEnergyInvestment, simulation.State.Creatures[1].BirthInvestmentRatio, 0.000001, "Hatchling investment ratio");
    AssertClose(MathF.Sqrt(12f / CreatureGenome.Baseline.OffspringEnergyInvestment), simulation.State.Creatures[1].Health, 0.000001, "Hatchling health");
}

static void EggEnvironmentalDamageFollowsVoidAndBiomePressure()
{
    var scenario = new SimulationScenario
    {
        Seed = 18,
        PipelineKind = SimulationPipelineKind.SimpleForaging,
        BiomeMapKind = BiomeMapKind.GeneratedNoise,
        WorldWidth = 1_500f,
        WorldHeight = 1_000f,
        BiomeCellSize = 250f,
        ResourceVoidBorderWidth = 100f,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f,
        EggEnvironmentalDamagePerSecond = 0.2f
    };
    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var state = simulation.State;
    var genomeId = 0;
    var parentId = state.SpawnCreature(genomeId, new SimVector2(500f, 500f), energy: 50f);
    var voidPosition = new SimVector2(50f, 500f);
    var barrenPosition = FindBiomeCellCenter(state.Biomes, BiomeKind.Barren);
    var sparsePosition = FindBiomeCellCenter(state.Biomes, BiomeKind.Sparse);
    var richPosition = FindBiomeCellCenter(state.Biomes, BiomeKind.Rich);

    state.SpawnEgg(genomeId, -1, parentId, voidPosition, energy: 28f, incubationSeconds: 100f, generation: 1);
    state.SpawnEgg(genomeId, -1, parentId, barrenPosition, energy: 28f, incubationSeconds: 100f, generation: 1);
    state.SpawnEgg(genomeId, -1, parentId, sparsePosition, energy: 28f, incubationSeconds: 100f, generation: 1);
    state.SpawnEgg(genomeId, -1, parentId, richPosition, energy: 28f, incubationSeconds: 100f, generation: 1);

    new EggEnvironmentalDamageSystem(0.2f).Update(state, 1f);

    AssertClose(0.8f, state.Eggs[0].Health, 0.000001, "Void egg health");
    AssertClose(0.9f, state.Eggs[1].Health, 0.000001, "Barren egg health");
    AssertClose(0.96f, state.Eggs[2].Health, 0.000001, "Sparse egg health");
    AssertClose(1f, state.Eggs[3].Health, 0.000001, "Rich egg health");

    var lethalSimulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var lethalState = lethalSimulation.State;
    var lethalParentId = lethalState.SpawnCreature(genomeId, new SimVector2(500f, 500f), energy: 50f);
    lethalState.SpawnEgg(genomeId, -1, lethalParentId, voidPosition, energy: 28f, incubationSeconds: 100f, generation: 1);

    new EggEnvironmentalDamageSystem(2f).Update(lethalState, 1f);
    new EggSystem().Update(lethalState, 1f);

    AssertEqual(0, lethalState.Eggs.Count, "Lethally damaged egg should be removed");
    AssertEqual(1, lethalState.Stats.EggDeathCount, "Lethally damaged egg should count as an egg death");
    AssertEqual(0, lethalState.Stats.EggPredationDeathCount, "Lethally damaged egg should not count as predation");
}

static void JuvenileCreaturesCannotReproduceBeforeMaturity()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 44,
        systems: [new ReproductionSystem()]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        ReproductionEnergyThreshold = 50f,
        OffspringEnergyInvestment = 20f,
        EggProductionEnergyPerSecond = 20f,
        MaturityAgeSeconds = 10f,
        ReproductionCooldownSeconds = 0f,
        MutationStrength = 0.01f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 80f);

    var juvenile = simulation.State.Creatures[0];
    juvenile.AgeSeconds = 9.9f;
    simulation.State.Creatures[0] = juvenile;

    simulation.Step();
    AssertEqual(1, simulation.State.Creatures.Count, "Juvenile creature count");
    AssertEqual(0, simulation.State.Eggs.Count, "Juvenile egg count");

    var adult = simulation.State.Creatures[0];
    adult.AgeSeconds = 10f;
    simulation.State.Creatures[0] = adult;

    simulation.Step();
    AssertEqual(1, simulation.State.Creatures.Count, "Adult creature count before egg hatches");
    AssertEqual(1, simulation.State.Eggs.Count, "Adult egg count");
}

static void JuvenileGrowthScalesEffectiveTraits()
{
    var genome = CreatureGenome.Baseline with
    {
        BodyRadius = 10f,
        MaxSpeed = 40f,
        EatCaloriesPerSecond = 20f,
        MaturityAgeSeconds = 100f
    };

    var newborn = new CreatureState { AgeSeconds = 0f };
    var juvenile = new CreatureState { AgeSeconds = 50f };
    var adult = new CreatureState { AgeSeconds = 100f };

    AssertClose(0f, CreatureGrowth.MaturityProgress(newborn, genome), 0.000001, "Newborn maturity progress");
    AssertClose(0.35f, CreatureGrowth.GrowthFactor(newborn, genome), 0.000001, "Newborn growth factor");
    AssertTrue(CreatureGrowth.GrowthFactor(juvenile, genome) > CreatureGrowth.GrowthFactor(newborn, genome), "Juvenile should be larger than newborn");
    AssertClose(1f, CreatureGrowth.GrowthFactor(adult, genome), 0.000001, "Adult growth factor");
    AssertClose(3.5f, CreatureGrowth.EffectiveBodyRadius(newborn, genome), 0.000001, "Newborn body radius");
    AssertClose(10f, CreatureGrowth.EffectiveBodyRadius(adult, genome), 0.000001, "Adult body radius");
    AssertClose(20f, CreatureGrowth.EffectiveEatCaloriesPerSecond(adult, genome), 0.000001, "Adult eat rate");
    AssertTrue(CreatureGrowth.EffectiveMaxSpeed(newborn, genome) < CreatureGrowth.EffectiveMaxSpeed(adult, genome), "Newborn speed should be lower");
    AssertTrue(CreatureGrowth.EffectiveMaxTurnRadiansPerSecond(newborn, genome) < CreatureGrowth.EffectiveMaxTurnRadiansPerSecond(adult, genome), "Newborn turn rate should be lower");
    AssertTrue(CreatureGrowth.EffectiveSenseRadius(newborn, genome) < CreatureGrowth.EffectiveSenseRadius(adult, genome), "Newborn sense radius should be lower");
    AssertClose(genome.VisionAngleRadians, CreatureGrowth.EffectiveVisionAngleRadians(newborn, genome), 0.000001, "Vision angle should not growth-scale");
}

static void OffspringInvestmentScalesJuvenileGrowth()
{
    var genome = CreatureGenome.Baseline with
    {
        BodyRadius = 10f,
        MaturityAgeSeconds = 100f
    };

    var lowInvestment = new CreatureState { AgeSeconds = 0f, BirthInvestmentRatio = 0.25f };
    var baselineInvestment = new CreatureState { AgeSeconds = 0f, BirthInvestmentRatio = 1f };
    var highInvestment = new CreatureState { AgeSeconds = 0f, BirthInvestmentRatio = 4f };

    AssertClose(0.18f, CreatureGrowth.GrowthFactor(lowInvestment, genome), 0.000001, "Low investment newborn growth");
    AssertClose(0.35f, CreatureGrowth.GrowthFactor(baselineInvestment, genome), 0.000001, "Baseline newborn growth");
    AssertClose(0.7f, CreatureGrowth.GrowthFactor(highInvestment, genome), 0.000001, "High investment newborn growth");
    AssertTrue(
        CreatureGrowth.EffectiveBodyRadius(lowInvestment, genome) < CreatureGrowth.EffectiveBodyRadius(highInvestment, genome),
        "Higher investment should produce a larger newborn");
}

static void MinimalLifeLoopIsRepeatable()
{
    var first = CreateLifeLoopSimulation();
    var second = CreateLifeLoopSimulation();

    first.RunSteps(40);
    second.RunSteps(40);

    AssertEqual(first.State.Creatures.Count, second.State.Creatures.Count, "Creature count");
    AssertEqual(first.State.Eggs.Count, second.State.Eggs.Count, "Egg count");
    AssertEqual(first.State.Resources.Count, second.State.Resources.Count, "Resource count");
    AssertEqual(first.State.Genomes.Count, second.State.Genomes.Count, "Genome count");

    for (var i = 0; i < first.State.Creatures.Count; i++)
    {
        var firstCreature = first.State.Creatures[i];
        var secondCreature = second.State.Creatures[i];

        AssertEqual(firstCreature.Id, secondCreature.Id, $"Creature {i} id");
        AssertEqual(firstCreature.ParentId, secondCreature.ParentId, $"Creature {i} parent id");
        AssertClose(firstCreature.Position.X, secondCreature.Position.X, 0.000001, $"Creature {i} x");
        AssertClose(firstCreature.Position.Y, secondCreature.Position.Y, 0.000001, $"Creature {i} y");
        AssertClose(firstCreature.Energy, secondCreature.Energy, 0.000001, $"Creature {i} energy");
    }

    for (var i = 0; i < first.State.Eggs.Count; i++)
    {
        var firstEgg = first.State.Eggs[i];
        var secondEgg = second.State.Eggs[i];

        AssertEqual(firstEgg.Id, secondEgg.Id, $"Egg {i} id");
        AssertEqual(firstEgg.ParentId, secondEgg.ParentId, $"Egg {i} parent id");
        AssertClose(firstEgg.Position.X, secondEgg.Position.X, 0.000001, $"Egg {i} x");
        AssertClose(firstEgg.Position.Y, secondEgg.Position.Y, 0.000001, $"Egg {i} y");
        AssertClose(firstEgg.AgeSeconds, secondEgg.AgeSeconds, 0.000001, $"Egg {i} age");
        AssertClose(firstEgg.Energy, secondEgg.Energy, 0.000001, $"Egg {i} energy");
        AssertClose(firstEgg.Health, secondEgg.Health, 0.000001, $"Egg {i} health");
        AssertClose(firstEgg.MaxHealth, secondEgg.MaxHealth, 0.000001, $"Egg {i} max health");
        AssertClose(firstEgg.InvestmentRatio, secondEgg.InvestmentRatio, 0.000001, $"Egg {i} investment");
    }

    for (var i = 0; i < first.State.Resources.Count; i++)
    {
        AssertClose(
            first.State.Resources[i].Calories,
            second.State.Resources[i].Calories,
            0.000001,
            $"Resource {i} calories");
    }
}

static Simulation CreateLifeLoopSimulation()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 200f,
            WorldHeight = 200f,
            FixedDeltaSeconds = 0.25f
        },
        seed: 98765,
        systems: SimulationPipelines.CreateMinimalLifeLoop(spatialCellSize: 32f));

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        ReproductionEnergyThreshold = 65f,
        OffspringEnergyInvestment = 20f,
        ReproductionCooldownSeconds = 3f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 35f);
    simulation.State.SpawnCreature(genomeId, new SimVector2(150f, 150f), energy: 35f);

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(60f, 50f),
        Radius = 5f,
        Calories = 100f,
        MaxCalories = 100f,
        RegrowthCaloriesPerSecond = 2f
    });

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(140f, 150f),
        Radius = 5f,
        Calories = 100f,
        MaxCalories = 100f,
        RegrowthCaloriesPerSecond = 2f
    });

    return simulation;
}

static void CreatureSensingReportsFoodDirection()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 5,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 100f,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    simulation.State.Creatures[0] = creature;

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(30f, 30f),
        Radius = 1f,
        Calories = 20f,
        MaxCalories = 20f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertTrue(senses.FoodDetected, "Food should be detected");
    AssertTrue(senses.PlantDetected, "Plant should be detected");
    AssertTrue(!senses.MeatDetected, "Meat should not be detected");
    AssertClose(0.707106, senses.FoodDirectionForward, 0.0001, "Food forward direction");
    AssertClose(0.707106, senses.FoodDirectionRight, 0.0001, "Food right direction");
    AssertClose(0.707106, senses.PlantDirectionForward, 0.0001, "Plant forward direction");
    AssertClose(0.707106, senses.PlantDirectionRight, 0.0001, "Plant right direction");
    AssertTrue(senses.FoodProximity > 0.85f, "Food proximity should be high");
    AssertClose(0.125f, senses.VisibleFoodDensity, 0.000001, "Visible food density");
    AssertClose(0.125f, senses.VisiblePlantDensity, 0.000001, "Visible plant density");
    AssertClose(0f, senses.VisibleMeatDensity, 0.000001, "Visible meat density");
    AssertClose(0.25f, senses.EnergyRatio, 0.000001, "Energy ratio");
    AssertClose(0.75f, senses.Hunger, 0.000001, "Hunger");
    AssertClose(0f, senses.EggReserveRatio, 0.000001, "Egg reserve ratio");
    AssertClose(0f, senses.ReproductionReadiness, 0.000001, "Reproduction readiness");
}

static void CreatureSensingSplitsPlantAndMeatCues()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 305,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 100f,
        VisionAngleRadians = MathF.Tau,
        DietaryAdaptation = 1f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    simulation.State.Creatures[0] = creature;

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        Position = new SimVector2(30f, 20f),
        Radius = 1f,
        Calories = 20f,
        MaxCalories = 20f
    });
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Meat,
        Position = new SimVector2(20f, 30f),
        Radius = 1f,
        Calories = 20f,
        MaxCalories = 20f
    });

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertTrue(senses.FoodDetected, "Diet-weighted food should be detected");
    AssertTrue(senses.PlantDetected, "Plant cue should be detected");
    AssertTrue(senses.MeatDetected, "Meat cue should be detected");
    AssertClose(0.25f, senses.VisibleFoodDensity, 0.000001, "Visible food density");
    AssertClose(0.125f, senses.VisiblePlantDensity, 0.000001, "Visible plant density");
    AssertClose(0.125f, senses.VisibleMeatDensity, 0.000001, "Visible meat density");
    AssertTrue(senses.PlantDirectionForward > 0.99f, "Plant should be in front");
    AssertClose(0f, senses.PlantDirectionRight, 0.0001, "Plant right direction");
    AssertClose(0f, senses.MeatDirectionForward, 0.0001, "Meat forward direction");
    AssertTrue(senses.MeatDirectionRight > 0.99f, "Meat should be to the right");
    AssertClose(0f, senses.FoodDirectionForward, 0.0001, "Meat specialist should prefer meat forward");
    AssertTrue(senses.FoodDirectionRight > 0.99f, "Meat specialist should prefer meat right");
}

static void CreatureSensingReportsPlantQualityCues()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 240f,
            WorldHeight = 160f,
            FixedDeltaSeconds = 0.1f
        },
        seed: 1305,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex, worldSenseIntervalTicks: 1),
            new EatingSystem(spatialIndex)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 50f,
        VisionAngleRadians = MathF.Tau,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f);
    simulation.State.SpawnCreature(genomeId, new SimVector2(120f, 20f), energy: 25f);
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 120f), energy: 25f);

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        PlantKind = PlantResourceKind.Tender,
        Position = new SimVector2(21f, 20f),
        Radius = 2f,
        Calories = 20f,
        MaxCalories = 20f
    });
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        PlantKind = PlantResourceKind.Tough,
        Position = new SimVector2(121f, 20f),
        Radius = 2f,
        Calories = 20f,
        MaxCalories = 20f
    });
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        PlantKind = PlantResourceKind.Tender,
        Position = new SimVector2(69f, 120f),
        Radius = 1f,
        Calories = 20f,
        MaxCalories = 20f
    });

    simulation.Step();
    simulation.Step();

    var tenderSenses = simulation.State.Creatures[0].Senses;
    var toughSenses = simulation.State.Creatures[1].Senses;
    var farSenses = simulation.State.Creatures[2].Senses;

    AssertTrue(tenderSenses.PlantDetected, "Tender plant should be visible");
    AssertTrue(toughSenses.PlantDetected, "Tough plant should be visible");
    AssertClose(
        PlantResourceTraits.BiteEaseSense(PlantResourceKind.Tender),
        tenderSenses.VisiblePlantBiteEase,
        0.000001,
        "Tender plant visual bite ease");
    AssertClose(
        PlantResourceTraits.EnergyQualitySense(PlantResourceKind.Tough),
        toughSenses.VisiblePlantEnergyQuality,
        0.000001,
        "Tough plant visual energy quality");
    AssertTrue(
        tenderSenses.VisiblePlantBiteEase > toughSenses.VisiblePlantBiteEase,
        "Tender plants should look easier to bite than tough plants at close range");
    AssertClose(
        PlantResourceTraits.EnergyQualitySense(PlantResourceKind.Tender),
        tenderSenses.PlantFoodContactEnergyQuality,
        0.000001,
        "Tender plant contact energy quality");
    AssertClose(
        PlantResourceTraits.BiteEaseSense(PlantResourceKind.Tough),
        toughSenses.PlantFoodContactBiteEase,
        0.000001,
        "Tough plant contact bite ease");
    AssertTrue(tenderSenses.RecentPlantRawYield > toughSenses.RecentPlantRawYield, "Tender plant should transfer raw calories faster");
    AssertTrue(farSenses.PlantDetected, "Far plant should still be visible as plant mass");
    AssertTrue(farSenses.VisiblePlantDensity > 0f, "Far plant should contribute visible plant density");
    AssertClose(0f, farSenses.VisiblePlantEnergyQuality, 0.000001, "Far plant should not reveal close quality");
    AssertClose(0f, farSenses.VisiblePlantBiteEase, 0.000001, "Far plant should not reveal close bite ease");
}

static void CreatureSensingReportsPlantPreferenceBridge()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 240f,
            WorldHeight = 160f,
            FixedDeltaSeconds = 0.1f
        },
        seed: 1306,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex, worldSenseIntervalTicks: 1, enableSectorVision: true)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 80f,
        VisionAngleRadians = MathF.Tau,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(40f, 40f), energy: 25f);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    creature.RichPlantPayoffTrace = 1f;
    creature.IsTouchingFood = true;
    creature.FoodContactKind = FoodContactKind.Resource;
    creature.FoodContactResourceKind = ResourceKind.Plant;
    creature.FoodContactPlantKind = PlantResourceKind.Rich;
    simulation.State.Creatures[0] = creature;

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        PlantKind = PlantResourceKind.Rich,
        Position = new SimVector2(52f, 60f),
        Radius = 2f,
        Calories = 20f,
        MaxCalories = 20f
    });

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertTrue(senses.PlantPreferenceDensity > 0.5f, "Preferred plant should contribute a visible preference cue");
    AssertTrue(senses.PlantPreferenceDirectionForward > 0f, "Preferred plant should be ahead");
    AssertTrue(senses.PlantPreferenceDirectionRight > 0f, "Preferred plant should be to the right");
    AssertTrue(senses.PlantFoodContactPreference > 0.9f, "Touched rich plant should match recent rich payoff");
}

static void CreatureSensingReportsVisibleCreatureCues()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 306,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 100f,
        VisionAngleRadians = MathF.Tau,
        DietaryAdaptation = 1f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f);
    var observer = simulation.State.Creatures[0];
    observer.HeadingRadians = 0f;
    observer.Velocity = new SimVector2(1f, 0f);
    simulation.State.Creatures[0] = observer;
    simulation.State.SpawnCreature(genomeId, new SimVector2(30f, 20f), energy: 25f);
    var visibleCreature = simulation.State.Creatures[1];
    visibleCreature.HeadingRadians = MathF.PI;
    visibleCreature.Velocity = new SimVector2(-2f, 0f);
    simulation.State.Creatures[1] = visibleCreature;

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertTrue(senses.CreatureDetected, "Creature should be detected");
    AssertTrue(!senses.MeatDetected, "Visible living creature should not become a generic meat cue");
    AssertTrue(!senses.FoodDetected, "Visible living creature should not become a generic food cue");
    AssertClose(0.125f, senses.VisibleCreatureDensity, 0.000001, "Visible creature density");
    AssertClose(0f, senses.VisibleMeatDensity, 0.000001, "Visible meat density excludes living creatures");
    AssertClose(0f, senses.VisibleFoodDensity, 0.000001, "Visible food density excludes living creatures");
    AssertTrue(senses.CreatureProximity > 0.9f, "Creature proximity should be high");
    AssertTrue(senses.CreatureDirectionForward > 0.99f, "Creature should be in front");
    AssertClose(0f, senses.CreatureDirectionRight, 0.0001, "Creature right direction");
    AssertClose(0f, senses.CreatureRelativeBodySize, 0.0001, "Same-size creature relative body size");
    AssertTrue(senses.CreatureRelativeSpeed > 0f, "Visible creature should be moving faster");
    AssertTrue(senses.CreatureApproachRate > 0f, "Visible creature should be closing distance");
    AssertTrue(senses.CreatureFacingAlignment > 0.99f, "Visible creature should be pointed at observer");
}

static void CreatureSensingSmellsSimilarCreaturesBeyondVision()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 1307,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex, worldSenseIntervalTicks: 1)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 60f,
        VisionAngleRadians = MathF.PI / 3f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(80f, 80f), energy: 25f);
    var observer = simulation.State.Creatures[0];
    observer.HeadingRadians = 0f;
    simulation.State.Creatures[0] = observer;

    var similarId = simulation.State.SpawnCreature(genomeId, new SimVector2(74f, 80f), energy: 25f);
    observer = simulation.State.Creatures[0];
    observer.IsTouchingCreature = true;
    observer.CreatureContactId = similarId;
    simulation.State.Creatures[0] = observer;

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertTrue(!senses.CreatureDetected, "Creature behind the observer should not be visually detected");
    AssertTrue(senses.CreatureSimilarityScentDetected, "Similar creature scent should not require vision");
    AssertTrue(senses.CreatureSimilarityScentDensity > 0.9f, "Nearby identical creature should have strong similarity scent");
    AssertTrue(senses.CreatureSimilarityScentDirectionForward < -0.9f, "Similar creature scent should point behind");
    AssertClose(0f, senses.CreatureSimilarityScentDirectionRight, 0.0001, "Similar creature scent right direction");
    AssertClose(1f, senses.CreatureContactSimilarity, 0.000001, "Identical contact similarity");
}

static void CreatureSensingSeparatesPredatorPreySimilarity()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 1308,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex, worldSenseIntervalTicks: 1)
        ]);

    var preyGenome = CreatureGenome.Baseline with
    {
        SenseRadius = 60f,
        VisionAngleRadians = MathF.PI / 3f,
        DietaryAdaptation = 0.05f,
        CarrionAdaptation = 0f,
        BiteStrength = 0.15f,
        DamageResistance = 0.7f,
        MaturityAgeSeconds = 0f
    };
    var predatorGenome = preyGenome with
    {
        DietaryAdaptation = 0.9f,
        CarrionAdaptation = 0.3f,
        BiteStrength = 1.2f,
        DamageResistance = 1.2f
    };
    var preyGenomeId = simulation.State.AddGenome(preyGenome);
    var predatorGenomeId = simulation.State.AddGenome(predatorGenome);

    simulation.State.SpawnCreature(preyGenomeId, new SimVector2(80f, 80f), energy: 25f);
    var observer = simulation.State.Creatures[0];
    observer.HeadingRadians = 0f;
    simulation.State.Creatures[0] = observer;

    var predatorId = simulation.State.SpawnCreature(predatorGenomeId, new SimVector2(74f, 80f), energy: 25f);
    observer = simulation.State.Creatures[0];
    observer.IsTouchingCreature = true;
    observer.CreatureContactId = predatorId;
    simulation.State.Creatures[0] = observer;

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertTrue(!senses.CreatureDetected, "Predator-like creature behind the observer should not be visually detected");
    AssertTrue(!senses.CreatureSimilarityScentDetected, "Predator-prey trait split should not create kin scent");
    AssertTrue(senses.CreatureContactSimilarity < 0.82f, "Predator-prey trait split should stay below scent similarity floor");
}

static void CreatureSensingHearsIntentionalSoundBeyondVision()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 1309,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(
                spatialIndex,
                worldSenseIntervalTicks: 1,
                soundRangeMultiplier: 3f,
                soundDensitySaturation: 0.2f)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 40f,
        VisionAngleRadians = MathF.PI / 3f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f);
    var listener = simulation.State.Creatures[0];
    listener.HeadingRadians = 0f;
    simulation.State.Creatures[0] = listener;

    simulation.State.SpawnCreature(genomeId, new SimVector2(92f, 20f), energy: 25f);
    var emitter = simulation.State.Creatures[1];
    var emitterActions = emitter.Actions;
    emitterActions.SoundAmplitude = 0.8f;
    emitterActions.SoundTone = -0.6f;
    emitter.Actions = emitterActions;
    simulation.State.Creatures[1] = emitter;

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertTrue(!senses.CreatureDetected, "Emitter outside vision radius should not be visually detected");
    AssertTrue(senses.SoundDetected, "Intentional sound should be heard beyond vision");
    AssertTrue(senses.SoundDensity > 0.4f, "Nearby emitted sound should have useful density");
    AssertTrue(senses.SoundDirectionForward > 0.4f, "Sound should point forward toward the emitter");
    AssertClose(0f, senses.SoundDirectionRight, 0.0001, "Sound right direction");
    AssertClose(-0.6f, senses.SoundTone, 0.0001, "Sound tone should preserve the emitted tone");
    AssertTrue(senses.SoundToneClarity > 0.4f, "Single emitted tone should have useful clarity");
}

static void CreatureSensingSmellsMeatBeyondVision()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 307,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(
                spatialIndex,
                meatScentRangeMultiplier: 3f,
                meatScentCaloriesForFullStrength: 40f,
                meatScentDensitySaturation: 0.1f)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 50f,
        VisionAngleRadians = MathF.PI / 3f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    simulation.State.Creatures[0] = creature;

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Meat,
        Position = new SimVector2(120f, 20f),
        Radius = 1f,
        Calories = 80f,
        MaxCalories = 80f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertTrue(!senses.FoodDetected, "Meat outside vision radius should not become exact food");
    AssertTrue(!senses.MeatDetected, "Meat outside vision radius should not be visibly detected");
    AssertClose(0f, senses.VisibleMeatDensity, 0.000001, "Invisible meat density");
    AssertTrue(senses.MeatScentDetected, "Meat scent should be detected beyond vision");
    AssertTrue(senses.MeatScentDensity > 0.9f, "Meat scent density should be strong in this probe");
    AssertTrue(senses.MeatScentDirectionForward > 0.9f, "Meat scent should bias forward");
    AssertClose(0f, senses.MeatScentDirectionRight, 0.0001, "Meat scent right direction");
}

static void CreatureSensingReportsRottenMeatCues()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 308,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(
                spatialIndex,
                meatScentRangeMultiplier: 2f,
                meatScentCaloriesForFullStrength: 40f,
                meatScentDensitySaturation: 0.1f)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 100f,
        VisionAngleRadians = MathF.Tau,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    simulation.State.Creatures[0] = creature;

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Meat,
        Position = new SimVector2(40f, 20f),
        Radius = 1f,
        Calories = 80f,
        MaxCalories = 80f,
        RegrowthCaloriesPerSecond = 0f,
        MeatAgeSeconds = MeatQuality.StaleAgeSeconds
    });

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertTrue(senses.MeatDetected, "Stale meat should be visibly detected");
    AssertClose(MeatQuality.MinimumFreshness, senses.VisibleMeatFreshness, 0.000001, "Visible stale meat freshness");
    AssertTrue(senses.MeatScentDetected, "Generic meat scent should be detected");
    AssertTrue(senses.RottenMeatScentDetected, "Rotten meat scent should be detected");
    AssertTrue(senses.RottenMeatScentDensity > 0.9f, "Rotten scent density should be strong in this probe");
    AssertTrue(senses.RottenMeatScentDirectionForward > 0.9f, "Rotten scent should bias forward");
    AssertClose(0f, senses.RottenMeatScentDirectionRight, 0.0001, "Rotten scent right direction");
}

static void CreatureSensingReportsLocalTerrainDrag()
{
    var scenario = new SimulationScenario
    {
        Seed = 407,
        BiomeMapKind = BiomeMapKind.GeneratedNoise,
        WorldWidth = 1_000f,
        WorldHeight = 700f,
        BiomeCellSize = 100f,
        ResourceVoidBorderWidth = 0f,
        WorldSenseIntervalTicks = 1,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f,
        BarrenBiomeSpeedMultiplier = 0.5f,
        SparseBiomeSpeedMultiplier = 0.8f,
        GrasslandBiomeSpeedMultiplier = 1f,
        RichBiomeSpeedMultiplier = 1.1f
    };
    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var speedProfile = scenario.CreateBiomeSpeedProfile();
    var probe = FindAdjacentBiomeDragProbe(simulation.State.Biomes, speedProfile);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 100f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, probe.Position, energy: 25f);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = probe.HeadingRadians;
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertClose(SpeedMultiplierToDrag(speedProfile.For(probe.CurrentBiome)), senses.CurrentTerrainDrag, 0.000001, "Current terrain drag");
    AssertClose(SpeedMultiplierToDrag(speedProfile.For(probe.ForwardBiome)), senses.ForwardTerrainDrag, 0.000001, "Forward terrain drag");

    creature = simulation.State.Creatures[0];
    creature.Position = probe.Position;
    creature.HeadingRadians = -MathF.PI * 0.5f;
    simulation.State.Creatures[0] = creature;
    simulation.Step();

    senses = simulation.State.Creatures[0].Senses;
    AssertClose(SpeedMultiplierToDrag(speedProfile.For(probe.ForwardBiome)), senses.RightTerrainDrag, 0.000001, "Right terrain drag");

    creature = simulation.State.Creatures[0];
    creature.Position = probe.Position;
    creature.HeadingRadians = MathF.PI * 0.5f;
    simulation.State.Creatures[0] = creature;
    simulation.Step();

    senses = simulation.State.Creatures[0].Senses;
    AssertClose(SpeedMultiplierToDrag(speedProfile.For(probe.ForwardBiome)), senses.LeftTerrainDrag, 0.000001, "Left terrain drag");
}

static void CreatureSensingReportsHabitatQuality()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 200f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 0.1f
        },
        seed: 408,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex, worldSenseIntervalTicks: 1)
        ]);
    simulation.State.SetBiomes(BiomeMap.CreateFromCells(
        simulation.State.Bounds,
        cellSize: 100f,
        cellCountX: 2,
        cellCountY: 1,
        [BiomeKind.Grassland, BiomeKind.Fertile]));
    simulation.State.SetLocalFertility(LocalFertilityMap.CreateFromCells(
        simulation.State.Bounds,
        enabled: true,
        cellSize: 100f,
        cellCountX: 2,
        cellCountY: 1,
        minimumMultiplier: 0.25f,
        recoveryPerSecond: 0f,
        depletionPerPlant: 0f,
        neighborDepletionShare: 0f,
        [0.5f, 1f]));

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        SenseRadius = 100f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(85f, 50f), energy: 25f);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertClose(ExpectedHabitatQuality(BiomeKind.Grassland, localFertility: 0.5f), senses.CurrentHabitatQuality, 0.000001, "Current habitat quality");
    AssertClose(ExpectedHabitatQuality(BiomeKind.Fertile, localFertility: 1f), senses.ForwardHabitatQuality, 0.000001, "Forward habitat quality");
    AssertClose(senses.CurrentHabitatQuality, senses.LeftHabitatQuality, 0.000001, "Left habitat quality");
    AssertClose(senses.CurrentHabitatQuality, senses.RightHabitatQuality, 0.000001, "Right habitat quality");
    AssertTrue(senses.ForwardHabitatQuality > senses.CurrentHabitatQuality, "Forward fertile habitat should be better than locally depleted grassland");
}

static void CreatureSensingAppliesBiomeVisionRangePenalty()
{
    var visionProfile = new BiomePressureProfile(
        desert: 1f,
        scrubland: 1f,
        grassland: 1f,
        fertile: 1f,
        forest: 0.5f,
        wetland: 1f,
        tundra: 1f,
        highland: 1f);

    AssertTrue(CanSeeForwardPlant(BiomeKind.Grassland, visionProfile), "Grassland should keep full visual range");
    AssertTrue(!CanSeeForwardPlant(BiomeKind.Forest, visionProfile), "Forest should shorten visual range");
}

static bool CanSeeForwardPlant(BiomeKind biome, BiomePressureProfile visionProfile)
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 160f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 0.1f
        },
        seed: 409,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(
                spatialIndex,
                biomeVisionRangeProfile: visionProfile,
                worldSenseIntervalTicks: 1)
        ]);
    simulation.State.SetBiomes(BiomeMap.CreateUniform(simulation.State.Bounds, cellSize: 100f, biome));

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 100f,
        VisionAngleRadians = MathF.Tau,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(40f, 50f), energy: 25f);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    simulation.State.Creatures[0] = creature;

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        PlantKind = PlantResourceKind.Generic,
        Position = new SimVector2(95f, 50f),
        Radius = 2f,
        Calories = 20f,
        MaxCalories = 20f
    });

    simulation.Step();

    return simulation.State.Creatures[0].Senses.PlantDetected;
}

static void CreatureSensingReportsMemoryDirection()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 408,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 100f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    creature.MemoryVector = new SimVector2(0f, 0.75f);
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertClose(0.75f, senses.MemoryStrength, 0.000001, "Memory strength");
    AssertClose(0f, senses.MemoryDirectionForward, 0.000001, "Memory forward direction");
    AssertClose(0.75f, senses.MemoryDirectionRight, 0.000001, "Memory right direction");
}

static void CreatureSensingReportsLocalObstacles()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 0.1f
        },
        seed: 409,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex)
        ]);
    var cells = new bool[25];
    cells[2 * 5 + 2] = true;
    simulation.State.SetObstacles(ObstacleMap.CreateFromCells(
        simulation.State.Bounds,
        cellSize: 20f,
        cellCountX: 5,
        cellCountY: 5,
        cells));

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 100f,
        BodyRadius = 1f,
        MaturityAgeSeconds = 0f
    });
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 50f), energy: 25f);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    creature.LastMovementBlocked = true;
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertTrue(senses.ForwardObstacle > 0f, "Obstacle ahead should be sensed before contact");
    AssertClose(0f, senses.LeftObstacle, 0.000001, "No obstacle left");
    AssertClose(0f, senses.RightObstacle, 0.000001, "No obstacle right");
    AssertClose(1f, senses.MovementBlocked, 0.000001, "Movement blocked cue");
}

static void CreatureSensingReportsEggReserveReadiness()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 105,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        ReproductionEnergyThreshold = 100f,
        OffspringEnergyInvestment = 20f,
        MaturityAgeSeconds = 5f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 30f);
    var creature = simulation.State.Creatures[0];
    creature.AgeSeconds = 5f;
    creature.ReproductiveEnergy = 10f;
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertClose(0.5f, senses.EggReserveRatio, 0.000001, "Partial egg reserve ratio");
    AssertClose(0f, senses.ReproductionReadiness, 0.000001, "Partial egg should not be ready to lay");

    creature = simulation.State.Creatures[0];
    creature.ReproductiveEnergy = 20f;
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    senses = simulation.State.Creatures[0].Senses;
    AssertClose(1f, senses.EggReserveRatio, 0.000001, "Full egg reserve ratio");
    AssertClose(1f, senses.ReproductionReadiness, 0.000001, "Full egg should be ready to lay");
}

static void CreatureSensingReportsReproductiveContext()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 508,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex, plantPayoffTraceHalfLifeSeconds: 0.1f)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        ReproductionEnergyThreshold = 100f,
        OffspringEnergyInvestment = 20f,
        EatCaloriesPerSecond = 10f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 115f, health: 0.4f);
    var creature = simulation.State.Creatures[0];
    creature.LastCaloriesEaten = 0.25f;
    creature.LastCaloriesDigested = 0.5f;
    creature.LastPlantDigestedEnergy = 0.5f;
    creature.LastTenderPlantDigestedEnergy = 0.1f;
    creature.LastRichPlantDigestedEnergy = 0.25f;
    creature.LastToughPlantDigestedEnergy = 0.15f;
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertClose(0.4f, senses.HealthRatio, 0.000001, "Health ratio");
    AssertClose(0.75f, senses.EnergySurplusRatio, 0.000001, "Energy surplus ratio");
    AssertClose(0.75f, senses.RecentFoodSuccess, 0.000001, "Recent food success");
    AssertClose(1f, senses.RecentFoodEnergyYield, 0.000001, "Recent food energy yield");
    AssertClose(0.2f, senses.RecentTenderPlantEnergyYield, 0.000001, "Recent tender plant energy yield");
    AssertClose(0.5f, senses.RecentRichPlantEnergyYield, 0.000001, "Recent rich plant energy yield");
    AssertClose(0.3f, senses.RecentToughPlantEnergyYield, 0.000001, "Recent tough plant energy yield");
    AssertClose(0.2f, senses.TenderPlantPayoffTrace, 0.000001, "Tender plant payoff trace");
    AssertClose(0.5f, senses.RichPlantPayoffTrace, 0.000001, "Rich plant payoff trace");
    AssertClose(0.3f, senses.ToughPlantPayoffTrace, 0.000001, "Tough plant payoff trace");

    creature = simulation.State.Creatures[0];
    creature.LastCaloriesEaten = 0f;
    creature.LastCaloriesDigested = 0f;
    creature.LastPlantDigestedEnergy = 0f;
    creature.LastTenderPlantDigestedEnergy = 0f;
    creature.LastRichPlantDigestedEnergy = 0f;
    creature.LastToughPlantDigestedEnergy = 0f;
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    var decayedSenses = simulation.State.Creatures[0].Senses;
    AssertClose(0.25f, decayedSenses.RichPlantPayoffTrace, 0.000001, "Configured payoff trace half-life");
    AssertTrue(
        decayedSenses.RichPlantPayoffTrace < senses.RichPlantPayoffTrace,
        "Plant payoff trace should decay without new payoff");
    AssertTrue(decayedSenses.RichPlantPayoffTrace > 0f, "Plant payoff trace should persist briefly after payoff");
}

static void CreatureVisionConeHidesFoodBehindIt()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 32f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 106,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 100f,
        VisionAngleRadians = MathF.PI / 2f,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });
    simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 25f);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    simulation.State.Creatures[0] = creature;

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(30f, 50f),
        Radius = 2f,
        Calories = 20f,
        MaxCalories = 20f
    });
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(70f, 50f),
        Radius = 2f,
        Calories = 20f,
        MaxCalories = 20f
    });

    simulation.Step();

    var senses = simulation.State.Creatures[0].Senses;
    AssertTrue(senses.FoodDetected, "Forward food should be detected");
    AssertTrue(senses.FoodDirectionForward > 0.99f, "Detected food should be in front");
    AssertClose(0.125f, senses.VisibleFoodDensity, 0.000001, "Only forward food should count toward visible density");
}

static void CreatureSectorVisionBucketsVisibleCategories()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 32f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 108,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex, enableSectorVision: true)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        SenseRadius = 100f,
        VisionAngleRadians = MathF.PI / 2f,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });
    var smallGenomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 1.5f,
        SenseRadius = 100f,
        VisionAngleRadians = MathF.PI / 2f,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });
    var largeGenomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 6f,
        SenseRadius = 100f,
        VisionAngleRadians = MathF.PI / 2f,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 25f);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    creature.Velocity = SimVector2.Zero;
    simulation.State.Creatures[0] = creature;

    simulation.State.SpawnCreature(genomeId, new SimVector2(85f, 50f), energy: 25f);
    var visibleCreature = simulation.State.Creatures[1];
    visibleCreature.HeadingRadians = MathF.PI;
    visibleCreature.Velocity = new SimVector2(-10f, 0f);
    simulation.State.Creatures[1] = visibleCreature;
    simulation.State.SpawnCreature(smallGenomeId, new SimVector2(85f, 42f), energy: 25f);
    simulation.State.SpawnCreature(largeGenomeId, new SimVector2(85f, 62f), energy: 25f);
    var parentId = simulation.State.Creatures[0].Id;
    simulation.State.SpawnEgg(
        genomeId,
        brainId: -1,
        parentId,
        new SimVector2(80f, 38f),
        energy: 20f,
        incubationSeconds: 100f,
        generation: 1);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(80f, 50f),
        Radius = 2f,
        Calories = 20f,
        MaxCalories = 20f
    });
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Meat,
        Position = new SimVector2(80f, 62f),
        Radius = 2f,
        Calories = 20f,
        MaxCalories = 20f
    });
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(20f, 50f),
        Radius = 2f,
        Calories = 20f,
        MaxCalories = 20f
    });

    simulation.Step();

    var sectors = simulation.State.Creatures[0].Senses.VisionSectors;
    AssertTrue(sectors.Get(4).PlantDensity > 0f, "Ahead plant should land in the center sector");
    AssertClose(
        PlantResourceTraits.EnergyQualitySense(PlantResourceKind.Generic),
        sectors.Get(4).PlantEnergyQuality,
        0.000001,
        "Center sector plant energy quality");
    AssertClose(
        PlantResourceTraits.BiteEaseSense(PlantResourceKind.Generic),
        sectors.Get(4).PlantBiteEase,
        0.000001,
        "Center sector plant bite ease");
    AssertTrue(sectors.Get(6).MeatDensity > 0f, "Ahead-right meat should land in a right-side sector");
    AssertTrue(sectors.Get(2).EggDensity > 0f, "Ahead-left egg should land in a left-side sector");
    AssertTrue(sectors.Get(4).CreatureDensity > 0f, "Ahead creature should land in the center sector");
    AssertTrue(sectors.Get(4).SimilarCreatureDensity > 0f, "Same-size creature should land in similar-size sector detail");
    AssertTrue(sectors.Get(4).CreatureApproachRate > 0.3f, "Center creature should report a closing sector cue");
    AssertTrue(sectors.Get(4).CreatureFacingAlignment > 0.99f, "Center creature should report a facing sector cue");
    AssertTrue(
        Enumerable.Range(0, VisionSectorSet.SectorCount).Any(index => sectors.Get(index).SmallerCreatureDensity > 0f),
        "Smaller creature should appear in smaller-size sector detail");
    AssertTrue(
        Enumerable.Range(0, VisionSectorSet.SectorCount).Any(index => sectors.Get(index).LargerCreatureDensity > 0f),
        "Larger creature should appear in larger-size sector detail");
    AssertClose(0f, sectors.Get(0).PlantDensity, 0.000001, "Behind plant should not appear in leftmost visible sector");
    AssertTrue(sectors.Get(4).PlantProximity > 0.6f, "Center plant proximity should be high enough to guide approach");
}

static void BrainIoRegistryDescribesDenseAdapterContract()
{
    AssertEqual(NeuralBrainSchema.InputCount, BrainIoRegistry.Inputs.Count, "Input registry count");
    AssertEqual(NeuralBrainSchema.OutputCount, BrainIoRegistry.Outputs.Count, "Output registry count");
    AssertEqual(8, BrainIoRegistry.PhysicalActionOutputs.Count, "Physical action output count");
    AssertEqual(2, BrainIoRegistry.ArchitectureInternalOutputs.Count, "Internal output count");

    for (var i = 0; i < BrainIoRegistry.Inputs.Count; i++)
    {
        AssertEqual(i, BrainIoRegistry.Inputs[i].FlatIndex, $"Input registry index {i}");
    }

    for (var i = 0; i < BrainIoRegistry.Outputs.Count; i++)
    {
        AssertEqual(i, BrainIoRegistry.Outputs[i].FlatIndex, $"Output registry index {i}");
    }

    var moveOutput = BrainIoRegistry.GetOutput(NeuralBrainSchema.MoveForwardOutput);
    var grabOutput = BrainIoRegistry.GetOutput(NeuralBrainSchema.GrabOutput);
    var soundOutput = BrainIoRegistry.GetOutput(NeuralBrainSchema.SoundAmplitudeOutput);
    var memoryOutput = BrainIoRegistry.GetOutput(NeuralBrainSchema.MemoryForwardOutput);
    AssertEqual("action.move_forward", moveOutput.Key, "Move output key");
    AssertEqual(BrainOutputScope.PhysicalAction, moveOutput.Scope, "Move output scope");
    AssertEqual("action.grab", grabOutput.Key, "Grab output key");
    AssertEqual(BrainOutputScope.PhysicalAction, grabOutput.Scope, "Grab output scope");
    AssertEqual(2, grabOutput.IntroducedVersion, "Grab output introduced version");
    AssertEqual("action.sound_amplitude", soundOutput.Key, "Sound output key");
    AssertEqual(BrainOutputScope.PhysicalAction, soundOutput.Scope, "Sound output scope");
    AssertEqual(3, soundOutput.IntroducedVersion, "Sound output introduced version");
    AssertEqual("dense_memory.write_forward", memoryOutput.Key, "Memory output key");
    AssertEqual(BrainOutputScope.ArchitectureInternal, memoryOutput.Scope, "Memory output scope");

    var memoryInput = BrainIoRegistry.GetInput(NeuralBrainSchema.MemoryForwardInput);
    var sectorInput = BrainIoRegistry.GetInput(
        NeuralBrainSchema.VisionSectorCreatureApproachRateInput(VisionSectorSet.CenterSectorIndex));
    var contactInput = BrainIoRegistry.GetInput(NeuralBrainSchema.FoodContactInput);
    var grabInput = BrainIoRegistry.GetInput(NeuralBrainSchema.GrabPressureInput);
    var soundInput = BrainIoRegistry.GetInput(NeuralBrainSchema.SoundToneInput);
    var fatInput = BrainIoRegistry.GetInput(NeuralBrainSchema.FatRatioInput);
    AssertEqual(BrainInputFreshnessPolicy.AdapterRuntime, memoryInput.Freshness, "Memory input freshness");
    AssertEqual(BrainInputFreshnessPolicy.WorldSenseStale, sectorInput.Freshness, "Sector input freshness");
    AssertEqual(BrainInputFreshnessPolicy.InternalOrContactFresh, contactInput.Freshness, "Contact input freshness");
    AssertEqual(BrainInputFreshnessPolicy.InternalOrContactFresh, grabInput.Freshness, "Grab input freshness");
    AssertEqual(3, grabInput.IntroducedVersion, "Grab input introduced version");
    AssertEqual(BrainInputFreshnessPolicy.WorldSenseStale, soundInput.Freshness, "Sound input freshness");
    AssertEqual(4, soundInput.IntroducedVersion, "Sound input introduced version");
    AssertEqual(BrainInputFreshnessPolicy.InternalOrContactFresh, fatInput.Freshness, "Fat input freshness");
    AssertEqual(5, fatInput.IntroducedVersion, "Fat input introduced version");
    AssertEqual("vision.sector.4.creature_approach_rate", sectorInput.Key, "Sector input key");
    AssertClose(0f, sectorInput.SubstrateX ?? float.NaN, 0.000001, "Center sector substrate x");
}

static void LegacyNeuralAdapterMapsGroupedBrainInputs()
{
    var genome = CreatureGenome.Baseline with { DietaryAdaptation = 0.35f };
    var senses = new CreatureSenseState
    {
        EnergyRatio = 0.8f,
        HealthRatio = 0.66f,
        Hunger = 0.2f,
        FoodProximity = 0.7f,
        FoodDirectionForward = 0.6f,
        FoodDirectionRight = -0.3f,
        VisibleFoodDensity = 0.4f,
        PlantProximity = 0.5f,
        PlantDirectionForward = 0.25f,
        PlantDirectionRight = 0.75f,
        VisiblePlantDensity = 0.45f,
        VisiblePlantEnergyQuality = 0.57f,
        VisiblePlantBiteEase = 0.67f,
        MeatProximity = 0.65f,
        MeatDirectionForward = -0.2f,
        MeatDirectionRight = 0.9f,
        VisibleMeatDensity = 0.33f,
        VisibleMeatFreshness = 0.55f,
        EggReserveRatio = 0.12f,
        ReproductionReadiness = 0.22f,
        EnergySurplusRatio = 0.32f,
        RecentFoodSuccess = 0.42f,
        RecentPlantRawYield = 0.36f,
        RecentPlantEnergyYield = 0.46f,
        RecentFoodEnergyYield = 0.56f,
        RecentTenderPlantEnergyYield = 0.16f,
        RecentRichPlantEnergyYield = 0.26f,
        RecentToughPlantEnergyYield = 0.36f,
        TenderPlantPayoffTrace = 0.06f,
        RichPlantPayoffTrace = 0.07f,
        ToughPlantPayoffTrace = 0.08f,
        PlantPreferenceDensity = 0.17f,
        PlantPreferenceDirectionForward = 0.27f,
        PlantPreferenceDirectionRight = -0.37f,
        PlantFoodContactPreference = 0.47f,
        CreatureProximity = 0.52f,
        CreatureDirectionForward = -0.62f,
        CreatureDirectionRight = 0.72f,
        VisibleCreatureDensity = 0.82f,
        CreatureRelativeBodySize = -0.18f,
        CreatureRelativeSpeed = 0.28f,
        CreatureApproachRate = 0.38f,
        CreatureFacingAlignment = -0.48f,
        MeatScentDensity = 0.58f,
        MeatScentDirectionForward = 0.68f,
        MeatScentDirectionRight = -0.78f,
        RottenMeatScentDensity = 0.13f,
        RottenMeatScentDirectionForward = -0.23f,
        RottenMeatScentDirectionRight = 0.34f,
        CreatureSimilarityScentDensity = 0.43f,
        CreatureSimilarityScentDirectionForward = -0.53f,
        CreatureSimilarityScentDirectionRight = 0.63f,
        SoundDensity = 0.29f,
        SoundDirectionForward = 0.39f,
        SoundDirectionRight = -0.49f,
        SoundTone = 0.59f,
        SoundToneClarity = 0.69f,
        FatRatio = 0.79f,
        MassBurdenRatio = 0.89f,
        CurrentTerrainDrag = 0.44f,
        ForwardTerrainDrag = 0.54f,
        LeftTerrainDrag = 0.64f,
        RightTerrainDrag = 0.74f,
        CurrentHabitatQuality = 0.15f,
        ForwardHabitatQuality = 0.25f,
        LeftHabitatQuality = 0.35f,
        RightHabitatQuality = 0.45f,
        ForwardObstacle = 0.84f,
        LeftObstacle = 0.14f,
        RightObstacle = 0.24f,
        MovementBlocked = 1f,
        FoodContact = 0.93f,
        PlantFoodContact = 1f,
        PlantFoodContactEnergyQuality = 0.73f,
        PlantFoodContactBiteEase = 0.83f,
        MeatFoodContact = 0.25f,
        EggFoodContact = 0.5f,
        CreatureContact = 0.75f,
        CreatureContactSimilarity = 0.85f,
        GrabPressure = 0.65f,
        GrabDirectionForward = -0.45f,
        GrabDirectionRight = 0.35f,
        CanGrabCreature = 1f,
        IsHoldingCreature = 1f,
        MemoryDirectionForward = 0.11f,
        MemoryDirectionRight = -0.21f,
        MemoryStrength = 0.31f
    };
    var sectors = default(VisionSectorSet);
    sectors.AddPlant(0, 0.5f, energyQuality: 0.61f, biteEase: 0.71f, qualityWeight: 1f);
    sectors.AddMeat(4, 0.7f);
    sectors.AddEgg(6, 0.8f);
    sectors.AddCreature(8, 0.9f, relativeBodySize: -0.5f);
    sectors.AddCreature(5, 0.4f, relativeBodySize: 0f);
    sectors.AddCreature(2, 0.6f, relativeBodySize: 0.5f, approachRate: 0.7f, facingAlignment: -0.35f);
    senses.VisionSectors = sectors;

    var frame = BrainInputFrame.FromSenses(senses, genome);
    var memory = LegacyNeuralMemoryInputFrame.FromSenses(senses);
    Span<float> inputs = stackalloc float[NeuralBrainSchema.InputCount];

    LegacyNeuralBrainAdapter.FillInputs(frame, memory, inputs);

    AssertClose(1f, inputs[NeuralBrainSchema.BiasInput], 0.000001, "Bias input");
    AssertClose(0.8f, inputs[NeuralBrainSchema.EnergyRatioInput], 0.000001, "Energy input");
    AssertClose(0.66f, inputs[NeuralBrainSchema.HealthRatioInput], 0.000001, "Health input");
    AssertClose(0.35f, inputs[NeuralBrainSchema.DietaryMeatBiasInput], 0.000001, "Diet input");
    AssertClose(0.4f, inputs[NeuralBrainSchema.VisibleFoodDensityInput], 0.000001, "Food density input");
    AssertClose(0.45f, inputs[NeuralBrainSchema.VisiblePlantDensityInput], 0.000001, "Plant density input");
    AssertClose(0.33f, inputs[NeuralBrainSchema.VisibleMeatDensityInput], 0.000001, "Meat density input");
    AssertClose(0.57f, inputs[NeuralBrainSchema.VisiblePlantEnergyQualityInput], 0.000001, "Plant energy quality input");
    AssertClose(0.67f, inputs[NeuralBrainSchema.VisiblePlantBiteEaseInput], 0.000001, "Plant bite ease input");
    AssertClose(0.55f, inputs[NeuralBrainSchema.VisibleMeatFreshnessInput], 0.000001, "Meat freshness input");
    AssertClose(0.82f, inputs[NeuralBrainSchema.VisibleCreatureDensityInput], 0.000001, "Creature density input");
    AssertClose(0.68f, inputs[NeuralBrainSchema.MeatScentForwardInput], 0.000001, "Meat scent forward input");
    AssertClose(0.34f, inputs[NeuralBrainSchema.RottenMeatScentRightInput], 0.000001, "Rot scent right input");
    AssertClose(0.43f, inputs[NeuralBrainSchema.CreatureSimilarityScentDensityInput], 0.000001, "Creature similarity scent density input");
    AssertClose(-0.53f, inputs[NeuralBrainSchema.CreatureSimilarityScentForwardInput], 0.000001, "Creature similarity scent forward input");
    AssertClose(0.63f, inputs[NeuralBrainSchema.CreatureSimilarityScentRightInput], 0.000001, "Creature similarity scent right input");
    AssertClose(0.29f, inputs[NeuralBrainSchema.SoundDensityInput], 0.000001, "Sound density input");
    AssertClose(0.39f, inputs[NeuralBrainSchema.SoundDirectionForwardInput], 0.000001, "Sound forward input");
    AssertClose(-0.49f, inputs[NeuralBrainSchema.SoundDirectionRightInput], 0.000001, "Sound right input");
    AssertClose(0.59f, inputs[NeuralBrainSchema.SoundToneInput], 0.000001, "Sound tone input");
    AssertClose(0.69f, inputs[NeuralBrainSchema.SoundToneClarityInput], 0.000001, "Sound clarity input");
    AssertClose(0.79f, inputs[NeuralBrainSchema.FatRatioInput], 0.000001, "Fat ratio input");
    AssertClose(0.89f, inputs[NeuralBrainSchema.MassBurdenInput], 0.000001, "Mass burden input");
    AssertClose(0.54f, inputs[NeuralBrainSchema.ForwardTerrainDragInput], 0.000001, "Terrain input");
    AssertClose(0.15f, inputs[NeuralBrainSchema.CurrentHabitatQualityInput], 0.000001, "Current habitat input");
    AssertClose(0.25f, inputs[NeuralBrainSchema.ForwardHabitatQualityInput], 0.000001, "Forward habitat input");
    AssertClose(0.35f, inputs[NeuralBrainSchema.LeftHabitatQualityInput], 0.000001, "Left habitat input");
    AssertClose(0.45f, inputs[NeuralBrainSchema.RightHabitatQualityInput], 0.000001, "Right habitat input");
    AssertClose(0.84f, inputs[NeuralBrainSchema.ForwardObstacleInput], 0.000001, "Obstacle input");
    AssertClose(0.93f, inputs[NeuralBrainSchema.FoodContactInput], 0.000001, "Food contact input");
    AssertClose(1f, inputs[NeuralBrainSchema.PlantFoodContactInput], 0.000001, "Plant food contact input");
    AssertClose(0.73f, inputs[NeuralBrainSchema.PlantFoodContactEnergyQualityInput], 0.000001, "Plant contact energy quality input");
    AssertClose(0.83f, inputs[NeuralBrainSchema.PlantFoodContactBiteEaseInput], 0.000001, "Plant contact bite ease input");
    AssertClose(0.25f, inputs[NeuralBrainSchema.MeatFoodContactInput], 0.000001, "Meat food contact input");
    AssertClose(0.5f, inputs[NeuralBrainSchema.EggFoodContactInput], 0.000001, "Egg food contact input");
    AssertClose(0.75f, inputs[NeuralBrainSchema.CreatureContactInput], 0.000001, "Creature contact input");
    AssertClose(0.85f, inputs[NeuralBrainSchema.CreatureContactSimilarityInput], 0.000001, "Creature contact similarity input");
    AssertClose(0.65f, inputs[NeuralBrainSchema.GrabPressureInput], 0.000001, "Grab pressure input");
    AssertClose(-0.45f, inputs[NeuralBrainSchema.GrabDirectionForwardInput], 0.000001, "Grab direction forward input");
    AssertClose(0.35f, inputs[NeuralBrainSchema.GrabDirectionRightInput], 0.000001, "Grab direction right input");
    AssertClose(1f, inputs[NeuralBrainSchema.CanGrabCreatureInput], 0.000001, "Can grab creature input");
    AssertClose(1f, inputs[NeuralBrainSchema.IsHoldingCreatureInput], 0.000001, "Is holding creature input");
    AssertClose(0.36f, inputs[NeuralBrainSchema.RecentPlantRawYieldInput], 0.000001, "Recent plant raw yield input");
    AssertClose(0.46f, inputs[NeuralBrainSchema.RecentPlantEnergyYieldInput], 0.000001, "Recent plant energy yield input");
    AssertClose(0.56f, inputs[NeuralBrainSchema.RecentFoodEnergyYieldInput], 0.000001, "Recent food energy yield input");
    AssertClose(0.16f, inputs[NeuralBrainSchema.RecentTenderPlantEnergyYieldInput], 0.000001, "Recent tender plant energy yield input");
    AssertClose(0.26f, inputs[NeuralBrainSchema.RecentRichPlantEnergyYieldInput], 0.000001, "Recent rich plant energy yield input");
    AssertClose(0.36f, inputs[NeuralBrainSchema.RecentToughPlantEnergyYieldInput], 0.000001, "Recent tough plant energy yield input");
    AssertClose(0.06f, inputs[NeuralBrainSchema.TenderPlantPayoffTraceInput], 0.000001, "Tender plant payoff trace input");
    AssertClose(0.07f, inputs[NeuralBrainSchema.RichPlantPayoffTraceInput], 0.000001, "Rich plant payoff trace input");
    AssertClose(0.08f, inputs[NeuralBrainSchema.ToughPlantPayoffTraceInput], 0.000001, "Tough plant payoff trace input");
    AssertClose(0.17f, inputs[NeuralBrainSchema.PlantPreferenceDensityInput], 0.000001, "Plant preference density input");
    AssertClose(0.27f, inputs[NeuralBrainSchema.PlantPreferenceForwardInput], 0.000001, "Plant preference forward input");
    AssertClose(-0.37f, inputs[NeuralBrainSchema.PlantPreferenceRightInput], 0.000001, "Plant preference right input");
    AssertClose(0.47f, inputs[NeuralBrainSchema.PlantFoodContactPreferenceInput], 0.000001, "Plant contact preference input");
    AssertClose(0.11f, inputs[NeuralBrainSchema.MemoryForwardInput], 0.000001, "Legacy memory forward input");
    AssertClose(-0.21f, inputs[NeuralBrainSchema.MemoryRightInput], 0.000001, "Legacy memory right input");
    AssertClose(0.125f, inputs[NeuralBrainSchema.VisionSectorPlantDensityInput(0)], 0.000001, "Sector plant density input");
    AssertClose(0.5f, inputs[NeuralBrainSchema.VisionSectorPlantProximityInput(0)], 0.000001, "Sector plant proximity input");
    AssertClose(0.61f, inputs[NeuralBrainSchema.VisionSectorPlantEnergyQualityInput(0)], 0.000001, "Sector plant energy quality input");
    AssertClose(0.71f, inputs[NeuralBrainSchema.VisionSectorPlantBiteEaseInput(0)], 0.000001, "Sector plant bite ease input");
    AssertClose(0.125f, inputs[NeuralBrainSchema.VisionSectorMeatDensityInput(4)], 0.000001, "Sector meat density input");
    AssertClose(0.7f, inputs[NeuralBrainSchema.VisionSectorMeatProximityInput(4)], 0.000001, "Sector meat proximity input");
    AssertClose(0.125f, inputs[NeuralBrainSchema.VisionSectorEggDensityInput(6)], 0.000001, "Sector egg density input");
    AssertClose(0.8f, inputs[NeuralBrainSchema.VisionSectorEggProximityInput(6)], 0.000001, "Sector egg proximity input");
    AssertClose(0.125f, inputs[NeuralBrainSchema.VisionSectorCreatureDensityInput(8)], 0.000001, "Sector creature density input");
    AssertClose(0.9f, inputs[NeuralBrainSchema.VisionSectorCreatureProximityInput(8)], 0.000001, "Sector creature proximity input");
    AssertClose(0.125f, inputs[NeuralBrainSchema.VisionSectorSmallerCreatureDensityInput(8)], 0.000001, "Sector smaller creature density input");
    AssertClose(0.9f, inputs[NeuralBrainSchema.VisionSectorSmallerCreatureProximityInput(8)], 0.000001, "Sector smaller creature proximity input");
    AssertClose(0.125f, inputs[NeuralBrainSchema.VisionSectorSimilarCreatureDensityInput(5)], 0.000001, "Sector similar creature density input");
    AssertClose(0.4f, inputs[NeuralBrainSchema.VisionSectorSimilarCreatureProximityInput(5)], 0.000001, "Sector similar creature proximity input");
    AssertClose(0.125f, inputs[NeuralBrainSchema.VisionSectorLargerCreatureDensityInput(2)], 0.000001, "Sector larger creature density input");
    AssertClose(0.6f, inputs[NeuralBrainSchema.VisionSectorLargerCreatureProximityInput(2)], 0.000001, "Sector larger creature proximity input");
    AssertClose(0.7f, inputs[NeuralBrainSchema.VisionSectorCreatureApproachRateInput(2)], 0.000001, "Sector creature approach rate input");
    AssertClose(-0.35f, inputs[NeuralBrainSchema.VisionSectorCreatureFacingAlignmentInput(2)], 0.000001, "Sector creature facing alignment input");

    Span<float> outputs = stackalloc float[NeuralBrainSchema.OutputCount];
    outputs[NeuralBrainSchema.MoveForwardOutput] = 2f;
    outputs[NeuralBrainSchema.TurnOutput] = -2f;
    outputs[NeuralBrainSchema.EatOutput] = 0.25f;
    outputs[NeuralBrainSchema.ReproduceOutput] = 0.5f;
    outputs[NeuralBrainSchema.AttackOutput] = -0.5f;
    outputs[NeuralBrainSchema.GrabOutput] = 2f;
    outputs[NeuralBrainSchema.SoundAmplitudeOutput] = 2f;
    outputs[NeuralBrainSchema.SoundToneOutput] = -2f;
    outputs[NeuralBrainSchema.MemoryForwardOutput] = 3f;
    outputs[NeuralBrainSchema.MemoryRightOutput] = -3f;

    var actionOutputs = LegacyNeuralBrainAdapter.ReadStandardOutputs(outputs);
    var memoryOutputs = LegacyNeuralBrainAdapter.ReadMemoryOutputs(outputs);

    AssertClose(1f, actionOutputs.MoveForward, 0.000001, "Move output is clamped");
    AssertClose(-1f, actionOutputs.Turn, 0.000001, "Turn output is clamped");
    AssertClose(0.25f, actionOutputs.Eat, 0.000001, "Eat output remains raw");
    AssertClose(0.5f, actionOutputs.Reproduce, 0.000001, "Reproduce output remains raw");
    AssertClose(-0.5f, actionOutputs.Attack, 0.000001, "Attack output remains raw");
    AssertClose(1f, actionOutputs.Grab, 0.000001, "Grab output is clamped");
    AssertClose(1f, actionOutputs.SoundAmplitude, 0.000001, "Sound amplitude output is clamped");
    AssertClose(-1f, actionOutputs.SoundTone, 0.000001, "Sound tone output is clamped");
    AssertClose(1f, memoryOutputs.DirectionForward, 0.000001, "Memory forward output is clamped");
    AssertClose(-1f, memoryOutputs.DirectionRight, 0.000001, "Memory right output is clamped");
}

static void NeuralControllerTurnsSensesIntoActions()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 32f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 6,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex, enableSectorVision: true),
            new NeuralControllerSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = 10f,
        MaxTurnRadiansPerSecond = 4f,
        SenseRadius = 100f,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager());

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    simulation.State.Creatures[0] = creature;

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(40f, 20f),
        Radius = 2f,
        Calories = 50f,
        MaxCalories = 50f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();

    creature = simulation.State.Creatures[0];
    AssertTrue(creature.Actions.MoveForward > 0.5f, "Forager should request forward movement");
    AssertTrue(Math.Abs(creature.Actions.Turn) < 0.001f, "Food straight ahead should not request turn");
    AssertTrue(!creature.Actions.WantsEat, "Forager should wait for food contact before eating");
    AssertTrue(creature.DesiredVelocity.X > 0f, "Desired velocity should face food");
}

static void NeuralControllerReusesActionsOnSkippedWorldSenses()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 610,
        systems:
        [
            new NeuralControllerSystem(reuseActionsOnSkippedWorldSenses: true)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = 10f,
        MaxTurnRadiansPerSecond = 4f,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });
    var weights = new float[NeuralBrainGenome.DirectWeightCount];
    weights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.BiasInput] = 2f;
    var brainId = simulation.State.AddBrain(new NeuralBrainGenome(weights));

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 50f, brainId: brainId);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    creature.Senses = new CreatureSenseState
    {
        WorldSenseRefreshed = false,
        WorldSenseTick = 0,
        EnergyRatio = 0.5f,
        HealthRatio = 1f,
        Hunger = 0.25f
    };
    creature.Actions = new CreatureActionState
    {
        MoveForward = 0.25f,
        Turn = 0.5f,
        EatOutput = -0.25f,
        ReproduceOutput = -0.25f,
        AttackOutput = -0.25f
    };
    creature.LastNeuralDecisionTick = 0;
    creature.LastNeuralEnergyRatio = 0.5f;
    creature.LastNeuralHealthRatio = 1f;
    creature.LastNeuralHunger = 0.25f;
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    creature = simulation.State.Creatures[0];
    AssertClose(0.25f, creature.Actions.MoveForward, 0.000001, "Skipped world sense should reuse previous move output");
    AssertClose(0.5f, creature.Actions.Turn, 0.000001, "Skipped world sense should reuse previous turn output");
    AssertEqual(0L, creature.LastNeuralDecisionTick, "Skipped world sense should not record a new neural decision");
    AssertClose(0.2f, creature.HeadingRadians, 0.000001, "Reused turn output should still update heading");
    AssertTrue(creature.DesiredVelocity.X > 0f, "Reused move output should still produce desired velocity");
}

static void NeuralControllerForcesDecisionsOnStaleContact()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 611,
        systems:
        [
            new NeuralControllerSystem(reuseActionsOnSkippedWorldSenses: true)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = 10f,
        MaxTurnRadiansPerSecond = 4f,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });
    var weights = new float[NeuralBrainGenome.DirectWeightCount];
    weights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.BiasInput] = 2f;
    weights[NeuralBrainSchema.EatOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.BiasInput] = 2f;
    var brainId = simulation.State.AddBrain(new NeuralBrainGenome(weights));

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 50f, brainId: brainId);
    var creature = simulation.State.Creatures[0];
    creature.Senses = new CreatureSenseState
    {
        WorldSenseRefreshed = false,
        WorldSenseTick = 0,
        EnergyRatio = 0.5f,
        HealthRatio = 1f,
        Hunger = 0.25f,
        FoodContact = 1f
    };
    creature.Actions = new CreatureActionState
    {
        MoveForward = 0f,
        Turn = 0f,
        EatOutput = -1f
    };
    creature.LastNeuralDecisionTick = 0;
    creature.LastNeuralEnergyRatio = 0.5f;
    creature.LastNeuralHealthRatio = 1f;
    creature.LastNeuralHunger = 0.25f;
    creature.IsTouchingFood = true;
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    creature = simulation.State.Creatures[0];
    AssertTrue(creature.Actions.MoveForward > 0.9f, "Food contact should force a fresh neural movement decision");
    AssertTrue(creature.Actions.WantsEat, "Food contact should force a fresh neural eat decision");
}

static void NeuralControllerParallelPathMatchesSingleThreadedPath()
{
    var singleThreaded = CreateNeuralControllerParallelProbe(threadCount: 1);
    var parallel = CreateNeuralControllerParallelProbe(threadCount: 4);

    singleThreaded.Step();
    parallel.Step();

    AssertEqual(singleThreaded.State.Creatures.Count, parallel.State.Creatures.Count, "Parallel probe creature count");
    for (var i = 0; i < singleThreaded.State.Creatures.Count; i++)
    {
        var expected = singleThreaded.State.Creatures[i];
        var actual = parallel.State.Creatures[i];
        AssertClose(expected.HeadingRadians, actual.HeadingRadians, 0.000001, $"Parallel probe heading {i}");
        AssertClose(expected.DesiredVelocity.X, actual.DesiredVelocity.X, 0.000001, $"Parallel probe desired velocity X {i}");
        AssertClose(expected.DesiredVelocity.Y, actual.DesiredVelocity.Y, 0.000001, $"Parallel probe desired velocity Y {i}");
        AssertClose(expected.MemoryVector.X, actual.MemoryVector.X, 0.000001, $"Parallel probe memory X {i}");
        AssertClose(expected.MemoryVector.Y, actual.MemoryVector.Y, 0.000001, $"Parallel probe memory Y {i}");
        AssertClose(expected.Actions.MoveForward, actual.Actions.MoveForward, 0.000001, $"Parallel probe move {i}");
        AssertClose(expected.Actions.Turn, actual.Actions.Turn, 0.000001, $"Parallel probe turn {i}");
        AssertClose(expected.Actions.EatOutput, actual.Actions.EatOutput, 0.000001, $"Parallel probe eat {i}");
        AssertClose(expected.Actions.ReproduceOutput, actual.Actions.ReproduceOutput, 0.000001, $"Parallel probe reproduce {i}");
        AssertClose(expected.Actions.AttackOutput, actual.Actions.AttackOutput, 0.000001, $"Parallel probe attack {i}");
        AssertEqual(expected.Actions.WantsEat, actual.Actions.WantsEat, $"Parallel probe eat intent {i}");
        AssertEqual(expected.Actions.WantsReproduce, actual.Actions.WantsReproduce, $"Parallel probe reproduce intent {i}");
        AssertEqual(expected.Actions.WantsAttack, actual.Actions.WantsAttack, $"Parallel probe attack intent {i}");
        AssertEqual(expected.LastNeuralDecisionTick, actual.LastNeuralDecisionTick, $"Parallel probe decision tick {i}");
    }

    AssertEqual(
        singleThreaded.Profile?.NeuralController.BrainEvaluations ?? -1,
        parallel.Profile?.NeuralController.BrainEvaluations ?? -2,
        "Parallel probe brain evaluations");
    AssertEqual(
        singleThreaded.Profile?.NeuralController.CreaturesControlled ?? -1,
        parallel.Profile?.NeuralController.CreaturesControlled ?? -2,
        "Parallel probe creatures controlled");
}

static Simulation CreateNeuralControllerParallelProbe(int threadCount)
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 612,
        systems: [new NeuralControllerSystem(neuralControllerThreadCount: threadCount)]);
    simulation.Profile = new SimulationProfile();

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = 11f,
        MaxTurnRadiansPerSecond = 3.5f,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });

    var weights = new float[NeuralBrainGenome.DirectWeightCount];
    weights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.BiasInput] = 0.35f;
    weights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisibleFoodDensityInput] = 2.2f;
    weights[
        NeuralBrainSchema.TurnOutput * NeuralBrainSchema.InputCount
        + NeuralBrainSchema.VisionSectorPlantProximityInput(VisionSectorSet.CenterSectorIndex + 1)] = 1.7f;
    weights[NeuralBrainSchema.EatOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.FoodContactInput] = 2.4f;
    weights[NeuralBrainSchema.ReproduceOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.ReproductionReadinessInput] = 1.9f;
    weights[NeuralBrainSchema.AttackOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.CreatureContactInput] = 1.5f;
    weights[NeuralBrainSchema.MemoryForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisibleFoodDensityInput] = 1.1f;
    weights[
        NeuralBrainSchema.MemoryRightOutput * NeuralBrainSchema.InputCount
        + NeuralBrainSchema.VisionSectorPlantProximityInput(VisionSectorSet.CenterSectorIndex + 1)] = 1.1f;
    var brainId = simulation.State.AddBrain(new NeuralBrainGenome(weights));

    for (var i = 0; i < 48; i++)
    {
        simulation.State.SpawnCreature(
            genomeId,
            new SimVector2(20f + i * 2f, 25f + i),
            energy: 50f + i,
            brainId: brainId);
        var creature = simulation.State.Creatures[^1];
        creature.HeadingRadians = i * 0.071f;
        creature.MemoryVector = new SimVector2((i % 5) * 0.05f, (i % 7) * -0.03f);
        creature.Senses = new CreatureSenseState
        {
            WorldSenseRefreshed = true,
            WorldSenseTick = 0,
            EnergyRatio = 0.35f + (i % 9) * 0.04f,
            HealthRatio = 0.8f + (i % 4) * 0.05f,
            Hunger = 0.2f + (i % 6) * 0.1f,
            FoodProximity = (i % 10) / 10f,
            FoodDirectionForward = i % 2 == 0 ? 0.75f : -0.15f,
            FoodDirectionRight = ((i % 7) - 3) * 0.18f,
            FoodContact = i % 11 == 0 ? 1f : 0f,
            CreatureContact = i % 13 == 0 ? 1f : 0f,
            ReproductionReadiness = (i % 8) / 8f
        };
        simulation.State.Creatures[^1] = creature;
    }

    return simulation;
}

static void NeuralControllerConsumesSectorVisionInputs()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 32f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 109,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex, enableSectorVision: true),
            new NeuralControllerSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = 10f,
        MaxTurnRadiansPerSecond = 4f,
        SenseRadius = 100f,
        VisionAngleRadians = MathF.PI / 2f,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });
    var weights = new float[NeuralBrainGenome.DirectWeightCount];
    weights[
        NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount
        + NeuralBrainSchema.VisionSectorPlantProximityInput(VisionSectorSet.CenterSectorIndex)] = 4f;
    var brainId = simulation.State.AddBrain(new NeuralBrainGenome(weights));

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    simulation.State.Creatures[0] = creature;

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(40f, 20f),
        Radius = 2f,
        Calories = 50f,
        MaxCalories = 50f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();

    creature = simulation.State.Creatures[0];
    AssertTrue(
        creature.Senses.VisionSectors.Get(VisionSectorSet.CenterSectorIndex).PlantProximity > 0.75f,
        "Center sector should contain the visible plant");
    AssertTrue(creature.Actions.MoveForward > 0.9f, "Sector plant proximity should drive forward movement");
    AssertTrue(creature.DesiredVelocity.X > 0f, "Sector-driven desired velocity should face forward");
}

static void NeuralControllerWritesSpatialMemory()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 409,
        systems: [new NeuralControllerSystem()]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = 10f,
        MaxTurnRadiansPerSecond = 4f,
        ReproductionEnergyThreshold = 100f
    });
    var weights = new float[NeuralBrainGenome.DirectWeightCount];
    weights[NeuralBrainSchema.MemoryForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisibleFoodDensityInput] = 4f;
    weights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.MemoryForwardInput] = 4f;
    var brainId = simulation.State.AddBrain(new NeuralBrainGenome(weights));

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    creature.Senses = new CreatureSenseState { VisibleFoodDensity = 1f };
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    creature = simulation.State.Creatures[0];
    AssertTrue(creature.Actions.MemoryForward > 0.9f, "Food cue should write forward memory");
    AssertTrue(creature.MemoryVector.X > 0.85f, "Memory should persist as a world-space forward vector");

    creature.Senses = new CreatureSenseState
    {
        MemoryDirectionForward = creature.MemoryVector.X,
        MemoryStrength = creature.MemoryVector.Length
    };
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    creature = simulation.State.Creatures[0];
    AssertTrue(creature.Actions.MoveForward > 0.9f, "Remembered forward direction should drive movement");
}

static void NeuralControllerHonorsMemoryTuning()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 410,
        systems: [new NeuralControllerSystem(memoryDecayPerSecond: 1f, memoryWriteRatePerSecond: 0f)]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    var weights = new float[NeuralBrainGenome.DirectWeightCount];
    weights[NeuralBrainSchema.MemoryForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisibleFoodDensityInput] = 4f;
    var brainId = simulation.State.AddBrain(new NeuralBrainGenome(weights));

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    creature.MemoryVector = new SimVector2(1f, 0f);
    creature.Senses = new CreatureSenseState { VisibleFoodDensity = 1f };
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    creature = simulation.State.Creatures[0];
    AssertTrue(creature.Actions.MemoryForward > 0.9f, "Food cue should still request memory write");
    AssertClose(MathF.Exp(-1f), creature.MemoryVector.Length, 0.000001, "Memory should decay by configured rate");
    AssertTrue(creature.MemoryVector.X < 0.4f, "Zero write rate should prevent refreshing memory");
}

static void ForagerPredatorTurnsCreatureProximityIntoAttackIntent()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 307,
        systems: [new NeuralControllerSystem()]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = 10f,
        MaxTurnRadiansPerSecond = 4f,
        DietaryAdaptation = 1f,
        MaturityAgeSeconds = 0f
    });
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateForagerPredator());
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);
    var creature = simulation.State.Creatures[0];
    var sectors = default(VisionSectorSet);
    sectors.AddCreature(VisionSectorSet.CenterSectorIndex, 1f, relativeBodySize: -0.35f);
    creature.Senses = new CreatureSenseState
    {
        Hunger = 1f,
        CreatureDetected = true,
        VisibleCreatureDensity = 0.125f,
        VisionSectors = sectors
    };
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    AssertTrue(simulation.State.Creatures[0].Actions.WantsAttack, "Forager predator should attack when a visible creature is very close");
}

static void ForagerPredatorTurnsCreatureContactIntoAttackIntent()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 308,
        systems: [new NeuralControllerSystem()]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = 10f,
        MaxTurnRadiansPerSecond = 4f,
        DietaryAdaptation = 1f,
        MaturityAgeSeconds = 0f
    });
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateForagerPredator());
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);
    var creature = simulation.State.Creatures[0];
    creature.Senses = new CreatureSenseState
    {
        Hunger = 1f,
        CreatureContact = 1f
    };
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    creature = simulation.State.Creatures[0];
    AssertTrue(creature.Actions.WantsAttack, "Forager predator should attack when creature contact says it has arrived");
    AssertTrue(creature.Actions.MoveForward < 0.5f, "Forager predator should slow down when already in creature contact");
}

static void ForagerPredatorSuppressesSimilarCreatureContactAttack()
{
    var unlikeContact = MeasureForagerPredatorActions(
        new CreatureSenseState
        {
            Hunger = 1f,
            CreatureContact = 1f,
            CreatureContactSimilarity = 0.2f
        });
    var similarContact = MeasureForagerPredatorActions(
        new CreatureSenseState
        {
            Hunger = 1f,
            CreatureContact = 1f,
            CreatureContactSimilarity = 0.9f
        });

    AssertTrue(unlikeContact.WantsAttack, "Predator should still attack low-similarity creature contact");
    AssertTrue(!similarContact.WantsAttack, "Predator should suppress attack against high-similarity creature contact");
    AssertTrue(
        unlikeContact.AttackOutput > similarContact.AttackOutput + 1f,
        "Creature contact similarity should strongly reduce predator attack output");
}

static void ForagerPredatorSteersAwayFromSimilarCreatureScent()
{
    var baseline = MeasureForagerPredatorActions(new CreatureSenseState { Hunger = 0.5f });
    var similarAhead = MeasureForagerPredatorActions(
        new CreatureSenseState
        {
            Hunger = 0.5f,
            CreatureSimilarityScentDetected = true,
            CreatureSimilarityScentDensity = 0.8f,
            CreatureSimilarityScentDirectionForward = 0.8f
        });
    var similarRight = MeasureForagerPredatorActions(
        new CreatureSenseState
        {
            Hunger = 0.5f,
            CreatureSimilarityScentDetected = true,
            CreatureSimilarityScentDensity = 0.8f,
            CreatureSimilarityScentDirectionRight = 0.8f
        });

    AssertTrue(
        similarAhead.MoveForward < baseline.MoveForward - 0.18f,
        "Predator should slow down when similar creature scent is directly ahead");
    AssertTrue(similarRight.Turn < -0.2f, "Predator should turn away from similar creature scent on the right");
}

static CreatureActionState MeasureForagerPredatorActions(CreatureSenseState senses)
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 309,
        systems: [new NeuralControllerSystem()]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = 10f,
        MaxTurnRadiansPerSecond = 4f,
        DietaryAdaptation = 1f,
        MaturityAgeSeconds = 0f
    });
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateForagerPredator(NeuralBrainSchema.DefaultHiddenNodeCount));
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);
    var creature = simulation.State.Creatures[0];
    creature.Senses = senses;
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    return simulation.State.Creatures[0].Actions;
}

static void SectorForagerStarterFollowsSectorPlantCues()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 411,
        systems: [new NeuralControllerSystem()]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaturityAgeSeconds = 0f
    });
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSectorForager());

    var rightSectors = default(VisionSectorSet);
    rightSectors.AddPlant(8, 0.8f);
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);
    var creature = simulation.State.Creatures[0];
    creature.Senses = new CreatureSenseState
    {
        Hunger = 1f,
        VisionSectors = rightSectors
    };
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    creature = simulation.State.Creatures[0];
    AssertTrue(creature.Actions.Turn > 0.9f, "Sector forager should turn toward plants in right-side sectors");
    AssertTrue(creature.Actions.MoveForward > 0.55f, "Sector forager should keep searching while hungry");
    AssertTrue(!creature.Actions.WantsEat, "Right-side plant proximity alone should not trigger strong eating");

    var centerSectors = default(VisionSectorSet);
    centerSectors.AddPlant(VisionSectorSet.CenterSectorIndex, 1f);
    creature.Senses = new CreatureSenseState
    {
        Hunger = 1f,
        VisionSectors = centerSectors
    };
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    creature = simulation.State.Creatures[0];
    AssertTrue(Math.Abs(creature.Actions.Turn) < 0.1f, "Centered sector plant should not produce a lateral turn");
    AssertTrue(creature.Actions.MoveForward > 0.85f, "Sector forager should drive toward centered plant-sector cues until body contact");
    AssertTrue(!creature.Actions.WantsEat, "Sector forager should use visual sectors for approach, not generic eating");

    creature.Senses = new CreatureSenseState
    {
        Hunger = 1f,
        FoodContact = 1f,
        PlantFoodContact = 1f
    };
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    creature = simulation.State.Creatures[0];
    AssertTrue(creature.Actions.WantsEat, "Sector forager should keep eating when body contact says it has arrived");
    AssertTrue(creature.Actions.MoveForward < 0.1f, "Sector forager should pause when body contact says it has arrived");

    creature.Senses = new CreatureSenseState
    {
        Hunger = 1f,
        FoodContact = 1f,
        EggFoodContact = 1f
    };
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    creature = simulation.State.Creatures[0];
    AssertTrue(!creature.Actions.WantsEat, "Sector forager should not treat egg contact like plant contact");
}

static void ScavengerStarterFollowsSectorMeatCues()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 415,
        systems: [new NeuralControllerSystem()]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        DietaryAdaptation = 0.3f,
        CarrionAdaptation = 0.4f,
        MaturityAgeSeconds = 0f
    });
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateScavengerForager());

    var rightSectors = default(VisionSectorSet);
    rightSectors.AddMeat(8, 0.8f);
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);
    var creature = simulation.State.Creatures[0];
    creature.Senses = new CreatureSenseState
    {
        Hunger = 1f,
        VisionSectors = rightSectors
    };
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    creature = simulation.State.Creatures[0];
    AssertTrue(creature.Actions.Turn > 0.6f, "Scavenger starter should turn toward meat in right-side sectors");
    AssertTrue(creature.Actions.MoveForward > 0.7f, "Scavenger starter should move toward sector meat cues");
}

static void PredatorStarterFollowsSectorCreatureCues()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 416,
        systems: [new NeuralControllerSystem()]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        DietaryAdaptation = 0.25f,
        MaturityAgeSeconds = 0f
    });
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateForagerPredator());

    var rightSectors = default(VisionSectorSet);
    rightSectors.AddCreature(8, 0.9f, relativeBodySize: -0.5f);
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);
    var creature = simulation.State.Creatures[0];
    creature.Senses = new CreatureSenseState
    {
        Hunger = 1f,
        VisionSectors = rightSectors
    };
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    creature = simulation.State.Creatures[0];
    AssertTrue(creature.Actions.Turn > 0.6f, "Predator starter should turn toward smaller creatures in right-side sectors");
    AssertTrue(creature.Actions.MoveForward > 0.6f, "Predator starter should move toward smaller creature sector cues");
    AssertTrue(creature.Actions.WantsAttack, "Predator starter should attack smaller creatures in strong right-side sector cues");
}

static void OpportunisticForagerSamplesMeatOnContact()
{
    AssertMeatContactTriggersEat(NeuralBrainGenome.CreateOpportunisticForager(), "Opportunistic forager");
}

static void SeedForagerSlowsDownNearFood()
{
    var farMove = MeasureSeedForagerMove(resourcePosition: new SimVector2(100f, 20f));
    var closeMove = MeasureSeedForagerMove(resourcePosition: new SimVector2(24f, 20f));

    AssertTrue(farMove > 0.85f, "Forager should move quickly toward distant food");
    AssertTrue(closeMove < 0.6f, "Forager should slow down near food");
    AssertTrue(farMove > closeMove + 0.25f, "Food proximity should reduce forward movement");
}

static void BehaviorAssaySummarizesSeedForagerResponses()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 401, systems: []);
    var herbivoreGenomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        DietaryAdaptation = 0.1f,
        MaturityAgeSeconds = 0f
    });
    var meatGenomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        DietaryAdaptation = 1f,
        MaturityAgeSeconds = 0f
    });
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager());

    simulation.State.SpawnCreature(herbivoreGenomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);
    simulation.State.SpawnCreature(meatGenomeId, new SimVector2(30f, 20f), energy: 25f, brainId: brainId);

    var summary = BehaviorAssay.Analyze(simulation.State);

    AssertEqual(2, summary.EvaluatedCreatureCount, "Assayed creature count");
    AssertEqual(47, summary.Results.Count, "Assay result count");
    AssertTrue(summary.PlantAhead.MoveForward > summary.Baseline.MoveForward, "Plant ahead should increase movement");
    AssertTrue(summary.PlantRight.Turn > 0.5f, "Plant right should turn right");
    AssertTrue(summary.PlantContact.EatShare > 0.9f, "Plant contact should trigger eating");
    AssertTrue(summary.RichPlantRight.Turn > 0.5f, "Rich plant right should turn right");
    AssertTrue(summary.TenderPlantContact.EatShare > 0.9f, "Tender plant contact should trigger eating");
    AssertTrue(summary.RichPlantContact.EatShare > 0.9f, "Rich plant contact should trigger eating");
    AssertTrue(summary.ToughPlantContact.EatShare > 0.9f, "Tough plant contact should trigger eating");
    AssertClose(
        summary.RichPlantRight.Turn,
        summary.RichTraceRichRight.Turn,
        0.000001,
        "Seed forager should not have built-in recent rich payoff steering");
    AssertClose(
        summary.Baseline.MoveForward,
        summary.PlantPreferenceAhead.MoveForward,
        0.000001,
        "Seed forager should not have built-in plant preference approach");
    AssertClose(
        summary.Baseline.Turn,
        summary.PlantPreferenceRight.Turn,
        0.000001,
        "Seed forager should not have built-in plant preference turning");
    AssertTrue(summary.ReproductionReady.ReproduceShare > 0.9f, "Ready creatures should lay eggs");
    AssertTrue(summary.CreatureAhead.AttackShare < 0.1f, "Seed forager should not arrive with built-in attack behavior");
    AssertEqual("little terrain differentiation", summary.TerrainResponse, "Seed forager should not arrive with built-in terrain response");
    AssertClose(0f, summary.FreshMeatPreferenceScore, 0.000001, "Seed forager fresh meat score");
    AssertClose(0f, summary.RottenScentAvoidanceScore, 0.000001, "Seed forager rot scent score");
    AssertEqual("little freshness differentiation", summary.RottenMeatResponse, "Seed forager should not arrive with built-in rot response");
}

static void BehaviorAssayReportsPlantChoiceProbes()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 409, systems: []);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaturityAgeSeconds = 0f
    });
    var weights = new float[NeuralBrainGenome.DirectWeightCount];
    weights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisiblePlantBiteEaseInput] = 3.5f;
    weights[NeuralBrainSchema.EatOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.PlantFoodContactBiteEaseInput] = 3.5f;
    var brainId = simulation.State.AddBrain(new NeuralBrainGenome(weights));

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);

    var summary = BehaviorAssay.Analyze(simulation.State);

    AssertTrue(
        summary.TenderPlantAhead.MoveForward > summary.ToughPlantAhead.MoveForward + 0.8f,
        "Plant-choice probe should expose bite-ease differences in visible plant cues");
    AssertTrue(
        summary.RichPlantAhead.MoveForward > summary.ToughPlantAhead.MoveForward + 0.3f,
        "Rich plant visual bite ease should be distinguishable from tough plant cues");
    AssertTrue(
        summary.TenderPlantContact.EatShare > summary.ToughPlantContact.EatShare,
        "Plant-choice probe should expose bite-ease differences in contact cues");
}

static void BehaviorAssayReportsPlantPreferenceBridgeProbes()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 410, systems: []);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaturityAgeSeconds = 0f
    });
    var weights = new float[NeuralBrainGenome.DirectWeightCount];
    weights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.PlantPreferenceForwardInput] = 4f;
    weights[NeuralBrainSchema.TurnOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.PlantPreferenceRightInput] = 4f;
    weights[NeuralBrainSchema.EatOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.PlantFoodContactPreferenceInput] = 4f;
    var brainId = simulation.State.AddBrain(new NeuralBrainGenome(weights));

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);

    var summary = BehaviorAssay.Analyze(simulation.State);

    AssertTrue(
        summary.PlantPreferenceAhead.MoveForward > summary.Baseline.MoveForward + 0.8f,
        "Plant preference bridge ahead should expose forward preference wiring");
    AssertTrue(
        summary.PlantPreferenceRight.Turn > summary.Baseline.Turn + 0.8f,
        "Plant preference bridge right should expose turning preference wiring");
    AssertTrue(
        summary.RichPreferenceRichRight.Turn > summary.RichPreferenceToughRight.Turn + 0.5f,
        "Rich payoff bridge should distinguish matching rich plants from mismatched tough plants");
    AssertTrue(
        summary.TenderPreferenceTenderRight.Turn > summary.Baseline.Turn + 0.8f,
        "Tender payoff bridge should expose matching tender preference");
    AssertTrue(
        summary.PlantPreferenceContact.EatShare > summary.RichPlantContact.EatShare,
        "Plant preference contact should expose contact preference wiring beyond ordinary plant contact");
}

static void BehaviorAssayDetectsFreshMeatPreference()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 403, systems: []);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaturityAgeSeconds = 0f
    });
    var weights = new float[NeuralBrainGenome.DirectWeightCount];
    weights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisibleMeatFreshnessInput] = 3f;
    weights[NeuralBrainSchema.TurnOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisibleMeatFreshnessInput] = 3f;
    var brainId = simulation.State.AddBrain(new NeuralBrainGenome(weights));

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);

    var summary = BehaviorAssay.Analyze(simulation.State);

    AssertTrue(summary.FreshMeatPreferenceScore > 0.3f, "Freshness-sensitive probe should prefer fresh meat cues");
    AssertEqual("prefers fresh meat", summary.RottenMeatResponse, "Freshness-sensitive rot response");
}

static void BehaviorAssayDetectsRottenScentAvoidance()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 404, systems: []);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaturityAgeSeconds = 0f
    });
    var weights = new float[NeuralBrainGenome.DirectWeightCount];
    weights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.MeatScentForwardInput] = 3f;
    weights[NeuralBrainSchema.TurnOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.MeatScentRightInput] = 3f;
    weights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.RottenMeatScentForwardInput] = -4f;
    weights[NeuralBrainSchema.TurnOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.RottenMeatScentRightInput] = -4f;
    var brainId = simulation.State.AddBrain(new NeuralBrainGenome(weights));

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);

    var summary = BehaviorAssay.Analyze(simulation.State);

    AssertTrue(summary.RottenScentAvoidanceScore > 0.8f, "Rot-scent-sensitive probe should prefer clean meat scent");
    AssertEqual("avoids rot scent", summary.RottenMeatResponse, "Rot-scent-sensitive rot response");
}

static void BrainInputDiagnosticsSummarizeFreshnessWiring()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 405, systems: []);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    var weights = new float[NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount: 1)];
    weights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisibleMeatFreshnessInput] = 2f;
    weights[NeuralBrainSchema.EatOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisibleMeatFreshnessInput] = -1f;
    weights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.RottenMeatScentForwardInput] = -4f;
    weights[NeuralBrainSchema.TurnOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.RottenMeatScentRightInput] = -3f;
    weights[NeuralBrainSchema.AttackOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisionSectorSmallerCreatureProximityInput(VisionSectorSet.CenterSectorIndex)] = 3f;
    weights[NeuralBrainSchema.TurnOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisionSectorSimilarCreatureDensityInput(VisionSectorSet.CenterSectorIndex)] = 1.8f;
    weights[NeuralBrainSchema.AttackOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisionSectorLargerCreatureProximityInput(VisionSectorSet.CenterSectorIndex)] = -2.4f;
    weights[NeuralBrainSchema.AttackOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisionSectorCreatureApproachRateInput(VisionSectorSet.CenterSectorIndex)] = 2.7f;
    weights[NeuralBrainSchema.AttackOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisionSectorCreatureFacingAlignmentInput(VisionSectorSet.CenterSectorIndex)] = -1.5f;
    weights[NeuralBrainGenome.DirectWeightCount + NeuralBrainSchema.VisibleMeatFreshnessInput] = 0.5f;
    weights[NeuralBrainGenome.DirectWeightCount + NeuralBrainSchema.RottenMeatScentDensityInput] = -0.75f;
    weights[NeuralBrainGenome.DirectWeightCount + NeuralBrainSchema.VisionSectorSmallerCreatureDensityInput(VisionSectorSet.CenterSectorIndex)] = 0.9f;
    weights[NeuralBrainGenome.DirectWeightCount + NeuralBrainSchema.VisionSectorSimilarCreatureDensityInput(VisionSectorSet.CenterSectorIndex)] = 0.6f;
    weights[NeuralBrainGenome.DirectWeightCount + NeuralBrainSchema.VisionSectorLargerCreatureDensityInput(VisionSectorSet.CenterSectorIndex)] = -1.2f;
    weights[NeuralBrainGenome.DirectWeightCount + NeuralBrainSchema.VisionSectorCreatureApproachRateInput(VisionSectorSet.CenterSectorIndex)] = 0.8f;
    weights[NeuralBrainGenome.DirectWeightCount + NeuralBrainSchema.VisionSectorCreatureFacingAlignmentInput(VisionSectorSet.CenterSectorIndex)] = -0.4f;
    var brainId = simulation.State.AddBrain(new NeuralBrainGenome(weights));

    var founderId = simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);

    var summary = BrainInputDiagnostics.Analyze(simulation.State);
    var lineages = BrainInputDiagnostics.AnalyzeTopFounderLineages(simulation.State, 10);
    const int CreatureSizeSectorInputCount = VisionSectorSet.SectorCount * 2;
    const int DirectCreatureSizeSectorDivisor = NeuralBrainSchema.OutputCount * CreatureSizeSectorInputCount;
    const int CreatureMotionSectorInputCount = VisionSectorSet.SectorCount;
    const int DirectCreatureMotionSectorDivisor = NeuralBrainSchema.OutputCount * CreatureMotionSectorInputCount;

    AssertEqual(1, summary.EvaluatedCreatureCount, "Brain diagnostic evaluated count");
    AssertClose(3f / NeuralBrainSchema.OutputCount, summary.DirectFreshnessWeightMagnitude, 0.000001, "Direct freshness magnitude");
    AssertClose(7f / (NeuralBrainSchema.OutputCount * 3f), summary.DirectRotScentWeightMagnitude, 0.000001, "Direct rot magnitude");
    AssertClose(3f / DirectCreatureSizeSectorDivisor, summary.DirectSmallerCreatureSectorWeightMagnitude, 0.000001, "Direct smaller creature sector magnitude");
    AssertClose(1.8f / DirectCreatureSizeSectorDivisor, summary.DirectSimilarCreatureSectorWeightMagnitude, 0.000001, "Direct similar creature sector magnitude");
    AssertClose(2.4f / DirectCreatureSizeSectorDivisor, summary.DirectLargerCreatureSectorWeightMagnitude, 0.000001, "Direct larger creature sector magnitude");
    AssertClose(2.7f / DirectCreatureMotionSectorDivisor, summary.DirectCreatureApproachSectorWeightMagnitude, 0.000001, "Direct creature approach sector magnitude");
    AssertClose(1.5f / DirectCreatureMotionSectorDivisor, summary.DirectCreatureFacingSectorWeightMagnitude, 0.000001, "Direct creature facing sector magnitude");
    AssertClose(0.5f, summary.HiddenFreshnessWeightMagnitude, 0.000001, "Hidden freshness magnitude");
    AssertClose(0.25f, summary.HiddenRotScentWeightMagnitude, 0.000001, "Hidden rot magnitude");
    AssertClose(0.9f / CreatureSizeSectorInputCount, summary.HiddenSmallerCreatureSectorWeightMagnitude, 0.000001, "Hidden smaller creature sector magnitude");
    AssertClose(0.6f / CreatureSizeSectorInputCount, summary.HiddenSimilarCreatureSectorWeightMagnitude, 0.000001, "Hidden similar creature sector magnitude");
    AssertClose(1.2f / CreatureSizeSectorInputCount, summary.HiddenLargerCreatureSectorWeightMagnitude, 0.000001, "Hidden larger creature sector magnitude");
    AssertClose(0.8f / CreatureMotionSectorInputCount, summary.HiddenCreatureApproachSectorWeightMagnitude, 0.000001, "Hidden creature approach sector magnitude");
    AssertClose(0.4f / CreatureMotionSectorInputCount, summary.HiddenCreatureFacingSectorWeightMagnitude, 0.000001, "Hidden creature facing sector magnitude");
    AssertClose(2f, summary.MoveFreshnessWeight, 0.000001, "Move freshness weight");
    AssertClose(-1f, summary.EatFreshnessWeight, 0.000001, "Eat freshness weight");
    AssertClose(-4f, summary.MoveRotScentForwardWeight, 0.000001, "Move rot forward weight");
    AssertClose(-3f, summary.TurnRotScentRightWeight, 0.000001, "Turn rot right weight");
    AssertClose(3f / CreatureSizeSectorInputCount, summary.AttackSmallerCreatureSectorWeight, 0.000001, "Attack smaller creature sector weight");
    AssertClose(-2.4f / CreatureSizeSectorInputCount, summary.AttackLargerCreatureSectorWeight, 0.000001, "Attack larger creature sector weight");
    AssertClose(2.7f / CreatureMotionSectorInputCount, summary.AttackCreatureApproachSectorWeight, 0.000001, "Attack creature approach sector weight");
    AssertClose(-1.5f / CreatureMotionSectorInputCount, summary.AttackCreatureFacingSectorWeight, 0.000001, "Attack creature facing sector weight");

    AssertEqual(1, lineages.Count, "Lineage diagnostic count");
    AssertEqual(founderId, lineages[0].FounderId, "Lineage diagnostic founder");
    AssertClose(summary.DirectRotScentWeightMagnitude, lineages[0].Diagnostics.DirectRotScentWeightMagnitude, 0.000001, "Lineage rot magnitude");
    AssertClose(summary.DirectLargerCreatureSectorWeightMagnitude, lineages[0].Diagnostics.DirectLargerCreatureSectorWeightMagnitude, 0.000001, "Lineage larger creature sector magnitude");
    AssertClose(summary.DirectCreatureApproachSectorWeightMagnitude, lineages[0].Diagnostics.DirectCreatureApproachSectorWeightMagnitude, 0.000001, "Lineage approach sector magnitude");
}

static void SpeciesBrainInputDiagnosticsSummarizeClusterWiring()
{
    var scenario = new SimulationScenario
    {
        Seed = 916,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f,
        WorldWidth = 300f,
        WorldHeight = 100f,
        ResourceVoidBorderWidth = 0f
    };
    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    var wiredWeights = new float[NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount: 1)];
    wiredWeights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisibleMeatFreshnessInput] = 2f;
    wiredWeights[NeuralBrainSchema.EatOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisibleMeatFreshnessInput] = -1f;
    wiredWeights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.RottenMeatScentForwardInput] = -4f;
    wiredWeights[NeuralBrainSchema.TurnOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.RottenMeatScentRightInput] = -3f;
    wiredWeights[NeuralBrainSchema.AttackOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisionSectorSmallerCreatureProximityInput(VisionSectorSet.CenterSectorIndex)] = 3f;
    wiredWeights[NeuralBrainSchema.TurnOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisionSectorSimilarCreatureDensityInput(VisionSectorSet.CenterSectorIndex)] = 1.8f;
    wiredWeights[NeuralBrainSchema.AttackOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisionSectorLargerCreatureProximityInput(VisionSectorSet.CenterSectorIndex)] = -2.4f;
    wiredWeights[NeuralBrainSchema.AttackOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisionSectorCreatureApproachRateInput(VisionSectorSet.CenterSectorIndex)] = 2.7f;
    wiredWeights[NeuralBrainSchema.AttackOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.VisionSectorCreatureFacingAlignmentInput(VisionSectorSet.CenterSectorIndex)] = -1.5f;
    wiredWeights[NeuralBrainGenome.DirectWeightCount + NeuralBrainSchema.VisibleMeatFreshnessInput] = 0.5f;
    wiredWeights[NeuralBrainGenome.DirectWeightCount + NeuralBrainSchema.RottenMeatScentDensityInput] = -0.75f;
    wiredWeights[NeuralBrainGenome.DirectWeightCount + NeuralBrainSchema.VisionSectorSmallerCreatureDensityInput(VisionSectorSet.CenterSectorIndex)] = 0.9f;
    wiredWeights[NeuralBrainGenome.DirectWeightCount + NeuralBrainSchema.VisionSectorSimilarCreatureDensityInput(VisionSectorSet.CenterSectorIndex)] = 0.6f;
    wiredWeights[NeuralBrainGenome.DirectWeightCount + NeuralBrainSchema.VisionSectorLargerCreatureDensityInput(VisionSectorSet.CenterSectorIndex)] = -1.2f;
    wiredWeights[NeuralBrainGenome.DirectWeightCount + NeuralBrainSchema.VisionSectorCreatureApproachRateInput(VisionSectorSet.CenterSectorIndex)] = 0.8f;
    wiredWeights[NeuralBrainGenome.DirectWeightCount + NeuralBrainSchema.VisionSectorCreatureFacingAlignmentInput(VisionSectorSet.CenterSectorIndex)] = -0.4f;
    var wiredBrainId = simulation.State.AddBrain(new NeuralBrainGenome(wiredWeights));
    var quietBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateZero(hiddenNodeCount: 1));

    simulation.State.SpawnCreature(genomeId, new SimVector2(30f, 30f), energy: 35f, brainId: wiredBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(40f, 30f), energy: 35f, brainId: wiredBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(180f, 30f), energy: 35f, brainId: quietBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(190f, 30f), energy: 35f, brainId: quietBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(200f, 30f), energy: 35f, brainId: quietBrainId);

    var summaries = SpeciesClusterAnalyzer.AnalyzeBrainInputDiagnostics(simulation.State, 10);
    const int CreatureSizeSectorInputCount = VisionSectorSet.SectorCount * 2;
    const int DirectCreatureSizeSectorDivisor = NeuralBrainSchema.OutputCount * CreatureSizeSectorInputCount;
    const int CreatureMotionSectorInputCount = VisionSectorSet.SectorCount;
    const int DirectCreatureMotionSectorDivisor = NeuralBrainSchema.OutputCount * CreatureMotionSectorInputCount;

    AssertEqual(2, summaries.Count, "Species diagnostic cluster count");
    AssertTrue(summaries.All(summary => summary.Diagnostics.EvaluatedCreatureCount == summary.LivingCreatures), "Species diagnostics should evaluate all living neural creatures in each cluster");

    var wired = summaries.Single(summary => summary.Diagnostics.DirectFreshnessWeightMagnitude > 0.1f);
    AssertEqual(2, wired.LivingCreatures, "Wired species diagnostic living count");
    AssertClose(3f / NeuralBrainSchema.OutputCount, wired.Diagnostics.DirectFreshnessWeightMagnitude, 0.000001, "Species direct freshness magnitude");
    AssertClose(7f / (NeuralBrainSchema.OutputCount * 3f), wired.Diagnostics.DirectRotScentWeightMagnitude, 0.000001, "Species direct rot magnitude");
    AssertClose(3f / DirectCreatureSizeSectorDivisor, wired.Diagnostics.DirectSmallerCreatureSectorWeightMagnitude, 0.000001, "Species direct smaller creature sector magnitude");
    AssertClose(1.8f / DirectCreatureSizeSectorDivisor, wired.Diagnostics.DirectSimilarCreatureSectorWeightMagnitude, 0.000001, "Species direct similar creature sector magnitude");
    AssertClose(2.4f / DirectCreatureSizeSectorDivisor, wired.Diagnostics.DirectLargerCreatureSectorWeightMagnitude, 0.000001, "Species direct larger creature sector magnitude");
    AssertClose(2.7f / DirectCreatureMotionSectorDivisor, wired.Diagnostics.DirectCreatureApproachSectorWeightMagnitude, 0.000001, "Species direct creature approach sector magnitude");
    AssertClose(1.5f / DirectCreatureMotionSectorDivisor, wired.Diagnostics.DirectCreatureFacingSectorWeightMagnitude, 0.000001, "Species direct creature facing sector magnitude");
    AssertClose(0.5f, wired.Diagnostics.HiddenFreshnessWeightMagnitude, 0.000001, "Species hidden freshness magnitude");
    AssertClose(0.25f, wired.Diagnostics.HiddenRotScentWeightMagnitude, 0.000001, "Species hidden rot magnitude");
    AssertClose(0.9f / CreatureSizeSectorInputCount, wired.Diagnostics.HiddenSmallerCreatureSectorWeightMagnitude, 0.000001, "Species hidden smaller creature sector magnitude");
    AssertClose(0.6f / CreatureSizeSectorInputCount, wired.Diagnostics.HiddenSimilarCreatureSectorWeightMagnitude, 0.000001, "Species hidden similar creature sector magnitude");
    AssertClose(1.2f / CreatureSizeSectorInputCount, wired.Diagnostics.HiddenLargerCreatureSectorWeightMagnitude, 0.000001, "Species hidden larger creature sector magnitude");
    AssertClose(0.8f / CreatureMotionSectorInputCount, wired.Diagnostics.HiddenCreatureApproachSectorWeightMagnitude, 0.000001, "Species hidden creature approach sector magnitude");
    AssertClose(0.4f / CreatureMotionSectorInputCount, wired.Diagnostics.HiddenCreatureFacingSectorWeightMagnitude, 0.000001, "Species hidden creature facing sector magnitude");
    AssertClose(-4f, wired.Diagnostics.MoveRotScentForwardWeight, 0.000001, "Species move rot forward weight");
    AssertClose(3f / CreatureSizeSectorInputCount, wired.Diagnostics.AttackSmallerCreatureSectorWeight, 0.000001, "Species attack smaller creature sector weight");
    AssertClose(-2.4f / CreatureSizeSectorInputCount, wired.Diagnostics.AttackLargerCreatureSectorWeight, 0.000001, "Species attack larger creature sector weight");
    AssertClose(2.7f / CreatureMotionSectorInputCount, wired.Diagnostics.AttackCreatureApproachSectorWeight, 0.000001, "Species attack creature approach sector weight");
    AssertClose(-1.5f / CreatureMotionSectorInputCount, wired.Diagnostics.AttackCreatureFacingSectorWeight, 0.000001, "Species attack creature facing sector weight");

    var quiet = summaries.Single(summary => summary.Diagnostics.DirectFreshnessWeightMagnitude == 0f);
    AssertEqual(3, quiet.LivingCreatures, "Quiet species diagnostic living count");
    AssertClose(0f, quiet.Diagnostics.DirectRotScentWeightMagnitude, 0.000001, "Quiet species rot magnitude");
    AssertClose(0f, quiet.Diagnostics.DirectSmallerCreatureSectorWeightMagnitude, 0.000001, "Quiet species smaller creature sector magnitude");
    AssertClose(0f, quiet.Diagnostics.DirectCreatureApproachSectorWeightMagnitude, 0.000001, "Quiet species creature approach sector magnitude");
}

static void ExplorerForagerKeepsSearchingWithoutFoodCues()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 402, systems: []);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaturityAgeSeconds = 0f
    });
    var seedBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager());
    var explorerBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateExplorerForager());

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: seedBrainId);
    var seedSummary = BehaviorAssay.Analyze(simulation.State);

    simulation.State.Creatures.Clear();
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: explorerBrainId);
    var explorerSummary = BehaviorAssay.Analyze(simulation.State);

    AssertTrue(
        explorerSummary.HungryNoCue.MoveForward > seedSummary.HungryNoCue.MoveForward + 0.5f,
        "Explorer forager should search harder than seed forager when hungry and no food is visible");
    AssertTrue(explorerSummary.HungryNoCue.MoveForward > 0.75f, "Explorer forager should cruise when hungry and no food is visible");
    AssertTrue(Math.Abs(explorerSummary.HungryNoCue.Turn) < 0.2f, "Explorer forager no-cue search should not be a tight circle");
    AssertTrue(explorerSummary.FedNoCue.MoveForward < explorerSummary.HungryNoCue.MoveForward - 0.25f, "Explorer forager search should remain hunger-sensitive");
    AssertTrue(explorerSummary.PlantRight.Turn > 0.5f, "Explorer forager should still turn toward visible plant cues");
    AssertTrue(explorerSummary.PlantContact.EatShare > 0.9f, "Explorer forager should still eat reachable plants on contact");
    AssertTrue(explorerSummary.CreatureAhead.AttackShare < 0.1f, "Explorer forager should not arrive with built-in attack behavior");
    AssertEqual("hunger-driven cruising", explorerSummary.SearchTendency, "Explorer forager search tendency");
}

static void BehaviorAssaySummarizesTerrainResponse()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 405, systems: []);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaturityAgeSeconds = 0f
    });
    var weights = new float[NeuralBrainSchema.InputCount * NeuralBrainSchema.OutputCount];
    weights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.BiasInput] = 1.0f;
    weights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.ForwardTerrainDragInput] = -4.0f;
    var brainId = simulation.State.AddBrain(new NeuralBrainGenome(weights));

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);

    var summary = BehaviorAssay.Analyze(simulation.State);

    AssertTrue(summary.Baseline.MoveForward > 0.7f, "Probe brain should cruise without terrain cue");
    AssertTrue(summary.SlowTerrainAhead.MoveForward < 0.1f, "Probe brain should slow down when rough terrain is ahead");
    AssertEqual("avoids slow terrain ahead", summary.TerrainResponse, "Terrain response classification");
}

static void BehaviorAssaySummarizesLateralTerrainResponse()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 406, systems: []);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaturityAgeSeconds = 0f
    });
    var weights = new float[NeuralBrainSchema.InputCount * NeuralBrainSchema.OutputCount];
    weights[NeuralBrainSchema.TurnOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.LeftTerrainDragInput] = 4.0f;
    weights[NeuralBrainSchema.TurnOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.RightTerrainDragInput] = -4.0f;
    var brainId = simulation.State.AddBrain(new NeuralBrainGenome(weights));

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);

    var summary = BehaviorAssay.Analyze(simulation.State);

    AssertTrue(summary.SlowTerrainLeft.Turn > 0.9f, "Probe brain should turn right away from rough terrain on the left");
    AssertTrue(summary.SlowTerrainRight.Turn < -0.9f, "Probe brain should turn left away from rough terrain on the right");
    AssertTrue(summary.EasierTerrainLeft.Turn < -0.9f, "Probe brain should turn toward easier terrain on the left");
    AssertTrue(summary.EasierTerrainRight.Turn > 0.9f, "Probe brain should turn toward easier terrain on the right");
    AssertEqual("steers toward easier terrain", summary.TerrainResponse, "Lateral terrain response classification");
}

static void NeuralBrainMigratesReproductiveContextInputs()
{
    const int legacyInputCount = 33;
    const int legacyOutputCount = 5;
    const int oldReproductionReadinessInput = 17;
    const int oldRightTerrainDragInput = 32;
    var legacyWeights = new float[legacyInputCount * legacyOutputCount];
    legacyWeights[NeuralBrainSchema.ReproduceOutput * legacyInputCount + oldReproductionReadinessInput] = 2.5f;
    legacyWeights[NeuralBrainSchema.TurnOutput * legacyInputCount + oldRightTerrainDragInput] = -1.25f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(0, brain.HiddenNodeCount, "Migrated brain hidden node count");
    AssertEqual(NeuralBrainGenome.DirectWeightCount, brain.Weights.Length, "Migrated brain weight count");
    AssertClose(
        2.5f,
        brain.GetWeight(NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.ReproductionReadinessInput),
        0.000001,
        "Migrated reproduction readiness weight");
    AssertClose(
        -1.25f,
        brain.GetWeight(NeuralBrainSchema.TurnOutput, NeuralBrainSchema.RightTerrainDragInput),
        0.000001,
        "Migrated terrain drag weight");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.EnergySurplusInput), 0.000001, "New energy surplus weight starts neutral");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.RecentFoodSuccessInput), 0.000001, "New food success weight starts neutral");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.MemoryForwardInput), 0.000001, "New memory input weight starts neutral");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.MemoryForwardOutput, NeuralBrainSchema.BiasInput), 0.000001, "New memory output starts neutral");
}

static void NeuralBrainMigratesMemoryInputsAndOutputs()
{
    const int legacyInputCount = 35;
    const int legacyOutputCount = 5;
    const int hiddenNodeCount = 4;
    const int oldRecentFoodSuccessInput = 34;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCount + oldRecentFoodSuccessInput] = -0.75f;
    legacyWeights[legacyHiddenInputOffset + NeuralBrainSchema.BiasInput] = 1.25f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.TurnOutput * hiddenNodeCount] = -2.5f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Migrated hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Migrated hidden brain weight count");
    AssertClose(
        -0.75f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RecentFoodSuccessInput),
        0.000001,
        "Migrated direct weight");
    AssertClose(1.25f, brain.GetHiddenInputWeight(0, NeuralBrainSchema.BiasInput), 0.000001, "Migrated hidden input weight");
    AssertClose(-2.5f, brain.GetHiddenOutputWeight(NeuralBrainSchema.TurnOutput, 0), 0.000001, "Migrated hidden output weight");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.MemoryForwardInput), 0.000001, "New memory input remains neutral");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.MemoryRightOutput, NeuralBrainSchema.BiasInput), 0.000001, "New memory output remains neutral");
}

static void NeuralBrainMigratesRottenMeatSensingInputs()
{
    const int legacyInputCount = 38;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 4;
    const int oldMemoryForwardOutput = 5;
    const int oldRecentFoodSuccessInput = 34;
    const int oldMemoryStrengthInput = 37;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCount + oldMemoryStrengthInput] = 0.8f;
    legacyWeights[legacyHiddenInputOffset + oldRecentFoodSuccessInput] = -1.5f;
    legacyWeights[legacyHiddenOutputOffset + oldMemoryForwardOutput * hiddenNodeCount] = 2.25f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Rotten sensing migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Rotten sensing migrated weight count");
    AssertClose(
        0.8f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.MemoryStrengthInput),
        0.000001,
        "Existing memory-strength direct weight remains in place");
    AssertClose(-1.5f, brain.GetHiddenInputWeight(0, NeuralBrainSchema.RecentFoodSuccessInput), 0.000001, "Existing hidden input remains in place");
    AssertClose(2.25f, brain.GetHiddenOutputWeight(NeuralBrainSchema.MemoryForwardOutput, 0), 0.000001, "Existing hidden output remains in place");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.VisibleMeatFreshnessInput), 0.000001, "New visible meat freshness input starts neutral");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RottenMeatScentDensityInput), 0.000001, "New rotten meat scent density input starts neutral");
    AssertClose(0f, brain.GetHiddenInputWeight(0, NeuralBrainSchema.RottenMeatScentForwardInput), 0.000001, "New hidden rotten scent direction input starts neutral");
}

static void NeuralBrainMigratesObstacleSensingInputs()
{
    const int legacyInputCount = 42;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 4;
    const int oldMemoryRightOutput = 6;
    const int oldVisibleMeatFreshnessInput = 38;
    const int oldRottenMeatScentRightInput = 41;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCount + oldRottenMeatScentRightInput] = -0.9f;
    legacyWeights[legacyHiddenInputOffset + oldVisibleMeatFreshnessInput] = 1.2f;
    legacyWeights[legacyHiddenOutputOffset + oldMemoryRightOutput * hiddenNodeCount] = -2.1f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Obstacle sensing migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Obstacle sensing migrated weight count");
    AssertClose(
        -0.9f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RottenMeatScentRightInput),
        0.000001,
        "Existing rot-scent direct weight remains in place");
    AssertClose(1.2f, brain.GetHiddenInputWeight(0, NeuralBrainSchema.VisibleMeatFreshnessInput), 0.000001, "Existing hidden freshness input remains in place");
    AssertClose(-2.1f, brain.GetHiddenOutputWeight(NeuralBrainSchema.MemoryRightOutput, 0), 0.000001, "Existing hidden output remains in place");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.ForwardObstacleInput), 0.000001, "New obstacle forward input starts neutral");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.TurnOutput, NeuralBrainSchema.LeftObstacleInput), 0.000001, "New obstacle left input starts neutral");
    AssertClose(0f, brain.GetHiddenInputWeight(0, NeuralBrainSchema.MovementBlockedInput), 0.000001, "New hidden blocked input starts neutral");
}

static void NeuralBrainMigratesSectorVisionInputs()
{
    const int legacyInputCount = 46;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 2;
    const int oldForwardObstacleInput = 42;
    const int oldMovementBlockedInput = 45;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCount + oldMovementBlockedInput] = -0.8f;
    legacyWeights[legacyHiddenInputOffset + oldForwardObstacleInput] = 1.3f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.TurnOutput * hiddenNodeCount] = -2.2f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Sector vision migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Sector vision migrated weight count");
    AssertClose(
        -0.8f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.MovementBlockedInput),
        0.000001,
        "Existing blocked movement direct weight remains in place");
    AssertClose(1.3f, brain.GetHiddenInputWeight(0, NeuralBrainSchema.ForwardObstacleInput), 0.000001, "Existing obstacle hidden input remains in place");
    AssertClose(-2.2f, brain.GetHiddenOutputWeight(NeuralBrainSchema.TurnOutput, 0), 0.000001, "Existing hidden output remains in place");
    AssertClose(
        0f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.VisionSectorPlantDensityInput(VisionSectorSet.CenterSectorIndex)),
        0.000001,
        "New sector plant density input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.VisionSectorMeatProximityInput(VisionSectorSet.CenterSectorIndex)),
        0.000001,
        "New hidden sector meat proximity input starts neutral");
}

static void NeuralBrainMigratesNearestAggregateSchema()
{
    const int legacyInputCount = 239;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 2;
    const int oldFoodForwardInput = 4;
    const int oldVisibleFoodDensityInput = 6;
    const int oldCreatureForwardInput = 20;
    const int oldMemoryStrengthInput = 37;
    const int oldFoodContactInput = 208;
    const int oldRightHabitatQualityInput = 238;
    var oldCenterPlantProximityInput = 46
        + VisionSectorSet.CenterSectorIndex * 18
        + 1;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCount + oldFoodForwardInput] = 5.0f;
    legacyWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCount + oldVisibleFoodDensityInput] = 1.2f;
    legacyWeights[NeuralBrainSchema.TurnOutput * legacyInputCount + oldCenterPlantProximityInput] = 2.3f;
    legacyWeights[NeuralBrainSchema.EatOutput * legacyInputCount + oldFoodContactInput] = 3.4f;
    legacyWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCount + oldRightHabitatQualityInput] = -0.7f;
    legacyWeights[legacyHiddenInputOffset + oldCreatureForwardInput] = 1.5f;
    legacyWeights[legacyHiddenInputOffset + oldMemoryStrengthInput] = 0.8f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.AttackOutput * hiddenNodeCount] = 1.1f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Nearest aggregate migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Nearest aggregate migrated weight count");
    AssertClose(
        0f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.VisiblePlantDensityInput),
        0.000001,
        "Removed nearest food direction input is dropped");
    AssertClose(
        1.2f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.VisibleFoodDensityInput),
        0.000001,
        "Existing visible food density shifts into current slot");
    AssertClose(
        2.3f,
        brain.GetWeight(
            NeuralBrainSchema.TurnOutput,
            NeuralBrainSchema.VisionSectorPlantProximityInput(VisionSectorSet.CenterSectorIndex)),
        0.000001,
        "Existing full sector input shifts into current sector range");
    AssertClose(
        3.4f,
        brain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.FoodContactInput),
        0.000001,
        "Existing food contact input shifts after shortened sector prefix");
    AssertClose(
        -0.7f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RightHabitatQualityInput),
        0.000001,
        "Existing habitat input shifts into current habitat range");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.VisibleCreatureDensityInput),
        0.000001,
        "Removed nearest creature direction hidden input is dropped");
    AssertClose(
        0.8f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.MemoryStrengthInput),
        0.000001,
        "Existing memory strength hidden input shifts into current slot");
    AssertClose(1.1f, brain.GetHiddenOutputWeight(NeuralBrainSchema.AttackOutput, 0), 0.000001, "Existing hidden output remains in place");
}

static void NeuralBrainMigratesFoodContactInput()
{
    const int legacyInputCount = 118;
    const int legacyOutputCount = 7;
    const int legacySectorChannelCount = 8;
    var legacyWeights = new float[legacyInputCount * legacyOutputCount];
    var oldCenterPlantInput = 46 + VisionSectorSet.CenterSectorIndex * legacySectorChannelCount + 1;
    var centerPlantInput = NeuralBrainSchema.VisionSectorPlantProximityInput(VisionSectorSet.CenterSectorIndex);
    legacyWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCount + oldCenterPlantInput] = 2.4f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(0, brain.HiddenNodeCount, "Food contact migration hidden node count");
    AssertEqual(NeuralBrainGenome.DirectWeightCount, brain.Weights.Length, "Food contact migrated weight count");
    AssertClose(
        2.4f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, centerPlantInput),
        0.000001,
        "Existing sector direct weight remains in place");
    AssertClose(
        0f,
        brain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.FoodContactInput),
        0.000001,
        "New food contact input starts neutral");

    const int legacyContactInputCount = 119;
    const int oldFoodContactInput = 118;
    var legacyContactWeights = new float[legacyContactInputCount * legacyOutputCount];
    legacyContactWeights[NeuralBrainSchema.EatOutput * legacyContactInputCount + oldFoodContactInput] = 3.3f;

    var contactBrain = new NeuralBrainGenome(legacyContactWeights);

    AssertEqual(NeuralBrainGenome.DirectWeightCount, contactBrain.Weights.Length, "Food contact kind migrated weight count");
    AssertClose(
        3.3f,
        contactBrain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.FoodContactInput),
        0.000001,
        "Existing generic food contact weight remains in place");
    AssertClose(
        0f,
        contactBrain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.PlantFoodContactInput),
        0.000001,
        "New plant contact input starts neutral");
}

static void NeuralBrainMigratesHealthRatioInput()
{
    const int legacyInputCount = 122;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 3;
    const int oldPlantFoodContactInput = 119;
    const int oldEggFoodContactInput = 121;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.EatOutput * legacyInputCount + oldEggFoodContactInput] = 2.8f;
    legacyWeights[legacyHiddenInputOffset + oldPlantFoodContactInput] = 1.6f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.MoveForwardOutput * hiddenNodeCount] = -1.1f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Health ratio migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Health ratio migrated weight count");
    AssertClose(
        2.8f,
        brain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.EggFoodContactInput),
        0.000001,
        "Existing egg contact direct weight remains in place");
    AssertClose(
        1.6f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.PlantFoodContactInput),
        0.000001,
        "Existing plant contact hidden input remains in place");
    AssertClose(
        -1.1f,
        brain.GetHiddenOutputWeight(NeuralBrainSchema.MoveForwardOutput, 0),
        0.000001,
        "Existing hidden output remains in place");
    AssertClose(
        0f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.HealthRatioInput),
        0.000001,
        "New health ratio direct input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.HealthRatioInput),
        0.000001,
        "New health ratio hidden input starts neutral");

    const int legacyInputCountWithHealth = 123;
    const int oldHealthRatioInput = 122;
    var legacyHealthWeights = new float[legacyInputCountWithHealth * legacyOutputCount];
    legacyHealthWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCountWithHealth + oldHealthRatioInput] = -1.7f;

    var healthBrain = new NeuralBrainGenome(legacyHealthWeights);

    AssertEqual(NeuralBrainGenome.DirectWeightCount, healthBrain.Weights.Length, "Creature sector size migration weight count");
    AssertClose(
        -1.7f,
        healthBrain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.HealthRatioInput),
        0.000001,
        "Existing health ratio input remains in place after creature sector size inputs are added");
    AssertClose(
        0f,
        healthBrain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.VisionSectorSmallerCreatureDensityInput(0)),
        0.000001,
        "New creature size sector input starts neutral");
}

static void NeuralBrainMigratesCreatureSectorMotionInputs()
{
    const int legacyInputCount = 177;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 2;
    const int legacySectorChannelCount = 14;
    const int oldFoodContactInput = 172;
    const int oldHealthRatioInput = 176;
    var oldCenterLargeCreatureProximityInput = 46
        + VisionSectorSet.CenterSectorIndex * legacySectorChannelCount
        + 13;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCount + oldCenterLargeCreatureProximityInput] = 2.2f;
    legacyWeights[NeuralBrainSchema.EatOutput * legacyInputCount + oldFoodContactInput] = 3.3f;
    legacyWeights[legacyHiddenInputOffset + oldHealthRatioInput] = -1.4f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.AttackOutput * hiddenNodeCount] = 1.1f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Creature sector motion migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Creature sector motion migrated weight count");
    AssertClose(
        2.2f,
        brain.GetWeight(
            NeuralBrainSchema.MoveForwardOutput,
            NeuralBrainSchema.VisionSectorLargerCreatureProximityInput(VisionSectorSet.CenterSectorIndex)),
        0.000001,
        "Existing larger-creature sector input remains in place");
    AssertClose(
        3.3f,
        brain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.FoodContactInput),
        0.000001,
        "Existing food contact input remains in place");
    AssertClose(
        -1.4f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.HealthRatioInput),
        0.000001,
        "Existing hidden health input remains in place");
    AssertClose(
        1.1f,
        brain.GetHiddenOutputWeight(NeuralBrainSchema.AttackOutput, 0),
        0.000001,
        "Existing hidden attack output remains in place");
    AssertClose(
        0f,
        brain.GetWeight(
            NeuralBrainSchema.MoveForwardOutput,
            NeuralBrainSchema.VisionSectorCreatureApproachRateInput(VisionSectorSet.CenterSectorIndex)),
        0.000001,
        "New sector creature approach input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(
            0,
            NeuralBrainSchema.VisionSectorCreatureFacingAlignmentInput(VisionSectorSet.CenterSectorIndex)),
        0.000001,
        "New hidden sector creature facing input starts neutral");
}

static void NeuralBrainMigratesCreatureContactInput()
{
    const int legacyInputCount = 195;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 2;
    const int oldHealthRatioInput = 194;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCount + oldHealthRatioInput] = -2.2f;
    legacyWeights[legacyHiddenInputOffset + oldHealthRatioInput] = 1.4f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.AttackOutput * hiddenNodeCount] = 1.1f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Creature contact migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Creature contact migrated weight count");
    AssertClose(
        -2.2f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.HealthRatioInput),
        0.000001,
        "Existing health ratio direct input remains in place");
    AssertClose(
        1.4f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.HealthRatioInput),
        0.000001,
        "Existing hidden health input remains in place");
    AssertClose(
        1.1f,
        brain.GetHiddenOutputWeight(NeuralBrainSchema.AttackOutput, 0),
        0.000001,
        "Existing hidden attack output remains in place");
    AssertClose(
        0f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.CreatureContactInput),
        0.000001,
        "New creature contact direct input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.CreatureContactInput),
        0.000001,
        "New creature contact hidden input starts neutral");
}

static void NeuralBrainMigratesPlantQualityInputs()
{
    const int legacyInputCount = 196;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 3;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    const int oldCreatureContactInput = 194;
    const int oldHealthRatioInput = 195;
    legacyWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCount + oldHealthRatioInput] = -0.7f;
    legacyWeights[legacyHiddenInputOffset + oldCreatureContactInput] = 1.2f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.EatOutput * hiddenNodeCount] = 2.4f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Plant quality migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Plant quality migrated weight count");
    AssertClose(
        -0.7f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.HealthRatioInput),
        0.000001,
        "Existing health direct input remains in place");
    AssertClose(
        1.2f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.CreatureContactInput),
        0.000001,
        "Existing creature contact hidden input remains in place");
    AssertClose(
        2.4f,
        brain.GetHiddenOutputWeight(NeuralBrainSchema.EatOutput, 0),
        0.000001,
        "Existing hidden eat output remains in place");
    AssertClose(
        0f,
        brain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.VisiblePlantEnergyQualityInput),
        0.000001,
        "New visible plant energy quality input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.PlantFoodContactBiteEaseInput),
        0.000001,
        "New contact plant bite ease hidden input starts neutral");
    AssertClose(
        0f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RecentPlantEnergyYieldInput),
        0.000001,
        "New recent plant energy yield input starts neutral");
}

static void NeuralBrainMigratesRecentFoodEnergyYieldInput()
{
    const int legacyInputCount = 202;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 2;
    const int oldRecentPlantRawYieldInput = 200;
    const int oldRecentPlantEnergyYieldInput = 201;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCount + oldRecentPlantEnergyYieldInput] = -1.2f;
    legacyWeights[legacyHiddenInputOffset + oldRecentPlantRawYieldInput] = 0.8f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.ReproduceOutput * hiddenNodeCount] = 1.4f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Food energy yield migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Food energy yield migrated weight count");
    AssertClose(
        -1.2f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RecentPlantEnergyYieldInput),
        0.000001,
        "Existing recent plant energy yield direct input remains in place");
    AssertClose(
        0.8f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.RecentPlantRawYieldInput),
        0.000001,
        "Existing recent plant raw hidden input remains in place");
    AssertClose(
        1.4f,
        brain.GetHiddenOutputWeight(NeuralBrainSchema.ReproduceOutput, 0),
        0.000001,
        "Existing hidden reproduce output remains in place");
    AssertClose(
        0f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RecentFoodEnergyYieldInput),
        0.000001,
        "New recent food energy yield direct input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.RecentFoodEnergyYieldInput),
        0.000001,
        "New recent food energy yield hidden input starts neutral");
}

static void NeuralBrainMigratesSectorPlantQualityInputs()
{
    const int legacyInputCount = 203;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 2;
    const int legacySectorChannelCount = 16;
    const int oldCenterMeatProximityInput = 46
        + VisionSectorSet.CenterSectorIndex * legacySectorChannelCount
        + 3;
    const int oldRecentFoodEnergyYieldInput = 202;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.TurnOutput * legacyInputCount + oldCenterMeatProximityInput] = 1.9f;
    legacyWeights[legacyHiddenInputOffset + oldRecentFoodEnergyYieldInput] = 0.7f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.EatOutput * hiddenNodeCount] = 1.3f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Sector plant quality migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Sector plant quality migrated weight count");
    AssertClose(
        1.9f,
        brain.GetWeight(NeuralBrainSchema.TurnOutput, NeuralBrainSchema.VisionSectorMeatProximityInput(VisionSectorSet.CenterSectorIndex)),
        0.000001,
        "Existing sector meat proximity input remains in place");
    AssertClose(
        0.7f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.RecentFoodEnergyYieldInput),
        0.000001,
        "Existing recent food energy yield hidden input remains in place");
    AssertClose(
        1.3f,
        brain.GetHiddenOutputWeight(NeuralBrainSchema.EatOutput, 0),
        0.000001,
        "Existing hidden eat output remains in place");
    AssertClose(
        0f,
        brain.GetWeight(NeuralBrainSchema.TurnOutput, NeuralBrainSchema.VisionSectorPlantEnergyQualityInput(VisionSectorSet.CenterSectorIndex)),
        0.000001,
        "New sector plant energy quality direct input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.VisionSectorPlantBiteEaseInput(VisionSectorSet.CenterSectorIndex)),
        0.000001,
        "New sector plant bite ease hidden input starts neutral");
}

static void NeuralBrainMigratesTypedPlantEnergyYieldInputs()
{
    const int legacyInputCount = 221;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 2;
    const int oldRecentFoodEnergyYieldInput = 220;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.TurnOutput * legacyInputCount + oldRecentFoodEnergyYieldInput] = -0.9f;
    legacyWeights[legacyHiddenInputOffset + oldRecentFoodEnergyYieldInput] = 1.1f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.MoveForwardOutput * hiddenNodeCount] = 1.5f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Typed plant yield migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Typed plant yield migrated weight count");
    AssertClose(
        -0.9f,
        brain.GetWeight(NeuralBrainSchema.TurnOutput, NeuralBrainSchema.RecentFoodEnergyYieldInput),
        0.000001,
        "Existing recent food energy yield direct input remains in place");
    AssertClose(
        1.1f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.RecentFoodEnergyYieldInput),
        0.000001,
        "Existing recent food energy yield hidden input remains in place");
    AssertClose(
        1.5f,
        brain.GetHiddenOutputWeight(NeuralBrainSchema.MoveForwardOutput, 0),
        0.000001,
        "Existing hidden movement output remains in place");
    AssertClose(
        0f,
        brain.GetWeight(NeuralBrainSchema.TurnOutput, NeuralBrainSchema.RecentTenderPlantEnergyYieldInput),
        0.000001,
        "New recent tender plant yield input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.RecentRichPlantEnergyYieldInput),
        0.000001,
        "New recent rich plant yield hidden input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.RecentToughPlantEnergyYieldInput),
        0.000001,
        "New recent tough plant yield hidden input starts neutral");
}

static void NeuralBrainMigratesPlantPayoffTraceInputs()
{
    const int legacyInputCount = 224;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 2;
    const int oldRecentRichPlantEnergyYieldInput = 222;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.EatOutput * legacyInputCount + oldRecentRichPlantEnergyYieldInput] = 0.9f;
    legacyWeights[legacyHiddenInputOffset + oldRecentRichPlantEnergyYieldInput] = -1.1f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.TurnOutput * hiddenNodeCount] = 1.6f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Plant payoff trace migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Plant payoff trace migrated weight count");
    AssertClose(
        0.9f,
        brain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.RecentRichPlantEnergyYieldInput),
        0.000001,
        "Existing rich plant yield direct input remains in place");
    AssertClose(
        -1.1f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.RecentRichPlantEnergyYieldInput),
        0.000001,
        "Existing rich plant yield hidden input remains in place");
    AssertClose(
        1.6f,
        brain.GetHiddenOutputWeight(NeuralBrainSchema.TurnOutput, 0),
        0.000001,
        "Existing hidden turn output remains in place");
    AssertClose(
        0f,
        brain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.TenderPlantPayoffTraceInput),
        0.000001,
        "New tender plant payoff trace input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.RichPlantPayoffTraceInput),
        0.000001,
        "New rich plant payoff trace hidden input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.ToughPlantPayoffTraceInput),
        0.000001,
        "New tough plant payoff trace hidden input starts neutral");
}

static void NeuralBrainMigratesPlantPreferenceBridgeInputs()
{
    const int legacyInputCount = 227;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 2;
    const int oldRichPlantPayoffTraceInput = 225;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCount + oldRichPlantPayoffTraceInput] = 0.8f;
    legacyWeights[legacyHiddenInputOffset + oldRichPlantPayoffTraceInput] = -1.2f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.TurnOutput * hiddenNodeCount] = 1.7f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Plant preference bridge migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Plant preference bridge migrated weight count");
    AssertClose(
        0.8f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RichPlantPayoffTraceInput),
        0.000001,
        "Existing rich plant payoff trace direct input remains in place");
    AssertClose(
        -1.2f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.RichPlantPayoffTraceInput),
        0.000001,
        "Existing rich plant payoff trace hidden input remains in place");
    AssertClose(
        1.7f,
        brain.GetHiddenOutputWeight(NeuralBrainSchema.TurnOutput, 0),
        0.000001,
        "Existing hidden turn output remains in place");
    AssertClose(
        0f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.PlantPreferenceDensityInput),
        0.000001,
        "New plant preference density direct input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.PlantPreferenceRightInput),
        0.000001,
        "New plant preference right hidden input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.PlantFoodContactPreferenceInput),
        0.000001,
        "New plant contact preference hidden input starts neutral");
}

static void NeuralBrainMigratesCreatureSimilarityInputs()
{
    const int legacyInputCount = 231;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 2;
    const int oldPlantFoodContactPreferenceInput = 230;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.EatOutput * legacyInputCount + oldPlantFoodContactPreferenceInput] = 1.3f;
    legacyWeights[legacyHiddenInputOffset + oldPlantFoodContactPreferenceInput] = -0.9f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.MoveForwardOutput * hiddenNodeCount] = 1.6f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Creature similarity migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Creature similarity migrated weight count");
    AssertClose(
        1.3f,
        brain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.PlantFoodContactPreferenceInput),
        0.000001,
        "Existing plant contact preference direct input remains in place");
    AssertClose(
        -0.9f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.PlantFoodContactPreferenceInput),
        0.000001,
        "Existing plant contact preference hidden input remains in place");
    AssertClose(
        1.6f,
        brain.GetHiddenOutputWeight(NeuralBrainSchema.MoveForwardOutput, 0),
        0.000001,
        "Existing hidden movement output remains in place");
    AssertClose(
        0f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.CreatureSimilarityScentDensityInput),
        0.000001,
        "New creature similarity scent density input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.CreatureSimilarityScentRightInput),
        0.000001,
        "New creature similarity scent right hidden input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.CreatureContactSimilarityInput),
        0.000001,
        "New creature contact similarity hidden input starts neutral");
}

static void NeuralBrainMigratesHabitatQualityInputs()
{
    const int legacyInputCount = 235;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 2;
    const int oldCreatureContactSimilarityInput = 234;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.EatOutput * legacyInputCount + oldCreatureContactSimilarityInput] = 1.2f;
    legacyWeights[legacyHiddenInputOffset + oldCreatureContactSimilarityInput] = -0.8f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.MoveForwardOutput * hiddenNodeCount] = 1.5f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Habitat quality migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Habitat quality migrated weight count");
    AssertClose(
        1.2f,
        brain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.CreatureContactSimilarityInput),
        0.000001,
        "Existing creature contact similarity direct input remains in place");
    AssertClose(
        -0.8f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.CreatureContactSimilarityInput),
        0.000001,
        "Existing creature contact similarity hidden input remains in place");
    AssertClose(
        1.5f,
        brain.GetHiddenOutputWeight(NeuralBrainSchema.MoveForwardOutput, 0),
        0.000001,
        "Existing hidden movement output remains in place");
    AssertClose(
        0f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.ForwardHabitatQualityInput),
        0.000001,
        "New forward habitat quality direct input starts neutral");
    AssertClose(
        0f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.RightHabitatQualityInput),
        0.000001,
        "New right habitat quality hidden input starts neutral");
}

static void NeuralBrainMigratesGrabOutputAndInputs()
{
    const int legacyInputCount = NeuralBrainSchema.RightHabitatQualityInput + 1;
    const int legacyOutputCount = 7;
    const int hiddenNodeCount = 3;
    const int oldMemoryForwardOutput = 5;
    const int oldMemoryRightOutput = 6;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[oldMemoryForwardOutput * legacyInputCount + NeuralBrainSchema.BiasInput] = 1.25f;
    legacyWeights[oldMemoryRightOutput * legacyInputCount + NeuralBrainSchema.BiasInput] = -1.5f;
    legacyWeights[NeuralBrainSchema.AttackOutput * legacyInputCount + NeuralBrainSchema.RightHabitatQualityInput] = 0.8f;
    legacyWeights[legacyHiddenInputOffset + NeuralBrainSchema.RightHabitatQualityInput] = -0.4f;
    legacyWeights[legacyHiddenOutputOffset + oldMemoryForwardOutput * hiddenNodeCount] = 2.5f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Grab migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Grab migrated weight count");
    AssertClose(
        1.25f,
        brain.GetWeight(NeuralBrainSchema.MemoryForwardOutput, NeuralBrainSchema.BiasInput),
        0.000001,
        "Old memory-forward output shifts past grab and sound");
    AssertClose(
        -1.5f,
        brain.GetWeight(NeuralBrainSchema.MemoryRightOutput, NeuralBrainSchema.BiasInput),
        0.000001,
        "Old memory-right output shifts past grab and sound");
    AssertClose(
        0.8f,
        brain.GetWeight(NeuralBrainSchema.AttackOutput, NeuralBrainSchema.RightHabitatQualityInput),
        0.000001,
        "Existing action output remains in place");
    AssertClose(
        -0.4f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.RightHabitatQualityInput),
        0.000001,
        "Existing hidden habitat input remains in place");
    AssertClose(
        2.5f,
        brain.GetHiddenOutputWeight(NeuralBrainSchema.MemoryForwardOutput, 0),
        0.000001,
        "Old hidden memory output shifts past grab and sound");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.GrabOutput, NeuralBrainSchema.BiasInput), 0.000001, "New grab output starts neutral");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.SoundAmplitudeOutput, NeuralBrainSchema.BiasInput), 0.000001, "New sound amplitude output starts neutral");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.SoundToneOutput, NeuralBrainSchema.BiasInput), 0.000001, "New sound tone output starts neutral");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.GrabPressureInput), 0.000001, "New grab pressure input starts neutral");
    AssertClose(0f, brain.GetHiddenInputWeight(0, NeuralBrainSchema.IsHoldingCreatureInput), 0.000001, "New holding input starts neutral");
}

static void NeuralBrainMigratesSoundOutputAndInputs()
{
    const int legacyInputCount = NeuralBrainSchema.IsHoldingCreatureInput + 1;
    const int legacyOutputCount = 8;
    const int hiddenNodeCount = 3;
    const int oldMemoryForwardOutput = 6;
    const int oldMemoryRightOutput = 7;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.GrabOutput * legacyInputCount + NeuralBrainSchema.CanGrabCreatureInput] = 1.4f;
    legacyWeights[oldMemoryForwardOutput * legacyInputCount + NeuralBrainSchema.BiasInput] = 1.7f;
    legacyWeights[oldMemoryRightOutput * legacyInputCount + NeuralBrainSchema.BiasInput] = -1.8f;
    legacyWeights[legacyHiddenInputOffset + NeuralBrainSchema.IsHoldingCreatureInput] = 0.9f;
    legacyWeights[legacyHiddenOutputOffset + oldMemoryForwardOutput * hiddenNodeCount] = 2.2f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Sound migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Sound migrated weight count");
    AssertClose(
        1.4f,
        brain.GetWeight(NeuralBrainSchema.GrabOutput, NeuralBrainSchema.CanGrabCreatureInput),
        0.000001,
        "Existing grab output remains in place");
    AssertClose(
        1.7f,
        brain.GetWeight(NeuralBrainSchema.MemoryForwardOutput, NeuralBrainSchema.BiasInput),
        0.000001,
        "Old memory-forward output shifts past sound");
    AssertClose(
        -1.8f,
        brain.GetWeight(NeuralBrainSchema.MemoryRightOutput, NeuralBrainSchema.BiasInput),
        0.000001,
        "Old memory-right output shifts past sound");
    AssertClose(
        0.9f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.IsHoldingCreatureInput),
        0.000001,
        "Existing hidden grab input remains in place");
    AssertClose(
        2.2f,
        brain.GetHiddenOutputWeight(NeuralBrainSchema.MemoryForwardOutput, 0),
        0.000001,
        "Old hidden memory output shifts past sound");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.SoundAmplitudeOutput, NeuralBrainSchema.BiasInput), 0.000001, "New sound amplitude output starts neutral");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.SoundToneOutput, NeuralBrainSchema.BiasInput), 0.000001, "New sound tone output starts neutral");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.SoundDensityInput), 0.000001, "New sound density input starts neutral");
    AssertClose(0f, brain.GetHiddenInputWeight(0, NeuralBrainSchema.SoundToneClarityInput), 0.000001, "New sound clarity input starts neutral");
}

static void NeuralBrainMigratesFatInputs()
{
    const int legacyInputCount = NeuralBrainSchema.SoundToneClarityInput + 1;
    const int legacyOutputCount = NeuralBrainSchema.OutputCount;
    const int hiddenNodeCount = 2;
    var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
    var legacyHiddenInputOffset = legacyDirectWeightCount;
    var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
    var legacyWeights = new float[legacyDirectWeightCount + hiddenNodeCount * (legacyInputCount + legacyOutputCount)];

    legacyWeights[NeuralBrainSchema.MoveForwardOutput * legacyInputCount + NeuralBrainSchema.SoundToneClarityInput] = 0.7f;
    legacyWeights[legacyHiddenInputOffset + NeuralBrainSchema.SoundToneClarityInput] = 0.4f;
    legacyWeights[legacyHiddenOutputOffset + NeuralBrainSchema.MoveForwardOutput * hiddenNodeCount] = 0.9f;

    var brain = new NeuralBrainGenome(legacyWeights);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Fat migration hidden node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Fat migrated weight count");
    AssertClose(
        0.7f,
        brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.SoundToneClarityInput),
        0.000001,
        "Existing sound clarity input remains in place");
    AssertClose(
        0.4f,
        brain.GetHiddenInputWeight(0, NeuralBrainSchema.SoundToneClarityInput),
        0.000001,
        "Existing hidden sound clarity input remains in place");
    AssertClose(
        0.9f,
        brain.GetHiddenOutputWeight(NeuralBrainSchema.MoveForwardOutput, 0),
        0.000001,
        "Existing hidden output remains in place");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.FatRatioInput), 0.000001, "New fat ratio input starts neutral");
    AssertClose(0f, brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.MassBurdenInput), 0.000001, "New mass burden input starts neutral");
    AssertClose(0f, brain.GetHiddenInputWeight(0, NeuralBrainSchema.FatRatioInput), 0.000001, "New hidden fat input starts neutral");
    AssertClose(0f, brain.GetHiddenInputWeight(0, NeuralBrainSchema.MassBurdenInput), 0.000001, "New hidden mass input starts neutral");
}

static void NeuralBrainSupportsHiddenNodes()
{
    const int hiddenNodeCount = 4;
    var weights = new float[NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount)];
    var hiddenInputOffset = NeuralBrainGenome.DirectWeightCount;
    var hiddenOutputOffset = NeuralBrainGenome.DirectWeightCount
        + hiddenNodeCount * NeuralBrainSchema.InputCount;

    weights[hiddenInputOffset + NeuralBrainSchema.BiasInput] = 2f;
    weights[hiddenOutputOffset + NeuralBrainSchema.MoveForwardOutput * hiddenNodeCount] = 2f;

    var brain = new NeuralBrainGenome(weights);
    Span<float> inputs = stackalloc float[NeuralBrainSchema.InputCount];
    Span<float> outputs = stackalloc float[NeuralBrainSchema.OutputCount];
    inputs[NeuralBrainSchema.BiasInput] = 1f;

    brain.Evaluate(inputs, outputs);

    AssertEqual(hiddenNodeCount, brain.HiddenNodeCount, "Hidden brain node count");
    AssertEqual(NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount), brain.Weights.Length, "Hidden brain weight count");
    AssertTrue(outputs[NeuralBrainSchema.MoveForwardOutput] > 0.9f, "Hidden node should influence output");
    AssertClose(2f, brain.GetHiddenInputWeight(0, NeuralBrainSchema.BiasInput), 0.000001, "Hidden input weight");
    AssertClose(2f, brain.GetHiddenOutputWeight(NeuralBrainSchema.MoveForwardOutput, 0), 0.000001, "Hidden output weight");
    AssertEqual(hiddenNodeCount * NeuralBrainSchema.InputCount, brain.HiddenInputWeightCount, "Hidden input weight count");
    AssertEqual(hiddenNodeCount * NeuralBrainSchema.OutputCount, brain.HiddenOutputWeightCount, "Hidden output weight count");
    AssertClose(2f, brain.SumAbsoluteHiddenInputWeights(), 0.000001, "Hidden input weight magnitude");
    AssertClose(2f, brain.SumAbsoluteHiddenOutputWeights(), 0.000001, "Hidden output weight magnitude");
    AssertEqual(1, brain.CountActiveHiddenOutputWeights(0.05f), "Active hidden output count");

    var inertHiddenWeights = new float[NeuralBrainGenome.GetExpectedWeightCount(hiddenNodeCount)];
    inertHiddenWeights[NeuralBrainSchema.MoveForwardOutput * NeuralBrainSchema.InputCount + NeuralBrainSchema.BiasInput] = 0.5f;
    inertHiddenWeights[hiddenInputOffset + NeuralBrainSchema.BiasInput] = 8f;
    var inertHiddenBrain = new NeuralBrainGenome(inertHiddenWeights);
    outputs.Clear();
    inertHiddenBrain.Evaluate(inputs, outputs);
    AssertClose(
        MathF.Tanh(0.5f),
        outputs[NeuralBrainSchema.MoveForwardOutput],
        0.000001,
        "Hidden inputs without hidden outputs should not affect behavior");

    var seedBrain = NeuralBrainGenome.CreateSeedForager(hiddenNodeCount);
    AssertEqual(hiddenNodeCount, seedBrain.HiddenNodeCount, "Seed hidden node count");
    AssertClose(
        0f,
        seedBrain.GetHiddenOutputWeight(NeuralBrainSchema.MoveForwardOutput, 0),
        0.000001,
        "Seed hidden outputs start neutral");
    AssertTrue(
        Math.Abs(seedBrain.GetHiddenInputWeight(0, NeuralBrainSchema.VisionSectorPlantProximityInput(VisionSectorSet.CenterSectorIndex))) > 0.000001f,
        "Seed hidden concepts should be prewired on the input side");
}

static void BrainFactoryDescribesHybridNeuralArchitecture()
{
    var descriptor = BrainFactory.Describe(BrainArchitectureKind.HybridNeural);

    AssertEqual(BrainArchitectureKind.HybridNeural, descriptor.Kind, "Descriptor kind");
    AssertEqual("Hybrid neural", descriptor.Name, "Descriptor name");
    AssertEqual(NeuralBrainSchema.InputCount, descriptor.InputCount, "Descriptor input count");
    AssertEqual(NeuralBrainSchema.OutputCount, descriptor.OutputCount, "Descriptor output count");
    AssertEqual(NeuralBrainSchema.DefaultHiddenNodeCount, descriptor.DefaultHiddenNodeCount, "Descriptor default hidden nodes");
    AssertEqual(0, descriptor.MinHiddenNodeCount, "Descriptor min hidden nodes");
    AssertEqual(NeuralBrainSchema.MaxHiddenNodeCount, descriptor.MaxHiddenNodeCount, "Descriptor max hidden nodes");
    AssertTrue(descriptor.SupportsHiddenNodes, "Hybrid neural descriptor should support hidden nodes");
    AssertTrue(
        descriptor.SupportsDirectInputOutputWeights,
        "Hybrid neural descriptor should report direct input/output support");
    AssertThrows<ArgumentOutOfRangeException>(
        () => BrainFactory.Describe((BrainArchitectureKind)999),
        "Unsupported brain architecture should be rejected");
}

static void BrainFactoryPreservesHybridStarterBrains()
{
    const int hiddenNodeCount = 4;

    AssertBrainsClose(
        NeuralBrainGenome.CreateZero(hiddenNodeCount),
        BrainFactory.CreateZero(BrainArchitectureKind.HybridNeural, hiddenNodeCount),
        "Zero hybrid brain");
    AssertBrainsClose(
        NeuralBrainGenome.CreateSeedForager(hiddenNodeCount),
        BrainFactory.CreateStarter(BrainArchitectureKind.HybridNeural, InitialBrainKind.SeedForager, hiddenNodeCount),
        "Seed forager hybrid brain");
    AssertBrainsClose(
        NeuralBrainGenome.CreateExplorerForager(hiddenNodeCount),
        BrainFactory.CreateStarter(BrainArchitectureKind.HybridNeural, InitialBrainKind.ExplorerForager, hiddenNodeCount),
        "Explorer forager hybrid brain");
    AssertBrainsClose(
        NeuralBrainGenome.CreateSectorForager(hiddenNodeCount),
        BrainFactory.CreateStarter(BrainArchitectureKind.HybridNeural, InitialBrainKind.SectorForager, hiddenNodeCount),
        "Sector forager hybrid brain");
    AssertBrainsClose(
        NeuralBrainGenome.CreateOpportunisticForager(hiddenNodeCount),
        BrainFactory.CreateStarter(BrainArchitectureKind.HybridNeural, InitialBrainKind.OpportunisticForager, hiddenNodeCount),
        "Opportunistic forager hybrid brain");
    AssertBrainsClose(
        NeuralBrainGenome.CreateScavengerForager(hiddenNodeCount),
        BrainFactory.CreateStarter(BrainArchitectureKind.HybridNeural, InitialBrainKind.ScavengerForager, hiddenNodeCount),
        "Scavenger forager hybrid brain");
    AssertBrainsClose(
        NeuralBrainGenome.CreateFreshnessAwareScavenger(hiddenNodeCount),
        BrainFactory.CreateStarter(
            BrainArchitectureKind.HybridNeural,
            InitialBrainKind.FreshnessAwareScavenger,
            hiddenNodeCount),
        "Freshness-aware scavenger hybrid brain");
    AssertBrainsClose(
        NeuralBrainGenome.CreateForagerPredator(hiddenNodeCount),
        BrainFactory.CreateStarter(BrainArchitectureKind.HybridNeural, InitialBrainKind.ForagerPredator, hiddenNodeCount),
        "Forager predator hybrid brain");
    AssertBrainsClose(
        NeuralBrainGenome.CreateRandom(new DeterministicRandom(91), scale: 0.5f, hiddenNodeCount: hiddenNodeCount),
        BrainFactory.CreateRandom(
            BrainArchitectureKind.HybridNeural,
            new DeterministicRandom(91),
            scale: 0.5f,
            hiddenNodeCount: hiddenNodeCount),
        "Random hybrid brain");
    AssertThrows<ArgumentException>(
        () => BrainFactory.CreateStarter(
            BrainArchitectureKind.HybridNeural,
            InitialBrainKind.RandomPerFounder,
            hiddenNodeCount),
        "Random-per-founder should not be created as a shared starter");
}

static void BrainFactoryMutatesHybridNeuralBrains()
{
    var source = BrainFactory.CreateZero(BrainArchitectureKind.HybridNeural, hiddenNodeCount: 2);

    var unchanged = BrainFactory.Mutate(
        BrainArchitectureKind.HybridNeural,
        source,
        new DeterministicRandom(45),
        mutationStrength: 0.5f,
        mutationRate: 0f);
    AssertBrainsClose(source, unchanged, "Zero-rate mutation");

    var mutated = BrainFactory.Mutate(
        BrainArchitectureKind.HybridNeural,
        source,
        new DeterministicRandom(45),
        mutationStrength: 0.5f,
        mutationRate: 0.05f);
    AssertTrue(
        mutated.Weights.Zip(source.Weights).Any(pair => Math.Abs(pair.First - pair.Second) > 0.000001f),
        "Nonzero mutation should change at least one brain weight");
}

static void BrainFactorySupportsHiddenLayerNeuralArchitecture()
{
    var descriptor = BrainFactory.Describe(BrainArchitectureKind.HiddenLayerNeural);

    AssertEqual(BrainArchitectureKind.HiddenLayerNeural, descriptor.Kind, "Hidden descriptor kind");
    AssertEqual("Hidden-layer neural", descriptor.Name, "Hidden descriptor name");
    AssertEqual(NeuralBrainSchema.DefaultHiddenLayerNodeCount, descriptor.DefaultHiddenNodeCount, "Hidden default node count");
    AssertEqual(1, descriptor.MinHiddenNodeCount, "Hidden min node count");
    AssertTrue(descriptor.SupportsHiddenNodes, "Hidden architecture should support hidden nodes");
    AssertTrue(!descriptor.SupportsDirectInputOutputWeights, "Hidden architecture should not support direct weights");

    var zero = BrainFactory.CreateZero(BrainArchitectureKind.HiddenLayerNeural);
    AssertEqual(NeuralBrainSchema.DefaultHiddenLayerNodeCount, zero.HiddenNodeCount, "Default hidden-layer zero node count");
    AssertDirectWeightsZero(zero, "Hidden-layer zero direct weights");

    var random = BrainFactory.CreateRandom(
        BrainArchitectureKind.HiddenLayerNeural,
        new DeterministicRandom(118),
        scale: 0.5f);
    AssertEqual(NeuralBrainSchema.DefaultHiddenLayerNodeCount, random.HiddenNodeCount, "Default hidden-layer random node count");
    AssertDirectWeightsZero(random, "Hidden-layer random direct weights");
    AssertTrue(random.Weights.Skip(NeuralBrainGenome.DirectWeightCount).Any(weight => Math.Abs(weight) > 0.000001f), "Hidden-layer random hidden weights");

    var starter = BrainFactory.CreateStarter(
        BrainArchitectureKind.HiddenLayerNeural,
        InitialBrainKind.SectorForager);
    AssertEqual(NeuralBrainSchema.DefaultHiddenLayerNodeCount, starter.HiddenNodeCount, "Default hidden-layer starter node count");
    AssertDirectWeightsZero(starter, "Hidden-layer starter direct weights");
    AssertTrue(starter.CountActiveHiddenOutputWeights(0.05f) >= NeuralBrainSchema.OutputCount, "Hidden-layer starter should wire outputs through hidden nodes");

    Span<float> inputs = stackalloc float[NeuralBrainSchema.InputCount];
    Span<float> outputs = stackalloc float[NeuralBrainSchema.OutputCount];
    inputs[NeuralBrainSchema.BiasInput] = 1f;
    inputs[NeuralBrainSchema.HungerInput] = 1f;
    inputs[NeuralBrainSchema.VisionSectorPlantProximityInput(VisionSectorSet.CenterSectorIndex - 1)] = 1f;
    starter.Evaluate(inputs, outputs);
    AssertTrue(Math.Abs(outputs[NeuralBrainSchema.TurnOutput]) > 0.01f, "Hidden-layer starter should respond through hidden nodes");

    var unchanged = BrainFactory.Mutate(
        BrainArchitectureKind.HiddenLayerNeural,
        starter,
        new DeterministicRandom(119),
        mutationStrength: 0.5f,
        mutationRate: 0f);
    AssertBrainsClose(starter, unchanged, "Zero-rate hidden-layer mutation");

    var mutated = BrainFactory.Mutate(
        BrainArchitectureKind.HiddenLayerNeural,
        starter,
        new DeterministicRandom(119),
        mutationStrength: 0.5f,
        mutationRate: 0.05f);
    AssertDirectWeightsZero(mutated, "Mutated hidden-layer direct weights");
    AssertTrue(
        mutated.Weights.Skip(NeuralBrainGenome.DirectWeightCount)
            .Zip(starter.Weights.Skip(NeuralBrainGenome.DirectWeightCount))
            .Any(pair => Math.Abs(pair.First - pair.Second) > 0.000001f),
        "Hidden-layer mutation should change at least one hidden weight");
}

static void BrainFactorySupportsRtNeatGraphArchitecture()
{
    var descriptor = BrainFactory.Describe(BrainArchitectureKind.RtNeatGraph);

    AssertEqual(BrainArchitectureKind.RtNeatGraph, descriptor.Kind, "rtNEAT descriptor kind");
    AssertEqual("rtNEAT graph", descriptor.Name, "rtNEAT descriptor name");
    AssertTrue(descriptor.InputCount >= BrainIoRegistry.Inputs.Count, "rtNEAT should expose semantic inputs");
    AssertEqual(BrainIoRegistry.PhysicalActionOutputs.Count, descriptor.OutputCount, "rtNEAT output count");

    var starter = BrainFactory.CreateStarter(
        BrainArchitectureKind.RtNeatGraph,
        InitialBrainKind.SparseGraphForager);
    AssertEqual(BrainArchitectureKind.RtNeatGraph, starter.ArchitectureKind, "rtNEAT starter architecture");
    AssertTrue(starter.RtNeat is not null, "rtNEAT starter should include graph payload");
    AssertEqual(0, starter.HiddenNodeCount, "rtNEAT starter should begin without hidden nodes");
    AssertTrue(starter.WeightCount > 0, "rtNEAT starter should include sparse connections and output biases");

    Span<float> denseInputs = stackalloc float[NeuralBrainSchema.InputCount];
    Span<float> denseOutputs = stackalloc float[NeuralBrainSchema.OutputCount];
    var noFood = starter.Evaluate(
        BrainInputFrame.FromSenses(new CreatureSenseState(), CreatureGenome.Baseline),
        default,
        denseInputs,
        denseOutputs).Actions;
    var plantToRight = starter.Evaluate(
        BrainInputFrame.FromSenses(
            new CreatureSenseState
            {
                PlantDetected = true,
                PlantProximity = 0.35f,
                PlantDirectionForward = 0.6f,
                PlantDirectionRight = 0.7f,
                VisiblePlantDensity = 0.5f
            },
            CreatureGenome.Baseline),
        default,
        denseInputs,
        denseOutputs).Actions;
    var plantContact = starter.Evaluate(
        BrainInputFrame.FromSenses(
            new CreatureSenseState
            {
                PlantDetected = true,
                PlantProximity = 1f,
                PlantDirectionForward = 1f,
                FoodContact = 1f,
                PlantFoodContact = 1f,
                VisiblePlantDensity = 1f
            },
            CreatureGenome.Baseline),
        default,
        denseInputs,
        denseOutputs).Actions;
    var eggContact = starter.Evaluate(
        BrainInputFrame.FromSenses(
            new CreatureSenseState
            {
                FoodDetected = true,
                FoodProximity = 1f,
                FoodDirectionForward = 1f,
                FoodContact = 1f,
                EggFoodContact = 1f,
                VisibleFoodDensity = 1f
            },
            CreatureGenome.Baseline),
        default,
        denseInputs,
        denseOutputs).Actions;

    AssertTrue(noFood.MoveForward > 0.5f, "rtNEAT starter should search forward by default");
    AssertTrue(noFood.Eat < 0f, "rtNEAT starter should not eat without plant contact");
    AssertTrue(plantToRight.Turn > 0.5f, "rtNEAT starter should turn toward plants on the right");
    AssertTrue(plantContact.MoveForward < noFood.MoveForward, "rtNEAT starter should slow near contacted plants");
    AssertTrue(plantContact.Eat > 0.25f, "rtNEAT starter should eat contacted plants");
    AssertTrue(eggContact.Eat < 0f, "rtNEAT starter should not begin as an egg predator");

    var scavengerStarter = BrainFactory.CreateStarter(
        BrainArchitectureKind.RtNeatGraph,
        InitialBrainKind.SparseGraphScavenger);
    var meatToRight = scavengerStarter.Evaluate(
        BrainInputFrame.FromSenses(
            new CreatureSenseState
            {
                MeatDetected = true,
                MeatProximity = 0.4f,
                MeatDirectionForward = 0.5f,
                MeatDirectionRight = 0.8f,
                VisibleMeatDensity = 0.7f,
                VisibleMeatFreshness = 1f
            },
            CreatureGenome.Baseline),
        default,
        denseInputs,
        denseOutputs).Actions;
    var meatContact = scavengerStarter.Evaluate(
        BrainInputFrame.FromSenses(
            new CreatureSenseState
            {
                MeatDetected = true,
                MeatProximity = 1f,
                MeatDirectionForward = 1f,
                FoodContact = 1f,
                MeatFoodContact = 1f,
                VisibleMeatDensity = 1f,
                VisibleMeatFreshness = 1f
            },
            CreatureGenome.Baseline),
        default,
        denseInputs,
        denseOutputs).Actions;
    var rottenMeatContact = scavengerStarter.Evaluate(
        BrainInputFrame.FromSenses(
            new CreatureSenseState
            {
                MeatDetected = true,
                MeatProximity = 1f,
                FoodContact = 1f,
                MeatFoodContact = 1f,
                VisibleMeatDensity = 1f,
                RottenMeatScentDetected = true,
                RottenMeatScentDensity = 1f
            },
            CreatureGenome.Baseline),
        default,
        denseInputs,
        denseOutputs).Actions;
    AssertTrue(scavengerStarter.RtNeat!.ConnectionCount > starter.RtNeat!.ConnectionCount, "rtNEAT scavenger starter should add meat-specific sparse connections");
    AssertTrue(meatToRight.Turn > 0.5f, "rtNEAT scavenger starter should turn toward meat on the right");
    AssertTrue(meatContact.Eat > 0.25f, "rtNEAT scavenger starter should eat contacted meat");
    AssertTrue(rottenMeatContact.Eat < meatContact.Eat, "rtNEAT scavenger starter should reduce eating near rotten scent");

    var predatorStarter = BrainFactory.CreateStarter(
        BrainArchitectureKind.RtNeatGraph,
        InitialBrainKind.SparseGraphPredator);
    var creatureToRight = predatorStarter.Evaluate(
        BrainInputFrame.FromSenses(
            new CreatureSenseState
            {
                CreatureDetected = true,
                CreatureProximity = 0.5f,
                CreatureDirectionForward = 0.45f,
                CreatureDirectionRight = 0.8f,
                VisibleCreatureDensity = 0.7f
            },
            CreatureGenome.Baseline),
        default,
        denseInputs,
        denseOutputs).Actions;
    var creatureContact = predatorStarter.Evaluate(
        BrainInputFrame.FromSenses(
            new CreatureSenseState
            {
                CreatureDetected = true,
                CreatureProximity = 1f,
                CreatureDirectionForward = 1f,
                CreatureContact = 1f,
                VisibleCreatureDensity = 1f
            },
            CreatureGenome.Baseline),
        default,
        denseInputs,
        denseOutputs).Actions;
    var meatToRightForPredator = predatorStarter.Evaluate(
        BrainInputFrame.FromSenses(
            new CreatureSenseState
            {
                MeatDetected = true,
                MeatProximity = 0.45f,
                MeatDirectionForward = 0.6f,
                MeatDirectionRight = 0.8f,
                MeatScentDetected = true,
                MeatScentDensity = 0.5f,
                MeatScentDirectionForward = 0.4f,
                MeatScentDirectionRight = 0.7f,
                VisibleMeatDensity = 0.7f,
                VisibleMeatFreshness = 1f
            },
            CreatureGenome.Baseline with { DietaryAdaptation = 0.75f }),
        default,
        denseInputs,
        denseOutputs).Actions;
    var meatContactForPredator = predatorStarter.Evaluate(
        BrainInputFrame.FromSenses(
            new CreatureSenseState
            {
                Hunger = 1f,
                MeatDetected = true,
                MeatProximity = 1f,
                MeatDirectionForward = 1f,
                FoodContact = 1f,
                MeatFoodContact = 1f,
                VisibleMeatDensity = 1f,
                VisibleMeatFreshness = 1f
            },
            CreatureGenome.Baseline with { DietaryAdaptation = 0.75f }),
        default,
        denseInputs,
        denseOutputs).Actions;
    var rottenMeatContactForPredator = predatorStarter.Evaluate(
        BrainInputFrame.FromSenses(
            new CreatureSenseState
            {
                Hunger = 1f,
                MeatDetected = true,
                MeatProximity = 1f,
                FoodContact = 1f,
                MeatFoodContact = 1f,
                VisibleMeatDensity = 1f,
                RottenMeatScentDetected = true,
                RottenMeatScentDensity = 1f
            },
            CreatureGenome.Baseline with { DietaryAdaptation = 0.75f }),
        default,
        denseInputs,
        denseOutputs).Actions;
    var similarCreatureContact = predatorStarter.Evaluate(
        BrainInputFrame.FromSenses(
            new CreatureSenseState
            {
                CreatureDetected = true,
                CreatureProximity = 1f,
                CreatureDirectionForward = 1f,
                CreatureContact = 1f,
                CreatureContactSimilarity = 1f,
                VisibleCreatureDensity = 1f
            },
            CreatureGenome.Baseline),
        default,
        denseInputs,
        denseOutputs).Actions;
    var hungryMeatBiasedSimilarCreatureContact = predatorStarter.Evaluate(
        BrainInputFrame.FromSenses(
            new CreatureSenseState
            {
                Hunger = 0.9f,
                CreatureDetected = true,
                CreatureProximity = 1f,
                CreatureDirectionForward = 1f,
                CreatureContact = 1f,
                CreatureContactSimilarity = 1f,
                VisibleCreatureDensity = 1f
            },
            CreatureGenome.Baseline with { DietaryAdaptation = 0.75f }),
        default,
        denseInputs,
        denseOutputs).Actions;
    AssertTrue(predatorStarter.RtNeat!.ConnectionCount > starter.RtNeat.ConnectionCount, "rtNEAT predator starter should add creature-contact sparse connections");
    AssertTrue(creatureToRight.Turn > 0.25f, "rtNEAT predator starter should turn toward creatures on the right");
    AssertTrue(creatureContact.Attack > 0.25f, "rtNEAT predator starter should attack contacted creatures");
    AssertTrue(creatureContact.Grab > 0.25f, "rtNEAT predator starter should grab contacted creatures");
    AssertTrue(meatToRightForPredator.Turn > 0.5f, "rtNEAT predator starter should turn toward fresh meat on the right");
    AssertTrue(meatToRightForPredator.MoveForward > noFood.MoveForward, "rtNEAT predator starter should approach visible fresh meat");
    AssertTrue(meatContactForPredator.Eat > 0.25f, "rtNEAT predator starter should eat contacted meat");
    AssertTrue(meatContactForPredator.MoveForward < meatToRightForPredator.MoveForward, "rtNEAT predator starter should slow when it reaches meat");
    AssertTrue(rottenMeatContactForPredator.Eat < meatContactForPredator.Eat, "rtNEAT predator starter should reduce eating near rotten scent");
    AssertTrue(similarCreatureContact.Attack < creatureContact.Attack, "rtNEAT predator starter should suppress attacks against similar contacts");
    AssertTrue(hungryMeatBiasedSimilarCreatureContact.Attack > 0.25f, "rtNEAT predator starter should let hunger and meat bias overcome similar-contact attack suppression");
    AssertTrue(hungryMeatBiasedSimilarCreatureContact.Grab > 0.35f, "rtNEAT predator starter should let hunger and meat bias overcome similar-contact grab suppression");

    var mutated = BrainFactory.Mutate(
        BrainArchitectureKind.RtNeatGraph,
        starter,
        new DeterministicRandom(442),
        mutationStrength: 0.5f,
        mutationRate: MutationProfile.Default.BrainMutationRate);
    AssertTrue(
        mutated.RtNeat is not null
        && (mutated.RtNeat.ConnectionCount != starter.RtNeat!.ConnectionCount
            || mutated.Weights.Zip(starter.Weights).Any(pair => Math.Abs(pair.First - pair.Second) > 0.000001f)),
        "rtNEAT mutation should alter graph structure or weights");

    var scenario = new SimulationScenario
    {
        Seed = 111,
        PipelineKind = SimulationPipelineKind.Neural,
        BrainArchitectureKind = BrainArchitectureKind.RtNeatGraph,
        InitialBrainKind = InitialBrainKind.SparseGraphForager,
        InitialCreatureCount = 1,
        InitialResourcesPerMillionArea = 0f
    };
    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var snapshotJson = SimulationSnapshotJson.ToJson(SimulationSnapshot.Capture(scenario, simulation));
    var restored = SimulationSnapshotJson.RestoreSimulation(SimulationSnapshotJson.FromJson(snapshotJson));
    var restoredCreature = restored.Simulation.State.Creatures.Single();
    var restoredBrain = restored.Simulation.State.GetBrain(restoredCreature.BrainId);
    AssertEqual(BrainArchitectureKind.RtNeatGraph, restoredBrain.ArchitectureKind, "Restored rtNEAT brain architecture");
    AssertTrue(restoredBrain.RtNeat is not null, "Restored rtNEAT brain should retain graph payload");
}

static void WorldStateTracksBrainArchitectureMetadata()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 304, systems: []);
    var brainId = simulation.State.AddBrain(
        NeuralBrainGenome.CreateZero(hiddenNodeCount: 1),
        BrainArchitectureKind.HybridNeural);

    AssertEqual(BrainArchitectureKind.HybridNeural, simulation.State.GetBrainArchitectureKind(brainId), "Stored brain architecture");
    AssertThrows<ArgumentOutOfRangeException>(
        () => simulation.State.GetBrainArchitectureKind(99),
        "Missing brain architecture should be rejected");
    AssertThrows<ArgumentOutOfRangeException>(
        () => simulation.State.AddBrain(NeuralBrainGenome.CreateZero(), (BrainArchitectureKind)999),
        "Unsupported brain architecture should be rejected by world state");
}

static void ScavengerForagerStarterBrainFollowsCarrionCues()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 407, systems: []);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        DietaryAdaptation = 0.3f,
        CarrionAdaptation = 0.4f,
        MaturityAgeSeconds = 0f
    });
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateScavengerForager());

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);

    var summary = BehaviorAssay.Analyze(simulation.State);

    AssertTrue(summary.PlantContact.EatShare > 0.9f, "Scavenger forager should still eat close plants on contact");
    AssertTrue(summary.MeatContact.EatShare > 0.9f, "Scavenger forager should eat meat on contact");
    AssertTrue(summary.MeatAhead.MoveForward > summary.Baseline.MoveForward + 0.2f, "Scavenger forager should pursue visible meat sectors");
    AssertTrue(summary.MeatRight.Turn > 0.8f, "Scavenger forager should turn toward visible meat");
    AssertTrue(summary.MeatScentAhead.MoveForward > summary.Baseline.MoveForward + 0.3f, "Scavenger forager should move toward meat scent");
    AssertTrue(summary.MeatScentRight.Turn > 0.6f, "Scavenger forager should turn toward meat scent");
    AssertTrue(summary.CreatureAhead.AttackShare < 0.1f, "Scavenger forager should not arrive with built-in attack behavior");
    AssertEqual("rare attack response", summary.PredatorTendency, "Scavenger forager attack tendency");
    AssertEqual("scavenger-leaning", summary.Ecotype, "Scavenger forager ecotype");
}

static void FreshnessAwareScavengerStarterBrainAvoidsRotCues()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 408, systems: []);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        DietaryAdaptation = 0.3f,
        CarrionAdaptation = 0.4f,
        MaturityAgeSeconds = 0f
    });
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateFreshnessAwareScavenger());

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);

    var summary = BehaviorAssay.Analyze(simulation.State);

    AssertTrue(summary.PlantContact.EatShare > 0.9f, "Freshness-aware scavenger should still eat close plants on contact");
    AssertTrue(summary.MeatContact.EatShare > 0.9f, "Freshness-aware scavenger should eat meat on contact");
    AssertTrue(summary.MeatAhead.MoveForward > summary.Baseline.MoveForward + 0.2f, "Freshness-aware scavenger should still pursue visible fresh meat sectors");
    AssertTrue(summary.MeatScentAhead.MoveForward > summary.Baseline.MoveForward + 0.3f, "Freshness-aware scavenger should still follow clean meat scent");
    AssertTrue(summary.RottenMeatAhead.MoveForward < summary.MeatAhead.MoveForward - 0.5f, "Freshness-aware scavenger should suppress movement toward stale meat");
    AssertTrue(summary.RottenMeatScentAhead.MoveForward < summary.MeatScentAhead.MoveForward - 0.45f, "Freshness-aware scavenger should avoid rot scent ahead");
    AssertTrue(summary.FreshMeatPreferenceScore > 0.8f, "Freshness-aware scavenger fresh preference score");
    AssertTrue(summary.RottenScentAvoidanceScore > 1.0f, "Freshness-aware scavenger rot avoidance score");
    AssertEqual("prefers fresh meat", summary.RottenMeatResponse, "Freshness-aware scavenger rot response");
    AssertTrue(summary.CreatureAhead.AttackShare < 0.1f, "Freshness-aware scavenger should not arrive with built-in attack behavior");
}

static void ForagerPredatorStarterBrainHuntsCreatureCues()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 402, systems: []);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        DietaryAdaptation = 0.2f,
        MaturityAgeSeconds = 0f
    });
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateForagerPredator());

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);

    var summary = BehaviorAssay.Analyze(simulation.State);

    AssertTrue(summary.PlantContact.EatShare > 0.9f, "Forager predator should still eat close plants on contact");
    AssertTrue(summary.MeatContact.EatShare > 0.9f, "Forager predator should eat meat on contact");
    AssertTrue(summary.CreatureAhead.AttackShare < 0.2f, "Forager predator should not treat a same-size creature sector as automatic prey");
    AssertTrue(summary.CreatureRight.Turn > 0.8f, "Forager predator should turn toward visible creatures on the right");
    AssertTrue(summary.SmallCreatureAhead.AttackShare > 0.9f, "Forager predator should attack smaller visible creatures");
    AssertTrue(summary.LargeCreatureApproaching.AttackShare < 0.2f, "Forager predator should hold back from large approaching creatures");
    AssertTrue(summary.RiskResponse == "size-aware restraint", "Forager predator should be classified as size-aware around risk cues");
    AssertTrue(summary.Ecotype == "small-prey predator", "Forager predator should be classified as a small-prey predator");
    AssertTrue(summary.Baseline.AttackShare < 0.1f, "Forager predator should not attack without visible creature cues");
}

static void MeatOrientedStartersEatMeatOnContact()
{
    AssertMeatContactTriggersEat(NeuralBrainGenome.CreateScavengerForager(), "Scavenger forager");
    AssertMeatContactTriggersEat(NeuralBrainGenome.CreateFreshnessAwareScavenger(), "Freshness-aware scavenger");
    AssertMeatContactTriggersEat(NeuralBrainGenome.CreateForagerPredator(), "Forager predator");
}

static void AssertMeatContactTriggersEat(NeuralBrainGenome brain, string label)
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 0.1f },
        seed: 414,
        systems: [new NeuralControllerSystem()]);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        DietaryAdaptation = 0.3f,
        CarrionAdaptation = 0.3f,
        MaturityAgeSeconds = 0f
    });
    var brainId = simulation.State.AddBrain(brain);

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);
    var creature = simulation.State.Creatures[0];
    creature.Senses = new CreatureSenseState
    {
        Hunger = 1f,
        FoodContact = 1f,
        MeatFoodContact = 1f
    };
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    AssertTrue(simulation.State.Creatures[0].Actions.WantsEat, $"{label} should intentionally eat meat on body contact");
}

static void LineageBehaviorAssaysSummarizeTopFounderStrategies()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 403, systems: []);
    var grazerGenomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        DietaryAdaptation = 0.1f,
        MaturityAgeSeconds = 0f
    });
    var predatorGenomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        DietaryAdaptation = 0.2f,
        MaturityAgeSeconds = 0f
    });
    var grazerBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager());
    var predatorBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateForagerPredator());

    var grazerFounder = simulation.State.SpawnCreature(grazerGenomeId, new SimVector2(20f, 20f), energy: 25f, brainId: grazerBrainId);
    var predatorFounder = simulation.State.SpawnCreature(predatorGenomeId, new SimVector2(40f, 20f), energy: 25f, brainId: predatorBrainId);
    simulation.State.SpawnCreature(predatorGenomeId, new SimVector2(45f, 20f), energy: 25f, generation: 1, parentId: predatorFounder, brainId: predatorBrainId);

    var summaries = BehaviorAssay.AnalyzeTopFounderLineages(simulation.State, maxLineages: 10);

    AssertEqual(2, summaries.Count, "Lineage behavior summary count");
    AssertEqual(predatorFounder, summaries[0].FounderId, "Largest living lineage should be listed first");

    var predator = summaries.First(summary => summary.FounderId == predatorFounder);
    AssertEqual(2, predator.TotalCreatures, "Predator lineage total");
    AssertEqual(2, predator.LivingCreatures, "Predator lineage living");
    AssertEqual(0, predator.DeadCreatures, "Predator lineage dead");
    AssertEqual(1, predator.MaxGeneration, "Predator lineage max generation");
    AssertClose(2f / 3f, predator.LivingShare, 0.000001, "Predator lineage living share");
    AssertEqual("small-prey predator", predator.Behavior.Ecotype, "Predator lineage ecotype");
    AssertEqual("size-aware restraint", predator.Behavior.RiskResponse, "Predator lineage risk response");

    var grazer = summaries.First(summary => summary.FounderId == grazerFounder);
    AssertEqual(1, grazer.LivingCreatures, "Grazer lineage living");
    AssertEqual(1, grazer.Behavior.EvaluatedCreatureCount, "Grazer assayed creature count");
}

static float MeasureSeedForagerMove(SimVector2 resourcePosition)
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 32f);
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 200f,
            WorldHeight = 200f,
            FixedDeltaSeconds = 0.1f
        },
        seed: 60,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureSensingSystem(spatialIndex, enableSectorVision: true),
            new NeuralControllerSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        MaxSpeed = 10f,
        SenseRadius = 100f,
        ReproductionEnergyThreshold = 100f,
        MaturityAgeSeconds = 0f
    });
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager());

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f, brainId: brainId);
    var creature = simulation.State.Creatures[0];
    creature.HeadingRadians = 0f;
    simulation.State.Creatures[0] = creature;

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = resourcePosition,
        Radius = 2f,
        Calories = 50f,
        MaxCalories = 50f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();
    return simulation.State.Creatures[0].Actions.MoveForward;
}

static void CreatureAttackDamagesContactTargets()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 308,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureAttackSystem(
                spatialIndex,
                biteDamagePerSecond: 0.25f,
                biteEnergyCostPerSecond: 0.1f,
                biteRangePadding: 1f,
                requireAttackIntent: true)
        ]);

    var attackerGenomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        BiteStrength = 2f,
        DamageResistance = 1f,
        MaturityAgeSeconds = 0f
    });
    var targetGenomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        BiteStrength = 1f,
        DamageResistance = 4f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(attackerGenomeId, new SimVector2(20f, 20f), energy: 10f);
    simulation.State.SpawnCreature(targetGenomeId, new SimVector2(26f, 20f), energy: 10f);
    var attacker = simulation.State.Creatures[0];
    attacker.HeadingRadians = 0f;
    attacker.Actions = new CreatureActionState { WantsAttack = true };
    simulation.State.Creatures[0] = attacker;

    simulation.Step();

    attacker = simulation.State.Creatures[0];
    var target = simulation.State.Creatures[1];
    AssertTrue(attacker.IsTouchingCreature, "Attacker should report creature contact");
    AssertEqual(target.Id, attacker.CreatureContactId, "Attacker contact id");
    AssertClose(0f, attacker.CreatureContactEdgeDistance, 0.000001, "Creature contact edge distance");
    AssertClose(0.25f, attacker.LastAttackDamageDealt, 0.000001, "Attack damage dealt");
    AssertClose(9.8f, attacker.Energy, 0.000001, "Attack energy cost");
    AssertClose(0.75f, target.Health, 0.000001, "Target health after bite");
}

static void CreatureGrabSlowsContactTargets()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 331,
        systems:
        [
            new CreatureGrabSystem(),
            new MovementSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        MaxSpeed = 10f,
        MovementEnergyPerSecond = 0f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);
    var targetId = simulation.State.SpawnCreature(genomeId, new SimVector2(26f, 20f), energy: 10f);
    var grabber = simulation.State.Creatures[0];
    grabber.IsTouchingCreature = true;
    grabber.CreatureContactId = targetId;
    grabber.Actions = new CreatureActionState { WantsGrab = true, GrabOutput = 1f };
    simulation.State.Creatures[0] = grabber;

    var target = simulation.State.Creatures[1];
    target.DesiredVelocity = new SimVector2(10f, 0f);
    simulation.State.Creatures[1] = target;

    simulation.Step();

    grabber = simulation.State.Creatures[0];
    target = simulation.State.Creatures[1];
    AssertEqual(target.Id, grabber.HeldCreatureId, "Grabber should hold target");
    AssertEqual(grabber.Id, target.GrabbedByCreatureId, "Target should know grabber");
    AssertClose(1f, target.GrabPressure, 0.000001, "Same-size full grab pressure");
    AssertClose(-1f, target.GrabDirection.X, 0.000001, "Grab direction points toward grabber");
    AssertClose(27.5f, target.Position.X, 0.000001, "Grabbed target movement should be slowed");
}

static void CreatureAttackDeathsBecomeInjuryMeat()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 309,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new CreatureAttackSystem(
                spatialIndex,
                biteDamagePerSecond: 2f,
                biteEnergyCostPerSecond: 0f,
                biteRangePadding: 1f,
                requireAttackIntent: true),
            new DeathSystem(meatCaloriesPerBodyRadius: 4f, meatEnergyFraction: 0.5f, meatDecayCaloriesPerSecond: 0.25f),
            new SpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);
    var targetId = simulation.State.SpawnCreature(genomeId, new SimVector2(24f, 20f), energy: 6f);
    var attackerId = simulation.State.Creatures[0].Id;
    var attacker = simulation.State.Creatures[0];
    attacker.HeadingRadians = 0f;
    attacker.Actions = new CreatureActionState { WantsAttack = true };
    simulation.State.Creatures[0] = attacker;

    simulation.Step();

    AssertEqual(1, simulation.State.Creatures.Count, "Only attacker should remain alive");
    AssertEqual(1, simulation.State.Stats.CreatureDeathCount, "Creature death count");
    AssertEqual(1, simulation.State.Stats.InjuryDeathCount, "Injury death count");
    AssertTrue(simulation.State.TryGetLineageRecord(targetId, out var targetRecord), "Target lineage lookup");
    AssertEqual(CreatureDeathReason.Injury, targetRecord.DeathReason, "Target death reason");
    AssertEqual(attackerId, targetRecord.DeathAttackerId, "Target death attacker");
    AssertEqual(1, simulation.State.Resources.Count, "Meat resource count");
    var meat = simulation.State.Resources[0];
    AssertEqual(ResourceKind.Meat, meat.Kind, "Killed target should leave meat");
    AssertEqual(attackerId, meat.FreshKillAttackerId, "Fresh-kill attacker id");
    AssertEqual(targetId, meat.FreshKillPreyId, "Fresh-kill prey id");
    AssertClose(20f, meat.FreshKillSecondsRemaining, 0.000001, "Fresh-kill credit window");
    attacker = simulation.State.Creatures[0];
    AssertTrue(attacker.LastLivePreyCaloriesEaten > 0f, "Attacker should receive fresh-kill intake credit");
    AssertClose(0f, attacker.LastCarcassCaloriesEaten, 0.000001, "Attacker fresh-kill intake should not count as passive carcass");
}

static void SparseMutationRatesGateGenomeAndBrainChanges()
{
    var random = new DeterministicRandom(77);
    var parent = CreatureGenome.Baseline with
    {
        MutationStrength = 0.5f,
        TraitMutationRate = 0f,
        BrainMutationRate = 0f
    };

    var child = parent.Mutated(random);
    AssertEqual(parent, child, "Zero trait mutation rate should preserve the genome");

    var zeroBrain = NeuralBrainGenome.CreateZero();
    var unchangedBrain = zeroBrain.Mutated(random, mutationStrength: 0.5f, mutationRate: 0f);
    AssertTrue(
        unchangedBrain.Weights.All(weight => Math.Abs(weight) < 0.000001f),
        "Zero brain mutation rate should preserve brain weights");

    var sparseBrain = zeroBrain.Mutated(random, mutationStrength: 0.5f, mutationRate: 0.000001f);
    AssertTrue(
        sparseBrain.Weights.Any(weight => Math.Abs(weight) > 0.000001f),
        "Nonzero sparse brain mutation rate should force at least one changed weight");
}

static void WorldMutationPolicyOverridesInheritedGenomeMutationSettings()
{
    var mutationProfile = new MutationProfile(
        MutationStrength: 0.25f,
        TraitMutationRate: 1f,
        BrainMutationRate: 1f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 617,
        systems:
        [
            new ReproductionSystem(mutationPolicy: new WorldMutationPolicy(mutationProfile))
        ]);
    var parentGenome = CreatureGenome.Baseline with
    {
        ReproductionEnergyThreshold = 50f,
        OffspringEnergyInvestment = 20f,
        EggProductionEnergyPerSecond = 20f,
        MaturityAgeSeconds = 0f,
        ReproductionCooldownSeconds = 0f,
        MutationStrength = 0f,
        TraitMutationRate = 0f,
        BrainMutationRate = 0f
    };
    var genomeId = simulation.State.AddGenome(parentGenome);
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateZero());
    simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 80f, brainId: brainId);

    simulation.Step();

    AssertEqual(1, simulation.State.Eggs.Count, "World mutation egg count");
    AssertEqual(2, simulation.State.Genomes.Count, "World mutation child genome count");
    var egg = simulation.State.Eggs[0];
    AssertClose(0.25f, egg.BirthMutationStrength, 0.000001, "Egg birth mutation strength");
    AssertClose(1f, egg.BirthTraitMutationRate, 0.000001, "Egg birth trait mutation rate");
    AssertClose(1f, egg.BirthBrainMutationRate, 0.000001, "Egg birth brain mutation rate");
    var childGenome = simulation.State.GetGenome(egg.GenomeId);
    AssertClose(0.25f, childGenome.MutationStrength, 0.000001, "Child genome records effective mutation strength");
    AssertClose(1f, childGenome.TraitMutationRate, 0.000001, "Child genome records effective trait mutation rate");
    AssertClose(1f, childGenome.BrainMutationRate, 0.000001, "Child genome records effective brain mutation rate");
    AssertTrue(
        Math.Abs(childGenome.BodyRadius - parentGenome.BodyRadius) > 0.000001f
        || Math.Abs(childGenome.MaxSpeed - parentGenome.MaxSpeed) > 0.000001f
        || Math.Abs(childGenome.SenseRadius - parentGenome.SenseRadius) > 0.000001f
        || Math.Abs(childGenome.ReproductionEnergyThreshold - parentGenome.ReproductionEnergyThreshold) > 0.000001f,
        "World mutation policy should mutate inherited body traits even when parent mutation genes are zero");
    AssertTrue(
        simulation.State.GetBrain(egg.BrainId).Weights.Any(weight => Math.Abs(weight) > 0.000001f),
        "World mutation policy should mutate brain weights even when parent mutation genes are zero");
}

static void IntentGatedEatingRequiresEatOutput()
{
    var spatialIndex = new UniformSpatialIndex(cellSize: 16f);
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 7,
        systems:
        [
            new SpatialIndexRebuildSystem(spatialIndex),
            new EatingSystem(spatialIndex, requireEatIntent: true),
            new DigestionSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 3f,
        EatCaloriesPerSecond = 12f,
        DigestionCaloriesPerSecond = 12f,
        DietaryAdaptation = 0f,
        MaturityAgeSeconds = 0f
    });

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 10f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(22f, 20f),
        Radius = 2f,
        Calories = 30f,
        MaxCalories = 30f,
        RegrowthCaloriesPerSecond = 0f
    });

    simulation.Step();

    AssertClose(10f, simulation.State.Creatures[0].Energy, 0.000001, "Energy without eat intent");
    AssertClose(30f, simulation.State.Resources[0].Calories, 0.000001, "Resource without eat intent");

    var creature = simulation.State.Creatures[0];
    creature.Actions = new CreatureActionState { WantsEat = true };
    simulation.State.Creatures[0] = creature;

    simulation.Step();

    AssertClose(22f, simulation.State.Creatures[0].Energy, 0.000001, "Energy with eat intent");
    AssertClose(12f, simulation.State.Creatures[0].LastCaloriesDigested, 0.000001, "Calories digested with eat intent");
    AssertClose(18f, simulation.State.Resources[0].Calories, 0.000001, "Resource with eat intent");
}

static void IntentGatedReproductionMutatesBrain()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 8,
        systems: [new ReproductionSystem(requireReproductionIntent: true)]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        ReproductionEnergyThreshold = 50f,
        OffspringEnergyInvestment = 20f,
        EggProductionEnergyPerSecond = 20f,
        MaturityAgeSeconds = 2f,
        ReproductionCooldownSeconds = 5f,
        MutationStrength = 0.5f
    });
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateZero());
    var parentId = simulation.State.SpawnCreature(
        genomeId,
        new SimVector2(50f, 50f),
        energy: 80f,
        brainId: brainId);

    var juvenileParent = simulation.State.Creatures[0];
    juvenileParent.AgeSeconds = 2f;
    simulation.State.Creatures[0] = juvenileParent;

    simulation.Step();
    AssertEqual(1, simulation.State.Creatures.Count, "Creature count without reproduction intent");
    AssertEqual(0, simulation.State.Eggs.Count, "Egg count without reproduction intent");
    AssertClose(20f, simulation.State.Creatures[0].ReproductiveEnergy, 0.000001, "Reserve should build without reproduction intent");
    AssertEqual(0, simulation.State.Stats.ReproductionAttemptCount, "Reproduction attempt count without intent");
    AssertEqual(1, simulation.State.Brains.Count, "Brain count before egg is laid");

    var parent = simulation.State.Creatures[0];
    parent.Actions = new CreatureActionState { WantsReproduce = true };
    simulation.State.Creatures[0] = parent;

    simulation.Step();

    AssertEqual(1, simulation.State.Creatures.Count, "Creature count with reproduction intent");
    AssertEqual(1, simulation.State.Eggs.Count, "Egg count with reproduction intent");
    AssertClose(0f, simulation.State.Creatures[0].ReproductiveEnergy, 0.000001, "Reserve should clear after egg is laid");
    AssertEqual(1, simulation.State.Stats.ReproductionAttemptCount, "Reproduction attempt count with intent");
    AssertEqual(2, simulation.State.Brains.Count, "Brain count after reproduction");
    AssertEqual(parentId, simulation.State.Eggs[0].ParentId, "Egg parent id");
    AssertEqual(1, simulation.State.Eggs[0].Generation, "Egg generation");
    AssertTrue(simulation.State.Eggs[0].BrainId >= 0, "Egg should have a brain id");
    AssertTrue(
        simulation.State.Brains[simulation.State.Eggs[0].BrainId].Weights.Any(weight => Math.Abs(weight) > 0.00001f),
        "Egg brain should contain mutated weights");
}

static void NeuralLifeLoopIsRepeatable()
{
    var first = CreateNeuralLifeLoopSimulation();
    var second = CreateNeuralLifeLoopSimulation();

    first.RunSteps(60);
    second.RunSteps(60);

    AssertEqual(first.State.Creatures.Count, second.State.Creatures.Count, "Creature count");
    AssertEqual(first.State.Eggs.Count, second.State.Eggs.Count, "Egg count");
    AssertEqual(first.State.Resources.Count, second.State.Resources.Count, "Resource count");
    AssertEqual(first.State.Genomes.Count, second.State.Genomes.Count, "Genome count");
    AssertEqual(first.State.Brains.Count, second.State.Brains.Count, "Brain count");

    for (var i = 0; i < first.State.Creatures.Count; i++)
    {
        var firstCreature = first.State.Creatures[i];
        var secondCreature = second.State.Creatures[i];

        AssertEqual(firstCreature.Id, secondCreature.Id, $"Creature {i} id");
        AssertEqual(firstCreature.ParentId, secondCreature.ParentId, $"Creature {i} parent id");
        AssertClose(firstCreature.Position.X, secondCreature.Position.X, 0.000001, $"Creature {i} x");
        AssertClose(firstCreature.Position.Y, secondCreature.Position.Y, 0.000001, $"Creature {i} y");
        AssertClose(firstCreature.Energy, secondCreature.Energy, 0.000001, $"Creature {i} energy");
        AssertClose(firstCreature.BirthInvestmentRatio, secondCreature.BirthInvestmentRatio, 0.000001, $"Creature {i} birth investment");
        AssertEqual(firstCreature.BrainId, secondCreature.BrainId, $"Creature {i} brain id");
    }

    for (var i = 0; i < first.State.Eggs.Count; i++)
    {
        var firstEgg = first.State.Eggs[i];
        var secondEgg = second.State.Eggs[i];

        AssertEqual(firstEgg.Id, secondEgg.Id, $"Egg {i} id");
        AssertEqual(firstEgg.ParentId, secondEgg.ParentId, $"Egg {i} parent id");
        AssertClose(firstEgg.Position.X, secondEgg.Position.X, 0.000001, $"Egg {i} x");
        AssertClose(firstEgg.Position.Y, secondEgg.Position.Y, 0.000001, $"Egg {i} y");
        AssertClose(firstEgg.AgeSeconds, secondEgg.AgeSeconds, 0.000001, $"Egg {i} age");
        AssertClose(firstEgg.Energy, secondEgg.Energy, 0.000001, $"Egg {i} energy");
        AssertClose(firstEgg.Health, secondEgg.Health, 0.000001, $"Egg {i} health");
        AssertClose(firstEgg.MaxHealth, secondEgg.MaxHealth, 0.000001, $"Egg {i} max health");
        AssertClose(firstEgg.InvestmentRatio, secondEgg.InvestmentRatio, 0.000001, $"Egg {i} investment");
        AssertEqual(firstEgg.BrainId, secondEgg.BrainId, $"Egg {i} brain id");
    }
}

static Simulation CreateNeuralLifeLoopSimulation()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 200f,
            WorldHeight = 200f,
            FixedDeltaSeconds = 0.25f
        },
        seed: 123456,
        systems: SimulationPipelines.CreateNeuralLifeLoop(spatialCellSize: 32f));

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        ReproductionEnergyThreshold = 70f,
        OffspringEnergyInvestment = 20f,
        ReproductionCooldownSeconds = 4f,
        MutationStrength = 0.05f
    });
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager());

    simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 35f, brainId: brainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(150f, 150f), energy: 35f, brainId: brainId);

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(60f, 50f),
        Radius = 5f,
        Calories = 100f,
        MaxCalories = 100f,
        RegrowthCaloriesPerSecond = 2f
    });

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(140f, 150f),
        Radius = 5f,
        Calories = 100f,
        MaxCalories = 100f,
        RegrowthCaloriesPerSecond = 2f
    });

    return simulation;
}

static void SpawnedCreaturesCreateLineageRecords()
{
    var simulation = new Simulation(new SimulationConfig(), seed: 9);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager());
    var creatureId = simulation.State.SpawnCreature(
        genomeId,
        new SimVector2(10f, 10f),
        energy: 25f,
        brainId: brainId);

    AssertEqual(1, simulation.State.LineageRecords.Count, "Lineage record count");
    AssertEqual(1, simulation.State.Stats.CreatureBirthCount, "Birth count");
    AssertEqual(1, simulation.State.Stats.FounderCreatureCount, "Founder count");
    AssertTrue(simulation.State.TryGetLineageRecord(creatureId, out var record), "Lineage lookup should succeed");
    AssertEqual(creatureId, record.Id, "Lineage id");
    AssertTrue(record.IsFounder, "Founder lineage flag");
    AssertTrue(record.IsAlive, "Lineage should be alive");
    AssertEqual(genomeId, record.GenomeId, "Lineage genome id");
    AssertEqual(brainId, record.BrainId, "Lineage brain id");
    AssertClose(25f, record.BirthEnergy, 0.000001, "Lineage birth energy");
    AssertClose(10f, record.BirthPosition.X, 0.000001, "Lineage birth position x");
    AssertClose(10f, record.BirthPosition.Y, 0.000001, "Lineage birth position y");
    AssertClose(1f, simulation.State.Stats.SpatialHeatmaps.Births.Sum(), 0.000001, "Birth heatmap count");
}

static void OffspringLineageRecordsParentAndGeneration()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 10,
        systems: [new ReproductionSystem(), new EggSystem()]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        ReproductionEnergyThreshold = 50f,
        OffspringEnergyInvestment = 20f,
        EggProductionEnergyPerSecond = 20f,
        EggIncubationSeconds = 1f,
        MaturityAgeSeconds = 30f,
        ReproductionCooldownSeconds = 5f,
        MutationStrength = 0f
    });
    var parentId = simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 80f);
    var parent = simulation.State.Creatures[0];
    parent.AgeSeconds = 30f;
    simulation.State.Creatures[0] = parent;

    simulation.Step();

    var child = simulation.State.Creatures[1];
    AssertEqual(0, simulation.State.Eggs.Count, "Egg should hatch during the same step");
    AssertTrue(simulation.State.TryGetLineageRecord(child.Id, out var childRecord), "Child lineage lookup should succeed");
    AssertEqual(parentId, childRecord.ParentId, "Child lineage parent id");
    AssertEqual(1, childRecord.Generation, "Child lineage generation");
    AssertEqual(2, simulation.State.Stats.CreatureBirthCount, "Total birth count");
    AssertEqual(1, simulation.State.Stats.FounderCreatureCount, "Founder count after child birth");
    AssertEqual(1, simulation.State.Stats.EggLaidCount, "Egg laid count");
    AssertEqual(1, simulation.State.Stats.EggHatchedCount, "Egg hatch count");
}

static void DeathSystemMarksLineageDeathReason()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 11,
        systems:
        [
            new MetabolismSystem(),
            new DeathSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BasalEnergyPerSecond = 10f
    });
    var creatureId = simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 5f);

    simulation.Step();

    AssertEqual(0, simulation.State.Creatures.Count, "Living creature count");
    AssertEqual(1, simulation.State.Stats.CreatureDeathCount, "Death count");
    AssertEqual(1, simulation.State.Stats.StarvationDeathCount, "Starvation death count");
    AssertTrue(simulation.State.TryGetLineageRecord(creatureId, out var record), "Dead lineage lookup should succeed");
    AssertTrue(!record.IsAlive, "Dead lineage should not be alive");
    AssertEqual(CreatureDeathReason.Starvation, record.DeathReason, "Death reason");
    AssertTrue(record.DeathPosition is not null, "Death position should be recorded");
    AssertClose(20f, record.DeathPosition!.Value.X, 0.000001, "Death position x");
    AssertClose(20f, record.DeathPosition.Value.Y, 0.000001, "Death position y");
    AssertClose(1f, simulation.State.Stats.SpatialHeatmaps.Deaths.Sum(), 0.000001, "Death heatmap count");
    AssertClose(1f, simulation.State.Stats.SpatialHeatmaps.StarvationDeaths.Sum(), 0.000001, "Starvation heatmap count");
}

static void SpatialHeatmapsRecordLifecycleAndInteractionEvents()
{
    var foodIndex = new UniformSpatialIndex(cellSize: 16f);
    var foodSimulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 101,
        systems:
        [
            new SpatialIndexRebuildSystem(foodIndex),
            new EatingSystem(foodIndex)
        ]);
    var foodGenomeId = foodSimulation.State.AddGenome(CreatureGenome.Baseline with
    {
        EatCaloriesPerSecond = 10f,
        GutCapacityCalories = 100f
    });
    foodSimulation.State.SpawnCreature(foodGenomeId, new SimVector2(40f, 40f), energy: 50f);
    foodSimulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        Position = new SimVector2(40f, 40f),
        Radius = 4f,
        Calories = 25f,
        MaxCalories = 25f
    });

    foodSimulation.Step();

    AssertTrue(foodSimulation.State.Stats.SpatialHeatmaps.PlantCaloriesEaten.Sum() > 0f, "Plant eating heatmap should record calories");

    var attackIndex = new UniformSpatialIndex(cellSize: 16f);
    var attackSimulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 102,
        systems:
        [
            new SpatialIndexRebuildSystem(attackIndex),
            new CreatureAttackSystem(attackIndex, biteDamagePerSecond: 1f, biteRangePadding: 4f, requireAttackIntent: false)
        ]);
    var attackerGenomeId = attackSimulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BodyRadius = 5f,
        BiteStrength = 1f,
        MaturityAgeSeconds = 0f
    });
    attackSimulation.State.SpawnCreature(attackerGenomeId, new SimVector2(50f, 50f), energy: 50f);
    attackSimulation.State.SpawnCreature(attackerGenomeId, new SimVector2(58f, 50f), energy: 50f);
    var attacker = attackSimulation.State.Creatures[0];
    attacker.HeadingRadians = 0f;
    attackSimulation.State.Creatures[0] = attacker;

    attackSimulation.Step();

    AssertTrue(attackSimulation.State.Stats.SpatialHeatmaps.AttackDamage.Sum() > 0f, "Attack heatmap should record damage");

    var snapshotScenario = new SimulationScenario { WorldWidth = 1000f, WorldHeight = 1000f };
    var snapshotJson = SimulationSnapshotJson.ToJson(SimulationSnapshot.Capture(snapshotScenario, foodSimulation));
    var restored = SimulationSnapshotJson.RestoreSimulation(SimulationSnapshotJson.FromJson(snapshotJson)).Simulation;
    AssertClose(
        foodSimulation.State.Stats.SpatialHeatmaps.PlantCaloriesEaten.Sum(),
        restored.State.Stats.SpatialHeatmaps.PlantCaloriesEaten.Sum(),
        0.000001,
        "Restored heatmap calories");
}

static void WorldStatePrunesExtinctPayloads()
{
    var simulation = CreatePayloadPruningFixture(runPruningSystem: false);
    var deadCreatureId = simulation.State.Creatures[0].Id;
    var liveCreatureId = simulation.State.Creatures[1].Id;

    var deadCreature = simulation.State.Creatures[0];
    deadCreature.Energy = 0f;
    simulation.State.Creatures[0] = deadCreature;
    simulation.Step();

    AssertEqual(1, simulation.State.Creatures.Count, "Living creature count before pruning");
    AssertEqual(2, simulation.State.Genomes.Count, "Genome count before pruning");
    AssertEqual(2, simulation.State.Brains.Count, "Brain count before pruning");

    var result = simulation.State.PruneExtinctPayloads();

    AssertEqual(1, result.PrunedGenomeCount, "Pruned genome count");
    AssertEqual(1, result.PrunedBrainCount, "Pruned brain count");
    AssertEqual(1, simulation.State.Genomes.Count, "Genome count after pruning");
    AssertEqual(1, simulation.State.Brains.Count, "Brain count after pruning");

    var survivor = simulation.State.Creatures[0];
    AssertEqual(liveCreatureId, survivor.Id, "Survivor id");
    AssertEqual(0, survivor.GenomeId, "Survivor genome remap");
    AssertEqual(0, survivor.BrainId, "Survivor brain remap");
    AssertClose(4f, simulation.State.GetGenome(survivor.GenomeId).BodyRadius, 0.000001, "Survivor genome payload");
    AssertEqual(BrainArchitectureKind.HiddenLayerNeural, simulation.State.GetBrainArchitectureKind(survivor.BrainId), "Survivor brain architecture");

    AssertTrue(simulation.State.TryGetLineageRecord(deadCreatureId, out var deadRecord), "Dead lineage lookup");
    AssertEqual(-1, deadRecord.GenomeId, "Dead lineage genome payload should be pruned");
    AssertEqual(-1, deadRecord.BrainId, "Dead lineage brain payload should be pruned");

    AssertTrue(simulation.State.TryGetLineageRecord(liveCreatureId, out var liveRecord), "Live lineage lookup");
    AssertEqual(0, liveRecord.GenomeId, "Live lineage genome remap");
    AssertEqual(0, liveRecord.BrainId, "Live lineage brain remap");
}

static void WorldStateKeepsSurvivorAncestorPayloads()
{
    var simulation = new Simulation(
        new SimulationConfig { WorldWidth = 500f, WorldHeight = 500f, FixedDeltaSeconds = 1f },
        seed: 211,
        systems: [new DeathSystem()]);
    var ancestorGenomeId = simulation.State.AddGenome(CreatureGenome.Baseline with { BodyRadius = 2f });
    var childGenomeId = simulation.State.AddGenome(CreatureGenome.Baseline with { BodyRadius = 4f });
    var sideGenomeId = simulation.State.AddGenome(CreatureGenome.Baseline with { BodyRadius = 6f });
    var ancestorBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager());
    var childBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager());
    var sideBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager());

    var ancestorId = simulation.State.SpawnCreature(
        ancestorGenomeId,
        new SimVector2(20f, 20f),
        energy: 5f,
        brainId: ancestorBrainId);
    var childId = simulation.State.SpawnCreature(
        childGenomeId,
        new SimVector2(40f, 20f),
        energy: 50f,
        generation: 1,
        parentId: ancestorId,
        brainId: childBrainId);
    var sideId = simulation.State.SpawnCreature(
        sideGenomeId,
        new SimVector2(60f, 20f),
        energy: 5f,
        brainId: sideBrainId);

    for (var i = 0; i < simulation.State.Creatures.Count; i++)
    {
        var creature = simulation.State.Creatures[i];
        if (creature.Id == ancestorId || creature.Id == sideId)
        {
            creature.Energy = 0f;
            simulation.State.Creatures[i] = creature;
        }
    }

    simulation.Step();

    AssertEqual(1, simulation.State.Creatures.Count, "Living child count before pruning");
    AssertEqual(childId, simulation.State.Creatures[0].Id, "Living child id before pruning");

    var result = simulation.State.PruneExtinctPayloads();

    AssertEqual(1, result.PrunedGenomeCount, "Only side branch genome should be pruned");
    AssertEqual(1, result.PrunedBrainCount, "Only side branch brain should be pruned");
    AssertEqual(2, simulation.State.Genomes.Count, "Ancestor and child genomes should remain");
    AssertEqual(2, simulation.State.Brains.Count, "Ancestor and child brains should remain");

    AssertTrue(simulation.State.TryGetLineageRecord(ancestorId, out var ancestorRecord), "Ancestor lineage lookup");
    AssertEqual(0, ancestorRecord.GenomeId, "Ancestor genome payload should be retained");
    AssertEqual(0, ancestorRecord.BrainId, "Ancestor brain payload should be retained");

    AssertTrue(simulation.State.TryGetLineageRecord(childId, out var childRecord), "Child lineage lookup");
    AssertEqual(1, childRecord.GenomeId, "Child genome payload should be remapped");
    AssertEqual(1, childRecord.BrainId, "Child brain payload should be remapped");

    AssertTrue(simulation.State.TryGetLineageRecord(sideId, out var sideRecord), "Side branch lineage lookup");
    AssertEqual(-1, sideRecord.GenomeId, "Side branch genome payload should be pruned");
    AssertEqual(-1, sideRecord.BrainId, "Side branch brain payload should be pruned");

    var analysis = SurvivorLineageAnalyzer.Analyze(simulation.State);
    AssertEqual(1, analysis.LivingCreatureCount, "Survivor ancestry living count");
    AssertTrue(
        analysis.Segments.Any(segment => segment.StartRecord.Id == ancestorId && segment.EndRecord.Id == childId),
        "Survivor ancestry should collapse ancestor-to-child chain into a segment");
}

static void PrunedSimulationSnapshotsRestoreContinuation()
{
    var scenario = new SimulationScenario
    {
        Seed = 112,
        WorldWidth = 500f,
        WorldHeight = 500f,
        ResourceVoidBorderWidth = 20f,
        FixedDeltaSeconds = 1f,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f,
        EnableExtinctPayloadPruning = true,
        ExtinctPayloadPruneIntervalTicks = 1
    };
    var simulation = CreatePayloadPruningFixture(runPruningSystem: false);
    var deadCreature = simulation.State.Creatures[0];
    deadCreature.Energy = 0f;
    simulation.State.Creatures[0] = deadCreature;
    simulation.Step();
    simulation.State.PruneExtinctPayloads();

    var snapshotJson = SimulationSnapshotJson.ToJson(SimulationSnapshot.Capture(scenario, simulation));
    var restored = SimulationSnapshotJson.RestoreSimulation(SimulationSnapshotJson.FromJson(snapshotJson)).Simulation;

    AssertEqual(1, restored.State.Creatures.Count, "Restored living creature count");
    AssertEqual(1, restored.State.Genomes.Count, "Restored genome payload count");
    AssertEqual(1, restored.State.Brains.Count, "Restored brain payload count");
    AssertEqual(2, restored.State.LineageRecords.Count, "Restored lineage count");
    AssertTrue(restored.State.LineageRecords.Any(record => record.GenomeId < 0), "Restored lineage should preserve pruned marker");

    restored.RunSteps(2);
    AssertTrue(restored.State.Tick >= simulation.State.Tick + 2, "Restored pruned snapshot should continue running");
}

static void ExtinctPayloadPruningSystemRunsInPipeline()
{
    var simulation = CreatePayloadPruningFixture(runPruningSystem: true);
    var deadCreature = simulation.State.Creatures[0];
    deadCreature.Energy = 0f;
    simulation.State.Creatures[0] = deadCreature;

    simulation.Step();

    AssertEqual(1, simulation.State.Creatures.Count, "Living creature count after pipeline pruning");
    AssertEqual(1, simulation.State.Genomes.Count, "Genome count after pipeline pruning");
    AssertEqual(1, simulation.State.Brains.Count, "Brain count after pipeline pruning");
    AssertEqual(0, simulation.State.Creatures[0].GenomeId, "Pipeline pruning survivor genome remap");
    AssertEqual(0, simulation.State.Creatures[0].BrainId, "Pipeline pruning survivor brain remap");
}

static Simulation CreatePayloadPruningFixture(bool runPruningSystem)
{
    var systems = runPruningSystem
        ? new ISimulationSystem[] { new DeathSystem(), new ExtinctPayloadPruningSystem(intervalTicks: 1) }
        : [new DeathSystem()];
    var simulation = new Simulation(
        new SimulationConfig { WorldWidth = 500f, WorldHeight = 500f, FixedDeltaSeconds = 1f },
        seed: 111,
        systems: systems);
    var deadGenomeId = simulation.State.AddGenome(CreatureGenome.Baseline with { BodyRadius = 2f });
    var deadBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager());
    var liveGenomeId = simulation.State.AddGenome(CreatureGenome.Baseline with { BodyRadius = 4f });
    var liveBrainId = simulation.State.AddBrain(
        NeuralBrainGenome.CreateHiddenLayerRandom(new DeterministicRandom(123), hiddenNodeCount: 8),
        BrainArchitectureKind.HiddenLayerNeural);

    simulation.State.SpawnCreature(deadGenomeId, new SimVector2(20f, 20f), energy: 5f, brainId: deadBrainId);
    simulation.State.SpawnCreature(liveGenomeId, new SimVector2(40f, 20f), energy: 50f, brainId: liveBrainId);
    return simulation;
}

static void StatsRecordingCapturesAggregateSnapshot()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 12,
        systems: [new StatsRecordingSystem(
            biomeMovementCostProfile: new BiomePressureProfile(1f, 1f, 1.25f, 1f),
            biomeBasalCostProfile: new BiomePressureProfile(1f, 1f, 1.5f, 1f),
            biomeSpeedProfile: new BiomePressureProfile(1f, 1f, 0.75f, 1f),
            enableSeasons: true,
            seasonLengthSeconds: 4f,
            seasonFertilityAmplitude: 0.5f,
            seasonPhaseOffsetSeconds: 1f)]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    var brainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager(4));
    var parentId = simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 5f, brainId: brainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(30f, 20f), energy: 7f, generation: 2, brainId: brainId);
    var childId = simulation.State.Creatures[1].Id;
    var seeingCreature = simulation.State.Creatures[0];
    seeingCreature.Senses = new CreatureSenseState
    {
        FoodDetected = true,
        PlantDetected = true,
        VisibleFoodDensity = 0.5f,
        VisiblePlantDensity = 0.4f,
        VisibleMeatDensity = 0.1f,
        MeatScentDetected = true,
        MeatScentDensity = 0.3f,
        RottenMeatScentDetected = true,
        RottenMeatScentDensity = 0.1f,
        CreatureSimilarityScentDetected = true,
        CreatureSimilarityScentDensity = 0.8f,
        SoundDetected = true,
        SoundDensity = 0.3f,
        SoundToneClarity = 0.6f,
        CreatureDetected = true,
        VisibleCreatureDensity = 0.05f,
        CreatureContactSimilarity = 0.9f,
        CanGrabCreature = 1f,
        EggReserveRatio = 0.5f,
        EnergySurplusRatio = 0.25f,
        FatRatio = 0.5f,
        MassBurdenRatio = 0.1f,
        RecentFoodSuccess = 0.75f,
        RecentFoodEnergyYield = 0.6f,
        TenderPlantPayoffTrace = 0.9f,
        RichPlantPayoffTrace = 0.3f,
        ToughPlantPayoffTrace = 0.1f,
        ReproductionReadiness = 1f,
        ForwardObstacle = 0.5f,
        LeftObstacle = 0.25f,
        RightObstacle = 0.1f
    };
    seeingCreature.Actions = new CreatureActionState
    {
        WantsReproduce = true,
        WantsAttack = true,
        WantsGrab = true,
        AttackOutput = 0.8f,
        GrabOutput = 0.7f,
        SoundAmplitude = 0.6f
    };
    seeingCreature.IsTouchingFood = true;
    seeingCreature.IsTouchingCreature = true;
    seeingCreature.HeldCreatureId = childId;
    seeingCreature.GrabStrength = 0.5f;
    seeingCreature.LastMovementBlocked = true;
    seeingCreature.LastCaloriesEaten = 4.25f;
    seeingCreature.LastPlantCaloriesEaten = 2.5f;
    seeingCreature.LastCarcassCaloriesEaten = 1f;
    seeingCreature.LastEggCaloriesEaten = 0.5f;
    seeingCreature.LastLivePreyCaloriesEaten = 0.25f;
    seeingCreature.LastFreshMeatCaloriesEaten = 1f;
    seeingCreature.LastCaloriesDigested = 3.5f;
    seeingCreature.LastPlantDigestedEnergy = 2f;
    seeingCreature.LastMeatDigestedEnergy = 1.5f;
    seeingCreature.LastRottenMeatDamage = 0.03f;
    seeingCreature.FatCalories = 6f;
    seeingCreature.LastFatStoredCalories = 1f;
    seeingCreature.GutPlantCalories = 20f;
    seeingCreature.GutMeatCalories = 5f;
    seeingCreature.LastAttackDamageDealt = 0.2f;
    seeingCreature.SecondsSinceLastMeal = 2f;
    seeingCreature.LastDistanceTraveled = 3f;
    seeingCreature.DistanceSinceLastMeal = 8f;
    seeingCreature.MemoryVector = new SimVector2(0.2f, 0f);
    simulation.State.Creatures[0] = seeingCreature;
    var searchingCreature = simulation.State.Creatures[1];
    searchingCreature.Senses = new CreatureSenseState
    {
        FoodDetected = true,
        MeatDetected = true,
        VisibleFoodDensity = 0.25f,
        VisiblePlantDensity = 0.05f,
        VisibleMeatDensity = 0.2f,
        VisibleMeatFreshness = MeatQuality.MinimumFreshness,
        MeatScentDetected = true,
        MeatScentDensity = 0.7f,
        RottenMeatScentDetected = true,
        RottenMeatScentDensity = 0.5f,
        CreatureSimilarityScentDetected = true,
        CreatureSimilarityScentDensity = 0.4f,
        SoundDetected = false,
        SoundDensity = 0f,
        SoundToneClarity = 0f,
        VisibleCreatureDensity = 0.15f,
        CreatureContactSimilarity = 0.2f,
        EggReserveRatio = 0.25f,
        EnergySurplusRatio = 0.05f,
        FatRatio = 0.25f,
        MassBurdenRatio = 0.05f,
        RecentFoodSuccess = 0.25f,
        RecentFoodEnergyYield = 0.2f,
        TenderPlantPayoffTrace = 0.1f,
        RichPlantPayoffTrace = 0.7f,
        ToughPlantPayoffTrace = 0.2f,
        ForwardObstacle = 0.25f,
        LeftObstacle = 0.5f,
        RightObstacle = 0.2f
    };
    searchingCreature.SecondsSinceLastMeal = 6f;
    searchingCreature.LastDistanceTraveled = 5f;
    searchingCreature.DistanceSinceLastMeal = 12f;
    searchingCreature.Actions = new CreatureActionState { AttackOutput = 0.1f };
    searchingCreature.FatCalories = 2f;
    searchingCreature.LastFatReleasedCalories = 0.4f;
    searchingCreature.IsTouchingCreature = true;
    searchingCreature.GrabbedByCreatureId = parentId;
    searchingCreature.GrabPressure = 0.5f;
    simulation.State.Creatures[1] = searchingCreature;
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(10f, 10f),
        Radius = 2f,
        Calories = 8f,
        MaxCalories = 10f,
        RegrowthCaloriesPerSecond = 0f
    });
    simulation.State.SpawnEgg(
        genomeId: genomeId,
        brainId: -1,
        parentId: parentId,
        position: new SimVector2(40f, 20f),
        energy: 6f,
        incubationSeconds: 5f,
        generation: 1);

    simulation.Step();

    AssertEqual(1, simulation.State.Stats.Snapshots.Count, "Snapshot count");
    var snapshot = simulation.State.Stats.Snapshots[0];
    AssertEqual(0L, snapshot.Tick, "Snapshot tick is captured before clock advance");
    AssertClose(0.25f, snapshot.SeasonPhase, 0.000001, "Snapshot season phase");
    AssertClose(1.5f, snapshot.SeasonFertilityMultiplier, 0.000001, "Snapshot season fertility");
    AssertEqual(2, snapshot.CreatureCount, "Snapshot creature count");
    AssertEqual(1, snapshot.EggCount, "Snapshot egg count");
    AssertEqual(1, snapshot.ResourceCount, "Snapshot resource count");
    AssertEqual(1, snapshot.PlantResourceCount, "Snapshot plant resource count");
    AssertEqual(0, snapshot.MeatResourceCount, "Snapshot meat resource count");
    AssertEqual(0, snapshot.DormantPlantResourceCount, "Snapshot dormant plant count");
    AssertClose(0f, snapshot.TotalDormantPlantSecondsRemaining, 0.000001, "Snapshot dormant plant remaining total");
    AssertClose(0f, snapshot.AverageDormantPlantSecondsRemaining, 0.000001, "Snapshot average dormant plant remaining");
    AssertClose(0.01f, snapshot.PlantPatchOccupiedCellShare, 0.000001, "Snapshot plant occupied cell share");
    AssertClose(1f, snapshot.PlantPatchTopDecileCaloriesShare, 0.000001, "Snapshot plant top decile calorie share");
    AssertClose(9.949874f, snapshot.PlantPatchiness, 0.00001, "Snapshot plant patchiness");
    AssertEqual(0, snapshot.LocalFertilityCellCount, "Snapshot local fertility cell count");
    AssertClose(1f, snapshot.AverageLocalFertilityMultiplier, 0.000001, "Snapshot average local fertility");
    AssertClose(1f, snapshot.MinimumLocalFertilityMultiplier, 0.000001, "Snapshot minimum local fertility");
    AssertClose(0f, snapshot.DepletedLocalFertilityCellShare, 0.000001, "Snapshot depleted local fertility share");
    AssertEqual(1, snapshot.GenomeCount, "Snapshot genome count");
    AssertEqual(1, snapshot.BrainCount, "Snapshot brain count");
    AssertClose(4f, snapshot.AverageBrainHiddenNodeCount, 0.000001, "Snapshot average hidden nodes");
    AssertEqual(4, snapshot.MaxBrainHiddenNodeCount, "Snapshot max hidden nodes");
    AssertClose(13.1f / (4f * NeuralBrainSchema.InputCount), snapshot.AverageBrainHiddenInputWeightMagnitude, 0.000001, "Snapshot hidden input weight magnitude");
    AssertClose(0f, snapshot.AverageBrainHiddenOutputWeightMagnitude, 0.000001, "Snapshot hidden output weight magnitude");
    AssertClose(0f, snapshot.ActiveBrainHiddenOutputShare, 0.000001, "Snapshot active hidden output share");
    AssertEqual(0, snapshot.RtNeatBrainCount, "Snapshot rtNEAT brain count");
    AssertClose(0f, snapshot.RtNeatBrainShare, 0.000001, "Snapshot rtNEAT brain share");
    AssertClose(0f, snapshot.AverageRtNeatHiddenNodeCount, 0.000001, "Snapshot average rtNEAT hidden nodes");
    AssertEqual(0, snapshot.MaxRtNeatHiddenNodeCount, "Snapshot max rtNEAT hidden nodes");
    AssertClose(0f, snapshot.AverageRtNeatConnectionCount, 0.000001, "Snapshot average rtNEAT connections");
    AssertEqual(0, snapshot.MaxRtNeatConnectionCount, "Snapshot max rtNEAT connections");
    AssertClose(0f, snapshot.AverageRtNeatEnabledConnectionCount, 0.000001, "Snapshot average rtNEAT enabled connections");
    AssertEqual(0, snapshot.MaxRtNeatEnabledConnectionCount, "Snapshot max rtNEAT enabled connections");
    AssertEqual(2, snapshot.MaxGeneration, "Snapshot max generation");
    AssertClose(12f, snapshot.TotalCreatureEnergy, 0.000001, "Snapshot creature energy");
    AssertClose(8f, snapshot.TotalFatCalories, 0.000001, "Snapshot fat energy");
    AssertClose(6f, snapshot.TotalEggEnergy, 0.000001, "Snapshot egg energy");
    AssertClose(0.5f, snapshot.TotalEggHealth, 0.000001, "Snapshot egg health");
    AssertClose(8f, snapshot.TotalResourceCalories, 0.000001, "Snapshot resource calories");
    AssertClose(8f, snapshot.TotalPlantCalories, 0.000001, "Snapshot plant calories");
    AssertClose(0f, snapshot.TotalMeatCalories, 0.000001, "Snapshot meat calories");
    AssertEqual(0, snapshot.BarrenCreatureCount, "Barren creature count");
    AssertEqual(0, snapshot.SparseCreatureCount, "Sparse creature count");
    AssertEqual(2, snapshot.GrasslandCreatureCount, "Grassland creature count");
    AssertEqual(0, snapshot.RichCreatureCount, "Rich creature count");
    AssertClose(1.25f, snapshot.AverageBiomeMovementCostMultiplier, 0.000001, "Average biome movement cost");
    AssertClose(1.5f, snapshot.AverageBiomeBasalCostMultiplier, 0.000001, "Average biome basal cost");
    AssertClose(0.75f, snapshot.AverageBiomeSpeedMultiplier, 0.000001, "Average biome speed");
    AssertEqual(1, snapshot.ObstacleBlockedCreatureCount, "Obstacle blocked creature count");
    AssertEqual(2, snapshot.ObstacleSensedCreatureCount, "Obstacle sensed creature count");
    AssertClose(0.375f, snapshot.AverageForwardObstacle, 0.000001, "Average forward obstacle sense");
    AssertClose(0.375f, snapshot.AverageLeftObstacle, 0.000001, "Average left obstacle sense");
    AssertClose(0.15f, snapshot.AverageRightObstacle, 0.000001, "Average right obstacle sense");
    AssertClose(0f, snapshot.BarrenPlantCalories, 0.000001, "Barren plant calories");
    AssertClose(8f, snapshot.GrasslandPlantCalories, 0.000001, "Grassland plant calories");
    AssertClose(0f, snapshot.RichMeatCalories, 0.000001, "Rich meat calories");
    AssertClose(0f, snapshot.BarrenCaloriesEatenPerSecond, 0.000001, "Barren calories eaten per second");
    AssertClose(4.25f, snapshot.GrasslandCaloriesEatenPerSecond, 0.000001, "Grassland calories eaten per second");
    AssertEqual(0, snapshot.BarrenDeathCount, "Barren death count");
    AssertEqual(0, snapshot.GrasslandDeathCount, "Grassland death count");
    AssertClose(2f, simulation.State.Stats.SpatialHeatmaps.CreatureExposureSeconds.Sum(), 0.000001, "Creature exposure seconds");
    AssertClose(2f, simulation.State.Stats.SpatialHeatmaps.BiomeCreatureExposureSeconds.Sum(), 0.000001, "Biome exposure seconds");
    AssertClose(25f, snapshot.AverageCreatureX, 0.000001, "Average creature x");
    AssertClose(30f, snapshot.MaxCreatureX, 0.000001, "Max creature x");
    AssertClose(25f, snapshot.AverageMaxCreatureXReached, 0.000001, "Average max creature x reached");
    AssertClose(30f, snapshot.MaxCreatureXReached, 0.000001, "Snapshot max x reached");
    AssertClose(30f, snapshot.RunMaxCreatureXReached, 0.000001, "Run max x reached");
    AssertClose(0.03f, snapshot.CurrentEastProgressShare, 0.000001, "Current east progress share");
    AssertClose(0.03f, snapshot.RunEastProgressShare, 0.000001, "Run east progress share");
    AssertEqual(2, snapshot.FoodDetectedCreatureCount, "Food detected count");
    AssertEqual(1, snapshot.PlantDetectedCreatureCount, "Plant detected count");
    AssertEqual(1, snapshot.MeatDetectedCreatureCount, "Meat detected count");
    AssertEqual(2, snapshot.MeatScentDetectedCreatureCount, "Meat scent detected count");
    AssertEqual(1, snapshot.CreatureDetectedCreatureCount, "Creature detected count");
    AssertEqual(1, snapshot.FoodContactCreatureCount, "Food contact count");
    AssertEqual(1, snapshot.EatingCreatureCount, "Eating creature count");
    AssertEqual(1, snapshot.ReproductionReadyCreatureCount, "Reproduction ready count");
    AssertEqual(1, snapshot.ReproductionIntentCreatureCount, "Reproduction intent count");
    AssertClose(0.375f, snapshot.AverageVisibleFoodDensity, 0.000001, "Average visible food density");
    AssertClose(0.225f, snapshot.AverageVisiblePlantDensity, 0.000001, "Average visible plant density");
    AssertClose(0.15f, snapshot.AverageVisibleMeatDensity, 0.000001, "Average visible meat density");
    AssertEqual(0, snapshot.FreshMeatDetectedCreatureCount, "Fresh meat detected count");
    AssertEqual(1, snapshot.StaleMeatDetectedCreatureCount, "Stale meat detected count");
    AssertEqual(1, snapshot.StaleMeatAvoidedCreatureCount, "Stale meat avoided count");
    AssertClose(MeatQuality.MinimumFreshness, snapshot.AverageVisibleMeatFreshness, 0.000001, "Average visible meat freshness");
    AssertClose(0.5f, snapshot.AverageMeatScentDensity, 0.000001, "Average meat scent density");
    AssertEqual(2, snapshot.RottenMeatScentDetectedCreatureCount, "Rotten meat scent detected count");
    AssertClose(0.3f, snapshot.AverageRottenMeatScentDensity, 0.000001, "Average rotten meat scent density");
    AssertClose(0.1f, snapshot.AverageVisibleCreatureDensity, 0.000001, "Average visible creature density");
    AssertEqual(2, snapshot.CreatureSimilarityScentDetectedCreatureCount, "Creature similarity scent detected count");
    AssertClose(0.6f, snapshot.AverageCreatureSimilarityScentDensity, 0.000001, "Average creature similarity scent density");
    AssertClose(4.25f, snapshot.TotalCaloriesEatenPerSecond, 0.000001, "Calories eaten per second");
    AssertClose(2.5f, snapshot.TotalPlantCaloriesEatenPerSecond, 0.000001, "Plant calories eaten per second");
    AssertClose(1f, snapshot.TotalCarcassCaloriesEatenPerSecond, 0.000001, "Carcass calories eaten per second");
    AssertClose(0.5f, snapshot.TotalEggCaloriesEatenPerSecond, 0.000001, "Egg calories eaten per second");
    AssertClose(0.25f, snapshot.TotalLivePreyCaloriesEatenPerSecond, 0.000001, "Live prey calories eaten per second");
    AssertClose(3.5f, snapshot.TotalCaloriesDigestedPerSecond, 0.000001, "Calories digested per second");
    AssertClose(2f, snapshot.TotalPlantDigestedEnergyPerSecond, 0.000001, "Plant energy digested per second");
    AssertClose(1.5f, snapshot.TotalMeatDigestedEnergyPerSecond, 0.000001, "Meat energy digested per second");
    AssertClose(1.75f / 4.25f, snapshot.MeatCaloriesEatenShare, 0.000001, "Meat calories eaten share");
    AssertClose(0.25f / 4.25f, snapshot.FreshKillCaloriesEatenShare, 0.000001, "Fresh kill calories eaten share");
    AssertClose(0f, snapshot.AverageMeatFreshness, 0.000001, "Average meat freshness without meat resources");
    AssertClose(1f, snapshot.TotalFreshMeatCaloriesEatenPerSecond, 0.000001, "Fresh meat calories eaten per second");
    AssertClose(0f, snapshot.TotalStaleMeatCaloriesEatenPerSecond, 0.000001, "Stale meat calories eaten per second");
    AssertClose(1f, snapshot.FreshMeatCaloriesEatenShare, 0.000001, "Fresh carcass share");
    AssertClose(0f, snapshot.StaleMeatCaloriesEatenShare, 0.000001, "Stale carcass share");
    AssertClose(0.03f, snapshot.TotalRottenMeatDamagePerSecond, 0.000001, "Rotten meat damage per second");
    AssertEqual(1, snapshot.RottenMeatDamagedCreatureCount, "Rotten meat damaged creature count");
    AssertClose(1.5f / 3.5f, snapshot.MeatDigestedEnergyShare, 0.000001, "Meat digested energy share");
    AssertClose(0.5f, snapshot.AverageGutFillRatio, 0.000001, "Average gut fill");
    AssertClose(0.4f, snapshot.AverageGutPlantShare, 0.000001, "Average gut plant share");
    AssertClose(0.1f, snapshot.AverageGutMeatShare, 0.000001, "Average gut meat share");
    AssertEqual(1, snapshot.AttackingCreatureCount, "Attacking creature count");
    AssertEqual(2, snapshot.CreatureContactCreatureCount, "Creature contact count");
    AssertEqual(1, snapshot.SimilarCreatureContactCreatureCount, "Similar creature contact count");
    AssertClose(0.55f, snapshot.AverageCreatureContactSimilarity, 0.000001, "Average creature contact similarity");
    AssertEqual(1, snapshot.AttackIntentCreatureCount, "Attack intent count");
    AssertEqual(1, snapshot.AttackIntentWhileTouchingCreatureCount, "Attack intent while touching count");
    AssertEqual(1, snapshot.AttackIntentWhileTouchingSimilarCreatureCount, "Attack intent while touching similar count");
    AssertEqual(1, snapshot.AttackNoIntentContactCreatureCount, "Contact without attack intent count");
    AssertEqual(2, snapshot.RawAttackPositiveCreatureCount, "Raw attack positive count");
    AssertEqual(1, snapshot.RawAttackNearGateCreatureCount, "Raw attack near gate count");
    AssertEqual(1, snapshot.RawAttackNearGateWhileTouchingCreatureCount, "Raw attack near gate while touching count");
    AssertClose(0.45f, snapshot.AverageAttackOutput, 0.000001, "Average attack output");
    AssertClose(0.45f, snapshot.AverageTouchingAttackOutput, 0.000001, "Average touching attack output");
    AssertClose(0.2f, snapshot.TotalAttackDamagePerSecond, 0.000001, "Attack damage per second");
    AssertEqual(1, snapshot.GrabIntentCreatureCount, "Grab intent count");
    AssertEqual(1, snapshot.CanGrabCreatureCount, "Can grab creature count");
    AssertEqual(1, snapshot.GrabIntentWhileCanGrabCreatureCount, "Grab intent while can grab count");
    AssertEqual(0, snapshot.GrabIntentWithoutCanGrabCreatureCount, "Grab intent without can grab count");
    AssertEqual(1, snapshot.HoldingCreatureCount, "Holding creature count");
    AssertEqual(1, snapshot.GrabbedCreatureCount, "Grabbed creature count");
    AssertClose(0.35f, snapshot.AverageGrabOutput, 0.000001, "Average grab output");
    AssertClose(0.7f, snapshot.AverageCanGrabGrabOutput, 0.000001, "Average can-grab grab output");
    AssertClose(0.5f, snapshot.AverageGrabPressure, 0.000001, "Average grab pressure");
    AssertClose(0.5f, snapshot.AverageGrabStrength, 0.000001, "Average grab strength");
    AssertEqual(1, snapshot.SoundEmittingCreatureCount, "Sound emitting count");
    AssertEqual(1, snapshot.SoundHeardCreatureCount, "Sound heard count");
    AssertClose(0.3f, snapshot.AverageSoundAmplitude, 0.000001, "Average sound amplitude");
    AssertClose(0.15f, snapshot.AverageSoundDensity, 0.000001, "Average sound density");
    AssertClose(0.3f, snapshot.AverageSoundToneClarity, 0.000001, "Average sound clarity");
    AssertClose(CreatureGenome.Baseline.DietaryAdaptation, snapshot.AverageDietaryAdaptation, 0.000001, "Average dietary adaptation");
    AssertClose(CreatureGenome.Baseline.CarrionAdaptation, snapshot.AverageCarrionAdaptation, 0.000001, "Average carrion adaptation");
    AssertClose(CreatureGenome.Baseline.TenderPlantAdaptation, snapshot.AverageTenderPlantAdaptation, 0.000001, "Average tender plant adaptation");
    AssertClose(CreatureGenome.Baseline.RichPlantAdaptation, snapshot.AverageRichPlantAdaptation, 0.000001, "Average rich plant adaptation");
    AssertClose(CreatureGenome.Baseline.ToughPlantAdaptation, snapshot.AverageToughPlantAdaptation, 0.000001, "Average tough plant adaptation");
    AssertClose(CreatureGenome.Baseline.BiteStrength, snapshot.AverageBiteStrength, 0.000001, "Average bite strength");
    AssertClose(CreatureGenome.Baseline.DamageResistance, snapshot.AverageDamageResistance, 0.000001, "Average damage resistance");
    AssertClose(CreatureGenome.Baseline.DietaryAdaptation, snapshot.AttackerAverageDietaryAdaptation, 0.000001, "Attacker dietary adaptation");
    AssertClose(CreatureGenome.Baseline.BiteStrength, snapshot.AttackerAverageBiteStrength, 0.000001, "Attacker bite strength");
    AssertClose(CreatureGenome.Baseline.DamageResistance, snapshot.AttackerAverageDamageResistance, 0.000001, "Attacker damage resistance");
    AssertClose(CreatureGenome.Baseline.DietaryAdaptation, snapshot.NonAttackerAverageDietaryAdaptation, 0.000001, "Non-attacker dietary adaptation");
    AssertClose(CreatureGenome.Baseline.BiteStrength, snapshot.NonAttackerAverageBiteStrength, 0.000001, "Non-attacker bite strength");
    AssertClose(CreatureGenome.Baseline.DamageResistance, snapshot.NonAttackerAverageDamageResistance, 0.000001, "Non-attacker damage resistance");
    AssertClose(4f, snapshot.AverageSecondsSinceLastMeal, 0.000001, "Average seconds since meal");
    AssertClose(8f, snapshot.TotalDistanceTraveledPerSecond, 0.000001, "Distance traveled per second");
    AssertClose(10f, snapshot.AverageDistanceSinceLastMeal, 0.000001, "Average distance since meal");
    AssertClose(4.25f / 8f, snapshot.CaloriesEatenPerDistance, 0.000001, "Calories eaten per distance");
    AssertClose(3.5f / 8f, snapshot.CaloriesDigestedPerDistance, 0.000001, "Calories digested per distance");
    AssertClose(4.25f / 2f, snapshot.CaloriesEatenPerFoodVisionEvent, 0.000001, "Calories eaten per food vision event");
    AssertClose(1f, snapshot.AverageBirthInvestmentRatio, 0.000001, "Average birth investment");
    AssertClose(0.375f, snapshot.AverageEggReserveRatio, 0.000001, "Average egg reserve ratio");
    AssertClose(0.15f, snapshot.AverageEnergySurplusRatio, 0.000001, "Average energy surplus ratio");
    AssertClose(0.375f, snapshot.AverageFatRatio, 0.000001, "Average fat ratio");
    AssertClose(0.075f, snapshot.AverageMassBurdenRatio, 0.000001, "Average fat mass burden");
    AssertClose(0.993142843f, snapshot.AverageFatSpeedMultiplier, 0.000001, "Average fat speed multiplier");
    AssertClose(CreatureGenome.Baseline.FatStorageCapacityCalories, snapshot.AverageFatStorageCapacityCalories, 0.000001, "Average fat storage capacity");
    AssertClose(CreatureGenome.Baseline.FatStorageEfficiency, snapshot.AverageFatStorageEfficiency, 0.000001, "Average fat storage efficiency");
    AssertClose(1f, snapshot.TotalFatStoredCaloriesPerSecond, 0.000001, "Fat stored calories per second");
    AssertClose(0.4f, snapshot.TotalFatReleasedCaloriesPerSecond, 0.000001, "Fat released calories per second");
    AssertClose(0.5f, snapshot.AverageRecentFoodSuccess, 0.000001, "Average recent food success");
    AssertClose(0.4f, snapshot.AverageRecentFoodEnergyYield, 0.000001, "Average recent food energy yield");
    AssertClose(0.5f, snapshot.AverageTenderPlantPayoffTrace, 0.000001, "Average tender plant payoff trace");
    AssertClose(0.5f, snapshot.AverageRichPlantPayoffTrace, 0.000001, "Average rich plant payoff trace");
    AssertClose(0.15f, snapshot.AverageToughPlantPayoffTrace, 0.000001, "Average tough plant payoff trace");
    AssertEqual(1, snapshot.ActiveMemoryCreatureCount, "Active memory creature count");
    AssertClose(0.1f, snapshot.AverageMemoryStrength, 0.000001, "Average memory strength");
    AssertClose(1f, snapshot.MemoryUserFoodContactShare, 0.000001, "Memory food contact share");
    AssertClose(0f, snapshot.NonMemoryUserFoodContactShare, 0.000001, "Non-memory food contact share");
    AssertClose(1f, snapshot.MemoryUserEatingShare, 0.000001, "Memory eating share");
    AssertClose(0f, snapshot.NonMemoryUserEatingShare, 0.000001, "Non-memory eating share");
    AssertClose(4.25f / 3f, snapshot.MemoryUserCaloriesEatenPerDistance, 0.000001, "Memory calories per distance");
    AssertClose(0f, snapshot.NonMemoryUserCaloriesEatenPerDistance, 0.000001, "Non-memory calories per distance");
    AssertClose(2f, snapshot.MemoryUserAverageSecondsSinceLastMeal, 0.000001, "Memory seconds since meal");
    AssertClose(6f, snapshot.NonMemoryUserAverageSecondsSinceLastMeal, 0.000001, "Non-memory seconds since meal");
    AssertClose(8f, snapshot.MemoryUserAverageDistanceSinceLastMeal, 0.000001, "Memory distance since meal");
    AssertClose(12f, snapshot.NonMemoryUserAverageDistanceSinceLastMeal, 0.000001, "Non-memory distance since meal");
    AssertClose(0.75f, snapshot.MemoryUserAverageRecentFoodSuccess, 0.000001, "Memory recent food success");
    AssertClose(0.25f, snapshot.NonMemoryUserAverageRecentFoodSuccess, 0.000001, "Non-memory recent food success");
    AssertClose(0f, snapshot.MemoryUserAverageGeneration, 0.000001, "Memory average generation");
    AssertClose(2f, snapshot.NonMemoryUserAverageGeneration, 0.000001, "Non-memory average generation");
    AssertClose(0.02f, snapshot.MemoryUserAverageMaxXProgressShare, 0.000001, "Memory average max x progress");
    AssertClose(0.03f, snapshot.NonMemoryUserAverageMaxXProgressShare, 0.000001, "Non-memory average max x progress");
    AssertClose(0f, snapshot.MemoryUserRightRegionShare, 0.000001, "Memory right region share");
    AssertClose(0f, snapshot.NonMemoryUserRightRegionShare, 0.000001, "Non-memory right region share");
    AssertClose(1f, snapshot.AverageEggHealthRatio, 0.000001, "Average egg health ratio");
    var expectedVisionRange = CreatureGrowth.EffectiveSenseRadius(simulation.State.Creatures[0], CreatureGenome.Baseline);
    AssertClose(expectedVisionRange, snapshot.AverageVisionRange, 0.000001, "Average vision range");
    AssertClose(CreatureGenome.Baseline.VisionAngleRadians, snapshot.AverageVisionAngleRadians, 0.000001, "Average vision angle");
    AssertEqual(2, snapshot.CreatureBirthCount, "Snapshot birth count");
    AssertEqual(1, snapshot.EggLaidCount, "Snapshot egg laid count");
    AssertEqual(0, snapshot.EggHatchedCount, "Snapshot egg hatch count");
    AssertEqual(0, snapshot.EggDeathCount, "Snapshot egg death count");
    AssertEqual(0, snapshot.EggPredationDeathCount, "Snapshot egg predation death count");
    AssertEqual(0, snapshot.CreatureDeathCount, "Snapshot death count");
    AssertEqual(0, snapshot.RottenMeatDeathCount, "Snapshot rotten meat death count");
    AssertEqual(0, snapshot.PlantDepletionCount, "Snapshot plant depletion count");
    AssertEqual(0, snapshot.PlantLocalDispersalCount, "Snapshot plant local dispersal count");
    AssertEqual(0, snapshot.PlantClusterRelocationCount, "Snapshot plant cluster relocation count");
    AssertEqual(0, snapshot.PlantGlobalRelocationCount, "Snapshot plant global relocation count");
    AssertEqual(0, snapshot.PlantDormancyStartedCount, "Snapshot plant dormancy started count");
    AssertEqual(0, snapshot.PlantDormancyCompletedCount, "Snapshot plant dormancy completed count");
    AssertClose(0f, snapshot.AverageLifespanSeconds, 0.000001, "Snapshot average lifespan without deaths");
    AssertClose(0f, snapshot.MedianLifespanSeconds, 0.000001, "Snapshot median lifespan without deaths");
}

static void StatsRecordingIgnoresExtinctBrainPayloads()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 114,
        systems: [new DeathSystem(), new StatsRecordingSystem()]);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    var extinctBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager(16));
    var activeBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager(4));

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 2f, brainId: extinctBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(40f, 20f), energy: 8f, brainId: activeBrainId);
    var extinctCreature = simulation.State.Creatures[0];
    extinctCreature.Energy = 0f;
    simulation.State.Creatures[0] = extinctCreature;

    simulation.Step();

    var snapshot = simulation.State.Stats.Snapshots.Single();
    AssertEqual(1, snapshot.CreatureCount, "Snapshot living creature count");
    AssertEqual(2, snapshot.BrainCount, "Snapshot retained brain payload count");
    AssertClose(4f, snapshot.AverageBrainHiddenNodeCount, 0.000001, "Snapshot active average hidden nodes");
    AssertEqual(4, snapshot.MaxBrainHiddenNodeCount, "Snapshot active max hidden nodes");
    AssertEqual(0, snapshot.RtNeatBrainCount, "Snapshot active rtNEAT brain count");
}

static void StatsRecordingReportsRtNeatTopologyTelemetry()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 115,
        systems: [new StatsRecordingSystem()]);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    var firstBrainId = simulation.State.AddBrain(CreateRtNeatTopologyTestBrain(hiddenNodeCount: 1, disabledHiddenOutputCount: 1));
    var secondBrainId = simulation.State.AddBrain(CreateRtNeatTopologyTestBrain(hiddenNodeCount: 2));
    _ = simulation.State.AddBrain(CreateRtNeatTopologyTestBrain(hiddenNodeCount: 4));

    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 8f, brainId: firstBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(40f, 20f), energy: 8f, brainId: secondBrainId);

    simulation.Step();

    var snapshot = simulation.State.Stats.Snapshots.Single();
    AssertEqual(2, snapshot.CreatureCount, "Snapshot rtNEAT creature count");
    AssertEqual(3, snapshot.BrainCount, "Snapshot retained rtNEAT brain payload count");
    AssertEqual(2, snapshot.RtNeatBrainCount, "Snapshot active rtNEAT brain count");
    AssertClose(1f, snapshot.RtNeatBrainShare, 0.000001, "Snapshot active rtNEAT brain share");
    AssertClose(1.5f, snapshot.AverageBrainHiddenNodeCount, 0.000001, "Snapshot active graph average hidden nodes");
    AssertEqual(2, snapshot.MaxBrainHiddenNodeCount, "Snapshot active graph max hidden nodes");
    AssertClose(1.5f, snapshot.AverageRtNeatHiddenNodeCount, 0.000001, "Snapshot average rtNEAT hidden nodes");
    AssertEqual(2, snapshot.MaxRtNeatHiddenNodeCount, "Snapshot max rtNEAT hidden nodes");
    AssertClose(9f, snapshot.AverageRtNeatConnectionCount, 0.000001, "Snapshot average rtNEAT connections");
    AssertEqual(10, snapshot.MaxRtNeatConnectionCount, "Snapshot max rtNEAT connections");
    AssertClose(8.5f, snapshot.AverageRtNeatEnabledConnectionCount, 0.000001, "Snapshot average rtNEAT enabled connections");
    AssertEqual(10, snapshot.MaxRtNeatEnabledConnectionCount, "Snapshot max rtNEAT enabled connections");
}

static BrainGenome CreateRtNeatTopologyTestBrain(int hiddenNodeCount, int disabledHiddenOutputCount = 0)
{
    var starter = RtNeatBrainGenome.CreateStarterForager();
    var nodes = starter.Nodes.ToList();
    var connections = starter.Connections.ToList();
    var inputIds = starter.Nodes
        .Where(node => node.Kind == RtNeatNodeKind.Input)
        .Select(node => node.Id)
        .ToArray();
    var outputId = starter.Nodes.First(node => node.Kind == RtNeatNodeKind.Output).Id;
    var nextNodeId = starter.NextNodeId;
    var nextInnovationId = starter.NextInnovationId;

    for (var i = 0; i < hiddenNodeCount; i++)
    {
        var hiddenNodeId = nextNodeId++;
        nodes.Add(new RtNeatNodeGene
        {
            Id = hiddenNodeId,
            Kind = RtNeatNodeKind.Hidden,
            Key = $"hidden.test.{i}",
            Activation = RtNeatActivationKind.Tanh,
            Bias = 0.1f * i,
            Depth = 0.5f
        });
        connections.Add(new RtNeatConnectionGene
        {
            InnovationId = nextInnovationId++,
            SourceNodeId = inputIds[i % inputIds.Length],
            TargetNodeId = hiddenNodeId,
            Weight = 1f
        });
        connections.Add(new RtNeatConnectionGene
        {
            InnovationId = nextInnovationId++,
            SourceNodeId = hiddenNodeId,
            TargetNodeId = outputId,
            Weight = 0.75f,
            Enabled = i >= disabledHiddenOutputCount
        });
    }

    return BrainGenome.FromRtNeat(starter with
    {
        Nodes = nodes.ToArray(),
        Connections = connections.ToArray(),
        NextNodeId = nextNodeId,
        NextInnovationId = nextInnovationId
    });
}

static void StatsRecordingReportsBiomePressureTelemetry()
{
    var scenario = new SimulationScenario
    {
        Seed = 137,
        EnableBiomes = true,
        BiomeMapKind = BiomeMapKind.VerticalEdgeBands,
        WorldWidth = 800f,
        WorldHeight = 200f,
        BiomeCellSize = 100f,
        ResourceVoidBorderWidth = 0f,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f
    };
    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);

    simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 10f);
    simulation.State.SpawnCreature(genomeId, new SimVector2(350f, 50f), energy: 10f);
    var richCreature = simulation.State.Creatures[0];
    richCreature.LastCaloriesEaten = 3f;
    simulation.State.Creatures[0] = richCreature;
    var barrenCreature = simulation.State.Creatures[1];
    barrenCreature.LastCaloriesEaten = 5f;
    simulation.State.Creatures[1] = barrenCreature;

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Plant,
        Position = new SimVector2(50f, 60f),
        Radius = 2f,
        Calories = 11f,
        MaxCalories = 12f
    });
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Meat,
        Position = new SimVector2(350f, 60f),
        Radius = 2f,
        Calories = 7f,
        MaxCalories = 7f
    });

    var dyingCreatureId = simulation.State.SpawnCreature(genomeId, new SimVector2(350f, 80f), energy: 1f);
    var dyingCreature = simulation.State.Creatures.Single(creature => creature.Id == dyingCreatureId);
    dyingCreature.Energy = 0f;
    simulation.State.Creatures[^1] = dyingCreature;
    new DeathSystem(meatCaloriesPerBodyRadius: 0f, meatEnergyFraction: 0f).Update(simulation.State, 1f);

    new StatsRecordingSystem(
        biomeSpeedProfile: scenario.CreateBiomeSpeedProfile()).Update(simulation.State, 1f);

    var snapshot = simulation.State.Stats.Snapshots.Single();
    AssertEqual(1, snapshot.RichCreatureCount, "Rich creature count");
    AssertEqual(1, snapshot.BarrenCreatureCount, "Barren creature count");
    AssertClose(11f, snapshot.RichPlantCalories, 0.000001, "Rich plant calories");
    AssertClose(7f, snapshot.BarrenMeatCalories, 0.000001, "Barren meat calories");
    AssertClose(3f, snapshot.RichCaloriesEatenPerSecond, 0.000001, "Rich calories eaten per second");
    AssertClose(5f, snapshot.BarrenCaloriesEatenPerSecond, 0.000001, "Barren calories eaten per second");
    AssertEqual(1, snapshot.BarrenDeathCount, "Barren death count");
    AssertEqual(0, snapshot.RichDeathCount, "Rich death count");
}

static void StatsRecordingReportsBiomeDeathCauses()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 300f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 1f
        },
        seed: 138,
        systems: []);
    simulation.State.SetBiomes(BiomeMap.CreateFromCells(
        simulation.State.Bounds,
        cellSize: 100f,
        cellCountX: 3,
        cellCountY: 1,
        [BiomeKind.Desert, BiomeKind.Forest, BiomeKind.Wetland]));

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 50f), energy: 10f);
    simulation.State.SpawnCreature(genomeId, new SimVector2(150f, 50f), energy: 10f);
    simulation.State.SpawnCreature(genomeId, new SimVector2(250f, 50f), energy: 10f);

    var starving = simulation.State.Creatures[0];
    starving.Energy = 0f;
    simulation.State.Creatures[0] = starving;

    var injured = simulation.State.Creatures[1];
    injured.Health = 0f;
    injured.LastAttackDamageTaken = 1f;
    injured.LastDamagingCreatureId = starving.Id;
    simulation.State.Creatures[1] = injured;

    var rotten = simulation.State.Creatures[2];
    rotten.Health = 0f;
    rotten.LastRottenMeatDamage = 1f;
    simulation.State.Creatures[2] = rotten;

    new DeathSystem(meatCaloriesPerBodyRadius: 0f, meatEnergyFraction: 0f).Update(simulation.State, 1f);

    var counts = simulation.State.Stats.CreatureDeathCausesByBiome;
    AssertEqual(3, counts.Total, "Biome cause death total");
    AssertEqual(1, counts.For(BiomeKind.Desert).Starvation, "Desert starvation deaths");
    AssertEqual(1, counts.For(BiomeKind.Forest).Injury, "Forest injury deaths");
    AssertEqual(1, counts.For(BiomeKind.Wetland).RottenMeat, "Wetland rotten meat deaths");
    AssertEqual(0, counts.For(BiomeKind.Grassland).Total, "Grassland cause deaths");

    var snapshot = SimulationSnapshot.Capture(
        new SimulationScenario
        {
            WorldWidth = 300f,
            WorldHeight = 100f,
            ResourceVoidBorderWidth = 0f,
            InitialCreatureCount = 0,
            InitialResourcesPerMillionArea = 0f
        },
        simulation);
    AssertEqual(1, snapshot.CreatureDeathCausesByBiome.For(BiomeKind.Desert).Starvation, "Snapshot desert starvation deaths");

    var restored = SimulationSnapshotJson.RestoreSimulation(snapshot);
    AssertEqual(
        1,
        restored.Simulation.State.Stats.CreatureDeathCausesByBiome.For(BiomeKind.Forest).Injury,
        "Restored forest injury deaths");
}

static void StatsRecordingReportsLifespanSummary()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 131,
        systems:
        [
            new MetabolismSystem(),
            new DeathSystem(meatCaloriesPerBodyRadius: 0f),
            new StatsRecordingSystem()
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline with
    {
        BasalEnergyPerSecond = 1f
    });
    simulation.State.SpawnCreature(genomeId, new SimVector2(10f, 10f), energy: 2f);
    simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 10f), energy: 4f);
    simulation.State.SpawnCreature(genomeId, new SimVector2(30f, 10f), energy: 6f);

    simulation.RunSteps(7);

    var snapshot = simulation.State.Stats.Snapshots[^1];
    AssertEqual(3, snapshot.CreatureDeathCount, "Lifespan death count");
    AssertClose(3f, snapshot.AverageLifespanSeconds, 0.000001, "Average dead-creature lifespan");
    AssertClose(3f, snapshot.MedianLifespanSeconds, 0.000001, "Median dead-creature lifespan");
}

static void StatsRecordingHonorsSampleInterval()
{
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 13,
        systems: [new StatsRecordingSystem(sampleIntervalTicks: 3)]);

    simulation.RunSteps(7);

    AssertEqual(3, simulation.State.Stats.Snapshots.Count, "Snapshot count");
    AssertEqual(0L, simulation.State.Stats.Snapshots[0].Tick, "First sampled tick");
    AssertEqual(3L, simulation.State.Stats.Snapshots[1].Tick, "Second sampled tick");
    AssertEqual(6L, simulation.State.Stats.Snapshots[2].Tick, "Third sampled tick");
}

static void ScenarioFactorySeedsRequestedWorld()
{
    var scenario = new SimulationScenario
    {
        Seed = 14,
        WorldWidth = 1_000f,
        WorldHeight = 1_000f,
        InitialCreatureCount = 3,
        InitialResourcesPerMillionArea = 4f,
        StatsSnapshotIntervalTicks = 5
    };

    var first = SimulationScenarioFactory.CreateSimulation(scenario);
    var second = SimulationScenarioFactory.CreateSimulation(scenario);

    AssertEqual(3, first.State.Creatures.Count, "Creature count");
    AssertEqual(4, first.State.Resources.Count, "Resource count");
    AssertEqual(1, first.State.Genomes.Count, "Genome count");
    AssertEqual(1, first.State.Brains.Count, "Brain count");
    AssertEqual(3, first.State.Stats.CreatureBirthCount, "Birth count");

    for (var i = 0; i < first.State.Creatures.Count; i++)
    {
        AssertClose(first.State.Creatures[i].Position.X, second.State.Creatures[i].Position.X, 0.000001, $"Creature {i} x");
        AssertClose(first.State.Creatures[i].Position.Y, second.State.Creatures[i].Position.Y, 0.000001, $"Creature {i} y");
        AssertClose(first.State.Creatures[i].Energy, second.State.Creatures[i].Energy, 0.000001, $"Creature {i} energy");
    }

    for (var i = 0; i < first.State.Resources.Count; i++)
    {
        AssertClose(first.State.Resources[i].Position.X, second.State.Resources[i].Position.X, 0.000001, $"Resource {i} x");
        AssertClose(first.State.Resources[i].Position.Y, second.State.Resources[i].Position.Y, 0.000001, $"Resource {i} y");
        AssertClose(first.State.Resources[i].Calories, second.State.Resources[i].Calories, 0.000001, $"Resource {i} calories");
    }
}

static void ScenarioFactorySeedsRequestedPlantTypeMix()
{
    var scenario = new SimulationScenario
    {
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 12f,
        WorldWidth = 1_000f,
        WorldHeight = 1_000f,
        ResourceVoidBorderWidth = 0f,
        GenericPlantWeight = 0f,
        TenderPlantWeight = 1f,
        RichPlantWeight = 0f,
        ToughPlantWeight = 0f,
        EnableBiomes = false
    }.Validated();

    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);

    AssertEqual(12, simulation.State.Resources.Count, "Seeded plant count");
    for (var i = 0; i < simulation.State.Resources.Count; i++)
    {
        var resource = simulation.State.Resources[i];
        AssertEqual(PlantResourceKind.Tender, resource.PlantKind, $"Seeded plant type {i}");
        AssertTrue(resource.MaxCalories <= scenario.ResourceMaxCalories * 0.8001f, $"Tender max calories {i}");
        AssertTrue(resource.RegrowthCaloriesPerSecond >= scenario.ResourceRegrowthMin * 1.44f, $"Tender regrowth lower bound {i}");
    }
}

static void PlantTypeHabitatAffinityBiasesBiomeSampling()
{
    var scenario = new SimulationScenario
    {
        GenericPlantWeight = 0.25f,
        TenderPlantWeight = 0.25f,
        RichPlantWeight = 0.25f,
        ToughPlantWeight = 0.25f,
        EnablePlantTypeHabitatAffinity = true
    }.Validated();

    var barrenCounts = SamplePlantTypeCounts(scenario, BiomeKind.Barren, seed: 91);
    var grasslandCounts = SamplePlantTypeCounts(scenario, BiomeKind.Grassland, seed: 91);
    var richCounts = SamplePlantTypeCounts(scenario, BiomeKind.Rich, seed: 91);
    var forestCounts = SamplePlantTypeCounts(scenario, BiomeKind.Forest, seed: 91);
    var wetlandCounts = SamplePlantTypeCounts(scenario, BiomeKind.Wetland, seed: 91);

    AssertTrue(
        barrenCounts[(int)PlantResourceKind.Tough] > barrenCounts[(int)PlantResourceKind.Generic],
        "Barren biomes should favor tough plants over generic plants");
    AssertTrue(
        grasslandCounts[(int)PlantResourceKind.Tender] > grasslandCounts[(int)PlantResourceKind.Generic],
        "Grassland biomes should favor tender plants over generic plants");
    AssertTrue(
        richCounts[(int)PlantResourceKind.Rich] > richCounts[(int)PlantResourceKind.Generic],
        "Rich biomes should favor rich plants over generic plants");
    AssertTrue(
        forestCounts[(int)PlantResourceKind.Rich] > forestCounts[(int)PlantResourceKind.Generic],
        "Forest biomes should favor rich plants over generic plants");
    AssertTrue(
        wetlandCounts[(int)PlantResourceKind.Rich] > wetlandCounts[(int)PlantResourceKind.Generic],
        "Wetland biomes should favor rich plants over generic plants");

    foreach (var biome in new[] { BiomeKind.Desert, BiomeKind.Grassland, BiomeKind.Forest, BiomeKind.Wetland })
    {
        var counts = SamplePlantTypeCounts(scenario, biome, seed: 117);
        foreach (var plantKind in Enum.GetValues<PlantResourceKind>())
        {
            AssertTrue(counts[(int)plantKind] > 0, $"{plantKind} plants should be able to spawn in {biome}");
        }
    }
}

static int[] SamplePlantTypeCounts(SimulationScenario scenario, BiomeKind biomeKind, ulong seed)
{
    const int sampleCount = 4_000;
    var random = new DeterministicRandom(seed);
    var counts = new int[Enum.GetValues<PlantResourceKind>().Length];
    for (var i = 0; i < sampleCount; i++)
    {
        counts[(int)scenario.SamplePlantResourceKind(random, biomeKind)]++;
    }

    return counts;
}

static void ScenarioFactoryHonorsInitialSpawnRegion()
{
    var scenario = new SimulationScenario
    {
        Seed = 141,
        WorldWidth = 900f,
        WorldHeight = 600f,
        ResourceVoidBorderWidth = 30f,
        InitialCreatureCount = 20,
        InitialCreatureSpawnRegion = InitialCreatureSpawnRegion.LeftThird,
        InitialResourcesPerMillionArea = 0f
    };

    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);

    AssertEqual(20, simulation.State.Creatures.Count, "Spawn-region creature count");
    foreach (var creature in simulation.State.Creatures)
    {
        AssertTrue(creature.Position.X >= 30f, $"Creature {creature.Id.Value} should avoid left resource void");
        AssertTrue(creature.Position.X < 300f, $"Creature {creature.Id.Value} should spawn in left third");
        AssertTrue(creature.Position.Y >= 30f, $"Creature {creature.Id.Value} should avoid top resource void");
        AssertTrue(creature.Position.Y <= 570f, $"Creature {creature.Id.Value} should avoid bottom resource void");
    }

    var quadrantScenario = scenario with
    {
        Seed = 142,
        InitialCreatureSpawnRegion = InitialCreatureSpawnRegion.LowerRightQuadrant
    };
    var quadrantSimulation = SimulationScenarioFactory.CreateSimulation(quadrantScenario);
    AssertEqual(20, quadrantSimulation.State.Creatures.Count, "Quadrant spawn-region creature count");
    foreach (var creature in quadrantSimulation.State.Creatures)
    {
        AssertTrue(creature.Position.X >= 450f, $"Creature {creature.Id.Value} should spawn in right half");
        AssertTrue(creature.Position.X <= 870f, $"Creature {creature.Id.Value} should avoid right resource void");
        AssertTrue(creature.Position.Y >= 300f, $"Creature {creature.Id.Value} should spawn in lower half");
        AssertTrue(creature.Position.Y <= 570f, $"Creature {creature.Id.Value} should avoid bottom resource void");
    }
}

static void ScenarioResourceDensityScalesWithWorldArea()
{
    var smallWorld = SimulationScenarioFactory.CreateSimulation(new SimulationScenario
    {
        Seed = 17,
        WorldWidth = 1_000f,
        WorldHeight = 1_000f,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 4f
    });
    var largeWorld = SimulationScenarioFactory.CreateSimulation(new SimulationScenario
    {
        Seed = 17,
        WorldWidth = 2_000f,
        WorldHeight = 2_000f,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 4f
    });

    AssertEqual(4, smallWorld.State.Resources.Count, "Small-world resource count");
    AssertEqual(16, largeWorld.State.Resources.Count, "Large-world resource count");
}

static void ScenarioResourceClusteringCreatesLocalFoodPatches()
{
    var scenario = new SimulationScenario
    {
        Seed = 71,
        EnableBiomes = false,
        WorldWidth = 500f,
        WorldHeight = 500f,
        ResourceVoidBorderWidth = 0f,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 64f,
        ResourceClusterStrength = 1f,
        ResourceClusterRadius = 35f
    };

    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);

    AssertEqual(16, simulation.State.Resources.Count, "Clustered resource count");
    for (var i = 1; i < simulation.State.Resources.Count; i++)
    {
        var nearestPrior = float.MaxValue;
        for (var j = 0; j < i; j++)
        {
            nearestPrior = MathF.Min(
                nearestPrior,
                SimVector2.Distance(simulation.State.Resources[i].Position, simulation.State.Resources[j].Position));
        }

        AssertTrue(
            nearestPrior <= scenario.ResourceClusterRadius + 0.001f,
            $"Resource {i} should spawn near an existing plant when clustering is forced");
    }
}

static void GeneratedSmallBiomeMapsContainVisibleVariety()
{
    var map = BiomeMap.Generate(new WorldBounds(1_000f, 700f), cellSize: 500f, seed: SimulationScenario.DefaultSeed);
    var kinds = new HashSet<BiomeKind>();

    for (var y = 0; y < map.CellCountY; y++)
    {
        for (var x = 0; x < map.CellCountX; x++)
        {
            kinds.Add(map.GetKind(x, y));
        }
    }

    AssertTrue(kinds.Count > 1, "Small generated biome maps should not collapse to one biome");
    AssertTrue(kinds.Contains(BiomeKind.Barren), "Small generated biome maps should include a low-fertility biome");
    AssertTrue(kinds.Contains(BiomeKind.Rich), "Small generated biome maps should include a high-fertility biome");
}

static void NaturalClimateBiomeMapsCreateBroadFiveBiomeRegions()
{
    var first = BiomeMap.GenerateNaturalClimate(
        new WorldBounds(4_000f, 4_000f),
        cellSize: 100f,
        seed: SimulationScenario.DefaultSeed);
    var second = BiomeMap.GenerateNaturalClimate(
        new WorldBounds(4_000f, 4_000f),
        cellSize: 100f,
        seed: SimulationScenario.DefaultSeed);
    var kinds = new HashSet<BiomeKind>();
    var sameNeighborEdges = 0;
    var totalNeighborEdges = 0;
    var isolatedCells = 0;

    for (var y = 0; y < first.CellCountY; y++)
    {
        for (var x = 0; x < first.CellCountX; x++)
        {
            var kind = first.GetKind(x, y);
            AssertEqual(kind, second.GetKind(x, y), $"Natural climate biome kind {x},{y}");
            kinds.Add(kind);
            var hasSameCardinalNeighbor = false;

            if (x + 1 < first.CellCountX)
            {
                totalNeighborEdges++;
                if (kind == first.GetKind(x + 1, y))
                {
                    sameNeighborEdges++;
                    hasSameCardinalNeighbor = true;
                }
            }

            if (y + 1 < first.CellCountY)
            {
                totalNeighborEdges++;
                if (kind == first.GetKind(x, y + 1))
                {
                    sameNeighborEdges++;
                    hasSameCardinalNeighbor = true;
                }
            }

            if (x > 0 && kind == first.GetKind(x - 1, y))
            {
                hasSameCardinalNeighbor = true;
            }

            if (y > 0 && kind == first.GetKind(x, y - 1))
            {
                hasSameCardinalNeighbor = true;
            }

            if (!hasSameCardinalNeighbor)
            {
                isolatedCells++;
            }
        }
    }

    var groupingShare = sameNeighborEdges / (float)Math.Max(1, totalNeighborEdges);
    var isolatedShare = isolatedCells / (float)Math.Max(1, first.CellCountX * first.CellCountY);
    AssertEqual(
        5,
        kinds.Count,
        $"Natural climate maps should use the five readable biomes, saw {kinds.Count}: {string.Join(", ", kinds.OrderBy(kind => kind))}");
    AssertTrue(kinds.Contains(BiomeKind.Desert), "Natural climate maps should include desert");
    AssertTrue(kinds.Contains(BiomeKind.Grassland), "Natural climate maps should include grassland");
    AssertTrue(kinds.Contains(BiomeKind.Fertile), "Natural climate maps should include fertile");
    AssertTrue(kinds.Contains(BiomeKind.Forest), "Natural climate maps should include forest");
    AssertTrue(kinds.Contains(BiomeKind.Wetland), "Natural climate maps should include wetland");
    AssertTrue(groupingShare > 0.78f, $"Natural climate maps should be regionally grouped, saw {groupingShare:0.00}");
    AssertTrue(isolatedShare < 0.04f, $"Natural climate maps should avoid isolated cell noise, saw {isolatedShare:0.00}");
}

static void NaturalClimateSingleCellMapsStayNeutral()
{
    var map = BiomeMap.GenerateNaturalClimate(
        new WorldBounds(4_000f, 4_000f),
        cellSize: 4_000f,
        seed: SimulationScenario.DefaultSeed);

    AssertEqual(1, map.CellCountX, "Single-cell natural climate count x");
    AssertEqual(1, map.CellCountY, "Single-cell natural climate count y");
    AssertEqual(BiomeKind.Grassland, map.GetKind(0, 0), "Single-cell natural climate biome");
}

static void BandedBiomeMapsCreateBroadRegions()
{
    var vertical = BiomeMap.GenerateBands(
        new WorldBounds(800f, 300f),
        cellSize: 100f,
        BiomeMapKind.VerticalBands);

    AssertEqual(8, vertical.CellCountX, "Vertical band count x");
    AssertEqual(3, vertical.CellCountY, "Vertical band count y");
    AssertEqual(BiomeKind.Barren, vertical.GetKind(0, 0), "Left edge biome");
    AssertEqual(BiomeKind.Rich, vertical.GetKind(3, 0), "Center-left biome");
    AssertEqual(BiomeKind.Rich, vertical.GetKind(4, 0), "Center-right biome");
    AssertEqual(BiomeKind.Barren, vertical.GetKind(7, 0), "Right edge biome");

    for (var y = 1; y < vertical.CellCountY; y++)
    {
        for (var x = 0; x < vertical.CellCountX; x++)
        {
            AssertEqual(vertical.GetKind(x, 0), vertical.GetKind(x, y), $"Vertical band {x},{y}");
        }
    }

    var horizontal = BiomeMap.GenerateBands(
        new WorldBounds(300f, 800f),
        cellSize: 100f,
        BiomeMapKind.HorizontalBands);

    AssertEqual(BiomeKind.Barren, horizontal.GetKind(0, 0), "Top edge biome");
    AssertEqual(BiomeKind.Rich, horizontal.GetKind(0, 3), "Center-top biome");
    AssertEqual(BiomeKind.Rich, horizontal.GetKind(0, 4), "Center-bottom biome");
    AssertEqual(BiomeKind.Barren, horizontal.GetKind(0, 7), "Bottom edge biome");
}

static void EdgeBandBiomeMapsCreateProductiveEnds()
{
    var vertical = BiomeMap.GenerateBands(
        new WorldBounds(800f, 300f),
        cellSize: 100f,
        BiomeMapKind.VerticalEdgeBands);

    AssertEqual(BiomeKind.Rich, vertical.GetKind(0, 0), "Left edge should be rich");
    AssertEqual(BiomeKind.Grassland, vertical.GetKind(1, 0), "Left inner band should be grassland");
    AssertEqual(BiomeKind.Sparse, vertical.GetKind(2, 0), "Left middle band should be sparse");
    AssertEqual(BiomeKind.Barren, vertical.GetKind(3, 0), "Center-left band should be barren");
    AssertEqual(BiomeKind.Barren, vertical.GetKind(4, 0), "Center-right band should be barren");
    AssertEqual(BiomeKind.Rich, vertical.GetKind(7, 0), "Right edge should be rich");

    for (var y = 1; y < vertical.CellCountY; y++)
    {
        for (var x = 0; x < vertical.CellCountX; x++)
        {
            AssertEqual(vertical.GetKind(x, 0), vertical.GetKind(x, y), $"Vertical edge band {x},{y}");
        }
    }

    var horizontal = BiomeMap.GenerateBands(
        new WorldBounds(300f, 800f),
        cellSize: 100f,
        BiomeMapKind.HorizontalEdgeBands);

    AssertEqual(BiomeKind.Rich, horizontal.GetKind(0, 0), "Top edge should be rich");
    AssertEqual(BiomeKind.Barren, horizontal.GetKind(0, 3), "Center-top should be barren");
    AssertEqual(BiomeKind.Barren, horizontal.GetKind(0, 4), "Center-bottom should be barren");
    AssertEqual(BiomeKind.Rich, horizontal.GetKind(0, 7), "Bottom edge should be rich");
}

static void EdgeLadderBiomeMapsKeepPoorCentersCrossable()
{
    var vertical = BiomeMap.GenerateBands(
        new WorldBounds(800f, 300f),
        cellSize: 100f,
        BiomeMapKind.VerticalEdgeLadderBands);

    AssertEqual(BiomeKind.Rich, vertical.GetKind(0, 0), "Left edge should be rich");
    AssertEqual(BiomeKind.Grassland, vertical.GetKind(1, 0), "Left inner band should be grassland");
    AssertEqual(BiomeKind.Sparse, vertical.GetKind(2, 0), "Left middle should be sparse");
    AssertEqual(BiomeKind.Sparse, vertical.GetKind(3, 0), "Center-left should be sparse");
    AssertEqual(BiomeKind.Sparse, vertical.GetKind(4, 0), "Center-right should be sparse");
    AssertEqual(BiomeKind.Grassland, vertical.GetKind(6, 0), "Right inner band should be grassland");
    AssertEqual(BiomeKind.Rich, vertical.GetKind(7, 0), "Right edge should be rich");

    for (var y = 1; y < vertical.CellCountY; y++)
    {
        for (var x = 0; x < vertical.CellCountX; x++)
        {
            AssertEqual(vertical.GetKind(x, 0), vertical.GetKind(x, y), $"Vertical ladder band {x},{y}");
        }
    }
}

static void EdgeCorridorBiomeMapsCreateHarshCrossings()
{
    var vertical = BiomeMap.GenerateBands(
        new WorldBounds(800f, 500f),
        cellSize: 100f,
        BiomeMapKind.VerticalEdgeCorridorBands);

    AssertEqual(BiomeKind.Rich, vertical.GetKind(0, 0), "Left edge should be rich");
    AssertEqual(BiomeKind.Grassland, vertical.GetKind(1, 0), "Left inner band should be grassland");
    AssertEqual(BiomeKind.Sparse, vertical.GetKind(2, 0), "Left approach band should be sparse");
    AssertEqual(BiomeKind.Barren, vertical.GetKind(3, 0), "Upper center should remain barren");
    AssertEqual(BiomeKind.Sparse, vertical.GetKind(3, 1), "Corridor shoulder should be sparse");
    AssertEqual(BiomeKind.Grassland, vertical.GetKind(3, 2), "Center corridor should be grassland");
    AssertEqual(BiomeKind.Sparse, vertical.GetKind(4, 3), "Lower corridor shoulder should be sparse");
    AssertEqual(BiomeKind.Barren, vertical.GetKind(4, 4), "Lower center should remain barren");
    AssertEqual(BiomeKind.Rich, vertical.GetKind(7, 0), "Right edge should be rich");

    var wide = BiomeMap.GenerateBands(
        new WorldBounds(800f, 800f),
        cellSize: 100f,
        BiomeMapKind.VerticalEdgeWideCorridorBands);

    AssertEqual(BiomeKind.Rich, wide.GetKind(0, 0), "Wide corridor left edge should stay rich");
    AssertEqual(BiomeKind.Barren, wide.GetKind(3, 0), "Wide corridor upper center should remain barren");
    AssertEqual(BiomeKind.Sparse, wide.GetKind(3, 1), "Wide corridor upper shoulder should be sparse");
    AssertEqual(BiomeKind.Grassland, wide.GetKind(3, 2), "Wide corridor should widen into grassland sooner");
    AssertEqual(BiomeKind.Grassland, wide.GetKind(4, 5), "Wide corridor should keep lower center grassland");
    AssertEqual(BiomeKind.Sparse, wide.GetKind(4, 6), "Wide corridor lower shoulder should be sparse");
    AssertEqual(BiomeKind.Barren, wide.GetKind(4, 7), "Wide corridor lower edge should remain barren");
    AssertEqual(BiomeKind.Rich, wide.GetKind(7, 0), "Wide corridor right edge should stay rich");
}

static void ObstacleMapsCreateBarriersAndScatteredRocks()
{
    var vertical = ObstacleMap.Generate(
        new WorldBounds(1_000f, 1_000f),
        cellSize: 100f,
        ObstacleMapKind.VerticalBarrierWithGaps,
        seed: 61);

    AssertEqual(10, vertical.CellCountX, "Vertical obstacle cell count x");
    AssertEqual(10, vertical.CellCountY, "Vertical obstacle cell count y");
    AssertTrue(vertical.IsBlocked(5, 0), "Vertical barrier should block its center column");
    AssertTrue(!vertical.IsBlocked(5, 5), "Vertical barrier should leave a central gap");
    AssertTrue(!vertical.IsBlocked(4, 0), "Vertical barrier should not block neighboring columns");
    AssertTrue(vertical.IsBlockedAt(new SimVector2(550f, 50f)), "Point inside blocked obstacle cell");
    AssertTrue(!vertical.IsBlockedAt(new SimVector2(550f, 550f)), "Point inside obstacle gap");

    var firstScatter = ObstacleMap.Generate(
        new WorldBounds(2_000f, 2_000f),
        cellSize: 100f,
        ObstacleMapKind.ScatteredRocks,
        seed: 62);
    var secondScatter = ObstacleMap.Generate(
        new WorldBounds(2_000f, 2_000f),
        cellSize: 100f,
        ObstacleMapKind.ScatteredRocks,
        seed: 62);

    AssertTrue(firstScatter.BlockedCellCount > 0, "Scattered rocks should produce some blocked cells on a large map");
    AssertEqual(firstScatter.BlockedCellCount, secondScatter.BlockedCellCount, "Scattered rock count should be deterministic");
    for (var y = 0; y < firstScatter.CellCountY; y++)
    {
        for (var x = 0; x < firstScatter.CellCountX; x++)
        {
            AssertEqual(firstScatter.IsBlocked(x, y), secondScatter.IsBlocked(x, y), $"Scattered rock cell {x},{y}");
        }
    }
}

static void BiomeMapSamplesResourcesByDensity()
{
    var map = BiomeMap.CreateFromCells(
        new WorldBounds(200f, 100f),
        cellSize: 100f,
        cellCountX: 2,
        cellCountY: 1,
        [BiomeKind.Barren, BiomeKind.Rich]);
    var random = new DeterministicRandom(2026);
    var barrenSamples = 0;
    var richSamples = 0;

    for (var i = 0; i < 2_000; i++)
    {
        var position = map.SampleResourcePosition(random);
        if (map.GetKindAt(position) == BiomeKind.Rich)
        {
            richSamples++;
        }
        else
        {
            barrenSamples++;
        }
    }

    AssertTrue(richSamples > barrenSamples * 20, "Rich biome should receive far more resource samples than barren biome");
}

static void ResourceVoidBorderExcludesPlantGrowth()
{
    var map = BiomeMap.CreateUniform(
        new WorldBounds(1_000f, 800f),
        cellSize: 200f,
        BiomeKind.Grassland,
        resourceVoidBorderWidth: 100f);
    var random = new DeterministicRandom(2027);

    AssertTrue(map.IsInResourceVoid(new SimVector2(50f, 400f)), "Left border should be resource void");
    AssertTrue(map.IsInResourceVoid(new SimVector2(950f, 400f)), "Right border should be resource void");
    AssertTrue(!map.IsInResourceVoid(new SimVector2(500f, 400f)), "Center should allow resources");
    AssertClose(0f, map.GetResourceDensityMultiplierAt(new SimVector2(50f, 400f)), 0.000001, "Void density multiplier");
    AssertClose(0f, map.GetResourceRegrowthMultiplierAt(new SimVector2(50f, 400f)), 0.000001, "Void regrowth multiplier");
    AssertTrue(map.GetResourceDensityMultiplierAt(new SimVector2(500f, 400f)) > 0f, "Inner density multiplier");

    for (var i = 0; i < 500; i++)
    {
        var position = map.SampleResourcePosition(random);
        AssertTrue(!map.IsInResourceVoid(position), $"Sample {i} should be inside the resource area");
    }
}

static void ResourceVoidClippedBiomeCellsAreSkippedDuringSampling()
{
    var scenario = new SimulationScenario
    {
        Seed = 42,
        BiomeMapKind = BiomeMapKind.NaturalClimate,
        WorldWidth = 4_000f,
        WorldHeight = 4_000f,
        BiomeCellSize = 100f,
        ResourceVoidBorderWidth = 160f,
        InitialCreatureCount = 80,
        InitialResourcesPerMillionArea = 19.5f
    };

    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    AssertTrue(simulation.State.Resources.Count > 0, "Seed 42 scenario should create starting plants");

    for (var i = 0; i < simulation.State.Resources.Count; i++)
    {
        AssertTrue(
            !simulation.State.Biomes.IsInResourceVoid(simulation.State.Resources[i].Position),
            $"Seed 42 starting plant {i} should spawn outside the resource void");
    }

    var map = BiomeMap.GenerateNaturalClimate(
        new WorldBounds(4_000f, 4_000f),
        cellSize: 100f,
        seed: 42,
        resourceVoidBorderWidth: 160f);
    var random = new DeterministicRandom(42);

    for (var i = 0; i < 2_000; i++)
    {
        var position = map.SampleResourcePosition(random);
        AssertTrue(!map.IsInResourceVoid(position), $"Sample {i} should be inside the resource area");
    }
}

static void CreatureOnlySpatialRebuildPreservesStaticEntities()
{
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f
        },
        seed: 2);
    var index = new UniformSpatialIndex(10f);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    simulation.State.SpawnCreature(genomeId, new SimVector2(10f, 10f), energy: 10f);
    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Position = new SimVector2(50f, 50f),
        Radius = 2f,
        Calories = 8f,
        MaxCalories = 10f
    });
    var candidates = new List<int>();
    var seen = new HashSet<int>();

    index.Rebuild(simulation.State);
    var creature = simulation.State.Creatures[0];
    creature.Position = new SimVector2(80f, 80f);
    simulation.State.Creatures[0] = creature;
    index.RebuildCreatures(simulation.State);

    index.AddCreatureCandidates(simulation.State, new SimVector2(10f, 10f), 5f, candidates, seen);
    AssertEqual(0, candidates.Count, "Old creature cell should be empty after creature-only rebuild");

    index.AddCreatureCandidates(simulation.State, new SimVector2(80f, 80f), 5f, candidates, seen);
    AssertEqual(1, candidates.Count, "New creature cell should contain moved creature");
    AssertEqual(0, candidates[0], "Moved creature index");

    var resourceIndex = index.FindNearestResourceWithCalories(
        simulation.State,
        new SimVector2(50f, 50f),
        radius: 5f);
    AssertEqual(0, resourceIndex, "Resource index should survive creature-only rebuild");
}

static void PersistentSpatialRebuildRemovesDecayedResources()
{
    var index = new UniformSpatialIndex(10f);
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 1f
        },
        seed: 23,
        systems:
        [
            new ResourceRegrowthSystem(),
            new SpatialIndexRebuildSystem(index)
        ]);

    simulation.State.SpawnResourcePatch(new ResourcePatchState
    {
        Kind = ResourceKind.Meat,
        Position = new SimVector2(50f, 50f),
        Radius = 2f,
        Calories = 1f,
        MaxCalories = 1f,
        DecayCaloriesPerSecond = 2f
    });

    index.Rebuild(simulation.State);
    AssertEqual(0, index.FindNearestResourceWithCalories(simulation.State, new SimVector2(50f, 50f), 5f), "Live meat precondition");

    simulation.Step();

    AssertEqual(0, simulation.State.Resources.Count, "Decayed meat should leave world state");
    AssertEqual(-1, index.FindNearestResourceWithCalories(simulation.State, new SimVector2(50f, 50f), 5f), "Decayed meat should leave spatial index");
}

static void PersistentSpatialRebuildRemovesHatchedEggs()
{
    var index = new UniformSpatialIndex(10f);
    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 100f,
            WorldHeight = 100f,
            FixedDeltaSeconds = 1f
        },
        seed: 404,
        systems:
        [
            new EggSystem(),
            new SpatialIndexRebuildSystem(index)
        ]);

    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    var parentId = simulation.State.SpawnCreature(genomeId, new SimVector2(20f, 20f), energy: 25f);
    simulation.State.SpawnEgg(
        genomeId,
        brainId: -1,
        parentId,
        new SimVector2(50f, 50f),
        energy: 12f,
        incubationSeconds: 1f,
        generation: 1);

    index.Rebuild(simulation.State);
    AssertEqual(0, index.FindNearestEggWithEnergy(simulation.State, new SimVector2(50f, 50f), 5f), "Live egg precondition");

    simulation.Step();

    AssertEqual(0, simulation.State.Eggs.Count, "Hatched egg should leave world state");
    AssertEqual(2, simulation.State.Creatures.Count, "Hatched egg should create offspring");
    AssertEqual(-1, index.FindNearestEggWithEnergy(simulation.State, new SimVector2(50f, 50f), 5f), "Hatched egg should leave spatial index");
}

static void ScenarioFactoryCreatesDeterministicBiomes()
{
    var scenario = new SimulationScenario
    {
        Seed = 18,
        BiomeMapKind = BiomeMapKind.GeneratedNoise,
        WorldWidth = 1_500f,
        WorldHeight = 1_000f,
        BiomeCellSize = 250f,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 200f
    };

    var first = SimulationScenarioFactory.CreateSimulation(scenario);
    var second = SimulationScenarioFactory.CreateSimulation(scenario);

    AssertEqual(6, first.State.Biomes.CellCountX, "Biome cell count x");
    AssertEqual(4, first.State.Biomes.CellCountY, "Biome cell count y");
    AssertEqual(first.State.Resources.Count, second.State.Resources.Count, "Resource count");
    var kinds = new HashSet<BiomeKind>();

    for (var y = 0; y < first.State.Biomes.CellCountY; y++)
    {
        for (var x = 0; x < first.State.Biomes.CellCountX; x++)
        {
            AssertEqual(first.State.Biomes.GetKind(x, y), second.State.Biomes.GetKind(x, y), $"Biome kind {x},{y}");
            kinds.Add(first.State.Biomes.GetKind(x, y));
        }
    }

    AssertTrue(kinds.Contains(BiomeKind.Barren), "Generated biomes should include barren cells");
    AssertTrue(kinds.Contains(BiomeKind.Sparse), "Generated biomes should include sparse cells");
    AssertTrue(kinds.Contains(BiomeKind.Grassland), "Generated biomes should include grassland cells");
    AssertTrue(kinds.Contains(BiomeKind.Rich), "Generated biomes should include rich cells");

    for (var i = 0; i < first.State.Resources.Count; i++)
    {
        AssertClose(first.State.Resources[i].Position.X, second.State.Resources[i].Position.X, 0.000001, $"Resource {i} x");
        AssertClose(first.State.Resources[i].Position.Y, second.State.Resources[i].Position.Y, 0.000001, $"Resource {i} y");
        AssertClose(first.State.Resources[i].RegrowthCaloriesPerSecond, second.State.Resources[i].RegrowthCaloriesPerSecond, 0.000001, $"Resource {i} regrowth");
    }
}

static void ScenarioFactoryHonorsBiomeMapKind()
{
    var scenario = new SimulationScenario
    {
        Seed = 19,
        BiomeMapKind = BiomeMapKind.VerticalBands,
        WorldWidth = 800f,
        WorldHeight = 400f,
        BiomeCellSize = 100f,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f
    };

    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);

    AssertEqual(BiomeKind.Barren, simulation.State.Biomes.GetKind(0, 0), "Vertical band left edge");
    AssertEqual(BiomeKind.Rich, simulation.State.Biomes.GetKind(3, 0), "Vertical band rich center");
    AssertEqual(BiomeKind.Barren, simulation.State.Biomes.GetKind(7, 0), "Vertical band right edge");
}

static void ManualBiomeMapJsonRoundTrips()
{
    var map = BiomeMap.CreateFromCells(
        new WorldBounds(300f, 200f),
        100f,
        3,
        2,
        [
            BiomeKind.Grassland,
            BiomeKind.Forest,
            BiomeKind.Wetland,
            BiomeKind.Desert,
            BiomeKind.Fertile,
            BiomeKind.Scrubland
        ],
        10f);
    var document = ManualBiomeMapDocument.FromBiomeMap(
        map,
        "Painted seed 42",
        BiomeMapKind.NaturalClimate,
        42UL);

    var json = ManualBiomeMapJson.ToJson(document);
    var roundTripped = ManualBiomeMapJson.FromJson(json);
    var loadedMap = roundTripped.ToBiomeMap();

    AssertTrue(json.Contains("\"schemaVersion\": \"lineage.manualBiomeMap.v1\""), "Manual biome map JSON should serialize schema version");
    AssertTrue(json.Contains("\"sourceMapKind\": \"naturalClimate\""), "Manual biome map JSON should serialize source map kind");
    AssertTrue(json.Contains("\"forest\""), "Manual biome map JSON should serialize biome cells by name");
    AssertEqual("Painted seed 42", roundTripped.Name, "Manual biome map name");
    AssertEqual(BiomeMapKind.NaturalClimate, roundTripped.SourceMapKind, "Manual biome map source kind");
    AssertEqual(42UL, roundTripped.SourceSeed ?? 0UL, "Manual biome map source seed");
    AssertClose(map.Bounds.Width, loadedMap.Bounds.Width, 0.000001, "Manual biome map world width");
    AssertClose(map.Bounds.Height, loadedMap.Bounds.Height, 0.000001, "Manual biome map world height");
    AssertClose(map.CellSize, loadedMap.CellSize, 0.000001, "Manual biome map cell size");
    AssertEqual(map.CellCountX, loadedMap.CellCountX, "Manual biome map cell count x");
    AssertEqual(map.CellCountY, loadedMap.CellCountY, "Manual biome map cell count y");
    AssertEqual(BiomeKind.Forest, loadedMap.GetKind(1, 0), "Manual biome map forest cell");
    AssertEqual(BiomeKind.Scrubland, loadedMap.GetKind(2, 1), "Manual biome map scrubland cell");
}

static void ScenarioFactoryHonorsManualBiomeMapPath()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), $"lineage_manual_biome_{Guid.NewGuid():N}");
    try
    {
        var mapPath = Path.Combine(tempRoot, "maps", "painted.json");
        ManualBiomeMapJson.Save(
            mapPath,
            new ManualBiomeMapDocument
            {
                Name = "Painted test map",
                SourceMapKind = BiomeMapKind.NaturalClimate,
                SourceSeed = 123UL,
                WorldWidth = 300f,
                WorldHeight = 200f,
                CellSize = 100f,
                CellCountX = 3,
                CellCountY = 2,
                ResourceVoidBorderWidth = 10f,
                Cells =
                [
                    BiomeKind.Desert,
                    BiomeKind.Grassland,
                    BiomeKind.Forest,
                    BiomeKind.Wetland,
                    BiomeKind.Fertile,
                    BiomeKind.Scrubland
                ]
            });

        var scenario = new SimulationScenario
        {
            Seed = 77UL,
            BiomeMapKind = BiomeMapKind.Manual,
            ManualBiomeMapPath = Path.Combine("maps", "painted.json"),
            WorldWidth = 300f,
            WorldHeight = 200f,
            BiomeCellSize = 100f,
            ResourceVoidBorderWidth = 10f,
            InitialCreatureCount = 0,
            InitialResourcesPerMillionArea = 0f
        };

        var simulation = SimulationScenarioFactory.CreateSimulation(scenario, tempRoot);

        AssertEqual(BiomeKind.Desert, simulation.State.Biomes.GetKind(0, 0), "Manual map first cell");
        AssertEqual(BiomeKind.Forest, simulation.State.Biomes.GetKind(2, 0), "Manual map forest cell");
        AssertEqual(BiomeKind.Fertile, simulation.State.Biomes.GetKind(1, 1), "Manual map fertile cell");
        AssertThrows<InvalidOperationException>(
            () => SimulationScenarioFactory.CreateSimulation(scenario with { WorldWidth = 400f }, tempRoot),
            "Manual biome map dimensions must match the scenario");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void ManualObstacleMapJsonRoundTrips()
{
    var map = ObstacleMap.CreateFromCells(
        new WorldBounds(300f, 200f),
        100f,
        3,
        2,
        [false, true, false, true, false, true]);
    var document = ManualObstacleMapDocument.FromObstacleMap(
        map,
        "Painted walls",
        ObstacleMapKind.VerticalBarrierWithGaps,
        42UL);

    var json = ManualObstacleMapJson.ToJson(document);
    var roundTripped = ManualObstacleMapJson.FromJson(json);
    var loadedMap = roundTripped.ToObstacleMap();

    AssertTrue(json.Contains("\"schemaVersion\": \"lineage.manualObstacleMap.v1\""), "Manual obstacle map JSON should serialize schema version");
    AssertTrue(json.Contains("\"sourceMapKind\": \"verticalBarrierWithGaps\""), "Manual obstacle map JSON should serialize source map kind");
    AssertTrue(json.Contains("\"blockedCells\""), "Manual obstacle map JSON should serialize obstacle cells");
    AssertEqual("Painted walls", roundTripped.Name, "Manual obstacle map name");
    AssertEqual(ObstacleMapKind.VerticalBarrierWithGaps, roundTripped.SourceMapKind, "Manual obstacle map source kind");
    AssertEqual(42UL, roundTripped.SourceSeed ?? 0UL, "Manual obstacle map source seed");
    AssertClose(map.Bounds.Width, loadedMap.Bounds.Width, 0.000001, "Manual obstacle map world width");
    AssertClose(map.Bounds.Height, loadedMap.Bounds.Height, 0.000001, "Manual obstacle map world height");
    AssertClose(map.CellSize, loadedMap.CellSize, 0.000001, "Manual obstacle map cell size");
    AssertEqual(map.CellCountX, loadedMap.CellCountX, "Manual obstacle map cell count x");
    AssertEqual(map.CellCountY, loadedMap.CellCountY, "Manual obstacle map cell count y");
    AssertTrue(loadedMap.IsBlocked(1, 0), "Manual obstacle map blocked cell");
    AssertTrue(!loadedMap.IsBlocked(2, 0), "Manual obstacle map open cell");
}

static void WorldMapArtifactJsonRoundTrips()
{
    var biomeMap = BiomeMap.CreateFromCells(
        new WorldBounds(300f, 200f),
        100f,
        3,
        2,
        [
            BiomeKind.Desert,
            BiomeKind.Grassland,
            BiomeKind.Forest,
            BiomeKind.Wetland,
            BiomeKind.Fertile,
            BiomeKind.Scrubland
        ],
        12f);
    var obstacleMap = ObstacleMap.CreateFromCells(
        new WorldBounds(300f, 200f),
        50f,
        6,
        4,
        [
            false, true, false, false, false, false,
            false, false, false, true, false, false,
            false, false, false, false, true, false,
            true, false, false, false, false, false
        ]);
    var document = WorldMapArtifactDocument.FromMaps(
        biomeMap,
        obstacleMap,
        "Reusable valley",
        BiomeMapKind.NaturalClimate,
        ObstacleMapKind.VerticalBarrierWithGaps,
        42UL);

    var json = WorldMapArtifactJson.ToJson(document);
    var roundTripped = WorldMapArtifactJson.FromJson(json);
    var loadedBiomeMap = roundTripped.ToBiomeMap();
    var loadedObstacleMap = roundTripped.ToObstacleMap();

    AssertTrue(json.Contains("\"schemaVersion\": \"lineage.worldMap.v1\""), "World map artifact JSON should serialize schema version");
    AssertTrue(json.Contains("\"sourceBiomeMapKind\": \"naturalClimate\""), "World map artifact JSON should serialize source biome map kind");
    AssertTrue(json.Contains("\"sourceObstacleMapKind\": \"verticalBarrierWithGaps\""), "World map artifact JSON should serialize source obstacle map kind");
    AssertTrue(json.Contains("\"biomeCells\""), "World map artifact JSON should serialize biome cells");
    AssertTrue(json.Contains("\"obstacleBlockedCells\""), "World map artifact JSON should serialize obstacle cells");
    AssertEqual("Reusable valley", roundTripped.Name, "World map artifact name");
    AssertEqual(42UL, roundTripped.SourceSeed ?? 0UL, "World map artifact source seed");
    AssertClose(biomeMap.Bounds.Width, loadedBiomeMap.Bounds.Width, 0.000001, "World map biome width");
    AssertClose(biomeMap.CellSize, loadedBiomeMap.CellSize, 0.000001, "World map biome cell size");
    AssertEqual(BiomeKind.Forest, loadedBiomeMap.GetKind(2, 0), "World map forest cell");
    AssertEqual(BiomeKind.Fertile, loadedBiomeMap.GetKind(1, 1), "World map fertile cell");
    AssertClose(obstacleMap.CellSize, loadedObstacleMap.CellSize, 0.000001, "World map obstacle cell size");
    AssertTrue(loadedObstacleMap.IsBlocked(1, 0), "World map obstacle blocked cell");
    AssertTrue(!loadedObstacleMap.IsBlocked(2, 0), "World map obstacle open cell");
}

static void ScenarioFactoryHonorsManualObstacleMapPath()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), $"lineage_manual_obstacle_{Guid.NewGuid():N}");
    try
    {
        var mapPath = Path.Combine(tempRoot, "maps", "walls.json");
        ManualObstacleMapJson.Save(
            mapPath,
            new ManualObstacleMapDocument
            {
                Name = "Painted wall map",
                SourceMapKind = ObstacleMapKind.VerticalBarrierWithGaps,
                SourceSeed = 123UL,
                WorldWidth = 300f,
                WorldHeight = 200f,
                CellSize = 100f,
                CellCountX = 3,
                CellCountY = 2,
                BlockedCells = [false, true, false, false, false, true]
            });

        var scenario = new SimulationScenario
        {
            Seed = 77UL,
            EnableObstacles = true,
            ObstacleMapKind = ObstacleMapKind.Manual,
            ManualObstacleMapPath = Path.Combine("maps", "walls.json"),
            WorldWidth = 300f,
            WorldHeight = 200f,
            ObstacleCellSize = 100f,
            ResourceVoidBorderWidth = 10f,
            InitialCreatureCount = 0,
            InitialResourcesPerMillionArea = 0f
        };

        var simulation = SimulationScenarioFactory.CreateSimulation(scenario, tempRoot);

        AssertTrue(simulation.State.Obstacles.IsBlocked(1, 0), "Manual obstacle map blocked first row");
        AssertTrue(!simulation.State.Obstacles.IsBlocked(0, 1), "Manual obstacle map open second row");
        AssertTrue(simulation.State.Obstacles.IsBlocked(2, 1), "Manual obstacle map blocked second row");
        AssertThrows<InvalidOperationException>(
            () => SimulationScenarioFactory.CreateSimulation(scenario with { WorldWidth = 400f }, tempRoot),
            "Manual obstacle map dimensions must match the scenario");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void ScenarioFactoryHonorsWorldMapPath()
{
    var tempRoot = Path.Combine(Path.GetTempPath(), $"lineage_world_map_{Guid.NewGuid():N}");
    try
    {
        var mapPath = Path.Combine(tempRoot, "maps", "painted.lineage-map.json");
        var biomeMap = BiomeMap.CreateFromCells(
            new WorldBounds(300f, 200f),
            100f,
            3,
            2,
            [
                BiomeKind.Desert,
                BiomeKind.Grassland,
                BiomeKind.Forest,
                BiomeKind.Wetland,
                BiomeKind.Fertile,
                BiomeKind.Scrubland
            ],
            10f);
        var obstacleMap = ObstacleMap.CreateFromCells(
            new WorldBounds(300f, 200f),
            100f,
            3,
            2,
            [false, true, false, false, false, true]);
        WorldMapArtifactJson.Save(
            mapPath,
            WorldMapArtifactDocument.FromMaps(
                biomeMap,
                obstacleMap,
                "Painted world",
                BiomeMapKind.NaturalClimate,
                ObstacleMapKind.VerticalBarrierWithGaps,
                123UL));

        var scenario = new SimulationScenario
        {
            Seed = 77UL,
            BiomeMapKind = BiomeMapKind.Manual,
            WorldMapPath = Path.Combine("maps", "painted.lineage-map.json"),
            EnableObstacles = true,
            ObstacleMapKind = ObstacleMapKind.Manual,
            WorldWidth = 300f,
            WorldHeight = 200f,
            BiomeCellSize = 100f,
            ObstacleCellSize = 100f,
            ResourceVoidBorderWidth = 10f,
            InitialCreatureCount = 0,
            InitialResourcesPerMillionArea = 0f
        };

        var simulation = SimulationScenarioFactory.CreateSimulation(scenario, tempRoot);

        AssertEqual(BiomeKind.Desert, simulation.State.Biomes.GetKind(0, 0), "World map first biome cell");
        AssertEqual(BiomeKind.Forest, simulation.State.Biomes.GetKind(2, 0), "World map forest cell");
        AssertEqual(BiomeKind.Fertile, simulation.State.Biomes.GetKind(1, 1), "World map fertile cell");
        AssertTrue(simulation.State.Obstacles.IsBlocked(1, 0), "World map blocked first row");
        AssertTrue(!simulation.State.Obstacles.IsBlocked(0, 1), "World map open second row");
        AssertTrue(simulation.State.Obstacles.IsBlocked(2, 1), "World map blocked second row");
        AssertThrows<InvalidOperationException>(
            () => SimulationScenarioFactory.CreateSimulation(scenario with { ObstacleCellSize = 50f }, tempRoot),
            "World map obstacle dimensions must match the scenario");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void ScenarioFactoryHonorsNaturalClimateBiomeMapKind()
{
    var scenario = new SimulationScenario
    {
        Seed = 21,
        BiomeMapKind = BiomeMapKind.NaturalClimate,
        WorldWidth = 4_000f,
        WorldHeight = 3_000f,
        BiomeCellSize = 100f,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f
    };

    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var expected = BiomeMap.GenerateNaturalClimate(
        new WorldBounds(scenario.WorldWidth, scenario.WorldHeight),
        scenario.BiomeCellSize,
        scenario.Seed,
        scenario.ResourceVoidBorderWidth);

    for (var y = 0; y < simulation.State.Biomes.CellCountY; y++)
    {
        for (var x = 0; x < simulation.State.Biomes.CellCountX; x++)
        {
            AssertEqual(expected.GetKind(x, y), simulation.State.Biomes.GetKind(x, y), $"Natural climate factory biome {x},{y}");
        }
    }
}

static void ScenarioFactoryHonorsObstacleMapKind()
{
    var scenario = new SimulationScenario
    {
        Seed = 29,
        EnableObstacles = true,
        ObstacleMapKind = ObstacleMapKind.VerticalBarrierWithGaps,
        ObstacleCellSize = 100f,
        WorldWidth = 1_000f,
        WorldHeight = 1_000f,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f
    };

    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var disabled = SimulationScenarioFactory.CreateSimulation(scenario with { EnableObstacles = false });

    AssertEqual(10, simulation.State.Obstacles.CellCountX, "Obstacle cell count x");
    AssertTrue(simulation.State.Obstacles.IsBlocked(5, 0), "Enabled scenario should create obstacle barrier");
    AssertTrue(!simulation.State.Obstacles.IsBlocked(5, 5), "Enabled scenario should create obstacle gap");
    AssertEqual(0, disabled.State.Obstacles.BlockedCellCount, "Disabled scenario should create empty obstacle map");
}

static void ScenarioFactorySupportsInitialBrainKinds()
{
    var scenario = new SimulationScenario
    {
        Seed = 16,
        InitialCreatureCount = 3,
        InitialResourcesPerMillionArea = 0f,
        InitialBrainKind = InitialBrainKind.RandomPerFounder
    };

    var first = SimulationScenarioFactory.CreateSimulation(scenario);
    var second = SimulationScenarioFactory.CreateSimulation(scenario);
    var seededBrain = NeuralBrainGenome.CreateSeedForager(scenario.BrainHiddenNodeCount);
    var randomBrain = first.State.Brains[0];

    AssertEqual(3, first.State.Brains.Count, "Randomized founder brain count");
    AssertEqual(3, first.State.Creatures.Select(creature => creature.BrainId).Distinct().Count(), "Randomized founder brain IDs");
    AssertEqual(scenario.BrainHiddenNodeCount, randomBrain.HiddenNodeCount, "Randomized founder hidden nodes");
    AssertTrue(
        first.State.Brains.Select((_, brainId) => first.State.GetBrainArchitectureKind(brainId))
            .All(kind => kind == BrainArchitectureKind.HybridNeural),
        "Randomized founder brains should record architecture metadata");

    AssertTrue(
        randomBrain.Weights.Zip(seededBrain.Weights).Any(pair => Math.Abs(pair.First - pair.Second) > 0.000001f),
        "Randomized initial brain should differ from the seed forager brain");

    AssertTrue(
        first.State.Brains[0].Weights.Zip(first.State.Brains[1].Weights).Any(pair => Math.Abs(pair.First - pair.Second) > 0.000001f),
        "Each randomized founder should receive independent brain weights");

    for (var brainIndex = 0; brainIndex < first.State.Brains.Count; brainIndex++)
    {
        for (var i = 0; i < first.State.Brains[brainIndex].Weights.Length; i++)
        {
            AssertClose(first.State.Brains[brainIndex].Weights[i], second.State.Brains[brainIndex].Weights[i], 0.000001, $"Random brain {brainIndex} weight {i}");
        }
    }

    var seededSimulation = SimulationScenarioFactory.CreateSimulation(scenario with
    {
        InitialBrainKind = InitialBrainKind.SeedForager
    });

    AssertEqual(1, seededSimulation.State.Brains.Count, "Seeded founder brain count");
    AssertEqual(1, seededSimulation.State.Creatures.Select(creature => creature.BrainId).Distinct().Count(), "Seeded founder brain IDs");
    AssertEqual(
        BrainArchitectureKind.HybridNeural,
        seededSimulation.State.GetBrainArchitectureKind(0),
        "Seeded founder brain architecture");

    for (var i = 0; i < seededBrain.Weights.Length; i++)
    {
        AssertClose(seededBrain.Weights[i], seededSimulation.State.Brains[0].Weights[i], 0.000001, $"Seed brain weight {i}");
    }

    var explorerSimulation = SimulationScenarioFactory.CreateSimulation(scenario with
    {
        InitialBrainKind = InitialBrainKind.ExplorerForager
    });
    var explorerBrain = NeuralBrainGenome.CreateExplorerForager(scenario.BrainHiddenNodeCount);

    AssertEqual(1, explorerSimulation.State.Brains.Count, "Explorer founder brain count");
    for (var i = 0; i < explorerBrain.Weights.Length; i++)
    {
        AssertClose(explorerBrain.Weights[i], explorerSimulation.State.Brains[0].Weights[i], 0.000001, $"Explorer brain weight {i}");
    }

    var sectorSimulation = SimulationScenarioFactory.CreateSimulation(scenario with
    {
        InitialBrainKind = InitialBrainKind.SectorForager
    });
    var sectorBrain = NeuralBrainGenome.CreateSectorForager(scenario.BrainHiddenNodeCount);

    AssertEqual(1, sectorSimulation.State.Brains.Count, "Sector founder brain count");
    for (var i = 0; i < sectorBrain.Weights.Length; i++)
    {
        AssertClose(sectorBrain.Weights[i], sectorSimulation.State.Brains[0].Weights[i], 0.000001, $"Sector brain weight {i}");
    }

    var opportunisticSimulation = SimulationScenarioFactory.CreateSimulation(scenario with
    {
        InitialBrainKind = InitialBrainKind.OpportunisticForager
    });
    var opportunisticBrain = NeuralBrainGenome.CreateOpportunisticForager(scenario.BrainHiddenNodeCount);

    AssertEqual(1, opportunisticSimulation.State.Brains.Count, "Opportunistic founder brain count");
    for (var i = 0; i < opportunisticBrain.Weights.Length; i++)
    {
        AssertClose(opportunisticBrain.Weights[i], opportunisticSimulation.State.Brains[0].Weights[i], 0.000001, $"Opportunistic brain weight {i}");
    }

    var scavengerSimulation = SimulationScenarioFactory.CreateSimulation(scenario with
    {
        InitialBrainKind = InitialBrainKind.ScavengerForager
    });
    var scavengerBrain = NeuralBrainGenome.CreateScavengerForager(scenario.BrainHiddenNodeCount);

    AssertEqual(1, scavengerSimulation.State.Brains.Count, "Scavenger founder brain count");
    for (var i = 0; i < scavengerBrain.Weights.Length; i++)
    {
        AssertClose(scavengerBrain.Weights[i], scavengerSimulation.State.Brains[0].Weights[i], 0.000001, $"Scavenger brain weight {i}");
    }

    var freshnessAwareSimulation = SimulationScenarioFactory.CreateSimulation(scenario with
    {
        InitialBrainKind = InitialBrainKind.FreshnessAwareScavenger
    });
    var freshnessAwareBrain = NeuralBrainGenome.CreateFreshnessAwareScavenger(scenario.BrainHiddenNodeCount);

    AssertEqual(1, freshnessAwareSimulation.State.Brains.Count, "Freshness-aware scavenger founder brain count");
    for (var i = 0; i < freshnessAwareBrain.Weights.Length; i++)
    {
        AssertClose(freshnessAwareBrain.Weights[i], freshnessAwareSimulation.State.Brains[0].Weights[i], 0.000001, $"Freshness-aware brain weight {i}");
    }

    var predatorSimulation = SimulationScenarioFactory.CreateSimulation(scenario with
    {
        InitialBrainKind = InitialBrainKind.ForagerPredator
    });
    var predatorBrain = NeuralBrainGenome.CreateForagerPredator(scenario.BrainHiddenNodeCount);

    AssertEqual(1, predatorSimulation.State.Brains.Count, "Predator founder brain count");
    for (var i = 0; i < predatorBrain.Weights.Length; i++)
    {
        AssertClose(predatorBrain.Weights[i], predatorSimulation.State.Brains[0].Weights[i], 0.000001, $"Predator brain weight {i}");
    }

    var hiddenLayerScenario = scenario with
    {
        InitialBrainKind = InitialBrainKind.SectorForager,
        BrainArchitectureKind = BrainArchitectureKind.HiddenLayerNeural
    };
    var hiddenLayerSimulation = SimulationScenarioFactory.CreateSimulation(hiddenLayerScenario);
    AssertEqual(1, hiddenLayerSimulation.State.Brains.Count, "Hidden-layer founder brain count");
    AssertEqual(
        BrainArchitectureKind.HiddenLayerNeural,
        hiddenLayerSimulation.State.GetBrainArchitectureKind(0),
        "Hidden-layer founder brain architecture");
    AssertEqual(
        NeuralBrainSchema.DefaultHiddenLayerNodeCount,
        hiddenLayerSimulation.State.Brains[0].HiddenNodeCount,
        "Hidden-layer founder default node count");
    AssertDirectWeightsZero(hiddenLayerSimulation.State.Brains[0], "Hidden-layer founder direct weights");
}

static void ScenarioFactoryHonorsReproductionIntentToggle()
{
    var gated = CreateIntentToggleSimulation(requireIntent: true);
    var ungated = CreateIntentToggleSimulation(requireIntent: false);

    gated.Step();
    ungated.Step();

    AssertEqual(0, gated.State.Eggs.Count, "Intent-gated scenario should not lay without reproduce output");
    AssertEqual(1, ungated.State.Eggs.Count, "Ungated scenario should lay when egg reserve is ready");
}

static Simulation CreateIntentToggleSimulation(bool requireIntent)
{
    var scenario = new SimulationScenario
    {
        Seed = 700,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialCreatureCount = 1,
        InitialResourcesPerMillionArea = 0f,
        RequireReproductionIntent = requireIntent,
        BasalEnergyPerSecond = 0f,
        ReproductionEnergyThreshold = 50f,
        OffspringEnergyInvestment = 20f,
        EggProductionEnergyPerSecond = 20f,
        MaturityAgeSeconds = 0f,
        ReproductionCooldownSeconds = 0f,
        MutationStrength = 0f
    };
    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var creature = simulation.State.Creatures[0];
    simulation.State.Brains[creature.BrainId] = BrainGenome.FromNeural(
        BrainArchitectureKind.HybridNeural,
        NeuralBrainGenome.CreateZero());
    creature.AgeSeconds = 10f;
    creature.Energy = 100f;
    creature.ReproductiveEnergy = 20f;
    simulation.State.Creatures[0] = creature;
    return simulation;
}

static void BrainProfileJsonRoundTripsNeuralControllers()
{
    var scenario = new SimulationScenario
    {
        Name = "Brain Source",
        Seed = 898,
        PipelineKind = SimulationPipelineKind.Neural,
        BrainArchitectureKind = BrainArchitectureKind.HiddenLayerNeural,
        BrainHiddenNodeCount = 8,
        InitialBrainKind = InitialBrainKind.SectorForager,
        InitialCreatureCount = 1,
        InitialResourcesPerMillionArea = 0f
    };
    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var creature = simulation.State.Creatures[0];

    var profile = BrainProfileExporter.ExportCreatureBrain(
        scenario,
        simulation.State,
        creature.Id,
        "Probe brain",
        "Round-trip brain test");
    var brainJson = BrainProfileJson.ToJson(profile);
    var roundTripped = BrainProfileJson.FromJson(brainJson);

    AssertTrue(brainJson.Contains("\"brainArchitectureKind\": \"hiddenLayerNeural\""), "Brain JSON should include architecture");
    AssertTrue(brainJson.Contains($"\"inputSchemaVersion\": {NeuralBrainSchema.InputSchemaVersion}"), "Brain JSON should include input schema version");
    AssertTrue(brainJson.Contains("\"outputSchemaVersion\": 3"), "Brain JSON should include output schema version");
    AssertEqual("Probe brain", roundTripped.Name, "Brain profile name");
    AssertEqual("Round-trip brain test", roundTripped.Notes, "Brain profile notes");
    AssertEqual(BrainArchitectureKind.HiddenLayerNeural, roundTripped.BrainArchitectureKind, "Brain profile architecture");
    AssertEqual(NeuralBrainSchema.InputSchemaVersion, roundTripped.InputSchemaVersion, "Brain profile input schema");
    AssertEqual(NeuralBrainSchema.OutputSchemaVersion, roundTripped.OutputSchemaVersion, "Brain profile output schema");
    AssertEqual(NeuralBrainSchema.InputCount, roundTripped.InputCount, "Brain profile input count");
    AssertEqual(NeuralBrainSchema.OutputCount, roundTripped.OutputCount, "Brain profile output count");
    AssertEqual(8, roundTripped.HiddenNodeCount, "Brain profile hidden node count");
    AssertEqual(creature.Id.Value, roundTripped.Source.CreatureId, "Brain profile source creature");
    AssertEqual(creature.Generation, roundTripped.Source.Generation, "Brain profile source generation");

    var sourceBrain = simulation.State.GetBrain(creature.BrainId);
    var loadedBrain = roundTripped.CreateBrain();
    AssertBrainsClose(sourceBrain, loadedBrain, "Brain profile weights");
}

static void BrainProfileJsonRoundTripsRtNeatGraphControllers()
{
    var profile = new BrainProfile
    {
        Name = "Sparse graph probe",
        Notes = "rtNEAT profile round-trip test",
        BrainArchitectureKind = BrainArchitectureKind.RtNeatGraph,
        RtNeatBrain = RtNeatBrainGenome.CreateStarterForager()
    }.Validated();

    var brainJson = BrainProfileJson.ToJson(profile);
    var roundTripped = BrainProfileJson.FromJson(brainJson);
    var loadedBrain = roundTripped.CreateBrain();

    AssertTrue(brainJson.Contains("\"brainArchitectureKind\": \"rtNeatGraph\""), "rtNEAT JSON should include architecture");
    AssertTrue(brainJson.Contains("\"rtNeatBrain\""), "rtNEAT JSON should include graph payload");
    AssertEqual("Sparse graph probe", roundTripped.Name, "rtNEAT brain profile name");
    AssertEqual(BrainArchitectureKind.RtNeatGraph, roundTripped.BrainArchitectureKind, "rtNEAT brain profile architecture");
    AssertTrue(roundTripped.RtNeatBrain is not null, "Round-tripped rtNEAT profile should retain graph payload");
    AssertEqual(profile.RtNeatBrain!.ConnectionCount, loadedBrain.RtNeat!.ConnectionCount, "rtNEAT connection count");
    AssertEqual(profile.WeightCount, loadedBrain.WeightCount, "rtNEAT profile weight count");
}

static void BrainProfileCompatibilityReportsSchemaStatus()
{
    var brain = BrainFactory.CreateStarter(
        BrainArchitectureKind.HybridNeural,
        InitialBrainKind.SeedForager,
        NeuralBrainSchema.DefaultHiddenNodeCount);
    var profile = new BrainProfile
    {
        Name = "Compatibility probe",
        Weights = brain.Weights.ToArray()
    };

    var current = BrainProfileCompatibility.Assess(profile);
    AssertTrue(current.IsCompatible, "Current brain profile should be compatible");
    AssertTrue(!current.RequiresNormalization, "Current brain profile should not need normalization");
    AssertEqual(0, current.Warnings.Count, "Current brain profile warning count");

    var staleCounts = BrainProfileCompatibility.Assess(profile with
    {
        InputCount = NeuralBrainSchema.InputCount - 1
    });
    AssertTrue(staleCounts.IsCompatible, "Stale schema counts should remain loadable");
    AssertTrue(staleCounts.RequiresNormalization, "Stale schema counts should be reported as normalized");
    AssertTrue(staleCounts.Warnings.Count > 0, "Stale schema counts should produce a warning");

    var futureSchema = BrainProfileCompatibility.Assess(profile with
    {
        InputSchemaVersion = NeuralBrainSchema.InputSchemaVersion + 1
    });
    AssertTrue(!futureSchema.IsCompatible, "Future brain input schema should be incompatible");
    AssertTrue(
        futureSchema.Status.Contains("newer than supported", StringComparison.OrdinalIgnoreCase),
        "Future brain schema warning should explain the version mismatch");
}

static void SpeciesProfileJsonRoundTripsRepresentativeGenomesAndBrains()
{
    var scenario = new SimulationScenario
    {
        Name = "Profile Export Probe",
        Seed = 900,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialBrainKind = InitialBrainKind.ScavengerForager,
        BrainHiddenNodeCount = 4,
        InitialCreatureCount = 4,
        InitialResourcesPerMillionArea = 0f
    };
    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    simulation.RunSteps(2);

    var creature = simulation.State.Creatures[0];
    var profile = SpeciesProfileExporter.ExportCreature(
        scenario,
        simulation.State,
        creature.Id,
        "Probe species",
        "Round-trip test") with
    {
        DefaultBrainPath = "brains/probe.brain.json"
    };
    var profileJson = SpeciesProfileJson.ToJson(profile);
    var roundTripped = SpeciesProfileJson.FromJson(profileJson);

    AssertTrue(profileJson.Contains("\"brainArchitectureKind\": \"hybridNeural\""), "Profile JSON should include brain architecture");
    AssertTrue(profileJson.Contains("\"defaultBrainPath\": \"brains/probe.brain.json\""), "Profile JSON should include default brain path");
    AssertEqual("Probe species", roundTripped.Name, "Profile name");
    AssertEqual("Round-trip test", roundTripped.Notes, "Profile notes");
    AssertEqual("brains/probe.brain.json", roundTripped.DefaultBrainPath, "Profile default brain path");
    AssertEqual(creature.Id.Value, roundTripped.Source.CreatureId, "Profile source creature");
    AssertEqual(creature.Generation, roundTripped.Source.Generation, "Profile source generation");
    AssertEqual(BrainArchitectureKind.HybridNeural, roundTripped.BrainArchitectureKind, "Profile brain architecture");
    AssertEqual(4, roundTripped.BrainHiddenNodeCount, "Profile hidden node count");
    AssertClose(
        simulation.State.GetGenome(creature.GenomeId).BodyRadius,
        roundTripped.Genome.BodyRadius,
        0.000001,
        "Profile genome body radius");

    var sourceBrain = simulation.State.GetBrain(creature.BrainId);
    var loadedBrain = roundTripped.CreateBrain();
    AssertEqual(sourceBrain.Weights.Length, loadedBrain.Weights.Length, "Profile brain weight count");
    for (var i = 0; i < sourceBrain.Weights.Length; i++)
    {
        AssertClose(sourceBrain.Weights[i], loadedBrain.Weights[i], 0.000001, $"Profile brain weight {i}");
    }
}

static void SpeciesProfileInjectionCreatesFounderCreatures()
{
    var sourceScenario = new SimulationScenario
    {
        Name = "Species Source",
        Seed = 901,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialBrainKind = InitialBrainKind.ExplorerForager,
        InitialCreatureCount = 3,
        InitialResourcesPerMillionArea = 0f
    };
    var source = SimulationScenarioFactory.CreateSimulation(sourceScenario);
    var profile = SpeciesProfileExporter.ExportDominantLivingLineageRepresentative(sourceScenario, source.State, "Explorer sample");

    var targetScenario = new SimulationScenario
    {
        Seed = 902,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f,
        ResourceVoidBorderWidth = 20f
    };
    var target = SimulationScenarioFactory.CreateSimulation(targetScenario);
    var genomeCountBefore = target.State.Genomes.Count;
    var brainCountBefore = target.State.Brains.Count;
    var result = SpeciesProfileInjector.Inject(
        target.State,
        profile,
        new SpeciesInjectionOptions(5, InitialCreatureSpawnRegion.LeftThird, EnergyOverride: 33f));

    AssertEqual("Explorer sample", result.SpeciesName, "Injected species name");
    AssertEqual(5, result.CreatureIds.Count, "Injected creature count");
    AssertEqual(5, target.State.Creatures.Count, "Target living creature count");
    AssertEqual(5, target.State.Stats.FounderCreatureCount, "Injected founder count");
    AssertEqual(genomeCountBefore + 1, target.State.Genomes.Count, "Injected genome count");
    AssertEqual(brainCountBefore + 1, target.State.Brains.Count, "Injected brain count");
    AssertEqual(
        profile.BrainArchitectureKind,
        target.State.GetBrainArchitectureKind(result.BrainId),
        "Injected brain architecture");

    foreach (var creature in target.State.Creatures)
    {
        AssertEqual(default(EntityId), creature.ParentId, "Injected creature should be a founder");
        AssertEqual(0, creature.Generation, "Injected creature generation");
        AssertClose(33f, creature.Energy, 0.000001, "Injected creature energy");
        AssertTrue(creature.Position.X >= 20f && creature.Position.X <= target.State.Bounds.Width / 3f, "Injected creature should spawn in left third away from void");
    }
}

static void SpeciesProfileInjectionHonorsQuadrantSpawnRegion()
{
    var sourceScenario = new SimulationScenario
    {
        Name = "Species Source",
        Seed = 903,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialBrainKind = InitialBrainKind.ExplorerForager,
        InitialCreatureCount = 3,
        InitialResourcesPerMillionArea = 0f
    };
    var source = SimulationScenarioFactory.CreateSimulation(sourceScenario);
    var profile = SpeciesProfileExporter.ExportDominantLivingLineageRepresentative(sourceScenario, source.State, "Quadrant sample");

    var targetScenario = new SimulationScenario
    {
        Seed = 904,
        PipelineKind = SimulationPipelineKind.Neural,
        WorldWidth = 1_000f,
        WorldHeight = 800f,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f,
        ResourceVoidBorderWidth = 40f
    };
    var target = SimulationScenarioFactory.CreateSimulation(targetScenario);
    var result = SpeciesProfileInjector.Inject(
        target.State,
        profile,
        new SpeciesInjectionOptions(20, InitialCreatureSpawnRegion.UpperLeftQuadrant, EnergyOverride: 33f));

    AssertEqual(20, result.CreatureIds.Count, "Injected quadrant creature count");
    foreach (var creature in target.State.Creatures)
    {
        AssertTrue(creature.Position.X >= 40f, $"Injected creature {creature.Id.Value} should avoid left resource void");
        AssertTrue(creature.Position.X < 500f, $"Injected creature {creature.Id.Value} should spawn in left half");
        AssertTrue(creature.Position.Y >= 40f, $"Injected creature {creature.Id.Value} should avoid top resource void");
        AssertTrue(creature.Position.Y < 400f, $"Injected creature {creature.Id.Value} should spawn in upper half");
    }
}

static void SpeciesProfileInjectionCanOverrideBrainKind()
{
    var sourceScenario = new SimulationScenario
    {
        Seed = 913,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialBrainKind = InitialBrainKind.ExplorerForager,
        InitialCreatureCount = 1,
        InitialResourcesPerMillionArea = 0f
    };
    var source = SimulationScenarioFactory.CreateSimulation(sourceScenario);
    var profile = SpeciesProfileExporter.ExportDominantLivingLineageRepresentative(sourceScenario, source.State, "Override sample");

    var targetScenario = new SimulationScenario
    {
        Seed = 914,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f
    };
    var target = SimulationScenarioFactory.CreateSimulation(targetScenario);
    var brainCountBefore = target.State.Brains.Count;
    var result = SpeciesProfileInjector.Inject(
        target.State,
        profile,
        new SpeciesInjectionOptions(
            Count: 3,
            EnergyOverride: 35f,
            BrainOverrideKind: InitialBrainKind.ForagerPredator,
            BrainArchitectureKind: BrainArchitectureKind.HiddenLayerNeural,
            BrainHiddenNodeCount: 8));

    AssertEqual(brainCountBefore + 1, target.State.Brains.Count, "Overridden species should add one shared starter brain");
    AssertEqual(
        BrainArchitectureKind.HiddenLayerNeural,
        target.State.GetBrainArchitectureKind(result.BrainId),
        "Overridden species brain architecture");
    AssertEqual(8, target.State.GetBrain(result.BrainId).HiddenNodeCount, "Overridden species hidden brain nodes");
    foreach (var creature in target.State.Creatures)
    {
        AssertEqual(result.BrainId, creature.BrainId, "Overridden species creature brain id");
    }
}

static void SpeciesProfileInjectionCanRandomizeBrainsPerFounder()
{
    var sourceScenario = new SimulationScenario
    {
        Seed = 915,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialBrainKind = InitialBrainKind.ExplorerForager,
        InitialCreatureCount = 1,
        InitialResourcesPerMillionArea = 0f
    };
    var source = SimulationScenarioFactory.CreateSimulation(sourceScenario);
    var profile = SpeciesProfileExporter.ExportDominantLivingLineageRepresentative(sourceScenario, source.State, "Randomized sample");

    var targetScenario = new SimulationScenario
    {
        Seed = 916,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f
    };
    var target = SimulationScenarioFactory.CreateSimulation(targetScenario);
    var brainCountBefore = target.State.Brains.Count;
    var result = SpeciesProfileInjector.Inject(
        target.State,
        profile,
        new SpeciesInjectionOptions(
            Count: 4,
            EnergyOverride: 35f,
            BrainOverrideKind: InitialBrainKind.RandomPerFounder,
            BrainArchitectureKind: BrainArchitectureKind.HybridNeural,
            BrainHiddenNodeCount: 4));

    var brainIds = target.State.Creatures.Select(creature => creature.BrainId).ToHashSet();
    AssertEqual(brainCountBefore + 4, target.State.Brains.Count, "Randomized species should create one brain per founder");
    AssertEqual(4, brainIds.Count, "Randomized species founder brain ids");
    AssertTrue(brainIds.Contains(result.BrainId), "Randomized species result should report one injected brain id");
    foreach (var brainId in brainIds)
    {
        AssertEqual(
            BrainArchitectureKind.HybridNeural,
            target.State.GetBrainArchitectureKind(brainId),
            "Randomized species brain architecture");
        AssertEqual(4, target.State.GetBrain(brainId).HiddenNodeCount, "Randomized species hidden brain nodes");
    }
}

static void SpeciesClusteringGroupsInjectedProfileFounders()
{
    var sourceScenario = new SimulationScenario
    {
        Name = "Cluster Source",
        Seed = 910,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialBrainKind = InitialBrainKind.ExplorerForager,
        InitialCreatureCount = 3,
        InitialResourcesPerMillionArea = 0f
    };
    var source = SimulationScenarioFactory.CreateSimulation(sourceScenario);
    var profile = SpeciesProfileExporter.ExportDominantLivingLineageRepresentative(sourceScenario, source.State, "Explorer cluster");

    var targetScenario = new SimulationScenario
    {
        Seed = 911,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f
    };
    var target = SimulationScenarioFactory.CreateSimulation(targetScenario);
    SpeciesProfileInjector.Inject(
        target.State,
        profile,
        new SpeciesInjectionOptions(5, InitialCreatureSpawnRegion.Uniform, EnergyOverride: 40f));

    var clusters = SpeciesClusterAnalyzer.Analyze(target.State);

    AssertEqual(1, clusters.Count, "Injected profile cluster count");
    AssertEqual(5, clusters[0].LivingCreatures, "Injected profile cluster living count");
    AssertEqual(5, clusters[0].FounderCount, "Injected profile cluster founder count");
    AssertClose(1f, clusters[0].LivingShare, 0.000001, "Injected profile cluster living share");
    AssertTrue(!string.IsNullOrWhiteSpace(clusters[0].Name), "Injected profile cluster should have a name");
}

static void SpeciesClusteringSplitsDistinctBrains()
{
    var scenario = new SimulationScenario
    {
        Seed = 912,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f,
        WorldWidth = 300f,
        WorldHeight = 100f,
        ResourceVoidBorderWidth = 0f
    };
    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    var saturatedWeights = new float[NeuralBrainGenome.DirectWeightCount];
    Array.Fill(saturatedWeights, 8f);
    var quietBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateZero());
    var saturatedBrainId = simulation.State.AddBrain(new NeuralBrainGenome(saturatedWeights));

    simulation.State.SpawnCreature(genomeId, new SimVector2(30f, 30f), energy: 35f, brainId: quietBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(40f, 30f), energy: 35f, brainId: quietBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(50f, 30f), energy: 35f, brainId: quietBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(180f, 30f), energy: 35f, brainId: saturatedBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(190f, 30f), energy: 35f, brainId: saturatedBrainId);

    var clusters = SpeciesClusterAnalyzer.Analyze(simulation.State);

    AssertEqual(2, clusters.Count, "Distinct brain cluster count");
    AssertEqual(3, clusters[0].LivingCreatures, "Largest distinct brain cluster");
    AssertEqual(2, clusters[1].LivingCreatures, "Second distinct brain cluster");
}

static void SpeciesClusteringSeparatesStarterEcotypes()
{
    var scenario = new SimulationScenario
    {
        Seed = 915,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f,
        WorldWidth = 300f,
        WorldHeight = 100f,
        ResourceVoidBorderWidth = 0f
    };
    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    var seedBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateSeedForager(4));
    var explorerBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateExplorerForager(4));
    var scavengerBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateScavengerForager(4));
    var predatorBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateForagerPredator(4));

    simulation.State.SpawnCreature(genomeId, new SimVector2(30f, 30f), energy: 35f, brainId: seedBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(40f, 30f), energy: 35f, brainId: seedBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(70f, 30f), energy: 35f, brainId: explorerBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(80f, 30f), energy: 35f, brainId: explorerBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(150f, 30f), energy: 35f, brainId: scavengerBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(160f, 30f), energy: 35f, brainId: scavengerBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(230f, 30f), energy: 35f, brainId: predatorBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(240f, 30f), energy: 35f, brainId: predatorBrainId);

    var clusters = SpeciesClusterAnalyzer.Analyze(simulation.State);

    AssertEqual(3, clusters.Count, "Starter ecotype cluster count");
    AssertTrue(clusters.Any(cluster => cluster.LivingCreatures == 4), "Seed and explorer starters should remain one plant-forager cluster");
    AssertEqual(2, clusters.Count(cluster => cluster.LivingCreatures == 2), "Scavenger and predator starters should split from plant foragers");

    var fingerprints = SpeciesClusterAnalyzer.AnalyzeBehaviorFingerprints(simulation.State);

    AssertEqual(3, fingerprints.Count, "Starter ecotype fingerprint count");
    AssertTrue(fingerprints.All(fingerprint => fingerprint.EvaluatedCreatureCount == fingerprint.LivingCreatures), "Species fingerprints should evaluate each neural creature");
    AssertTrue(fingerprints.Any(fingerprint => fingerprint.Ecotype == "small-prey predator"), "Species fingerprints should preserve predator behavior");
    AssertTrue(fingerprints.Any(fingerprint => fingerprint.ForagingBias == "meat/egg-biased"), "Species fingerprints should preserve scavenger behavior");

    var predatorFingerprint = fingerprints.First(fingerprint => fingerprint.Ecotype == "small-prey predator");
    var representative = SpeciesClusterAnalyzer.FindRepresentative(simulation.State, predatorFingerprint.Name);
    var profile = SpeciesProfileExporter.ExportSpeciesClusterRepresentative(
        scenario,
        simulation.State,
        predatorFingerprint.SpeciesId.ToString());

    AssertEqual(predatorFingerprint.SpeciesId, representative.SpeciesId, "Cluster representative species id");
    AssertEqual(predatorFingerprint.Name, profile.Name, "Cluster export default profile name");
    AssertEqual(representative.CreatureId.Value, profile.Source.CreatureId, "Cluster export representative creature");
}

static void SpeciesClusteringHandlesNonNeuralCreatures()
{
    var scenario = new SimulationScenario
    {
        Seed = 913,
        PipelineKind = SimulationPipelineKind.SimpleForaging,
        InitialCreatureCount = 3,
        InitialResourcesPerMillionArea = 0f
    };
    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);

    var clusters = SpeciesClusterAnalyzer.Analyze(simulation.State);

    AssertEqual(1, clusters.Count, "Simple controller cluster count");
    AssertEqual(3, clusters[0].LivingCreatures, "Simple controller living count");
}

static void SpeciesClusterHistoryTracksSnapshots()
{
    var scenario = new SimulationScenario
    {
        Seed = 914,
        PipelineKind = SimulationPipelineKind.Neural,
        InitialCreatureCount = 0,
        InitialResourcesPerMillionArea = 0f,
        StatsSnapshotIntervalTicks = 2,
        WorldWidth = 300f,
        WorldHeight = 100f,
        ResourceVoidBorderWidth = 0f
    };
    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var genomeId = simulation.State.AddGenome(CreatureGenome.Baseline);
    var saturatedWeights = new float[NeuralBrainGenome.DirectWeightCount];
    Array.Fill(saturatedWeights, 8f);
    var quietBrainId = simulation.State.AddBrain(NeuralBrainGenome.CreateZero());
    var saturatedBrainId = simulation.State.AddBrain(new NeuralBrainGenome(saturatedWeights));

    simulation.State.SpawnCreature(genomeId, new SimVector2(30f, 30f), energy: 35f, brainId: quietBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(40f, 30f), energy: 35f, brainId: quietBrainId);
    simulation.State.SpawnCreature(genomeId, new SimVector2(180f, 30f), energy: 35f, brainId: saturatedBrainId);
    simulation.RunSteps(3);

    var history = SpeciesClusterAnalyzer.AnalyzeHistory(simulation.State, simulation.State.Stats.Snapshots);
    var finalTick = simulation.State.Tick;
    var finalRows = history.Rows
        .Where(row => row.Tick == finalTick)
        .OrderBy(row => row.Rank)
        .ToArray();

    AssertEqual(2, history.Clusters.Count, "Species history cluster count");
    AssertEqual(2, history.Clusters[0].FinalLivingCreatures, "Largest history cluster final living count");
    AssertEqual(1, history.Clusters[1].FinalLivingCreatures, "Second history cluster final living count");
    AssertEqual(2, finalRows.Length, "Species history final row count");
    AssertEqual(3, finalRows.Sum(row => row.LivingCreatures), "Species history final living total");
    AssertClose(2f / 3f, finalRows[0].LivingShare, 0.000001, "Species history final dominant share");
    AssertTrue(history.Rows.All(row => row.Rank > 0), "Species history rows should be ranked");
    AssertTrue(history.Clusters.All(cluster => !string.IsNullOrWhiteSpace(cluster.LifecycleLabel)), "Species history clusters should have lifecycle labels");
    AssertTrue(history.DiversityRows.Count > 0, "Species history should include diversity rows");
    AssertEqual(simulation.State.Tick, history.DiversityRows[^1].Tick, "Species history should include current final tick");
    AssertEqual(simulation.State.Creatures.Count, history.DiversityRows[^1].TotalLiving, "Species history final diversity total should match current world state");
    AssertEqual(2, history.DiversityRows[^1].ActiveClusterCount, "Species history final active cluster count");
    AssertClose(2f / 3f, history.DiversityRows[^1].DominantLivingShare, 0.000001, "Species history final dominant diversity share");
    AssertTrue(history.Notes.Count > 0, "Species history should include interpretation notes");

    var summaries = SpeciesClusterAnalyzer.Analyze(simulation.State);
    var interpretations = SpeciesClusterAnalyzer.InterpretClusters(summaries, history);

    AssertEqual(2, interpretations.Count, "Species interpretation count");
    AssertTrue(interpretations.All(interpretation => !string.IsNullOrWhiteSpace(interpretation.RoleLabel)), "Species interpretations should include role labels");
    AssertTrue(interpretations.All(interpretation => !string.IsNullOrWhiteSpace(interpretation.AncestryLabel)), "Species interpretations should include ancestry labels");
    AssertTrue(interpretations.All(interpretation => !string.IsNullOrWhiteSpace(interpretation.TrendLabel)), "Species interpretations should include trend labels");
    AssertTrue(interpretations.All(interpretation => !string.IsNullOrWhiteSpace(interpretation.ImportanceLabel)), "Species interpretations should include importance labels");
    AssertTrue(interpretations.Any(interpretation => interpretation.EvidenceLabel.Contains("peak", StringComparison.Ordinal)), "Species interpretations should include history evidence");

    var behaviorChanges = SpeciesClusterAnalyzer.AnalyzeBehaviorChanges(simulation.State, history);

    AssertEqual(2, behaviorChanges.Count, "Species behavior change count");
    AssertTrue(behaviorChanges.All(change => change.EarlySampleCount > 0), "Species behavior changes should include early samples");
    AssertTrue(behaviorChanges.All(change => change.FinalSampleCount > 0), "Species behavior changes should include final samples");
    AssertTrue(behaviorChanges.All(change => change.FinalSampleKind == "final living"), "Living species behavior changes should use final living samples");
    AssertTrue(behaviorChanges.All(change => !string.IsNullOrWhiteSpace(change.Summary)), "Species behavior changes should include summaries");
}

static void SpeciesBehaviorChangeHighlightsNotableShifts()
{
    var changes = new[]
    {
        new SpeciesClusterBehaviorChange(
            Rank: 1,
            SpeciesId: 10,
            Name: "Stable",
            EarlySampleCount: 8,
            FinalSampleCount: 8,
            FinalSampleKind: "final living",
            EarlyEcotype: "generalist forager",
            FinalEcotype: "generalist forager",
            EarlyForagingBias: "mixed food response",
            FinalForagingBias: "mixed food response",
            EarlyRottenMeatResponse: "little freshness differentiation",
            FinalRottenMeatResponse: "little freshness differentiation",
            EarlyRiskResponse: "little risk differentiation",
            FinalRiskResponse: "little risk differentiation",
            EarlyTerrainResponse: "little terrain differentiation",
            FinalTerrainResponse: "little terrain differentiation",
            EarlyPredatorTendency: "rare attack response",
            FinalPredatorTendency: "rare attack response",
            EarlyMovementStyle: "moderate wandering",
            FinalMovementStyle: "moderate wandering",
            EarlyReproductionTendency: "readily lays completed eggs",
            FinalReproductionTendency: "readily lays completed eggs",
            BaselineMoveDelta: 0.04f,
            PlantMoveDelta: 0.05f,
            MeatMoveDelta: -0.05f,
            RotScentMoveDelta: 0.02f,
            SmallAttackDelta: 0.04f,
            EggLayingDelta: 0.05f,
            Summary: "no meaningful behavioral change detected"),
        new SpeciesClusterBehaviorChange(
            Rank: 2,
            SpeciesId: 20,
            Name: "Shifted",
            EarlySampleCount: 8,
            FinalSampleCount: 8,
            FinalSampleKind: "final living",
            EarlyEcotype: "generalist forager",
            FinalEcotype: "generalist forager",
            EarlyForagingBias: "mixed food response",
            FinalForagingBias: "meat-biased forager",
            EarlyRottenMeatResponse: "little freshness differentiation",
            FinalRottenMeatResponse: "little freshness differentiation",
            EarlyRiskResponse: "little risk differentiation",
            FinalRiskResponse: "little risk differentiation",
            EarlyTerrainResponse: "little terrain differentiation",
            FinalTerrainResponse: "rough terrain specialist",
            EarlyPredatorTendency: "rare attack response",
            FinalPredatorTendency: "frequent attack response",
            EarlyMovementStyle: "moderate wandering",
            FinalMovementStyle: "moderate wandering",
            EarlyReproductionTendency: "rare egg laying",
            FinalReproductionTendency: "readily lays completed eggs",
            BaselineMoveDelta: 0.1f,
            PlantMoveDelta: -0.35f,
            MeatMoveDelta: 0.3f,
            RotScentMoveDelta: 0.02f,
            SmallAttackDelta: 0.08f,
            EggLayingDelta: 0.18f,
            Summary: "synthetic shift")
    };

    var notable = SpeciesClusterAnalyzer.FindNotableBehaviorChanges(changes);

    AssertEqual(1, notable.Count, "Notable behavior shift count");
    AssertEqual(20, notable[0].SpeciesId, "Notable behavior shift species");
    AssertTrue(notable[0].Score > 0f, "Notable behavior shift should have a score");
    AssertTrue(notable[0].Summary.Contains("food response", StringComparison.Ordinal), "Notable behavior shift should mention food");
    AssertTrue(notable[0].Summary.Contains("attack", StringComparison.Ordinal), "Notable behavior shift should mention attack");
    AssertEqual(0, SpeciesClusterAnalyzer.FindNotableBehaviorChanges(changes, maxChanges: 0).Count, "Zero max notable behavior changes");
}

static void ScenarioSpeciesRosterInjectsProfileFounders()
{
    var sourceScenario = new SimulationScenario
    {
        Seed = 802,
        InitialCreatureCount = 1,
        InitialResourcesPerMillionArea = 0f,
        InitialBrainKind = InitialBrainKind.ExplorerForager
    };
    var source = SimulationScenarioFactory.CreateSimulation(sourceScenario);
    var profile = SpeciesProfileExporter.ExportDominantLivingLineageRepresentative(
        sourceScenario,
        source.State,
        "Roster explorer");

    var tempRoot = Path.Combine(Path.GetTempPath(), $"lineage_roster_{Guid.NewGuid():N}");
    try
    {
        var profilePath = Path.Combine(tempRoot, "species", "roster-explorer.species.json");
        SpeciesProfileJson.Save(profilePath, profile);

        var scenarioPath = Path.Combine(tempRoot, "scenarios", "roster-scenario.json");
        var scenario = new SimulationScenario
        {
            Seed = 803,
            WorldWidth = 900f,
            WorldHeight = 600f,
            ResourceVoidBorderWidth = 20f,
            InitialCreatureCount = 99,
            InitialResourcesPerMillionArea = 0f,
            SpeciesSeeds =
            [
                new SpeciesScenarioSeed
                {
                    ProfilePath = "species/roster-explorer.species.json",
                    Count = 4,
                    SpawnRegion = InitialCreatureSpawnRegion.RightThird,
                    EnergyOverride = 44f
                }
            ]
        };
        SimulationScenarioJson.Save(scenarioPath, scenario);

        var loadedScenario = SimulationScenarioJson.Load(scenarioPath);
        var simulation = SimulationScenarioFactory.CreateSimulation(loadedScenario);
        AssertEqual(0, simulation.State.Creatures.Count, "Scenario roster should replace generic initial creatures");
        AssertEqual(0, simulation.State.Genomes.Count, "Generic starter genome should not be created for roster scenarios");
        AssertEqual(0, simulation.State.Brains.Count, "Generic starter brain should not be created for roster scenarios");

        var results = SimulationScenarioSpeciesSeeder.InjectScenarioSpecies(
            loadedScenario,
            simulation.State,
            scenarioPath);

        AssertEqual(1, results.Count, "Roster injection result count");
        AssertEqual("Roster explorer", results[0].SpeciesName, "Roster species name");
        AssertEqual(4, results[0].CreatureIds.Count, "Roster creature count");
        AssertEqual(4, simulation.State.Creatures.Count, "Injected roster living count");
        AssertEqual(4, simulation.State.Stats.FounderCreatureCount, "Injected roster founder count");
        AssertEqual(1, simulation.State.Genomes.Count, "Roster genome count");
        AssertEqual(1, simulation.State.Brains.Count, "Roster brain count");

        foreach (var creature in simulation.State.Creatures)
        {
            AssertClose(44f, creature.Energy, 0.000001, "Roster energy override");
            AssertTrue(creature.Position.X > simulation.State.Bounds.Width * 2f / 3f, "Roster creature should spawn in right third");
        }
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void ScenarioSpeciesRosterLabelNamesInjectionGroups()
{
    var sourceScenario = new SimulationScenario
    {
        Seed = 813,
        InitialCreatureCount = 1,
        InitialResourcesPerMillionArea = 0f,
        InitialBrainKind = InitialBrainKind.ExplorerForager
    };
    var source = SimulationScenarioFactory.CreateSimulation(sourceScenario);
    var profile = SpeciesProfileExporter.ExportDominantLivingLineageRepresentative(
        sourceScenario,
        source.State,
        "Reusable body");

    var tempRoot = Path.Combine(Path.GetTempPath(), $"lineage_roster_label_{Guid.NewGuid():N}");
    try
    {
        var profilePath = Path.Combine(tempRoot, "species", "reusable-body.species.json");
        SpeciesProfileJson.Save(profilePath, profile);

        var scenarioPath = Path.Combine(tempRoot, "scenarios", "roster-label-scenario.json");
        var scenario = new SimulationScenario
        {
            Seed = 814,
            InitialCreatureCount = 0,
            InitialResourcesPerMillionArea = 0f,
            SpeciesSeeds =
            [
                new SpeciesScenarioSeed
                {
                    Label = "Reusable body control",
                    ProfilePath = "species/reusable-body.species.json",
                    Count = 2,
                    Enabled = true
                },
                new SpeciesScenarioSeed
                {
                    Label = "Reusable body transplant",
                    ProfilePath = "species/reusable-body.species.json",
                    Count = 3,
                    BrainOverrideKind = InitialBrainKind.ScavengerForager,
                    Enabled = true
                }
            ]
        };
        SimulationScenarioJson.Save(scenarioPath, scenario);

        var loadedScenario = SimulationScenarioJson.Load(scenarioPath);
        var simulation = SimulationScenarioFactory.CreateSimulation(loadedScenario);
        var results = SimulationScenarioSpeciesSeeder.InjectScenarioSpecies(
            loadedScenario,
            simulation.State,
            scenarioPath);

        AssertEqual(2, results.Count, "Labeled roster injection result count");
        AssertEqual("Reusable body control", results[0].SpeciesName, "First roster label");
        AssertEqual("Reusable body transplant", results[1].SpeciesName, "Second roster label");

        var summaries = RosterLineageAnalyzer
            .Analyze(simulation.State.LineageRecords, results, simulation.State.Tick)
            .ToDictionary(summary => summary.ProfileName);
        AssertEqual(2, summaries["Reusable body control"].FounderCount, "Control founder count");
        AssertEqual(3, summaries["Reusable body transplant"].FounderCount, "Transplant founder count");
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void ScenarioSpeciesRosterInjectsBrainOverrides()
{
    var sourceScenario = new SimulationScenario
    {
        Seed = 806,
        InitialCreatureCount = 1,
        InitialResourcesPerMillionArea = 0f,
        InitialBrainKind = InitialBrainKind.ExplorerForager
    };
    var source = SimulationScenarioFactory.CreateSimulation(sourceScenario);
    var profile = SpeciesProfileExporter.ExportDominantLivingLineageRepresentative(
        sourceScenario,
        source.State,
        "Roster override");

    var tempRoot = Path.Combine(Path.GetTempPath(), $"lineage_roster_brain_{Guid.NewGuid():N}");
    try
    {
        var profilePath = Path.Combine(tempRoot, "species", "roster-override.species.json");
        SpeciesProfileJson.Save(profilePath, profile);

        var scenarioPath = Path.Combine(tempRoot, "scenarios", "roster-brain-scenario.json");
        var scenario = new SimulationScenario
        {
            Seed = 807,
            PipelineKind = SimulationPipelineKind.Neural,
            BrainArchitectureKind = BrainArchitectureKind.HiddenLayerNeural,
            BrainHiddenNodeCount = 8,
            InitialCreatureCount = 99,
            InitialResourcesPerMillionArea = 0f,
            SpeciesSeeds =
            [
                new SpeciesScenarioSeed
                {
                    ProfilePath = "species/roster-override.species.json",
                    Count = 3,
                    EnergyOverride = 40f,
                    BrainOverrideKind = InitialBrainKind.ScavengerForager
                }
            ]
        };
        SimulationScenarioJson.Save(scenarioPath, scenario);

        var loadedScenario = SimulationScenarioJson.Load(scenarioPath);
        var simulation = SimulationScenarioFactory.CreateSimulation(loadedScenario);
        var results = SimulationScenarioSpeciesSeeder.InjectScenarioSpecies(
            loadedScenario,
            simulation.State,
            scenarioPath);

        AssertEqual(1, results.Count, "Roster override injection result count");
        AssertEqual(3, results[0].CreatureIds.Count, "Roster override creature count");
        AssertEqual(1, simulation.State.Brains.Count, "Roster override should use a shared starter brain");
        AssertEqual(
            BrainArchitectureKind.HiddenLayerNeural,
            simulation.State.GetBrainArchitectureKind(results[0].BrainId),
            "Roster override brain architecture");
        AssertEqual(8, simulation.State.GetBrain(results[0].BrainId).HiddenNodeCount, "Roster override hidden brain nodes");
        foreach (var creature in simulation.State.Creatures)
        {
            AssertEqual(results[0].BrainId, creature.BrainId, "Roster override creature brain id");
        }
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void ScenarioSpeciesRosterInjectsBrainProfilePaths()
{
    var sourceScenario = new SimulationScenario
    {
        Seed = 808,
        InitialCreatureCount = 1,
        InitialResourcesPerMillionArea = 0f,
        InitialBrainKind = InitialBrainKind.ExplorerForager
    };
    var source = SimulationScenarioFactory.CreateSimulation(sourceScenario);
    var sourceCreature = source.State.Creatures[0];
    var speciesProfile = SpeciesProfileExporter.ExportCreature(
        sourceScenario,
        source.State,
        sourceCreature.Id,
        "Roster body");

    var brainProfile = new BrainProfile
    {
        Name = "Roster transplanted brain",
        BrainArchitectureKind = BrainArchitectureKind.HiddenLayerNeural,
        Weights = BrainFactory.CreateStarter(
                BrainArchitectureKind.HiddenLayerNeural,
                InitialBrainKind.ForagerPredator,
                hiddenNodeCount: 8)
            .Weights
            .ToArray()
    }.Validated();

    var tempRoot = Path.Combine(Path.GetTempPath(), $"lineage_roster_brain_profile_{Guid.NewGuid():N}");
    try
    {
        var profilePath = Path.Combine(tempRoot, "species", "roster-body.species.json");
        var brainPath = Path.Combine(tempRoot, "brains", "transplant.brain.json");
        SpeciesProfileJson.Save(profilePath, speciesProfile);
        BrainProfileJson.Save(brainPath, brainProfile);

        var scenarioPath = Path.Combine(tempRoot, "scenarios", "roster-brain-profile-scenario.json");
        var scenario = new SimulationScenario
        {
            Seed = 809,
            PipelineKind = SimulationPipelineKind.Neural,
            InitialCreatureCount = 0,
            InitialResourcesPerMillionArea = 0f,
            SpeciesSeeds =
            [
                new SpeciesScenarioSeed
                {
                    ProfilePath = "species/roster-body.species.json",
                    BrainProfilePath = "brains/transplant.brain.json",
                    Count = 2,
                    EnergyOverride = 40f
                }
            ]
        };
        SimulationScenarioJson.Save(scenarioPath, scenario);

        var loadedScenario = SimulationScenarioJson.Load(scenarioPath);
        var simulation = SimulationScenarioFactory.CreateSimulation(loadedScenario);
        var results = SimulationScenarioSpeciesSeeder.InjectScenarioSpecies(
            loadedScenario,
            simulation.State,
            scenarioPath);

        AssertEqual(1, results.Count, "Roster brain profile injection result count");
        AssertEqual(2, results[0].CreatureIds.Count, "Roster brain profile creature count");
        AssertEqual(
            BrainArchitectureKind.HiddenLayerNeural,
            simulation.State.GetBrainArchitectureKind(results[0].BrainId),
            "Roster brain profile architecture");
        AssertEqual(8, simulation.State.GetBrain(results[0].BrainId).HiddenNodeCount, "Roster brain profile hidden nodes");
        foreach (var creature in simulation.State.Creatures)
        {
            AssertEqual(results[0].GenomeId, creature.GenomeId, "Roster brain profile should keep species body genome");
            AssertEqual(results[0].BrainId, creature.BrainId, "Roster brain profile creature brain id");
        }
    }
    finally
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}

static void RosterLineageSummariesGroupInjectedProfileDescendants()
{
    var sourceScenario = new SimulationScenario
    {
        Seed = 804,
        InitialCreatureCount = 1,
        InitialResourcesPerMillionArea = 0f,
        InitialBrainKind = InitialBrainKind.ExplorerForager
    };
    var source = SimulationScenarioFactory.CreateSimulation(sourceScenario);
    var profile = SpeciesProfileExporter.ExportDominantLivingLineageRepresentative(
        sourceScenario,
        source.State,
        "Source profile");

    var simulation = new Simulation(
        new SimulationConfig
        {
            WorldWidth = 500f,
            WorldHeight = 500f
        },
        seed: 805);
    var foragerInjection = SpeciesProfileInjector.Inject(
        simulation.State,
        profile with { Name = "Roster forager" },
        new SpeciesInjectionOptions(2, EnergyOverride: 40f));
    var scavengerInjection = SpeciesProfileInjector.Inject(
        simulation.State,
        profile with { Name = "Roster scavenger" },
        new SpeciesInjectionOptions(1, EnergyOverride: 40f));

    simulation.State.SpawnCreature(
        foragerInjection.GenomeId,
        new SimVector2(100f, 100f),
        energy: 20f,
        generation: 1,
        parentId: foragerInjection.CreatureIds[0],
        brainId: foragerInjection.BrainId);

    var summaries = RosterLineageAnalyzer.Analyze(
            simulation.State.LineageRecords,
            [foragerInjection, scavengerInjection])
        .ToDictionary(summary => summary.ProfileName);

    AssertEqual(2, summaries.Count, "Roster summary count");

    var foragerSummary = summaries["Roster forager"];
    AssertEqual(2, foragerSummary.FounderCount, "Forager founder count");
    AssertEqual(3, foragerSummary.TotalCreatures, "Forager total count");
    AssertEqual(1, foragerSummary.DescendantCount, "Forager descendant count");
    AssertEqual(3, foragerSummary.LivingCreatures, "Forager living count");
    AssertEqual(1, foragerSummary.MaxGeneration, "Forager max generation");

    var forager = simulation.State.Creatures.First(creature => creature.Id == foragerInjection.CreatureIds[0]);
    forager.IsTouchingFood = true;
    forager.IsTouchingCreature = true;
    forager.LastCaloriesEaten = 10f;
    forager.LastPlantCaloriesEaten = 4f;
    forager.LastCarcassCaloriesEaten = 3f;
    forager.LastEggCaloriesEaten = 1f;
    forager.LastLivePreyCaloriesEaten = 2f;
    forager.LastFreshMeatCaloriesEaten = 5f;
    forager.LastStaleMeatCaloriesEaten = 1f;
    forager.LastRottenMeatDamage = 0.5f;
    forager.LastAttackDamageDealt = 0.25f;
    forager.LastAttackDamageTaken = 0.1f;
    forager.Actions = new CreatureActionState { WantsAttack = true };
    var foragerSenses = forager.Senses;
    foragerSenses.MeatDetected = true;
    foragerSenses.VisibleMeatFreshness = 1f;
    foragerSenses.RottenMeatScentDetected = true;
    foragerSenses.CreatureContactSimilarity = 1f;
    forager.Senses = foragerSenses;
    var foragerIndex = simulation.State.Creatures.FindIndex(creature => creature.Id == forager.Id);
    simulation.State.Creatures[foragerIndex] = forager;
    new LineageTelemetrySystem().Update(simulation.State, 2f);

    summaries = RosterLineageAnalyzer.Analyze(
            simulation.State.LineageRecords,
            [foragerInjection, scavengerInjection])
        .ToDictionary(summary => summary.ProfileName);
    foragerSummary = summaries["Roster forager"];
    AssertClose(6f, foragerSummary.TelemetryLivingSeconds, 0.000001, "Forager telemetry seconds");
    AssertClose(10f / 6f, foragerSummary.CaloriesEatenPerSecond, 0.000001, "Forager calories per second");
    AssertClose(4f / 6f, foragerSummary.PlantCaloriesEatenPerSecond, 0.000001, "Forager plant calories per second");
    AssertClose(1f, foragerSummary.MeatCaloriesEatenPerSecond, 0.000001, "Forager meat calories per second");
    AssertClose(2f / 6f, foragerSummary.FreshKillCaloriesEatenPerSecond, 0.000001, "Forager fresh-kill calories per second");
    AssertClose(0.6f, foragerSummary.MeatCaloriesEatenShare, 0.000001, "Forager meat calorie share");
    AssertClose(0.2f, foragerSummary.FreshKillCaloriesEatenShare, 0.000001, "Forager fresh-kill calorie share");
    AssertClose(5f / 6f, foragerSummary.FreshMeatCaloriesEatenShare, 0.000001, "Forager fresh meat share");
    AssertClose(0.5f / 6f, foragerSummary.RottenMeatDamagePerSecond, 0.000001, "Forager rotten damage per second");
    AssertClose(0.25f / 6f, foragerSummary.AttackDamageDealtPerSecond, 0.000001, "Forager attack damage per second");
    AssertClose(0.1f / 6f, foragerSummary.AttackDamageTakenPerSecond, 0.000001, "Forager damage taken per second");
    AssertClose(1f / 3f, foragerSummary.EatingShare, 0.000001, "Forager eating share");
    AssertClose(1f / 3f, foragerSummary.MeatEatingShare, 0.000001, "Forager meat eating share");
    AssertClose(1f / 3f, foragerSummary.MeatDetectedShare, 0.000001, "Forager meat detected share");
    AssertClose(1f / 3f, foragerSummary.AttackIntentTouchingShare, 0.000001, "Forager touching attack share");

    var scavenger = simulation.State.Creatures.First(creature => creature.Id == scavengerInjection.CreatureIds[0]);
    scavenger.Health = 0f;
    scavenger.LastAttackDamageTaken = 1f;
    scavenger.LastDamagingCreatureId = foragerInjection.CreatureIds[0];
    var scavengerIndex = simulation.State.Creatures.FindIndex(creature => creature.Id == scavenger.Id);
    simulation.State.Creatures[scavengerIndex] = scavenger;
    new DeathSystem().Update(simulation.State, 1f);

    summaries = RosterLineageAnalyzer.Analyze(
            simulation.State.LineageRecords,
            [foragerInjection, scavengerInjection],
            finalTick: 10)
        .ToDictionary(summary => summary.ProfileName);
    foragerSummary = summaries["Roster forager"];
    AssertEqual(1, foragerSummary.CrossProfileInjuryKillsDealt, "Forager cross-profile kills dealt");
    AssertEqual(0, foragerSummary.SameProfileInjuryKillsDealt, "Forager same-profile kills dealt");

    var scavengerSummary = summaries["Roster scavenger"];
    AssertEqual(1, scavengerSummary.FounderCount, "Scavenger founder count");
    AssertEqual(1, scavengerSummary.TotalCreatures, "Scavenger total count");
    AssertEqual(0, scavengerSummary.DescendantCount, "Scavenger descendant count");
    AssertEqual(0, scavengerSummary.LivingCreatures, "Scavenger living count");
    AssertEqual(0, scavengerSummary.MaxGeneration, "Scavenger max generation");
    AssertEqual(1, scavengerSummary.InjuryDeathsFromOtherProfile, "Scavenger other-profile injury death");
    AssertEqual(0, scavengerSummary.InjuryDeathsFromSameProfile, "Scavenger same-profile injury death");
    AssertEqual(0, scavengerSummary.InjuryDeathsFromUnknownProfile, "Scavenger unattributed injury death");
    AssertEqual((long?)0, scavengerSummary.ExtinctionTick, "Scavenger extinction tick");
    AssertClose(0f, scavengerSummary.TailAverageLivingCreatures, 0.000001, "Scavenger tail living");
}

static void SimulationSnapshotsRestoreExactContinuation()
{
    var scenario = new SimulationScenario
    {
        Seed = 33,
        WorldWidth = 500f,
        WorldHeight = 400f,
        BiomeCellSize = 125f,
        InitialBrainKind = InitialBrainKind.SeedForager,
        EnableSectorVision = false,
        EnableObstacles = true,
        ObstacleMapKind = ObstacleMapKind.HorizontalBarrierWithGaps,
        ObstacleCellSize = 100f,
        InitialCreatureCount = 8,
        InitialResourcesPerMillionArea = 80f,
        StatsSnapshotIntervalTicks = 1
    };
    var original = SimulationScenarioFactory.CreateSimulation(scenario);

    original.RunSteps(50);
    var snapshot = SimulationSnapshot.Capture(scenario, original);
    var snapshotJson = SimulationSnapshotJson.ToJson(snapshot);
    AssertTrue(snapshotJson.Contains("\"brainArchitectureKinds\""), "Snapshot JSON should include brain architectures");
    var roundTrippedSnapshot = SimulationSnapshotJson.FromJson(snapshotJson);
    var restored = SimulationSnapshotJson.RestoreSimulation(roundTrippedSnapshot).Simulation;

    original.RunSteps(25);
    restored.RunSteps(25);

    AssertEqual(original.State.Tick, restored.State.Tick, "Restored tick");
    AssertClose(original.State.ElapsedSeconds, restored.State.ElapsedSeconds, 0.000001, "Restored elapsed seconds");
    AssertEqual(original.State.Random.State, restored.State.Random.State, "Restored random state");
    AssertEqual(original.State.Creatures.Count, restored.State.Creatures.Count, "Restored creature count");
    AssertEqual(original.State.Eggs.Count, restored.State.Eggs.Count, "Restored egg count");
    AssertEqual(original.State.Resources.Count, restored.State.Resources.Count, "Restored resource count");
    AssertEqual(original.State.LineageRecords.Count, restored.State.LineageRecords.Count, "Restored lineage count");
    AssertEqual(original.State.Stats.Snapshots.Count, restored.State.Stats.Snapshots.Count, "Restored snapshot count");
    AssertEqual(original.State.Obstacles.BlockedCellCount, restored.State.Obstacles.BlockedCellCount, "Restored obstacle count");
    AssertEqual(original.State.Brains.Count, restored.State.Brains.Count, "Restored brain count");
    AssertEqual(original.State.Brains.Count, roundTrippedSnapshot.BrainArchitectureKinds.Length, "Snapshot brain architecture count");
    for (var brainId = 0; brainId < original.State.Brains.Count; brainId++)
    {
        AssertEqual(
            original.State.GetBrainArchitectureKind(brainId),
            restored.State.GetBrainArchitectureKind(brainId),
            $"Restored brain {brainId} architecture");
    }

    for (var i = 0; i < original.State.Creatures.Count; i++)
    {
        var expected = original.State.Creatures[i];
        var actual = restored.State.Creatures[i];
        AssertEqual(expected.Id, actual.Id, $"Creature {i} id");
        AssertEqual(expected.ParentId, actual.ParentId, $"Creature {i} parent");
        AssertClose(expected.Position.X, actual.Position.X, 0.000001, $"Creature {i} x");
        AssertClose(expected.Position.Y, actual.Position.Y, 0.000001, $"Creature {i} y");
        AssertClose(expected.Energy, actual.Energy, 0.000001, $"Creature {i} energy");
        AssertClose(expected.BirthInvestmentRatio, actual.BirthInvestmentRatio, 0.000001, $"Creature {i} birth investment");
        AssertClose(expected.HeadingRadians, actual.HeadingRadians, 0.000001, $"Creature {i} heading");
        AssertClose(expected.LastDistanceTraveled, actual.LastDistanceTraveled, 0.000001, $"Creature {i} last distance");
        AssertEqual(expected.LastMovementBlocked, actual.LastMovementBlocked, $"Creature {i} blocked movement");
        AssertClose(expected.DistanceSinceLastMeal, actual.DistanceSinceLastMeal, 0.000001, $"Creature {i} meal distance");
        AssertEqual(expected.LastDamagingCreatureId, actual.LastDamagingCreatureId, $"Creature {i} last damaging creature");
        AssertEqual(expected.GenomeId, actual.GenomeId, $"Creature {i} genome");
        AssertEqual(expected.BrainId, actual.BrainId, $"Creature {i} brain");
    }

    for (var i = 0; i < original.State.Eggs.Count; i++)
    {
        var expected = original.State.Eggs[i];
        var actual = restored.State.Eggs[i];
        AssertEqual(expected.Id, actual.Id, $"Egg {i} id");
        AssertEqual(expected.ParentId, actual.ParentId, $"Egg {i} parent");
        AssertClose(expected.Position.X, actual.Position.X, 0.000001, $"Egg {i} x");
        AssertClose(expected.Position.Y, actual.Position.Y, 0.000001, $"Egg {i} y");
        AssertClose(expected.AgeSeconds, actual.AgeSeconds, 0.000001, $"Egg {i} age");
        AssertClose(expected.Energy, actual.Energy, 0.000001, $"Egg {i} energy");
        AssertClose(expected.Health, actual.Health, 0.000001, $"Egg {i} health");
        AssertClose(expected.MaxHealth, actual.MaxHealth, 0.000001, $"Egg {i} max health");
        AssertEqual(expected.PendingDeathReason, actual.PendingDeathReason, $"Egg {i} pending death reason");
        AssertClose(expected.InvestmentRatio, actual.InvestmentRatio, 0.000001, $"Egg {i} investment");
        AssertClose(expected.IncubationSeconds, actual.IncubationSeconds, 0.000001, $"Egg {i} incubation");
        AssertEqual(expected.GenomeId, actual.GenomeId, $"Egg {i} genome");
        AssertEqual(expected.BrainId, actual.BrainId, $"Egg {i} brain");
    }

    for (var i = 0; i < original.State.Resources.Count; i++)
    {
        var expected = original.State.Resources[i];
        var actual = restored.State.Resources[i];
        AssertEqual(expected.Id, actual.Id, $"Resource {i} id");
        AssertClose(expected.Position.X, actual.Position.X, 0.000001, $"Resource {i} x");
        AssertClose(expected.Position.Y, actual.Position.Y, 0.000001, $"Resource {i} y");
        AssertClose(expected.Calories, actual.Calories, 0.000001, $"Resource {i} calories");
        AssertEqual(expected.Kind, actual.Kind, $"Resource {i} kind");
        AssertClose(expected.DecayCaloriesPerSecond, actual.DecayCaloriesPerSecond, 0.000001, $"Resource {i} decay");
        AssertClose(expected.RegrowthCaloriesPerSecond, actual.RegrowthCaloriesPerSecond, 0.000001, $"Resource {i} regrowth");
        AssertEqual(expected.FreshKillAttackerId, actual.FreshKillAttackerId, $"Resource {i} fresh-kill attacker");
        AssertEqual(expected.FreshKillPreyId, actual.FreshKillPreyId, $"Resource {i} fresh-kill prey");
        AssertClose(expected.FreshKillSecondsRemaining, actual.FreshKillSecondsRemaining, 0.000001, $"Resource {i} fresh-kill timer");
    }
}

static void SimulationSnapshotCaptureCanSampleStatsHistory()
{
    var scenario = new SimulationScenario
    {
        StatsSnapshotIntervalTicks = 1
    };
    var simulation = new Simulation(
        new SimulationConfig { FixedDeltaSeconds = 1f },
        seed: 34,
        systems: [new StatsRecordingSystem()]);

    simulation.RunSteps(10);

    var full = SimulationSnapshot.Capture(scenario, simulation);
    AssertEqual(10, full.StatsSnapshots.Length, "Full snapshot history count");
    AssertEqual(0L, full.StatsSnapshots[0].Tick, "Full snapshot first tick");
    AssertEqual(9L, full.StatsSnapshots[^1].Tick, "Full snapshot final tick");

    var sampled = SimulationSnapshot.Capture(scenario, simulation, maxStatsSnapshots: 4);
    AssertEqual(4, sampled.StatsSnapshots.Length, "Sampled snapshot history count");
    AssertEqual(0L, sampled.StatsSnapshots[0].Tick, "Sampled snapshot first tick");
    AssertEqual(9L, sampled.StatsSnapshots[^1].Tick, "Sampled snapshot final tick");
    AssertTrue(
        sampled.StatsSnapshots[1].Tick > sampled.StatsSnapshots[0].Tick
        && sampled.StatsSnapshots[2].Tick > sampled.StatsSnapshots[1].Tick,
        "Sampled snapshots should preserve chronological order");

    var finalOnly = SimulationSnapshot.Capture(scenario, simulation, maxStatsSnapshots: 1);
    AssertEqual(1, finalOnly.StatsSnapshots.Length, "Final-only snapshot history count");
    AssertEqual(9L, finalOnly.StatsSnapshots[0].Tick, "Final-only snapshot tick");

    var none = SimulationSnapshot.Capture(scenario, simulation, maxStatsSnapshots: 0);
    AssertEqual(0, none.StatsSnapshots.Length, "Empty snapshot history count");
}

static void ScenarioPressureKnobsSeedStartingGenome()
{
    var scenario = new SimulationScenario
    {
        Seed = 15,
        PipelineKind = SimulationPipelineKind.SimpleForaging,
        EnableBiomes = false,
        FixedDeltaSeconds = 1f,
        InitialCreatureCount = 1,
        InitialResourcesPerMillionArea = 0f,
        InitialCreatureEnergyMin = 10f,
        InitialCreatureEnergyMax = 10f,
        BasalEnergyPerSecond = 0.75f,
        BodyRadiusEnergyCostPerSecond = 0.2f,
        MaxSpeedEnergyCostPerSecond = 0.01f,
        TurnRateEnergyCostPerSecond = 0.02f,
        SenseRadiusEnergyCostPerSecond = 0.001f,
        VisionAngleRadians = MathF.PI / 2f,
        VisionAngleEnergyCostPerSecond = 0.03f,
        EatRateEnergyCostPerSecond = 0.004f,
        GutCapacityEnergyCostPerSecond = 0.007f,
        DigestionRateEnergyCostPerSecond = 0.008f,
        BiteStrengthEnergyCostPerSecond = 0.05f,
        DamageResistanceEnergyCostPerSecond = 0.02f,
        PlantSpecializationEnergyCostPerSecond = 0f,
        EggEnergyCostPerSecond = 0.02f,
        EggEnvironmentalDamagePerSecond = 0.04f,
        MovementEnergyPerSecond = 1.25f,
        EatCaloriesPerSecond = 9.5f,
        GutCapacityCalories = 40f,
        DigestionCaloriesPerSecond = 11f,
        EggProductionEnergyPerSecond = 4.5f,
        EggIncubationSeconds = 7f,
        MaturityAgeSeconds = 0f,
        DietaryAdaptation = 0.25f,
        CarrionAdaptation = 0.35f,
        TenderPlantAdaptation = 0.2f,
        RichPlantAdaptation = 0.3f,
        ToughPlantAdaptation = 0.4f,
        BiteStrength = 0.75f,
        DamageResistance = 1.25f,
        DeathMeatCaloriesPerBodyRadius = 5f,
        DeathMeatEnergyFraction = 0.4f,
        MeatDecayCaloriesPerSecond = 0.08f,
        RottenMeatDamagePerRawKcal = 0.005f,
        MeatScentRangeMultiplier = 4f,
        MeatScentCaloriesForFullStrength = 70f,
        MeatScentDensitySaturation = 1.5f,
        BiteDamagePerSecond = 0.4f,
        BiteEnergyCostPerSecond = 0.12f,
        BiteRangePadding = 1.5f,
        MutationStrength = 0.03f,
        TraitMutationRate = 0.3f,
        BrainMutationRate = 0.11f,
        StatsSnapshotIntervalTicks = 1
    };

    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var genome = simulation.State.Genomes[0];

    AssertClose(0.75f, genome.BasalEnergyPerSecond, 0.000001, "Seeded basal energy");
    AssertClose(0.01f, scenario.MaxSpeedEnergyCostPerSecond, 0.000001, "Scenario max-speed energy");
    AssertClose(0.02f, scenario.TurnRateEnergyCostPerSecond, 0.000001, "Scenario turn-rate energy");
    AssertClose(0.001f, scenario.SenseRadiusEnergyCostPerSecond, 0.000001, "Scenario sense-radius energy");
    AssertClose(MathF.PI / 2f, genome.VisionAngleRadians, 0.000001, "Seeded vision angle");
    AssertClose(0.03f, scenario.VisionAngleEnergyCostPerSecond, 0.000001, "Scenario vision-angle energy");
    AssertClose(0.004f, scenario.EatRateEnergyCostPerSecond, 0.000001, "Scenario eat-rate energy");
    AssertClose(0.007f, scenario.GutCapacityEnergyCostPerSecond, 0.000001, "Scenario gut-capacity energy");
    AssertClose(0.008f, scenario.DigestionRateEnergyCostPerSecond, 0.000001, "Scenario digestion-rate energy");
    AssertClose(0.05f, scenario.BiteStrengthEnergyCostPerSecond, 0.000001, "Scenario bite-strength energy");
    AssertClose(0.02f, scenario.DamageResistanceEnergyCostPerSecond, 0.000001, "Scenario damage-resistance energy");
    AssertClose(0f, scenario.PlantSpecializationEnergyCostPerSecond, 0.000001, "Scenario plant specialization energy");
    AssertClose(0.02f, scenario.EggEnergyCostPerSecond, 0.000001, "Scenario egg energy");
    AssertClose(0.04f, scenario.EggEnvironmentalDamagePerSecond, 0.000001, "Scenario egg environmental damage");
    AssertClose(1.25f, genome.MovementEnergyPerSecond, 0.000001, "Seeded movement energy");
    AssertClose(1.6f, scenario.MovementSpeedCostExponent, 0.000001, "Scenario movement speed cost exponent");
    AssertClose(9.5f, genome.EatCaloriesPerSecond, 0.000001, "Seeded eat rate");
    AssertClose(40f, genome.GutCapacityCalories, 0.000001, "Seeded gut capacity");
    AssertClose(11f, genome.DigestionCaloriesPerSecond, 0.000001, "Seeded digestion rate");
    AssertClose(4.5f, genome.EggProductionEnergyPerSecond, 0.000001, "Seeded egg production");
    AssertClose(7f, genome.EggIncubationSeconds, 0.000001, "Seeded egg incubation");
    AssertClose(0f, genome.MaturityAgeSeconds, 0.000001, "Seeded maturity age");
    AssertClose(0.25f, genome.DietaryAdaptation, 0.000001, "Seeded dietary adaptation");
    AssertClose(0.35f, genome.CarrionAdaptation, 0.000001, "Seeded carrion adaptation");
    AssertClose(0.2f, genome.TenderPlantAdaptation, 0.000001, "Seeded tender plant adaptation");
    AssertClose(0.3f, genome.RichPlantAdaptation, 0.000001, "Seeded rich plant adaptation");
    AssertClose(0.4f, genome.ToughPlantAdaptation, 0.000001, "Seeded tough plant adaptation");
    AssertClose(0.75f, genome.BiteStrength, 0.000001, "Seeded bite strength");
    AssertClose(1.25f, genome.DamageResistance, 0.000001, "Seeded damage resistance");
    AssertClose(5f, scenario.DeathMeatCaloriesPerBodyRadius, 0.000001, "Scenario death meat body calories");
    AssertClose(0.4f, scenario.DeathMeatEnergyFraction, 0.000001, "Scenario death meat energy fraction");
    AssertClose(0.08f, scenario.MeatDecayCaloriesPerSecond, 0.000001, "Scenario meat decay");
    AssertClose(0.005f, scenario.RottenMeatDamagePerRawKcal, 0.000001, "Scenario rotten meat damage");
    AssertClose(4f, scenario.MeatScentRangeMultiplier, 0.000001, "Scenario meat scent range");
    AssertClose(70f, scenario.MeatScentCaloriesForFullStrength, 0.000001, "Scenario meat scent calorie scale");
    AssertClose(1.5f, scenario.MeatScentDensitySaturation, 0.000001, "Scenario meat scent saturation");
    AssertClose(0.4f, scenario.BiteDamagePerSecond, 0.000001, "Scenario bite damage");
    AssertClose(0.12f, scenario.BiteEnergyCostPerSecond, 0.000001, "Scenario bite energy cost");
    AssertClose(1.5f, scenario.BiteRangePadding, 0.000001, "Scenario bite reach");
    AssertClose(0.03f, genome.MutationStrength, 0.000001, "Seeded mutation strength");
    AssertClose(0.3f, genome.TraitMutationRate, 0.000001, "Seeded trait mutation rate");
    AssertClose(0.11f, genome.BrainMutationRate, 0.000001, "Seeded brain mutation rate");

    simulation.Step();

    AssertClose(7.608353f, simulation.State.Creatures[0].Energy, 0.00001, "Scenario energy pressure");
}

static void ScenarioJsonMigratesLegacyResourceCount()
{
    var json =
        """
        {
          "name": "Legacy Resources",
          "worldWidth": 1000,
          "worldHeight": 700,
          "randomizeInitialBrainWeights": true,
          "initialResourceCount": 140
        }
        """;

    var scenario = SimulationScenarioJson.FromJson(json);

    AssertClose(200f, scenario.InitialResourcesPerMillionArea, 0.0001, "Migrated resource density");
    AssertEqual(140, scenario.CalculateInitialResourceCount(), "Migrated resource count");
    AssertEqual(InitialBrainKind.RandomPerFounder, scenario.InitialBrainKind, "Migrated legacy random brain mode");
}

static void BiomeJsonMigratesLegacyNames()
{
    var options = new JsonSerializerOptions();
    options.Converters.Add(new BiomeKindJsonConverter());

    AssertEqual(BiomeKind.Desert, JsonSerializer.Deserialize<BiomeKind>("\"barren\"", options), "Legacy barren biome");
    AssertEqual(BiomeKind.Scrubland, JsonSerializer.Deserialize<BiomeKind>("\"sparse\"", options), "Legacy sparse biome");
    AssertEqual(BiomeKind.Fertile, JsonSerializer.Deserialize<BiomeKind>("\"rich\"", options), "Legacy rich biome");
    AssertEqual("\"desert\"", JsonSerializer.Serialize(BiomeKind.Barren, options), "Legacy barren writes canonical desert");
    AssertEqual("\"scrubland\"", JsonSerializer.Serialize(BiomeKind.Sparse, options), "Legacy sparse writes canonical scrubland");
    AssertEqual("\"fertile\"", JsonSerializer.Serialize(BiomeKind.Rich, options), "Legacy rich writes canonical fertile");
}

static void ScenarioMetadataDescribesEditableJsonFields()
{
    var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    var scenarioProperties = typeof(SimulationScenario)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
        .Where(property => property.GetMethod is not null)
        .Select(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
            ?? jsonOptions.PropertyNamingPolicy!.ConvertName(property.Name))
        .ToArray();

    AssertEqual(scenarioProperties.Length, SimulationScenarioMetadata.Fields.Count, "Scenario metadata field count");
    AssertEqual(
        scenarioProperties.Length,
        SimulationScenarioMetadata.Fields.Select(field => field.JsonName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
        "Scenario metadata unique JSON names");

    foreach (var jsonName in scenarioProperties)
    {
        AssertTrue(
            SimulationScenarioMetadata.FindByJsonName(jsonName) is not null,
            $"Scenario metadata contains {jsonName}");
    }

    var brain = SimulationScenarioMetadata.FindByJsonName("brainArchitectureKind")
        ?? throw new InvalidOperationException("Missing brain architecture metadata.");
    AssertEqual("Brain & Vision", brain.Group, "Brain architecture group");
    AssertEqual("enum", brain.Type, "Brain architecture type");
    AssertTrue(brain.EnumValues.Contains("hybridNeural"), "Brain architecture enum values");

    var density = SimulationScenarioMetadata.FindByJsonName("initialResourcesPerMillionArea")
        ?? throw new InvalidOperationException("Missing resource density metadata.");
    AssertEqual("Plants", density.Group, "Resource density group");
    AssertEqual("per 1M area", density.Units, "Resource density units");
    AssertEqual(0d, density.Minimum, "Resource density minimum");

    var species = SimulationScenarioMetadata.FindByJsonName("speciesSeeds")
        ?? throw new InvalidOperationException("Missing species seed metadata.");
    AssertEqual("json", species.Type, "Species seeds type");
    AssertEqual("Species", species.Group, "Species seeds group");
    AssertTrue(species.Advanced, "Species seeds should be advanced");

    var neuralReuse = SimulationScenarioMetadata.FindByJsonName("reuseNeuralActionsOnSkippedWorldSenses")
        ?? throw new InvalidOperationException("Missing neural action reuse metadata.");
    AssertEqual("boolean", neuralReuse.Type, "Neural action reuse type");
    AssertEqual("Performance", neuralReuse.Group, "Neural action reuse group");
    AssertTrue(neuralReuse.Advanced, "Neural action reuse should be advanced");

    var sensingThreads = SimulationScenarioMetadata.FindByJsonName("sensingThreadCount")
        ?? throw new InvalidOperationException("Missing sensing thread metadata.");
    AssertEqual("number", sensingThreads.Type, "Sensing thread type");
    AssertEqual("Performance", sensingThreads.Group, "Sensing thread group");
    AssertEqual("threads", sensingThreads.Units, "Sensing thread units");
    AssertTrue(sensingThreads.Advanced, "Sensing thread count should be advanced");

    var neuralThreads = SimulationScenarioMetadata.FindByJsonName("neuralControllerThreadCount")
        ?? throw new InvalidOperationException("Missing neural controller thread metadata.");
    AssertEqual("number", neuralThreads.Type, "Neural controller thread type");
    AssertEqual("Performance", neuralThreads.Group, "Neural controller thread group");
    AssertEqual("threads", neuralThreads.Units, "Neural controller thread units");
    AssertTrue(neuralThreads.Advanced, "Neural controller thread count should be advanced");

    var closeMinimum = SimulationScenarioMetadata.FindByJsonName("closeSenseRefreshMinimumTicks")
        ?? throw new InvalidOperationException("Missing close sense refresh minimum metadata.");
    AssertEqual("number", closeMinimum.Type, "Close sense refresh minimum type");
    AssertEqual("Brain & Vision", closeMinimum.Group, "Close sense refresh minimum group");
    AssertEqual("ticks", closeMinimum.Units, "Close sense refresh minimum units");
    AssertTrue(closeMinimum.Advanced, "Close sense refresh minimum should be advanced");
}

static void ScenarioJsonRoundTrips()
{
    var scenario = new SimulationScenario
    {
        Name = "Sparse Food",
        Seed = 1234,
        PipelineKind = SimulationPipelineKind.SimpleForaging,
        BrainArchitectureKind = BrainArchitectureKind.HiddenLayerNeural,
        InitialBrainKind = InitialBrainKind.ForagerPredator,
        BrainHiddenNodeCount = 16,
        EnableBiomes = false,
        BiomeMapKind = BiomeMapKind.HorizontalBands,
        WorldMapPath = "maps/user/painted.lineage-map.json",
        ManualBiomeMapPath = "maps/painted.json",
        EnableObstacles = true,
        ObstacleMapKind = ObstacleMapKind.ScatteredRocks,
        ManualObstacleMapPath = "maps/walls.json",
        ObstacleCellSize = 150f,
        BiomeCellSize = 250f,
        ResourceVoidBorderWidth = 25f,
        WorldWidth = 500f,
        WorldHeight = 300f,
        WorldSenseIntervalTicks = 6,
        CloseSenseRefreshProximity = 0.93f,
        CloseSenseRefreshMinimumTicks = 3,
        PlantPayoffTraceHalfLifeSeconds = 31f,
        SensingThreadCount = 3,
        EnableSectorVision = true,
        ReuseNeuralActionsOnSkippedWorldSenses = true,
        NeuralControllerThreadCount = 4,
        StatsSnapshotIntervalTicks = 12,
        InitialCreatureCount = 7,
        InitialCreatureSpawnRegion = InitialCreatureSpawnRegion.RightThird,
        SpeciesSeeds =
        [
            new SpeciesScenarioSeed
                {
                    ProfilePath = "species/alpha.species.json",
                    Count = 3,
                    SpawnRegion = InitialCreatureSpawnRegion.LeftThird,
                    EnergyOverride = 42f,
                    BrainOverrideKind = InitialBrainKind.ScavengerForager
                },
            new SpeciesScenarioSeed
            {
                ProfilePath = "species/disabled.species.json",
                Count = 2,
                SpawnRegion = InitialCreatureSpawnRegion.BottomThird,
                BrainProfilePath = "brains/disabled.brain.json",
                Enabled = false
            }
        ],
        InitialResourcesPerMillionArea = 37.5f,
        GenericPlantWeight = 0.25f,
        TenderPlantWeight = 0.2f,
        RichPlantWeight = 0.35f,
        ToughPlantWeight = 0.2f,
        EnablePlantTypeHabitatAffinity = true,
        PlantRespawnDelaySecondsMin = 12f,
        PlantRespawnDelaySecondsMax = 34f,
        PlantLocalDispersalChance = 0.27f,
        PlantLocalDispersalRadius = 188f,
        EnableLocalFertility = true,
        LocalFertilityCellSize = 175f,
        LocalFertilityMinimumMultiplier = 0.31f,
        LocalFertilityRecoveryPerSecond = 0.0007f,
        LocalFertilityDepletionPerPlant = 0.11f,
        LocalFertilityNeighborDepletionShare = 0.42f,
        EnableSeasons = true,
        SeasonLengthSeconds = 480f,
        SeasonFertilityAmplitude = 0.45f,
        SeasonPhaseOffsetSeconds = 120f,
        SeasonPhaseMode = SeasonPhaseMode.HorizontalOpposed,
        BarrenBiomeSeasonalAmplitudeMultiplier = 0.4f,
        SparseBiomeSeasonalAmplitudeMultiplier = 0.8f,
        GrasslandBiomeSeasonalAmplitudeMultiplier = 1.1f,
        RichBiomeSeasonalAmplitudeMultiplier = 1.4f,
        ForestBiomeSeasonalAmplitudeMultiplier = 1.05f,
        WetlandBiomeSeasonalAmplitudeMultiplier = 1.15f,
        ResourceClusterStrength = 0.33f,
        ResourceClusterRadius = 123f,
        BarrenBiomeMovementCostMultiplier = 1.4f,
        SparseBiomeMovementCostMultiplier = 1.2f,
        GrasslandBiomeMovementCostMultiplier = 1.05f,
        RichBiomeMovementCostMultiplier = 0.85f,
        ForestBiomeMovementCostMultiplier = 1.15f,
        WetlandBiomeMovementCostMultiplier = 1.45f,
        BarrenBiomeSpeedMultiplier = 0.7f,
        SparseBiomeSpeedMultiplier = 0.85f,
        GrasslandBiomeSpeedMultiplier = 1f,
        RichBiomeSpeedMultiplier = 1.05f,
        ForestBiomeSpeedMultiplier = 0.82f,
        WetlandBiomeSpeedMultiplier = 0.62f,
        BarrenBiomeVisionRangeMultiplier = 1.08f,
        SparseBiomeVisionRangeMultiplier = 0.95f,
        GrasslandBiomeVisionRangeMultiplier = 1.02f,
        RichBiomeVisionRangeMultiplier = 1.06f,
        ForestBiomeVisionRangeMultiplier = 0.55f,
        WetlandBiomeVisionRangeMultiplier = 0.78f,
        BarrenBiomeBasalCostMultiplier = 1.3f,
        SparseBiomeBasalCostMultiplier = 1.1f,
        GrasslandBiomeBasalCostMultiplier = 1.02f,
        RichBiomeBasalCostMultiplier = 0.9f,
        ForestBiomeBasalCostMultiplier = 0.88f,
        WetlandBiomeBasalCostMultiplier = 1.08f,
        BasalEnergyPerSecond = 0.31f,
        BodyRadiusEnergyCostPerSecond = 0.04f,
        MaxSpeedEnergyCostPerSecond = 0.003f,
        TurnRateEnergyCostPerSecond = 0.012f,
        SenseRadiusEnergyCostPerSecond = 0.0003f,
        VisionAngleRadians = MathF.PI,
        VisionAngleEnergyCostPerSecond = 0.017f,
        EatRateEnergyCostPerSecond = 0.0025f,
        GutCapacityEnergyCostPerSecond = 0.0007f,
        DigestionRateEnergyCostPerSecond = 0.0065f,
        BiteStrengthEnergyCostPerSecond = 0.041f,
        DamageResistanceEnergyCostPerSecond = 0.032f,
        PlantSpecializationEnergyCostPerSecond = 0.019f,
        MemoryEnergyCostPerSecond = 0.021f,
        MemoryDecayPerSecond = 0.09f,
        MemoryWriteRatePerSecond = 1.75f,
        EggEnergyCostPerSecond = 0.023f,
        EggEnvironmentalDamagePerSecond = 0.037f,
        MovementEnergyPerSecond = 0.62f,
        MovementSpeedCostExponent = 1.75f,
        EatCaloriesPerSecond = 14f,
        GutCapacityCalories = 65f,
        DigestionCaloriesPerSecond = 16f,
        EggProductionEnergyPerSecond = 3.75f,
        EggIncubationSeconds = 19f,
        MaturityAgeSeconds = 33f,
        RequireReproductionIntent = false,
        ReproductivePrimeAgeSeconds = 180f,
        ReproductiveSenescenceAgeSeconds = 720f,
        SenescentFertilityMultiplier = 0.2f,
        CrowdingFertilityPenalty = 0.55f,
        DietaryAdaptation = 0.42f,
        CarrionAdaptation = 0.37f,
        TenderPlantAdaptation = 0.11f,
        RichPlantAdaptation = 0.22f,
        ToughPlantAdaptation = 0.33f,
        BiteStrength = 0.7f,
        DamageResistance = 1.4f,
        DeathMeatCaloriesPerBodyRadius = 3.5f,
        DeathMeatEnergyFraction = 0.25f,
        MeatDecayCaloriesPerSecond = 0.09f,
        RottenMeatDamagePerRawKcal = 0.006f,
        MeatScentRangeMultiplier = 2.5f,
        MeatScentCaloriesForFullStrength = 55f,
        MeatScentDensitySaturation = 1.75f,
        BiteDamagePerSecond = 0.44f,
        BiteEnergyCostPerSecond = 0.13f,
        BiteRangePadding = 1.75f,
        RelocateDepletedResources = false,
        MutationStrength = 0.02f,
        TraitMutationRate = 0.12f,
        BrainMutationRate = 0.04f
    };

    var json = SimulationScenarioJson.ToJson(scenario);
    var roundTripped = SimulationScenarioJson.FromJson(json);

    AssertTrue(json.Contains("\"pipelineKind\": \"simpleForaging\""), "JSON should serialize pipeline as a string");
    AssertTrue(json.Contains("\"brainArchitectureKind\": \"hiddenLayerNeural\""), "JSON should serialize brain architecture");
    AssertTrue(json.Contains("\"initialBrainKind\": \"foragerPredator\""), "JSON should serialize initial brain kind as a string");
    AssertTrue(json.Contains("\"brainHiddenNodeCount\": 16"), "JSON should serialize hidden brain nodes");
    AssertTrue(!json.Contains("randomizeInitialBrainWeights"), "JSON should not serialize legacy random brain flag");
    AssertTrue(json.Contains("\"biomeMapKind\": \"horizontalBands\""), "JSON should serialize biome map kind as a string");
    AssertTrue(json.Contains("\"worldMapPath\": \"maps/user/painted.lineage-map.json\""), "JSON should serialize world map path");
    AssertTrue(json.Contains("\"manualBiomeMapPath\": null"), "JSON should clear manual biome map path when world map path is present");
    AssertTrue(json.Contains("\"obstacleMapKind\": \"scatteredRocks\""), "JSON should serialize obstacle map kind as a string");
    AssertTrue(json.Contains("\"manualObstacleMapPath\": null"), "JSON should clear manual obstacle map path when world map path is present");
    AssertTrue(json.Contains("\"initialCreatureSpawnRegion\": \"rightThird\""), "JSON should serialize initial spawn region");
    AssertTrue(json.Contains("\"speciesSeeds\""), "JSON should serialize species seeds");
    AssertTrue(json.Contains("\"profilePath\": \"species/alpha.species.json\""), "JSON should serialize species seed profile paths");
    AssertTrue(json.Contains("\"spawnRegion\": \"leftThird\""), "JSON should serialize species seed spawn regions");
    AssertTrue(json.Contains("\"brainOverrideKind\": \"scavengerForager\""), "JSON should serialize species seed brain overrides");
    AssertTrue(json.Contains("\"brainProfilePath\": \"brains/disabled.brain.json\""), "JSON should serialize species seed brain profile paths");
    AssertTrue(json.Contains("\"initialResourcesPerMillionArea\""), "JSON should serialize resource density");
    AssertTrue(json.Contains("\"enablePlantTypeHabitatAffinity\""), "JSON should serialize plant habitat affinity");
    AssertTrue(json.Contains("\"plantRespawnDelaySecondsMin\""), "JSON should serialize plant respawn delay");
    AssertTrue(json.Contains("\"plantLocalDispersalChance\""), "JSON should serialize plant local dispersal chance");
    AssertTrue(json.Contains("\"plantLocalDispersalRadius\""), "JSON should serialize plant local dispersal radius");
    AssertTrue(json.Contains("\"enableLocalFertility\""), "JSON should serialize local fertility toggle");
    AssertTrue(json.Contains("\"localFertilityDepletionPerPlant\""), "JSON should serialize local fertility depletion");
    AssertTrue(json.Contains("\"enableSeasons\""), "JSON should serialize season toggle");
    AssertTrue(json.Contains("\"seasonFertilityAmplitude\""), "JSON should serialize season fertility");
    AssertTrue(json.Contains("\"seasonPhaseMode\": \"horizontalOpposed\""), "JSON should serialize season phase mode");
    AssertTrue(json.Contains("\"barrenBiomeSeasonalAmplitudeMultiplier\""), "JSON should serialize biome seasonal response");
    AssertTrue(json.Contains("\"resourceClusterStrength\""), "JSON should serialize resource clustering");
    AssertTrue(json.Contains("\"barrenBiomeMovementCostMultiplier\""), "JSON should serialize biome movement cost");
    AssertTrue(json.Contains("\"barrenBiomeSpeedMultiplier\""), "JSON should serialize biome speed");
    AssertTrue(json.Contains("\"barrenBiomeVisionRangeMultiplier\""), "JSON should serialize biome vision range");
    AssertTrue(json.Contains("\"worldSenseIntervalTicks\""), "JSON should serialize world sense interval");
    AssertTrue(json.Contains("\"closeSenseRefreshProximity\""), "JSON should serialize close sense threshold");
    AssertTrue(json.Contains("\"closeSenseRefreshMinimumTicks\""), "JSON should serialize close sense refresh minimum");
    AssertTrue(json.Contains("\"plantPayoffTraceHalfLifeSeconds\""), "JSON should serialize plant payoff trace half-life");
    AssertTrue(json.Contains("\"sensingThreadCount\""), "JSON should serialize sensing thread count");
    AssertTrue(json.Contains("\"enableSectorVision\""), "JSON should serialize sector vision toggle");
    AssertTrue(json.Contains("\"reuseNeuralActionsOnSkippedWorldSenses\""), "JSON should serialize neural action reuse toggle");
    AssertTrue(json.Contains("\"neuralControllerThreadCount\""), "JSON should serialize neural controller thread count");
    AssertTrue(json.Contains("\"rottenMeatDamagePerRawKcal\""), "JSON should serialize rotten meat damage");
    AssertTrue(json.Contains("\"plantSpecializationEnergyCostPerSecond\""), "JSON should serialize plant specialization cost");
    AssertTrue(json.Contains("\"tenderPlantAdaptation\""), "JSON should serialize tender plant adaptation");
    AssertEqual(scenario.Name, roundTripped.Name, "Scenario name");
    AssertEqual(scenario.Seed, roundTripped.Seed, "Scenario seed");
    AssertEqual(scenario.PipelineKind, roundTripped.PipelineKind, "Scenario pipeline kind");
    AssertEqual(scenario.BrainArchitectureKind, roundTripped.BrainArchitectureKind, "Scenario brain architecture");
    AssertEqual(scenario.InitialBrainKind, roundTripped.InitialBrainKind, "Scenario initial brain mode");
    AssertEqual(scenario.BrainHiddenNodeCount, roundTripped.BrainHiddenNodeCount, "Scenario brain hidden nodes");
    AssertEqual(scenario.EnableBiomes, roundTripped.EnableBiomes, "Scenario biome mode");
    AssertEqual(scenario.BiomeMapKind, roundTripped.BiomeMapKind, "Scenario biome map kind");
    AssertEqual(scenario.WorldMapPath, roundTripped.WorldMapPath, "Scenario world map path");
    AssertEqual(null, roundTripped.ManualBiomeMapPath, "Scenario manual biome map path cleared by world map");
    AssertEqual(scenario.EnableObstacles, roundTripped.EnableObstacles, "Scenario obstacle mode");
    AssertEqual(scenario.ObstacleMapKind, roundTripped.ObstacleMapKind, "Scenario obstacle map kind");
    AssertEqual(null, roundTripped.ManualObstacleMapPath, "Scenario manual obstacle map path cleared by world map");
    AssertClose(scenario.ObstacleCellSize, roundTripped.ObstacleCellSize, 0.000001, "Scenario obstacle cell size");
    AssertClose(scenario.BiomeCellSize, roundTripped.BiomeCellSize, 0.000001, "Scenario biome cell size");
    AssertClose(scenario.ResourceVoidBorderWidth, roundTripped.ResourceVoidBorderWidth, 0.000001, "Scenario resource void border");
    AssertClose(scenario.WorldWidth, roundTripped.WorldWidth, 0.000001, "Scenario world width");
    AssertClose(scenario.WorldHeight, roundTripped.WorldHeight, 0.000001, "Scenario world height");
    AssertEqual(scenario.WorldSenseIntervalTicks, roundTripped.WorldSenseIntervalTicks, "Scenario world sense interval");
    AssertClose(scenario.CloseSenseRefreshProximity, roundTripped.CloseSenseRefreshProximity, 0.000001, "Scenario close sense threshold");
    AssertEqual(scenario.CloseSenseRefreshMinimumTicks, roundTripped.CloseSenseRefreshMinimumTicks, "Scenario close sense refresh minimum");
    AssertClose(scenario.PlantPayoffTraceHalfLifeSeconds, roundTripped.PlantPayoffTraceHalfLifeSeconds, 0.000001, "Scenario plant payoff trace half-life");
    AssertEqual(scenario.SensingThreadCount, roundTripped.SensingThreadCount, "Scenario sensing thread count");
    AssertEqual(scenario.EnableSectorVision, roundTripped.EnableSectorVision, "Scenario sector vision toggle");
    AssertEqual(scenario.ReuseNeuralActionsOnSkippedWorldSenses, roundTripped.ReuseNeuralActionsOnSkippedWorldSenses, "Scenario neural action reuse toggle");
    AssertEqual(scenario.NeuralControllerThreadCount, roundTripped.NeuralControllerThreadCount, "Scenario neural controller thread count");
    AssertEqual(scenario.StatsSnapshotIntervalTicks, roundTripped.StatsSnapshotIntervalTicks, "Scenario snapshot interval");
    AssertEqual(scenario.InitialCreatureCount, roundTripped.InitialCreatureCount, "Scenario creature count");
    AssertEqual(scenario.InitialCreatureSpawnRegion, roundTripped.InitialCreatureSpawnRegion, "Scenario initial spawn region");
    AssertEqual(2, roundTripped.SpeciesSeeds.Length, "Scenario species seed count");
    AssertEqual("species/alpha.species.json", roundTripped.SpeciesSeeds[0].ProfilePath, "Scenario species seed profile");
    AssertEqual(3, roundTripped.SpeciesSeeds[0].Count, "Scenario species seed creature count");
    AssertEqual(InitialCreatureSpawnRegion.LeftThird, roundTripped.SpeciesSeeds[0].SpawnRegion, "Scenario species seed spawn region");
    AssertClose(42f, roundTripped.SpeciesSeeds[0].EnergyOverride ?? 0f, 0.000001, "Scenario species seed energy override");
    AssertEqual(InitialBrainKind.ScavengerForager, roundTripped.SpeciesSeeds[0].BrainOverrideKind, "Scenario species seed brain override");
    AssertEqual("brains/disabled.brain.json", roundTripped.SpeciesSeeds[1].BrainProfilePath, "Scenario species seed brain profile path");
    AssertTrue(!roundTripped.SpeciesSeeds[1].Enabled, "Scenario disabled species seed");
    AssertClose(scenario.InitialResourcesPerMillionArea, roundTripped.InitialResourcesPerMillionArea, 0.000001, "Scenario resource density");
    AssertClose(scenario.GenericPlantWeight, roundTripped.GenericPlantWeight, 0.000001, "Scenario generic plant weight");
    AssertClose(scenario.TenderPlantWeight, roundTripped.TenderPlantWeight, 0.000001, "Scenario tender plant weight");
    AssertClose(scenario.RichPlantWeight, roundTripped.RichPlantWeight, 0.000001, "Scenario rich plant weight");
    AssertClose(scenario.ToughPlantWeight, roundTripped.ToughPlantWeight, 0.000001, "Scenario tough plant weight");
    AssertEqual(scenario.EnablePlantTypeHabitatAffinity, roundTripped.EnablePlantTypeHabitatAffinity, "Scenario plant habitat affinity");
    AssertClose(scenario.PlantRespawnDelaySecondsMin, roundTripped.PlantRespawnDelaySecondsMin, 0.000001, "Scenario plant respawn min delay");
    AssertClose(scenario.PlantRespawnDelaySecondsMax, roundTripped.PlantRespawnDelaySecondsMax, 0.000001, "Scenario plant respawn max delay");
    AssertClose(scenario.PlantLocalDispersalChance, roundTripped.PlantLocalDispersalChance, 0.000001, "Scenario plant local dispersal chance");
    AssertClose(scenario.PlantLocalDispersalRadius, roundTripped.PlantLocalDispersalRadius, 0.000001, "Scenario plant local dispersal radius");
    AssertEqual(scenario.EnableLocalFertility, roundTripped.EnableLocalFertility, "Scenario local fertility toggle");
    AssertClose(scenario.LocalFertilityCellSize, roundTripped.LocalFertilityCellSize, 0.000001, "Scenario local fertility cell size");
    AssertClose(scenario.LocalFertilityMinimumMultiplier, roundTripped.LocalFertilityMinimumMultiplier, 0.000001, "Scenario local fertility minimum");
    AssertClose(scenario.LocalFertilityRecoveryPerSecond, roundTripped.LocalFertilityRecoveryPerSecond, 0.000001, "Scenario local fertility recovery");
    AssertClose(scenario.LocalFertilityDepletionPerPlant, roundTripped.LocalFertilityDepletionPerPlant, 0.000001, "Scenario local fertility depletion");
    AssertClose(scenario.LocalFertilityNeighborDepletionShare, roundTripped.LocalFertilityNeighborDepletionShare, 0.000001, "Scenario local fertility neighbor share");
    AssertEqual(scenario.EnableSeasons, roundTripped.EnableSeasons, "Scenario season toggle");
    AssertClose(scenario.SeasonLengthSeconds, roundTripped.SeasonLengthSeconds, 0.000001, "Scenario season length");
    AssertClose(scenario.SeasonFertilityAmplitude, roundTripped.SeasonFertilityAmplitude, 0.000001, "Scenario season fertility amplitude");
    AssertClose(scenario.SeasonPhaseOffsetSeconds, roundTripped.SeasonPhaseOffsetSeconds, 0.000001, "Scenario season phase offset");
    AssertEqual(scenario.SeasonPhaseMode, roundTripped.SeasonPhaseMode, "Scenario season phase mode");
    AssertClose(scenario.BarrenBiomeSeasonalAmplitudeMultiplier, roundTripped.BarrenBiomeSeasonalAmplitudeMultiplier, 0.000001, "Scenario barren biome seasonal response");
    AssertClose(scenario.SparseBiomeSeasonalAmplitudeMultiplier, roundTripped.SparseBiomeSeasonalAmplitudeMultiplier, 0.000001, "Scenario sparse biome seasonal response");
    AssertClose(scenario.GrasslandBiomeSeasonalAmplitudeMultiplier, roundTripped.GrasslandBiomeSeasonalAmplitudeMultiplier, 0.000001, "Scenario grassland biome seasonal response");
    AssertClose(scenario.RichBiomeSeasonalAmplitudeMultiplier, roundTripped.RichBiomeSeasonalAmplitudeMultiplier, 0.000001, "Scenario rich biome seasonal response");
    AssertClose(scenario.ForestBiomeSeasonalAmplitudeMultiplier, roundTripped.ForestBiomeSeasonalAmplitudeMultiplier, 0.000001, "Scenario forest biome seasonal response");
    AssertClose(scenario.WetlandBiomeSeasonalAmplitudeMultiplier, roundTripped.WetlandBiomeSeasonalAmplitudeMultiplier, 0.000001, "Scenario wetland biome seasonal response");
    AssertClose(scenario.ResourceClusterStrength, roundTripped.ResourceClusterStrength, 0.000001, "Scenario resource cluster strength");
    AssertClose(scenario.ResourceClusterRadius, roundTripped.ResourceClusterRadius, 0.000001, "Scenario resource cluster radius");
    AssertClose(scenario.BarrenBiomeMovementCostMultiplier, roundTripped.BarrenBiomeMovementCostMultiplier, 0.000001, "Scenario barren movement biome cost");
    AssertClose(scenario.SparseBiomeMovementCostMultiplier, roundTripped.SparseBiomeMovementCostMultiplier, 0.000001, "Scenario sparse movement biome cost");
    AssertClose(scenario.GrasslandBiomeMovementCostMultiplier, roundTripped.GrasslandBiomeMovementCostMultiplier, 0.000001, "Scenario grassland movement biome cost");
    AssertClose(scenario.RichBiomeMovementCostMultiplier, roundTripped.RichBiomeMovementCostMultiplier, 0.000001, "Scenario rich movement biome cost");
    AssertClose(scenario.ForestBiomeMovementCostMultiplier, roundTripped.ForestBiomeMovementCostMultiplier, 0.000001, "Scenario forest movement biome cost");
    AssertClose(scenario.WetlandBiomeMovementCostMultiplier, roundTripped.WetlandBiomeMovementCostMultiplier, 0.000001, "Scenario wetland movement biome cost");
    AssertClose(scenario.BarrenBiomeSpeedMultiplier, roundTripped.BarrenBiomeSpeedMultiplier, 0.000001, "Scenario barren biome speed");
    AssertClose(scenario.SparseBiomeSpeedMultiplier, roundTripped.SparseBiomeSpeedMultiplier, 0.000001, "Scenario sparse biome speed");
    AssertClose(scenario.GrasslandBiomeSpeedMultiplier, roundTripped.GrasslandBiomeSpeedMultiplier, 0.000001, "Scenario grassland biome speed");
    AssertClose(scenario.RichBiomeSpeedMultiplier, roundTripped.RichBiomeSpeedMultiplier, 0.000001, "Scenario rich biome speed");
    AssertClose(scenario.ForestBiomeSpeedMultiplier, roundTripped.ForestBiomeSpeedMultiplier, 0.000001, "Scenario forest biome speed");
    AssertClose(scenario.WetlandBiomeSpeedMultiplier, roundTripped.WetlandBiomeSpeedMultiplier, 0.000001, "Scenario wetland biome speed");
    AssertClose(scenario.BarrenBiomeVisionRangeMultiplier, roundTripped.BarrenBiomeVisionRangeMultiplier, 0.000001, "Scenario barren biome vision range");
    AssertClose(scenario.SparseBiomeVisionRangeMultiplier, roundTripped.SparseBiomeVisionRangeMultiplier, 0.000001, "Scenario sparse biome vision range");
    AssertClose(scenario.GrasslandBiomeVisionRangeMultiplier, roundTripped.GrasslandBiomeVisionRangeMultiplier, 0.000001, "Scenario grassland biome vision range");
    AssertClose(scenario.RichBiomeVisionRangeMultiplier, roundTripped.RichBiomeVisionRangeMultiplier, 0.000001, "Scenario rich biome vision range");
    AssertClose(scenario.ForestBiomeVisionRangeMultiplier, roundTripped.ForestBiomeVisionRangeMultiplier, 0.000001, "Scenario forest biome vision range");
    AssertClose(scenario.WetlandBiomeVisionRangeMultiplier, roundTripped.WetlandBiomeVisionRangeMultiplier, 0.000001, "Scenario wetland biome vision range");
    AssertClose(scenario.BarrenBiomeBasalCostMultiplier, roundTripped.BarrenBiomeBasalCostMultiplier, 0.000001, "Scenario barren basal biome cost");
    AssertClose(scenario.SparseBiomeBasalCostMultiplier, roundTripped.SparseBiomeBasalCostMultiplier, 0.000001, "Scenario sparse basal biome cost");
    AssertClose(scenario.GrasslandBiomeBasalCostMultiplier, roundTripped.GrasslandBiomeBasalCostMultiplier, 0.000001, "Scenario grassland basal biome cost");
    AssertClose(scenario.RichBiomeBasalCostMultiplier, roundTripped.RichBiomeBasalCostMultiplier, 0.000001, "Scenario rich basal biome cost");
    AssertClose(scenario.ForestBiomeBasalCostMultiplier, roundTripped.ForestBiomeBasalCostMultiplier, 0.000001, "Scenario forest basal biome cost");
    AssertClose(scenario.WetlandBiomeBasalCostMultiplier, roundTripped.WetlandBiomeBasalCostMultiplier, 0.000001, "Scenario wetland basal biome cost");
    AssertClose(scenario.BasalEnergyPerSecond, roundTripped.BasalEnergyPerSecond, 0.000001, "Scenario basal energy");
    AssertClose(scenario.BodyRadiusEnergyCostPerSecond, roundTripped.BodyRadiusEnergyCostPerSecond, 0.000001, "Scenario body-size energy");
    AssertClose(scenario.MaxSpeedEnergyCostPerSecond, roundTripped.MaxSpeedEnergyCostPerSecond, 0.000001, "Scenario max-speed energy");
    AssertClose(scenario.TurnRateEnergyCostPerSecond, roundTripped.TurnRateEnergyCostPerSecond, 0.000001, "Scenario turn-rate energy");
    AssertClose(scenario.SenseRadiusEnergyCostPerSecond, roundTripped.SenseRadiusEnergyCostPerSecond, 0.000001, "Scenario sense-radius energy");
    AssertClose(scenario.VisionAngleRadians, roundTripped.VisionAngleRadians, 0.000001, "Scenario vision angle");
    AssertClose(scenario.VisionAngleEnergyCostPerSecond, roundTripped.VisionAngleEnergyCostPerSecond, 0.000001, "Scenario vision-angle energy");
    AssertClose(scenario.EatRateEnergyCostPerSecond, roundTripped.EatRateEnergyCostPerSecond, 0.000001, "Scenario eat-rate energy");
    AssertClose(scenario.GutCapacityEnergyCostPerSecond, roundTripped.GutCapacityEnergyCostPerSecond, 0.000001, "Scenario gut-capacity energy");
    AssertClose(scenario.DigestionRateEnergyCostPerSecond, roundTripped.DigestionRateEnergyCostPerSecond, 0.000001, "Scenario digestion-rate energy");
    AssertClose(scenario.BiteStrengthEnergyCostPerSecond, roundTripped.BiteStrengthEnergyCostPerSecond, 0.000001, "Scenario bite-strength energy");
    AssertClose(scenario.DamageResistanceEnergyCostPerSecond, roundTripped.DamageResistanceEnergyCostPerSecond, 0.000001, "Scenario damage-resistance energy");
    AssertClose(scenario.PlantSpecializationEnergyCostPerSecond, roundTripped.PlantSpecializationEnergyCostPerSecond, 0.000001, "Scenario plant specialization energy");
    AssertClose(scenario.MemoryEnergyCostPerSecond, roundTripped.MemoryEnergyCostPerSecond, 0.000001, "Scenario memory energy");
    AssertClose(scenario.MemoryDecayPerSecond, roundTripped.MemoryDecayPerSecond, 0.000001, "Scenario memory decay");
    AssertClose(scenario.MemoryWriteRatePerSecond, roundTripped.MemoryWriteRatePerSecond, 0.000001, "Scenario memory write rate");
    AssertClose(scenario.EggEnergyCostPerSecond, roundTripped.EggEnergyCostPerSecond, 0.000001, "Scenario egg energy");
    AssertClose(scenario.EggEnvironmentalDamagePerSecond, roundTripped.EggEnvironmentalDamagePerSecond, 0.000001, "Scenario egg environmental damage");
    AssertClose(scenario.MovementEnergyPerSecond, roundTripped.MovementEnergyPerSecond, 0.000001, "Scenario movement energy");
    AssertClose(scenario.MovementSpeedCostExponent, roundTripped.MovementSpeedCostExponent, 0.000001, "Scenario movement speed cost exponent");
    AssertClose(scenario.EatCaloriesPerSecond, roundTripped.EatCaloriesPerSecond, 0.000001, "Scenario eat rate");
    AssertClose(scenario.GutCapacityCalories, roundTripped.GutCapacityCalories, 0.000001, "Scenario gut capacity");
    AssertClose(scenario.DigestionCaloriesPerSecond, roundTripped.DigestionCaloriesPerSecond, 0.000001, "Scenario digestion rate");
    AssertClose(scenario.EggProductionEnergyPerSecond, roundTripped.EggProductionEnergyPerSecond, 0.000001, "Scenario egg production");
    AssertClose(scenario.EggIncubationSeconds, roundTripped.EggIncubationSeconds, 0.000001, "Scenario egg incubation");
    AssertClose(scenario.MaturityAgeSeconds, roundTripped.MaturityAgeSeconds, 0.000001, "Scenario maturity age");
    AssertEqual(scenario.RequireReproductionIntent, roundTripped.RequireReproductionIntent, "Scenario reproduction intent gate");
    AssertClose(scenario.ReproductivePrimeAgeSeconds, roundTripped.ReproductivePrimeAgeSeconds, 0.000001, "Scenario prime fertility age");
    AssertClose(scenario.ReproductiveSenescenceAgeSeconds, roundTripped.ReproductiveSenescenceAgeSeconds, 0.000001, "Scenario senescence age");
    AssertClose(scenario.SenescentFertilityMultiplier, roundTripped.SenescentFertilityMultiplier, 0.000001, "Scenario senescent fertility");
    AssertClose(scenario.CrowdingFertilityPenalty, roundTripped.CrowdingFertilityPenalty, 0.000001, "Scenario crowding fertility");
    AssertClose(scenario.DietaryAdaptation, roundTripped.DietaryAdaptation, 0.000001, "Scenario dietary adaptation");
    AssertClose(scenario.CarrionAdaptation, roundTripped.CarrionAdaptation, 0.000001, "Scenario carrion adaptation");
    AssertClose(scenario.TenderPlantAdaptation, roundTripped.TenderPlantAdaptation, 0.000001, "Scenario tender plant adaptation");
    AssertClose(scenario.RichPlantAdaptation, roundTripped.RichPlantAdaptation, 0.000001, "Scenario rich plant adaptation");
    AssertClose(scenario.ToughPlantAdaptation, roundTripped.ToughPlantAdaptation, 0.000001, "Scenario tough plant adaptation");
    AssertClose(scenario.BiteStrength, roundTripped.BiteStrength, 0.000001, "Scenario bite strength");
    AssertClose(scenario.DamageResistance, roundTripped.DamageResistance, 0.000001, "Scenario damage resistance");
    AssertClose(scenario.DeathMeatCaloriesPerBodyRadius, roundTripped.DeathMeatCaloriesPerBodyRadius, 0.000001, "Scenario death meat body calories");
    AssertClose(scenario.DeathMeatEnergyFraction, roundTripped.DeathMeatEnergyFraction, 0.000001, "Scenario death meat energy fraction");
    AssertClose(scenario.MeatDecayCaloriesPerSecond, roundTripped.MeatDecayCaloriesPerSecond, 0.000001, "Scenario meat decay");
    AssertClose(scenario.RottenMeatDamagePerRawKcal, roundTripped.RottenMeatDamagePerRawKcal, 0.000001, "Scenario rotten meat damage");
    AssertClose(scenario.MeatScentRangeMultiplier, roundTripped.MeatScentRangeMultiplier, 0.000001, "Scenario meat scent range");
    AssertClose(scenario.MeatScentCaloriesForFullStrength, roundTripped.MeatScentCaloriesForFullStrength, 0.000001, "Scenario meat scent calorie scale");
    AssertClose(scenario.MeatScentDensitySaturation, roundTripped.MeatScentDensitySaturation, 0.000001, "Scenario meat scent saturation");
    AssertClose(scenario.BiteDamagePerSecond, roundTripped.BiteDamagePerSecond, 0.000001, "Scenario bite damage");
    AssertClose(scenario.BiteEnergyCostPerSecond, roundTripped.BiteEnergyCostPerSecond, 0.000001, "Scenario bite energy cost");
    AssertClose(scenario.BiteRangePadding, roundTripped.BiteRangePadding, 0.000001, "Scenario bite reach");
    AssertEqual(scenario.RelocateDepletedResources, roundTripped.RelocateDepletedResources, "Scenario resource relocation");
    AssertClose(scenario.MutationStrength, roundTripped.MutationStrength, 0.000001, "Scenario mutation strength");
    AssertClose(scenario.TraitMutationRate, roundTripped.TraitMutationRate, 0.000001, "Scenario trait mutation rate");
    AssertClose(scenario.BrainMutationRate, roundTripped.BrainMutationRate, 0.000001, "Scenario brain mutation rate");
}

static void AssertTrue(bool condition, string context)
{
    if (!condition)
    {
        throw new InvalidOperationException(context);
    }
}

static void AssertEqual<T>(T expected, T actual, string context)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{context}: expected {expected}, actual {actual}.");
    }
}

static void AssertBrainsClose(NeuralBrainGenome expected, NeuralBrainGenome actual, string context)
{
    AssertEqual(expected.HiddenNodeCount, actual.HiddenNodeCount, $"{context} hidden node count");
    AssertEqual(expected.Weights.Length, actual.Weights.Length, $"{context} weight count");

    for (var i = 0; i < expected.Weights.Length; i++)
    {
        AssertClose(expected.Weights[i], actual.Weights[i], 0.000001, $"{context} weight {i}");
    }
}

static void AssertDirectWeightsZero(NeuralBrainGenome brain, string context)
{
    for (var i = 0; i < NeuralBrainGenome.DirectWeightCount; i++)
    {
        AssertClose(0f, brain.Weights[i], 0.000001, $"{context} weight {i}");
    }
}

static void AssertClose(double expected, double actual, double tolerance, string context)
{
    if (Math.Abs(expected - actual) > tolerance)
    {
        throw new InvalidOperationException($"{context}: expected {expected}, actual {actual}.");
    }
}

static void AssertThrows<TException>(Action action, string context)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"{context}: expected exception {typeof(TException).Name}.");
}

static SimVector2 FindBiomeCellCenter(BiomeMap map, BiomeKind kind)
{
    for (var y = 0; y < map.CellCountY; y++)
    {
        for (var x = 0; x < map.CellCountX; x++)
        {
            if (map.GetKind(x, y) != kind)
            {
                continue;
            }

            var bounds = map.GetCellBounds(x, y);
            var position = new SimVector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.5f);
            if (!map.IsInResourceVoid(position))
            {
                return position;
            }
        }
    }

    throw new InvalidOperationException($"Biome map did not contain a non-void {kind} cell.");
}

static (SimVector2 Position, float HeadingRadians, BiomeKind CurrentBiome, BiomeKind ForwardBiome) FindAdjacentBiomeDragProbe(
    BiomeMap map,
    BiomePressureProfile speedProfile)
{
    for (var y = 0; y < map.CellCountY; y++)
    {
        for (var x = 0; x < map.CellCountX - 1; x++)
        {
            var current = map.GetKind(x, y);
            var forward = map.GetKind(x + 1, y);
            if (Math.Abs(speedProfile.For(current) - speedProfile.For(forward)) <= 0.000001f)
            {
                continue;
            }

            var bounds = map.GetCellBounds(x, y);
            return (
                new SimVector2(bounds.X + bounds.Width - 25f, bounds.Y + bounds.Height * 0.5f),
                0f,
                current,
                forward);
        }
    }

    for (var y = 0; y < map.CellCountY - 1; y++)
    {
        for (var x = 0; x < map.CellCountX; x++)
        {
            var current = map.GetKind(x, y);
            var forward = map.GetKind(x, y + 1);
            if (Math.Abs(speedProfile.For(current) - speedProfile.For(forward)) <= 0.000001f)
            {
                continue;
            }

            var bounds = map.GetCellBounds(x, y);
            return (
                new SimVector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height - 25f),
                MathF.PI * 0.5f,
                current,
                forward);
        }
    }

    throw new InvalidOperationException("Biome map did not contain adjacent cells with different terrain drag.");
}

static float SpeedMultiplierToDrag(float speedMultiplier)
{
    return Math.Clamp(1f - speedMultiplier, -1f, 1f);
}

static float ExpectedHabitatQuality(BiomeKind biome, float localFertility)
{
    var maximumDensity = BiomeKinds.All.Max(BiomeMap.GetResourceDensityMultiplier);
    var maximumRegrowth = BiomeKinds.All.Max(BiomeMap.GetResourceRegrowthMultiplier);
    var densityQuality = BiomeMap.GetResourceDensityMultiplier(biome) / maximumDensity;
    var regrowthQuality = BiomeMap.GetResourceRegrowthMultiplier(biome) / maximumRegrowth;
    return Math.Clamp((densityQuality * 0.65f + regrowthQuality * 0.35f) * localFertility, 0f, 1f);
}

/// <summary>
/// Test-only system that proves systems can mutate world state deterministically.
/// </summary>
sealed class ProbeMovementSystem : ISimulationSystem
{
    public void Update(WorldState state, float deltaSeconds)
    {
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var nudge = new SimVector2(
                state.Random.NextSingle(-1f, 1f),
                state.Random.NextSingle(-1f, 1f));

            creature.Position = state.Bounds.Clamp(creature.Position + nudge * deltaSeconds);
            creature.AgeSeconds += deltaSeconds;

            state.Creatures[i] = creature;
        }
    }
}

sealed class ProfilingNoOpSystem : ISimulationSystem
{
    public void Update(WorldState state, float deltaSeconds)
    {
    }
}
