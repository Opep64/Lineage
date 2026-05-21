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

        File.WriteAllText(path, ToJson(snapshot));
    }

    public static SimulationSnapshot Load(string path)
    {
        return FromJson(File.ReadAllText(path));
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

        state.Biomes = snapshot.Biomes.ToMap(state.Bounds);
        state.Random.State = snapshot.RandomState;
        state.RestoreClock(snapshot.Tick, snapshot.ElapsedSeconds);
        state.RestoreNextEntityId(snapshot.NextEntityId);

        foreach (var genome in snapshot.Genomes)
        {
            state.Genomes.Add(NormalizeGenome(genome).Validated());
        }

        foreach (var weights in snapshot.BrainWeights)
        {
            state.Brains.Add(new NeuralBrainGenome(weights));
        }

        state.Creatures.AddRange(snapshot.Creatures.Select(NormalizeCreature));
        state.Eggs.AddRange(snapshot.Eggs.Select(NormalizeEgg));
        state.Resources.AddRange(snapshot.Resources);
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
            snapshot.StatsSnapshots);
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

        foreach (var weights in snapshot.BrainWeights)
        {
            _ = new NeuralBrainGenome(weights);
        }

        _ = snapshot.Biomes.ToMap(new WorldBounds(snapshot.Scenario.WorldWidth, snapshot.Scenario.WorldHeight));
        return snapshot;
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

        return genome;
    }

    private static CreatureState NormalizeCreature(CreatureState creature)
    {
        if (creature.BirthInvestmentRatio <= 0f)
        {
            creature.BirthInvestmentRatio = 1f;
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

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
