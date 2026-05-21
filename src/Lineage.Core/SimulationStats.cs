namespace Lineage.Core;

/// <summary>
/// Aggregated counters and sampled snapshots for a simulation run.
/// </summary>
public sealed class SimulationStats
{
    public int CreatureBirthCount { get; private set; }

    public int FounderCreatureCount { get; private set; }

    public int CreatureDeathCount { get; private set; }

    public int EggLaidCount { get; private set; }

    public int EggHatchedCount { get; private set; }

    public int EggDeathCount { get; private set; }

    public int EggPredationDeathCount { get; private set; }

    public int StarvationDeathCount { get; private set; }

    public int InjuryDeathCount { get; private set; }

    public List<SimulationStatsSnapshot> Snapshots { get; } = [];

    internal void RecordCreatureBirth(CreatureLineageRecord record)
    {
        CreatureBirthCount++;

        if (record.IsFounder)
        {
            FounderCreatureCount++;
        }
    }

    internal void RecordCreatureDeath(CreatureDeathReason reason)
    {
        CreatureDeathCount++;

        switch (reason)
        {
            case CreatureDeathReason.Starvation:
                StarvationDeathCount++;
                break;
            case CreatureDeathReason.Injury:
                InjuryDeathCount++;
                break;
        }
    }

    internal void RecordEggLaid()
    {
        EggLaidCount++;
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
        IEnumerable<SimulationStatsSnapshot> snapshots)
    {
        CreatureBirthCount = creatureBirthCount;
        FounderCreatureCount = founderCreatureCount;
        CreatureDeathCount = creatureDeathCount;
        EggLaidCount = eggLaidCount;
        EggHatchedCount = eggHatchedCount;
        EggDeathCount = eggDeathCount;
        EggPredationDeathCount = eggPredationDeathCount;
        StarvationDeathCount = starvationDeathCount;
        InjuryDeathCount = injuryDeathCount;
        Snapshots.Clear();
        Snapshots.AddRange(snapshots);
    }
}
