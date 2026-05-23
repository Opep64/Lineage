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

    public int BarrenDeathCount { get; private set; }

    public int SparseDeathCount { get; private set; }

    public int GrasslandDeathCount { get; private set; }

    public int RichDeathCount { get; private set; }

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

    internal void RecordCreatureBirth(CreatureLineageRecord record)
    {
        CreatureBirthCount++;

        if (record.IsFounder)
        {
            FounderCreatureCount++;
        }
    }

    internal void RecordCreatureDeath(CreatureDeathReason reason, float lifespanSeconds, BiomeKind biome)
    {
        CreatureDeathCount++;
        AddDeadCreatureLifespan(lifespanSeconds);

        switch (reason)
        {
            case CreatureDeathReason.Starvation:
                StarvationDeathCount++;
                break;
            case CreatureDeathReason.Injury:
                InjuryDeathCount++;
                break;
        }

        switch (biome)
        {
            case BiomeKind.Barren:
                BarrenDeathCount++;
                break;
            case BiomeKind.Sparse:
                SparseDeathCount++;
                break;
            case BiomeKind.Rich:
                RichDeathCount++;
                break;
            default:
                GrasslandDeathCount++;
                break;
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
        IEnumerable<SimulationStatsSnapshot> snapshots,
        int reproductionAttemptCount = 0,
        int barrenDeathCount = 0,
        int sparseDeathCount = 0,
        int grasslandDeathCount = 0,
        int richDeathCount = 0)
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
        BarrenDeathCount = barrenDeathCount;
        SparseDeathCount = sparseDeathCount;
        GrasslandDeathCount = grasslandDeathCount;
        RichDeathCount = richDeathCount;
        Snapshots.Clear();
        Snapshots.AddRange(snapshots);
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
}
