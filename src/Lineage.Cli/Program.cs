using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json.Nodes;
using Lineage.Core;

try
{
    var options = RunOptions.Parse(args);
    if (options.ShowHelp)
    {
        PrintHelp();
        return;
    }

    if (options.IsProbe)
    {
        var results = RunProbe(options);
        var outputPath = options.ProbeOutputPath ?? Path.Combine("out", "probe_summary.csv");
        var reportPath = options.ProbeReportPath ?? Path.ChangeExtension(outputPath, ".html");
        ProbeCsvWriter.Write(outputPath, results);
        ProbeReportWriter.Write(reportPath, options, results);
        PrintProbeSummary(options, results, outputPath, reportPath);
        return;
    }

    if (options.IsBatch)
    {
        var results = RunBatch(options);
        var reportPath = options.BatchReportPath ?? Path.Combine(options.BatchOutputDirectory, "comparison_report.html");
        BatchComparisonReportWriter.Write(reportPath, options, results);
        PrintBatchSummary(options, results, reportPath);
        return;
    }

    var result = RunSingle(options);
    PrintSummary(result);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}

static void PrintHelp()
{
    Console.WriteLine(
        """
        Lineage.Cli - headless simulation runner

        Options:
          --scenario <path>          Load scenario JSON.
          --load-snapshot <path>     Load a saved simulation snapshot instead of starting from a scenario.
          --save-scenario <path>     Save the resolved scenario JSON before running.
          --ticks <n>                Number of simulation ticks to run. Default: 5000
          --seed <n>                 Override scenario seed.
          --pipeline <neural|simple> Override controller pipeline.
          --creatures <n>            Override initial creature count.
          --spatial-cell-size <n>    Override spatial index cell size.
          --resources-per-million-area <n> Override initial resource density.
          --resources <n>            Legacy absolute resource count override; converted to density.
          --snapshot-interval <n>    Override stats snapshot interval.
          --output <path>            Stats CSV output path.
          --lineage-output <path>    Lineage event CSV output path.
          --traits-output <path>     Final trait summary CSV output path.
          --founders-output <path>   Founder lineage summary CSV output path.
          --generations-output <path> Generation survival summary CSV output path.
          --lineage-trends-output <path> Founder lineage trend CSV output path.
          --report <path>            HTML run report output path.
          --profile                  Time each simulation system and print/write a profile.
          --profile-output <path>    Per-system profile CSV output path; writes a sensing sidecar.
          --profile-start-tick <n>   Start profiling after n completed ticks. Default: 0
          --profile-end-tick <n>     Stop profiling after n completed ticks.
          --save-snapshot <path>     Save final simulation snapshot JSON.
          --checkpoint-interval <n>  Save loadable snapshot checkpoints every n ticks.
          --checkpoint-dir <dir>      Directory for checkpoint JSON files.
          --inject-species <path>    Inject a species profile JSON, usually species/name.species.json. Can repeat.
          --inject-species-count <n> Founder count per injected profile. Default: 10
          --inject-species-region <region> Spawn region for injected species. Default: uniform
          --inject-species-energy <n> Override starting energy for injected founders.
          --export-species <path>    Export a species profile, usually species/name.species.json.
          --export-species-creature <id> Export this living creature ID instead of the dominant lineage.
          --export-species-founder <id> Export a representative from this founder lineage.
          --export-species-name <text> Name for the exported species profile.
          --export-species-notes <text> Notes for the exported species profile.
          --batch-scenario <path>    Add a scenario to a batch comparison. Can repeat.
          --batch-report <path>      HTML comparison report output path.
          --batch-output-dir <dir>   Per-run batch output directory. Default: out/batch
          --probe                    Run a lightweight multi-scenario tuning probe.
          --probe-scenario <path>    Add a scenario to a lightweight probe. Can repeat.
          --probe-seeds <a,b,c>      Comma-separated seed overrides for probe runs.
          --probe-variant <name:key=value,...> Add a temporary scenario variant. Can repeat; base also runs.
          --probe-output <path>      Compact probe CSV output path. Default: out/probe_summary.csv
          --probe-report <path>      Compact probe HTML output path. Default: probe output with .html extension
          --probe-snapshot-interval <n> Override stats interval for probe runs. Default: 100
          --probe-stop-on-extinction Stop a probe run early if all creatures die.
          --probe-max-population <n> Stop a probe run early if population exceeds n.
          --no-output                Run without writing CSV.
          --help                     Show this help.

        Examples:
          dotnet run --project .\src\Lineage.Cli -- --scenario .\scenarios\balanced-foraging.json --ticks 20000
          dotnet run --project .\src\Lineage.Cli -- --ticks 20000 --seed 42 --output .\out\seed42_stats.csv --report .\out\seed42_report.html
          dotnet run --project .\src\Lineage.Cli -- --batch-report .\out\preset_comparison.html --ticks 20000 --seed 42
          dotnet run --project .\src\Lineage.Cli -- --probe --ticks 20000 --probe-seeds 42,43,44
          dotnet run --project .\src\Lineage.Cli -- --probe --probe-scenario .\scenarios\scavenger-pressure.json --probe-variant sparse:initialResourcesPerMillionArea=120,resourceRegrowthMax=1.0
        """);
}

static RunResult RunSingle(RunOptions options)
{
    var (scenario, simulation) = CreateSimulationRun(options);
    if (options.SaveScenarioPath is not null)
    {
        SimulationScenarioJson.Save(options.SaveScenarioPath, scenario);
    }

    var speciesInjections = InjectStartupSpeciesProfiles(options, scenario, simulation);
    if (options.ProfileEnabled)
    {
        simulation.Profile = new SimulationProfile();
    }

    var stopwatch = Stopwatch.StartNew();

    var outputPaths = options.ResolveOutputPaths(scenario);
    var checkpoints = RunSimulation(options, scenario, simulation, outputPaths);
    stopwatch.Stop();

    if (options.SaveSnapshotPath is not null)
    {
        SimulationSnapshotJson.Save(options.SaveSnapshotPath, SimulationSnapshot.Capture(scenario, simulation));
    }

    WriteRunOutputs(options, scenario, simulation, stopwatch.Elapsed, outputPaths, checkpoints);

    SpeciesProfile? exportedSpecies = null;
    if (options.ExportSpeciesPath is not null)
    {
        exportedSpecies = ExportSpeciesProfile(options, scenario, simulation.State);
        SpeciesProfileJson.Save(options.ExportSpeciesPath, exportedSpecies);
    }

    return new RunResult(
        options,
        scenario,
        simulation,
        stopwatch.Elapsed,
        outputPaths,
        checkpoints,
        speciesInjections,
        options.ExportSpeciesPath,
        exportedSpecies);
}

static (SimulationScenario Scenario, Simulation Simulation) CreateSimulationRun(RunOptions options)
{
    if (options.LoadSnapshotPath is not null)
    {
        var restored = SimulationSnapshotJson.LoadSimulation(options.LoadSnapshotPath);
        return (restored.Scenario, restored.Simulation);
    }

    var scenario = options.CreateScenario();
    return (scenario, SimulationScenarioFactory.CreateSimulation(scenario));
}

static IReadOnlyList<SpeciesInjectionResult> InjectStartupSpeciesProfiles(
    RunOptions options,
    SimulationScenario scenario,
    Simulation simulation)
{
    var results = new List<SpeciesInjectionResult>();
    if (options.LoadSnapshotPath is null)
    {
        results.AddRange(SimulationScenarioSpeciesSeeder.InjectScenarioSpecies(
            scenario,
            simulation.State,
            options.ScenarioPath,
            Directory.GetCurrentDirectory()));
    }

    results.AddRange(InjectSpeciesProfiles(options, simulation));
    return results;
}

static IReadOnlyList<SpeciesInjectionResult> InjectSpeciesProfiles(RunOptions options, Simulation simulation)
{
    if (options.InjectSpeciesPaths.Count == 0)
    {
        return Array.Empty<SpeciesInjectionResult>();
    }

    var results = new List<SpeciesInjectionResult>(options.InjectSpeciesPaths.Count);
    foreach (var path in options.InjectSpeciesPaths)
    {
        var profile = SpeciesProfileJson.Load(SimulationScenarioSpeciesSeeder.ResolveProfilePath(
            path,
            options.ScenarioPath,
            Directory.GetCurrentDirectory()));
        results.Add(SpeciesProfileInjector.Inject(
            simulation.State,
            profile,
            new SpeciesInjectionOptions(
                options.InjectSpeciesCount,
                options.InjectSpeciesRegion,
                options.InjectSpeciesEnergy)));
    }

    return results;
}

static SpeciesProfile ExportSpeciesProfile(RunOptions options, SimulationScenario scenario, WorldState state)
{
    if (options.ExportSpeciesCreatureId is not null && options.ExportSpeciesFounderId is not null)
    {
        throw new ArgumentException("--export-species-creature and --export-species-founder cannot both be used.");
    }

    if (options.ExportSpeciesCreatureId is not null)
    {
        return SpeciesProfileExporter.ExportCreature(
            scenario,
            state,
            new EntityId(options.ExportSpeciesCreatureId.Value),
            options.ExportSpeciesName,
            options.ExportSpeciesNotes);
    }

    if (options.ExportSpeciesFounderId is not null)
    {
        return SpeciesProfileExporter.ExportFounderLineageRepresentative(
            scenario,
            state,
            new EntityId(options.ExportSpeciesFounderId.Value),
            options.ExportSpeciesName,
            options.ExportSpeciesNotes);
    }

    return SpeciesProfileExporter.ExportDominantLivingLineageRepresentative(
        scenario,
        state,
        options.ExportSpeciesName,
        options.ExportSpeciesNotes);
}

static IReadOnlyList<CheckpointArtifact> RunSimulation(
    RunOptions options,
    SimulationScenario scenario,
    Simulation simulation,
    OutputPaths outputPaths)
{
    if (options.ProfileEndTick is not null
        && options.ProfileEndTick.Value <= (options.ProfileStartTick ?? 0))
    {
        throw new ArgumentException("--profile-end-tick must be greater than --profile-start-tick.");
    }

    if (options.CheckpointIntervalTicks is null && !options.HasProfileWindow)
    {
        simulation.RunSteps(options.Ticks);
        if (simulation.Profile is not null)
        {
            simulation.Profile.IsActive = false;
        }

        return Array.Empty<CheckpointArtifact>();
    }

    string? checkpointDirectory = null;
    if (options.CheckpointIntervalTicks is not null)
    {
        checkpointDirectory = outputPaths.CheckpointDirectory
            ?? throw new InvalidOperationException("Checkpoint interval was set without a checkpoint directory.");
        Directory.CreateDirectory(checkpointDirectory);
    }

    var checkpoints = new List<CheckpointArtifact>();
    for (var i = 0; i < options.Ticks; i++)
    {
        SetProfileWindowActivity(options, simulation);
        simulation.Step();
        if (options.CheckpointIntervalTicks is not null
            && simulation.State.Tick % options.CheckpointIntervalTicks.Value == 0)
        {
            var path = Path.Combine(checkpointDirectory!, $"tick_{simulation.State.Tick:D10}.json");
            SimulationSnapshotJson.Save(path, SimulationSnapshot.Capture(scenario, simulation));
            checkpoints.Add(new CheckpointArtifact(simulation.State.Tick, path));
        }
    }

    if (simulation.Profile is not null)
    {
        simulation.Profile.IsActive = false;
    }

    return checkpoints;
}

static void SetProfileWindowActivity(RunOptions options, Simulation simulation)
{
    if (simulation.Profile is null)
    {
        return;
    }

    var startTick = options.ProfileStartTick ?? 0;
    var endTick = options.ProfileEndTick;
    simulation.Profile.IsActive = simulation.State.Tick >= startTick
        && (endTick is null || simulation.State.Tick < endTick.Value);
}

static IReadOnlyList<RunResult> RunBatch(RunOptions options)
{
    IReadOnlyList<string> scenarioPaths = options.BatchScenarioPaths.Count > 0
        ? options.BatchScenarioPaths
        : new[]
        {
            Path.Combine("scenarios", "gentle-foraging.json"),
            Path.Combine("scenarios", "balanced-foraging.json"),
            Path.Combine("scenarios", "harsh-foraging.json")
        };

    var results = new List<RunResult>(scenarioPaths.Count);
    for (var i = 0; i < scenarioPaths.Count; i++)
    {
        var runOptions = options.CreateBatchRunOptions(scenarioPaths[i], i + 1);
        results.Add(RunSingle(runOptions));
    }

    return results;
}

static IReadOnlyList<ProbeRunResult> RunProbe(RunOptions options)
{
    var scenarioPaths = ResolveProbeScenarioPaths(options);
    var seedOverrides = ResolveProbeSeedOverrides(options);
    var variants = ResolveProbeVariants(options);
    var results = new List<ProbeRunResult>(scenarioPaths.Count * seedOverrides.Count * variants.Count);

    foreach (var scenarioPath in scenarioPaths)
    {
        foreach (var variant in variants)
        {
            foreach (var seedOverride in seedOverrides)
            {
                var scenarioOptions = options with
                {
                    ScenarioPath = scenarioPath,
                    SeedOverride = seedOverride,
                    SnapshotIntervalTicksOverride = options.ProbeSnapshotIntervalTicks
                        ?? options.SnapshotIntervalTicksOverride
                        ?? 100,
                    SaveScenarioPath = null,
                    OutputPath = null,
                    LineageOutputPath = null,
                    TraitSummaryOutputPath = null,
                    FounderSummaryOutputPath = null,
                    GenerationSummaryOutputPath = null,
                    LineageTrendOutputPath = null,
                    ReportPath = null,
                    SaveSnapshotPath = null,
                    CheckpointIntervalTicks = null,
                    CheckpointDirectory = null,
                    DisableOutput = true
                };

                var baseScenario = scenarioOptions.CreateScenario();
                var scenario = variant.Apply(baseScenario);
                scenario = scenario with
                {
                    Seed = seedOverride ?? scenario.Seed,
                    StatsSnapshotIntervalTicks = scenarioOptions.SnapshotIntervalTicksOverride ?? scenario.StatsSnapshotIntervalTicks
                };
                scenario = scenario.Validated();

                var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
                _ = SimulationScenarioSpeciesSeeder.InjectScenarioSpecies(
                    scenario,
                    simulation.State,
                    scenarioPath,
                    Directory.GetCurrentDirectory());
                var stopwatch = Stopwatch.StartNew();
                var status = RunProbeSimulation(options, simulation);
                stopwatch.Stop();
                results.Add(ProbeRunResult.From(scenarioPath, baseScenario.Name, variant, scenario, simulation, stopwatch.Elapsed, status, options.Ticks));
            }
        }
    }

    return results;
}

static ProbeRunStatus RunProbeSimulation(RunOptions options, Simulation simulation)
{
    for (var i = 0; i < options.Ticks; i++)
    {
        simulation.Step();

        if (options.ProbeStopOnExtinction && simulation.State.Creatures.Count == 0)
        {
            return ProbeRunStatus.Extinct;
        }

        if (options.ProbeMaxPopulation is not null && simulation.State.Creatures.Count > options.ProbeMaxPopulation.Value)
        {
            return ProbeRunStatus.MaxPopulation;
        }
    }

    return ProbeRunStatus.Completed;
}

static IReadOnlyList<string> ResolveProbeScenarioPaths(RunOptions options)
{
    if (options.ProbeScenarioPaths.Count > 0)
    {
        return options.ProbeScenarioPaths;
    }

    if (options.BatchScenarioPaths.Count > 0)
    {
        return options.BatchScenarioPaths;
    }

    return
    [
        Path.Combine("scenarios", "gentle-foraging.json"),
        Path.Combine("scenarios", "balanced-foraging.json"),
        Path.Combine("scenarios", "harsh-foraging.json"),
        Path.Combine("scenarios", "scavenger-pressure.json"),
        Path.Combine("scenarios", "omnivore-pressure.json"),
        Path.Combine("scenarios", "predation-pressure.json")
    ];
}

static IReadOnlyList<ulong?> ResolveProbeSeedOverrides(RunOptions options)
{
    if (options.ProbeSeeds.Count > 0)
    {
        return options.ProbeSeeds.Select(seed => (ulong?)seed).ToArray();
    }

    return [options.SeedOverride];
}

static IReadOnlyList<ProbeVariant> ResolveProbeVariants(RunOptions options)
{
    if (options.ProbeVariants.Count == 0)
    {
        return [ProbeVariant.Base];
    }

    var variants = new ProbeVariant[options.ProbeVariants.Count + 1];
    variants[0] = ProbeVariant.Base;
    for (var i = 0; i < options.ProbeVariants.Count; i++)
    {
        variants[i + 1] = options.ProbeVariants[i];
    }

    return variants;
}

static void WriteRunOutputs(
    RunOptions options,
    SimulationScenario scenario,
    Simulation simulation,
    TimeSpan elapsed,
    OutputPaths outputPaths,
    IReadOnlyList<CheckpointArtifact> checkpoints)
{
    if (outputPaths.StatsPath is not null)
    {
        StatsCsvWriter.Write(outputPaths.StatsPath, simulation.State.Stats.Snapshots);
        LineageCsvWriter.Write(outputPaths.LineagePath!, simulation.State.LineageRecords);
        TraitSummaryCsvWriter.Write(outputPaths.TraitSummaryPath!, simulation.State);
        FounderSummaryCsvWriter.Write(outputPaths.FounderSummaryPath!, simulation.State.LineageRecords);
        GenerationSummaryCsvWriter.Write(outputPaths.GenerationSummaryPath!, simulation.State.LineageRecords);
        LineageTrendCsvWriter.Write(outputPaths.LineageTrendPath!, simulation.State.Stats.Snapshots, simulation.State.LineageRecords);
    }

    if (outputPaths.ProfilePath is not null && simulation.Profile is not null)
    {
        ProfileCsvWriter.Write(outputPaths.ProfilePath, simulation.Profile);
    }

    if (outputPaths.SensingProfilePath is not null && simulation.Profile is not null)
    {
        SensingProfileCsvWriter.Write(outputPaths.SensingProfilePath, simulation.Profile.Sensing);
    }

    if (outputPaths.ReportPath is not null)
    {
        RunReportWriter.Write(outputPaths.ReportPath, options, scenario, simulation, elapsed, outputPaths, checkpoints);
    }
}

static void PrintSummary(RunResult result)
{
    var options = result.Options;
    var scenario = result.Scenario;
    var state = result.Simulation.State;
    var outputPaths = result.OutputPaths;
    var snapshot = state.Stats.Snapshots.Count > 0
        ? state.Stats.Snapshots[^1]
        : default;

    Console.WriteLine($"Scenario: {scenario.Name}");
    if (options.ScenarioPath is not null)
    {
        Console.WriteLine($"Scenario file: {Path.GetFullPath(options.ScenarioPath)}");
    }

    if (options.LoadSnapshotPath is not null)
    {
        Console.WriteLine($"Loaded snapshot: {Path.GetFullPath(options.LoadSnapshotPath)}");
    }

    Console.WriteLine($"Pipeline: {scenario.PipelineKind}");
    Console.WriteLine($"Seed: {scenario.Seed}");
    Console.WriteLine($"Ticks: {options.Ticks}");
    Console.WriteLine($"Elapsed wall time: {result.Elapsed.TotalSeconds:0.000}s");
    Console.WriteLine($"Final creatures: {state.Creatures.Count}");
    Console.WriteLine($"Eggs: {state.Eggs.Count}");
    Console.WriteLine($"Births: {state.Stats.CreatureBirthCount}");
    Console.WriteLine($"Eggs laid: {state.Stats.EggLaidCount}");
    Console.WriteLine($"Eggs hatched: {state.Stats.EggHatchedCount}");
    Console.WriteLine($"Egg deaths: {state.Stats.EggDeathCount}");
    Console.WriteLine($"Egg predation deaths: {state.Stats.EggPredationDeathCount}");
        Console.WriteLine($"Deaths: {state.Stats.CreatureDeathCount}");
        Console.WriteLine($"Starvation deaths: {state.Stats.StarvationDeathCount}");
        Console.WriteLine($"Injury deaths: {state.Stats.InjuryDeathCount}");
        Console.WriteLine($"Rotten meat deaths: {state.Stats.RottenMeatDeathCount}");
    Console.WriteLine($"Max generation: {snapshot.MaxGeneration}");
    Console.WriteLine($"Obstacle sensed: {Percent(snapshot.ObstacleSensedCreatureCount, snapshot.CreatureCount):0.0}%");
    Console.WriteLine($"Obstacle blocked: {Percent(snapshot.ObstacleBlockedCreatureCount, snapshot.CreatureCount):0.0}%");
    Console.WriteLine($"Snapshots: {state.Stats.Snapshots.Count}");
    if (options.ProfileEnabled)
    {
        var profileStart = options.ProfileStartTick ?? 0;
        var profileEnd = options.ProfileEndTick?.ToString(CultureInfo.InvariantCulture) ?? "end";
        Console.WriteLine($"Profile window: {profileStart} to {profileEnd}");
    }

    if (options.SaveScenarioPath is not null)
    {
        Console.WriteLine($"Saved scenario: {Path.GetFullPath(options.SaveScenarioPath)}");
    }

    foreach (var injection in result.SpeciesInjections)
    {
        Console.WriteLine(
            $"Injected species: {injection.SpeciesName} x{injection.CreatureIds.Count} " +
            $"(genome {injection.GenomeId}, brain {injection.BrainId})");
    }

    if (outputPaths.StatsPath is not null)
    {
        Console.WriteLine($"Stats CSV: {Path.GetFullPath(outputPaths.StatsPath)}");
        Console.WriteLine($"Lineage CSV: {Path.GetFullPath(outputPaths.LineagePath!)}");
        Console.WriteLine($"Traits CSV: {Path.GetFullPath(outputPaths.TraitSummaryPath!)}");
        Console.WriteLine($"Founders CSV: {Path.GetFullPath(outputPaths.FounderSummaryPath!)}");
        Console.WriteLine($"Generations CSV: {Path.GetFullPath(outputPaths.GenerationSummaryPath!)}");
        Console.WriteLine($"Lineage trends CSV: {Path.GetFullPath(outputPaths.LineageTrendPath!)}");
    }

    if (result.Simulation.Profile is not null)
    {
        PrintProfileSummary(result.Simulation.Profile);
    }

    if (outputPaths.ProfilePath is not null)
    {
        Console.WriteLine($"Profile CSV: {Path.GetFullPath(outputPaths.ProfilePath)}");
    }

    if (outputPaths.SensingProfilePath is not null)
    {
        Console.WriteLine($"Sensing profile CSV: {Path.GetFullPath(outputPaths.SensingProfilePath)}");
    }

    if (outputPaths.ReportPath is not null)
    {
        Console.WriteLine($"Report: {Path.GetFullPath(outputPaths.ReportPath)}");
    }

    if (options.SaveSnapshotPath is not null)
    {
        Console.WriteLine($"Snapshot: {Path.GetFullPath(options.SaveSnapshotPath)}");
    }

    if (result.ExportedSpeciesPath is not null && result.ExportedSpecies is not null)
    {
        Console.WriteLine(
            $"Exported species: {result.ExportedSpecies.Name} from creature " +
            $"{result.ExportedSpecies.Source.CreatureId} to {Path.GetFullPath(result.ExportedSpeciesPath)}");
    }

    if (outputPaths.CheckpointDirectory is not null)
    {
        Console.WriteLine($"Checkpoint directory: {Path.GetFullPath(outputPaths.CheckpointDirectory)}");
        Console.WriteLine($"Checkpoints: {result.Checkpoints.Count}");
    }
}

static float Percent(int count, int total)
{
    return total > 0
        ? count / (float)total * 100f
        : 0f;
}

static void PrintProfileSummary(SimulationProfile profile, int maxSystems = 8)
{
    Console.WriteLine($"Profiled steps: {profile.ProfiledSteps}");
    Console.WriteLine($"Profiled system time: {profile.TotalMilliseconds:0.000}ms");
    foreach (var system in profile.Systems
        .OrderByDescending(system => system.TotalMilliseconds)
        .Take(maxSystems))
    {
        var share = profile.TotalMilliseconds > 0
            ? system.TotalMilliseconds / profile.TotalMilliseconds * 100.0
            : 0.0;
        Console.WriteLine(
            $"  {system.SystemName}: {system.TotalMilliseconds:0.000}ms ({share:0.0}%), avg {system.AverageMillisecondsPerCall:0.0000}ms");
    }

    PrintSensingProfileSummary(profile.Sensing);
}

static void PrintSensingProfileSummary(SimulationSensingProfile profile)
{
    if (profile.CreaturesSensed == 0)
    {
        return;
    }

    Console.WriteLine($"Sensing profile: {profile.CreaturesSensed} creature updates, {profile.TotalMeasuredMilliseconds:0.000}ms measured inside sensing");
    Console.WriteLine(
        $"  Trait cache: {profile.TraitCacheMilliseconds:0.000}ms, {FormatAverage(profile.TraitCacheCreatures, profile.Updates):0.00} creatures/update");
    Console.WriteLine(
        $"  World sense refreshes: {profile.WorldSenseRefreshes} refreshed, {profile.WorldSenseSkippedUpdates} skipped"
        + $" (scheduled {profile.WorldSenseScheduledRefreshes}, close {profile.WorldSenseCloseRefreshes}, forced {profile.WorldSenseForcedRefreshes})");
    Console.WriteLine(
        $"  Creature setup: {profile.CreatureSetupMilliseconds:0.000}ms");
    Console.WriteLine(
        $"  Internal state: {profile.InternalStateMilliseconds:0.000}ms");
    Console.WriteLine(
        $"  Resource query: {profile.ResourceQueryMilliseconds:0.000}ms, {FormatAverage(profile.ResourceCandidates, profile.ResourceQueries):0.00} candidates/query");
    Console.WriteLine(
        $"    Plant candidates: {FormatAverage(profile.PlantResourceQueryCandidates, profile.PlantResourceQueries):0.00}/query");
    Console.WriteLine(
        $"    Meat candidates: {FormatAverage(profile.MeatResourceQueryCandidates, profile.MeatResourceQueries):0.00}/query");
    Console.WriteLine(
        $"  Resource scan: {profile.ResourceScanMilliseconds:0.000}ms, plants {profile.PlantCandidates}, meat {profile.MeatResourceCandidates}, visible plants {profile.VisiblePlantCandidates}, visible meat {profile.VisibleMeatResourceCandidates}");
    Console.WriteLine(
        $"  Egg query/scan: {(profile.EggQueryMilliseconds + profile.EggScanMilliseconds):0.000}ms, {FormatAverage(profile.EggCandidates, profile.EggQueries):0.00} candidates/query, visible {profile.VisibleEggCandidates}");
    Console.WriteLine(
        $"  Creature query/scan: {(profile.CreatureQueryMilliseconds + profile.CreatureScanMilliseconds):0.000}ms, {FormatAverage(profile.CreatureCandidates, profile.CreatureQueries):0.00} candidates/query, visible {profile.VisibleCreatureCandidates}");
    Console.WriteLine(
        $"    Creature cells: {FormatAverage(profile.CreatureCellsVisited, profile.CreatureQueries):0.00}/query, non-empty {FormatAverage(profile.CreatureNonEmptyCellsVisited, profile.CreatureQueries):0.00}/query");
    Console.WriteLine(
        $"    Creature rejects/query: distance {FormatAverage(profile.CreatureDistanceRejectedCandidates, profile.CreatureQueries):0.00}, range {FormatAverage(profile.CreatureRangeRejectedCandidates, profile.CreatureQueries):0.00}, vision {FormatAverage(profile.CreatureVisionRejectedCandidates, profile.CreatureQueries):0.00}, self {FormatAverage(profile.CreatureSelfRejectedCandidates, profile.CreatureQueries):0.00}");
    Console.WriteLine(
        $"    Body radius cache misses: {profile.CreatureBodyRadiusCacheMisses}");
    Console.WriteLine(
        $"  Terrain sense: {profile.TerrainSenseMilliseconds:0.000}ms");
    Console.WriteLine(
        $"  Obstacle sense: {profile.ObstacleSenseMilliseconds:0.000}ms, avg {FormatAverage((long)(profile.ObstacleSenseMilliseconds * 1000.0), profile.ObstacleSenseSamples):0.00}us/creature");
    Console.WriteLine(
        $"  Memory sense: {profile.MemorySenseMilliseconds:0.000}ms");
    Console.WriteLine(
        $"  Sense finalization: {profile.SenseFinalizationMilliseconds:0.000}ms");
}

static double FormatAverage(long numerator, long denominator)
{
    return denominator > 0
        ? numerator / (double)denominator
        : 0.0;
}

static void PrintBatchSummary(RunOptions options, IReadOnlyList<RunResult> results, string reportPath)
{
    Console.WriteLine($"Batch comparison runs: {results.Count}");
    Console.WriteLine($"Ticks per run: {options.Ticks}");
    Console.WriteLine($"Report: {Path.GetFullPath(reportPath)}");
    Console.WriteLine();

    foreach (var result in results)
    {
        var state = result.Simulation.State;
        var snapshot = state.Stats.Snapshots.Count > 0 ? state.Stats.Snapshots[^1] : default;
        Console.WriteLine(
            $"{result.Scenario.Name}: final {state.Creatures.Count}, births {state.Stats.CreatureBirthCount}, " +
            $"eggs {state.Eggs.Count}, laid {state.Stats.EggLaidCount}, hatched {state.Stats.EggHatchedCount}, " +
            $"deaths {state.Stats.CreatureDeathCount}, starved {state.Stats.StarvationDeathCount}, " +
            $"injury {state.Stats.InjuryDeathCount}, rotten {state.Stats.RottenMeatDeathCount}, max gen {snapshot.MaxGeneration}");
    }
}

static void PrintProbeSummary(
    RunOptions options,
    IReadOnlyList<ProbeRunResult> results,
    string outputPath,
    string reportPath)
{
    Console.WriteLine($"Probe runs: {results.Count}");
    Console.WriteLine($"Ticks requested per run: {options.Ticks}");
    Console.WriteLine($"CSV: {Path.GetFullPath(outputPath)}");
    Console.WriteLine($"Report: {Path.GetFullPath(reportPath)}");
    Console.WriteLine($"Total wall time: {results.Sum(result => result.WallSeconds):0.000}s");
    Console.WriteLine();

    foreach (var group in results
        .GroupBy(result => new ProbeResultGroupKey(result.ScenarioName, result.VariantName, result.VariantOverrides))
        .OrderBy(group => group.Key.ScenarioName)
        .ThenBy(group => group.Key.VariantName))
    {
        var averagePopulation = group.Average(result => result.FinalCreatures);
        var averageTicksPerSecond = group.Average(result => result.TicksPerSecond);
        var statuses = string.Join(", ", group.Select(result => result.Status.ToString()).Distinct().Order());
        Console.WriteLine(
            $"{group.Key.ScenarioName} / {group.Key.VariantName}: runs {group.Count()}, avg final {averagePopulation:0.0}, " +
            $"pop range {group.Min(result => result.FinalCreatures)}-{group.Max(result => result.FinalCreatures)}, " +
            $"avg {averageTicksPerSecond:0.0} ticks/s, {statuses}");
    }
}

internal sealed record RunOptions
{
    public int Ticks { get; init; } = 5_000;

    public string? ScenarioPath { get; init; }

    public string? LoadSnapshotPath { get; init; }

    public IReadOnlyList<string> BatchScenarioPaths { get; init; } = Array.Empty<string>();

    public string? SaveScenarioPath { get; init; }

    public ulong? SeedOverride { get; init; }

    public SimulationPipelineKind? PipelineKindOverride { get; init; }

    public int? InitialCreatureCountOverride { get; init; }

    public float? SpatialCellSizeOverride { get; init; }

    public float? InitialResourcesPerMillionAreaOverride { get; init; }

    public int? LegacyInitialResourceCountOverride { get; init; }

    public int? SnapshotIntervalTicksOverride { get; init; }

    public string? OutputPath { get; init; }

    public string? LineageOutputPath { get; init; }

    public string? TraitSummaryOutputPath { get; init; }

    public string? FounderSummaryOutputPath { get; init; }

    public string? GenerationSummaryOutputPath { get; init; }

    public string? LineageTrendOutputPath { get; init; }

    public string? ReportPath { get; init; }

    public bool Profile { get; init; }

    public string? ProfileOutputPath { get; init; }

    public int? ProfileStartTick { get; init; }

    public int? ProfileEndTick { get; init; }

    public string? SaveSnapshotPath { get; init; }

    public int? CheckpointIntervalTicks { get; init; }

    public string? CheckpointDirectory { get; init; }

    public IReadOnlyList<string> InjectSpeciesPaths { get; init; } = Array.Empty<string>();

    public int InjectSpeciesCount { get; init; } = 10;

    public InitialCreatureSpawnRegion InjectSpeciesRegion { get; init; } = InitialCreatureSpawnRegion.Uniform;

    public float? InjectSpeciesEnergy { get; init; }

    public string? ExportSpeciesPath { get; init; }

    public int? ExportSpeciesCreatureId { get; init; }

    public int? ExportSpeciesFounderId { get; init; }

    public string? ExportSpeciesName { get; init; }

    public string? ExportSpeciesNotes { get; init; }

    public string? BatchReportPath { get; init; }

    public string BatchOutputDirectory { get; init; } = Path.Combine("out", "batch");

    public bool ProbeMode { get; init; }

    public IReadOnlyList<string> ProbeScenarioPaths { get; init; } = Array.Empty<string>();

    public IReadOnlyList<ulong> ProbeSeeds { get; init; } = Array.Empty<ulong>();

    public IReadOnlyList<ProbeVariant> ProbeVariants { get; init; } = Array.Empty<ProbeVariant>();

    public string? ProbeOutputPath { get; init; }

    public string? ProbeReportPath { get; init; }

    public int? ProbeSnapshotIntervalTicks { get; init; }

    public bool ProbeStopOnExtinction { get; init; }

    public int? ProbeMaxPopulation { get; init; }

    public bool DisableOutput { get; init; }

    public bool ShowHelp { get; init; }

    public bool ProfileEnabled => Profile
        || ProfileOutputPath is not null
        || ProfileStartTick is not null
        || ProfileEndTick is not null;

    public bool HasProfileWindow => ProfileStartTick is not null || ProfileEndTick is not null;

    public bool IsProbe => ProbeMode
        || ProbeScenarioPaths.Count > 0
        || ProbeSeeds.Count > 0
        || ProbeVariants.Count > 0
        || ProbeOutputPath is not null
        || ProbeReportPath is not null
        || ProbeSnapshotIntervalTicks is not null
        || ProbeStopOnExtinction
        || ProbeMaxPopulation is not null;

    public bool IsBatch => BatchScenarioPaths.Count > 0 || BatchReportPath is not null;

    public SimulationScenario CreateScenario()
    {
        var scenario = ScenarioPath is null
            ? new SimulationScenario { StatsSnapshotIntervalTicks = 10 }
            : SimulationScenarioJson.Load(ScenarioPath);

        scenario = scenario with
        {
            Seed = SeedOverride ?? scenario.Seed,
            PipelineKind = PipelineKindOverride ?? scenario.PipelineKind,
            InitialCreatureCount = InitialCreatureCountOverride ?? scenario.InitialCreatureCount,
            SpatialCellSize = SpatialCellSizeOverride ?? scenario.SpatialCellSize,
            StatsSnapshotIntervalTicks = SnapshotIntervalTicksOverride ?? scenario.StatsSnapshotIntervalTicks
        };

        if (LegacyInitialResourceCountOverride is not null)
        {
            scenario = scenario with
            {
                InitialResourcesPerMillionArea = SimulationScenario.CalculateResourcesPerMillionArea(
                    LegacyInitialResourceCountOverride.Value,
                    scenario.WorldWidth,
                    scenario.WorldHeight)
            };
        }

        if (InitialResourcesPerMillionAreaOverride is not null)
        {
            scenario = scenario with
            {
                InitialResourcesPerMillionArea = InitialResourcesPerMillionAreaOverride.Value
            };
        }

        return scenario.Validated();
    }

    public OutputPaths ResolveOutputPaths(SimulationScenario scenario)
    {
        if (DisableOutput)
        {
            var disabledSensingProfilePath = ProfileOutputPath is not null
                ? AddSuffix(ProfileOutputPath, "sensing")
                : null;
            var disabledPaths = new OutputPaths(
                null,
                null,
                null,
                null,
                null,
                null,
                ReportPath,
                ProfileOutputPath,
                disabledSensingProfilePath,
                null);
            return disabledPaths with { CheckpointDirectory = ResolveCheckpointDirectory(scenario, disabledPaths) };
        }

        var statsPath = OutputPath ?? Path.Combine("out", $"lineage_run_{scenario.Seed}_stats.csv");
        var profilePath = ProfileOutputPath ?? (ProfileEnabled ? AddSuffix(statsPath, "profile") : null);
        var sensingProfilePath = profilePath is not null
            ? AddSuffix(profilePath, "sensing")
            : null;
        var paths = new OutputPaths(
            statsPath,
            LineageOutputPath ?? AddSuffix(statsPath, "lineage"),
            TraitSummaryOutputPath ?? AddSuffix(statsPath, "traits"),
            FounderSummaryOutputPath ?? AddSuffix(statsPath, "founders"),
            GenerationSummaryOutputPath ?? AddSuffix(statsPath, "generations"),
            LineageTrendOutputPath ?? AddSuffix(statsPath, "lineage_trends"),
            ReportPath,
            profilePath,
            sensingProfilePath,
            null);
        return paths with { CheckpointDirectory = ResolveCheckpointDirectory(scenario, paths) };
    }

    public RunOptions CreateBatchRunOptions(string scenarioPath, int index)
    {
        var scenarioName = Path.GetFileNameWithoutExtension(scenarioPath);
        var slug = Slugify($"{index:00}_{scenarioName}");
        var statsPath = Path.Combine(BatchOutputDirectory, $"{slug}_stats.csv");

        return this with
        {
            ScenarioPath = scenarioPath,
            SaveScenarioPath = null,
            OutputPath = DisableOutput ? null : statsPath,
            LineageOutputPath = null,
            TraitSummaryOutputPath = null,
            FounderSummaryOutputPath = null,
            GenerationSummaryOutputPath = null,
            LineageTrendOutputPath = null,
            ProfileOutputPath = ProfileEnabled ? Path.Combine(BatchOutputDirectory, $"{slug}_profile.csv") : null,
            ReportPath = DisableOutput ? null : Path.Combine(BatchOutputDirectory, $"{slug}_report.html"),
            SaveSnapshotPath = DisableOutput ? null : Path.Combine(BatchOutputDirectory, $"{slug}_snapshot.json"),
            CheckpointDirectory = CheckpointIntervalTicks is null
                ? null
                : CheckpointDirectory is null
                    ? Path.Combine(BatchOutputDirectory, $"{slug}_checkpoints")
                    : Path.Combine(CheckpointDirectory, slug)
        };
    }

    public static RunOptions Parse(string[] args)
    {
        var options = new RunOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--help":
                case "-h":
                    options = options with { ShowHelp = true };
                    break;
                case "--scenario":
                    options = options with { ScenarioPath = ReadValue(args, ref i, arg) };
                    break;
                case "--load-snapshot":
                    options = options with { LoadSnapshotPath = ReadValue(args, ref i, arg) };
                    break;
                case "--batch-scenario":
                    options = options with { BatchScenarioPaths = Append(options.BatchScenarioPaths, ReadValue(args, ref i, arg)) };
                    break;
                case "--save-scenario":
                    options = options with { SaveScenarioPath = ReadValue(args, ref i, arg) };
                    break;
                case "--ticks":
                    options = options with { Ticks = ParseNonNegativeInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--seed":
                    options = options with { SeedOverride = ParseSeed(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--pipeline":
                    options = options with { PipelineKindOverride = ParsePipeline(ReadValue(args, ref i, arg)) };
                    break;
                case "--creatures":
                    options = options with { InitialCreatureCountOverride = ParseNonNegativeInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--spatial-cell-size":
                    options = options with { SpatialCellSizeOverride = ParsePositiveFloat(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--resources-per-million-area":
                    options = options with { InitialResourcesPerMillionAreaOverride = ParseNonNegativeFloat(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--resources":
                    options = options with { LegacyInitialResourceCountOverride = ParseNonNegativeInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--snapshot-interval":
                    options = options with { SnapshotIntervalTicksOverride = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--output":
                    options = options with { OutputPath = ReadValue(args, ref i, arg), DisableOutput = false };
                    break;
                case "--lineage-output":
                    options = options with { LineageOutputPath = ReadValue(args, ref i, arg), DisableOutput = false };
                    break;
                case "--traits-output":
                    options = options with { TraitSummaryOutputPath = ReadValue(args, ref i, arg), DisableOutput = false };
                    break;
                case "--founders-output":
                    options = options with { FounderSummaryOutputPath = ReadValue(args, ref i, arg), DisableOutput = false };
                    break;
                case "--generations-output":
                    options = options with { GenerationSummaryOutputPath = ReadValue(args, ref i, arg), DisableOutput = false };
                    break;
                case "--lineage-trends-output":
                    options = options with { LineageTrendOutputPath = ReadValue(args, ref i, arg), DisableOutput = false };
                    break;
                case "--report":
                    options = options with { ReportPath = ReadValue(args, ref i, arg) };
                    break;
                case "--profile":
                    options = options with { Profile = true };
                    break;
                case "--profile-output":
                    options = options with { ProfileOutputPath = ReadValue(args, ref i, arg) };
                    break;
                case "--profile-start-tick":
                    options = options with { ProfileStartTick = ParseNonNegativeInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--profile-end-tick":
                    options = options with { ProfileEndTick = ParseNonNegativeInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--save-snapshot":
                    options = options with { SaveSnapshotPath = ReadValue(args, ref i, arg) };
                    break;
                case "--checkpoint-interval":
                    options = options with { CheckpointIntervalTicks = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--checkpoint-dir":
                    options = options with { CheckpointDirectory = ReadValue(args, ref i, arg) };
                    break;
                case "--inject-species":
                    options = options with { InjectSpeciesPaths = Append(options.InjectSpeciesPaths, ReadValue(args, ref i, arg)) };
                    break;
                case "--inject-species-count":
                    options = options with { InjectSpeciesCount = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--inject-species-region":
                    options = options with { InjectSpeciesRegion = ParseSpawnRegion(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--inject-species-energy":
                    options = options with { InjectSpeciesEnergy = ParsePositiveFloat(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--export-species":
                    options = options with { ExportSpeciesPath = ReadValue(args, ref i, arg) };
                    break;
                case "--export-species-creature":
                    options = options with { ExportSpeciesCreatureId = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--export-species-founder":
                    options = options with { ExportSpeciesFounderId = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--export-species-name":
                    options = options with { ExportSpeciesName = ReadValue(args, ref i, arg) };
                    break;
                case "--export-species-notes":
                    options = options with { ExportSpeciesNotes = ReadValue(args, ref i, arg) };
                    break;
                case "--batch-report":
                    options = options with { BatchReportPath = ReadValue(args, ref i, arg) };
                    break;
                case "--batch-output-dir":
                    options = options with { BatchOutputDirectory = ReadValue(args, ref i, arg) };
                    break;
                case "--probe":
                    options = options with { ProbeMode = true };
                    break;
                case "--probe-scenario":
                    options = options with { ProbeScenarioPaths = Append(options.ProbeScenarioPaths, ReadValue(args, ref i, arg)) };
                    break;
                case "--probe-seeds":
                    options = options with { ProbeSeeds = ParseSeedList(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--probe-variant":
                    options = options with { ProbeVariants = Append(options.ProbeVariants, ProbeVariant.Parse(ReadValue(args, ref i, arg), arg)) };
                    break;
                case "--probe-output":
                    options = options with { ProbeOutputPath = ReadValue(args, ref i, arg) };
                    break;
                case "--probe-report":
                    options = options with { ProbeReportPath = ReadValue(args, ref i, arg) };
                    break;
                case "--probe-snapshot-interval":
                    options = options with { ProbeSnapshotIntervalTicks = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--probe-stop-on-extinction":
                    options = options with { ProbeStopOnExtinction = true };
                    break;
                case "--probe-max-population":
                    options = options with { ProbeMaxPopulation = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--no-output":
                    options = options with { DisableOutput = true };
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{arg}'. Use --help for usage.");
            }
        }

        return options;
    }

    private static IReadOnlyList<T> Append<T>(IReadOnlyList<T> values, T value)
    {
        var copy = new T[values.Count + 1];
        for (var i = 0; i < values.Count; i++)
        {
            copy[i] = values[i];
        }

        copy[^1] = value;
        return copy;
    }

    private static string Slugify(string value)
    {
        var chars = value.Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '_').ToArray();
        var slug = new string(chars).Trim('_');
        return string.IsNullOrWhiteSpace(slug) ? "run" : slug;
    }

    private static string AddSuffix(string path, string suffix)
    {
        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        return Path.Combine(
            string.IsNullOrWhiteSpace(directory) ? string.Empty : directory,
            $"{fileName}_{suffix}{extension}");
    }

    private string? ResolveCheckpointDirectory(SimulationScenario scenario, OutputPaths outputPaths)
    {
        if (CheckpointIntervalTicks is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(CheckpointDirectory))
        {
            return CheckpointDirectory;
        }

        if (!string.IsNullOrWhiteSpace(SaveSnapshotPath))
        {
            return AddDirectorySuffix(SaveSnapshotPath, "checkpoints");
        }

        if (!string.IsNullOrWhiteSpace(outputPaths.ReportPath))
        {
            return AddDirectorySuffix(outputPaths.ReportPath, "checkpoints");
        }

        if (!string.IsNullOrWhiteSpace(outputPaths.StatsPath))
        {
            return AddDirectorySuffix(outputPaths.StatsPath, "checkpoints");
        }

        return Path.Combine("out", $"lineage_run_{scenario.Seed}_checkpoints");
    }

    private static string AddDirectorySuffix(string path, string suffix)
    {
        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileNameWithoutExtension(path);
        return Path.Combine(
            string.IsNullOrWhiteSpace(directory) ? string.Empty : directory,
            $"{fileName}_{suffix}");
    }

    private static string ReadValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{optionName} requires a value.");
        }

        index++;
        return args[index];
    }

    private static int ParsePositiveInt(string value, string optionName)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            throw new ArgumentException($"{optionName} must be a positive integer.");
        }

        return parsed;
    }

    private static int ParseNonNegativeInt(string value, string optionName)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
        {
            throw new ArgumentException($"{optionName} must be a non-negative integer.");
        }

        return parsed;
    }

    private static float ParseNonNegativeFloat(string value, string optionName)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || !float.IsFinite(parsed)
            || parsed < 0f)
        {
            throw new ArgumentException($"{optionName} must be a finite non-negative number.");
        }

        return parsed;
    }

    private static float ParsePositiveFloat(string value, string optionName)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || !float.IsFinite(parsed)
            || parsed <= 0f)
        {
            throw new ArgumentException($"{optionName} must be a finite positive number.");
        }

        return parsed;
    }

    private static ulong ParseSeed(string value, string optionName)
    {
        if (!ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new ArgumentException($"{optionName} must be an unsigned integer.");
        }

        return parsed;
    }

    private static IReadOnlyList<ulong> ParseSeedList(string value, string optionName)
    {
        var seeds = value
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(seed => ParseSeed(seed, optionName))
            .ToArray();

        if (seeds.Length == 0)
        {
            throw new ArgumentException($"{optionName} must include at least one seed.");
        }

        return seeds;
    }

    private static SimulationPipelineKind ParsePipeline(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "neural" => SimulationPipelineKind.Neural,
            "simple" or "foraging" or "simple-foraging" => SimulationPipelineKind.SimpleForaging,
            _ => throw new ArgumentException("--pipeline must be 'neural' or 'simple'.")
        };
    }

    private static InitialCreatureSpawnRegion ParseSpawnRegion(string value, string optionName)
    {
        var normalized = value.Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (Enum.TryParse<InitialCreatureSpawnRegion>(normalized, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        var choices = string.Join(", ", Enum.GetNames<InitialCreatureSpawnRegion>());
        throw new ArgumentException($"{optionName} must be one of: {choices}.");
    }
}

internal readonly record struct RunResult(
    RunOptions Options,
    SimulationScenario Scenario,
    Simulation Simulation,
    TimeSpan Elapsed,
    OutputPaths OutputPaths,
    IReadOnlyList<CheckpointArtifact> Checkpoints,
    IReadOnlyList<SpeciesInjectionResult> SpeciesInjections,
    string? ExportedSpeciesPath,
    SpeciesProfile? ExportedSpecies);

internal readonly record struct OutputPaths(
    string? StatsPath,
    string? LineagePath,
    string? TraitSummaryPath,
    string? FounderSummaryPath,
    string? GenerationSummaryPath,
    string? LineageTrendPath,
    string? ReportPath,
    string? ProfilePath,
    string? SensingProfilePath,
    string? CheckpointDirectory);

internal readonly record struct CheckpointArtifact(long Tick, string Path);

internal enum ProbeRunStatus
{
    Completed,
    Extinct,
    MaxPopulation
}

internal readonly record struct ProbeResultGroupKey(string ScenarioName, string VariantName, string VariantOverrides);

internal readonly record struct ScenarioOverride(string PropertyName, string Value)
{
    public string DisplayText => $"{PropertyName}={Value}";
}

internal sealed record ProbeVariant(string Name, IReadOnlyList<ScenarioOverride> Overrides)
{
    public static ProbeVariant Base { get; } = new("base", Array.Empty<ScenarioOverride>());

    public string OverrideSummary => Overrides.Count == 0
        ? string.Empty
        : string.Join("; ", Overrides.Select(scenarioOverride => scenarioOverride.DisplayText));

    public SimulationScenario Apply(SimulationScenario scenario)
    {
        if (Overrides.Count == 0)
        {
            return scenario;
        }

        var jsonObject = JsonNode.Parse(SimulationScenarioJson.ToJson(scenario)) as JsonObject
            ?? throw new InvalidOperationException("Scenario JSON did not produce an object.");
        var propertyNames = jsonObject.Select(property => property.Key).ToArray();

        foreach (var scenarioOverride in Overrides)
        {
            var propertyName = ResolvePropertyName(propertyNames, scenarioOverride.PropertyName);
            jsonObject[propertyName] = ParseOverrideValue(scenarioOverride.Value);
        }

        return SimulationScenarioJson.FromJson(jsonObject.ToJsonString());
    }

    public static ProbeVariant Parse(string value, string optionName)
    {
        var separatorIndex = value.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
        {
            throw new ArgumentException($"{optionName} must use name:key=value[,key=value] format.");
        }

        var name = value[..separatorIndex].Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"{optionName} variant name cannot be empty.");
        }

        if (string.Equals(name, Base.Name, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"{optionName} variant name 'base' is reserved.");
        }

        var overrideText = value[(separatorIndex + 1)..];
        var overrides = overrideText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => ParseOverride(token, optionName))
            .ToArray();

        if (overrides.Length == 0)
        {
            throw new ArgumentException($"{optionName} must include at least one key=value override.");
        }

        return new ProbeVariant(name, overrides);
    }

    private static ScenarioOverride ParseOverride(string token, string optionName)
    {
        var separatorIndex = token.IndexOf('=', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == token.Length - 1)
        {
            throw new ArgumentException($"{optionName} overrides must use key=value entries.");
        }

        var propertyName = token[..separatorIndex].Trim();
        var value = token[(separatorIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(propertyName) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{optionName} overrides must include non-empty keys and values.");
        }

        return new ScenarioOverride(propertyName, value);
    }

    private static string ResolvePropertyName(IReadOnlyList<string> propertyNames, string requestedName)
    {
        var normalizedRequest = NormalizeName(requestedName);
        foreach (var propertyName in propertyNames)
        {
            if (NormalizeName(propertyName) == normalizedRequest)
            {
                return propertyName;
            }
        }

        throw new ArgumentException($"Unknown scenario override '{requestedName}'. Use a scenario JSON property name such as initialResourcesPerMillionArea.");
    }

    private static string NormalizeName(string value)
    {
        return string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();
    }

    private static JsonNode? ParseOverrideValue(string value)
    {
        if (bool.TryParse(value, out var boolValue))
        {
            return JsonValue.Create(boolValue);
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            return JsonValue.Create(longValue);
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue)
            && double.IsFinite(doubleValue))
        {
            return JsonValue.Create(doubleValue);
        }

        return JsonValue.Create(value);
    }
}

internal readonly record struct ProbeRunResult(
    string ScenarioName,
    string ScenarioPath,
    string VariantName,
    string VariantOverrides,
    ulong Seed,
    ProbeRunStatus Status,
    int RequestedTicks,
    long FinalTick,
    double SimulatedSeconds,
    double WallSeconds,
    double TicksPerSecond,
    SimulationPipelineKind PipelineKind,
    InitialBrainKind InitialBrainKind,
    int InitialCreatures,
    int InitialResources,
    float ResourceDensityPerMillion,
    float ResourceClusterStrength,
    float ResourceClusterRadius,
    int FinalCreatures,
    int FinalEggs,
    int FinalResources,
    int FinalPlants,
    int FinalMeat,
    int Births,
    int EggsLaid,
    int EggsHatched,
    int EggDeaths,
    int EggPredationDeaths,
    int Deaths,
    int StarvationDeaths,
    int InjuryDeaths,
    int MaxGeneration,
    float FinalResourceRatio,
    float TotalResourceCalories,
    float TotalPlantCalories,
    float TotalMeatCalories,
    int BarrenCreatureCount,
    int SparseCreatureCount,
    int GrasslandCreatureCount,
    int RichCreatureCount,
    float AverageBiomeMovementCostMultiplier,
    float AverageBiomeBasalCostMultiplier,
    float AverageBiomeSpeedMultiplier,
    float BarrenCaloriesEatenPerSecond,
    float SparseCaloriesEatenPerSecond,
    float GrasslandCaloriesEatenPerSecond,
    float RichCaloriesEatenPerSecond,
    int BarrenDeathCount,
    int SparseDeathCount,
    int GrasslandDeathCount,
    int RichDeathCount,
    float CurrentEastProgressShare,
    float RunEastProgressShare,
    int MiddleRegionCreatureCount,
    int RightRegionCreatureCount,
    string BehaviorMovementStyle,
    string BehaviorSearchTendency,
    string BehaviorEcotype,
    string BehaviorTerrainResponse,
    string BehaviorRottenMeatResponse,
    float BehaviorFreshMeatPreferenceScore,
    float BehaviorRottenScentAvoidanceScore,
    float FoodDetectedShare,
    float PlantDetectedShare,
    float MeatDetectedShare,
    float FreshMeatDetectedShare,
    float StaleMeatDetectedShare,
    float StaleMeatAvoidedShare,
    float AverageVisibleMeatFreshness,
    float MeatScentDetectedShare,
    float RottenMeatScentDetectedShare,
    float AverageRottenMeatScentDensity,
    float CreatureDetectedShare,
    float FoodContactShare,
    float EatingShare,
    float AttackingShare,
    float VisibleFoodDensity,
    float CaloriesEatenPerSecond,
    float MeatCaloriesEatenShare,
    float FreshKillCaloriesEatenShare,
    float AverageMeatFreshness,
    float AverageCarrionAdaptation,
    float FreshMeatCaloriesEatenShare,
    float StaleMeatCaloriesEatenShare,
    float FreshMeatCaloriesEatenPerSecond,
    float StaleMeatCaloriesEatenPerSecond,
    float RottenMeatDamagePerSecond,
    float RottenMeatDamagedShare,
    float MeatDigestedEnergyShare,
    float CaloriesEatenPerDistance,
    float CaloriesDigestedPerDistance,
    float CaloriesEatenPerFoodVisionEvent,
    float AverageSecondsSinceLastMeal,
    float AverageDistanceSinceLastMeal,
    int TailSnapshotCount,
    long TailStartTick,
    long TailEndTick,
    double TailSeconds,
    float TailAverageCreatures,
    float TailAverageDietaryAdaptation,
    float TailAverageCarrionAdaptation,
    float TailFreshMeatDetectedShare,
    float TailStaleMeatDetectedShare,
    float TailStaleMeatAvoidedShare,
    float TailAverageVisibleMeatFreshness,
    float TailRottenMeatScentDetectedShare,
    float TailAverageRottenMeatScentDensity,
    float TailMeatCaloriesEatenShare,
    float TailFreshKillCaloriesEatenShare,
    float TailAverageMeatFreshness,
    float TailFreshMeatCaloriesEatenShare,
    float TailStaleMeatCaloriesEatenShare,
    float TailRottenMeatDamagePerSecond,
    float TailRottenMeatDamagedShare,
    float TailMeatDigestedEnergyShare,
    float TailAttackingShare,
    float TailDeathsPerSecond,
    float TailStarvationDeathsPerSecond,
    float TailInjuryDeathsPerSecond,
    float TailCaloriesEatenPerDistance,
    float TailAverageSecondsSinceLastMeal)
{
    public static ProbeRunResult From(
        string scenarioPath,
        string scenarioName,
        ProbeVariant variant,
        SimulationScenario scenario,
        Simulation simulation,
        TimeSpan elapsed,
        ProbeRunStatus status,
        int requestedTicks)
    {
        var state = simulation.State;
        var snapshot = state.Stats.Snapshots.Count > 0
            ? state.Stats.Snapshots[^1]
            : default;
        var resourceCapacity = state.Resources.Sum(resource => resource.MaxCalories);
        var resourceCalories = state.Resources.Sum(resource => resource.Calories);
        var wallSeconds = Math.Max(elapsed.TotalSeconds, 0.000001);
        var tail = ProbeTailSummary.From(state.Stats.Snapshots);
        var behavior = BehaviorAssay.Analyze(state);

        return new ProbeRunResult(
            scenarioName,
            scenarioPath,
            variant.Name,
            variant.OverrideSummary,
            scenario.Seed,
            status,
            requestedTicks,
            state.Tick,
            state.ElapsedSeconds,
            elapsed.TotalSeconds,
            state.Tick / wallSeconds,
            scenario.PipelineKind,
            scenario.InitialBrainKind,
            scenario.InitialCreatureCount,
            scenario.CalculateInitialResourceCount(),
            scenario.InitialResourcesPerMillionArea,
            scenario.ResourceClusterStrength,
            scenario.ResourceClusterRadius,
            state.Creatures.Count,
            state.Eggs.Count,
            state.Resources.Count,
            state.Resources.Count(resource => resource.Kind == ResourceKind.Plant),
            state.Resources.Count(resource => resource.Kind == ResourceKind.Meat),
            state.Stats.CreatureBirthCount,
            state.Stats.EggLaidCount,
            state.Stats.EggHatchedCount,
            state.Stats.EggDeathCount,
            state.Stats.EggPredationDeathCount,
            state.Stats.CreatureDeathCount,
            state.Stats.StarvationDeathCount,
            state.Stats.InjuryDeathCount,
            snapshot.MaxGeneration,
            resourceCapacity > 0f ? resourceCalories / resourceCapacity : 0f,
            snapshot.TotalResourceCalories,
            snapshot.TotalPlantCalories,
            snapshot.TotalMeatCalories,
            snapshot.BarrenCreatureCount,
            snapshot.SparseCreatureCount,
            snapshot.GrasslandCreatureCount,
            snapshot.RichCreatureCount,
            snapshot.AverageBiomeMovementCostMultiplier,
            snapshot.AverageBiomeBasalCostMultiplier,
            snapshot.AverageBiomeSpeedMultiplier,
            snapshot.BarrenCaloriesEatenPerSecond,
            snapshot.SparseCaloriesEatenPerSecond,
            snapshot.GrasslandCaloriesEatenPerSecond,
            snapshot.RichCaloriesEatenPerSecond,
            snapshot.BarrenDeathCount,
            snapshot.SparseDeathCount,
            snapshot.GrasslandDeathCount,
            snapshot.RichDeathCount,
            snapshot.CurrentEastProgressShare,
            snapshot.RunEastProgressShare,
            snapshot.MiddleRegionCreatureCount,
            snapshot.RightRegionCreatureCount,
            behavior.MovementStyle,
            behavior.SearchTendency,
            behavior.Ecotype,
            behavior.TerrainResponse,
            behavior.RottenMeatResponse,
            behavior.FreshMeatPreferenceScore,
            behavior.RottenScentAvoidanceScore,
            Share(snapshot.FoodDetectedCreatureCount, snapshot.CreatureCount),
            Share(snapshot.PlantDetectedCreatureCount, snapshot.CreatureCount),
            Share(snapshot.MeatDetectedCreatureCount, snapshot.CreatureCount),
            Share(snapshot.FreshMeatDetectedCreatureCount, snapshot.CreatureCount),
            Share(snapshot.StaleMeatDetectedCreatureCount, snapshot.CreatureCount),
            Share(snapshot.StaleMeatAvoidedCreatureCount, snapshot.CreatureCount),
            snapshot.AverageVisibleMeatFreshness,
            Share(snapshot.MeatScentDetectedCreatureCount, snapshot.CreatureCount),
            Share(snapshot.RottenMeatScentDetectedCreatureCount, snapshot.CreatureCount),
            snapshot.AverageRottenMeatScentDensity,
            Share(snapshot.CreatureDetectedCreatureCount, snapshot.CreatureCount),
            Share(snapshot.FoodContactCreatureCount, snapshot.CreatureCount),
            Share(snapshot.EatingCreatureCount, snapshot.CreatureCount),
            Share(snapshot.AttackingCreatureCount, snapshot.CreatureCount),
            snapshot.AverageVisibleFoodDensity,
            snapshot.TotalCaloriesEatenPerSecond,
            snapshot.MeatCaloriesEatenShare,
            snapshot.FreshKillCaloriesEatenShare,
            snapshot.AverageMeatFreshness,
            snapshot.AverageCarrionAdaptation,
            snapshot.FreshMeatCaloriesEatenShare,
            snapshot.StaleMeatCaloriesEatenShare,
            snapshot.TotalFreshMeatCaloriesEatenPerSecond,
            snapshot.TotalStaleMeatCaloriesEatenPerSecond,
            snapshot.TotalRottenMeatDamagePerSecond,
            Share(snapshot.RottenMeatDamagedCreatureCount, snapshot.CreatureCount),
            snapshot.MeatDigestedEnergyShare,
            snapshot.CaloriesEatenPerDistance,
            snapshot.CaloriesDigestedPerDistance,
            snapshot.CaloriesEatenPerFoodVisionEvent,
            snapshot.AverageSecondsSinceLastMeal,
            snapshot.AverageDistanceSinceLastMeal,
            tail.SnapshotCount,
            tail.StartTick,
            tail.EndTick,
            tail.Seconds,
            tail.AverageCreatures,
            tail.AverageDietaryAdaptation,
            tail.AverageCarrionAdaptation,
            tail.FreshMeatDetectedShare,
            tail.StaleMeatDetectedShare,
            tail.StaleMeatAvoidedShare,
            tail.AverageVisibleMeatFreshness,
            tail.RottenMeatScentDetectedShare,
            tail.AverageRottenMeatScentDensity,
            tail.MeatCaloriesEatenShare,
            tail.FreshKillCaloriesEatenShare,
            tail.AverageMeatFreshness,
            tail.FreshMeatCaloriesEatenShare,
            tail.StaleMeatCaloriesEatenShare,
            tail.RottenMeatDamagePerSecond,
            tail.RottenMeatDamagedShare,
            tail.MeatDigestedEnergyShare,
            tail.AttackingShare,
            tail.DeathsPerSecond,
            tail.StarvationDeathsPerSecond,
            tail.InjuryDeathsPerSecond,
            tail.CaloriesEatenPerDistance,
            tail.AverageSecondsSinceLastMeal);
    }

    private static float Share(int count, int total)
    {
        return total > 0 ? count / (float)total : 0f;
    }
}

internal readonly record struct ProbeTailSummary(
    int SnapshotCount,
    long StartTick,
    long EndTick,
    double Seconds,
    float AverageCreatures,
    float AverageDietaryAdaptation,
    float AverageCarrionAdaptation,
    float FreshMeatDetectedShare,
    float StaleMeatDetectedShare,
    float StaleMeatAvoidedShare,
    float AverageVisibleMeatFreshness,
    float RottenMeatScentDetectedShare,
    float AverageRottenMeatScentDensity,
    float MeatCaloriesEatenShare,
    float FreshKillCaloriesEatenShare,
    float AverageMeatFreshness,
    float FreshMeatCaloriesEatenShare,
    float StaleMeatCaloriesEatenShare,
    float RottenMeatDamagePerSecond,
    float RottenMeatDamagedShare,
    float MeatDigestedEnergyShare,
    float AttackingShare,
    float DeathsPerSecond,
    float StarvationDeathsPerSecond,
    float InjuryDeathsPerSecond,
    float CaloriesEatenPerDistance,
    float AverageSecondsSinceLastMeal)
{
    public static ProbeTailSummary From(IReadOnlyList<SimulationStatsSnapshot> snapshots)
    {
        if (snapshots.Count == 0)
        {
            return default;
        }

        var tailCount = Math.Min(snapshots.Count, Math.Max(3, (int)Math.Ceiling(snapshots.Count * 0.25)));
        var startIndex = snapshots.Count - tailCount;
        var first = snapshots[startIndex];
        var last = snapshots[^1];
        var seconds = Math.Max(0.0, last.ElapsedSeconds - first.ElapsedSeconds);

        var averageCreatures = 0f;
        var averageDietaryAdaptation = 0f;
        var averageCarrionAdaptation = 0f;
        var freshMeatDetectedShare = 0f;
        var staleMeatDetectedShare = 0f;
        var staleMeatAvoidedShare = 0f;
        var averageVisibleMeatFreshness = 0f;
        var rottenMeatScentDetectedShare = 0f;
        var averageRottenMeatScentDensity = 0f;
        var meatCaloriesEatenShare = 0f;
        var freshKillCaloriesEatenShare = 0f;
        var averageMeatFreshness = 0f;
        var freshMeatCaloriesEatenShare = 0f;
        var staleMeatCaloriesEatenShare = 0f;
        var rottenMeatDamagePerSecond = 0f;
        var rottenMeatDamagedShare = 0f;
        var meatDigestedEnergyShare = 0f;
        var attackingShare = 0f;
        var caloriesEatenPerDistance = 0f;
        var averageSecondsSinceLastMeal = 0f;

        for (var i = startIndex; i < snapshots.Count; i++)
        {
            var snapshot = snapshots[i];
            averageCreatures += snapshot.CreatureCount;
            averageDietaryAdaptation += snapshot.AverageDietaryAdaptation;
            averageCarrionAdaptation += snapshot.AverageCarrionAdaptation;
            freshMeatDetectedShare += Share(snapshot.FreshMeatDetectedCreatureCount, snapshot.CreatureCount);
            staleMeatDetectedShare += Share(snapshot.StaleMeatDetectedCreatureCount, snapshot.CreatureCount);
            staleMeatAvoidedShare += Share(snapshot.StaleMeatAvoidedCreatureCount, snapshot.CreatureCount);
            averageVisibleMeatFreshness += snapshot.AverageVisibleMeatFreshness;
            rottenMeatScentDetectedShare += Share(snapshot.RottenMeatScentDetectedCreatureCount, snapshot.CreatureCount);
            averageRottenMeatScentDensity += snapshot.AverageRottenMeatScentDensity;
            meatCaloriesEatenShare += snapshot.MeatCaloriesEatenShare;
            freshKillCaloriesEatenShare += snapshot.FreshKillCaloriesEatenShare;
            averageMeatFreshness += snapshot.AverageMeatFreshness;
            freshMeatCaloriesEatenShare += snapshot.FreshMeatCaloriesEatenShare;
            staleMeatCaloriesEatenShare += snapshot.StaleMeatCaloriesEatenShare;
            rottenMeatDamagePerSecond += snapshot.TotalRottenMeatDamagePerSecond;
            rottenMeatDamagedShare += Share(snapshot.RottenMeatDamagedCreatureCount, snapshot.CreatureCount);
            meatDigestedEnergyShare += snapshot.MeatDigestedEnergyShare;
            attackingShare += Share(snapshot.AttackingCreatureCount, snapshot.CreatureCount);
            caloriesEatenPerDistance += snapshot.CaloriesEatenPerDistance;
            averageSecondsSinceLastMeal += snapshot.AverageSecondsSinceLastMeal;
        }

        var divisor = tailCount;
        var deathRateDivisor = seconds > 0.0 ? (float)seconds : 0f;
        return new ProbeTailSummary(
            tailCount,
            first.Tick,
            last.Tick,
            seconds,
            averageCreatures / divisor,
            averageDietaryAdaptation / divisor,
            averageCarrionAdaptation / divisor,
            freshMeatDetectedShare / divisor,
            staleMeatDetectedShare / divisor,
            staleMeatAvoidedShare / divisor,
            averageVisibleMeatFreshness / divisor,
            rottenMeatScentDetectedShare / divisor,
            averageRottenMeatScentDensity / divisor,
            meatCaloriesEatenShare / divisor,
            freshKillCaloriesEatenShare / divisor,
            averageMeatFreshness / divisor,
            freshMeatCaloriesEatenShare / divisor,
            staleMeatCaloriesEatenShare / divisor,
            rottenMeatDamagePerSecond / divisor,
            rottenMeatDamagedShare / divisor,
            meatDigestedEnergyShare / divisor,
            attackingShare / divisor,
            Rate(last.CreatureDeathCount - first.CreatureDeathCount, deathRateDivisor),
            Rate(last.StarvationDeathCount - first.StarvationDeathCount, deathRateDivisor),
            Rate(last.InjuryDeathCount - first.InjuryDeathCount, deathRateDivisor),
            caloriesEatenPerDistance / divisor,
            averageSecondsSinceLastMeal / divisor);
    }

    private static float Share(int count, int total)
    {
        return total > 0 ? count / (float)total : 0f;
    }

    private static float Rate(int count, float seconds)
    {
        return seconds > 0f ? count / seconds : 0f;
    }
}

internal static class ProbeCsvWriter
{
    public static void Write(string path, IReadOnlyList<ProbeRunResult> results)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("scenario,scenario_path,variant,variant_overrides,seed,status,requested_ticks,final_tick,simulated_seconds,wall_seconds,ticks_per_second,pipeline,initial_brain,initial_creatures,initial_resources,resource_density_per_million,resource_cluster_strength,resource_cluster_radius,final_creatures,final_eggs,final_resources,final_plants,final_meat,births,eggs_laid,eggs_hatched,egg_deaths,egg_predation_deaths,deaths,starvation_deaths,injury_deaths,max_generation,final_resource_ratio,total_resource_calories,total_plant_calories,total_meat_calories,barren_creatures,sparse_creatures,grassland_creatures,rich_creatures,avg_biome_movement_cost,avg_biome_basal_cost,avg_biome_speed,barren_calories_eaten_per_second,sparse_calories_eaten_per_second,grassland_calories_eaten_per_second,rich_calories_eaten_per_second,barren_deaths,sparse_deaths,grassland_deaths,rich_deaths,current_east_progress_share,run_east_progress_share,middle_region_creatures,right_region_creatures,behavior_movement_style,behavior_search_tendency,behavior_ecotype,behavior_terrain_response,behavior_rotten_meat_response,behavior_fresh_meat_preference_score,behavior_rotten_scent_avoidance_score,food_detected_share,plant_detected_share,meat_detected_share,fresh_meat_detected_share,stale_meat_detected_share,stale_meat_avoided_share,avg_visible_meat_freshness,meat_scent_detected_share,rotten_meat_scent_detected_share,avg_rotten_meat_scent_density,creature_detected_share,food_contact_share,eating_share,attacking_share,visible_food_density,calories_eaten_per_second,meat_calories_eaten_share,fresh_kill_calories_eaten_share,avg_meat_freshness,avg_carrion_adaptation,fresh_meat_calories_eaten_share,stale_meat_calories_eaten_share,fresh_meat_calories_eaten_per_second,stale_meat_calories_eaten_per_second,rotten_meat_damage_per_second,rotten_meat_damaged_share,meat_digested_energy_share,calories_eaten_per_distance,calories_digested_per_distance,calories_eaten_per_food_vision_event,avg_seconds_since_last_meal,avg_distance_since_last_meal,tail_snapshot_count,tail_start_tick,tail_end_tick,tail_seconds,tail_avg_creatures,tail_avg_dietary_adaptation,tail_avg_carrion_adaptation,tail_fresh_meat_detected_share,tail_stale_meat_detected_share,tail_stale_meat_avoided_share,tail_avg_visible_meat_freshness,tail_rotten_meat_scent_detected_share,tail_avg_rotten_meat_scent_density,tail_meat_calories_eaten_share,tail_fresh_kill_calories_eaten_share,tail_avg_meat_freshness,tail_fresh_meat_calories_eaten_share,tail_stale_meat_calories_eaten_share,tail_rotten_meat_damage_per_second,tail_rotten_meat_damaged_share,tail_meat_digested_energy_share,tail_attacking_share,tail_deaths_per_second,tail_starvation_deaths_per_second,tail_injury_deaths_per_second,tail_calories_eaten_per_distance,tail_avg_seconds_since_last_meal");

        foreach (var result in results)
        {
            writer.WriteLine(string.Join(
                ',',
                Csv(result.ScenarioName),
                Csv(result.ScenarioPath),
                Csv(result.VariantName),
                Csv(result.VariantOverrides),
                result.Seed.ToString(CultureInfo.InvariantCulture),
                result.Status.ToString(),
                result.RequestedTicks.ToString(CultureInfo.InvariantCulture),
                result.FinalTick.ToString(CultureInfo.InvariantCulture),
                Format(result.SimulatedSeconds),
                result.WallSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                result.TicksPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                result.PipelineKind.ToString(),
                result.InitialBrainKind.ToString(),
                result.InitialCreatures.ToString(CultureInfo.InvariantCulture),
                result.InitialResources.ToString(CultureInfo.InvariantCulture),
                Format(result.ResourceDensityPerMillion),
                Format(result.ResourceClusterStrength),
                Format(result.ResourceClusterRadius),
                result.FinalCreatures.ToString(CultureInfo.InvariantCulture),
                result.FinalEggs.ToString(CultureInfo.InvariantCulture),
                result.FinalResources.ToString(CultureInfo.InvariantCulture),
                result.FinalPlants.ToString(CultureInfo.InvariantCulture),
                result.FinalMeat.ToString(CultureInfo.InvariantCulture),
                result.Births.ToString(CultureInfo.InvariantCulture),
                result.EggsLaid.ToString(CultureInfo.InvariantCulture),
                result.EggsHatched.ToString(CultureInfo.InvariantCulture),
                result.EggDeaths.ToString(CultureInfo.InvariantCulture),
                result.EggPredationDeaths.ToString(CultureInfo.InvariantCulture),
                result.Deaths.ToString(CultureInfo.InvariantCulture),
                result.StarvationDeaths.ToString(CultureInfo.InvariantCulture),
                result.InjuryDeaths.ToString(CultureInfo.InvariantCulture),
                result.MaxGeneration.ToString(CultureInfo.InvariantCulture),
                Format(result.FinalResourceRatio),
                Format(result.TotalResourceCalories),
                Format(result.TotalPlantCalories),
                Format(result.TotalMeatCalories),
                result.BarrenCreatureCount.ToString(CultureInfo.InvariantCulture),
                result.SparseCreatureCount.ToString(CultureInfo.InvariantCulture),
                result.GrasslandCreatureCount.ToString(CultureInfo.InvariantCulture),
                result.RichCreatureCount.ToString(CultureInfo.InvariantCulture),
                Format(result.AverageBiomeMovementCostMultiplier),
                Format(result.AverageBiomeBasalCostMultiplier),
                Format(result.AverageBiomeSpeedMultiplier),
                Format(result.BarrenCaloriesEatenPerSecond),
                Format(result.SparseCaloriesEatenPerSecond),
                Format(result.GrasslandCaloriesEatenPerSecond),
                Format(result.RichCaloriesEatenPerSecond),
                result.BarrenDeathCount.ToString(CultureInfo.InvariantCulture),
                result.SparseDeathCount.ToString(CultureInfo.InvariantCulture),
                result.GrasslandDeathCount.ToString(CultureInfo.InvariantCulture),
                result.RichDeathCount.ToString(CultureInfo.InvariantCulture),
                Format(result.CurrentEastProgressShare),
                Format(result.RunEastProgressShare),
                result.MiddleRegionCreatureCount.ToString(CultureInfo.InvariantCulture),
                result.RightRegionCreatureCount.ToString(CultureInfo.InvariantCulture),
                Csv(result.BehaviorMovementStyle),
                Csv(result.BehaviorSearchTendency),
                Csv(result.BehaviorEcotype),
                Csv(result.BehaviorTerrainResponse),
                Csv(result.BehaviorRottenMeatResponse),
                Format(result.BehaviorFreshMeatPreferenceScore),
                Format(result.BehaviorRottenScentAvoidanceScore),
                Format(result.FoodDetectedShare),
                Format(result.PlantDetectedShare),
                Format(result.MeatDetectedShare),
                Format(result.FreshMeatDetectedShare),
                Format(result.StaleMeatDetectedShare),
                Format(result.StaleMeatAvoidedShare),
                Format(result.AverageVisibleMeatFreshness),
                Format(result.MeatScentDetectedShare),
                Format(result.RottenMeatScentDetectedShare),
                Format(result.AverageRottenMeatScentDensity),
                Format(result.CreatureDetectedShare),
                Format(result.FoodContactShare),
                Format(result.EatingShare),
                Format(result.AttackingShare),
                Format(result.VisibleFoodDensity),
                Format(result.CaloriesEatenPerSecond),
                Format(result.MeatCaloriesEatenShare),
                Format(result.FreshKillCaloriesEatenShare),
                Format(result.AverageMeatFreshness),
                Format(result.AverageCarrionAdaptation),
                Format(result.FreshMeatCaloriesEatenShare),
                Format(result.StaleMeatCaloriesEatenShare),
                Format(result.FreshMeatCaloriesEatenPerSecond),
                Format(result.StaleMeatCaloriesEatenPerSecond),
                Format(result.RottenMeatDamagePerSecond),
                Format(result.RottenMeatDamagedShare),
                Format(result.MeatDigestedEnergyShare),
                Format(result.CaloriesEatenPerDistance),
                Format(result.CaloriesDigestedPerDistance),
                Format(result.CaloriesEatenPerFoodVisionEvent),
                Format(result.AverageSecondsSinceLastMeal),
                Format(result.AverageDistanceSinceLastMeal),
                result.TailSnapshotCount.ToString(CultureInfo.InvariantCulture),
                result.TailStartTick.ToString(CultureInfo.InvariantCulture),
                result.TailEndTick.ToString(CultureInfo.InvariantCulture),
                Format(result.TailSeconds),
                Format(result.TailAverageCreatures),
                Format(result.TailAverageDietaryAdaptation),
                Format(result.TailAverageCarrionAdaptation),
                Format(result.TailFreshMeatDetectedShare),
                Format(result.TailStaleMeatDetectedShare),
                Format(result.TailStaleMeatAvoidedShare),
                Format(result.TailAverageVisibleMeatFreshness),
                Format(result.TailRottenMeatScentDetectedShare),
                Format(result.TailAverageRottenMeatScentDensity),
                Format(result.TailMeatCaloriesEatenShare),
                Format(result.TailFreshKillCaloriesEatenShare),
                Format(result.TailAverageMeatFreshness),
                Format(result.TailFreshMeatCaloriesEatenShare),
                Format(result.TailStaleMeatCaloriesEatenShare),
                Format(result.TailRottenMeatDamagePerSecond),
                Format(result.TailRottenMeatDamagedShare),
                Format(result.TailMeatDigestedEnergyShare),
                Format(result.TailAttackingShare),
                Format(result.TailDeathsPerSecond),
                Format(result.TailStarvationDeathsPerSecond),
                Format(result.TailInjuryDeathsPerSecond),
                Format(result.TailCaloriesEatenPerDistance),
                Format(result.TailAverageSecondsSinceLastMeal)));
        }
    }

    private static string Format(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static string Format(double value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static string Csv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}

internal static class ProbeReportWriter
{
    public static void Write(string path, RunOptions options, IReadOnlyList<ProbeRunResult> results)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        var groups = results
            .GroupBy(result => new ProbeResultGroupKey(result.ScenarioName, result.VariantName, result.VariantOverrides))
            .OrderBy(group => group.Key.ScenarioName)
            .ThenBy(group => group.Key.VariantName)
            .ToArray();

        WriteDocumentStart(writer, "Lineage Probe Summary");
        writer.WriteLine("<header><div class=\"page-width\">");
        writer.WriteLine("<p class=\"eyebrow\">Lineage Experiment</p>");
        writer.WriteLine("<h1>Probe Summary</h1>");
        writer.WriteLine($"<p>{Html(results.Count)} lightweight runs, {Html(options.Ticks)} requested ticks each.</p>");
        writer.WriteLine("</div></header>");
        writer.WriteLine("<main class=\"page-width\">");

        writer.WriteLine("<section><h2>Overview</h2><div class=\"metric-grid\">");
        WriteMetric(writer, "Runs", results.Count.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Scenarios", results.Select(result => result.ScenarioName).Distinct().Count().ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Scenario variants", groups.Length.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Ticks requested", options.Ticks.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Total wall time", $"{results.Sum(result => result.WallSeconds):0.###} seconds");
        WriteMetric(writer, "Average ticks/s", $"{results.Average(result => result.TicksPerSecond):0.###}");
        WriteMetric(writer, "Snapshot interval", (options.ProbeSnapshotIntervalTicks ?? options.SnapshotIntervalTicksOverride ?? 100).ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Stop on extinction", options.ProbeStopOnExtinction ? "Yes" : "No");
        WriteMetric(writer, "Max population stop", options.ProbeMaxPopulation?.ToString(CultureInfo.InvariantCulture) ?? "Off");
        writer.WriteLine("</div></section>");

        writer.WriteLine("<section><h2>Scenario Summary</h2><div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Scenario</th><th>Variant</th><th>Overrides</th><th>Runs</th><th>Status</th><th>Avg final</th><th>Range</th><th>Tail pop</th><th>Avg eggs</th><th>Avg deaths</th><th>Avg injury</th><th>East max</th><th>Right now</th><th>Biome speed</th><th>Rough kcal/s</th><th>Rich kcal/s</th><th>Rough deaths</th><th>Terrain assay</th><th>Rot assay</th><th>Fresh pref</th><th>Rot avoid</th><th>Final meat</th><th>Tail meat</th><th>Tail fresh</th><th>Tail stale</th><th>Tail stale seen</th><th>Tail stale avoided</th><th>Tail rot scent</th><th>Tail rot dmg/s</th><th>Tail diet</th><th>Tail carrion</th><th>Tail attack</th><th>Tail deaths/s</th><th>kcal/distance</th><th>Ticks/s</th></tr></thead><tbody>");
        foreach (var group in groups)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(group.Key.ScenarioName)}</td>" +
                $"<td>{Html(group.Key.VariantName)}</td>" +
                $"<td>{Html(string.IsNullOrWhiteSpace(group.Key.VariantOverrides) ? "None" : group.Key.VariantOverrides)}</td>" +
                $"<td>{Html(group.Count())}</td>" +
                $"<td>{Html(FormatStatuses(group))}</td>" +
                $"<td>{Html(group.Average(result => result.FinalCreatures).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html($"{group.Min(result => result.FinalCreatures)}-{group.Max(result => result.FinalCreatures)}")}</td>" +
                $"<td>{Html(group.Average(result => result.TailAverageCreatures).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.FinalEggs).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.Deaths).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.InjuryDeaths).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.RunEastProgressShare)))}</td>" +
                $"<td>{Html(group.Average(result => result.RightRegionCreatureCount).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.AverageBiomeSpeedMultiplier).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.BarrenCaloriesEatenPerSecond + result.SparseCaloriesEatenPerSecond).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.RichCaloriesEatenPerSecond).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.BarrenDeathCount + result.SparseDeathCount).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatDistinct(group.Select(result => result.BehaviorTerrainResponse)))}</td>" +
                $"<td>{Html(FormatDistinct(group.Select(result => result.BehaviorRottenMeatResponse)))}</td>" +
                $"<td>{Html(group.Average(result => result.BehaviorFreshMeatPreferenceScore).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.BehaviorRottenScentAvoidanceScore).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.MeatCaloriesEatenShare)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailMeatCaloriesEatenShare)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailAverageMeatFreshness)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailStaleMeatCaloriesEatenShare)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailStaleMeatDetectedShare)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailStaleMeatAvoidedShare)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailRottenMeatScentDetectedShare)))}</td>" +
                $"<td>{Html(group.Average(result => result.TailRottenMeatDamagePerSecond).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.TailAverageDietaryAdaptation).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.TailAverageCarrionAdaptation).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailAttackingShare)))}</td>" +
                $"<td>{Html(group.Average(result => result.TailDeathsPerSecond).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.CaloriesEatenPerDistance).ToString("0.####", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.TicksPerSecond).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div></section>");

        writer.WriteLine("<section><h2>Run Rows</h2><div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Scenario</th><th>Variant</th><th>Seed</th><th>Status</th><th>Tick</th><th>Wall</th><th>Ticks/s</th><th>Final pop</th><th>Tail pop</th><th>Eggs</th><th>Deaths</th><th>Injury</th><th>Max gen</th><th>East now</th><th>East max</th><th>Middle</th><th>Right</th><th>Biome speed</th><th>Rough kcal/s</th><th>Rich kcal/s</th><th>Rough deaths</th><th>Terrain assay</th><th>Rot assay</th><th>Fresh pref</th><th>Rot avoid</th><th>Tail window</th><th>Food seen</th><th>Final meat</th><th>Tail meat</th><th>Tail fresh</th><th>Tail stale</th><th>Tail stale seen</th><th>Tail stale avoided</th><th>Tail rot scent</th><th>Tail rot dmg/s</th><th>Tail diet</th><th>Tail carrion</th><th>Tail attack</th><th>Tail deaths/s</th><th>kcal/distance</th></tr></thead><tbody>");
        foreach (var result in results
            .OrderBy(result => result.ScenarioName)
            .ThenBy(result => result.VariantName)
            .ThenBy(result => result.Seed))
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(result.ScenarioName)}</td>" +
                $"<td>{Html(result.VariantName)}</td>" +
                $"<td>{Html(result.Seed)}</td>" +
                $"<td>{Html(result.Status)}</td>" +
                $"<td>{Html(result.FinalTick)}</td>" +
                $"<td>{Html($"{result.WallSeconds:0.###}s")}</td>" +
                $"<td>{Html(result.TicksPerSecond.ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.FinalCreatures)}</td>" +
                $"<td>{Html(result.TailAverageCreatures.ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.FinalEggs)}</td>" +
                $"<td>{Html(result.Deaths)}</td>" +
                $"<td>{Html(result.InjuryDeaths)}</td>" +
                $"<td>{Html(result.MaxGeneration)}</td>" +
                $"<td>{Html(FormatPercent(result.CurrentEastProgressShare))}</td>" +
                $"<td>{Html(FormatPercent(result.RunEastProgressShare))}</td>" +
                $"<td>{Html(result.MiddleRegionCreatureCount)}</td>" +
                $"<td>{Html(result.RightRegionCreatureCount)}</td>" +
                $"<td>{Html(result.AverageBiomeSpeedMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html((result.BarrenCaloriesEatenPerSecond + result.SparseCaloriesEatenPerSecond).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.RichCaloriesEatenPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.BarrenDeathCount + result.SparseDeathCount)}</td>" +
                $"<td>{Html(result.BehaviorTerrainResponse)}</td>" +
                $"<td>{Html(result.BehaviorRottenMeatResponse)}</td>" +
                $"<td>{Html(result.BehaviorFreshMeatPreferenceScore.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.BehaviorRottenScentAvoidanceScore.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html($"{result.TailStartTick}-{result.TailEndTick}")}</td>" +
                $"<td>{Html(FormatPercent(result.FoodDetectedShare))}</td>" +
                $"<td>{Html(FormatPercent(result.MeatCaloriesEatenShare))}</td>" +
                $"<td>{Html(FormatPercent(result.TailMeatCaloriesEatenShare))}</td>" +
                $"<td>{Html(FormatPercent(result.TailAverageMeatFreshness))}</td>" +
                $"<td>{Html(FormatPercent(result.TailStaleMeatCaloriesEatenShare))}</td>" +
                $"<td>{Html(FormatPercent(result.TailStaleMeatDetectedShare))}</td>" +
                $"<td>{Html(FormatPercent(result.TailStaleMeatAvoidedShare))}</td>" +
                $"<td>{Html(FormatPercent(result.TailRottenMeatScentDetectedShare))}</td>" +
                $"<td>{Html(result.TailRottenMeatDamagePerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.TailAverageDietaryAdaptation.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.TailAverageCarrionAdaptation.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(result.TailAttackingShare))}</td>" +
                $"<td>{Html(result.TailDeathsPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.CaloriesEatenPerDistance.ToString("0.####", CultureInfo.InvariantCulture))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div></section>");
        writer.WriteLine("</main>");
        writer.WriteLine("</body></html>");
    }

    private static string FormatStatuses(IEnumerable<ProbeRunResult> results)
    {
        return string.Join(", ", results.Select(result => result.Status.ToString()).Distinct().Order());
    }

    private static string FormatDistinct(IEnumerable<string> values)
    {
        var distinct = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .Order()
            .ToArray();
        return distinct.Length == 0 ? "-" : string.Join("; ", distinct);
    }

    private static string FormatPercent(double value)
    {
        return $"{value * 100.0:0.##}%";
    }

    private static double Share(int count, int total)
    {
        return total > 0 ? count / (double)total : 0.0;
    }

    private static void WriteDocumentStart(StreamWriter writer, string title)
    {
        writer.WriteLine("<!doctype html>");
        writer.WriteLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        writer.WriteLine($"<title>{Html(title)}</title>");
        writer.WriteLine(
            """
            <style>
            :root { color-scheme: light; --bg:#f6f7f2; --text:#172015; --muted:#62705e; --panel:#fff; --line:#dfe5d9; --accent:#2f7d45; }
            body { margin:0; background:var(--bg); color:var(--text); font-family:"Segoe UI", system-ui, sans-serif; line-height:1.45; }
            header { padding:34px 0 24px; background:#162015; color:#f4f7ef; }
            .page-width { width:min(1180px, calc(100% - 32px)); margin:0 auto; }
            .eyebrow { margin:0 0 6px; color:#a9c9aa; font-size:.78rem; text-transform:uppercase; }
            h1,h2 { margin:0; } h1 { font-size:2rem; } h2 { margin-bottom:14px; font-size:1.15rem; }
            main { padding:22px 0 40px; } section { margin-top:16px; padding:18px; background:var(--panel); border:1px solid var(--line); border-radius:8px; }
            .metric-grid { display:grid; grid-template-columns:repeat(auto-fit, minmax(190px, 1fr)); gap:10px; }
            .metric { padding:10px 12px; border:1px solid var(--line); border-radius:6px; background:#fbfcf8; }
            .metric-label { color:var(--muted); font-size:.75rem; text-transform:uppercase; }
            .metric-value { display:block; margin-top:4px; overflow-wrap:anywhere; font-weight:650; }
            .table-wrap { overflow-x:auto; } table { width:100%; border-collapse:collapse; font-size:.92rem; }
            th,td { padding:8px 10px; border-bottom:1px solid var(--line); text-align:right; white-space:nowrap; }
            th:first-child,td:first-child { text-align:left; } th { color:var(--muted); font-size:.76rem; text-transform:uppercase; }
            </style>
            """);
        writer.WriteLine("</head><body>");
    }

    private static void WriteMetric(StreamWriter writer, string label, string value)
    {
        writer.WriteLine("<div class=\"metric\">");
        writer.WriteLine($"<span class=\"metric-label\">{Html(label)}</span>");
        writer.WriteLine($"<span class=\"metric-value\">{Html(value)}</span>");
        writer.WriteLine("</div>");
    }

    private static string Html(object? value)
    {
        return WebUtility.HtmlEncode(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
    }
}

internal static class StatsCsvWriter
{
    public static void Write(string path, IReadOnlyList<SimulationStatsSnapshot> snapshots)
    {
        using var writer = CreateWriter(path);
        writer.WriteLine("tick,elapsed_seconds,season_phase,season_fertility_multiplier,creatures,eggs,resources,plant_resources,meat_resources,dormant_plant_resources,total_dormant_plant_seconds_remaining,avg_dormant_plant_seconds_remaining,plant_patch_occupied_cell_share,plant_patch_top_decile_calories_share,plant_patchiness,genomes,brains,avg_brain_hidden_nodes,max_brain_hidden_nodes,avg_hidden_input_weight_magnitude,avg_hidden_output_weight_magnitude,active_hidden_output_share,max_generation,total_creature_energy,total_egg_energy,total_egg_health,total_resource_calories,total_plant_calories,total_meat_calories,barren_creatures,barren_creature_share,sparse_creatures,sparse_creature_share,grassland_creatures,grassland_creature_share,rich_creatures,rich_creature_share,avg_biome_movement_cost,avg_biome_basal_cost,avg_biome_speed,obstacle_blocked_creatures,obstacle_blocked_share,obstacle_sensed_creatures,obstacle_sensed_share,avg_forward_obstacle,avg_left_obstacle,avg_right_obstacle,barren_plant_calories,sparse_plant_calories,grassland_plant_calories,rich_plant_calories,barren_meat_calories,sparse_meat_calories,grassland_meat_calories,rich_meat_calories,barren_calories_eaten_per_second,sparse_calories_eaten_per_second,grassland_calories_eaten_per_second,rich_calories_eaten_per_second,barren_deaths,sparse_deaths,grassland_deaths,rich_deaths,avg_creature_x,max_creature_x,avg_max_creature_x_reached,max_creature_x_reached,run_max_creature_x_reached,current_east_progress_share,run_east_progress_share,food_detected_creatures,food_detected_share,plant_detected_creatures,plant_detected_share,meat_detected_creatures,meat_detected_share,meat_scent_detected_creatures,meat_scent_detected_share,creature_detected_creatures,creature_detected_share,food_contact_creatures,food_contact_share,eating_creatures,eating_share,attacking_creatures,attacking_share,avg_visible_food_density,avg_visible_plant_density,avg_visible_meat_density,fresh_meat_detected_creatures,fresh_meat_detected_share,stale_meat_detected_creatures,stale_meat_detected_share,stale_meat_avoided_creatures,stale_meat_avoided_share,avg_visible_meat_freshness,avg_meat_scent_density,rotten_meat_scent_detected_creatures,rotten_meat_scent_detected_share,avg_rotten_meat_scent_density,avg_visible_creature_density,total_calories_eaten_per_second,plant_calories_eaten_per_second,carcass_calories_eaten_per_second,egg_calories_eaten_per_second,live_prey_calories_eaten_per_second,meat_calories_eaten_share,fresh_kill_calories_eaten_share,total_calories_digested_per_second,plant_digested_energy_per_second,meat_digested_energy_per_second,meat_digested_energy_share,avg_gut_fill_ratio,avg_gut_plant_share,avg_gut_meat_share,avg_dietary_adaptation,avg_carrion_adaptation,avg_bite_strength,avg_damage_resistance,attacker_avg_dietary_adaptation,attacker_avg_bite_strength,attacker_avg_damage_resistance,non_attacker_avg_dietary_adaptation,non_attacker_avg_bite_strength,non_attacker_avg_damage_resistance,total_attack_damage_per_second,avg_seconds_since_last_meal,total_distance_traveled_per_second,avg_distance_since_last_meal,calories_eaten_per_distance,calories_digested_per_distance,calories_eaten_per_food_vision_event,avg_birth_investment_ratio,avg_egg_health_ratio,avg_vision_range,avg_vision_angle_degrees,births,eggs_laid,reproduction_attempts,eggs_hatched,egg_deaths,egg_predation_deaths,deaths,starvation_deaths,injury_deaths,rotten_meat_deaths,plant_depletions,plant_local_dispersals,plant_cluster_relocations,plant_global_relocations,plant_dormancy_started,plant_dormancy_completed,avg_plant_dormancy_scheduled_seconds,avg_plant_dormancy_completed_seconds,avg_meat_freshness,fresh_meat_calories_eaten_per_second,stale_meat_calories_eaten_per_second,fresh_meat_calories_eaten_share,stale_meat_calories_eaten_share,rotten_meat_damage_per_second,rotten_meat_damaged_creatures,rotten_meat_damaged_share,avg_lifespan_seconds,median_lifespan_seconds,reproduction_ready_creatures,reproduction_ready_share,reproduction_intent_creatures,reproduction_intent_share,avg_egg_reserve_ratio,avg_energy_surplus_ratio,avg_recent_food_success,active_memory_creatures,active_memory_share,avg_memory_strength,memory_food_contact_share,non_memory_food_contact_share,memory_eating_share,non_memory_eating_share,memory_calories_eaten_per_distance,non_memory_calories_eaten_per_distance,memory_avg_seconds_since_last_meal,non_memory_avg_seconds_since_last_meal,memory_avg_distance_since_last_meal,non_memory_avg_distance_since_last_meal,memory_avg_recent_food_success,non_memory_avg_recent_food_success,memory_avg_generation,non_memory_avg_generation,memory_avg_max_x_progress_share,non_memory_avg_max_x_progress_share,memory_right_region_share,non_memory_right_region_share,left_region_creatures,left_region_creature_share,middle_region_creatures,middle_region_creature_share,right_region_creatures,right_region_creature_share,left_region_eggs,middle_region_eggs,right_region_eggs,left_region_plant_calories,middle_region_plant_calories,right_region_plant_calories,left_region_meat_calories,middle_region_meat_calories,right_region_meat_calories,left_region_avg_generation,middle_region_avg_generation,right_region_avg_generation,left_region_season_fertility,middle_region_season_fertility,right_region_season_fertility");

        foreach (var snapshot in snapshots)
        {
            writer.WriteLine(string.Join(
                ',',
                snapshot.Tick.ToString(CultureInfo.InvariantCulture),
                snapshot.ElapsedSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.SeasonPhase.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.SeasonFertilityMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggCount.ToString(CultureInfo.InvariantCulture),
                snapshot.ResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.MeatResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.DormantPlantResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.TotalDormantPlantSecondsRemaining.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageDormantPlantSecondsRemaining.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.PlantPatchOccupiedCellShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.PlantPatchTopDecileCaloriesShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.PlantPatchiness.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.GenomeCount.ToString(CultureInfo.InvariantCulture),
                snapshot.BrainCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageBrainHiddenNodeCount.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxBrainHiddenNodeCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageBrainHiddenInputWeightMagnitude.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageBrainHiddenOutputWeightMagnitude.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ActiveBrainHiddenOutputShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxGeneration.ToString(CultureInfo.InvariantCulture),
                snapshot.TotalCreatureEnergy.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalEggEnergy.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalEggHealth.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalResourceCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.BarrenCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.BarrenCreatureCount, snapshot.CreatureCount),
                snapshot.SparseCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.SparseCreatureCount, snapshot.CreatureCount),
                snapshot.GrasslandCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.GrasslandCreatureCount, snapshot.CreatureCount),
                snapshot.RichCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RichCreatureCount, snapshot.CreatureCount),
                snapshot.AverageBiomeMovementCostMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageBiomeBasalCostMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageBiomeSpeedMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ObstacleBlockedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.ObstacleBlockedCreatureCount, snapshot.CreatureCount),
                snapshot.ObstacleSensedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.ObstacleSensedCreatureCount, snapshot.CreatureCount),
                snapshot.AverageForwardObstacle.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageLeftObstacle.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageRightObstacle.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.BarrenPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.SparsePlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.GrasslandPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RichPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.BarrenMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.SparseMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.GrasslandMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RichMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.BarrenCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.SparseCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.GrasslandCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RichCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.BarrenDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.SparseDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.GrasslandDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.RichDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageCreatureX.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxCreatureX.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageMaxCreatureXReached.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxCreatureXReached.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RunMaxCreatureXReached.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CurrentEastProgressShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RunEastProgressShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.FoodDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.FoodDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.PlantDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.PlantDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.MeatDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.MeatDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.MeatScentDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.MeatScentDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.CreatureDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.CreatureDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.FoodContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.FoodContactCreatureCount, snapshot.CreatureCount),
                snapshot.EatingCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.EatingCreatureCount, snapshot.CreatureCount),
                snapshot.AttackingCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.AttackingCreatureCount, snapshot.CreatureCount),
                snapshot.AverageVisibleFoodDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageVisiblePlantDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageVisibleMeatDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.FreshMeatDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.FreshMeatDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.StaleMeatDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.StaleMeatDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.StaleMeatAvoidedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.StaleMeatAvoidedCreatureCount, snapshot.CreatureCount),
                snapshot.AverageVisibleMeatFreshness.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageMeatScentDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RottenMeatScentDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RottenMeatScentDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.AverageRottenMeatScentDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageVisibleCreatureDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalPlantCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalCarcassCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalEggCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalLivePreyCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MeatCaloriesEatenShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.FreshKillCaloriesEatenShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalCaloriesDigestedPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalPlantDigestedEnergyPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalMeatDigestedEnergyPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MeatDigestedEnergyShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageGutFillRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageGutPlantShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageGutMeatShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageDietaryAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageCarrionAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageBiteStrength.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageDamageResistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AttackerAverageDietaryAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AttackerAverageBiteStrength.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AttackerAverageDamageResistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonAttackerAverageDietaryAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonAttackerAverageBiteStrength.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonAttackerAverageDamageResistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalAttackDamagePerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageSecondsSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalDistanceTraveledPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageDistanceSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CaloriesEatenPerDistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CaloriesDigestedPerDistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CaloriesEatenPerFoodVisionEvent.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageBirthInvestmentRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageEggHealthRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageVisionRange.ToString("0.######", CultureInfo.InvariantCulture),
                ToDegrees(snapshot.AverageVisionAngleRadians).ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CreatureBirthCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggLaidCount.ToString(CultureInfo.InvariantCulture),
                snapshot.ReproductionAttemptCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggHatchedCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggPredationDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.CreatureDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.StarvationDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.InjuryDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.RottenMeatDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantDepletionCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantLocalDispersalCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantClusterRelocationCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantGlobalRelocationCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantDormancyStartedCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantDormancyCompletedCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AveragePlantDormancyScheduledSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AveragePlantDormancyCompletedSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageMeatFreshness.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalFreshMeatCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalStaleMeatCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.FreshMeatCaloriesEatenShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.StaleMeatCaloriesEatenShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalRottenMeatDamagePerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RottenMeatDamagedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RottenMeatDamagedCreatureCount, snapshot.CreatureCount),
                snapshot.AverageLifespanSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MedianLifespanSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ReproductionReadyCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.ReproductionReadyCreatureCount, snapshot.CreatureCount),
                snapshot.ReproductionIntentCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.ReproductionIntentCreatureCount, snapshot.CreatureCount),
                snapshot.AverageEggReserveRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageEnergySurplusRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageRecentFoodSuccess.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ActiveMemoryCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.ActiveMemoryCreatureCount, snapshot.CreatureCount),
                snapshot.AverageMemoryStrength.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserFoodContactShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserFoodContactShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserEatingShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserEatingShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserCaloriesEatenPerDistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserCaloriesEatenPerDistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserAverageSecondsSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserAverageSecondsSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserAverageDistanceSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserAverageDistanceSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserAverageRecentFoodSuccess.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserAverageRecentFoodSuccess.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserAverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserAverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserAverageMaxXProgressShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserAverageMaxXProgressShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserRightRegionShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserRightRegionShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.LeftRegionCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.LeftRegionCreatureCount, snapshot.CreatureCount),
                snapshot.MiddleRegionCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.MiddleRegionCreatureCount, snapshot.CreatureCount),
                snapshot.RightRegionCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RightRegionCreatureCount, snapshot.CreatureCount),
                snapshot.LeftRegionEggCount.ToString(CultureInfo.InvariantCulture),
                snapshot.MiddleRegionEggCount.ToString(CultureInfo.InvariantCulture),
                snapshot.RightRegionEggCount.ToString(CultureInfo.InvariantCulture),
                snapshot.LeftRegionPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MiddleRegionPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RightRegionPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.LeftRegionMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MiddleRegionMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RightRegionMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.LeftRegionAverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MiddleRegionAverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RightRegionAverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.LeftRegionSeasonFertilityMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MiddleRegionSeasonFertilityMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RightRegionSeasonFertilityMultiplier.ToString("0.######", CultureInfo.InvariantCulture)));
        }
    }

    internal static StreamWriter CreateWriter(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new StreamWriter(path);
    }

    private static string FormatShare(int count, int total)
    {
        return (total > 0 ? count / (float)total : 0f).ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static float ToDegrees(float radians)
    {
        return radians * 180f / MathF.PI;
    }
}

internal static class LineageCsvWriter
{
    public static void Write(string path, IReadOnlyList<CreatureLineageRecord> records)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("id,parent_id,birth_tick,birth_elapsed_seconds,generation,genome_id,brain_id,birth_energy,max_x_reached,death_tick,death_elapsed_seconds,death_reason,is_founder,is_alive");

        foreach (var record in records)
        {
            writer.WriteLine(string.Join(
                ',',
                record.Id.Value.ToString(CultureInfo.InvariantCulture),
                record.ParentId.Value.ToString(CultureInfo.InvariantCulture),
                record.BirthTick.ToString(CultureInfo.InvariantCulture),
                record.BirthElapsedSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                record.Generation.ToString(CultureInfo.InvariantCulture),
                record.GenomeId.ToString(CultureInfo.InvariantCulture),
                record.BrainId.ToString(CultureInfo.InvariantCulture),
                record.BirthEnergy.ToString("0.######", CultureInfo.InvariantCulture),
                record.MaxXReached.ToString("0.######", CultureInfo.InvariantCulture),
                record.DeathTick?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                record.DeathElapsedSeconds?.ToString("0.######", CultureInfo.InvariantCulture) ?? string.Empty,
                record.DeathReason?.ToString() ?? string.Empty,
                record.IsFounder.ToString(CultureInfo.InvariantCulture),
                record.IsAlive.ToString(CultureInfo.InvariantCulture)));
        }
    }
}

internal static class TraitSummaryCsvWriter
{
    public static void Write(string path, WorldState state)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("scope,count,avg_body_radius,min_body_radius,max_body_radius,avg_max_speed,min_max_speed,max_max_speed,avg_vision_range,min_vision_range,max_vision_range,avg_vision_angle_degrees,min_vision_angle_degrees,max_vision_angle_degrees,avg_reproduction_threshold,min_reproduction_threshold,max_reproduction_threshold,avg_offspring_investment,min_offspring_investment,max_offspring_investment,avg_egg_production_per_second,min_egg_production_per_second,max_egg_production_per_second,avg_egg_incubation_seconds,min_egg_incubation_seconds,max_egg_incubation_seconds,avg_maturity_age_seconds,min_maturity_age_seconds,max_maturity_age_seconds,avg_dietary_adaptation,min_dietary_adaptation,max_dietary_adaptation,avg_carrion_adaptation,min_carrion_adaptation,max_carrion_adaptation,avg_plant_digestion,min_plant_digestion,max_plant_digestion,avg_meat_digestion,min_meat_digestion,max_meat_digestion,avg_fresh_meat_digestion,min_fresh_meat_digestion,max_fresh_meat_digestion,avg_stale_meat_digestion,min_stale_meat_digestion,max_stale_meat_digestion,avg_gut_capacity,min_gut_capacity,max_gut_capacity,avg_digestion_rate,min_digestion_rate,max_digestion_rate,avg_bite_strength,min_bite_strength,max_bite_strength,avg_damage_resistance,min_damage_resistance,max_damage_resistance,avg_mutation_strength,min_mutation_strength,max_mutation_strength,avg_trait_mutation_rate,min_trait_mutation_rate,max_trait_mutation_rate,avg_brain_mutation_rate,min_brain_mutation_rate,max_brain_mutation_rate");

        if (state.Creatures.Count == 0)
        {
            writer.WriteLine("living_creatures,0" + new string(',', 66));
            return;
        }

        var summary = TraitAccumulator.FromLivingCreatures(state);
        writer.WriteLine(string.Join(
            ',',
            "living_creatures",
            summary.Count.ToString(CultureInfo.InvariantCulture),
            Format(summary.BodyRadius.Average),
            Format(summary.BodyRadius.Min),
            Format(summary.BodyRadius.Max),
            Format(summary.MaxSpeed.Average),
            Format(summary.MaxSpeed.Min),
            Format(summary.MaxSpeed.Max),
            Format(summary.SenseRadius.Average),
            Format(summary.SenseRadius.Min),
            Format(summary.SenseRadius.Max),
            Format(ToDegrees(summary.VisionAngleRadians.Average)),
            Format(ToDegrees(summary.VisionAngleRadians.Min)),
            Format(ToDegrees(summary.VisionAngleRadians.Max)),
            Format(summary.ReproductionThreshold.Average),
            Format(summary.ReproductionThreshold.Min),
            Format(summary.ReproductionThreshold.Max),
            Format(summary.OffspringInvestment.Average),
            Format(summary.OffspringInvestment.Min),
            Format(summary.OffspringInvestment.Max),
            Format(summary.EggProductionEnergyPerSecond.Average),
            Format(summary.EggProductionEnergyPerSecond.Min),
            Format(summary.EggProductionEnergyPerSecond.Max),
            Format(summary.EggIncubationSeconds.Average),
            Format(summary.EggIncubationSeconds.Min),
            Format(summary.EggIncubationSeconds.Max),
            Format(summary.MaturityAgeSeconds.Average),
            Format(summary.MaturityAgeSeconds.Min),
            Format(summary.MaturityAgeSeconds.Max),
            Format(summary.DietaryAdaptation.Average),
            Format(summary.DietaryAdaptation.Min),
            Format(summary.DietaryAdaptation.Max),
            Format(summary.CarrionAdaptation.Average),
            Format(summary.CarrionAdaptation.Min),
            Format(summary.CarrionAdaptation.Max),
            Format(summary.PlantDigestion.Average),
            Format(summary.PlantDigestion.Min),
            Format(summary.PlantDigestion.Max),
            Format(summary.MeatDigestion.Average),
            Format(summary.MeatDigestion.Min),
            Format(summary.MeatDigestion.Max),
            Format(summary.FreshMeatDigestion.Average),
            Format(summary.FreshMeatDigestion.Min),
            Format(summary.FreshMeatDigestion.Max),
            Format(summary.StaleMeatDigestion.Average),
            Format(summary.StaleMeatDigestion.Min),
            Format(summary.StaleMeatDigestion.Max),
            Format(summary.GutCapacityCalories.Average),
            Format(summary.GutCapacityCalories.Min),
            Format(summary.GutCapacityCalories.Max),
            Format(summary.DigestionCaloriesPerSecond.Average),
            Format(summary.DigestionCaloriesPerSecond.Min),
            Format(summary.DigestionCaloriesPerSecond.Max),
            Format(summary.BiteStrength.Average),
            Format(summary.BiteStrength.Min),
            Format(summary.BiteStrength.Max),
            Format(summary.DamageResistance.Average),
            Format(summary.DamageResistance.Min),
            Format(summary.DamageResistance.Max),
            Format(summary.MutationStrength.Average),
            Format(summary.MutationStrength.Min),
            Format(summary.MutationStrength.Max),
            Format(summary.TraitMutationRate.Average),
            Format(summary.TraitMutationRate.Min),
            Format(summary.TraitMutationRate.Max),
            Format(summary.BrainMutationRate.Average),
            Format(summary.BrainMutationRate.Min),
            Format(summary.BrainMutationRate.Max)));
    }

    private static string Format(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static float ToDegrees(float radians)
    {
        return radians * 180f / MathF.PI;
    }
}

internal static class FounderSummaryCsvWriter
{
    public static void Write(string path, IReadOnlyList<CreatureLineageRecord> records)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("founder_id,total_creatures,descendant_count,living_creatures,dead_creatures,max_generation");

        foreach (var summary in Summarize(records).OrderBy(summary => summary.FounderId.Value))
        {
            writer.WriteLine(string.Join(
                ',',
                summary.FounderId.Value.ToString(CultureInfo.InvariantCulture),
                summary.TotalCreatures.ToString(CultureInfo.InvariantCulture),
                summary.DescendantCount.ToString(CultureInfo.InvariantCulture),
                summary.LivingCreatures.ToString(CultureInfo.InvariantCulture),
                summary.DeadCreatures.ToString(CultureInfo.InvariantCulture),
                summary.MaxGeneration.ToString(CultureInfo.InvariantCulture)));
        }
    }

    public static IReadOnlyList<FounderSummary> Summarize(IReadOnlyList<CreatureLineageRecord> records)
    {
        var byId = records.ToDictionary(record => record.Id);
        var summaries = new Dictionary<EntityId, FounderAccumulator>();

        foreach (var record in records)
        {
            var founderId = FindFounderId(record, byId);
            summaries.TryGetValue(founderId, out var summary);
            summary.TotalCreatures++;
            summary.LivingCreatures += record.IsAlive ? 1 : 0;
            summary.DeadCreatures += record.IsAlive ? 0 : 1;
            summary.MaxGeneration = Math.Max(summary.MaxGeneration, record.Generation);
            summaries[founderId] = summary;
        }

        return summaries
            .Select(pair => new FounderSummary(
                pair.Key,
                pair.Value.TotalCreatures,
                Math.Max(0, pair.Value.TotalCreatures - 1),
                pair.Value.LivingCreatures,
                pair.Value.DeadCreatures,
                pair.Value.MaxGeneration))
            .ToArray();
    }

    public static EntityId FindFounderId(
        CreatureLineageRecord record,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> byId)
    {
        var current = record;
        while (!current.IsFounder && byId.TryGetValue(current.ParentId, out var parent))
        {
            current = parent;
        }

        return current.Id;
    }

    private struct FounderAccumulator
    {
        public int TotalCreatures;
        public int LivingCreatures;
        public int DeadCreatures;
        public int MaxGeneration;
    }
}

internal readonly record struct FounderSummary(
    EntityId FounderId,
    int TotalCreatures,
    int DescendantCount,
    int LivingCreatures,
    int DeadCreatures,
    int MaxGeneration);

internal static class GenerationSummaryCsvWriter
{
    public static void Write(string path, IReadOnlyList<CreatureLineageRecord> records)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("generation,births,living,dead,starvation_deaths,injury_deaths,rotten_meat_deaths,survival_rate");

        foreach (var summary in Summarize(records))
        {
            writer.WriteLine(string.Join(
                ',',
                summary.Generation.ToString(CultureInfo.InvariantCulture),
                summary.Births.ToString(CultureInfo.InvariantCulture),
                summary.Living.ToString(CultureInfo.InvariantCulture),
                summary.Dead.ToString(CultureInfo.InvariantCulture),
                summary.StarvationDeaths.ToString(CultureInfo.InvariantCulture),
                summary.InjuryDeaths.ToString(CultureInfo.InvariantCulture),
                summary.RottenMeatDeaths.ToString(CultureInfo.InvariantCulture),
                summary.SurvivalRate.ToString("0.######", CultureInfo.InvariantCulture)));
        }
    }

    public static IReadOnlyList<GenerationSummary> Summarize(IReadOnlyList<CreatureLineageRecord> records)
    {
        return records
            .GroupBy(record => record.Generation)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var births = group.Count();
                var living = group.Count(record => record.IsAlive);
                var starvationDeaths = group.Count(record => record.DeathReason == CreatureDeathReason.Starvation);
                var injuryDeaths = group.Count(record => record.DeathReason == CreatureDeathReason.Injury);
                var rottenMeatDeaths = group.Count(record => record.DeathReason == CreatureDeathReason.RottenMeat);

                return new GenerationSummary(
                    group.Key,
                    births,
                    living,
                    births - living,
                    starvationDeaths,
                    injuryDeaths,
                    rottenMeatDeaths);
            })
            .ToArray();
    }
}

internal readonly record struct GenerationSummary(
    int Generation,
    int Births,
    int Living,
    int Dead,
    int StarvationDeaths,
    int InjuryDeaths,
    int RottenMeatDeaths)
{
    public float SurvivalRate => Births > 0 ? Living / (float)Births : 0f;
}

internal static class LineageTrendCsvWriter
{
    private const int DefaultMaxRowsPerSnapshot = 10;

    public static void Write(
        string path,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        IReadOnlyList<CreatureLineageRecord> records)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("tick,elapsed_seconds,rank,founder_id,living_creatures,total_living,living_share,founder_min_generation,founder_avg_generation,founder_max_generation,overall_min_generation,overall_avg_generation,overall_max_generation");

        foreach (var row in Summarize(snapshots, records))
        {
            writer.WriteLine(string.Join(
                ',',
                row.Tick.ToString(CultureInfo.InvariantCulture),
                row.ElapsedSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                row.Rank.ToString(CultureInfo.InvariantCulture),
                row.FounderId.Value.ToString(CultureInfo.InvariantCulture),
                row.LivingCreatures.ToString(CultureInfo.InvariantCulture),
                row.TotalLiving.ToString(CultureInfo.InvariantCulture),
                row.LivingShare.ToString("0.######", CultureInfo.InvariantCulture),
                row.FounderMinGeneration.ToString(CultureInfo.InvariantCulture),
                row.FounderAverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                row.FounderMaxGeneration.ToString(CultureInfo.InvariantCulture),
                row.OverallMinGeneration.ToString(CultureInfo.InvariantCulture),
                row.OverallAverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                row.OverallMaxGeneration.ToString(CultureInfo.InvariantCulture)));
        }
    }

    public static IReadOnlyList<LineageTrendRow> Summarize(
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        IReadOnlyList<CreatureLineageRecord> records,
        int maxRowsPerSnapshot = DefaultMaxRowsPerSnapshot)
    {
        if (snapshots.Count == 0 || records.Count == 0 || maxRowsPerSnapshot <= 0)
        {
            return Array.Empty<LineageTrendRow>();
        }

        var byId = records.ToDictionary(record => record.Id);
        var founderByCreature = records.ToDictionary(
            record => record.Id,
            record => FounderSummaryCsvWriter.FindFounderId(record, byId));
        var births = records
            .OrderBy(record => record.BirthTick)
            .ThenBy(record => record.Id.Value)
            .ToArray();
        var deaths = records
            .Where(record => record.DeathTick is not null)
            .OrderBy(record => record.DeathTick!.Value)
            .ThenBy(record => record.Id.Value)
            .ToArray();
        var orderedSnapshots = snapshots
            .OrderBy(snapshot => snapshot.Tick)
            .ToArray();

        var rows = new List<LineageTrendRow>();
        var activeFounders = new Dictionary<EntityId, GenerationAccumulator>();
        var overallGenerations = new GenerationAccumulator();
        var birthIndex = 0;
        var deathIndex = 0;

        foreach (var snapshot in orderedSnapshots)
        {
            while (birthIndex < births.Length && births[birthIndex].BirthTick <= snapshot.Tick)
            {
                AddActiveCreature(activeFounders, overallGenerations, births[birthIndex], founderByCreature);
                birthIndex++;
            }

            while (deathIndex < deaths.Length && deaths[deathIndex].DeathTick!.Value <= snapshot.Tick)
            {
                RemoveActiveCreature(activeFounders, overallGenerations, deaths[deathIndex], founderByCreature);
                deathIndex++;
            }

            if (overallGenerations.Count == 0)
            {
                continue;
            }

            var rank = 1;
            foreach (var pair in activeFounders
                .Where(pair => pair.Value.Count > 0)
                .OrderByDescending(pair => pair.Value.Count)
                .ThenBy(pair => pair.Key.Value)
                .Take(maxRowsPerSnapshot))
            {
                rows.Add(new LineageTrendRow(
                    snapshot.Tick,
                    snapshot.ElapsedSeconds,
                    rank,
                    pair.Key,
                    pair.Value.Count,
                    overallGenerations.Count,
                    pair.Value.Count / (float)overallGenerations.Count,
                    pair.Value.MinGeneration,
                    pair.Value.AverageGeneration,
                    pair.Value.MaxGeneration,
                    overallGenerations.MinGeneration,
                    overallGenerations.AverageGeneration,
                    overallGenerations.MaxGeneration));
                rank++;
            }
        }

        return rows;
    }

    private static void AddActiveCreature(
        IDictionary<EntityId, GenerationAccumulator> activeFounders,
        GenerationAccumulator overallGenerations,
        CreatureLineageRecord record,
        IReadOnlyDictionary<EntityId, EntityId> founderByCreature)
    {
        var founderId = founderByCreature[record.Id];
        if (!activeFounders.TryGetValue(founderId, out var accumulator))
        {
            accumulator = new GenerationAccumulator();
            activeFounders.Add(founderId, accumulator);
        }

        accumulator.Add(record.Generation);
        overallGenerations.Add(record.Generation);
    }

    private static void RemoveActiveCreature(
        IReadOnlyDictionary<EntityId, GenerationAccumulator> activeFounders,
        GenerationAccumulator overallGenerations,
        CreatureLineageRecord record,
        IReadOnlyDictionary<EntityId, EntityId> founderByCreature)
    {
        var founderId = founderByCreature[record.Id];
        if (activeFounders.TryGetValue(founderId, out var accumulator))
        {
            accumulator.Remove(record.Generation);
        }

        overallGenerations.Remove(record.Generation);
    }

    private sealed class GenerationAccumulator
    {
        private readonly SortedDictionary<int, int> _generationCounts = new();
        private long _generationSum;

        public int Count { get; private set; }

        public int MinGeneration => Count > 0 ? _generationCounts.First().Key : 0;

        public int MaxGeneration => Count > 0 ? _generationCounts.Last().Key : 0;

        public float AverageGeneration => Count > 0 ? _generationSum / (float)Count : 0f;

        public void Add(int generation)
        {
            _generationCounts.TryGetValue(generation, out var count);
            _generationCounts[generation] = count + 1;
            _generationSum += generation;
            Count++;
        }

        public void Remove(int generation)
        {
            if (!_generationCounts.TryGetValue(generation, out var count) || count == 0)
            {
                return;
            }

            if (count == 1)
            {
                _generationCounts.Remove(generation);
            }
            else
            {
                _generationCounts[generation] = count - 1;
            }

            _generationSum -= generation;
            Count--;
        }
    }
}

internal readonly record struct LineageTrendRow(
    long Tick,
    double ElapsedSeconds,
    int Rank,
    EntityId FounderId,
    int LivingCreatures,
    int TotalLiving,
    float LivingShare,
    int FounderMinGeneration,
    float FounderAverageGeneration,
    int FounderMaxGeneration,
    int OverallMinGeneration,
    float OverallAverageGeneration,
    int OverallMaxGeneration);

internal static class ProfileCsvWriter
{
    public static void Write(string path, SimulationProfile profile)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("system,calls,total_ms,avg_ms_per_call,share");

        var total = profile.TotalMilliseconds;
        foreach (var system in profile.Systems.OrderByDescending(system => system.TotalMilliseconds))
        {
            var share = total > 0.0
                ? system.TotalMilliseconds / total
                : 0.0;
            writer.WriteLine(string.Join(
                ',',
                EscapeCsv(system.SystemName),
                system.CallCount.ToString(CultureInfo.InvariantCulture),
                system.TotalMilliseconds.ToString("0.######", CultureInfo.InvariantCulture),
                system.AverageMillisecondsPerCall.ToString("0.######", CultureInfo.InvariantCulture),
                share.ToString("0.######", CultureInfo.InvariantCulture)));
        }
    }

    private static string EscapeCsv(string value)
    {
        return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}

internal static class SensingProfileCsvWriter
{
    public static void Write(string path, SimulationSensingProfile profile)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("phase,queries,candidates,plant_candidates,meat_candidates,visible,total_ms,avg_candidates_per_query,avg_ms_per_query,cells_visited,non_empty_cells,distance_rejects,self_rejects,nonviable_rejects,range_rejects,vision_rejects,body_radius_cache_misses,scheduled_refreshes,close_refreshes,forced_refreshes,skipped_updates");
        WriteRow(
            writer,
            "trait_cache",
            profile.Updates,
            profile.TraitCacheCreatures,
            0,
            0,
            0,
            profile.TraitCacheMilliseconds,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
        WriteRow(
            writer,
            "world_sense_refresh",
            profile.CreaturesSensed,
            profile.WorldSenseRefreshes,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            scheduledRefreshes: profile.WorldSenseScheduledRefreshes,
            closeRefreshes: profile.WorldSenseCloseRefreshes,
            forcedRefreshes: profile.WorldSenseForcedRefreshes,
            skippedUpdates: profile.WorldSenseSkippedUpdates);
        WriteRow(
            writer,
            "creature_setup",
            profile.CreaturesSensed,
            profile.CreaturesSensed,
            0,
            0,
            0,
            profile.CreatureSetupMilliseconds,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
        WriteRow(
            writer,
            "internal_state",
            profile.CreaturesSensed,
            profile.CreaturesSensed,
            0,
            0,
            0,
            profile.InternalStateMilliseconds,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
        WriteRow(
            writer,
            "resource_query",
            profile.ResourceQueries,
            profile.ResourceCandidates,
            profile.PlantResourceQueryCandidates,
            profile.MeatResourceQueryCandidates,
            profile.VisiblePlantCandidates + profile.VisibleMeatResourceCandidates,
            profile.ResourceQueryMilliseconds,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
        WriteRow(
            writer,
            "resource_scan",
            profile.ResourceQueries,
            profile.ResourceCandidates,
            profile.PlantCandidates,
            profile.MeatResourceCandidates,
            profile.VisiblePlantCandidates + profile.VisibleMeatResourceCandidates,
            profile.ResourceScanMilliseconds,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
        WriteRow(
            writer,
            "egg_query",
            profile.EggQueries,
            profile.EggCandidates,
            0,
            0,
            profile.VisibleEggCandidates,
            profile.EggQueryMilliseconds,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
        WriteRow(
            writer,
            "egg_scan",
            profile.EggQueries,
            profile.EggCandidates,
            0,
            0,
            profile.VisibleEggCandidates,
            profile.EggScanMilliseconds,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
        WriteRow(
            writer,
            "creature_query",
            profile.CreatureQueries,
            profile.CreatureCandidates,
            0,
            0,
            profile.VisibleCreatureCandidates,
            profile.CreatureQueryMilliseconds,
            profile.CreatureCellsVisited,
            profile.CreatureNonEmptyCellsVisited,
            profile.CreatureDistanceRejectedCandidates,
            profile.CreatureSelfRejectedCandidates,
            profile.CreatureNonviableRejectedCandidates,
            profile.CreatureRangeRejectedCandidates,
            profile.CreatureVisionRejectedCandidates,
            profile.CreatureBodyRadiusCacheMisses);
        WriteRow(
            writer,
            "creature_scan",
            profile.CreatureQueries,
            profile.CreatureCandidates,
            0,
            0,
            profile.VisibleCreatureCandidates,
            profile.CreatureScanMilliseconds,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
        WriteRow(
            writer,
            "terrain_sense",
            profile.CreaturesSensed,
            profile.CreaturesSensed,
            0,
            0,
            0,
            profile.TerrainSenseMilliseconds,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
        WriteRow(
            writer,
            "obstacle_sense",
            profile.ObstacleSenseSamples,
            0,
            0,
            0,
            0,
            profile.ObstacleSenseMilliseconds,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
        WriteRow(
            writer,
            "memory_sense",
            profile.CreaturesSensed,
            profile.CreaturesSensed,
            0,
            0,
            0,
            profile.MemorySenseMilliseconds,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
        WriteRow(
            writer,
            "sense_finalization",
            profile.CreaturesSensed,
            profile.CreaturesSensed,
            0,
            0,
            0,
            profile.SenseFinalizationMilliseconds,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
    }

    private static void WriteRow(
        TextWriter writer,
        string phase,
        long queries,
        long candidates,
        long plantCandidates,
        long meatCandidates,
        long visible,
        double totalMilliseconds,
        long cellsVisited,
        long nonEmptyCells,
        long distanceRejects,
        long selfRejects,
        long nonviableRejects,
        long rangeRejects,
        long visionRejects,
        long bodyRadiusCacheMisses,
        long scheduledRefreshes = 0,
        long closeRefreshes = 0,
        long forcedRefreshes = 0,
        long skippedUpdates = 0)
    {
        var averageCandidates = queries > 0
            ? candidates / (double)queries
            : 0.0;
        var averageMilliseconds = queries > 0
            ? totalMilliseconds / queries
            : 0.0;

        writer.WriteLine(string.Join(
            ',',
            phase,
            queries.ToString(CultureInfo.InvariantCulture),
            candidates.ToString(CultureInfo.InvariantCulture),
            plantCandidates.ToString(CultureInfo.InvariantCulture),
            meatCandidates.ToString(CultureInfo.InvariantCulture),
            visible.ToString(CultureInfo.InvariantCulture),
            totalMilliseconds.ToString("0.######", CultureInfo.InvariantCulture),
            averageCandidates.ToString("0.######", CultureInfo.InvariantCulture),
            averageMilliseconds.ToString("0.######", CultureInfo.InvariantCulture),
            cellsVisited.ToString(CultureInfo.InvariantCulture),
            nonEmptyCells.ToString(CultureInfo.InvariantCulture),
            distanceRejects.ToString(CultureInfo.InvariantCulture),
            selfRejects.ToString(CultureInfo.InvariantCulture),
            nonviableRejects.ToString(CultureInfo.InvariantCulture),
            rangeRejects.ToString(CultureInfo.InvariantCulture),
            visionRejects.ToString(CultureInfo.InvariantCulture),
            bodyRadiusCacheMisses.ToString(CultureInfo.InvariantCulture),
            scheduledRefreshes.ToString(CultureInfo.InvariantCulture),
            closeRefreshes.ToString(CultureInfo.InvariantCulture),
            forcedRefreshes.ToString(CultureInfo.InvariantCulture),
            skippedUpdates.ToString(CultureInfo.InvariantCulture)));
    }
}

internal static class BatchComparisonReportWriter
{
    public static void Write(string path, RunOptions options, IReadOnlyList<RunResult> results)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        var summaries = results.Select(CreateSummary).ToArray();

        WriteDocumentStart(writer, "Lineage Batch Comparison");

        writer.WriteLine("<header>");
        writer.WriteLine("<div class=\"page-width\">");
        writer.WriteLine("<p class=\"eyebrow\">Lineage Experiment</p>");
        writer.WriteLine("<h1>Batch Comparison</h1>");
        writer.WriteLine($"<p>{Html(results.Count)} scenario runs, {Html(options.Ticks)} ticks each.</p>");
        writer.WriteLine("</div>");
        writer.WriteLine("</header>");
        writer.WriteLine("<main class=\"page-width\">");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Overview</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Runs", results.Count.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Ticks per run", options.Ticks.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Batch output dir", Path.GetFullPath(options.BatchOutputDirectory));
        WriteMetric(writer, "Total wall time", $"{results.Sum(result => result.Elapsed.TotalSeconds):0.###} seconds");
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Run Verdicts</h2>");
        writer.WriteLine("<ul>");
        foreach (var summary in summaries)
        {
            writer.WriteLine($"<li><strong>{Html(summary.ScenarioName)}:</strong> {Html(summary.Verdict)}</li>");
        }

        writer.WriteLine("</ul>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Outcome Comparison</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Scenario</th><th>Seed</th><th>Final Pop</th><th>Eggs</th><th>Pop Change</th><th>Births</th><th>Eggs Laid</th><th>Hatched</th><th>Egg Deaths</th><th>Egg Pred</th><th>Deaths</th><th>Starved</th><th>Injury</th><th>Max Gen</th><th>Resource Final</th><th>Dominant Founder</th><th>Report</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in summaries)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.ScenarioName)}</td>" +
                $"<td>{Html(summary.Seed)}</td>" +
                $"<td>{Html(summary.FinalPopulation)}</td>" +
                $"<td>{Html(summary.FinalEggs)}</td>" +
                $"<td>{Html($"{summary.PopulationChangePercent:0.0}%")}</td>" +
                $"<td>{Html(summary.Births)}</td>" +
                $"<td>{Html(summary.EggsLaid)}</td>" +
                $"<td>{Html(summary.EggsHatched)}</td>" +
                $"<td>{Html(summary.EggDeaths)}</td>" +
                $"<td>{Html(summary.EggPredationDeaths)}</td>" +
                $"<td>{Html(summary.Deaths)}</td>" +
                $"<td>{Html(summary.StarvationDeaths)}</td>" +
                $"<td>{Html(summary.InjuryDeaths)}</td>" +
                $"<td>{Html(summary.MaxGeneration)}</td>" +
                $"<td>{Html($"{summary.FinalResourceRatio * 100f:0.0}%")}</td>" +
                $"<td>{Html(summary.DominantFounderText)}</td>" +
                $"<td>{FormatReportLink(path, summary.ReportPath)}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Final Foraging Comparison</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Scenario</th><th>Seeing Food</th><th>Touching Food</th><th>Eating</th><th>Visible Density</th><th>Calories Eaten</th><th>Time Since Meal</th><th>Meal Distance</th><th>Calories/Distance</th><th>Calories/Food Vision</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in summaries)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.ScenarioName)}</td>" +
                $"<td>{Html(FormatPercent(summary.FoodSeenShare))}</td>" +
                $"<td>{Html(FormatPercent(summary.FoodContactShare))}</td>" +
                $"<td>{Html(FormatPercent(summary.EatingShare))}</td>" +
                $"<td>{Html(summary.VisibleFoodDensity.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html($"{summary.CaloriesEatenPerSecond:0.###} kcal/s")}</td>" +
                $"<td>{Html($"{summary.AverageSecondsSinceLastMeal:0.###} s")}</td>" +
                $"<td>{Html($"{summary.AverageDistanceSinceLastMeal:0.###} u")}</td>" +
                $"<td>{Html($"{summary.CaloriesEatenPerDistance:0.###} kcal/u")}</td>" +
                $"<td>{Html($"{summary.CaloriesEatenPerFoodVisionEvent:0.###} kcal/event")}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Final Predation Comparison</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Scenario</th><th>Attacking</th><th>Attack Damage</th><th>Fresh Kill</th><th>Carcass</th><th>Egg</th><th>Meat Energy</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in summaries)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.ScenarioName)}</td>" +
                $"<td>{Html(FormatPercent(summary.AttackShare))}</td>" +
                $"<td>{Html($"{summary.AttackDamagePerSecond:0.###} health/s")}</td>" +
                $"<td>{Html($"{summary.FreshKillCaloriesEatenPerSecond:0.###} kcal/s")}</td>" +
                $"<td>{Html($"{summary.CarcassCaloriesEatenPerSecond:0.###} kcal/s")}</td>" +
                $"<td>{Html($"{summary.EggCaloriesEatenPerSecond:0.###} kcal/s")}</td>" +
                $"<td>{Html($"{summary.MeatDigestedEnergyPerSecond:0.###} energy/s")}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Final Trait Comparison</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Scenario</th><th>Living</th><th>Body Radius</th><th>Max Speed</th><th>Vision Range</th><th>Vision Angle</th><th>Repro Threshold</th><th>Offspring Investment</th><th>Egg Production</th><th>Egg Incubation</th><th>Maturity</th><th>Gut Capacity</th><th>Digestion Rate</th><th>Bite Strength</th><th>Damage Resistance</th><th>Mutation Strength</th><th>Trait Mut Rate</th><th>Brain Mut Rate</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in summaries)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.ScenarioName)}</td>" +
                $"<td>{Html(summary.FinalPopulation)}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.BodyRadius))}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.MaxSpeed))}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.SenseRadius))}</td>" +
                $"<td>{Html(FormatDegreesSummary(summary.Traits.VisionAngleRadians))}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.ReproductionThreshold))}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.OffspringInvestment))}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.EggProductionEnergyPerSecond))}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.EggIncubationSeconds))}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.MaturityAgeSeconds))}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.GutCapacityCalories))}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.DigestionCaloriesPerSecond))}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.BiteStrength))}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.DamageResistance))}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.MutationStrength))}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.TraitMutationRate))}</td>" +
                $"<td>{Html(FormatSummary(summary.Traits.BrainMutationRate))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Output Files</h2>");
        writer.WriteLine("<ul class=\"file-list\">");
        foreach (var summary in summaries)
        {
            if (summary.ReportPath is not null)
            {
                writer.WriteLine($"<li>{Html(summary.ScenarioName)} report: <code>{Html(Path.GetFullPath(summary.ReportPath))}</code></li>");
            }
        }

        writer.WriteLine("</ul>");
        writer.WriteLine("</section>");

        writer.WriteLine("</main>");
        WriteDocumentEnd(writer);
    }

    private static BatchRunSummary CreateSummary(RunResult result)
    {
        var state = result.Simulation.State;
        var snapshots = state.Stats.Snapshots;
        var finalSnapshot = snapshots.Count > 0 ? snapshots[^1] : default;
        var initialPopulation = Math.Max(1, result.Scenario.InitialCreatureCount);
        var resourceCapacity = state.Resources.Sum(resource => resource.MaxCalories);
        var finalResourceCalories = state.Resources.Sum(resource => resource.Calories);
        var dominantFounder = FounderSummaryCsvWriter.Summarize(state.LineageRecords)
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenByDescending(summary => summary.TotalCreatures)
            .ThenBy(summary => summary.FounderId.Value)
            .FirstOrDefault();
        var dominantFounderShare = state.Creatures.Count > 0
            ? dominantFounder.LivingCreatures / (float)state.Creatures.Count
            : 0f;
        var traits = state.Creatures.Count > 0
            ? TraitAccumulator.FromLivingCreatures(state)
            : default;
        var finalResourceRatio = resourceCapacity > 0f
            ? finalResourceCalories / resourceCapacity
            : 0f;
        var populationChangePercent = (state.Creatures.Count - initialPopulation) / (float)initialPopulation * 100f;

        return new BatchRunSummary(
            result.Scenario.Name,
            result.Scenario.Seed,
            state.Creatures.Count,
            state.Eggs.Count,
            populationChangePercent,
            state.Stats.CreatureBirthCount,
            state.Stats.EggLaidCount,
            state.Stats.EggHatchedCount,
            state.Stats.EggDeathCount,
            state.Stats.EggPredationDeathCount,
            state.Stats.CreatureDeathCount,
            state.Stats.StarvationDeathCount,
            state.Stats.InjuryDeathCount,
            finalSnapshot.MaxGeneration,
            finalResourceRatio,
            Share(finalSnapshot.FoodDetectedCreatureCount, finalSnapshot.CreatureCount),
            Share(finalSnapshot.FoodContactCreatureCount, finalSnapshot.CreatureCount),
            Share(finalSnapshot.EatingCreatureCount, finalSnapshot.CreatureCount),
            finalSnapshot.AverageVisibleFoodDensity,
            finalSnapshot.TotalCaloriesEatenPerSecond,
            finalSnapshot.AverageSecondsSinceLastMeal,
            finalSnapshot.AverageDistanceSinceLastMeal,
            finalSnapshot.CaloriesEatenPerDistance,
            finalSnapshot.CaloriesEatenPerFoodVisionEvent,
            Share(finalSnapshot.AttackingCreatureCount, finalSnapshot.CreatureCount),
            finalSnapshot.TotalAttackDamagePerSecond,
            finalSnapshot.TotalCarcassCaloriesEatenPerSecond,
            finalSnapshot.TotalEggCaloriesEatenPerSecond,
            finalSnapshot.TotalLivePreyCaloriesEatenPerSecond,
            finalSnapshot.TotalMeatDigestedEnergyPerSecond,
            dominantFounder.FounderId,
            dominantFounderShare,
            traits,
            result.OutputPaths.ReportPath,
            BuildVerdict(result.Scenario, state, finalSnapshot, finalResourceRatio, dominantFounderShare));
    }

    private static string BuildVerdict(
        SimulationScenario scenario,
        WorldState state,
        SimulationStatsSnapshot finalSnapshot,
        float finalResourceRatio,
        float dominantFounderShare)
    {
        if (state.Creatures.Count == 0)
        {
            return "population collapsed";
        }

        if (state.Stats.CreatureDeathCount == 0)
        {
            return finalSnapshot.MaxGeneration <= 1
                ? "very low pressure; little turnover yet"
                : "growth with weak death pressure";
        }

        if (scenario.InitialCreatureCount > 0 && state.Creatures.Count < scenario.InitialCreatureCount * 0.25f)
        {
            return "high pressure; population barely persisted";
        }

        if (finalResourceRatio > 0.85f && state.Stats.StarvationDeathCount < scenario.InitialCreatureCount)
        {
            return "food remained abundant; selection may be gentle";
        }

        if (dominantFounderShare > 0.5f && finalSnapshot.MaxGeneration >= 3)
        {
            return "clear founder-lineage dominance emerged";
        }

        return "viable run with measurable turnover";
    }

    private static float Share(int count, int total)
    {
        return total > 0 ? count / (float)total : 0f;
    }

    private static string FormatPercent(float value)
    {
        return $"{value * 100f:0.0}%";
    }

    private static string FormatReportLink(string batchPath, string? reportPath)
    {
        if (reportPath is null)
        {
            return Html("-");
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(batchPath)) ?? Directory.GetCurrentDirectory();
        var relative = Path.GetRelativePath(directory, Path.GetFullPath(reportPath))
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/')
            .Replace("\\", "/", StringComparison.Ordinal);
        return $"<a href=\"{Html(relative)}\">report</a>";
    }

    private static string FormatSummary(FloatSummary summary)
    {
        return summary == default
            ? "-"
            : $"{summary.Average:0.###} ({summary.Min:0.###}-{summary.Max:0.###})";
    }

    private static string FormatDegreesSummary(FloatSummary summary)
    {
        return summary == default
            ? "-"
            : FormatSummary(ToDegrees(summary));
    }

    private static float ToDegrees(float radians)
    {
        return radians * 180f / MathF.PI;
    }

    private static FloatSummary ToDegrees(FloatSummary summary)
    {
        return new FloatSummary(
            ToDegrees(summary.Average),
            ToDegrees(summary.Min),
            ToDegrees(summary.Max));
    }

    private static void WriteDocumentStart(StreamWriter writer, string title)
    {
        writer.WriteLine("<!doctype html>");
        writer.WriteLine("<html lang=\"en\">");
        writer.WriteLine("<head>");
        writer.WriteLine("<meta charset=\"utf-8\">");
        writer.WriteLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        writer.WriteLine($"<title>{Html(title)}</title>");
        writer.WriteLine(
            """
            <style>
            :root {
              color-scheme: light;
              --bg: #f6f7f2;
              --text: #172015;
              --muted: #62705e;
              --panel: #ffffff;
              --line: #dfe5d9;
              --accent: #2f7d45;
            }
            body {
              margin: 0;
              background: var(--bg);
              color: var(--text);
              font-family: "Segoe UI", system-ui, sans-serif;
              line-height: 1.45;
            }
            header {
              padding: 34px 0 24px;
              background: #162015;
              color: #f4f7ef;
            }
            .page-width {
              width: min(1180px, calc(100% - 32px));
              margin: 0 auto;
            }
            .eyebrow {
              margin: 0 0 6px;
              color: #a9c9aa;
              font-size: 0.78rem;
              letter-spacing: 0;
              text-transform: uppercase;
            }
            h1, h2 { margin: 0; }
            h1 { font-size: 2rem; }
            h2 { margin-bottom: 14px; font-size: 1.15rem; }
            main { padding: 22px 0 40px; }
            section {
              margin-top: 16px;
              padding: 18px;
              background: var(--panel);
              border: 1px solid var(--line);
              border-radius: 8px;
            }
            .metric-grid {
              display: grid;
              grid-template-columns: repeat(auto-fit, minmax(190px, 1fr));
              gap: 10px;
            }
            .metric {
              padding: 10px 12px;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #fbfcf8;
            }
            .metric-label {
              color: var(--muted);
              font-size: 0.75rem;
              text-transform: uppercase;
            }
            .metric-value {
              display: block;
              margin-top: 4px;
              overflow-wrap: anywhere;
              font-weight: 650;
            }
            .table-wrap { overflow-x: auto; }
            table {
              width: 100%;
              border-collapse: collapse;
              font-size: 0.92rem;
            }
            th, td {
              padding: 8px 10px;
              border-bottom: 1px solid var(--line);
              text-align: right;
              white-space: nowrap;
            }
            th:first-child, td:first-child { text-align: left; }
            th {
              color: var(--muted);
              font-size: 0.76rem;
              text-transform: uppercase;
            }
            a { color: var(--accent); font-weight: 650; }
            ul { margin: 0; padding-left: 20px; }
            li + li { margin-top: 6px; }
            code {
              padding: 2px 5px;
              border-radius: 4px;
              background: #eef2e9;
              color: #253525;
            }
            .file-list code { overflow-wrap: anywhere; }
            @media (max-width: 640px) {
              h1 { font-size: 1.7rem; }
              section { padding: 14px; }
              th, td { padding: 7px 8px; }
            }
            </style>
            """);
        writer.WriteLine("</head>");
        writer.WriteLine("<body>");
    }

    private static void WriteDocumentEnd(StreamWriter writer)
    {
        writer.WriteLine("</body>");
        writer.WriteLine("</html>");
    }

    private static void WriteMetric(StreamWriter writer, string label, string value)
    {
        writer.WriteLine("<div class=\"metric\">");
        writer.WriteLine($"<span class=\"metric-label\">{Html(label)}</span>");
        writer.WriteLine($"<span class=\"metric-value\">{Html(value)}</span>");
        writer.WriteLine("</div>");
    }

    private static string Html(object? value)
    {
        return WebUtility.HtmlEncode(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
    }
}

internal readonly record struct BatchRunSummary(
    string ScenarioName,
    ulong Seed,
    int FinalPopulation,
    int FinalEggs,
    float PopulationChangePercent,
    int Births,
    int EggsLaid,
    int EggsHatched,
    int EggDeaths,
    int EggPredationDeaths,
    int Deaths,
    int StarvationDeaths,
    int InjuryDeaths,
    int MaxGeneration,
    float FinalResourceRatio,
    float FoodSeenShare,
    float FoodContactShare,
    float EatingShare,
    float VisibleFoodDensity,
    float CaloriesEatenPerSecond,
    float AverageSecondsSinceLastMeal,
    float AverageDistanceSinceLastMeal,
    float CaloriesEatenPerDistance,
    float CaloriesEatenPerFoodVisionEvent,
    float AttackShare,
    float AttackDamagePerSecond,
    float CarcassCaloriesEatenPerSecond,
    float EggCaloriesEatenPerSecond,
    float FreshKillCaloriesEatenPerSecond,
    float MeatDigestedEnergyPerSecond,
    EntityId DominantFounderId,
    float DominantFounderShare,
    TraitAccumulator Traits,
    string? ReportPath,
    string Verdict)
{
    public string DominantFounderText => DominantFounderId == default
        ? "-"
        : $"#{DominantFounderId.Value} ({DominantFounderShare * 100f:0.0}%)";
}

internal static class RunReportWriter
{
    private const int ReportTrendRowCount = 8;

    public static void Write(
        string path,
        RunOptions options,
        SimulationScenario scenario,
        Simulation simulation,
        TimeSpan elapsed,
        OutputPaths outputPaths,
        IReadOnlyList<CheckpointArtifact> checkpoints)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        var state = simulation.State;
        var snapshots = state.Stats.Snapshots;
        var finalSnapshot = snapshots.Count > 0 ? snapshots[^1] : default;
        var populationTrend = Trend.From(snapshots, snapshot => snapshot.CreatureCount, state.Creatures.Count);
        var eggTrend = Trend.From(snapshots, snapshot => snapshot.EggCount, state.Eggs.Count);
        var resourceTrend = Trend.From(
            snapshots,
            snapshot => snapshot.TotalResourceCalories,
            state.Resources.Sum(resource => resource.Calories));
        var foodSeenTrend = Trend.From(
            snapshots,
            snapshot => Share(snapshot.FoodDetectedCreatureCount, snapshot.CreatureCount) * 100f,
            Share(finalSnapshot.FoodDetectedCreatureCount, finalSnapshot.CreatureCount) * 100f);
        var visibleFoodDensityTrend = Trend.From(
            snapshots,
            snapshot => snapshot.AverageVisibleFoodDensity,
            finalSnapshot.AverageVisibleFoodDensity);
        var eatingContactTrend = Trend.From(
            snapshots,
            snapshot => Share(snapshot.FoodContactCreatureCount, snapshot.CreatureCount) * 100f,
            Share(finalSnapshot.FoodContactCreatureCount, finalSnapshot.CreatureCount) * 100f);
        var caloriesEatenTrend = Trend.From(
            snapshots,
            snapshot => snapshot.TotalCaloriesEatenPerSecond,
            finalSnapshot.TotalCaloriesEatenPerSecond);
        var caloriesDigestedTrend = Trend.From(
            snapshots,
            snapshot => snapshot.TotalCaloriesDigestedPerSecond,
            finalSnapshot.TotalCaloriesDigestedPerSecond);
        var gutFillTrend = Trend.From(
            snapshots,
            snapshot => snapshot.AverageGutFillRatio * 100f,
            finalSnapshot.AverageGutFillRatio * 100f);
        var attackingTrend = Trend.From(
            snapshots,
            snapshot => Share(snapshot.AttackingCreatureCount, snapshot.CreatureCount) * 100f,
            Share(finalSnapshot.AttackingCreatureCount, finalSnapshot.CreatureCount) * 100f);
        var attackDamageTrend = Trend.From(
            snapshots,
            snapshot => snapshot.TotalAttackDamagePerSecond,
            finalSnapshot.TotalAttackDamagePerSecond);
        var mealGapTrend = Trend.From(
            snapshots,
            snapshot => snapshot.AverageSecondsSinceLastMeal,
            finalSnapshot.AverageSecondsSinceLastMeal);
        var mealDistanceTrend = Trend.From(
            snapshots,
            snapshot => snapshot.AverageDistanceSinceLastMeal,
            finalSnapshot.AverageDistanceSinceLastMeal);
        var distanceTraveledTrend = Trend.From(
            snapshots,
            snapshot => snapshot.TotalDistanceTraveledPerSecond,
            finalSnapshot.TotalDistanceTraveledPerSecond);
        var caloriesPerDistanceTrend = Trend.From(
            snapshots,
            snapshot => snapshot.CaloriesEatenPerDistance,
            finalSnapshot.CaloriesEatenPerDistance);
        var caloriesPerFoodVisionTrend = Trend.From(
            snapshots,
            snapshot => snapshot.CaloriesEatenPerFoodVisionEvent,
            finalSnapshot.CaloriesEatenPerFoodVisionEvent);
        var behaviorSummary = BehaviorAssay.Analyze(state);
        var lineageBehaviorSummaries = BehaviorAssay.AnalyzeTopFounderLineages(state, 10);
        var brainInputDiagnostics = BrainInputDiagnostics.Analyze(state);
        var lineageBrainInputDiagnostics = BrainInputDiagnostics.AnalyzeTopFounderLineages(state, 10);
        var founderSummaries = FounderSummaryCsvWriter.Summarize(state.LineageRecords)
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenByDescending(summary => summary.TotalCreatures)
            .ThenBy(summary => summary.FounderId.Value)
            .ToArray();
        var founderExplorationSummaries = SummarizeFounderExploration(state, 10);
        var generationSummaries = GenerationSummaryCsvWriter.Summarize(state.LineageRecords);
        var dominantLineageRows = LineageTrendCsvWriter.Summarize(snapshots, state.LineageRecords, maxRowsPerSnapshot: 1);
        var traitSummary = state.Creatures.Count > 0
            ? TraitAccumulator.FromLivingCreatures(state)
            : default;
        var biomeSummaries = state.Biomes.SummarizeResources(state.Resources);
        var seasonPressure = SeasonPressureAnalysis.Analyze(scenario, snapshots);

        WriteDocumentStart(writer, $"Lineage Run Report - {scenario.Name}");

        writer.WriteLine("<header>");
        writer.WriteLine("<div class=\"page-width\">");
        writer.WriteLine("<p class=\"eyebrow\">Lineage Experiment</p>");
        writer.WriteLine("<h1>Run Report</h1>");
        writer.WriteLine($"<p>{Html(scenario.Name)} with the {Html(scenario.PipelineKind)} pipeline, seed {Html(scenario.Seed)}.</p>");
        writer.WriteLine("</div>");
        writer.WriteLine("</header>");
        writer.WriteLine("<main class=\"page-width\">");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Run</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Scenario", scenario.Name);
        WriteMetric(writer, "Pipeline", scenario.PipelineKind.ToString());
        WriteMetric(writer, "Initial brain", FormatInitialBrainKind(scenario.InitialBrainKind));
        WriteMetric(writer, "Brain hidden nodes", scenario.BrainHiddenNodeCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Seed", scenario.Seed.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Ticks requested", options.Ticks.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Final tick", state.Tick.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Simulated seconds", state.ElapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Wall time", $"{elapsed.TotalSeconds:0.###} seconds");
        WriteMetric(writer, "Snapshot interval", $"{scenario.StatsSnapshotIntervalTicks} ticks");
        WriteMetric(
            writer,
            "Checkpoint interval",
            options.CheckpointIntervalTicks is null
                ? "Off"
                : $"{options.CheckpointIntervalTicks.Value} ticks");
        WriteMetric(writer, "Checkpoints saved", checkpoints.Count.ToString(CultureInfo.InvariantCulture));
        if (options.ScenarioPath is not null)
        {
            WriteMetric(writer, "Scenario file", Path.GetFullPath(options.ScenarioPath));
        }

        WriteMetric(writer, "Scenario species roster", FormatScenarioSpeciesSeeds(scenario));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        var startingGenome = CreatureGenome.Baseline with
        {
            DietaryAdaptation = scenario.DietaryAdaptation,
            CarrionAdaptation = scenario.CarrionAdaptation
        };

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Pressure Settings</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Initial creatures", scenario.InitialCreatureCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Initial creature spawn", scenario.InitialCreatureSpawnRegion.ToString());
        WriteMetric(writer, "World sense interval", $"{scenario.WorldSenseIntervalTicks} ticks");
        WriteMetric(writer, "Close sense refresh", FormatPercent(scenario.CloseSenseRefreshProximity));
        WriteMetric(writer, "Initial resource density", $"{scenario.InitialResourcesPerMillionArea:0.###} per 1M area");
        WriteMetric(writer, "Initial resource patches", scenario.CalculateInitialResourceCount().ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Biomes", scenario.EnableBiomes ? "Enabled" : "Disabled");
        WriteMetric(writer, "Biome map", scenario.BiomeMapKind.ToString());
        WriteMetric(writer, "Biome cell size", scenario.BiomeCellSize.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Obstacles", scenario.EnableObstacles ? "Enabled" : "Disabled");
        WriteMetric(writer, "Obstacle map", scenario.ObstacleMapKind.ToString());
        WriteMetric(writer, "Obstacle cell size", scenario.ObstacleCellSize.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Resource void border", $"{scenario.ResourceVoidBorderWidth:0.###} world units");
        WriteMetric(writer, "Resource calories", FormatRange(scenario.ResourceCaloriesMin, scenario.ResourceCaloriesMax));
        WriteMetric(writer, "Resource regrowth", $"{FormatRange(scenario.ResourceRegrowthMin, scenario.ResourceRegrowthMax)} kcal/s");
        WriteMetric(writer, "Depleted resources relocate", scenario.RelocateDepletedResources ? "Yes" : "No");
        WriteMetric(writer, "Plant respawn delay", $"{FormatRange(scenario.PlantRespawnDelaySecondsMin, scenario.PlantRespawnDelaySecondsMax)} seconds");
        WriteMetric(writer, "Seasons", scenario.EnableSeasons ? "Enabled" : "Disabled");
        WriteMetric(writer, "Season length", $"{scenario.SeasonLengthSeconds:0.###} seconds");
        WriteMetric(writer, "Season fertility swing", FormatPercent(scenario.SeasonFertilityAmplitude));
        WriteMetric(writer, "Season phase offset", $"{scenario.SeasonPhaseOffsetSeconds:0.###} seconds");
        WriteMetric(writer, "Season phase mode", scenario.SeasonPhaseMode.ToString());
        WriteMetric(writer, "Biome season response", FormatBiomePressureProfile(scenario.CreateBiomeSeasonalAmplitudeProfile()));
        WriteMetric(writer, "Resource clustering", FormatPercent(scenario.ResourceClusterStrength));
        WriteMetric(writer, "Resource cluster radius", $"{scenario.ResourceClusterRadius:0.###} world units");
        WriteMetric(writer, "Plant local dispersal", FormatPercent(scenario.PlantLocalDispersalChance));
        WriteMetric(writer, "Plant local dispersal radius", $"{scenario.PlantLocalDispersalRadius:0.###} world units");
        WriteMetric(writer, "Biome movement costs", FormatBiomePressureProfile(scenario.CreateBiomeMovementCostProfile()));
        WriteMetric(writer, "Biome basal costs", FormatBiomePressureProfile(scenario.CreateBiomeBasalCostProfile()));
        WriteMetric(writer, "Biome speed", FormatBiomePressureProfile(scenario.CreateBiomeSpeedProfile()));
        WriteMetric(writer, "Basal upkeep", $"{scenario.BasalEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Body radius upkeep", $"{scenario.BodyRadiusEnergyCostPerSecond:0.###} energy/radius/s");
        WriteMetric(writer, "Max speed upkeep", $"{scenario.MaxSpeedEnergyCostPerSecond:0.######} energy/speed/s");
        WriteMetric(writer, "Turn rate upkeep", $"{scenario.TurnRateEnergyCostPerSecond:0.######} energy/rad/s/s");
        WriteMetric(writer, "Sense radius upkeep", $"{scenario.SenseRadiusEnergyCostPerSecond:0.######} energy/radius/s");
        WriteMetric(writer, "Vision angle", $"{ToDegrees(scenario.VisionAngleRadians):0.###} degrees");
        WriteMetric(writer, "Vision angle upkeep", $"{scenario.VisionAngleEnergyCostPerSecond:0.######} energy/radian/s");
        WriteMetric(writer, "Eat rate upkeep", $"{scenario.EatRateEnergyCostPerSecond:0.######} energy/rate/s");
        WriteMetric(writer, "Gut capacity upkeep", $"{scenario.GutCapacityEnergyCostPerSecond:0.######} energy/capacity/s");
        WriteMetric(writer, "Digestion rate upkeep", $"{scenario.DigestionRateEnergyCostPerSecond:0.######} energy/rate/s");
        WriteMetric(writer, "Bite strength upkeep", $"{scenario.BiteStrengthEnergyCostPerSecond:0.######} energy/strength/s");
        WriteMetric(writer, "Damage resistance upkeep", $"{scenario.DamageResistanceEnergyCostPerSecond:0.######} energy/resistance/s");
        WriteMetric(writer, "Active memory upkeep", $"{scenario.MemoryEnergyCostPerSecond:0.######} energy/full-memory/s");
        WriteMetric(writer, "Memory decay", $"{scenario.MemoryDecayPerSecond:0.######}/s");
        WriteMetric(writer, "Memory write rate", $"{scenario.MemoryWriteRatePerSecond:0.######}/s");
        WriteMetric(writer, "Egg upkeep", $"{scenario.EggEnergyCostPerSecond:0.######} energy/egg/s");
        WriteMetric(writer, "Egg exposure damage", $"{scenario.EggEnvironmentalDamagePerSecond:0.######} health/s");
        WriteMetric(writer, "Movement upkeep", $"{scenario.MovementEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Movement speed cost exponent", scenario.MovementSpeedCostExponent.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Eat rate", $"{scenario.EatCaloriesPerSecond:0.###} kcal/s");
        WriteMetric(writer, "Gut capacity", $"{scenario.GutCapacityCalories:0.###} kcal");
        WriteMetric(writer, "Digestion rate", $"{scenario.DigestionCaloriesPerSecond:0.###} kcal/s");
        WriteMetric(writer, "Reproduction threshold", scenario.ReproductionEnergyThreshold.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Offspring investment", scenario.OffspringEnergyInvestment.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Egg production", $"{scenario.EggProductionEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Egg incubation", $"{scenario.EggIncubationSeconds:0.###} seconds");
        WriteMetric(writer, "Maturity age", $"{scenario.MaturityAgeSeconds:0.###} seconds");
        WriteMetric(writer, "Require reproduction intent", scenario.RequireReproductionIntent ? "Yes" : "No");
        WriteMetric(writer, "Prime fertility age", $"{scenario.ReproductivePrimeAgeSeconds:0.###} seconds");
        WriteMetric(writer, "Senescence age", $"{scenario.ReproductiveSenescenceAgeSeconds:0.###} seconds");
        WriteMetric(writer, "Senescent fertility", FormatPercent(scenario.SenescentFertilityMultiplier));
        WriteMetric(writer, "Crowding fertility penalty", FormatPercent(scenario.CrowdingFertilityPenalty));
        WriteMetric(writer, "Starting diet", $"{scenario.DietaryAdaptation:0.###} meat bias");
        WriteMetric(writer, "Starting carrion", $"{scenario.CarrionAdaptation:0.###} stale-meat bias");
        WriteMetric(writer, "Starting bite strength", scenario.BiteStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Starting damage resistance", scenario.DamageResistance.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Starting plant digestion", FormatPercent(CreatureDigestion.PlantEfficiency(startingGenome)));
        WriteMetric(writer, "Starting meat digestion", FormatPercent(CreatureDigestion.MeatEfficiency(startingGenome)));
        WriteMetric(writer, "Starting fresh meat digestion", FormatPercent(CreatureDigestion.FreshMeatEnergyEfficiency(startingGenome)));
        WriteMetric(writer, "Starting stale meat digestion", FormatPercent(CreatureDigestion.StaleMeatEnergyEfficiency(startingGenome)));
        WriteMetric(writer, "Death meat body calories", $"{scenario.DeathMeatCaloriesPerBodyRadius:0.###} kcal/radius");
        WriteMetric(writer, "Death meat energy fraction", FormatPercent(scenario.DeathMeatEnergyFraction));
        WriteMetric(writer, "Meat decay", $"{scenario.MeatDecayCaloriesPerSecond:0.###} kcal/s");
        WriteMetric(writer, "Rotten meat damage", $"{scenario.RottenMeatDamagePerRawKcal:0.####} health/raw kcal");
        WriteMetric(writer, "Meat scent range", $"{scenario.MeatScentRangeMultiplier:0.###}x vision");
        WriteMetric(writer, "Meat scent full strength", $"{scenario.MeatScentCaloriesForFullStrength:0.###} kcal");
        WriteMetric(writer, "Meat scent saturation", scenario.MeatScentDensitySaturation.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Bite damage", $"{scenario.BiteDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Bite energy cost", $"{scenario.BiteEnergyCostPerSecond:0.###} energy/s");
        WriteMetric(writer, "Bite reach", $"{scenario.BiteRangePadding:0.###} world units");
        WriteMetric(writer, "Mutation strength", scenario.MutationStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Trait mutation rate", FormatPercent(scenario.TraitMutationRate));
        WriteMetric(writer, "Brain mutation rate", FormatPercent(scenario.BrainMutationRate));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Outcome</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Final population", state.Creatures.Count.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Eggs", state.Eggs.Count.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plants", finalSnapshot.PlantResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Meat", finalSnapshot.MeatResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plant calories", finalSnapshot.TotalPlantCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Meat calories", finalSnapshot.TotalMeatCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Dormant plants", finalSnapshot.DormantPlantResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg dormancy remaining", $"{finalSnapshot.AverageDormantPlantSecondsRemaining:0.###} seconds");
        WriteMetric(writer, "Plant patch occupied", FormatPercent(finalSnapshot.PlantPatchOccupiedCellShare));
        WriteMetric(writer, "Plant top-decile calories", FormatPercent(finalSnapshot.PlantPatchTopDecileCaloriesShare));
        WriteMetric(writer, "Plant patchiness", finalSnapshot.PlantPatchiness.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plant depletions", state.Stats.PlantDepletionCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plant relocations", FormatPlantRelocations(state.Stats));
        WriteMetric(writer, "Avg dormancy scheduled", $"{state.Stats.AveragePlantDormancyScheduledSeconds:0.###} seconds");
        WriteMetric(writer, "Avg dormancy completed", $"{state.Stats.AveragePlantDormancyCompletedSeconds:0.###} seconds");
        WriteMetric(writer, "Avg meat freshness", FormatPercent(finalSnapshot.AverageMeatFreshness));
        WriteMetric(writer, "Births", state.Stats.CreatureBirthCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Eggs laid", state.Stats.EggLaidCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Reproduction attempts", state.Stats.ReproductionAttemptCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Attempt success", FormatPercent(Share(state.Stats.EggLaidCount, state.Stats.ReproductionAttemptCount)));
        WriteMetric(writer, "Eggs hatched", state.Stats.EggHatchedCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Egg deaths", state.Stats.EggDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Egg predation deaths", state.Stats.EggPredationDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Egg survival", FormatPercent(Share(state.Stats.EggHatchedCount, state.Stats.EggLaidCount)));
        WriteMetric(writer, "Offspring alive", FormatPercent(Share(state.Creatures.Count(creature => creature.Generation > 0), state.Stats.EggHatchedCount)));
        WriteMetric(writer, "Egg health", $"{finalSnapshot.AverageEggHealthRatio * 100f:0.0}%");
        WriteMetric(writer, "Birth investment", $"{finalSnapshot.AverageBirthInvestmentRatio:0.###}x");
        WriteMetric(writer, "Reproduction intent", FormatPercent(Share(finalSnapshot.ReproductionIntentCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Ready to lay", FormatPercent(Share(finalSnapshot.ReproductionReadyCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Egg reserve", FormatPercent(finalSnapshot.AverageEggReserveRatio));
        WriteMetric(writer, "Energy surplus", FormatPercent(finalSnapshot.AverageEnergySurplusRatio));
        WriteMetric(writer, "Food success", FormatPercent(finalSnapshot.AverageRecentFoodSuccess));
        WriteMetric(writer, "Active memory", $"{FormatPercent(Share(finalSnapshot.ActiveMemoryCreatureCount, finalSnapshot.CreatureCount))} ({finalSnapshot.ActiveMemoryCreatureCount})");
        WriteMetric(writer, "Memory strength", finalSnapshot.AverageMemoryStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Deaths", state.Stats.CreatureDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Starvation deaths", state.Stats.StarvationDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Injury deaths", state.Stats.InjuryDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Rotten meat deaths", state.Stats.RottenMeatDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average lifespan", $"{finalSnapshot.AverageLifespanSeconds:0.###} seconds");
        WriteMetric(writer, "Median lifespan", $"{finalSnapshot.MedianLifespanSeconds:0.###} seconds");
        WriteMetric(writer, "Max generation", finalSnapshot.MaxGeneration.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg hidden input weight", finalSnapshot.AverageBrainHiddenInputWeightMagnitude.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg hidden output weight", finalSnapshot.AverageBrainHiddenOutputWeightMagnitude.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Active hidden outputs", FormatPercent(finalSnapshot.ActiveBrainHiddenOutputShare));
        WriteMetric(writer, "Avg movement biome cost", $"{finalSnapshot.AverageBiomeMovementCostMultiplier:0.###}x");
        WriteMetric(writer, "Avg basal biome cost", $"{finalSnapshot.AverageBiomeBasalCostMultiplier:0.###}x");
        WriteMetric(writer, "Avg biome speed", $"{finalSnapshot.AverageBiomeSpeedMultiplier:0.###}x");
        WriteMetric(writer, "Season phase", FormatPercent(finalSnapshot.SeasonPhase));
        WriteMetric(writer, "Season fertility", $"{finalSnapshot.SeasonFertilityMultiplier:0.###}x");
        WriteMetric(writer, "Region season fertility", FormatRegionValues(
            finalSnapshot.LeftRegionSeasonFertilityMultiplier,
            finalSnapshot.MiddleRegionSeasonFertilityMultiplier,
            finalSnapshot.RightRegionSeasonFertilityMultiplier,
            "0.###x"));
        WriteMetric(writer, "Region population", FormatRegionCounts(
            finalSnapshot.LeftRegionCreatureCount,
            finalSnapshot.MiddleRegionCreatureCount,
            finalSnapshot.RightRegionCreatureCount));
        WriteMetric(writer, "Current east progress", FormatPercent(finalSnapshot.CurrentEastProgressShare));
        WriteMetric(writer, "Run east progress", FormatPercent(finalSnapshot.RunEastProgressShare));
        WriteMetric(writer, "Max creature x", finalSnapshot.MaxCreatureX.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Max x ever reached", finalSnapshot.RunMaxCreatureXReached.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Region plant kcal", FormatRegionValues(
            finalSnapshot.LeftRegionPlantCalories,
            finalSnapshot.MiddleRegionPlantCalories,
            finalSnapshot.RightRegionPlantCalories,
            "0"));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        WriteSeasonPressureSection(
            writer,
            seasonPressure,
            founderSummaries.Count(summary => summary.LivingCreatures > 0),
            founderSummaries.Length);

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Foraging Diagnostics</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Seeing food", FormatPercent(Share(finalSnapshot.FoodDetectedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Seeing plants", FormatPercent(Share(finalSnapshot.PlantDetectedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Seeing meat", FormatPercent(Share(finalSnapshot.MeatDetectedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Seeing fresh meat", FormatPercent(Share(finalSnapshot.FreshMeatDetectedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Seeing stale meat", FormatPercent(Share(finalSnapshot.StaleMeatDetectedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Stale seen but not eaten", FormatPercent(Share(finalSnapshot.StaleMeatAvoidedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Visible meat freshness", FormatPercent(finalSnapshot.AverageVisibleMeatFreshness));
        WriteMetric(writer, "Smelling meat", FormatPercent(Share(finalSnapshot.MeatScentDetectedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Smelling rot", FormatPercent(Share(finalSnapshot.RottenMeatScentDetectedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Seeing creatures", FormatPercent(Share(finalSnapshot.CreatureDetectedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Touching food", FormatPercent(Share(finalSnapshot.FoodContactCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Eating this tick", FormatPercent(Share(finalSnapshot.EatingCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Gut fullness", FormatPercent(finalSnapshot.AverageGutFillRatio));
        WriteMetric(writer, "Gut plant share", FormatPercent(finalSnapshot.AverageGutPlantShare));
        WriteMetric(writer, "Gut meat share", FormatPercent(finalSnapshot.AverageGutMeatShare));
        WriteMetric(writer, "Visible food density", finalSnapshot.AverageVisibleFoodDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible plant density", finalSnapshot.AverageVisiblePlantDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible meat density", finalSnapshot.AverageVisibleMeatDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Meat scent density", finalSnapshot.AverageMeatScentDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Rot scent density", finalSnapshot.AverageRottenMeatScentDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible creature density", finalSnapshot.AverageVisibleCreatureDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Obstacle sensed", FormatPercent(Share(finalSnapshot.ObstacleSensedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Movement blocked", FormatPercent(Share(finalSnapshot.ObstacleBlockedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Obstacle pressure", $"{finalSnapshot.AverageForwardObstacle:0.###} fwd / {finalSnapshot.AverageLeftObstacle:0.###} left / {finalSnapshot.AverageRightObstacle:0.###} right");
        WriteMetric(writer, "Calories eaten", $"{finalSnapshot.TotalCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Plant eaten", $"{finalSnapshot.TotalPlantCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Carcass eaten", $"{finalSnapshot.TotalCarcassCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Fresh meat eaten", $"{finalSnapshot.TotalFreshMeatCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Stale meat eaten", $"{finalSnapshot.TotalStaleMeatCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Rotten damage", $"{finalSnapshot.TotalRottenMeatDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Rotten affected", FormatPercent(Share(finalSnapshot.RottenMeatDamagedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Egg eaten", $"{finalSnapshot.TotalEggCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Fresh kill eaten", $"{finalSnapshot.TotalLivePreyCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Calories digested", $"{finalSnapshot.TotalCaloriesDigestedPerSecond:0.###} energy/s");
        WriteMetric(writer, "Plant energy", $"{finalSnapshot.TotalPlantDigestedEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Meat energy", $"{finalSnapshot.TotalMeatDigestedEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Time since meal", $"{finalSnapshot.AverageSecondsSinceLastMeal:0.###} s avg");
        WriteMetric(writer, "Distance moved", $"{finalSnapshot.TotalDistanceTraveledPerSecond:0.###} units/s");
        WriteMetric(writer, "Distance since meal", $"{finalSnapshot.AverageDistanceSinceLastMeal:0.###} units avg");
        WriteMetric(writer, "Raw per distance", $"{finalSnapshot.CaloriesEatenPerDistance:0.###} kcal/unit");
        WriteMetric(writer, "Energy per distance", $"{finalSnapshot.CaloriesDigestedPerDistance:0.###} energy/unit");
        WriteMetric(writer, "Raw per food vision", $"{finalSnapshot.CaloriesEatenPerFoodVisionEvent:0.###} kcal/event");
        WriteMetric(writer, "Average vision range", finalSnapshot.AverageVisionRange.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average vision angle", $"{ToDegrees(finalSnapshot.AverageVisionAngleRadians):0.###} degrees");
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Memory Diagnostics</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Active memory", $"{FormatPercent(Share(finalSnapshot.ActiveMemoryCreatureCount, finalSnapshot.CreatureCount))} ({finalSnapshot.ActiveMemoryCreatureCount})");
        WriteMetric(writer, "Avg memory strength", finalSnapshot.AverageMemoryStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Food contact", $"{FormatPercent(finalSnapshot.MemoryUserFoodContactShare)} memory / {FormatPercent(finalSnapshot.NonMemoryUserFoodContactShare)} non");
        WriteMetric(writer, "Eating", $"{FormatPercent(finalSnapshot.MemoryUserEatingShare)} memory / {FormatPercent(finalSnapshot.NonMemoryUserEatingShare)} non");
        WriteMetric(writer, "Food success", $"{FormatPercent(finalSnapshot.MemoryUserAverageRecentFoodSuccess)} memory / {FormatPercent(finalSnapshot.NonMemoryUserAverageRecentFoodSuccess)} non");
        WriteMetric(writer, "Raw per distance", $"{finalSnapshot.MemoryUserCaloriesEatenPerDistance:0.###} memory / {finalSnapshot.NonMemoryUserCaloriesEatenPerDistance:0.###} non");
        WriteMetric(writer, "Meal gap", $"{finalSnapshot.MemoryUserAverageSecondsSinceLastMeal:0.###}s memory / {finalSnapshot.NonMemoryUserAverageSecondsSinceLastMeal:0.###}s non");
        WriteMetric(writer, "Meal distance", $"{finalSnapshot.MemoryUserAverageDistanceSinceLastMeal:0.###}u memory / {finalSnapshot.NonMemoryUserAverageDistanceSinceLastMeal:0.###}u non");
        WriteMetric(writer, "Generation", $"{finalSnapshot.MemoryUserAverageGeneration:0.###} memory / {finalSnapshot.NonMemoryUserAverageGeneration:0.###} non");
        WriteMetric(writer, "Avg max-X progress", $"{FormatPercent(finalSnapshot.MemoryUserAverageMaxXProgressShare)} memory / {FormatPercent(finalSnapshot.NonMemoryUserAverageMaxXProgressShare)} non");
        WriteMetric(writer, "Right-region share", $"{FormatPercent(finalSnapshot.MemoryUserRightRegionShare)} memory / {FormatPercent(finalSnapshot.NonMemoryUserRightRegionShare)} non");
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        var attackDamagePerAttacker = finalSnapshot.AttackingCreatureCount > 0
            ? finalSnapshot.TotalAttackDamagePerSecond / finalSnapshot.AttackingCreatureCount
            : 0f;
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Predation Diagnostics</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Seeing creatures", FormatPercent(Share(finalSnapshot.CreatureDetectedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Attacking this tick", FormatPercent(Share(finalSnapshot.AttackingCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Attack damage", $"{finalSnapshot.TotalAttackDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Damage per attacker", $"{attackDamagePerAttacker:0.###} health/s");
        WriteMetric(writer, "Injury deaths", state.Stats.InjuryDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Rotten meat deaths", state.Stats.RottenMeatDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Fresh kill share", FormatPercent(finalSnapshot.FreshKillCaloriesEatenShare));
        WriteMetric(writer, "Meat raw share", FormatPercent(finalSnapshot.MeatCaloriesEatenShare));
        WriteMetric(writer, "Fresh meat share", FormatPercent(finalSnapshot.FreshMeatCaloriesEatenShare));
        WriteMetric(writer, "Stale meat share", FormatPercent(finalSnapshot.StaleMeatCaloriesEatenShare));
        WriteMetric(writer, "Rotten damage", $"{finalSnapshot.TotalRottenMeatDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Rotten affected", FormatPercent(Share(finalSnapshot.RottenMeatDamagedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Meat energy share", FormatPercent(finalSnapshot.MeatDigestedEnergyShare));
        WriteMetric(writer, "Average diet", finalSnapshot.AverageDietaryAdaptation.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average carrion", finalSnapshot.AverageCarrionAdaptation.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average bite", finalSnapshot.AverageBiteStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average resistance", finalSnapshot.AverageDamageResistance.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Attacker diet", finalSnapshot.AttackerAverageDietaryAdaptation.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Attacker bite", finalSnapshot.AttackerAverageBiteStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Attacker resistance", finalSnapshot.AttackerAverageDamageResistance.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Non-attacker diet", finalSnapshot.NonAttackerAverageDietaryAdaptation.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Non-attacker bite", finalSnapshot.NonAttackerAverageBiteStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Non-attacker resistance", finalSnapshot.NonAttackerAverageDamageResistance.ToString("0.###", CultureInfo.InvariantCulture));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Biomes</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Biome</th><th>Area</th><th>Area Share</th><th>Density Mult</th><th>Regrowth Mult</th><th>Season Amp</th><th>Move Cost</th><th>Basal Cost</th><th>Speed</th><th>Resources</th><th>Resources/M</th><th>Plant kcal</th><th>Meat kcal</th><th>Eaten/s</th><th>Deaths</th><th>Living</th><th>Living Share</th></tr></thead>");
        writer.WriteLine("<tbody>");
        var movementCostProfile = scenario.CreateBiomeMovementCostProfile();
        var basalCostProfile = scenario.CreateBiomeBasalCostProfile();
        var speedProfile = scenario.CreateBiomeSpeedProfile();
        var seasonalAmplitudeProfile = scenario.CreateBiomeSeasonalAmplitudeProfile();
        foreach (var summary in biomeSummaries)
        {
            var resourcesPerMillion = summary.Area > 0f
                ? summary.ResourceCount / summary.Area * SimulationScenario.ResourceDensityAreaUnits
                : 0f;
            var livingCreatureCount = CreatureCountForBiome(finalSnapshot, summary.Kind);
            var movementCost = movementCostProfile.For(summary.Kind).ToString("0.###", CultureInfo.InvariantCulture);
            var basalCost = basalCostProfile.For(summary.Kind).ToString("0.###", CultureInfo.InvariantCulture);
            var speed = speedProfile.For(summary.Kind).ToString("0.###", CultureInfo.InvariantCulture);
            var seasonalAmplitude = seasonalAmplitudeProfile.For(summary.Kind).ToString("0.###", CultureInfo.InvariantCulture);
            var meatCalories = MeatCaloriesForBiome(finalSnapshot, summary.Kind);
            var eatenPerSecond = CaloriesEatenForBiome(finalSnapshot, summary.Kind);
            var deaths = DeathCountForBiome(finalSnapshot, summary.Kind);
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(FormatBiomeKind(summary.Kind))}</td>" +
                $"<td>{Html(summary.Area.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(summary.Area / MathF.Max(1f, state.Bounds.Width * state.Bounds.Height)))}</td>" +
                $"<td>{Html(summary.ResourceDensityMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ResourceRegrowthMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html($"{seasonalAmplitude}x")}</td>" +
                $"<td>{Html($"{movementCost}x")}</td>" +
                $"<td>{Html($"{basalCost}x")}</td>" +
                $"<td>{Html($"{speed}x")}</td>" +
                $"<td>{Html(summary.ResourceCount)}</td>" +
                $"<td>{Html(resourcesPerMillion.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ResourceCalories.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(meatCalories.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(eatenPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(deaths)}</td>" +
                $"<td>{Html(livingCreatureCount)}</td>" +
                $"<td>{Html(FormatPercent(Share(livingCreatureCount, finalSnapshot.CreatureCount)))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        WriteChartsSection(writer, snapshots);

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Trends</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Metric</th><th>Start</th><th>Final</th><th>Min</th><th>Max</th><th>Change</th></tr></thead>");
        writer.WriteLine("<tbody>");
        WriteTrendRow(writer, "Population", populationTrend, "creatures");
        WriteTrendRow(writer, "Eggs", eggTrend, "eggs");
        WriteTrendRow(writer, "Resource calories", resourceTrend, "kcal");
        WriteTrendRow(writer, "Seeing food", foodSeenTrend, "%");
        WriteTrendRow(writer, "Touching food", eatingContactTrend, "%");
        WriteTrendRow(writer, "Visible food density", visibleFoodDensityTrend, "");
        WriteTrendRow(writer, "Calories eaten", caloriesEatenTrend, "raw kcal/s");
        WriteTrendRow(writer, "Calories digested", caloriesDigestedTrend, "energy/s");
        WriteTrendRow(writer, "Gut fullness", gutFillTrend, "%");
        WriteTrendRow(writer, "Attacking", attackingTrend, "%");
        WriteTrendRow(writer, "Attack damage", attackDamageTrend, "health/s");
        WriteTrendRow(writer, "Time since meal", mealGapTrend, "s avg");
        WriteTrendRow(writer, "Distance since meal", mealDistanceTrend, "units avg");
        WriteTrendRow(writer, "Distance moved", distanceTraveledTrend, "units/s");
        WriteTrendRow(writer, "Calories per distance", caloriesPerDistanceTrend, "kcal/unit");
        WriteTrendRow(writer, "Calories per food vision", caloriesPerFoodVisionTrend, "kcal/event");
        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        WriteBehaviorAssaySection(writer, behaviorSummary);
        WriteLineageBehaviorAssaySection(writer, lineageBehaviorSummaries);
        WriteBrainInputDiagnosticsSection(writer, brainInputDiagnostics);
        WriteLineageBrainInputDiagnosticsSection(writer, lineageBrainInputDiagnostics);

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Diagnostics</h2>");
        writer.WriteLine("<ul>");
        foreach (var diagnostic in BuildDiagnostics(scenario, state, populationTrend, resourceTrend, finalSnapshot))
        {
            writer.WriteLine($"<li>{Html(diagnostic)}</li>");
        }

        writer.WriteLine("</ul>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Dominant Lineage Over Time</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Tick</th><th>Time</th><th>Founder</th><th>Living</th><th>Share</th><th>Founder Generations</th><th>Overall Generations</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var row in SelectReportRows(dominantLineageRows))
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(row.Tick)}</td>" +
                $"<td>{Html(row.ElapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(row.FounderId.Value)}</td>" +
                $"<td>{Html(row.LivingCreatures)}</td>" +
                $"<td>{Html(FormatPercent(row.LivingShare))}</td>" +
                $"<td>{Html(FormatGenerationRange(row.FounderMinGeneration, row.FounderAverageGeneration, row.FounderMaxGeneration))}</td>" +
                $"<td>{Html(FormatGenerationRange(row.OverallMinGeneration, row.OverallAverageGeneration, row.OverallMaxGeneration))}</td>" +
                "</tr>");
        }

        if (dominantLineageRows.Count == 0)
        {
            WriteEmptyRow(writer, 7, "No living lineages were present in recorded snapshots.");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Top Founder Lineages</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Founder</th><th>Total</th><th>Descendants</th><th>Living</th><th>Dead</th><th>Max Generation</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in founderSummaries.Take(10))
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.FounderId.Value)}</td>" +
                $"<td>{Html(summary.TotalCreatures)}</td>" +
                $"<td>{Html(summary.DescendantCount)}</td>" +
                $"<td>{Html(summary.LivingCreatures)}</td>" +
                $"<td>{Html(summary.DeadCreatures)}</td>" +
                $"<td>{Html(summary.MaxGeneration)}</td>" +
                "</tr>");
        }

        if (founderSummaries.Length == 0)
        {
            WriteEmptyRow(writer, 6, "No founder records were present.");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Founder Exploration</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Founder</th><th>Total</th><th>Living</th><th>Max Generation</th><th>Current East</th><th>Ever East</th><th>Living Middle</th><th>Living Right</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in founderExplorationSummaries)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.FounderId.Value)}</td>" +
                $"<td>{Html(summary.TotalCreatures)}</td>" +
                $"<td>{Html(summary.LivingCreatures)}</td>" +
                $"<td>{Html(summary.MaxGeneration)}</td>" +
                $"<td>{Html(FormatPercent(summary.CurrentEastProgressShare))}</td>" +
                $"<td>{Html(FormatPercent(summary.MaxEastProgressShare))}</td>" +
                $"<td>{Html(summary.LivingMiddleCreatures)}</td>" +
                $"<td>{Html(summary.LivingRightCreatures)}</td>" +
                "</tr>");
        }

        if (founderExplorationSummaries.Count == 0)
        {
            WriteEmptyRow(writer, 8, "No founder exploration data was present.");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Generation Survival</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Generation</th><th>Births</th><th>Living</th><th>Dead</th><th>Starvation Deaths</th><th>Injury Deaths</th><th>Rotten Meat Deaths</th><th>Survival Rate</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in generationSummaries)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.Generation)}</td>" +
                $"<td>{Html(summary.Births)}</td>" +
                $"<td>{Html(summary.Living)}</td>" +
                $"<td>{Html(summary.Dead)}</td>" +
                $"<td>{Html(summary.StarvationDeaths)}</td>" +
                $"<td>{Html(summary.InjuryDeaths)}</td>" +
                $"<td>{Html(summary.RottenMeatDeaths)}</td>" +
                $"<td>{Html(FormatPercent(summary.SurvivalRate))}</td>" +
                "</tr>");
        }

        if (generationSummaries.Count == 0)
        {
            WriteEmptyRow(writer, 8, "No generation records were present.");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Final Living Traits</h2>");
        if (state.Creatures.Count == 0)
        {
            writer.WriteLine("<p>No living creatures remained at the end of the run.</p>");
        }
        else
        {
            writer.WriteLine("<div class=\"table-wrap\"><table>");
            writer.WriteLine("<thead><tr><th>Trait</th><th>Average</th><th>Min</th><th>Max</th></tr></thead>");
            writer.WriteLine("<tbody>");
            WriteTraitRow(writer, "Body radius", traitSummary.BodyRadius);
            WriteTraitRow(writer, "Max speed", traitSummary.MaxSpeed);
            WriteTraitRow(writer, "Vision range", traitSummary.SenseRadius);
            WriteTraitRow(writer, "Vision angle degrees", ToDegrees(traitSummary.VisionAngleRadians));
            WriteTraitRow(writer, "Reproduction threshold", traitSummary.ReproductionThreshold);
            WriteTraitRow(writer, "Offspring investment", traitSummary.OffspringInvestment);
            WriteTraitRow(writer, "Egg production per second", traitSummary.EggProductionEnergyPerSecond);
            WriteTraitRow(writer, "Egg incubation seconds", traitSummary.EggIncubationSeconds);
            WriteTraitRow(writer, "Maturity age seconds", traitSummary.MaturityAgeSeconds);
            WriteTraitRow(writer, "Dietary adaptation meat bias", traitSummary.DietaryAdaptation);
            WriteTraitRow(writer, "Carrion adaptation stale bias", traitSummary.CarrionAdaptation);
            WriteTraitRow(writer, "Plant digestion efficiency", traitSummary.PlantDigestion);
            WriteTraitRow(writer, "Meat digestion efficiency", traitSummary.MeatDigestion);
            WriteTraitRow(writer, "Fresh meat digestion efficiency", traitSummary.FreshMeatDigestion);
            WriteTraitRow(writer, "Stale meat digestion efficiency", traitSummary.StaleMeatDigestion);
            WriteTraitRow(writer, "Bite strength", traitSummary.BiteStrength);
            WriteTraitRow(writer, "Damage resistance", traitSummary.DamageResistance);
            WriteTraitRow(writer, "Mutation strength", traitSummary.MutationStrength);
            WriteTraitRow(writer, "Trait mutation rate", traitSummary.TraitMutationRate);
            WriteTraitRow(writer, "Brain mutation rate", traitSummary.BrainMutationRate);
            writer.WriteLine("</tbody></table></div>");
        }

        writer.WriteLine("</section>");

        if (outputPaths.CheckpointDirectory is not null)
        {
            writer.WriteLine("<section>");
            writer.WriteLine("<h2>Checkpoints</h2>");
            writer.WriteLine($"<p>Checkpoint snapshots can be loaded directly from the Godot launcher.</p>");
            writer.WriteLine("<div class=\"table-wrap\"><table>");
            writer.WriteLine("<thead><tr><th>Tick</th><th>Snapshot</th><th>Path</th></tr></thead>");
            writer.WriteLine("<tbody>");
            foreach (var checkpoint in checkpoints)
            {
                var fullPath = Path.GetFullPath(checkpoint.Path);
                writer.WriteLine(
                    "<tr>" +
                    $"<td>{Html(checkpoint.Tick)}</td>" +
                    $"<td><a href=\"{Html(FileHref(fullPath))}\">{Html(Path.GetFileName(fullPath))}</a></td>" +
                    $"<td><code>{Html(fullPath)}</code></td>" +
                    "</tr>");
            }

            if (checkpoints.Count == 0)
            {
                WriteEmptyRow(writer, 3, "No checkpoint was written because the run ended before the first checkpoint interval.");
            }

            writer.WriteLine("</tbody></table></div>");
            writer.WriteLine("</section>");
        }

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Output Files</h2>");
        writer.WriteLine("<ul class=\"file-list\">");
        WriteOptionalPath(writer, "Stats CSV", outputPaths.StatsPath);
        WriteOptionalPath(writer, "Lineage CSV", outputPaths.LineagePath);
        WriteOptionalPath(writer, "Traits CSV", outputPaths.TraitSummaryPath);
        WriteOptionalPath(writer, "Founders CSV", outputPaths.FounderSummaryPath);
        WriteOptionalPath(writer, "Generations CSV", outputPaths.GenerationSummaryPath);
        WriteOptionalPath(writer, "Lineage trends CSV", outputPaths.LineageTrendPath);
        WriteOptionalPath(writer, "Profile CSV", outputPaths.ProfilePath);
        WriteOptionalPath(writer, "Sensing profile CSV", outputPaths.SensingProfilePath);
        WriteOptionalPath(writer, "Snapshot JSON", options.SaveSnapshotPath);
        WriteOptionalPath(writer, "Checkpoint directory", outputPaths.CheckpointDirectory);
        writer.WriteLine("</ul>");
        writer.WriteLine("</section>");

        writer.WriteLine("</main>");
        WriteDocumentEnd(writer);
    }

    private static void WriteDocumentStart(StreamWriter writer, string title)
    {
        writer.WriteLine("<!doctype html>");
        writer.WriteLine("<html lang=\"en\">");
        writer.WriteLine("<head>");
        writer.WriteLine("<meta charset=\"utf-8\">");
        writer.WriteLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        writer.WriteLine($"<title>{Html(title)}</title>");
        writer.WriteLine(
            """
            <style>
            :root {
              color-scheme: light;
              --bg: #f6f7f2;
              --panel: #ffffff;
              --ink: #1f261f;
              --muted: #637061;
              --line: #dfe5d9;
              --accent: #2e6848;
              --accent-dark: #203b2b;
            }
            * { box-sizing: border-box; }
            body {
              margin: 0;
              font-family: "Segoe UI", Arial, sans-serif;
              background: var(--bg);
              color: var(--ink);
              line-height: 1.45;
            }
            header {
              background: var(--accent-dark);
              color: #fff;
              padding: 32px 0 30px;
            }
            header p { max-width: 760px; margin: 8px 0 0; color: #dbe8d7; }
            h1, h2 { margin: 0; line-height: 1.15; }
            h1 { font-size: 2.15rem; }
            h2 { font-size: 1.25rem; margin-bottom: 16px; }
            section {
              margin: 20px 0;
              padding: 18px;
              background: var(--panel);
              border: 1px solid var(--line);
              border-radius: 8px;
            }
            .page-width {
              width: min(1120px, calc(100% - 32px));
              margin: 0 auto;
            }
            .eyebrow {
              margin: 0 0 6px;
              color: #b9d8be;
              font-size: 0.78rem;
              font-weight: 700;
              text-transform: uppercase;
            }
            .metric-grid {
              display: grid;
              grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
              gap: 10px;
            }
            .metric {
              min-width: 0;
              padding: 10px 12px;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #fbfcf8;
            }
            .metric-label {
              display: block;
              color: var(--muted);
              font-size: 0.78rem;
              font-weight: 700;
              text-transform: uppercase;
            }
            .metric-value {
              display: block;
              margin-top: 4px;
              overflow-wrap: anywhere;
              font-size: 1rem;
              font-weight: 650;
            }
            .table-wrap { overflow-x: auto; }
            table {
              width: 100%;
              border-collapse: collapse;
              font-size: 0.94rem;
            }
            th, td {
              padding: 8px 10px;
              border-bottom: 1px solid var(--line);
              text-align: right;
              white-space: nowrap;
            }
            th:first-child, td:first-child { text-align: left; }
            th {
              color: var(--muted);
              font-size: 0.78rem;
              text-transform: uppercase;
            }
            tr:last-child td { border-bottom: 0; }
            ul { margin: 0; padding-left: 20px; }
            li + li { margin-top: 6px; }
            code {
              padding: 2px 5px;
              border-radius: 4px;
              background: #eef2e9;
              color: #253525;
            }
            .file-list code { overflow-wrap: anywhere; }
            .empty { color: var(--muted); text-align: left; }
            .chart-grid {
              display: grid;
              grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
              gap: 16px;
            }
            .chart-card {
              border: 1px solid var(--line);
              border-radius: 8px;
              padding: 12px;
              background: #fbfcf8;
            }
            .chart-card h3 {
              margin: 0 0 8px;
              font-size: 0.95rem;
            }
            .chart-card svg {
              width: 100%;
              height: auto;
              display: block;
            }
            .chart-axis {
              stroke: #d6ddcf;
              stroke-width: 1;
            }
            .chart-label {
              fill: var(--muted);
              font-size: 11px;
            }
            .chart-legend {
              display: flex;
              flex-wrap: wrap;
              gap: 8px 12px;
              margin-top: 8px;
              color: var(--muted);
              font-size: 0.82rem;
            }
            .legend-swatch {
              display: inline-block;
              width: 10px;
              height: 10px;
              margin-right: 4px;
              border-radius: 2px;
              vertical-align: -1px;
            }
            @media (max-width: 640px) {
              h1 { font-size: 1.7rem; }
              section { padding: 14px; }
              th, td { padding: 7px 8px; }
            }
            </style>
            """);
        writer.WriteLine("</head>");
        writer.WriteLine("<body>");
    }

    private static void WriteDocumentEnd(StreamWriter writer)
    {
        writer.WriteLine("</body>");
        writer.WriteLine("</html>");
    }

    private static void WriteMetric(StreamWriter writer, string label, string value)
    {
        writer.WriteLine("<div class=\"metric\">");
        writer.WriteLine($"<span class=\"metric-label\">{Html(label)}</span>");
        writer.WriteLine($"<span class=\"metric-value\">{Html(value)}</span>");
        writer.WriteLine("</div>");
    }

    private static void WriteSeasonPressureSection(
        StreamWriter writer,
        SeasonPressureSummary summary,
        int livingFounderLineages,
        int totalFounderLineages)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Season Pressure</h2>");
        if (!summary.Enabled)
        {
            writer.WriteLine("<p class=\"empty\">Seasons are disabled for this scenario.</p>");
            writer.WriteLine("</section>");
            return;
        }

        if (summary.SnapshotCount == 0)
        {
            writer.WriteLine("<p class=\"empty\">No stats snapshots were recorded, so season pressure could not be analyzed.</p>");
            writer.WriteLine("</section>");
            return;
        }

        var low = summary.LowFertility;
        var high = summary.HighFertility;
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Cycles observed", summary.CyclesObserved.ToString("0.##", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Fertility range", FormatFertilityRange(summary));
        WriteMetric(writer, "Low fertility population", FormatSeasonPopulation(low));
        WriteMetric(writer, "High fertility population", FormatSeasonPopulation(high));
        WriteMetric(writer, "Low fertility plants", FormatSeasonPlantCalories(low));
        WriteMetric(writer, "High fertility plants", FormatSeasonPlantCalories(high));
        WriteMetric(writer, "Low starvation rate", FormatSeasonRate(low, band => band.StarvationDeathsPerSecond));
        WriteMetric(writer, "High starvation rate", FormatSeasonRate(high, band => band.StarvationDeathsPerSecond));
        WriteMetric(writer, "Low eggs laid rate", FormatSeasonRate(low, band => band.EggsLaidPerSecond));
        WriteMetric(writer, "High eggs laid rate", FormatSeasonRate(high, band => band.EggsLaidPerSecond));
        WriteMetric(writer, "Living founder lineages", totalFounderLineages > 0 ? $"{livingFounderLineages} / {totalFounderLineages}" : "n/a");
        writer.WriteLine("</div>");

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Season phase</th><th>Samples</th><th>Avg fertility</th><th>Avg pop</th><th>Min pop</th><th>Avg plant kcal</th><th>Min plant kcal</th><th>Births/s</th><th>Eggs/s</th><th>Deaths/s</th><th>Starvation/s</th><th>Food seen</th><th>Meal gap</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var bin in summary.PhaseBins)
        {
            WriteSeasonPressureRow(writer, bin);
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSeasonPressureRow(StreamWriter writer, SeasonPressureBand bin)
    {
        if (bin.SampleCount == 0)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(bin.Label)}</td>" +
                $"<td>{Html(bin.SampleCount)}</td>" +
                "<td colspan=\"11\" class=\"empty\">No snapshots in this phase.</td>" +
                "</tr>");
            return;
        }

        writer.WriteLine(
            "<tr>" +
            $"<td>{Html(bin.Label)}</td>" +
            $"<td>{Html(bin.SampleCount)}</td>" +
            $"<td>{Html($"{bin.AverageFertility:0.###}x")}</td>" +
            $"<td>{Html(bin.AveragePopulation.ToString("0.#", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(bin.MinPopulation)}</td>" +
            $"<td>{Html(bin.AveragePlantCalories.ToString("0.#", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(bin.MinPlantCalories.ToString("0.#", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(bin.BirthsPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(bin.EggsLaidPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(bin.DeathsPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(bin.StarvationDeathsPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(FormatPercent(bin.AverageFoodSeenShare))}</td>" +
            $"<td>{Html($"{bin.AverageMealGapSeconds:0.#}s")}</td>" +
            "</tr>");
    }

    private static string FormatSeasonPopulation(SeasonPressureBand band)
    {
        return band.SampleCount > 0
            ? $"{band.AveragePopulation:0.#} avg, {band.MinPopulation} min"
            : "No samples";
    }

    private static string FormatSeasonPlantCalories(SeasonPressureBand band)
    {
        return band.SampleCount > 0
            ? $"{band.AveragePlantCalories:0} kcal avg, {band.MinPlantCalories:0} min"
            : "No samples";
    }

    private static string FormatSeasonRate(SeasonPressureBand band, Func<SeasonPressureBand, float> selector)
    {
        return band.SampleCount > 0
            ? $"{selector(band):0.###}/s"
            : "No samples";
    }

    private static string FormatFertilityRange(SeasonPressureSummary summary)
    {
        var observed = summary.PhaseBins
            .Where(bin => bin.SampleCount > 0)
            .ToArray();
        if (observed.Length == 0)
        {
            return "n/a";
        }

        return $"{observed.Min(bin => bin.MinFertility):0.###}x-{observed.Max(bin => bin.MaxFertility):0.###}x";
    }

    private static void WriteChartsSection(StreamWriter writer, IReadOnlyList<SimulationStatsSnapshot> snapshots)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Graphs</h2>");
        if (snapshots.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No stats snapshots were recorded, so no graphs are available.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"chart-grid\">");
        WriteLineChart(
            writer,
            "Population and eggs",
            "",
            snapshots,
            new ChartSeries("Creatures", "#2f7d4f", snapshots.Select(snapshot => (float)snapshot.CreatureCount).ToArray()),
            new ChartSeries("Eggs", "#d69d2f", snapshots.Select(snapshot => (float)snapshot.EggCount).ToArray()));
        WriteLineChart(
            writer,
            "Dead-creature lifespan",
            " s",
            snapshots,
            new ChartSeries("Average", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageLifespanSeconds).ToArray()),
            new ChartSeries("Median", "#8f4cb8", snapshots.Select(snapshot => snapshot.MedianLifespanSeconds).ToArray()));
        WriteLineChart(
            writer,
            "Reproduction state",
            "%",
            snapshots,
            new ChartSeries("Intent", "#d69d2f", snapshots.Select(snapshot => Share(snapshot.ReproductionIntentCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Ready", "#8f4cb8", snapshots.Select(snapshot => Share(snapshot.ReproductionReadyCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Reserve", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageEggReserveRatio * 100f).ToArray()),
            new ChartSeries("Surplus", "#2f7d4f", snapshots.Select(snapshot => snapshot.AverageEnergySurplusRatio * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Resource calories",
            " kcal",
            snapshots,
            new ChartSeries("Plants", "#35a862", snapshots.Select(snapshot => snapshot.TotalPlantCalories).ToArray()),
            new ChartSeries("Meat", "#b84a4a", snapshots.Select(snapshot => snapshot.TotalMeatCalories).ToArray()));
        WriteLineChart(
            writer,
            "Plant patch structure",
            "%",
            snapshots,
            new ChartSeries("Occupied cells", "#35a862", snapshots.Select(snapshot => snapshot.PlantPatchOccupiedCellShare * 100f).ToArray()),
            new ChartSeries("Top decile kcal", "#d69d2f", snapshots.Select(snapshot => snapshot.PlantPatchTopDecileCaloriesShare * 100f).ToArray()),
            new ChartSeries("Patchiness", "#8f4cb8", snapshots.Select(snapshot => snapshot.PlantPatchiness * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Season fertility",
            "x",
            snapshots,
            new ChartSeries("Global", "#6a8fce", snapshots.Select(snapshot => snapshot.SeasonFertilityMultiplier).ToArray()),
            new ChartSeries("Left", "#35a862", snapshots.Select(snapshot => snapshot.LeftRegionSeasonFertilityMultiplier).ToArray()),
            new ChartSeries("Middle", "#d69d2f", snapshots.Select(snapshot => snapshot.MiddleRegionSeasonFertilityMultiplier).ToArray()),
            new ChartSeries("Right", "#8f4cb8", snapshots.Select(snapshot => snapshot.RightRegionSeasonFertilityMultiplier).ToArray()));
        WriteLineChart(
            writer,
            "Migration regions",
            "%",
            snapshots,
            new ChartSeries("Left", "#35a862", snapshots.Select(snapshot => Share(snapshot.LeftRegionCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Middle", "#d69d2f", snapshots.Select(snapshot => Share(snapshot.MiddleRegionCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Right", "#8f4cb8", snapshots.Select(snapshot => Share(snapshot.RightRegionCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Eastward progress",
            "%",
            snapshots,
            new ChartSeries("Current max", "#6a8fce", snapshots.Select(snapshot => snapshot.CurrentEastProgressShare * 100f).ToArray()),
            new ChartSeries("Run max", "#8f4cb8", snapshots.Select(snapshot => snapshot.RunEastProgressShare * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Regional plant calories",
            " kcal",
            snapshots,
            new ChartSeries("Left", "#35a862", snapshots.Select(snapshot => snapshot.LeftRegionPlantCalories).ToArray()),
            new ChartSeries("Middle", "#d69d2f", snapshots.Select(snapshot => snapshot.MiddleRegionPlantCalories).ToArray()),
            new ChartSeries("Right", "#8f4cb8", snapshots.Select(snapshot => snapshot.RightRegionPlantCalories).ToArray()));
        WriteLineChart(
            writer,
            "Biome occupancy",
            "%",
            snapshots,
            new ChartSeries("Barren", "#9a6b3b", snapshots.Select(snapshot => Share(snapshot.BarrenCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Sparse", "#7f8f3a", snapshots.Select(snapshot => Share(snapshot.SparseCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Grassland", "#35a862", snapshots.Select(snapshot => Share(snapshot.GrasslandCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => Share(snapshot.RichCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Biome pressure",
            "x",
            snapshots,
            new ChartSeries("Move cost", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageBiomeMovementCostMultiplier).ToArray()),
            new ChartSeries("Basal cost", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageBiomeBasalCostMultiplier).ToArray()),
            new ChartSeries("Speed", "#2f7d4f", snapshots.Select(snapshot => snapshot.AverageBiomeSpeedMultiplier).ToArray()));
        WriteLineChart(
            writer,
            "Biome foraging",
            " kcal/s",
            snapshots,
            new ChartSeries("Barren", "#9a6b3b", snapshots.Select(snapshot => snapshot.BarrenCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Sparse", "#7f8f3a", snapshots.Select(snapshot => snapshot.SparseCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Grassland", "#35a862", snapshots.Select(snapshot => snapshot.GrasslandCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => snapshot.RichCaloriesEatenPerSecond).ToArray()));
        WriteLineChart(
            writer,
            "Biome deaths",
            "",
            snapshots,
            new ChartSeries("Barren", "#9a6b3b", snapshots.Select(snapshot => (float)snapshot.BarrenDeathCount).ToArray()),
            new ChartSeries("Sparse", "#7f8f3a", snapshots.Select(snapshot => (float)snapshot.SparseDeathCount).ToArray()),
            new ChartSeries("Grassland", "#35a862", snapshots.Select(snapshot => (float)snapshot.GrasslandDeathCount).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => (float)snapshot.RichDeathCount).ToArray()));
        WriteLineChart(
            writer,
            "Foraging signals",
            "%",
            snapshots,
            new ChartSeries("Seeing food", "#2f7d4f", snapshots.Select(snapshot => Share(snapshot.FoodDetectedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Smelling meat", "#b84a4a", snapshots.Select(snapshot => Share(snapshot.MeatScentDetectedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Smelling rot", "#7d5546", snapshots.Select(snapshot => Share(snapshot.RottenMeatScentDetectedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Touching food", "#6a8fce", snapshots.Select(snapshot => Share(snapshot.FoodContactCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Eating", "#d69d2f", snapshots.Select(snapshot => Share(snapshot.EatingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Obstacle pressure",
            "%",
            snapshots,
            new ChartSeries("Sensed", "#6a8fce", snapshots.Select(snapshot => Share(snapshot.ObstacleSensedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Blocked", "#d96b3b", snapshots.Select(snapshot => Share(snapshot.ObstacleBlockedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Forward cue", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageForwardObstacle * 100f).ToArray()),
            new ChartSeries("Side cue", "#2f7d4f", snapshots.Select(snapshot => MathF.Max(snapshot.AverageLeftObstacle, snapshot.AverageRightObstacle) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Search Efficiency",
            "",
            snapshots,
            new ChartSeries("Distance/s", "#6a8fce", snapshots.Select(snapshot => snapshot.TotalDistanceTraveledPerSecond).ToArray()),
            new ChartSeries("Meal distance", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageDistanceSinceLastMeal).ToArray()),
            new ChartSeries("Raw kcal/unit", "#2f7d4f", snapshots.Select(snapshot => snapshot.CaloriesEatenPerDistance).ToArray()),
            new ChartSeries("Raw kcal/vision", "#8f4cb8", snapshots.Select(snapshot => snapshot.CaloriesEatenPerFoodVisionEvent).ToArray()));
        WriteLineChart(
            writer,
            "Memory use",
            "%",
            snapshots,
            new ChartSeries("Active creatures", "#6a8fce", snapshots.Select(snapshot => Share(snapshot.ActiveMemoryCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Avg strength", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageMemoryStrength * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Memory foraging split",
            "",
            snapshots,
            new ChartSeries("Memory kcal/unit", "#2f7d4f", snapshots.Select(snapshot => snapshot.MemoryUserCaloriesEatenPerDistance).ToArray()),
            new ChartSeries("Non-memory kcal/unit", "#d69d2f", snapshots.Select(snapshot => snapshot.NonMemoryUserCaloriesEatenPerDistance).ToArray()));
        WriteLineChart(
            writer,
            "Memory route progress",
            "%",
            snapshots,
            new ChartSeries("Memory avg max-X", "#6a8fce", snapshots.Select(snapshot => snapshot.MemoryUserAverageMaxXProgressShare * 100f).ToArray()),
            new ChartSeries("Non-memory avg max-X", "#d69d2f", snapshots.Select(snapshot => snapshot.NonMemoryUserAverageMaxXProgressShare * 100f).ToArray()),
            new ChartSeries("Memory right region", "#8f4cb8", snapshots.Select(snapshot => snapshot.MemoryUserRightRegionShare * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Digestion",
            "",
            snapshots,
            new ChartSeries("Raw eaten/s", "#d69d2f", snapshots.Select(snapshot => snapshot.TotalCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Digested/s", "#2f7d4f", snapshots.Select(snapshot => snapshot.TotalCaloriesDigestedPerSecond).ToArray()),
            new ChartSeries("Gut fullness %", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageGutFillRatio * 100f).ToArray()),
            new ChartSeries("Rotten dmg/s", "#7d5546", snapshots.Select(snapshot => snapshot.TotalRottenMeatDamagePerSecond).ToArray()));
        WriteLineChart(
            writer,
            "Food Source Intake",
            " kcal/s",
            snapshots,
            new ChartSeries("Plant eaten/s", "#35a862", snapshots.Select(snapshot => snapshot.TotalPlantCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Carcass eaten/s", "#b84a4a", snapshots.Select(snapshot => snapshot.TotalCarcassCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Fresh meat/s", "#e05a47", snapshots.Select(snapshot => snapshot.TotalFreshMeatCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Stale meat/s", "#7d5546", snapshots.Select(snapshot => snapshot.TotalStaleMeatCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Egg eaten/s", "#d69d2f", snapshots.Select(snapshot => snapshot.TotalEggCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Fresh kill eaten/s", "#8f4cb8", snapshots.Select(snapshot => snapshot.TotalLivePreyCaloriesEatenPerSecond).ToArray()));
        WriteLineChart(
            writer,
            "Predation Diagnostics",
            "%",
            snapshots,
            new ChartSeries("Seeing creatures", "#6a8fce", snapshots.Select(snapshot => Share(snapshot.CreatureDetectedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Attacking", "#e05a47", snapshots.Select(snapshot => Share(snapshot.AttackingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Fresh kill share", "#8f4cb8", snapshots.Select(snapshot => snapshot.FreshKillCaloriesEatenShare * 100f).ToArray()),
            new ChartSeries("Fresh meat share", "#d69d2f", snapshots.Select(snapshot => snapshot.FreshMeatCaloriesEatenShare * 100f).ToArray()),
            new ChartSeries("Meat energy share", "#b84a4a", snapshots.Select(snapshot => snapshot.MeatDigestedEnergyShare * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Meat Freshness",
            "%",
            snapshots,
            new ChartSeries("Avg freshness", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageMeatFreshness * 100f).ToArray()),
            new ChartSeries("Visible freshness", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageVisibleMeatFreshness * 100f).ToArray()),
            new ChartSeries("Fresh eaten share", "#35a862", snapshots.Select(snapshot => snapshot.FreshMeatCaloriesEatenShare * 100f).ToArray()),
            new ChartSeries("Stale eaten share", "#b84a4a", snapshots.Select(snapshot => snapshot.StaleMeatCaloriesEatenShare * 100f).ToArray()),
            new ChartSeries("Stale seen", "#7d5546", snapshots.Select(snapshot => Share(snapshot.StaleMeatDetectedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Stale avoided", "#9a6b3b", snapshots.Select(snapshot => Share(snapshot.StaleMeatAvoidedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Rotten affected", "#8f4cb8", snapshots.Select(snapshot => Share(snapshot.RottenMeatDamagedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Diet Traits",
            "",
            snapshots,
            new ChartSeries("Diet meat bias", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageDietaryAdaptation).ToArray()),
            new ChartSeries("Carrion bias", "#7d5546", snapshots.Select(snapshot => snapshot.AverageCarrionAdaptation).ToArray()));
        WriteLineChart(
            writer,
            "Digested Energy Source",
            " energy/s",
            snapshots,
            new ChartSeries("Plant energy/s", "#35a862", snapshots.Select(snapshot => snapshot.TotalPlantDigestedEnergyPerSecond).ToArray()),
            new ChartSeries("Meat energy/s", "#b84a4a", snapshots.Select(snapshot => snapshot.TotalMeatDigestedEnergyPerSecond).ToArray()));
        WriteLineChart(
            writer,
            "Combat pressure",
            "",
            snapshots,
            new ChartSeries("Attacking %", "#d96b3b", snapshots.Select(snapshot => Share(snapshot.AttackingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Attack damage", "#9d3434", snapshots.Select(snapshot => snapshot.TotalAttackDamagePerSecond).ToArray()));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");
    }

    private static void WriteLineChart(
        StreamWriter writer,
        string title,
        string unit,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        params ChartSeries[] series)
    {
        const float width = 720f;
        const float height = 240f;
        const float left = 46f;
        const float right = 14f;
        const float top = 16f;
        const float bottom = 34f;
        var plotWidth = width - left - right;
        var plotHeight = height - top - bottom;

        var min = 0f;
        var max = 1f;
        var hasValue = false;
        foreach (var chartSeries in series)
        {
            foreach (var value in chartSeries.Values)
            {
                if (!float.IsFinite(value))
                {
                    continue;
                }

                if (!hasValue)
                {
                    min = value;
                    max = value;
                    hasValue = true;
                }
                else
                {
                    min = Math.Min(min, value);
                    max = Math.Max(max, value);
                }
            }
        }

        min = Math.Min(0f, min);
        if (Math.Abs(max - min) < 0.000001f)
        {
            max = min + 1f;
        }

        writer.WriteLine("<div class=\"chart-card\">");
        writer.WriteLine($"<h3>{Html(title)}</h3>");
        writer.WriteLine($"<svg viewBox=\"0 0 {width:0} {height:0}\" role=\"img\" aria-label=\"{Html(title)} chart\">");
        writer.WriteLine($"<line class=\"chart-axis\" x1=\"{left:0}\" y1=\"{top:0}\" x2=\"{left:0}\" y2=\"{height - bottom:0}\" />");
        writer.WriteLine($"<line class=\"chart-axis\" x1=\"{left:0}\" y1=\"{height - bottom:0}\" x2=\"{width - right:0}\" y2=\"{height - bottom:0}\" />");
        writer.WriteLine($"<text class=\"chart-label\" x=\"4\" y=\"{top + 4:0}\">{Html(FormatChartValue(max, unit))}</text>");
        writer.WriteLine($"<text class=\"chart-label\" x=\"4\" y=\"{height - bottom:0}\">{Html(FormatChartValue(min, unit))}</text>");

        foreach (var chartSeries in series)
        {
            if (chartSeries.Values.Length == 0)
            {
                continue;
            }

            var points = new string[chartSeries.Values.Length];
            for (var i = 0; i < chartSeries.Values.Length; i++)
            {
                var x = chartSeries.Values.Length == 1
                    ? left
                    : left + i / (float)(chartSeries.Values.Length - 1) * plotWidth;
                var y = top + (max - chartSeries.Values[i]) / (max - min) * plotHeight;
                points[i] = $"{x.ToString("0.###", CultureInfo.InvariantCulture)},{y.ToString("0.###", CultureInfo.InvariantCulture)}";
            }

            writer.WriteLine($"<polyline points=\"{Html(string.Join(' ', points))}\" fill=\"none\" stroke=\"{Html(chartSeries.Color)}\" stroke-width=\"2.4\" stroke-linejoin=\"round\" stroke-linecap=\"round\" />");
        }

        writer.WriteLine("</svg>");
        writer.WriteLine("<div class=\"chart-legend\">");
        foreach (var chartSeries in series)
        {
            var final = chartSeries.Values.Length > 0 ? chartSeries.Values[^1] : 0f;
            writer.WriteLine(
                $"<span><span class=\"legend-swatch\" style=\"background:{Html(chartSeries.Color)}\"></span>{Html(chartSeries.Label)} {Html(FormatChartValue(final, unit))}</span>");
        }

        writer.WriteLine("</div>");
        writer.WriteLine("</div>");
    }

    private static void WriteBehaviorAssaySection(StreamWriter writer, BehaviorAssaySummary summary)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Behavior Assays</h2>");
        if (summary.EvaluatedCreatureCount == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living neural creatures were available for behavior assays.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Brains evaluated", summary.EvaluatedCreatureCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Movement style", summary.MovementStyle);
        WriteMetric(writer, "Search response", summary.SearchTendency);
        WriteMetric(writer, "Population ecotype", summary.Ecotype);
        WriteMetric(writer, "Food response", summary.ForagingBias);
        WriteMetric(writer, "Creature attack response", summary.PredatorTendency);
        WriteMetric(writer, "Risk response", summary.RiskResponse);
        WriteMetric(writer, "Terrain response", summary.TerrainResponse);
        WriteMetric(writer, "Egg laying", summary.ReproductionTendency);
        WriteMetric(writer, "Rotten meat response", summary.RottenMeatResponse);
        WriteMetric(writer, "Fresh meat preference", summary.FreshMeatPreferenceScore.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Rot scent avoidance", summary.RottenScentAvoidanceScore.ToString("0.###", CultureInfo.InvariantCulture));
        writer.WriteLine("</div>");

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Assay</th><th>Move</th><th>Turn</th><th>Eat</th><th>Reproduce</th><th>Attack</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var result in summary.Results)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(result.Name)}</td>" +
                $"<td>{Html(result.MoveForward.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.Turn.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(result.EatShare))}</td>" +
                $"<td>{Html(FormatPercent(result.ReproduceShare))}</td>" +
                $"<td>{Html(FormatPercent(result.AttackShare))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteLineageBehaviorAssaySection(StreamWriter writer, IReadOnlyList<LineageBehaviorAssaySummary> summaries)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Lineage Behavior Assays</h2>");
        if (summaries.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living founder lineages were available for behavior assays.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Founder</th><th>Living</th><th>Share</th><th>Ecotype</th><th>Food</th><th>Rotten Meat</th><th>Risk</th><th>Terrain</th><th>Attack</th><th>Movement</th><th>Egg Laying</th><th>Small Attack</th><th>Large Approach Attack</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in summaries)
        {
            var behavior = summary.Behavior;
            writer.WriteLine(
                "<tr>" +
                $"<td>#{Html(summary.FounderId.Value)}</td>" +
                $"<td>{Html(summary.LivingCreatures)}</td>" +
                $"<td>{Html(FormatPercent(summary.LivingShare))}</td>" +
                $"<td>{Html(behavior.Ecotype)}</td>" +
                $"<td>{Html(behavior.ForagingBias)}</td>" +
                $"<td>{Html(behavior.RottenMeatResponse)}</td>" +
                $"<td>{Html(behavior.RiskResponse)}</td>" +
                $"<td>{Html(behavior.TerrainResponse)}</td>" +
                $"<td>{Html(behavior.PredatorTendency)}</td>" +
                $"<td>{Html(behavior.MovementStyle)}</td>" +
                $"<td>{Html(behavior.ReproductionTendency)}</td>" +
                $"<td>{Html(FormatPercent(behavior.SmallCreatureAhead.AttackShare))}</td>" +
                $"<td>{Html(FormatPercent(behavior.LargeCreatureApproaching.AttackShare))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteBrainInputDiagnosticsSection(StreamWriter writer, BrainInputDiagnosticSummary summary)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Freshness Brain Wiring</h2>");
        if (summary.EvaluatedCreatureCount == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living neural creatures were available for brain-input diagnostics.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Brains evaluated", summary.EvaluatedCreatureCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Direct freshness magnitude", FormatBrainWeight(summary.DirectFreshnessWeightMagnitude));
        WriteMetric(writer, "Direct rot-scent magnitude", FormatBrainWeight(summary.DirectRotScentWeightMagnitude));
        WriteMetric(writer, "Hidden freshness magnitude", FormatBrainWeight(summary.HiddenFreshnessWeightMagnitude));
        WriteMetric(writer, "Hidden rot-scent magnitude", FormatBrainWeight(summary.HiddenRotScentWeightMagnitude));
        WriteMetric(writer, "Move from freshness", FormatSignedBrainWeight(summary.MoveFreshnessWeight));
        WriteMetric(writer, "Eat from freshness", FormatSignedBrainWeight(summary.EatFreshnessWeight));
        WriteMetric(writer, "Move from rot ahead", FormatSignedBrainWeight(summary.MoveRotScentForwardWeight));
        WriteMetric(writer, "Turn from rot right", FormatSignedBrainWeight(summary.TurnRotScentRightWeight));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");
    }

    private static void WriteLineageBrainInputDiagnosticsSection(
        StreamWriter writer,
        IReadOnlyList<LineageBrainInputDiagnosticSummary> summaries)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Lineage Freshness Wiring</h2>");
        if (summaries.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living founder lineages were available for brain-input diagnostics.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Founder</th><th>Living</th><th>Share</th><th>Fresh Direct</th><th>Rot Direct</th><th>Fresh Hidden</th><th>Rot Hidden</th><th>Move Fresh</th><th>Eat Fresh</th><th>Move Rot Ahead</th><th>Turn Rot Right</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in summaries)
        {
            var diagnostics = summary.Diagnostics;
            writer.WriteLine(
                "<tr>" +
                $"<td>#{Html(summary.FounderId.Value)}</td>" +
                $"<td>{Html(summary.LivingCreatures)}</td>" +
                $"<td>{Html(FormatPercent(summary.LivingShare))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectFreshnessWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectRotScentWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenFreshnessWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenRotScentWeightMagnitude))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveFreshnessWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.EatFreshnessWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveRotScentForwardWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.TurnRotScentRightWeight))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static string FormatChartValue(float value, string unit)
    {
        return unit switch
        {
            "%" => $"{value:0.#}%",
            " kcal" => $"{value:0.#} kcal",
            "x" => $"{value:0.###}x",
            _ => value.ToString("0.###", CultureInfo.InvariantCulture)
        };
    }

    private static void WriteTrendRow(StreamWriter writer, string name, Trend trend, string unit)
    {
        writer.WriteLine(
            "<tr>" +
            $"<td>{Html(name)}</td>" +
            $"<td>{Html(FormatTrendValue(trend.Start, unit))}</td>" +
            $"<td>{Html(FormatTrendValue(trend.Final, unit))}</td>" +
            $"<td>{Html(trend.Min.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(trend.Max.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html($"{trend.Change:0.###} ({trend.PercentChange:0.#}%)")}</td>" +
            "</tr>");
    }

    private readonly record struct ChartSeries(string Label, string Color, float[] Values);

    private readonly record struct FounderExplorationSummary(
        EntityId FounderId,
        int TotalCreatures,
        int LivingCreatures,
        int MaxGeneration,
        float CurrentEastProgressShare,
        float MaxEastProgressShare,
        int LivingMiddleCreatures,
        int LivingRightCreatures);

    private struct FounderExplorationAccumulator
    {
        public int TotalCreatures;
        public int LivingCreatures;
        public int MaxGeneration;
        public float CurrentMaxX;
        public float MaxXReached;
        public int LivingMiddleCreatures;
        public int LivingRightCreatures;
    }

    private static string FormatTrendValue(float value, string unit)
    {
        return unit switch
        {
            "" => value.ToString("0.###", CultureInfo.InvariantCulture),
            "%" => $"{value:0.###}%",
            _ => $"{value:0.###} {unit}"
        };
    }

    private static void WriteTraitRow(StreamWriter writer, string name, FloatSummary summary)
    {
        writer.WriteLine(
            "<tr>" +
            $"<td>{Html(name)}</td>" +
            $"<td>{Html(summary.Average.ToString("0.######", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(summary.Min.ToString("0.######", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(summary.Max.ToString("0.######", CultureInfo.InvariantCulture))}</td>" +
            "</tr>");
    }

    private static void WriteOptionalPath(StreamWriter writer, string label, string? path)
    {
        if (path is not null)
        {
            var fullPath = Path.GetFullPath(path);
            writer.WriteLine(
                $"<li>{Html(label)}: <a href=\"{Html(FileHref(fullPath))}\"><code>{Html(fullPath)}</code></a></li>");
        }
    }

    private static string FileHref(string path)
    {
        return new Uri(Path.GetFullPath(path)).AbsoluteUri;
    }

    private static void WriteEmptyRow(StreamWriter writer, int colspan, string message)
    {
        writer.WriteLine($"<tr><td class=\"empty\" colspan=\"{colspan}\">{Html(message)}</td></tr>");
    }

    private static IReadOnlyList<LineageTrendRow> SelectReportRows(IReadOnlyList<LineageTrendRow> rows)
    {
        if (rows.Count <= ReportTrendRowCount)
        {
            return rows;
        }

        var selected = new List<LineageTrendRow>();
        var lastIndex = -1;
        for (var i = 0; i < ReportTrendRowCount; i++)
        {
            var index = (int)Math.Round(i * (rows.Count - 1) / (double)(ReportTrendRowCount - 1));
            if (index == lastIndex)
            {
                continue;
            }

            selected.Add(rows[index]);
            lastIndex = index;
        }

        return selected;
    }

    private static string FormatGenerationRange(int min, float average, int max)
    {
        return min == max
            ? $"{min} avg {average:0.##}"
            : $"{min}-{max} avg {average:0.##}";
    }

    private static string FormatRange(float min, float max)
    {
        return $"{min.ToString("0.###", CultureInfo.InvariantCulture)}-{max.ToString("0.###", CultureInfo.InvariantCulture)}";
    }

    private static string FormatInitialBrainKind(InitialBrainKind kind)
    {
        return kind switch
        {
            InitialBrainKind.SeedForager => "Seed forager",
            InitialBrainKind.ExplorerForager => "Explorer forager",
            InitialBrainKind.ScavengerForager => "Scavenger forager",
            InitialBrainKind.FreshnessAwareScavenger => "Freshness-aware scavenger",
            InitialBrainKind.ForagerPredator => "Forager predator",
            InitialBrainKind.RandomPerFounder => "Per-founder random weights",
            _ => kind.ToString()
        };
    }

    private static string FormatScenarioSpeciesSeeds(SimulationScenario scenario)
    {
        var seeds = scenario.EnabledSpeciesSeeds().ToArray();
        if (seeds.Length == 0)
        {
            return "None";
        }

        return string.Join(
            ", ",
            seeds.Select(seed =>
            {
                var energy = seed.EnergyOverride is null
                    ? "profile energy"
                    : $"{seed.EnergyOverride.Value:0.###} energy";
                return $"{seed.Count} x {Path.GetFileName(seed.ProfilePath)} in {seed.SpawnRegion} ({energy})";
            }));
    }

    private static IReadOnlyList<FounderExplorationSummary> SummarizeFounderExploration(WorldState state, int maxRows)
    {
        if (state.LineageRecords.Count == 0 || maxRows <= 0)
        {
            return Array.Empty<FounderExplorationSummary>();
        }

        var recordsById = state.LineageRecords.ToDictionary(record => record.Id);
        var accumulators = new Dictionary<EntityId, FounderExplorationAccumulator>();
        foreach (var record in state.LineageRecords)
        {
            var founderId = FounderSummaryCsvWriter.FindFounderId(record, recordsById);
            accumulators.TryGetValue(founderId, out var accumulator);
            accumulator.TotalCreatures++;
            accumulator.MaxGeneration = Math.Max(accumulator.MaxGeneration, record.Generation);
            accumulator.MaxXReached = Math.Max(accumulator.MaxXReached, record.MaxXReached);
            accumulators[founderId] = accumulator;
        }

        var third = state.Bounds.Width / 3f;
        foreach (var creature in state.Creatures)
        {
            if (!recordsById.TryGetValue(creature.Id, out var record))
            {
                continue;
            }

            var founderId = FounderSummaryCsvWriter.FindFounderId(record, recordsById);
            accumulators.TryGetValue(founderId, out var accumulator);
            accumulator.LivingCreatures++;
            accumulator.CurrentMaxX = Math.Max(accumulator.CurrentMaxX, creature.Position.X);
            accumulator.MaxXReached = Math.Max(accumulator.MaxXReached, creature.MaxXReached);
            if (creature.Position.X >= third * 2f)
            {
                accumulator.LivingRightCreatures++;
            }
            else if (creature.Position.X >= third)
            {
                accumulator.LivingMiddleCreatures++;
            }

            accumulators[founderId] = accumulator;
        }

        return accumulators
            .Select(pair => new FounderExplorationSummary(
                pair.Key,
                pair.Value.TotalCreatures,
                pair.Value.LivingCreatures,
                pair.Value.MaxGeneration,
                EastProgressShare(pair.Value.CurrentMaxX, state.Bounds),
                EastProgressShare(pair.Value.MaxXReached, state.Bounds),
                pair.Value.LivingMiddleCreatures,
                pair.Value.LivingRightCreatures))
            .OrderByDescending(summary => summary.MaxEastProgressShare)
            .ThenByDescending(summary => summary.CurrentEastProgressShare)
            .ThenByDescending(summary => summary.LivingCreatures)
            .ThenBy(summary => summary.FounderId.Value)
            .Take(maxRows)
            .ToArray();
    }

    private static string FormatPercent(float value)
    {
        return $"{value * 100f:0.0}%";
    }

    private static string FormatPlantRelocations(SimulationStats stats)
    {
        return $"local {stats.PlantLocalDispersalCount}, cluster {stats.PlantClusterRelocationCount}, global {stats.PlantGlobalRelocationCount}";
    }

    private static string FormatBrainWeight(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatSignedBrainWeight(float value)
    {
        return value.ToString("+0.###;-0.###;0", CultureInfo.InvariantCulture);
    }

    private static float Share(int count, int total)
    {
        return total > 0 ? count / (float)total : 0f;
    }

    private static float EastProgressShare(float x, WorldBounds bounds)
    {
        return bounds.Width > 0f
            ? Math.Clamp(x / bounds.Width, 0f, 1f)
            : 0f;
    }

    private static string FormatBiomeKind(BiomeKind biome)
    {
        return biome.ToString();
    }

    private static string FormatBiomePressureProfile(BiomePressureProfile profile)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Barren {profile.Barren:0.###}x, Sparse {profile.Sparse:0.###}x, Grassland {profile.Grassland:0.###}x, Rich {profile.Rich:0.###}x");
    }

    private static string FormatRegionCounts(int left, int middle, int right)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Left {left}, Middle {middle}, Right {right}");
    }

    private static string FormatRegionValues(float left, float middle, float right, string format)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Left {left.ToString(format, CultureInfo.InvariantCulture)}, Middle {middle.ToString(format, CultureInfo.InvariantCulture)}, Right {right.ToString(format, CultureInfo.InvariantCulture)}");
    }

    private static int CreatureCountForBiome(SimulationStatsSnapshot snapshot, BiomeKind biome)
    {
        return biome switch
        {
            BiomeKind.Barren => snapshot.BarrenCreatureCount,
            BiomeKind.Sparse => snapshot.SparseCreatureCount,
            BiomeKind.Rich => snapshot.RichCreatureCount,
            _ => snapshot.GrasslandCreatureCount
        };
    }

    private static float MeatCaloriesForBiome(SimulationStatsSnapshot snapshot, BiomeKind biome)
    {
        return biome switch
        {
            BiomeKind.Barren => snapshot.BarrenMeatCalories,
            BiomeKind.Sparse => snapshot.SparseMeatCalories,
            BiomeKind.Rich => snapshot.RichMeatCalories,
            _ => snapshot.GrasslandMeatCalories
        };
    }

    private static float CaloriesEatenForBiome(SimulationStatsSnapshot snapshot, BiomeKind biome)
    {
        return biome switch
        {
            BiomeKind.Barren => snapshot.BarrenCaloriesEatenPerSecond,
            BiomeKind.Sparse => snapshot.SparseCaloriesEatenPerSecond,
            BiomeKind.Rich => snapshot.RichCaloriesEatenPerSecond,
            _ => snapshot.GrasslandCaloriesEatenPerSecond
        };
    }

    private static int DeathCountForBiome(SimulationStatsSnapshot snapshot, BiomeKind biome)
    {
        return biome switch
        {
            BiomeKind.Barren => snapshot.BarrenDeathCount,
            BiomeKind.Sparse => snapshot.SparseDeathCount,
            BiomeKind.Rich => snapshot.RichDeathCount,
            _ => snapshot.GrasslandDeathCount
        };
    }

    private static float ToDegrees(float radians)
    {
        return radians * 180f / MathF.PI;
    }

    private static FloatSummary ToDegrees(FloatSummary summary)
    {
        return new FloatSummary(
            ToDegrees(summary.Average),
            ToDegrees(summary.Min),
            ToDegrees(summary.Max));
    }

    private static IEnumerable<string> BuildDiagnostics(
        SimulationScenario scenario,
        WorldState state,
        Trend populationTrend,
        Trend resourceTrend,
        SimulationStatsSnapshot finalSnapshot)
    {
        var diagnostics = new List<string>();
        var reproductionCount = Math.Max(0, state.Stats.CreatureBirthCount - scenario.InitialCreatureCount);
        var resourceCapacity = state.Resources.Sum(resource => resource.MaxCalories);

        if (reproductionCount == 0)
        {
            diagnostics.Add("No reproduction occurred. The scenario may be too harsh, too short, or reproduction thresholds may be too high.");
        }

        if (state.Stats.CreatureDeathCount == 0)
        {
            diagnostics.Add("No deaths occurred. Selection pressure is probably weak for this run length.");
        }

        if (state.Creatures.Count == 0)
        {
            diagnostics.Add("Population collapsed to zero.");
        }
        else if (scenario.InitialCreatureCount > 0 && state.Creatures.Count <= Math.Max(1f, scenario.InitialCreatureCount * 0.1f))
        {
            diagnostics.Add("Population fell below 10% of its initial size.");
        }

        if (scenario.InitialCreatureCount > 0 && state.Creatures.Count >= scenario.InitialCreatureCount * 5)
        {
            diagnostics.Add("Population expanded beyond 5x its initial size.");
        }

        if (resourceCapacity > 0f)
        {
            var finalResourceRatio = resourceTrend.Final / resourceCapacity;
            var minResourceRatio = resourceTrend.Min / resourceCapacity;

            if (finalResourceRatio > 0.9f && minResourceRatio > 0.75f)
            {
                diagnostics.Add("Resources stayed abundant. Food pressure may not be strong enough to drive much selection.");
            }
            else if (finalResourceRatio < 0.1f)
            {
                diagnostics.Add("Resources were nearly depleted at the end of the run.");
            }
        }

        if (state.Creatures.Count > 0)
        {
            var seeingFoodShare = Share(finalSnapshot.FoodDetectedCreatureCount, finalSnapshot.CreatureCount);
            var eatingShare = Share(finalSnapshot.EatingCreatureCount, finalSnapshot.CreatureCount);

            if (seeingFoodShare < 0.05f && finalSnapshot.AverageSecondsSinceLastMeal > 20f)
            {
                diagnostics.Add("Very few creatures saw food recently and the average time since meal is high. Foraging pressure may be severe.");
            }
            else if (seeingFoodShare > 0.75f && eatingShare > 0.2f)
            {
                diagnostics.Add("Most creatures can see food and many are eating on the sampled tick. Food search pressure may still be weak.");
            }

            if (finalSnapshot.AverageVisionAngleRadians > MathF.Tau * 0.85f)
            {
                diagnostics.Add("Average vision angle is close to full-circle vision. Consider increasing vision-angle upkeep if this remains dominant.");
            }

            var memoryShare = Share(finalSnapshot.ActiveMemoryCreatureCount, finalSnapshot.CreatureCount);
            if (memoryShare > 0.05f)
            {
                var memoryKcalDelta = finalSnapshot.MemoryUserCaloriesEatenPerDistance
                    - finalSnapshot.NonMemoryUserCaloriesEatenPerDistance;
                var memoryProgressDelta = finalSnapshot.MemoryUserAverageMaxXProgressShare
                    - finalSnapshot.NonMemoryUserAverageMaxXProgressShare;
                diagnostics.Add(
                    $"Active memory is present in {FormatPercent(memoryShare)} of living creatures; memory users differ from non-memory users by {memoryKcalDelta:0.###} raw kcal/unit and {FormatPercent(memoryProgressDelta)} average max-X progress.");
            }
        }

        if (finalSnapshot.MaxGeneration == 0)
        {
            diagnostics.Add("No later-generation creatures were present in the latest stats snapshot.");
        }

        if (Math.Abs(populationTrend.PercentChange) < 5f && state.Stats.CreatureDeathCount == 0 && reproductionCount == 0)
        {
            diagnostics.Add("Population was nearly static. A longer run or stronger pressure may be needed.");
        }

        if (diagnostics.Count == 0)
        {
            diagnostics.Add("No obvious warning signs were detected in this first-pass report.");
        }

        return diagnostics;
    }

    private static string Html(object? value)
    {
        return WebUtility.HtmlEncode(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
    }
}

internal readonly record struct Trend(float Start, float Final, float Min, float Max)
{
    public float Change => Final - Start;

    public float PercentChange => Math.Abs(Start) > 0.000001f ? Change / Start * 100f : 0f;

    public static Trend From(
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        Func<SimulationStatsSnapshot, float> selector,
        float fallbackFinal)
    {
        if (snapshots.Count == 0)
        {
            return new Trend(fallbackFinal, fallbackFinal, fallbackFinal, fallbackFinal);
        }

        var start = selector(snapshots[0]);
        var min = start;
        var max = start;

        for (var i = 0; i < snapshots.Count; i++)
        {
            var value = selector(snapshots[i]);
            min = Math.Min(min, value);
            max = Math.Max(max, value);
        }

        min = Math.Min(min, fallbackFinal);
        max = Math.Max(max, fallbackFinal);
        return new Trend(start, fallbackFinal, min, max);
    }
}

internal readonly record struct TraitAccumulator(
    int Count,
    FloatSummary BodyRadius,
    FloatSummary MaxSpeed,
    FloatSummary SenseRadius,
    FloatSummary VisionAngleRadians,
    FloatSummary ReproductionThreshold,
    FloatSummary OffspringInvestment,
    FloatSummary EggProductionEnergyPerSecond,
    FloatSummary EggIncubationSeconds,
    FloatSummary MaturityAgeSeconds,
    FloatSummary DietaryAdaptation,
    FloatSummary CarrionAdaptation,
    FloatSummary PlantDigestion,
    FloatSummary MeatDigestion,
    FloatSummary FreshMeatDigestion,
    FloatSummary StaleMeatDigestion,
    FloatSummary GutCapacityCalories,
    FloatSummary DigestionCaloriesPerSecond,
    FloatSummary BiteStrength,
    FloatSummary DamageResistance,
    FloatSummary MutationStrength,
    FloatSummary TraitMutationRate,
    FloatSummary BrainMutationRate)
{
    public static TraitAccumulator FromLivingCreatures(WorldState state)
    {
        var bodyRadius = new FloatAccumulator();
        var maxSpeed = new FloatAccumulator();
        var senseRadius = new FloatAccumulator();
        var visionAngleRadians = new FloatAccumulator();
        var reproductionThreshold = new FloatAccumulator();
        var offspringInvestment = new FloatAccumulator();
        var eggProductionEnergyPerSecond = new FloatAccumulator();
        var eggIncubationSeconds = new FloatAccumulator();
        var maturityAgeSeconds = new FloatAccumulator();
        var dietaryAdaptation = new FloatAccumulator();
        var carrionAdaptation = new FloatAccumulator();
        var plantDigestion = new FloatAccumulator();
        var meatDigestion = new FloatAccumulator();
        var freshMeatDigestion = new FloatAccumulator();
        var staleMeatDigestion = new FloatAccumulator();
        var gutCapacityCalories = new FloatAccumulator();
        var digestionCaloriesPerSecond = new FloatAccumulator();
        var biteStrength = new FloatAccumulator();
        var damageResistance = new FloatAccumulator();
        var mutationStrength = new FloatAccumulator();
        var traitMutationRate = new FloatAccumulator();
        var brainMutationRate = new FloatAccumulator();

        foreach (var creature in state.Creatures)
        {
            var genome = state.GetGenome(creature.GenomeId);
            bodyRadius.Add(genome.BodyRadius);
            maxSpeed.Add(genome.MaxSpeed);
            senseRadius.Add(genome.SenseRadius);
            visionAngleRadians.Add(genome.VisionAngleRadians);
            reproductionThreshold.Add(genome.ReproductionEnergyThreshold);
            offspringInvestment.Add(genome.OffspringEnergyInvestment);
            eggProductionEnergyPerSecond.Add(genome.EggProductionEnergyPerSecond);
            eggIncubationSeconds.Add(genome.EggIncubationSeconds);
            maturityAgeSeconds.Add(genome.MaturityAgeSeconds);
            dietaryAdaptation.Add(genome.DietaryAdaptation);
            carrionAdaptation.Add(genome.CarrionAdaptation);
            plantDigestion.Add(CreatureDigestion.PlantEfficiency(genome));
            meatDigestion.Add(CreatureDigestion.MeatEfficiency(genome));
            freshMeatDigestion.Add(CreatureDigestion.FreshMeatEnergyEfficiency(genome));
            staleMeatDigestion.Add(CreatureDigestion.StaleMeatEnergyEfficiency(genome));
            gutCapacityCalories.Add(genome.GutCapacityCalories);
            digestionCaloriesPerSecond.Add(genome.DigestionCaloriesPerSecond);
            biteStrength.Add(genome.BiteStrength);
            damageResistance.Add(genome.DamageResistance);
            mutationStrength.Add(genome.MutationStrength);
            traitMutationRate.Add(genome.TraitMutationRate);
            brainMutationRate.Add(genome.BrainMutationRate);
        }

        return new TraitAccumulator(
            state.Creatures.Count,
            bodyRadius.ToSummary(),
            maxSpeed.ToSummary(),
            senseRadius.ToSummary(),
            visionAngleRadians.ToSummary(),
            reproductionThreshold.ToSummary(),
            offspringInvestment.ToSummary(),
            eggProductionEnergyPerSecond.ToSummary(),
            eggIncubationSeconds.ToSummary(),
            maturityAgeSeconds.ToSummary(),
            dietaryAdaptation.ToSummary(),
            carrionAdaptation.ToSummary(),
            plantDigestion.ToSummary(),
            meatDigestion.ToSummary(),
            freshMeatDigestion.ToSummary(),
            staleMeatDigestion.ToSummary(),
            gutCapacityCalories.ToSummary(),
            digestionCaloriesPerSecond.ToSummary(),
            biteStrength.ToSummary(),
            damageResistance.ToSummary(),
            mutationStrength.ToSummary(),
            traitMutationRate.ToSummary(),
            brainMutationRate.ToSummary());
    }
}

internal readonly record struct FloatSummary(float Average, float Min, float Max);

internal struct FloatAccumulator
{
    private float _sum;
    private float _min;
    private float _max;

    public int Count { get; private set; }

    public void Add(float value)
    {
        if (Count == 0)
        {
            _min = value;
            _max = value;
        }
        else
        {
            _min = Math.Min(_min, value);
            _max = Math.Max(_max, value);
        }

        _sum += value;
        Count++;
    }

    public FloatSummary ToSummary()
    {
        return Count == 0
            ? new FloatSummary(0f, 0f, 0f)
            : new FloatSummary(_sum / Count, _min, _max);
    }
}
