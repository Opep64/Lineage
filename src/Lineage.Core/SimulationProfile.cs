using System.Diagnostics;

namespace Lineage.Core;

/// <summary>
/// Optional per-system timing data for a simulation run.
/// </summary>
///
/// <remarks>
/// Profiling is opt-in. A normal simulation keeps this object null and pays no
/// per-system timing overhead; CLI/debug runs can attach one when they need a
/// quick view of where update time is going.
/// </remarks>
public sealed class SimulationProfile
{
    private SimulationSystemProfile[] _systems = [];

    public bool IsActive { get; set; } = true;

    public long ProfiledSteps { get; private set; }

    public IReadOnlyList<SimulationSystemProfile> Systems => _systems;

    public SimulationSensingProfile Sensing { get; } = new();

    public double TotalMilliseconds => _systems.Sum(system => system.TotalMilliseconds);

    internal void EnsureSystems(IReadOnlyList<ISimulationSystem> systems)
    {
        if (_systems.Length == systems.Count)
        {
            return;
        }

        _systems = new SimulationSystemProfile[systems.Count];
        for (var i = 0; i < systems.Count; i++)
        {
            _systems[i] = new SimulationSystemProfile(systems[i].GetType().Name);
        }
    }

    internal void BeginStep()
    {
        ProfiledSteps++;
    }

    internal void RecordSystem(int index, long elapsedTimestampTicks)
    {
        var system = _systems[index];
        system.CallCount++;
        system.ElapsedTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }
}

public sealed class SimulationSystemProfile(string systemName)
{
    public string SystemName { get; } = systemName;

    public long CallCount { get; internal set; }

    public long ElapsedTimestampTicks { get; internal set; }

    public double TotalMilliseconds => ElapsedTimestampTicks * 1000.0 / Stopwatch.Frequency;

    public double AverageMillisecondsPerCall => CallCount > 0
        ? TotalMilliseconds / CallCount
        : 0.0;
}

public sealed class SimulationSensingProfile
{
    public long Updates { get; internal set; }

    public long CreaturesSensed { get; internal set; }

    public long ResourceQueries { get; internal set; }

    public long ResourceCandidates { get; internal set; }

    public long PlantResourceQueries { get; internal set; }

    public long PlantResourceQueryCandidates { get; internal set; }

    public long MeatResourceQueries { get; internal set; }

    public long MeatResourceQueryCandidates { get; internal set; }

    public long PlantCandidates { get; internal set; }

    public long MeatResourceCandidates { get; internal set; }

    public long VisiblePlantCandidates { get; internal set; }

    public long VisibleMeatResourceCandidates { get; internal set; }

    public long EggQueries { get; internal set; }

    public long EggCandidates { get; internal set; }

    public long VisibleEggCandidates { get; internal set; }

    public long CreatureQueries { get; internal set; }

    public long CreatureCandidates { get; internal set; }

    public long VisibleCreatureCandidates { get; internal set; }

    public long ResourceQueryTimestampTicks { get; internal set; }

    public long PlantResourceQueryTimestampTicks { get; internal set; }

    public long MeatResourceQueryTimestampTicks { get; internal set; }

    public long ResourceScanTimestampTicks { get; internal set; }

    public long EggQueryTimestampTicks { get; internal set; }

    public long EggScanTimestampTicks { get; internal set; }

    public long CreatureQueryTimestampTicks { get; internal set; }

    public long CreatureScanTimestampTicks { get; internal set; }

    public double ResourceQueryMilliseconds => ToMilliseconds(ResourceQueryTimestampTicks);

    public double PlantResourceQueryMilliseconds => ToMilliseconds(PlantResourceQueryTimestampTicks);

    public double MeatResourceQueryMilliseconds => ToMilliseconds(MeatResourceQueryTimestampTicks);

    public double ResourceScanMilliseconds => ToMilliseconds(ResourceScanTimestampTicks);

    public double EggQueryMilliseconds => ToMilliseconds(EggQueryTimestampTicks);

    public double EggScanMilliseconds => ToMilliseconds(EggScanTimestampTicks);

    public double CreatureQueryMilliseconds => ToMilliseconds(CreatureQueryTimestampTicks);

    public double CreatureScanMilliseconds => ToMilliseconds(CreatureScanTimestampTicks);

    public double TotalMeasuredMilliseconds =>
        ResourceQueryMilliseconds
        + ResourceScanMilliseconds
        + EggQueryMilliseconds
        + EggScanMilliseconds
        + CreatureQueryMilliseconds
        + CreatureScanMilliseconds;

    internal void BeginUpdate(int creatureCount)
    {
        Updates++;
        CreaturesSensed += Math.Max(0, creatureCount);
    }

    internal void RecordResourceQuery(int candidateCount, long elapsedTimestampTicks)
    {
        ResourceQueries++;
        ResourceCandidates += Math.Max(0, candidateCount);
        ResourceQueryTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    internal void RecordPlantResourceQuery(int candidateCount, long elapsedTimestampTicks)
    {
        PlantResourceQueries++;
        PlantResourceQueryCandidates += Math.Max(0, candidateCount);
        RecordResourceQuery(candidateCount, elapsedTimestampTicks);
        PlantResourceQueryTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    internal void RecordMeatResourceQuery(int candidateCount, long elapsedTimestampTicks)
    {
        MeatResourceQueries++;
        MeatResourceQueryCandidates += Math.Max(0, candidateCount);
        RecordResourceQuery(candidateCount, elapsedTimestampTicks);
        MeatResourceQueryTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    internal void RecordSplitResourceQuery(int plantCandidateCount, int meatCandidateCount, long elapsedTimestampTicks)
    {
        var safePlantCandidateCount = Math.Max(0, plantCandidateCount);
        var safeMeatCandidateCount = Math.Max(0, meatCandidateCount);
        var safeElapsed = Math.Max(0, elapsedTimestampTicks);

        ResourceQueries++;
        ResourceCandidates += safePlantCandidateCount + safeMeatCandidateCount;
        ResourceQueryTimestampTicks += safeElapsed;

        PlantResourceQueries++;
        PlantResourceQueryCandidates += safePlantCandidateCount;
        MeatResourceQueries++;
        MeatResourceQueryCandidates += safeMeatCandidateCount;
    }

    internal void RecordResourceScan(
        int plantCandidates,
        int meatCandidates,
        int visiblePlants,
        int visibleMeatResources,
        long elapsedTimestampTicks)
    {
        PlantCandidates += Math.Max(0, plantCandidates);
        MeatResourceCandidates += Math.Max(0, meatCandidates);
        VisiblePlantCandidates += Math.Max(0, visiblePlants);
        VisibleMeatResourceCandidates += Math.Max(0, visibleMeatResources);
        ResourceScanTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    internal void RecordEggQuery(int candidateCount, long elapsedTimestampTicks)
    {
        EggQueries++;
        EggCandidates += Math.Max(0, candidateCount);
        EggQueryTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    internal void RecordEggScan(int visibleEggs, long elapsedTimestampTicks)
    {
        VisibleEggCandidates += Math.Max(0, visibleEggs);
        EggScanTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    internal void RecordCreatureQuery(int candidateCount, long elapsedTimestampTicks)
    {
        CreatureQueries++;
        CreatureCandidates += Math.Max(0, candidateCount);
        CreatureQueryTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    internal void RecordCreatureScan(int visibleCreatures, long elapsedTimestampTicks)
    {
        VisibleCreatureCandidates += Math.Max(0, visibleCreatures);
        CreatureScanTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    private static double ToMilliseconds(long elapsedTimestampTicks)
    {
        return elapsedTimestampTicks * 1000.0 / Stopwatch.Frequency;
    }
}
