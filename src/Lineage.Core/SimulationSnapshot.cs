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

    public SmallPreyState[] SmallPrey { get; init; } = [];

    public ResourcePatchState[] Resources { get; init; } = [];

    public CreatureGenome[] Genomes { get; init; } = [];

    public float[][] BrainWeights { get; init; } = [];

    public BrainArchitectureKind[] BrainArchitectureKinds { get; init; } = [];

    public BrainSnapshot[] BrainPayloads { get; init; } = [];

    public CreatureLineageRecord[] LineageRecords { get; init; } = [];

    public SimulationStatsSnapshot[] StatsSnapshots { get; init; } = [];

    public SimulationSpatialHeatmapSnapshot SpatialHeatmaps { get; init; } = new();

    public int CreatureBirthCount { get; init; }

    public int FounderCreatureCount { get; init; }

    public int CreatureDeathCount { get; init; }

    public int EggLaidCount { get; init; }

    public int ReproductionAttemptCount { get; init; }

    public int EggHatchedCount { get; init; }

    public int EggDeathCount { get; init; }

    public int EggPredationDeathCount { get; init; }

    public int StarvationDeathCount { get; init; }

    public int InjuryDeathCount { get; init; }

    public int RottenMeatDeathCount { get; init; }

    public int OldAgeDeathCount { get; init; }

    public int BarrenDeathCount { get; init; }

    public int SparseDeathCount { get; init; }

    public int GrasslandDeathCount { get; init; }

    public int RichDeathCount { get; init; }

    public int ForestDeathCount { get; init; }

    public int WetlandDeathCount { get; init; }

    public int TundraDeathCount { get; init; }

    public int HighlandDeathCount { get; init; }

    public BiomeDeathCauseCounts CreatureDeathCausesByBiome { get; init; }

    public int PlantDepletionCount { get; init; }

    public int PlantLocalDispersalCount { get; init; }

    public int PlantClusterRelocationCount { get; init; }

    public int PlantGlobalRelocationCount { get; init; }

    public int PlantDormancyStartedCount { get; init; }

    public int PlantDormancyCompletedCount { get; init; }

    public float PlantDormancyScheduledSecondsTotal { get; init; }

    public float PlantDormancyCompletedSecondsTotal { get; init; }

    public int SmallPreySpawnedCount { get; init; }

    public int SmallPreyKilledCount { get; init; }

    public int SmallPreyEatenCount { get; init; }

    public float MaxCreatureXReached { get; init; }

    public BiomeSnapshot Biomes { get; init; } = new();

    public ObstacleSnapshot Obstacles { get; init; } = new();

    public LocalFertilitySnapshot LocalFertility { get; init; } = new();

    public TemperatureSnapshot Temperature { get; init; } = new();

    public static SimulationSnapshot Capture(
        SimulationScenario scenario,
        Simulation simulation,
        int? maxStatsSnapshots = null)
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
            SmallPrey = state.SmallPrey.ToArray(),
            Resources = state.Resources.ToArray(),
            Genomes = state.Genomes.ToArray(),
            BrainWeights = state.Brains.Select(brain => brain.Weights.ToArray()).ToArray(),
            BrainArchitectureKinds = state.Brains
                .Select((_, brainId) => state.GetBrainArchitectureKind(brainId))
                .ToArray(),
            BrainPayloads = state.Brains.Select(BrainSnapshot.Capture).ToArray(),
            LineageRecords = state.LineageRecords.ToArray(),
            StatsSnapshots = SelectStatsSnapshots(state.Stats.Snapshots, maxStatsSnapshots),
            SpatialHeatmaps = state.Stats.SpatialHeatmaps.ToSnapshot(),
            CreatureBirthCount = state.Stats.CreatureBirthCount,
            FounderCreatureCount = state.Stats.FounderCreatureCount,
            CreatureDeathCount = state.Stats.CreatureDeathCount,
            EggLaidCount = state.Stats.EggLaidCount,
            ReproductionAttemptCount = state.Stats.ReproductionAttemptCount,
            EggHatchedCount = state.Stats.EggHatchedCount,
            EggDeathCount = state.Stats.EggDeathCount,
            EggPredationDeathCount = state.Stats.EggPredationDeathCount,
            StarvationDeathCount = state.Stats.StarvationDeathCount,
            InjuryDeathCount = state.Stats.InjuryDeathCount,
            RottenMeatDeathCount = state.Stats.RottenMeatDeathCount,
            OldAgeDeathCount = state.Stats.OldAgeDeathCount,
            BarrenDeathCount = state.Stats.BarrenDeathCount,
            SparseDeathCount = state.Stats.SparseDeathCount,
            GrasslandDeathCount = state.Stats.GrasslandDeathCount,
            RichDeathCount = state.Stats.RichDeathCount,
            ForestDeathCount = state.Stats.ForestDeathCount,
            WetlandDeathCount = state.Stats.WetlandDeathCount,
            TundraDeathCount = state.Stats.TundraDeathCount,
            HighlandDeathCount = state.Stats.HighlandDeathCount,
            CreatureDeathCausesByBiome = state.Stats.CreatureDeathCausesByBiome,
            PlantDepletionCount = state.Stats.PlantDepletionCount,
            PlantLocalDispersalCount = state.Stats.PlantLocalDispersalCount,
            PlantClusterRelocationCount = state.Stats.PlantClusterRelocationCount,
            PlantGlobalRelocationCount = state.Stats.PlantGlobalRelocationCount,
            PlantDormancyStartedCount = state.Stats.PlantDormancyStartedCount,
            PlantDormancyCompletedCount = state.Stats.PlantDormancyCompletedCount,
            PlantDormancyScheduledSecondsTotal = state.Stats.PlantDormancyScheduledSecondsTotal,
            PlantDormancyCompletedSecondsTotal = state.Stats.PlantDormancyCompletedSecondsTotal,
            SmallPreySpawnedCount = state.Stats.SmallPreySpawnedCount,
            SmallPreyKilledCount = state.Stats.SmallPreyKilledCount,
            SmallPreyEatenCount = state.Stats.SmallPreyEatenCount,
            MaxCreatureXReached = state.Stats.MaxCreatureXReached,
            Biomes = BiomeSnapshot.Capture(state.Biomes),
            Obstacles = ObstacleSnapshot.Capture(state.Obstacles),
            LocalFertility = LocalFertilitySnapshot.Capture(state.LocalFertility),
            Temperature = TemperatureSnapshot.Capture(state.Temperature)
        };
    }

    private static SimulationStatsSnapshot[] SelectStatsSnapshots(
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        int? maxStatsSnapshots)
    {
        if (maxStatsSnapshots is null || snapshots.Count <= maxStatsSnapshots.Value)
        {
            return snapshots.ToArray();
        }

        if (maxStatsSnapshots.Value <= 0 || snapshots.Count == 0)
        {
            return [];
        }

        if (maxStatsSnapshots.Value == 1)
        {
            return [snapshots[^1]];
        }

        var selected = new SimulationStatsSnapshot[maxStatsSnapshots.Value];
        for (var i = 0; i < selected.Length; i++)
        {
            var index = (int)Math.Round(i * (snapshots.Count - 1) / (double)(selected.Length - 1));
            selected[i] = snapshots[index];
        }

        return selected;
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

public sealed record ObstacleSnapshot
{
    public float CellSize { get; init; } = 1f;

    public int CellCountX { get; init; } = 1;

    public int CellCountY { get; init; } = 1;

    public bool[] BlockedCells { get; init; } = [];

    public static ObstacleSnapshot Capture(ObstacleMap map)
    {
        return new ObstacleSnapshot
        {
            CellSize = map.CellSize,
            CellCountX = map.CellCountX,
            CellCountY = map.CellCountY,
            BlockedCells = map.GetCellsCopy()
        };
    }

    public ObstacleMap ToMap(WorldBounds bounds)
    {
        return BlockedCells.Length == CellCountX * CellCountY
            ? ObstacleMap.CreateFromCells(bounds, CellSize, CellCountX, CellCountY, BlockedCells)
            : ObstacleMap.CreateEmpty(bounds, MathF.Max(bounds.Width, bounds.Height));
    }
}

public sealed record LocalFertilitySnapshot
{
    public bool Enabled { get; init; }

    public float CellSize { get; init; } = 1f;

    public int CellCountX { get; init; } = 1;

    public int CellCountY { get; init; } = 1;

    public float MinimumMultiplier { get; init; } = 1f;

    public float RecoveryPerSecond { get; init; }

    public float DepletionPerPlant { get; init; }

    public float NeighborDepletionShare { get; init; }

    public float[] Multipliers { get; init; } = [1f];

    public static LocalFertilitySnapshot Capture(LocalFertilityMap map)
    {
        return new LocalFertilitySnapshot
        {
            Enabled = map.Enabled,
            CellSize = map.CellSize,
            CellCountX = map.CellCountX,
            CellCountY = map.CellCountY,
            MinimumMultiplier = map.MinimumMultiplier,
            RecoveryPerSecond = map.RecoveryPerSecond,
            DepletionPerPlant = map.DepletionPerPlant,
            NeighborDepletionShare = map.NeighborDepletionShare,
            Multipliers = map.GetCellsCopy()
        };
    }

    public LocalFertilityMap ToMap(WorldBounds bounds)
    {
        return Multipliers.Length == CellCountX * CellCountY
            ? LocalFertilityMap.CreateFromCells(
                bounds,
                Enabled,
                CellSize,
                CellCountX,
                CellCountY,
                MinimumMultiplier,
                RecoveryPerSecond,
                DepletionPerPlant,
                NeighborDepletionShare,
                Multipliers)
            : LocalFertilityMap.CreateDisabled(bounds);
    }
}

public sealed record TemperatureSnapshot
{
    public bool Enabled { get; init; }

    public float CellSize { get; init; } = 1f;

    public int CellCountX { get; init; } = 1;

    public int CellCountY { get; init; } = 1;

    public float[] Temperatures { get; init; } = [TemperatureMap.NeutralTemperature];

    public static TemperatureSnapshot Capture(TemperatureMap map)
    {
        return new TemperatureSnapshot
        {
            Enabled = map.Enabled,
            CellSize = map.CellSize,
            CellCountX = map.CellCountX,
            CellCountY = map.CellCountY,
            Temperatures = map.GetCellsCopy()
        };
    }

    public TemperatureMap ToMap(WorldBounds bounds)
    {
        return Temperatures.Length == CellCountX * CellCountY
            ? TemperatureMap.CreateFromCells(
                bounds,
                Enabled,
                CellSize,
                CellCountX,
                CellCountY,
                Temperatures)
            : TemperatureMap.CreateNeutral(bounds);
    }
}

public sealed record RestoredSimulation(SimulationScenario Scenario, Simulation Simulation);
