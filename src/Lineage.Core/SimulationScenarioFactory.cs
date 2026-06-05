namespace Lineage.Core;

/// <summary>
/// Builds a seeded simulation from a scenario definition.
/// </summary>
public static class SimulationScenarioFactory
{
    private const ulong InitialBrainRandomizationSalt = 0x6C696E6561676542UL;

    public static Simulation CreateSimulation(SimulationScenario scenario, string? scenarioDirectory = null)
    {
        scenario = scenario.Validated();

        var simulation = new Simulation(
            new SimulationConfig
            {
                WorldWidth = scenario.WorldWidth,
                WorldHeight = scenario.WorldHeight,
                FixedDeltaSeconds = scenario.FixedDeltaSeconds
            },
            scenario.Seed,
            CreatePipeline(scenario));

        var biomeMap = CreateBiomeMap(scenario, scenarioDirectory);
        simulation.State.SetBiomes(biomeMap);
        simulation.State.SetTemperature(CreateTemperatureMap(scenario, biomeMap));
        simulation.State.SetObstacles(CreateObstacleMap(scenario, scenarioDirectory));
        simulation.State.SetLocalFertility(CreateLocalFertilityMap(scenario));
        SeedWorld(simulation, scenario);
        return simulation;
    }

    public static ISimulationSystem[] CreatePipeline(SimulationScenario scenario)
    {
        return scenario.PipelineKind switch
        {
            SimulationPipelineKind.Neural => SimulationPipelines.CreateNeuralLifeLoop(
                scenario.SpatialCellSize,
                scenario.StatsSnapshotIntervalTicks,
                scenario.BodyRadiusEnergyCostPerSecond,
                scenario.MaxSpeedEnergyCostPerSecond,
                scenario.TurnRateEnergyCostPerSecond,
                scenario.SenseRadiusEnergyCostPerSecond,
                scenario.VisionAngleEnergyCostPerSecond,
                scenario.EatRateEnergyCostPerSecond,
                scenario.GutCapacityEnergyCostPerSecond,
                scenario.DigestionRateEnergyCostPerSecond,
                scenario.BiteStrengthEnergyCostPerSecond,
                scenario.DamageResistanceEnergyCostPerSecond,
                scenario.PlantSpecializationEnergyCostPerSecond,
                scenario.MemoryEnergyCostPerSecond,
                scenario.RtNeatHiddenNodeEnergyCostPerSecond,
                scenario.RtNeatEnabledConnectionEnergyCostPerSecond,
                scenario.MemoryDecayPerSecond,
                scenario.MemoryWriteRatePerSecond,
                scenario.EggEnergyCostPerSecond,
                scenario.EggEnvironmentalDamagePerSecond,
                scenario.DeathMeatCaloriesPerBodyRadius,
                scenario.DeathMeatEnergyFraction,
                scenario.MeatDecayCaloriesPerSecond,
                scenario.RottenMeatDamagePerRawKcal,
                scenario.MeatScentRangeMultiplier,
                scenario.MeatScentCaloriesForFullStrength,
                scenario.MeatScentDensitySaturation,
                scenario.EnableSmallPrey,
                scenario.SmallPreyPerMillionArea,
                scenario.SmallPreyMaxSpawnsPerSecond,
                scenario.SmallPreyRadius,
                scenario.SmallPreyCalories,
                scenario.SmallPreyHealth,
                scenario.SmallPreyMaxSpeed,
                scenario.SmallPreyWanderIntervalSecondsMin,
                scenario.SmallPreyWanderIntervalSecondsMax,
                scenario.CreateSmallPreySpawnWeightProfile(),
                scenario.SoundRangeMultiplier,
                scenario.SoundDensitySaturation,
                scenario.WorldSenseIntervalTicks,
                scenario.CloseSenseRefreshProximity,
                scenario.CloseSenseRefreshMinimumTicks,
                scenario.EnableSectorVision,
                scenario.PlantPayoffTraceHalfLifeSeconds,
                scenario.SensingThreadCount,
                scenario.ReuseNeuralActionsOnSkippedWorldSenses,
                scenario.NeuralControllerThreadCount,
                scenario.BiteDamagePerSecond,
                scenario.BiteEnergyCostPerSecond,
                scenario.BiteRangePadding,
                scenario.RelocateDepletedResources,
                scenario.ResourceClusterStrength,
                scenario.ResourceClusterRadius,
                scenario.PlantLocalDispersalChance,
                scenario.PlantLocalDispersalRadius,
                scenario.PlantRespawnDelaySecondsMin,
                scenario.PlantRespawnDelaySecondsMax,
                scenario.ResourceCaloriesMin,
                scenario.ResourceCaloriesMax,
                scenario.EnableSeasons,
                scenario.SeasonLengthSeconds,
                scenario.SeasonFertilityAmplitude,
                scenario.SeasonPhaseOffsetSeconds,
                scenario.SeasonPhaseMode,
                scenario.CreateBiomeMovementCostProfile(),
                scenario.CreateBiomeBasalCostProfile(),
                scenario.ThermalMismatchBasalCostMultiplier,
                scenario.CreateBiomeSpeedProfile(),
                scenario.CreateBiomeVisionRangeProfile(),
                scenario.MovementSpeedCostExponent,
                scenario.RequireReproductionIntent,
                scenario.ReproductivePrimeAgeSeconds,
                scenario.ReproductiveSenescenceAgeSeconds,
                scenario.SenescentFertilityMultiplier,
                scenario.CrowdingFertilityPenalty,
                scenario.CreateBiomeSeasonalAmplitudeProfile(),
                scenario.EnableExtinctPayloadPruning,
                scenario.ExtinctPayloadPruneIntervalTicks,
                scenario.MutationStrength,
                scenario.TraitMutationRate,
                scenario.BrainMutationRate,
                scenario.FatDepositEnergyRatio,
                scenario.FatWithdrawEnergyRatio,
                scenario.FatTransferCapacitySharePerSecond,
                scenario.HealingDelaySeconds,
                scenario.HealingHealthFractionPerSecond,
                scenario.HealingEnergyCostPerHealth,
                scenario.HealingMinimumEnergy),
            SimulationPipelineKind.SimpleForaging => SimulationPipelines.CreateMinimalLifeLoop(
                scenario.SpatialCellSize,
                scenario.StatsSnapshotIntervalTicks,
                scenario.BodyRadiusEnergyCostPerSecond,
                scenario.MaxSpeedEnergyCostPerSecond,
                scenario.TurnRateEnergyCostPerSecond,
                scenario.SenseRadiusEnergyCostPerSecond,
                scenario.VisionAngleEnergyCostPerSecond,
                scenario.EatRateEnergyCostPerSecond,
                scenario.GutCapacityEnergyCostPerSecond,
                scenario.DigestionRateEnergyCostPerSecond,
                scenario.BiteStrengthEnergyCostPerSecond,
                scenario.DamageResistanceEnergyCostPerSecond,
                scenario.PlantSpecializationEnergyCostPerSecond,
                scenario.MemoryEnergyCostPerSecond,
                scenario.RtNeatHiddenNodeEnergyCostPerSecond,
                scenario.RtNeatEnabledConnectionEnergyCostPerSecond,
                scenario.EggEnergyCostPerSecond,
                scenario.EggEnvironmentalDamagePerSecond,
                scenario.DeathMeatCaloriesPerBodyRadius,
                scenario.DeathMeatEnergyFraction,
                scenario.MeatDecayCaloriesPerSecond,
                scenario.RottenMeatDamagePerRawKcal,
                scenario.MeatScentRangeMultiplier,
                scenario.MeatScentCaloriesForFullStrength,
                scenario.MeatScentDensitySaturation,
                scenario.EnableSmallPrey,
                scenario.SmallPreyPerMillionArea,
                scenario.SmallPreyMaxSpawnsPerSecond,
                scenario.SmallPreyRadius,
                scenario.SmallPreyCalories,
                scenario.SmallPreyHealth,
                scenario.SmallPreyMaxSpeed,
                scenario.SmallPreyWanderIntervalSecondsMin,
                scenario.SmallPreyWanderIntervalSecondsMax,
                scenario.CreateSmallPreySpawnWeightProfile(),
                scenario.BiteDamagePerSecond,
                scenario.BiteEnergyCostPerSecond,
                scenario.BiteRangePadding,
                scenario.RelocateDepletedResources,
                scenario.ResourceClusterStrength,
                scenario.ResourceClusterRadius,
                scenario.PlantLocalDispersalChance,
                scenario.PlantLocalDispersalRadius,
                scenario.PlantRespawnDelaySecondsMin,
                scenario.PlantRespawnDelaySecondsMax,
                scenario.ResourceCaloriesMin,
                scenario.ResourceCaloriesMax,
                scenario.EnableSeasons,
                scenario.SeasonLengthSeconds,
                scenario.SeasonFertilityAmplitude,
                scenario.SeasonPhaseOffsetSeconds,
                scenario.SeasonPhaseMode,
                scenario.CreateBiomeMovementCostProfile(),
                scenario.CreateBiomeBasalCostProfile(),
                scenario.ThermalMismatchBasalCostMultiplier,
                scenario.CreateBiomeSpeedProfile(),
                scenario.MovementSpeedCostExponent,
                scenario.RequireReproductionIntent,
                scenario.ReproductivePrimeAgeSeconds,
                scenario.ReproductiveSenescenceAgeSeconds,
                scenario.SenescentFertilityMultiplier,
                scenario.CrowdingFertilityPenalty,
                scenario.CreateBiomeSeasonalAmplitudeProfile(),
                scenario.EnableExtinctPayloadPruning,
                scenario.ExtinctPayloadPruneIntervalTicks,
                scenario.MutationStrength,
                scenario.TraitMutationRate,
                scenario.BrainMutationRate,
                scenario.FatDepositEnergyRatio,
                scenario.FatWithdrawEnergyRatio,
                scenario.FatTransferCapacitySharePerSecond,
                scenario.HealingDelaySeconds,
                scenario.HealingHealthFractionPerSecond,
                scenario.HealingEnergyCostPerHealth,
                scenario.HealingMinimumEnergy),
            _ => throw new InvalidOperationException($"Unsupported pipeline kind: {scenario.PipelineKind}.")
        };
    }

    private static void SeedWorld(Simulation simulation, SimulationScenario scenario)
    {
        var state = simulation.State;

        var initialResourceCount = scenario.CalculateInitialResourceCount();
        for (var i = 0; i < initialResourceCount; i++)
        {
            var position = ResourcePlacement.SamplePlantPosition(
                state,
                scenario.ResourceClusterStrength,
                scenario.ResourceClusterRadius);
            var biomeKind = state.Biomes.GetKindAt(position);
            var plantKind = scenario.SamplePlantResourceKind(state.Random, biomeKind);
            var plantTraits = PlantResourceTraits.For(plantKind);
            var radius = RandomRange(state, scenario.ResourceRadiusMin, scenario.ResourceRadiusMax)
                * plantTraits.RadiusMultiplier;
            var maxCalories = scenario.ResourceMaxCalories * plantTraits.MaxCaloriesMultiplier;
            var calories = Math.Min(
                maxCalories,
                RandomRange(state, scenario.ResourceCaloriesMin, scenario.ResourceCaloriesMax)
                * plantTraits.InitialCaloriesMultiplier);
            state.SpawnResourcePatch(new ResourcePatchState
            {
                Position = position,
                PlantKind = plantKind,
                HabitatBiomeKind = biomeKind,
                Radius = Math.Max(0.1f, radius),
                Calories = calories,
                MaxCalories = maxCalories,
                RegrowthCaloriesPerSecond = RandomRange(
                    state,
                    scenario.ResourceRegrowthMin,
                    scenario.ResourceRegrowthMax)
                    * plantTraits.RegrowthMultiplier
                    * state.Biomes.GetResourceRegrowthMultiplierAt(position)
            });
        }

        if (scenario.HasEnabledSpeciesSeeds())
        {
            return;
        }

        var initialGenome = (CreatureGenome.Baseline with
        {
            BodyRadius = scenario.InitialBodyRadius,
            MaxSpeed = scenario.InitialMaxSpeed,
            MaxTurnRadiansPerSecond = scenario.InitialMaxTurnRadiansPerSecond,
            SenseRadius = scenario.InitialSenseRadius,
            MetabolicPace = scenario.MetabolicPace,
            BasalEnergyPerSecond = scenario.BasalEnergyPerSecond,
            MovementEnergyPerSecond = scenario.MovementEnergyPerSecond,
            EatCaloriesPerSecond = scenario.EatCaloriesPerSecond,
            GutCapacityCalories = scenario.GutCapacityCalories,
            DigestionCaloriesPerSecond = scenario.DigestionCaloriesPerSecond,
            FatStorageCapacityCalories = scenario.FatStorageCapacityCalories,
            FatStorageEfficiency = scenario.FatStorageEfficiency,
            VisionAngleRadians = scenario.VisionAngleRadians,
            ReproductionEnergyThreshold = scenario.ReproductionEnergyThreshold,
            OffspringEnergyInvestment = scenario.OffspringEnergyInvestment,
            EggProductionEnergyPerSecond = scenario.EggProductionEnergyPerSecond,
            EggIncubationSeconds = scenario.EggIncubationSeconds,
            MaturityAgeSeconds = scenario.MaturityAgeSeconds,
            ReproductionCooldownSeconds = scenario.ReproductionCooldownSeconds,
            MaxLifeExpectancySeconds = scenario.MaxLifeExpectancySeconds,
            DietaryAdaptation = scenario.DietaryAdaptation,
            CarrionAdaptation = scenario.CarrionAdaptation,
            TenderPlantAdaptation = scenario.TenderPlantAdaptation,
            RichPlantAdaptation = scenario.RichPlantAdaptation,
            ToughPlantAdaptation = scenario.ToughPlantAdaptation,
            ThermalOptimum = scenario.ThermalOptimum,
            ThermalTolerance = scenario.ThermalTolerance,
            BiteStrength = scenario.BiteStrength,
            DamageResistance = scenario.DamageResistance,
            MutationStrength = scenario.MutationStrength,
            TraitMutationRate = scenario.TraitMutationRate,
            BrainMutationRate = scenario.BrainMutationRate
        }).Validated();
        var sharedBrainId = CreateSharedInitialBrainId(state, scenario);
        var initialBrainRandom = scenario.InitialBrainKind == InitialBrainKind.RandomPerFounder
            ? new DeterministicRandom(scenario.Seed ^ InitialBrainRandomizationSalt)
            : null;
        var initialBodyRadius = initialGenome.BodyRadius;
        var initialMutationProfile = MutationProfile.FromScenario(scenario);

        for (var i = 0; i < scenario.InitialCreatureCount; i++)
        {
            var brainId = CreateFounderBrainId(state, scenario, sharedBrainId, initialBrainRandom);
            var founderGenomeId = state.AddGenome(initialGenome.WithRandomScentSignature(state.Random));
            state.SpawnCreature(
                founderGenomeId,
                RandomCreaturePosition(state, scenario.InitialCreatureSpawnRegion, initialBodyRadius),
                energy: RandomRange(
                    state,
                    scenario.InitialCreatureEnergyMin,
                    scenario.InitialCreatureEnergyMax),
                brainId: brainId,
                birthMutationProfile: initialMutationProfile);
        }
    }

    public static BiomeMap CreateBiomeMap(SimulationScenario scenario, string? scenarioDirectory = null)
    {
        scenario = scenario.Validated();
        var bounds = new WorldBounds(scenario.WorldWidth, scenario.WorldHeight);
        if (!scenario.EnableBiomes)
        {
            return BiomeMap.CreateUniform(
                bounds,
                MathF.Max(scenario.WorldWidth, scenario.WorldHeight),
                BiomeKind.Grassland,
                scenario.ResourceVoidBorderWidth);
        }

        return scenario.BiomeMapKind switch
        {
            BiomeMapKind.Manual => LoadManualBiomeMap(scenario, scenarioDirectory),
            BiomeMapKind.NaturalClimate => BiomeMap.GenerateNaturalClimate(
                bounds,
                scenario.BiomeCellSize,
                scenario.Seed,
                scenario.ResourceVoidBorderWidth),
            BiomeMapKind.HorizontalBands
                or BiomeMapKind.VerticalBands
                or BiomeMapKind.HorizontalEdgeBands
                or BiomeMapKind.VerticalEdgeBands
                or BiomeMapKind.HorizontalEdgeLadderBands
                or BiomeMapKind.VerticalEdgeLadderBands
                or BiomeMapKind.VerticalEdgeCorridorBands
                or BiomeMapKind.VerticalEdgeWideCorridorBands => BiomeMap.GenerateBands(
                bounds,
                scenario.BiomeCellSize,
                scenario.BiomeMapKind,
                scenario.ResourceVoidBorderWidth),
            _ => BiomeMap.Generate(bounds, scenario.BiomeCellSize, scenario.Seed, scenario.ResourceVoidBorderWidth)
        };
    }

    public static string ResolveManualBiomeMapPath(string manualBiomeMapPath, string? scenarioDirectory = null)
    {
        return ResolveManualMapPath(manualBiomeMapPath, scenarioDirectory, nameof(manualBiomeMapPath));
    }

    public static string ResolveManualObstacleMapPath(string manualObstacleMapPath, string? scenarioDirectory = null)
    {
        return ResolveManualMapPath(manualObstacleMapPath, scenarioDirectory, nameof(manualObstacleMapPath));
    }

    public static string ResolveWorldMapPath(string worldMapPath, string? scenarioDirectory = null)
    {
        return ResolveManualMapPath(worldMapPath, scenarioDirectory, nameof(worldMapPath));
    }

    private static BiomeMap LoadManualBiomeMap(SimulationScenario scenario, string? scenarioDirectory)
    {
        if (!string.IsNullOrWhiteSpace(scenario.WorldMapPath))
        {
            var worldMapPath = ResolveWorldMapPath(scenario.WorldMapPath, scenarioDirectory);
            var worldMap = WorldMapArtifactJson.Load(worldMapPath);
            ValidateWorldMapBiomeMatchesScenario(worldMap, scenario, worldMapPath);
            return worldMap.ToBiomeMap();
        }

        var manualPath = ResolveManualBiomeMapPath(
            scenario.ManualBiomeMapPath
                ?? throw new InvalidOperationException("Manual biome maps require manualBiomeMapPath."),
            scenarioDirectory);
        var document = ManualBiomeMapJson.Load(manualPath);
        ValidateManualBiomeMapMatchesScenario(document, scenario, manualPath);
        return document.ToBiomeMap();
    }

    private static void ValidateManualBiomeMapMatchesScenario(
        ManualBiomeMapDocument document,
        SimulationScenario scenario,
        string manualPath)
    {
        AssertClose(document.WorldWidth, scenario.WorldWidth, nameof(document.WorldWidth), manualPath);
        AssertClose(document.WorldHeight, scenario.WorldHeight, nameof(document.WorldHeight), manualPath);
        AssertClose(document.CellSize, scenario.BiomeCellSize, nameof(document.CellSize), manualPath);
        AssertClose(document.ResourceVoidBorderWidth, scenario.ResourceVoidBorderWidth, nameof(document.ResourceVoidBorderWidth), manualPath);

        var expectedCellCountX = Math.Max(1, (int)MathF.Ceiling(scenario.WorldWidth / scenario.BiomeCellSize));
        var expectedCellCountY = Math.Max(1, (int)MathF.Ceiling(scenario.WorldHeight / scenario.BiomeCellSize));
        if (document.CellCountX != expectedCellCountX || document.CellCountY != expectedCellCountY)
        {
            throw new InvalidOperationException(
                $"Manual biome map '{manualPath}' is {document.CellCountX}x{document.CellCountY} cells, " +
                $"but the scenario expects {expectedCellCountX}x{expectedCellCountY}.");
        }
    }

    private static void ValidateWorldMapBiomeMatchesScenario(
        WorldMapArtifactDocument document,
        SimulationScenario scenario,
        string worldMapPath)
    {
        AssertClose(document.WorldWidth, scenario.WorldWidth, nameof(document.WorldWidth), worldMapPath, "World map artifact");
        AssertClose(document.WorldHeight, scenario.WorldHeight, nameof(document.WorldHeight), worldMapPath, "World map artifact");
        AssertClose(document.BiomeCellSize, scenario.BiomeCellSize, nameof(document.BiomeCellSize), worldMapPath, "World map artifact");
        AssertClose(document.ResourceVoidBorderWidth, scenario.ResourceVoidBorderWidth, nameof(document.ResourceVoidBorderWidth), worldMapPath, "World map artifact");

        var expectedCellCountX = Math.Max(1, (int)MathF.Ceiling(scenario.WorldWidth / scenario.BiomeCellSize));
        var expectedCellCountY = Math.Max(1, (int)MathF.Ceiling(scenario.WorldHeight / scenario.BiomeCellSize));
        if (document.BiomeCellCountX != expectedCellCountX || document.BiomeCellCountY != expectedCellCountY)
        {
            throw new InvalidOperationException(
                $"World map artifact '{worldMapPath}' biome grid is {document.BiomeCellCountX}x{document.BiomeCellCountY} cells, " +
                $"but the scenario expects {expectedCellCountX}x{expectedCellCountY}.");
        }
    }

    private static void AssertClose(
        float actual,
        float expected,
        string name,
        string manualPath,
        string mapLabel = "Manual biome map")
    {
        if (MathF.Abs(actual - expected) > 0.0001f)
        {
            throw new InvalidOperationException(
                $"{mapLabel} '{manualPath}' {name} is {actual}, but the scenario expects {expected}.");
        }
    }

    public static ObstacleMap CreateObstacleMap(SimulationScenario scenario, string? scenarioDirectory = null)
    {
        scenario = scenario.Validated();
        var bounds = new WorldBounds(scenario.WorldWidth, scenario.WorldHeight);
        if (!scenario.EnableObstacles || scenario.ObstacleMapKind == ObstacleMapKind.None)
        {
            return ObstacleMap.CreateEmpty(bounds, scenario.ObstacleCellSize);
        }

        return scenario.ObstacleMapKind switch
        {
            ObstacleMapKind.Manual => LoadManualObstacleMap(scenario, scenarioDirectory),
            _ => ObstacleMap.Generate(bounds, scenario.ObstacleCellSize, scenario.ObstacleMapKind, scenario.Seed)
        };
    }

    public static TemperatureMap CreateTemperatureMap(SimulationScenario scenario, BiomeMap biomes)
    {
        scenario = scenario.Validated();
        return scenario.EnableTemperature
            ? TemperatureMap.GenerateFromBiomes(biomes, scenario.Seed)
            : TemperatureMap.CreateNeutral(new WorldBounds(scenario.WorldWidth, scenario.WorldHeight));
    }

    private static string ResolveManualMapPath(string path, string? scenarioDirectory, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Manual map path is required.", argumentName);
        }

        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        var baseDirectory = string.IsNullOrWhiteSpace(scenarioDirectory)
            ? Directory.GetCurrentDirectory()
            : scenarioDirectory;
        var scenarioRelativePath = Path.GetFullPath(Path.Combine(baseDirectory, path));
        if (File.Exists(scenarioRelativePath))
        {
            return scenarioRelativePath;
        }

        var repositoryRoot = TryFindRepositoryRoot(baseDirectory);
        if (!string.IsNullOrWhiteSpace(repositoryRoot))
        {
            var repositoryRelativePath = Path.GetFullPath(Path.Combine(repositoryRoot, path));
            if (File.Exists(repositoryRelativePath))
            {
                return repositoryRelativePath;
            }
        }

        return scenarioRelativePath;
    }

    private static ObstacleMap LoadManualObstacleMap(SimulationScenario scenario, string? scenarioDirectory)
    {
        if (!string.IsNullOrWhiteSpace(scenario.WorldMapPath))
        {
            var worldMapPath = ResolveWorldMapPath(scenario.WorldMapPath, scenarioDirectory);
            var worldMap = WorldMapArtifactJson.Load(worldMapPath);
            ValidateWorldMapObstacleMatchesScenario(worldMap, scenario, worldMapPath);
            return worldMap.ToObstacleMap();
        }

        var manualPath = ResolveManualObstacleMapPath(
            scenario.ManualObstacleMapPath
                ?? throw new InvalidOperationException("Manual obstacle maps require manualObstacleMapPath."),
            scenarioDirectory);
        var document = ManualObstacleMapJson.Load(manualPath);
        ValidateManualObstacleMapMatchesScenario(document, scenario, manualPath);
        return document.ToObstacleMap();
    }

    private static void ValidateManualObstacleMapMatchesScenario(
        ManualObstacleMapDocument document,
        SimulationScenario scenario,
        string manualPath)
    {
        AssertClose(document.WorldWidth, scenario.WorldWidth, nameof(document.WorldWidth), manualPath, "Manual obstacle map");
        AssertClose(document.WorldHeight, scenario.WorldHeight, nameof(document.WorldHeight), manualPath, "Manual obstacle map");
        AssertClose(document.CellSize, scenario.ObstacleCellSize, nameof(document.CellSize), manualPath, "Manual obstacle map");

        var expectedCellCountX = Math.Max(1, (int)MathF.Ceiling(scenario.WorldWidth / scenario.ObstacleCellSize));
        var expectedCellCountY = Math.Max(1, (int)MathF.Ceiling(scenario.WorldHeight / scenario.ObstacleCellSize));
        if (document.CellCountX != expectedCellCountX || document.CellCountY != expectedCellCountY)
        {
            throw new InvalidOperationException(
                $"Manual obstacle map '{manualPath}' is {document.CellCountX}x{document.CellCountY} cells, " +
                $"but the scenario expects {expectedCellCountX}x{expectedCellCountY}.");
        }
    }

    private static void ValidateWorldMapObstacleMatchesScenario(
        WorldMapArtifactDocument document,
        SimulationScenario scenario,
        string worldMapPath)
    {
        AssertClose(document.WorldWidth, scenario.WorldWidth, nameof(document.WorldWidth), worldMapPath, "World map artifact");
        AssertClose(document.WorldHeight, scenario.WorldHeight, nameof(document.WorldHeight), worldMapPath, "World map artifact");
        AssertClose(document.ObstacleCellSize, scenario.ObstacleCellSize, nameof(document.ObstacleCellSize), worldMapPath, "World map artifact");

        var expectedCellCountX = Math.Max(1, (int)MathF.Ceiling(scenario.WorldWidth / scenario.ObstacleCellSize));
        var expectedCellCountY = Math.Max(1, (int)MathF.Ceiling(scenario.WorldHeight / scenario.ObstacleCellSize));
        if (document.ObstacleCellCountX != expectedCellCountX || document.ObstacleCellCountY != expectedCellCountY)
        {
            throw new InvalidOperationException(
                $"World map artifact '{worldMapPath}' obstacle grid is {document.ObstacleCellCountX}x{document.ObstacleCellCountY} cells, " +
                $"but the scenario expects {expectedCellCountX}x{expectedCellCountY}.");
        }
    }

    private static string? TryFindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Lineage.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static LocalFertilityMap CreateLocalFertilityMap(SimulationScenario scenario)
    {
        var bounds = new WorldBounds(scenario.WorldWidth, scenario.WorldHeight);
        return scenario.EnableLocalFertility
            ? LocalFertilityMap.Create(
                bounds,
                scenario.LocalFertilityCellSize,
                scenario.LocalFertilityMinimumMultiplier,
                scenario.LocalFertilityRecoveryPerSecond,
                scenario.LocalFertilityDepletionPerPlant,
                scenario.LocalFertilityNeighborDepletionShare)
            : LocalFertilityMap.CreateDisabled(bounds);
    }

    private static int CreateSharedInitialBrainId(WorldState state, SimulationScenario scenario)
    {
        if (scenario.PipelineKind != SimulationPipelineKind.Neural
            || scenario.InitialBrainKind == InitialBrainKind.RandomPerFounder)
        {
            return -1;
        }

        return state.AddBrain(BrainFactory.CreateStarter(
            scenario.BrainArchitectureKind,
            scenario.InitialBrainKind,
            scenario.BrainHiddenNodeCount));
    }

    private static int CreateFounderBrainId(
        WorldState state,
        SimulationScenario scenario,
        int sharedBrainId,
        DeterministicRandom? initialBrainRandom)
    {
        if (scenario.PipelineKind != SimulationPipelineKind.Neural)
        {
            return -1;
        }

        if (scenario.InitialBrainKind != InitialBrainKind.RandomPerFounder)
        {
            return sharedBrainId;
        }

        if (initialBrainRandom is null)
        {
            throw new InvalidOperationException("Randomized initial brains require a deterministic brain RNG.");
        }

        // Keep initial brain variation independent of world/resource placement randomness.
        return state.AddBrain(BrainFactory.CreateRandom(
            scenario.BrainArchitectureKind,
            initialBrainRandom,
            hiddenNodeCount: scenario.BrainHiddenNodeCount));
    }

    private static SimVector2 RandomCreaturePosition(
        WorldState state,
        InitialCreatureSpawnRegion spawnRegion,
        float bodyRadius)
    {
        var bounds = ResolveCreatureSpawnBounds(state, spawnRegion);
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var candidate = new SimVector2(
                RandomRange(state, bounds.Left, bounds.Right),
                RandomRange(state, bounds.Top, bounds.Bottom));

            if (!state.Obstacles.IsBlockedForCircle(candidate, bodyRadius))
            {
                return candidate;
            }
        }

        return new SimVector2(
            RandomRange(state, bounds.Left, bounds.Right),
            RandomRange(state, bounds.Top, bounds.Bottom));
    }

    private static CreatureSpawnBounds ResolveCreatureSpawnBounds(WorldState state, InitialCreatureSpawnRegion spawnRegion)
    {
        var left = 0f;
        var top = 0f;
        var right = state.Bounds.Width;
        var bottom = state.Bounds.Height;
        var thirdWidth = state.Bounds.Width / 3f;
        var thirdHeight = state.Bounds.Height / 3f;
        var halfWidth = state.Bounds.Width * 0.5f;
        var halfHeight = state.Bounds.Height * 0.5f;

        switch (spawnRegion)
        {
            case InitialCreatureSpawnRegion.LeftThird:
                right = thirdWidth;
                break;
            case InitialCreatureSpawnRegion.MiddleThird:
                left = thirdWidth;
                right = thirdWidth * 2f;
                break;
            case InitialCreatureSpawnRegion.RightThird:
                left = thirdWidth * 2f;
                break;
            case InitialCreatureSpawnRegion.TopThird:
                bottom = thirdHeight;
                break;
            case InitialCreatureSpawnRegion.BottomThird:
                top = thirdHeight * 2f;
                break;
            case InitialCreatureSpawnRegion.UpperLeftQuadrant:
                right = halfWidth;
                bottom = halfHeight;
                break;
            case InitialCreatureSpawnRegion.UpperRightQuadrant:
                left = halfWidth;
                bottom = halfHeight;
                break;
            case InitialCreatureSpawnRegion.LowerLeftQuadrant:
                right = halfWidth;
                top = halfHeight;
                break;
            case InitialCreatureSpawnRegion.LowerRightQuadrant:
                left = halfWidth;
                top = halfHeight;
                break;
        }

        if (spawnRegion != InitialCreatureSpawnRegion.Uniform)
        {
            var padding = MathF.Min(
                state.Biomes.ResourceVoidBorderWidth,
                MathF.Min(state.Bounds.Width, state.Bounds.Height) * 0.45f);
            left = MathF.Max(left, padding);
            top = MathF.Max(top, padding);
            right = MathF.Min(right, state.Bounds.Width - padding);
            bottom = MathF.Min(bottom, state.Bounds.Height - padding);
        }

        if (right <= left)
        {
            left = 0f;
            right = state.Bounds.Width;
        }

        if (bottom <= top)
        {
            top = 0f;
            bottom = state.Bounds.Height;
        }

        return new CreatureSpawnBounds(left, top, right, bottom);
    }

    private static float RandomRange(WorldState state, float inclusiveMin, float exclusiveMax)
    {
        return Math.Abs(exclusiveMax - inclusiveMin) <= float.Epsilon
            ? inclusiveMin
            : state.Random.NextSingle(inclusiveMin, exclusiveMax);
    }

    private readonly record struct CreatureSpawnBounds(float Left, float Top, float Right, float Bottom);
}
