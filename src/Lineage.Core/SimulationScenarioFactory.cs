namespace Lineage.Core;

/// <summary>
/// Builds a seeded simulation from a scenario definition.
/// </summary>
public static class SimulationScenarioFactory
{
    private const ulong InitialBrainRandomizationSalt = 0x6C696E6561676542UL;

    public static Simulation CreateSimulation(SimulationScenario scenario)
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

        simulation.State.Biomes = CreateBiomeMap(scenario);
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
                scenario.EggEnergyCostPerSecond,
                scenario.EggEnvironmentalDamagePerSecond,
                scenario.DeathMeatCaloriesPerBodyRadius,
                scenario.DeathMeatEnergyFraction,
                scenario.MeatDecayCaloriesPerSecond,
                scenario.MeatScentRangeMultiplier,
                scenario.MeatScentCaloriesForFullStrength,
                scenario.MeatScentDensitySaturation,
                scenario.BiteDamagePerSecond,
                scenario.BiteEnergyCostPerSecond,
                scenario.BiteRangePadding,
                scenario.RelocateDepletedResources,
                scenario.ResourceClusterStrength,
                scenario.ResourceClusterRadius,
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
                scenario.CreateBiomeSpeedProfile(),
                scenario.MovementSpeedCostExponent,
                scenario.RequireReproductionIntent,
                scenario.ReproductivePrimeAgeSeconds,
                scenario.ReproductiveSenescenceAgeSeconds,
                scenario.SenescentFertilityMultiplier,
                scenario.CrowdingFertilityPenalty,
                scenario.CreateBiomeSeasonalAmplitudeProfile()),
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
                scenario.EggEnergyCostPerSecond,
                scenario.EggEnvironmentalDamagePerSecond,
                scenario.DeathMeatCaloriesPerBodyRadius,
                scenario.DeathMeatEnergyFraction,
                scenario.MeatDecayCaloriesPerSecond,
                scenario.MeatScentRangeMultiplier,
                scenario.MeatScentCaloriesForFullStrength,
                scenario.MeatScentDensitySaturation,
                scenario.BiteDamagePerSecond,
                scenario.BiteEnergyCostPerSecond,
                scenario.BiteRangePadding,
                scenario.RelocateDepletedResources,
                scenario.ResourceClusterStrength,
                scenario.ResourceClusterRadius,
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
                scenario.CreateBiomeSpeedProfile(),
                scenario.MovementSpeedCostExponent,
                scenario.RequireReproductionIntent,
                scenario.ReproductivePrimeAgeSeconds,
                scenario.ReproductiveSenescenceAgeSeconds,
                scenario.SenescentFertilityMultiplier,
                scenario.CrowdingFertilityPenalty,
                scenario.CreateBiomeSeasonalAmplitudeProfile()),
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
            state.SpawnResourcePatch(new ResourcePatchState
            {
                Position = position,
                Radius = RandomRange(state, scenario.ResourceRadiusMin, scenario.ResourceRadiusMax),
                Calories = RandomRange(state, scenario.ResourceCaloriesMin, scenario.ResourceCaloriesMax),
                MaxCalories = scenario.ResourceMaxCalories,
                RegrowthCaloriesPerSecond = RandomRange(
                    state,
                    scenario.ResourceRegrowthMin,
                    scenario.ResourceRegrowthMax)
                    * state.Biomes.GetResourceRegrowthMultiplierAt(position)
            });
        }

        if (scenario.HasEnabledSpeciesSeeds())
        {
            return;
        }

        var genomeId = state.AddGenome(CreatureGenome.Baseline with
        {
            BasalEnergyPerSecond = scenario.BasalEnergyPerSecond,
            MovementEnergyPerSecond = scenario.MovementEnergyPerSecond,
            EatCaloriesPerSecond = scenario.EatCaloriesPerSecond,
            GutCapacityCalories = scenario.GutCapacityCalories,
            DigestionCaloriesPerSecond = scenario.DigestionCaloriesPerSecond,
            VisionAngleRadians = scenario.VisionAngleRadians,
            ReproductionEnergyThreshold = scenario.ReproductionEnergyThreshold,
            OffspringEnergyInvestment = scenario.OffspringEnergyInvestment,
            EggProductionEnergyPerSecond = scenario.EggProductionEnergyPerSecond,
            EggIncubationSeconds = scenario.EggIncubationSeconds,
            MaturityAgeSeconds = scenario.MaturityAgeSeconds,
            ReproductionCooldownSeconds = scenario.ReproductionCooldownSeconds,
            DietaryAdaptation = scenario.DietaryAdaptation,
            CarrionAdaptation = scenario.CarrionAdaptation,
            BiteStrength = scenario.BiteStrength,
            DamageResistance = scenario.DamageResistance,
            MutationStrength = scenario.MutationStrength,
            TraitMutationRate = scenario.TraitMutationRate,
            BrainMutationRate = scenario.BrainMutationRate
        });
        var sharedBrainId = CreateSharedInitialBrainId(state, scenario);
        var initialBrainRandom = scenario.InitialBrainKind == InitialBrainKind.RandomPerFounder
            ? new DeterministicRandom(scenario.Seed ^ InitialBrainRandomizationSalt)
            : null;

        for (var i = 0; i < scenario.InitialCreatureCount; i++)
        {
            var brainId = CreateFounderBrainId(state, scenario, sharedBrainId, initialBrainRandom);
            state.SpawnCreature(
                genomeId,
                RandomCreaturePosition(state, scenario.InitialCreatureSpawnRegion),
                energy: RandomRange(
                    state,
                    scenario.InitialCreatureEnergyMin,
                    scenario.InitialCreatureEnergyMax),
                brainId: brainId);
        }
    }

    private static BiomeMap CreateBiomeMap(SimulationScenario scenario)
    {
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
            BiomeMapKind.HorizontalBands
                or BiomeMapKind.VerticalBands
                or BiomeMapKind.HorizontalEdgeBands
                or BiomeMapKind.VerticalEdgeBands
                or BiomeMapKind.HorizontalEdgeLadderBands
                or BiomeMapKind.VerticalEdgeLadderBands
                or BiomeMapKind.VerticalEdgeCorridorBands => BiomeMap.GenerateBands(
                bounds,
                scenario.BiomeCellSize,
                scenario.BiomeMapKind,
                scenario.ResourceVoidBorderWidth),
            _ => BiomeMap.Generate(bounds, scenario.BiomeCellSize, scenario.Seed, scenario.ResourceVoidBorderWidth)
        };
    }

    private static int CreateSharedInitialBrainId(WorldState state, SimulationScenario scenario)
    {
        if (scenario.PipelineKind != SimulationPipelineKind.Neural
            || scenario.InitialBrainKind == InitialBrainKind.RandomPerFounder)
        {
            return -1;
        }

        return state.AddBrain(CreateInitialBrain(scenario.InitialBrainKind, scenario.BrainHiddenNodeCount));
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
        return state.AddBrain(NeuralBrainGenome.CreateRandom(
            initialBrainRandom,
            hiddenNodeCount: scenario.BrainHiddenNodeCount));
    }

    private static NeuralBrainGenome CreateInitialBrain(InitialBrainKind initialBrainKind, int hiddenNodeCount)
    {
        return initialBrainKind switch
        {
            InitialBrainKind.SeedForager => NeuralBrainGenome.CreateSeedForager(hiddenNodeCount),
            InitialBrainKind.ExplorerForager => NeuralBrainGenome.CreateExplorerForager(hiddenNodeCount),
            InitialBrainKind.ScavengerForager => NeuralBrainGenome.CreateScavengerForager(hiddenNodeCount),
            InitialBrainKind.ForagerPredator => NeuralBrainGenome.CreateForagerPredator(hiddenNodeCount),
            InitialBrainKind.RandomPerFounder => throw new ArgumentException("Random-per-founder brains are created individually."),
            _ => throw new ArgumentOutOfRangeException(nameof(initialBrainKind), initialBrainKind, "Unsupported initial brain kind.")
        };
    }

    private static SimVector2 RandomCreaturePosition(WorldState state, InitialCreatureSpawnRegion spawnRegion)
    {
        var bounds = ResolveCreatureSpawnBounds(state, spawnRegion);
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
