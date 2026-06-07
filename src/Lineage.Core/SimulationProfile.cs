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

    public SimulationNeuralControllerProfile NeuralController { get; } = new();

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

public sealed class SimulationNeuralControllerProfile
{
    public long Updates { get; internal set; }

    public long CreaturesControlled { get; internal set; }

    public long BrainlessCreatures { get; internal set; }

    public long BrainEvaluations { get; internal set; }

    public long ReusedActions { get; internal set; }

    public long ReuseDisabledEvaluations { get; internal set; }

    public long FreshWorldSenseEvaluations { get; internal set; }

    public long FirstDecisionEvaluations { get; internal set; }

    public long ImmediateCueEvaluations { get; internal set; }

    public long InternalChangeEvaluations { get; internal set; }

    public long MaxReuseAgeEvaluations { get; internal set; }

    public double ReusedActionShare => CreaturesControlled > 0
        ? ReusedActions / (double)CreaturesControlled
        : 0.0;

    internal void BeginUpdate(int creatureCount)
    {
        Updates++;
        CreaturesControlled += Math.Max(0, creatureCount);
    }

    internal void RecordBrainlessCreature()
    {
        BrainlessCreatures++;
    }

    internal void RecordReusedAction()
    {
        ReusedActions++;
    }

    internal void RecordBrainEvaluation(NeuralDecisionReason reason)
    {
        BrainEvaluations++;
        switch (reason)
        {
            case NeuralDecisionReason.ReuseDisabled:
                ReuseDisabledEvaluations++;
                break;
            case NeuralDecisionReason.FreshWorldSense:
                FreshWorldSenseEvaluations++;
                break;
            case NeuralDecisionReason.FirstDecision:
                FirstDecisionEvaluations++;
                break;
            case NeuralDecisionReason.ImmediateCue:
                ImmediateCueEvaluations++;
                break;
            case NeuralDecisionReason.InternalChange:
                InternalChangeEvaluations++;
                break;
            case NeuralDecisionReason.MaxReuseAge:
                MaxReuseAgeEvaluations++;
                break;
            case NeuralDecisionReason.ReusedAction:
                throw new ArgumentOutOfRangeException(nameof(reason), reason, "Reused actions are not brain evaluations.");
            default:
                throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unsupported neural decision reason.");
        }
    }
}

public sealed class SimulationSensingProfile
{
    public long Updates { get; internal set; }

    public long CreaturesSensed { get; internal set; }

    public long TraitCacheCreatures { get; internal set; }

    public long WorldSenseRefreshes { get; internal set; }

    public long WorldSenseScheduledRefreshes { get; internal set; }

    public long WorldSenseCloseRefreshes { get; internal set; }

    public long WorldSenseImmediateCloseRefreshes { get; internal set; }

    public long WorldSenseProximityCloseRefreshes { get; internal set; }

    public long WorldSenseForcedRefreshes { get; internal set; }

    public long WorldSenseSkippedUpdates { get; internal set; }

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

    public long CreatureCellsVisited { get; internal set; }

    public long CreatureNonEmptyCellsVisited { get; internal set; }

    public long CreatureDistanceRejectedCandidates { get; internal set; }

    public long CreatureSelfRejectedCandidates { get; internal set; }

    public long CreatureNonviableRejectedCandidates { get; internal set; }

    public long CreatureRangeRejectedCandidates { get; internal set; }

    public long CreatureVisionRejectedCandidates { get; internal set; }

    public long CreatureBodyRadiusCacheMisses { get; internal set; }

    public long ObstacleSenseSamples { get; internal set; }

    public long CreatureSetupSamples { get; internal set; }

    public long InternalStateSamples { get; internal set; }

    public long TerrainSenseSamples { get; internal set; }

    public long MemorySenseSamples { get; internal set; }

    public long SenseFinalizationSamples { get; internal set; }

    public long ResourceQueryTimestampTicks { get; internal set; }

    public long PlantResourceQueryTimestampTicks { get; internal set; }

    public long MeatResourceQueryTimestampTicks { get; internal set; }

    public long ResourceScanTimestampTicks { get; internal set; }

    public long EggQueryTimestampTicks { get; internal set; }

    public long EggScanTimestampTicks { get; internal set; }

    public long CreatureQueryTimestampTicks { get; internal set; }

    public long CreatureScanTimestampTicks { get; internal set; }

    public long TraitCacheTimestampTicks { get; internal set; }

    public long CreatureSetupTimestampTicks { get; internal set; }

    public long InternalStateTimestampTicks { get; internal set; }

    public long TerrainSenseTimestampTicks { get; internal set; }

    public long MemorySenseTimestampTicks { get; internal set; }

    public long SenseFinalizationTimestampTicks { get; internal set; }

    public long ObstacleSenseTimestampTicks { get; internal set; }

    public double TraitCacheMilliseconds => ToMilliseconds(TraitCacheTimestampTicks);

    public double CreatureSetupMilliseconds => ToMilliseconds(CreatureSetupTimestampTicks);

    public double InternalStateMilliseconds => ToMilliseconds(InternalStateTimestampTicks);

    public double TerrainSenseMilliseconds => ToMilliseconds(TerrainSenseTimestampTicks);

    public double MemorySenseMilliseconds => ToMilliseconds(MemorySenseTimestampTicks);

    public double SenseFinalizationMilliseconds => ToMilliseconds(SenseFinalizationTimestampTicks);

    public double ResourceQueryMilliseconds => ToMilliseconds(ResourceQueryTimestampTicks);

    public double PlantResourceQueryMilliseconds => ToMilliseconds(PlantResourceQueryTimestampTicks);

    public double MeatResourceQueryMilliseconds => ToMilliseconds(MeatResourceQueryTimestampTicks);

    public double ResourceScanMilliseconds => ToMilliseconds(ResourceScanTimestampTicks);

    public double EggQueryMilliseconds => ToMilliseconds(EggQueryTimestampTicks);

    public double EggScanMilliseconds => ToMilliseconds(EggScanTimestampTicks);

    public double CreatureQueryMilliseconds => ToMilliseconds(CreatureQueryTimestampTicks);

    public double CreatureScanMilliseconds => ToMilliseconds(CreatureScanTimestampTicks);

    public double ObstacleSenseMilliseconds => ToMilliseconds(ObstacleSenseTimestampTicks);

    public double TotalMeasuredMilliseconds =>
        TraitCacheMilliseconds
        + CreatureSetupMilliseconds
        + InternalStateMilliseconds
        + TerrainSenseMilliseconds
        + MemorySenseMilliseconds
        + SenseFinalizationMilliseconds
        + ResourceQueryMilliseconds
        + ResourceScanMilliseconds
        + EggQueryMilliseconds
        + EggScanMilliseconds
        + CreatureQueryMilliseconds
        + CreatureScanMilliseconds
        + ObstacleSenseMilliseconds;

    internal void BeginUpdate(int creatureCount)
    {
        Updates++;
        CreaturesSensed += Math.Max(0, creatureCount);
    }

    internal void RecordTraitCache(int creatureCount, long elapsedTimestampTicks)
    {
        TraitCacheCreatures += Math.Max(0, creatureCount);
        TraitCacheTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    internal void RecordWorldSenseRefresh(WorldSenseRefreshReason reason)
    {
        switch (reason)
        {
            case WorldSenseRefreshReason.Skipped:
                WorldSenseSkippedUpdates++;
                break;
            case WorldSenseRefreshReason.ImmediateClose:
                WorldSenseRefreshes++;
                WorldSenseCloseRefreshes++;
                WorldSenseImmediateCloseRefreshes++;
                break;
            case WorldSenseRefreshReason.ProximityClose:
                WorldSenseRefreshes++;
                WorldSenseCloseRefreshes++;
                WorldSenseProximityCloseRefreshes++;
                break;
            case WorldSenseRefreshReason.Scheduled:
                WorldSenseRefreshes++;
                WorldSenseScheduledRefreshes++;
                break;
            case WorldSenseRefreshReason.Forced:
                WorldSenseRefreshes++;
                WorldSenseForcedRefreshes++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unsupported world sense refresh reason.");
        }
    }

    internal void RecordCreatureSetup(long elapsedTimestampTicks)
    {
        CreatureSetupSamples++;
        CreatureSetupTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    internal void RecordInternalState(long elapsedTimestampTicks)
    {
        InternalStateSamples++;
        InternalStateTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    internal void RecordTerrainSense(long elapsedTimestampTicks)
    {
        TerrainSenseSamples++;
        TerrainSenseTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    internal void RecordMemorySense(long elapsedTimestampTicks)
    {
        MemorySenseSamples++;
        MemorySenseTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    internal void RecordSenseFinalization(long elapsedTimestampTicks)
    {
        SenseFinalizationSamples++;
        SenseFinalizationTimestampTicks += Math.Max(0, elapsedTimestampTicks);
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

    internal void RecordCreatureQuery(VisibleCreatureQueryResult result, long elapsedTimestampTicks)
    {
        RecordCreatureQuery(result.CandidateCount, elapsedTimestampTicks);
        CreatureCellsVisited += Math.Max(0, result.CellsVisited);
        CreatureNonEmptyCellsVisited += Math.Max(0, result.NonEmptyCellsVisited);
        CreatureDistanceRejectedCandidates += Math.Max(0, result.DistanceRejectedCount);
        CreatureSelfRejectedCandidates += Math.Max(0, result.SelfRejectedCount);
        CreatureNonviableRejectedCandidates += Math.Max(0, result.NonviableRejectedCount);
        CreatureRangeRejectedCandidates += Math.Max(0, result.RangeRejectedCount);
        CreatureVisionRejectedCandidates += Math.Max(0, result.VisionRejectedCount);
        CreatureBodyRadiusCacheMisses += Math.Max(0, result.BodyRadiusCacheMissCount);
    }

    internal void RecordCreatureScan(int visibleCreatures, long elapsedTimestampTicks)
    {
        VisibleCreatureCandidates += Math.Max(0, visibleCreatures);
        CreatureScanTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    internal void RecordObstacleSense(long elapsedTimestampTicks)
    {
        ObstacleSenseSamples++;
        ObstacleSenseTimestampTicks += Math.Max(0, elapsedTimestampTicks);
    }

    internal void MergeFrom(SimulationSensingProfile other)
    {
        ArgumentNullException.ThrowIfNull(other);
        Updates += other.Updates;
        CreaturesSensed += other.CreaturesSensed;
        TraitCacheCreatures += other.TraitCacheCreatures;
        WorldSenseRefreshes += other.WorldSenseRefreshes;
        WorldSenseScheduledRefreshes += other.WorldSenseScheduledRefreshes;
        WorldSenseCloseRefreshes += other.WorldSenseCloseRefreshes;
        WorldSenseImmediateCloseRefreshes += other.WorldSenseImmediateCloseRefreshes;
        WorldSenseProximityCloseRefreshes += other.WorldSenseProximityCloseRefreshes;
        WorldSenseForcedRefreshes += other.WorldSenseForcedRefreshes;
        WorldSenseSkippedUpdates += other.WorldSenseSkippedUpdates;
        ResourceQueries += other.ResourceQueries;
        ResourceCandidates += other.ResourceCandidates;
        PlantResourceQueries += other.PlantResourceQueries;
        PlantResourceQueryCandidates += other.PlantResourceQueryCandidates;
        MeatResourceQueries += other.MeatResourceQueries;
        MeatResourceQueryCandidates += other.MeatResourceQueryCandidates;
        PlantCandidates += other.PlantCandidates;
        MeatResourceCandidates += other.MeatResourceCandidates;
        VisiblePlantCandidates += other.VisiblePlantCandidates;
        VisibleMeatResourceCandidates += other.VisibleMeatResourceCandidates;
        EggQueries += other.EggQueries;
        EggCandidates += other.EggCandidates;
        VisibleEggCandidates += other.VisibleEggCandidates;
        CreatureQueries += other.CreatureQueries;
        CreatureCandidates += other.CreatureCandidates;
        VisibleCreatureCandidates += other.VisibleCreatureCandidates;
        CreatureCellsVisited += other.CreatureCellsVisited;
        CreatureNonEmptyCellsVisited += other.CreatureNonEmptyCellsVisited;
        CreatureDistanceRejectedCandidates += other.CreatureDistanceRejectedCandidates;
        CreatureSelfRejectedCandidates += other.CreatureSelfRejectedCandidates;
        CreatureNonviableRejectedCandidates += other.CreatureNonviableRejectedCandidates;
        CreatureRangeRejectedCandidates += other.CreatureRangeRejectedCandidates;
        CreatureVisionRejectedCandidates += other.CreatureVisionRejectedCandidates;
        CreatureBodyRadiusCacheMisses += other.CreatureBodyRadiusCacheMisses;
        ObstacleSenseSamples += other.ObstacleSenseSamples;
        CreatureSetupSamples += other.CreatureSetupSamples;
        InternalStateSamples += other.InternalStateSamples;
        TerrainSenseSamples += other.TerrainSenseSamples;
        MemorySenseSamples += other.MemorySenseSamples;
        SenseFinalizationSamples += other.SenseFinalizationSamples;
        ResourceQueryTimestampTicks += other.ResourceQueryTimestampTicks;
        PlantResourceQueryTimestampTicks += other.PlantResourceQueryTimestampTicks;
        MeatResourceQueryTimestampTicks += other.MeatResourceQueryTimestampTicks;
        ResourceScanTimestampTicks += other.ResourceScanTimestampTicks;
        EggQueryTimestampTicks += other.EggQueryTimestampTicks;
        EggScanTimestampTicks += other.EggScanTimestampTicks;
        CreatureQueryTimestampTicks += other.CreatureQueryTimestampTicks;
        CreatureScanTimestampTicks += other.CreatureScanTimestampTicks;
        TraitCacheTimestampTicks += other.TraitCacheTimestampTicks;
        CreatureSetupTimestampTicks += other.CreatureSetupTimestampTicks;
        InternalStateTimestampTicks += other.InternalStateTimestampTicks;
        TerrainSenseTimestampTicks += other.TerrainSenseTimestampTicks;
        MemorySenseTimestampTicks += other.MemorySenseTimestampTicks;
        SenseFinalizationTimestampTicks += other.SenseFinalizationTimestampTicks;
        ObstacleSenseTimestampTicks += other.ObstacleSenseTimestampTicks;
    }

    private static double ToMilliseconds(long elapsedTimestampTicks)
    {
        return elapsedTimestampTicks * 1000.0 / Stopwatch.Frequency;
    }
}

internal enum WorldSenseRefreshReason
{
    Skipped,
    Scheduled,
    ImmediateClose,
    ProximityClose,
    Forced
}
