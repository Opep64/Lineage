namespace Lineage.Core;

/// <summary>
/// Complete serializable state for pausing a run and restoring it later.
/// </summary>
///
/// <remarks>
/// A snapshot is intentionally distinct from a scenario. The scenario describes how
/// to create a run; the snapshot captures one exact moment inside that run.
/// </remarks>
public sealed record SimulationSnapshot
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public SimulationScenario Scenario { get; init; } = new();

    public long Tick { get; init; }

    public double ElapsedSeconds { get; init; }

    public ulong RandomState { get; init; }

    public int NextEntityId { get; init; }

    public CreatureState[] Creatures { get; init; } = [];

    public EggState[] Eggs { get; init; } = [];

    public ResourcePatchState[] Resources { get; init; } = [];

    public CreatureGenome[] Genomes { get; init; } = [];

    public float[][] BrainWeights { get; init; } = [];

    public CreatureLineageRecord[] LineageRecords { get; init; } = [];

    public SimulationStatsSnapshot[] StatsSnapshots { get; init; } = [];

    public int CreatureBirthCount { get; init; }

    public int FounderCreatureCount { get; init; }

    public int CreatureDeathCount { get; init; }

    public int EggLaidCount { get; init; }

    public int EggHatchedCount { get; init; }

    public int EggDeathCount { get; init; }

    public int EggPredationDeathCount { get; init; }

    public int StarvationDeathCount { get; init; }

    public int InjuryDeathCount { get; init; }

    public BiomeSnapshot Biomes { get; init; } = new();

    public static SimulationSnapshot Capture(SimulationScenario scenario, Simulation simulation)
    {
        var state = simulation.State;
        return new SimulationSnapshot
        {
            Scenario = scenario.Validated(),
            Tick = state.Tick,
            ElapsedSeconds = state.ElapsedSeconds,
            RandomState = state.Random.State,
            NextEntityId = state.NextEntityId,
            Creatures = state.Creatures.ToArray(),
            Eggs = state.Eggs.ToArray(),
            Resources = state.Resources.ToArray(),
            Genomes = state.Genomes.ToArray(),
            BrainWeights = state.Brains.Select(brain => brain.Weights.ToArray()).ToArray(),
            LineageRecords = state.LineageRecords.ToArray(),
            StatsSnapshots = state.Stats.Snapshots.ToArray(),
            CreatureBirthCount = state.Stats.CreatureBirthCount,
            FounderCreatureCount = state.Stats.FounderCreatureCount,
            CreatureDeathCount = state.Stats.CreatureDeathCount,
            EggLaidCount = state.Stats.EggLaidCount,
            EggHatchedCount = state.Stats.EggHatchedCount,
            EggDeathCount = state.Stats.EggDeathCount,
            EggPredationDeathCount = state.Stats.EggPredationDeathCount,
            StarvationDeathCount = state.Stats.StarvationDeathCount,
            InjuryDeathCount = state.Stats.InjuryDeathCount,
            Biomes = BiomeSnapshot.Capture(state.Biomes)
        };
    }
}

public sealed record BiomeSnapshot
{
    public float CellSize { get; init; } = 1f;

    public float ResourceVoidBorderWidth { get; init; }

    public int CellCountX { get; init; } = 1;

    public int CellCountY { get; init; } = 1;

    public BiomeKind[] Cells { get; init; } = [BiomeKind.Grassland];

    public static BiomeSnapshot Capture(BiomeMap map)
    {
        return new BiomeSnapshot
        {
            CellSize = map.CellSize,
            ResourceVoidBorderWidth = map.ResourceVoidBorderWidth,
            CellCountX = map.CellCountX,
            CellCountY = map.CellCountY,
            Cells = map.GetCellsCopy()
        };
    }

    public BiomeMap ToMap(WorldBounds bounds)
    {
        return BiomeMap.CreateFromCells(bounds, CellSize, CellCountX, CellCountY, Cells, ResourceVoidBorderWidth);
    }
}

public sealed record RestoredSimulation(SimulationScenario Scenario, Simulation Simulation);
