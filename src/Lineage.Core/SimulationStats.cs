namespace Lineage.Core;

/// <summary>
/// Aggregated counters and sampled snapshots for a simulation run.
/// </summary>
public sealed class SimulationStats
{
    private readonly List<float> _deadCreatureLifespans = [];
    private float _deadCreatureLifespanTotalSeconds;

    public int CreatureBirthCount { get; private set; }

    public int FounderCreatureCount { get; private set; }

    public int CreatureDeathCount { get; private set; }

    public int EggLaidCount { get; private set; }

    public int ReproductionAttemptCount { get; private set; }

    public int EggHatchedCount { get; private set; }

    public int EggDeathCount { get; private set; }

    public int EggPredationDeathCount { get; private set; }

    public int StarvationDeathCount { get; private set; }

    public int InjuryDeathCount { get; private set; }

    public int RottenMeatDeathCount { get; private set; }

    public int BarrenDeathCount { get; private set; }

    public int SparseDeathCount { get; private set; }

    public int GrasslandDeathCount { get; private set; }

    public int RichDeathCount { get; private set; }

    public int ForestDeathCount { get; private set; }

    public int WetlandDeathCount { get; private set; }

    public int TundraDeathCount { get; private set; }

    public int HighlandDeathCount { get; private set; }

    public BiomeDeathCauseCounts CreatureDeathCausesByBiome { get; private set; }

    public SimulationSpatialHeatmaps SpatialHeatmaps { get; } = new();

    public int PlantDepletionCount { get; private set; }

    public int PlantLocalDispersalCount { get; private set; }

    public int PlantClusterRelocationCount { get; private set; }

    public int PlantGlobalRelocationCount { get; private set; }

    public int PlantDormancyStartedCount { get; private set; }

    public int PlantDormancyCompletedCount { get; private set; }

    public float PlantDormancyScheduledSecondsTotal { get; private set; }

    public float PlantDormancyCompletedSecondsTotal { get; private set; }

    public float MaxCreatureXReached { get; private set; }

    public float AverageDeadCreatureLifespanSeconds => _deadCreatureLifespans.Count == 0
        ? 0f
        : _deadCreatureLifespanTotalSeconds / _deadCreatureLifespans.Count;

    public float MedianDeadCreatureLifespanSeconds
    {
        get
        {
            if (_deadCreatureLifespans.Count == 0)
            {
                return 0f;
            }

            var middle = _deadCreatureLifespans.Count / 2;
            return _deadCreatureLifespans.Count % 2 == 1
                ? _deadCreatureLifespans[middle]
                : (_deadCreatureLifespans[middle - 1] + _deadCreatureLifespans[middle]) * 0.5f;
        }
    }

    public List<SimulationStatsSnapshot> Snapshots { get; } = [];

    public float AveragePlantDormancyScheduledSeconds => PlantDormancyStartedCount == 0
        ? 0f
        : PlantDormancyScheduledSecondsTotal / PlantDormancyStartedCount;

    public float AveragePlantDormancyCompletedSeconds => PlantDormancyCompletedCount == 0
        ? 0f
        : PlantDormancyCompletedSecondsTotal / PlantDormancyCompletedCount;

    internal void RecordCreatureBirth(CreatureLineageRecord record, WorldBounds bounds)
    {
        CreatureBirthCount++;
        RecordEastwardProgress(record.MaxXReached);
        SpatialHeatmaps.RecordBirth(bounds, record.BirthPosition);

        if (record.IsFounder)
        {
            FounderCreatureCount++;
        }
    }

    internal void RecordCreatureDeath(
        CreatureDeathReason reason,
        float lifespanSeconds,
        BiomeKind biome,
        WorldBounds bounds,
        SimVector2 position)
    {
        CreatureDeathCount++;
        AddDeadCreatureLifespan(lifespanSeconds);
        CreatureDeathCausesByBiome = CreatureDeathCausesByBiome.Add(biome, reason);
        SpatialHeatmaps.RecordDeath(bounds, position, reason);

        switch (reason)
        {
            case CreatureDeathReason.Starvation:
                StarvationDeathCount++;
                break;
            case CreatureDeathReason.Injury:
                InjuryDeathCount++;
                break;
            case CreatureDeathReason.RottenMeat:
                RottenMeatDeathCount++;
                break;
        }

        switch (BiomeKinds.Canonicalize(biome))
        {
            case BiomeKind.Desert:
                BarrenDeathCount++;
                break;
            case BiomeKind.Scrubland:
                SparseDeathCount++;
                break;
            case BiomeKind.Fertile:
                RichDeathCount++;
                break;
            case BiomeKind.Forest:
                ForestDeathCount++;
                break;
            case BiomeKind.Wetland:
                WetlandDeathCount++;
                break;
            case BiomeKind.Tundra:
                TundraDeathCount++;
                break;
            case BiomeKind.Highland:
                HighlandDeathCount++;
                break;
            default:
                GrasslandDeathCount++;
                break;
        }
    }

    internal void RecordEastwardProgress(float x)
    {
        if (float.IsFinite(x))
        {
            MaxCreatureXReached = Math.Max(MaxCreatureXReached, x);
        }
    }

    internal void RecordEggLaid()
    {
        EggLaidCount++;
    }

    internal void RecordReproductionAttempt()
    {
        ReproductionAttemptCount++;
    }

    internal void RecordEggHatched()
    {
        EggHatchedCount++;
    }

    internal void RecordEggDeath(EggDeathReason reason = EggDeathReason.Unknown)
    {
        EggDeathCount++;
        if (reason == EggDeathReason.Predation)
        {
            EggPredationDeathCount++;
        }
    }

    internal void RecordFoodEaten(WorldBounds bounds, SimVector2 position, ResourceKind kind, float calories)
    {
        SpatialHeatmaps.RecordFoodEaten(bounds, position, kind, calories);
    }

    internal void RecordEggEaten(WorldBounds bounds, SimVector2 position, float calories)
    {
        SpatialHeatmaps.RecordEggEaten(bounds, position, calories);
    }

    internal void RecordAttackDamage(WorldBounds bounds, SimVector2 position, float damage)
    {
        SpatialHeatmaps.RecordAttackDamage(bounds, position, damage);
    }

    internal void RecordPlantDepletion()
    {
        PlantDepletionCount++;
    }

    internal void RecordPlantRelocation(PlantPlacementMode placementMode)
    {
        switch (placementMode)
        {
            case PlantPlacementMode.LocalDispersal:
                PlantLocalDispersalCount++;
                break;
            case PlantPlacementMode.Cluster:
                PlantClusterRelocationCount++;
                break;
            default:
                PlantGlobalRelocationCount++;
                break;
        }
    }

    internal void RecordPlantDormancyStarted(float scheduledSeconds)
    {
        PlantDormancyStartedCount++;
        PlantDormancyScheduledSecondsTotal += NormalizeDuration(scheduledSeconds);
    }

    internal void RecordPlantDormancyCompleted(float scheduledSeconds)
    {
        PlantDormancyCompletedCount++;
        PlantDormancyCompletedSecondsTotal += NormalizeDuration(scheduledSeconds);
    }

    internal void RecordSnapshot(SimulationStatsSnapshot snapshot)
    {
        Snapshots.Add(snapshot);
    }

    internal void RestoreDeadCreatureLifespans(IEnumerable<CreatureLineageRecord> lineageRecords)
    {
        _deadCreatureLifespans.Clear();
        _deadCreatureLifespanTotalSeconds = 0f;

        foreach (var record in lineageRecords)
        {
            if (record.DeathElapsedSeconds is null)
            {
                continue;
            }

            AddDeadCreatureLifespan(MathF.Max(0f, (float)(record.DeathElapsedSeconds.Value - record.BirthElapsedSeconds)));
        }
    }

    internal void Restore(
        int creatureBirthCount,
        int founderCreatureCount,
        int creatureDeathCount,
        int eggLaidCount,
        int eggHatchedCount,
        int eggDeathCount,
        int eggPredationDeathCount,
        int starvationDeathCount,
        int injuryDeathCount,
        int rottenMeatDeathCount,
        IEnumerable<SimulationStatsSnapshot> snapshots,
        int reproductionAttemptCount = 0,
        int barrenDeathCount = 0,
        int sparseDeathCount = 0,
        int grasslandDeathCount = 0,
        int richDeathCount = 0,
        float maxCreatureXReached = 0f,
        int plantDepletionCount = 0,
        int plantLocalDispersalCount = 0,
        int plantClusterRelocationCount = 0,
        int plantGlobalRelocationCount = 0,
        int plantDormancyStartedCount = 0,
        int plantDormancyCompletedCount = 0,
        float plantDormancyScheduledSecondsTotal = 0f,
        float plantDormancyCompletedSecondsTotal = 0f,
        int forestDeathCount = 0,
        int wetlandDeathCount = 0,
        int tundraDeathCount = 0,
        int highlandDeathCount = 0,
        BiomeDeathCauseCounts creatureDeathCausesByBiome = default)
    {
        CreatureBirthCount = creatureBirthCount;
        FounderCreatureCount = founderCreatureCount;
        CreatureDeathCount = creatureDeathCount;
        EggLaidCount = eggLaidCount;
        ReproductionAttemptCount = reproductionAttemptCount;
        EggHatchedCount = eggHatchedCount;
        EggDeathCount = eggDeathCount;
        EggPredationDeathCount = eggPredationDeathCount;
        StarvationDeathCount = starvationDeathCount;
        InjuryDeathCount = injuryDeathCount;
        RottenMeatDeathCount = rottenMeatDeathCount;
        BarrenDeathCount = barrenDeathCount;
        SparseDeathCount = sparseDeathCount;
        GrasslandDeathCount = grasslandDeathCount;
        RichDeathCount = richDeathCount;
        ForestDeathCount = forestDeathCount;
        WetlandDeathCount = wetlandDeathCount;
        TundraDeathCount = tundraDeathCount;
        HighlandDeathCount = highlandDeathCount;
        CreatureDeathCausesByBiome = creatureDeathCausesByBiome;
        PlantDepletionCount = plantDepletionCount;
        PlantLocalDispersalCount = plantLocalDispersalCount;
        PlantClusterRelocationCount = plantClusterRelocationCount;
        PlantGlobalRelocationCount = plantGlobalRelocationCount;
        PlantDormancyStartedCount = plantDormancyStartedCount;
        PlantDormancyCompletedCount = plantDormancyCompletedCount;
        PlantDormancyScheduledSecondsTotal = NormalizeDuration(plantDormancyScheduledSecondsTotal);
        PlantDormancyCompletedSecondsTotal = NormalizeDuration(plantDormancyCompletedSecondsTotal);
        MaxCreatureXReached = float.IsFinite(maxCreatureXReached) && maxCreatureXReached > 0f
            ? maxCreatureXReached
            : 0f;
        Snapshots.Clear();
        Snapshots.AddRange(snapshots);
        SpatialHeatmaps.Restore(null);
    }

    private void AddDeadCreatureLifespan(float lifespanSeconds)
    {
        var normalized = float.IsFinite(lifespanSeconds) && lifespanSeconds > 0f
            ? lifespanSeconds
            : 0f;
        var insertIndex = _deadCreatureLifespans.BinarySearch(normalized);
        if (insertIndex < 0)
        {
            insertIndex = ~insertIndex;
        }

        _deadCreatureLifespans.Insert(insertIndex, normalized);
        _deadCreatureLifespanTotalSeconds += normalized;
    }

    private static float NormalizeDuration(float seconds)
    {
        return float.IsFinite(seconds) && seconds > 0f
            ? seconds
            : 0f;
    }
}
