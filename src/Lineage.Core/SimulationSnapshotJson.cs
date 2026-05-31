using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lineage.Core;

/// <summary>
/// JSON helpers for saving and restoring complete simulation snapshots.
/// </summary>
public static class SimulationSnapshotJson
{
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    public static string ToJson(SimulationSnapshot snapshot)
    {
        return JsonSerializer.Serialize(Validate(snapshot), JsonOptions);
    }

    public static SimulationSnapshot FromJson(string json)
    {
        var snapshot = JsonSerializer.Deserialize<SimulationSnapshot>(json, JsonOptions)
            ?? throw new InvalidOperationException("Snapshot JSON did not contain a snapshot object.");
        return Validate(snapshot);
    }

    public static void Save(string path, SimulationSnapshot snapshot)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, Validate(snapshot), JsonOptions);
    }

    public static SimulationSnapshot Load(string path)
    {
        using var stream = File.OpenRead(path);
        var snapshot = JsonSerializer.Deserialize<SimulationSnapshot>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Snapshot JSON did not contain a snapshot object.");
        return Validate(snapshot);
    }

    public static RestoredSimulation LoadSimulation(string path)
    {
        return RestoreSimulation(Load(path));
    }

    public static RestoredSimulation RestoreSimulation(SimulationSnapshot snapshot)
    {
        snapshot = Validate(snapshot);
        var scenario = snapshot.Scenario.Validated();
        var simulation = new Simulation(
            new SimulationConfig
            {
                WorldWidth = scenario.WorldWidth,
                WorldHeight = scenario.WorldHeight,
                FixedDeltaSeconds = scenario.FixedDeltaSeconds
            },
            scenario.Seed,
            SimulationScenarioFactory.CreatePipeline(scenario));

        RestoreState(simulation.State, snapshot);
        return new RestoredSimulation(scenario, simulation);
    }

    private static void RestoreState(WorldState state, SimulationSnapshot snapshot)
    {
        state.Creatures.Clear();
        state.Eggs.Clear();
        state.Resources.Clear();
        state.Genomes.Clear();
        state.Brains.Clear();
        state.BrainArchitectureKinds.Clear();

        state.Biomes = snapshot.Biomes.ToMap(state.Bounds);
        state.SetObstacles(snapshot.Obstacles.ToMap(state.Bounds));
        state.SetLocalFertility(snapshot.LocalFertility.ToMap(state.Bounds));
        state.Random.State = snapshot.RandomState;
        state.RestoreClock(snapshot.Tick, snapshot.ElapsedSeconds);
        state.RestoreNextEntityId(snapshot.NextEntityId);

        foreach (var genome in snapshot.Genomes)
        {
            state.Genomes.Add(NormalizeGenome(genome).Validated());
        }

        var brainArchitectureKinds = NormalizeBrainArchitectureKinds(snapshot);
        for (var i = 0; i < snapshot.BrainWeights.Length; i++)
        {
            state.AddBrain(new NeuralBrainGenome(snapshot.BrainWeights[i]), brainArchitectureKinds[i]);
        }

        state.Creatures.AddRange(snapshot.Creatures.Select(NormalizeCreature));
        state.Eggs.AddRange(snapshot.Eggs.Select(NormalizeEgg));
        state.Resources.AddRange(snapshot.Resources.Select(resource => NormalizeResource(resource, state.Biomes)));
        state.MarkEggsDirty();
        state.MarkResourcesDirty();
        state.RestoreLineageRecords(snapshot.LineageRecords);
        state.Stats.Restore(
            snapshot.CreatureBirthCount,
            snapshot.FounderCreatureCount,
            snapshot.CreatureDeathCount,
            snapshot.EggLaidCount,
            snapshot.EggHatchedCount,
            snapshot.EggDeathCount,
            snapshot.EggPredationDeathCount,
            snapshot.StarvationDeathCount,
            snapshot.InjuryDeathCount,
            snapshot.RottenMeatDeathCount,
            snapshot.StatsSnapshots,
            snapshot.ReproductionAttemptCount,
            snapshot.BarrenDeathCount,
            snapshot.SparseDeathCount,
            snapshot.GrasslandDeathCount,
            snapshot.RichDeathCount,
            snapshot.MaxCreatureXReached,
            snapshot.PlantDepletionCount,
            snapshot.PlantLocalDispersalCount,
            snapshot.PlantClusterRelocationCount,
            snapshot.PlantGlobalRelocationCount,
            snapshot.PlantDormancyStartedCount,
            snapshot.PlantDormancyCompletedCount,
            snapshot.PlantDormancyScheduledSecondsTotal,
            snapshot.PlantDormancyCompletedSecondsTotal,
            snapshot.ForestDeathCount,
            snapshot.WetlandDeathCount,
            snapshot.TundraDeathCount,
            snapshot.HighlandDeathCount,
            snapshot.CreatureDeathCausesByBiome);
        state.Stats.SpatialHeatmaps.Restore(snapshot.SpatialHeatmaps);
        state.Stats.RestoreDeadCreatureLifespans(snapshot.LineageRecords);
    }

    private static SimulationSnapshot Validate(SimulationSnapshot snapshot)
    {
        if (snapshot.Version != SimulationSnapshot.CurrentVersion)
        {
            throw new InvalidOperationException($"Unsupported snapshot version {snapshot.Version}.");
        }

        _ = snapshot.Scenario.Validated();

        if (snapshot.Tick < 0)
        {
            throw new InvalidOperationException("Snapshot tick cannot be negative.");
        }

        if (!double.IsFinite(snapshot.ElapsedSeconds) || snapshot.ElapsedSeconds < 0)
        {
            throw new InvalidOperationException("Snapshot elapsed seconds must be finite and non-negative.");
        }

        if (snapshot.NextEntityId <= 0)
        {
            throw new InvalidOperationException("Snapshot next entity ID must be positive.");
        }

        foreach (var genome in snapshot.Genomes)
        {
            _ = NormalizeGenome(genome).Validated();
        }

        var brainArchitectureKinds = NormalizeBrainArchitectureKinds(snapshot);
        for (var i = 0; i < snapshot.BrainWeights.Length; i++)
        {
            _ = BrainFactory.Describe(brainArchitectureKinds[i]);
            _ = new NeuralBrainGenome(snapshot.BrainWeights[i]);
        }

        _ = snapshot.Biomes.ToMap(new WorldBounds(snapshot.Scenario.WorldWidth, snapshot.Scenario.WorldHeight));
        _ = snapshot.Obstacles.ToMap(new WorldBounds(snapshot.Scenario.WorldWidth, snapshot.Scenario.WorldHeight));
        _ = snapshot.LocalFertility.ToMap(new WorldBounds(snapshot.Scenario.WorldWidth, snapshot.Scenario.WorldHeight));
        return snapshot;
    }

    private static BrainArchitectureKind[] NormalizeBrainArchitectureKinds(SimulationSnapshot snapshot)
    {
        if (snapshot.BrainArchitectureKinds.Length == 0)
        {
            return Enumerable
                .Repeat(snapshot.Scenario.BrainArchitectureKind, snapshot.BrainWeights.Length)
                .ToArray();
        }

        if (snapshot.BrainArchitectureKinds.Length != snapshot.BrainWeights.Length)
        {
            throw new InvalidOperationException("Snapshot brain architecture count must match brain weight count.");
        }

        return snapshot.BrainArchitectureKinds;
    }

    private static CreatureGenome NormalizeGenome(CreatureGenome genome)
    {
        if (genome.VisionAngleRadians <= 0f)
        {
            genome = genome with { VisionAngleRadians = CreatureGenome.Baseline.VisionAngleRadians };
        }

        if (genome.EggIncubationSeconds <= 0f)
        {
            genome = genome with { EggIncubationSeconds = CreatureGenome.Baseline.EggIncubationSeconds };
        }

        if (genome.EggProductionEnergyPerSecond <= 0f)
        {
            genome = genome with { EggProductionEnergyPerSecond = CreatureGenome.Baseline.EggProductionEnergyPerSecond };
        }

        if (genome.BiteStrength <= 0f)
        {
            genome = genome with { BiteStrength = CreatureGenome.Baseline.BiteStrength };
        }

        if (genome.GutCapacityCalories <= 0f)
        {
            genome = genome with { GutCapacityCalories = CreatureGenome.Baseline.GutCapacityCalories };
        }

        if (genome.DigestionCaloriesPerSecond <= 0f)
        {
            genome = genome with { DigestionCaloriesPerSecond = CreatureGenome.Baseline.DigestionCaloriesPerSecond };
        }

        if (genome.FatStorageCapacityCalories <= 0f)
        {
            genome = genome with { FatStorageCapacityCalories = CreatureGenome.Baseline.FatStorageCapacityCalories };
        }

        if (genome.FatStorageEfficiency <= 0f)
        {
            genome = genome with { FatStorageEfficiency = CreatureGenome.Baseline.FatStorageEfficiency };
        }

        if (genome.DamageResistance <= 0f)
        {
            genome = genome with { DamageResistance = CreatureGenome.Baseline.DamageResistance };
        }

        return genome;
    }

    private static CreatureState NormalizeCreature(CreatureState creature)
    {
        if (creature.BirthInvestmentRatio <= 0f)
        {
            creature.BirthInvestmentRatio = 1f;
        }

        if (!float.IsFinite(creature.MaxXReached) || creature.MaxXReached < creature.Position.X)
        {
            creature.MaxXReached = creature.Position.X;
        }

        if (creature.GutMeatCalories <= 0f)
        {
            creature.GutMeatQualityCalories = 0f;
        }
        else if (!float.IsFinite(creature.GutMeatQualityCalories) || creature.GutMeatQualityCalories <= 0f)
        {
            creature.GutMeatQualityCalories = creature.GutMeatCalories;
        }
        else
        {
            creature.GutMeatQualityCalories = Math.Clamp(
                creature.GutMeatQualityCalories,
                creature.GutMeatCalories * MeatQuality.MinimumFreshness,
                creature.GutMeatCalories);
        }

        return creature;
    }

    private static EggState NormalizeEgg(EggState egg)
    {
        if (egg.InvestmentRatio <= 0f)
        {
            egg.InvestmentRatio = OffspringDevelopment.InvestmentRatio(egg.Energy);
        }

        if (egg.MaxHealth <= 0f)
        {
            egg.MaxHealth = OffspringDevelopment.EggMaxHealth(egg.InvestmentRatio);
        }

        if (egg.Health <= 0f)
        {
            egg.Health = egg.MaxHealth;
        }

        return egg;
    }

    private static ResourcePatchState NormalizeResource(ResourcePatchState resource, BiomeMap? biomes = null)
    {
        if (resource.Kind != ResourceKind.Meat)
        {
            resource.MeatAgeSeconds = 0f;
            resource.HabitatBiomeKind ??= biomes?.GetKindAt(resource.Position);
            if (!float.IsFinite(resource.RespawnSecondsRemaining) || resource.RespawnSecondsRemaining < 0f)
            {
                resource.RespawnSecondsRemaining = 0f;
            }

            if (!float.IsFinite(resource.RespawnSecondsTotal) || resource.RespawnSecondsTotal < 0f)
            {
                resource.RespawnSecondsTotal = 0f;
            }

            if (resource.RespawnSecondsRemaining > 0f && resource.RespawnSecondsTotal <= 0f)
            {
                resource.RespawnSecondsTotal = resource.RespawnSecondsRemaining;
            }

            if (resource.Calories > 0f)
            {
                resource.RespawnSecondsRemaining = 0f;
                resource.RespawnSecondsTotal = 0f;
            }

            return resource;
        }

        resource.RespawnSecondsRemaining = 0f;
        resource.RespawnSecondsTotal = 0f;
        resource.HabitatBiomeKind = null;
        if (!float.IsFinite(resource.MeatAgeSeconds) || resource.MeatAgeSeconds < 0f)
        {
            resource.MeatAgeSeconds = 0f;
        }

        return resource;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        options.Converters.Add(new BiomeKindJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
