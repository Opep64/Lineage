using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json.Nodes;
using Lineage.Core;

ConsoleLogRedirect? consoleLogRedirect = null;

try
{
    var options = RunOptions.Parse(args).ExpandProcessIdToken();
    consoleLogRedirect = ConsoleLogRedirect.Create(options.StdoutLogPath, options.StderrLogPath);
    if (options.ShowHelp)
    {
        PrintHelp();
        return;
    }

    if (options.IsBiomeMapExport)
    {
        ExportBiomeMap(options);
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
finally
{
    consoleLogRedirect?.Dispose();
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
          --export-biome-map <path>  Export the scenario's biome map as editable manual-map JSON, then exit.
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
          --species-output <path>    Living species cluster summary CSV output path.
          --species-trends-output <path> Species cluster trend CSV output path.
          --founders-output <path>   Founder lineage summary CSV output path.
          --thermal-ecotypes-output <path> Thermal ecotype summary CSV output path.
          --generations-output <path> Generation survival summary CSV output path.
          --lineage-trends-output <path> Founder lineage trend CSV output path.
          --roster-output <path>     Starter profile lineage summary CSV output path.
          --report <path>            HTML run report output path.
          --profile                  Time each simulation system and print/write a profile.
          --profile-output <path>    Per-system profile CSV output path; writes a sensing sidecar.
          --profile-start-tick <n>   Start profiling after n completed ticks. Default: 0
          --profile-end-tick <n>     Stop profiling after n completed ticks.
          --save-snapshot <path>     Save final simulation snapshot JSON.
          --checkpoint-interval <n>  Save loadable snapshot checkpoints every n ticks.
          --checkpoint-dir <dir>      Directory for checkpoint JSON files.
          --status <path>            Periodically write machine-readable run status JSON.
          --control <path>           Poll a JSON control file for stop/checkpoint requests.
          --stdout-log <path>        Append console output to a file.
          --stderr-log <path>        Append console errors to a file.
          --status-interval <n>      Status write interval in ticks. Default: 100
          --status-detail-interval <n> Recompute heavier status metrics every n ticks. Default: 1000
          --stop-on-extinction       Stop early when no creatures and no eggs remain alive.
          --close-sense-refresh-minimum-ticks <n> Minimum stale-world-sense age before proximity close refreshes.
          --sensing-threads <n>      Worker threads for creature sensing. Default: scenario value.
          --reuse-neural-actions-on-skipped-world-senses Reuse prior neural outputs when world senses are stale.
          --no-reuse-neural-actions-on-skipped-world-senses Disable stale-world-sense neural action reuse.
          --neural-controller-threads <n> Worker threads for neural controller evaluation. Default: scenario value.
          --prune-extinct-payloads   Compact genome/brain payloads not referenced by living creatures or eggs.
          --prune-extinct-payload-interval <n> Payload pruning interval in ticks. Default: scenario value.
          --inject-species <path>    Inject a species profile JSON, usually species/name.species.json. Can repeat.
          --inject-species-count <n> Founder count per injected profile. Default: 10
          --inject-species-region <region> Spawn region for injected species. Default: uniform
          --inject-species-energy <n> Override starting energy for injected founders.
          --export-species <path>    Export a species profile, usually species/name.species.json.
          --export-species-creature <id> Export this living creature ID instead of the dominant lineage.
          --export-species-founder <id> Export a representative from this founder lineage.
          --export-species-cluster <id|name> Export the closest living representative of this species cluster.
          --export-species-name <text> Name for the exported species profile.
          --export-species-notes <text> Notes for the exported species profile.
          --export-species-paired-brain Save the exported representative's brain and make it the species default.
          --export-species-paired-brain-path <path> Override paired brain output path.
          --export-brain <path>      Export a brain profile, usually brains/name.brain.json.
          --export-brain-creature <id> Export this living creature brain instead of the dominant lineage.
          --export-brain-name <text> Name for the exported brain profile.
          --export-brain-notes <text> Notes for the exported brain profile.
          --batch-scenario <path>    Add a scenario to a batch comparison. Can repeat.
          --batch-report <path>      HTML comparison report output path.
          --batch-output-dir <dir>   Per-run batch output directory. Default: out/batch
          --probe                    Run a lightweight multi-scenario tuning probe.
          --probe-scenario <path>    Add a scenario to a lightweight probe. Can repeat.
          --probe-seeds <a,b,c>      Comma-separated seed overrides for probe runs.
          --probe-variant <name:key=value,...> Add a temporary scenario variant. Can repeat; base also runs.
          --probe-output <path>      Compact probe CSV output path. Default: out/probe_summary.csv
          --probe-report <path>      Compact probe HTML output path. Default: probe output with .html extension
          --probe-snapshot-interval <n> Override stats interval for probe runs. Default: scenario value
          --probe-stop-on-extinction Stop a probe run early if all creatures die.
          --probe-max-population <n> Stop a probe run early if population exceeds n.
          --no-output                Run without writing CSV.
          --help                     Show this help.

        Examples:
          dotnet run --project .\src\Lineage.Cli -- --scenario .\scenarios\balanced-foraging.json --ticks 20000
          dotnet run --project .\src\Lineage.Cli -- --ticks 20000 --seed 42 --output .\out\seed42_stats.csv --report .\out\seed42_report.html
          dotnet run --project .\src\Lineage.Cli -- --batch-report .\out\preset_comparison.html --ticks 20000 --seed 42
          dotnet run --project .\src\Lineage.Cli -- --probe --ticks 20000 --probe-seeds 42,43,44
          dotnet run --project .\src\Lineage.Cli -- --probe --probe-scenario .\scenarios\balanced-foraging.json --probe-variant sparse:initialResourcesPerMillionArea=120,resourceRegrowthMax=1.0
        """);
}

static RunResult RunSingle(RunOptions options)
{
    if (options.ExportSpeciesPairedBrainEnabled && options.ExportSpeciesPath is null)
    {
        throw new ArgumentException("--export-species-paired-brain requires --export-species.");
    }

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
    var runFiles = new CliRunFiles(options, scenario, outputPaths);
    var runResult = RunSimulation(options, scenario, simulation, outputPaths, runFiles);
    var checkpoints = runResult.Checkpoints;
    stopwatch.Stop();

    if (options.SaveSnapshotPath is not null)
    {
        SimulationSnapshotJson.Save(options.SaveSnapshotPath, SimulationSnapshot.Capture(scenario, simulation));
    }

    WriteRunOutputs(options, scenario, simulation, stopwatch.Elapsed, outputPaths, speciesInjections, checkpoints);
    runFiles.WriteStatus("completed", simulation, runResult.CompletedSteps, checkpoints, runResult.StopReason);

    SpeciesProfile? exportedSpecies = null;
    BrainProfile? exportedSpeciesPairedBrain = null;
    string? exportedSpeciesPairedBrainPath = null;
    if (options.ExportSpeciesPath is not null)
    {
        exportedSpecies = ExportSpeciesProfile(options, scenario, simulation.State);
        if (options.ExportSpeciesPairedBrainEnabled)
        {
            exportedSpeciesPairedBrain = ExportPairedSpeciesBrainProfile(exportedSpecies, scenario, simulation.State);
            exportedSpeciesPairedBrainPath = ResolvePairedSpeciesBrainPath(options, exportedSpecies);
            BrainProfileJson.Save(exportedSpeciesPairedBrainPath, exportedSpeciesPairedBrain);
            exportedSpecies = exportedSpecies with
            {
                DefaultBrainPath = NormalizeCatalogReference(exportedSpeciesPairedBrainPath)
            };
        }

        SpeciesProfileJson.Save(options.ExportSpeciesPath, exportedSpecies);
    }

    if (options.ExportBrainPath is not null)
    {
        BrainProfileJson.Save(options.ExportBrainPath, ExportBrainProfile(options, scenario, simulation.State));
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
        exportedSpecies,
        exportedSpeciesPairedBrainPath,
        exportedSpeciesPairedBrain);
}

static void ExportBiomeMap(RunOptions options)
{
    if (options.ExportBiomeMapPath is null)
    {
        throw new ArgumentException("--export-biome-map requires a path.");
    }

    if (options.LoadSnapshotPath is not null)
    {
        throw new ArgumentException("--export-biome-map exports scenario maps and cannot be combined with --load-snapshot.");
    }

    var scenario = options.CreateScenario();
    if (options.SaveScenarioPath is not null)
    {
        SimulationScenarioJson.Save(options.SaveScenarioPath, scenario);
    }

    var map = SimulationScenarioFactory.CreateBiomeMap(scenario, options.ScenarioDirectory);
    var document = ManualBiomeMapDocument.FromBiomeMap(
        map,
        scenario.Name,
        scenario.EnableBiomes ? scenario.BiomeMapKind : null,
        scenario.Seed);
    ManualBiomeMapJson.Save(options.ExportBiomeMapPath, document);

    Console.WriteLine($"Scenario: {scenario.Name}");
    if (options.ScenarioPath is not null)
    {
        Console.WriteLine($"Scenario file: {Path.GetFullPath(options.ScenarioPath)}");
    }

    if (options.SaveScenarioPath is not null)
    {
        Console.WriteLine($"Saved scenario: {Path.GetFullPath(options.SaveScenarioPath)}");
    }

    Console.WriteLine($"Exported biome map: {Path.GetFullPath(options.ExportBiomeMapPath)}");
    Console.WriteLine($"Map: {map.CellCountX}x{map.CellCountY} cells, {map.CellSize:0.###}u cell size");
}

static (SimulationScenario Scenario, Simulation Simulation) CreateSimulationRun(RunOptions options)
{
    if (options.LoadSnapshotPath is not null)
    {
        var snapshot = SimulationSnapshotJson.Load(options.LoadSnapshotPath);
        var restored = SimulationSnapshotJson.RestoreSimulation(snapshot with
        {
            Scenario = options.ApplySnapshotRuntimeOverrides(snapshot.Scenario)
        });
        return (restored.Scenario, restored.Simulation);
    }

    var scenario = options.CreateScenario();
    return (scenario, SimulationScenarioFactory.CreateSimulation(scenario, options.ScenarioDirectory));
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

    results.AddRange(InjectSpeciesProfiles(options, scenario, simulation));
    return results;
}

static IReadOnlyList<SpeciesInjectionResult> InjectSpeciesProfiles(
    RunOptions options,
    SimulationScenario scenario,
    Simulation simulation)
{
    if (options.InjectSpeciesPaths.Count == 0)
    {
        return Array.Empty<SpeciesInjectionResult>();
    }

    var results = new List<SpeciesInjectionResult>(options.InjectSpeciesPaths.Count);
    foreach (var path in options.InjectSpeciesPaths)
    {
        var resolvedProfilePath = SimulationScenarioSpeciesSeeder.ResolveProfilePath(
            path,
            options.ScenarioPath,
            Directory.GetCurrentDirectory());
        var profile = SpeciesProfileJson.Load(resolvedProfilePath);
        var brainProfile = string.IsNullOrWhiteSpace(profile.DefaultBrainPath)
            ? null
            : BrainProfileJson.Load(SimulationScenarioSpeciesSeeder.ResolveBrainProfilePath(
                profile.DefaultBrainPath,
                resolvedProfilePath,
                options.ScenarioPath,
                Directory.GetCurrentDirectory()));
        results.Add(SpeciesProfileInjector.Inject(
            simulation.State,
            profile,
            new SpeciesInjectionOptions(
                options.InjectSpeciesCount,
                options.InjectSpeciesRegion,
                options.InjectSpeciesEnergy,
                BrainOverrideProfile: brainProfile,
                MutationProfile: MutationProfile.FromScenario(scenario))));
    }

    return results;
}

static SpeciesProfile ExportSpeciesProfile(RunOptions options, SimulationScenario scenario, WorldState state)
{
    var selectorCount = (options.ExportSpeciesCreatureId is null ? 0 : 1)
        + (options.ExportSpeciesFounderId is null ? 0 : 1)
        + (options.ExportSpeciesClusterKey is null ? 0 : 1);
    if (selectorCount > 1)
    {
        throw new ArgumentException("--export-species-creature, --export-species-founder, and --export-species-cluster cannot be combined.");
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

    if (options.ExportSpeciesClusterKey is not null)
    {
        return SpeciesProfileExporter.ExportSpeciesClusterRepresentative(
            scenario,
            state,
            options.ExportSpeciesClusterKey,
            options.ExportSpeciesName,
            options.ExportSpeciesNotes);
    }

    return SpeciesProfileExporter.ExportDominantLivingLineageRepresentative(
        scenario,
        state,
        options.ExportSpeciesName,
        options.ExportSpeciesNotes);
}

static BrainProfile ExportBrainProfile(RunOptions options, SimulationScenario scenario, WorldState state)
{
    if (options.ExportBrainCreatureId is not null)
    {
        return BrainProfileExporter.ExportCreatureBrain(
            scenario,
            state,
            new EntityId(options.ExportBrainCreatureId.Value),
            options.ExportBrainName,
            options.ExportBrainNotes);
    }

    return BrainProfileExporter.ExportDominantLivingLineageBrain(
        scenario,
        state,
        options.ExportBrainName,
        options.ExportBrainNotes);
}

static BrainProfile ExportPairedSpeciesBrainProfile(
    SpeciesProfile species,
    SimulationScenario scenario,
    WorldState state)
{
    var name = $"{species.Name} Brain";
    var notes = string.IsNullOrWhiteSpace(species.Notes)
        ? $"Paired controller exported with species profile {species.Name}."
        : $"Paired controller exported with species profile {species.Name}. {species.Notes}";
    return BrainProfileExporter.ExportCreatureBrain(
        scenario,
        state,
        new EntityId(species.Source.CreatureId),
        name,
        notes);
}

static string ResolvePairedSpeciesBrainPath(RunOptions options, SpeciesProfile species)
{
    if (!string.IsNullOrWhiteSpace(options.ExportSpeciesPairedBrainPath))
    {
        return BrainProfileJson.WithFileExtension(options.ExportSpeciesPairedBrainPath);
    }

    var path = Path.Combine(
        "brains",
        "user",
        $"{SlugifyProfileName($"{species.Name} Brain")}{BrainProfileJson.FileExtension}");
    return GetUniquePath(path, BrainProfileJson.FileExtension);
}

static string NormalizeCatalogReference(string path)
{
    var fullPath = Path.GetFullPath(path);
    var root = Path.GetFullPath(Directory.GetCurrentDirectory());
    var relativePath = Path.GetRelativePath(root, fullPath);
    return !relativePath.StartsWith("..", StringComparison.Ordinal)
        && !Path.IsPathFullyQualified(relativePath)
            ? relativePath.Replace('\\', '/')
            : fullPath;
}

static string GetUniquePath(string preferredPath, string? fullExtension = null)
{
    if (!File.Exists(preferredPath))
    {
        return preferredPath;
    }

    var directory = Path.GetDirectoryName(preferredPath) ?? ".";
    var rawFileName = Path.GetFileName(preferredPath);
    var fileName = !string.IsNullOrWhiteSpace(fullExtension)
        && rawFileName.EndsWith(fullExtension, StringComparison.OrdinalIgnoreCase)
            ? rawFileName[..^fullExtension.Length]
            : Path.GetFileNameWithoutExtension(preferredPath);
    var extension = !string.IsNullOrWhiteSpace(fullExtension)
        && rawFileName.EndsWith(fullExtension, StringComparison.OrdinalIgnoreCase)
            ? fullExtension
            : Path.GetExtension(preferredPath);
    for (var index = 2; index < 1000; index++)
    {
        var candidate = Path.Combine(directory, $"{fileName}_{index}{extension}");
        if (!File.Exists(candidate))
        {
            return candidate;
        }
    }

    throw new IOException("Could not choose a unique paired brain profile path.");
}

static string SlugifyProfileName(string value)
{
    var chars = value.Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '_').ToArray();
    var slug = new string(chars).Trim('_');
    return string.IsNullOrWhiteSpace(slug) ? "profile" : slug;
}

static SimulationRunResult RunSimulation(
    RunOptions options,
    SimulationScenario scenario,
    Simulation simulation,
    OutputPaths outputPaths,
    CliRunFiles runFiles)
{
    if (options.ProfileEndTick is not null
        && options.ProfileEndTick.Value <= (options.ProfileStartTick ?? 0))
    {
        throw new ArgumentException("--profile-end-tick must be greater than --profile-start-tick.");
    }

    if (options.CheckpointIntervalTicks is null && !options.HasProfileWindow && !runFiles.RequiresStepLoop)
    {
        simulation.RunSteps(options.Ticks);
        if (simulation.Profile is not null)
        {
            simulation.Profile.IsActive = false;
        }

        return new SimulationRunResult(Array.Empty<CheckpointArtifact>(), options.Ticks, null);
    }

    string? checkpointDirectory = null;
    if (options.CheckpointIntervalTicks is not null)
    {
        checkpointDirectory = outputPaths.CheckpointDirectory
            ?? throw new InvalidOperationException("Checkpoint interval was set without a checkpoint directory.");
        Directory.CreateDirectory(checkpointDirectory);
    }

    var checkpoints = new List<CheckpointArtifact>();
    long completedSteps = 0;
    string? stopReason = null;
    runFiles.WriteStatus("running", simulation, completedSteps, checkpoints);
    for (var i = 0; i < options.Ticks; i++)
    {
        SetProfileWindowActivity(options, simulation);
        simulation.Step();
        completedSteps++;
        if (options.CheckpointIntervalTicks is not null
            && simulation.State.Tick % options.CheckpointIntervalTicks.Value == 0)
        {
            var path = Path.Combine(checkpointDirectory!, $"tick_{simulation.State.Tick:D10}.json");
            SimulationSnapshotJson.Save(path, SimulationSnapshot.Capture(scenario, simulation));
            checkpoints.Add(new CheckpointArtifact(simulation.State.Tick, path));
        }

        var command = runFiles.ReadControlCommand();
        if (command.RequestsCheckpoint)
        {
            checkpoints.Add(runFiles.SaveCheckpoint(scenario, simulation, "requested"));
        }

        if (command.RequestsStop)
        {
            stopReason = command.Kind == CliRunControlKind.CheckpointAndStop
                ? "checkpoint-and-stop"
                : "stop-requested";
        }

        if (stopReason is null
            && options.StopOnExtinction
            && simulation.State.Creatures.Count == 0
            && simulation.State.Eggs.Count == 0)
        {
            stopReason = "extinction";
        }

        if (runFiles.ShouldWriteStatus(completedSteps) || stopReason is not null)
        {
            runFiles.WriteStatus(
                stopReason is null ? "running" : "stopping",
                simulation,
                completedSteps,
                checkpoints,
                stopReason);
        }

        if (stopReason is not null)
        {
            break;
        }
    }

    if (simulation.Profile is not null)
    {
        simulation.Profile.IsActive = false;
    }

    return new SimulationRunResult(checkpoints, completedSteps, stopReason);
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
        : [Path.Combine("scenarios", "balanced-foraging.json")];

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
                        ?? options.SnapshotIntervalTicksOverride,
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

                var simulation = SimulationScenarioFactory.CreateSimulation(scenario, scenarioOptions.ScenarioDirectory);
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

    return [Path.Combine("scenarios", "balanced-foraging.json")];
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
    IReadOnlyList<SpeciesInjectionResult> speciesInjections,
    IReadOnlyList<CheckpointArtifact> checkpoints)
{
    if (outputPaths.StatsPath is not null)
    {
        StatsCsvWriter.Write(outputPaths.StatsPath, simulation.State.Stats.Snapshots);
        LineageCsvWriter.Write(outputPaths.LineagePath!, simulation.State.LineageRecords);
        TraitSummaryCsvWriter.Write(outputPaths.TraitSummaryPath!, simulation.State);
        SpeciesClusterCsvWriter.Write(outputPaths.SpeciesSummaryPath!, simulation.State);
        SpeciesClusterTrendCsvWriter.Write(outputPaths.SpeciesTrendPath!, simulation.State.Stats.Snapshots, simulation.State);
        FounderSummaryCsvWriter.Write(outputPaths.FounderSummaryPath!, simulation.State.LineageRecords);
        ThermalEcotypeCsvWriter.Write(outputPaths.ThermalEcotypeSummaryPath!, simulation.State);
        GenerationSummaryCsvWriter.Write(outputPaths.GenerationSummaryPath!, simulation.State.LineageRecords);
        LineageTrendCsvWriter.Write(outputPaths.LineageTrendPath!, simulation.State.Stats.Snapshots, simulation.State.LineageRecords);
        RosterLineageSummaryCsvWriter.Write(
            outputPaths.RosterSummaryPath!,
            simulation.State.LineageRecords,
            speciesInjections,
            simulation.State.Tick);
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
        RunReportWriter.Write(outputPaths.ReportPath, options, scenario, simulation, elapsed, outputPaths, speciesInjections, checkpoints);
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
        Console.WriteLine($"Old age deaths: {state.Stats.OldAgeDeathCount}");
    Console.WriteLine($"Max generation: {snapshot.MaxGeneration}");
    Console.WriteLine($"Obstacle sensed: {Percent(snapshot.ObstacleSensedCreatureCount, snapshot.CreatureCount):0.0}%");
    Console.WriteLine($"Movement blocked: {Percent(snapshot.ObstacleBlockedCreatureCount, snapshot.CreatureCount):0.0}%");
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
        Console.WriteLine($"Species clusters CSV: {Path.GetFullPath(outputPaths.SpeciesSummaryPath!)}");
        Console.WriteLine($"Species trends CSV: {Path.GetFullPath(outputPaths.SpeciesTrendPath!)}");
        Console.WriteLine($"Founders CSV: {Path.GetFullPath(outputPaths.FounderSummaryPath!)}");
        Console.WriteLine($"Thermal ecotypes CSV: {Path.GetFullPath(outputPaths.ThermalEcotypeSummaryPath!)}");
        Console.WriteLine($"Generations CSV: {Path.GetFullPath(outputPaths.GenerationSummaryPath!)}");
        Console.WriteLine($"Lineage trends CSV: {Path.GetFullPath(outputPaths.LineageTrendPath!)}");
        Console.WriteLine($"Roster lineages CSV: {Path.GetFullPath(outputPaths.RosterSummaryPath!)}");
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

    if (result.ExportedSpeciesPairedBrainPath is not null && result.ExportedSpeciesPairedBrain is not null)
    {
        Console.WriteLine(
            $"Exported paired brain: {result.ExportedSpeciesPairedBrain.Name} from creature " +
            $"{result.ExportedSpeciesPairedBrain.Source.CreatureId} to {Path.GetFullPath(result.ExportedSpeciesPairedBrainPath)}");
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

    PrintNeuralControllerProfileSummary(profile.NeuralController);
    PrintSensingProfileSummary(profile.Sensing);
}

static void PrintNeuralControllerProfileSummary(SimulationNeuralControllerProfile profile)
{
    if (profile.CreaturesControlled == 0)
    {
        return;
    }

    Console.WriteLine(
        $"Neural controller profile: {profile.BrainEvaluations} evaluations, {profile.ReusedActions} reused"
        + $" ({profile.ReusedActionShare * 100.0:0.0}% reused)");
    Console.WriteLine(
        $"  Evaluation reasons: fresh {profile.FreshWorldSenseEvaluations}, immediate {profile.ImmediateCueEvaluations}, internal {profile.InternalChangeEvaluations}, first {profile.FirstDecisionEvaluations}, max-age {profile.MaxReuseAgeEvaluations}, disabled {profile.ReuseDisabledEvaluations}");
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
        + $" (scheduled {profile.WorldSenseScheduledRefreshes}, close {profile.WorldSenseCloseRefreshes}"
        + $" = immediate {profile.WorldSenseImmediateCloseRefreshes} + proximity {profile.WorldSenseProximityCloseRefreshes}, forced {profile.WorldSenseForcedRefreshes})");
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
        $"  Resource scan: {profile.ResourceScanMilliseconds:0.000}ms"
        + $" (plant {profile.PlantResourceScanMilliseconds:0.000}ms, meat {profile.MeatResourceScanMilliseconds:0.000}ms)"
        + $", plants {profile.PlantCandidates}, meat {profile.MeatResourceCandidates}, visible plants {profile.VisiblePlantCandidates}, visible meat {profile.VisibleMeatResourceCandidates}");
    Console.WriteLine(
        $"  Egg query/scan: {(profile.EggQueryMilliseconds + profile.EggScanMilliseconds):0.000}ms, {FormatAverage(profile.EggCandidates, profile.EggQueries):0.00} candidates/query, visible {profile.VisibleEggCandidates}"
        + $", lineage scent {profile.EggLineageScentCandidates}/{profile.EggLineageScentHits}, identity scent {profile.EggIdentityScentCandidates}/{profile.EggIdentityScentHits}");
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
        $"  Sense finalization: {profile.SenseFinalizationMilliseconds:0.000}ms"
        + $" (refreshed {profile.SenseFinalizationRefreshedMilliseconds:0.000}ms/{profile.SenseFinalizationRefreshedSamples}, skipped {profile.SenseFinalizationSkippedMilliseconds:0.000}ms/{profile.SenseFinalizationSkippedSamples}, bridge {profile.PlantPreferenceBridgeMilliseconds:0.000}ms)");
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
            $"injury {state.Stats.InjuryDeathCount}, rotten {state.Stats.RottenMeatDeathCount}, " +
            $"old age {state.Stats.OldAgeDeathCount}, max gen {snapshot.MaxGeneration}");
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

    public string? ExportBiomeMapPath { get; init; }

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

    public string? SpeciesSummaryOutputPath { get; init; }

    public string? SpeciesTrendOutputPath { get; init; }

    public string? FounderSummaryOutputPath { get; init; }

    public string? ThermalEcotypeSummaryOutputPath { get; init; }

    public string? GenerationSummaryOutputPath { get; init; }

    public string? LineageTrendOutputPath { get; init; }

    public string? RosterSummaryOutputPath { get; init; }

    public string? ReportPath { get; init; }

    public bool Profile { get; init; }

    public string? ProfileOutputPath { get; init; }

    public int? ProfileStartTick { get; init; }

    public int? ProfileEndTick { get; init; }

    public string? SaveSnapshotPath { get; init; }

    public int? CheckpointIntervalTicks { get; init; }

    public string? CheckpointDirectory { get; init; }

    public string? StatusPath { get; init; }

    public string? ControlPath { get; init; }

    public string? StdoutLogPath { get; init; }

    public string? StderrLogPath { get; init; }

    public int StatusIntervalTicks { get; init; } = 100;

    public int StatusDetailIntervalTicks { get; init; } = 1000;

    public bool StopOnExtinction { get; init; }

    public int? CloseSenseRefreshMinimumTicksOverride { get; init; }

    public int? SensingThreadCountOverride { get; init; }

    public bool? ReuseNeuralActionsOnSkippedWorldSensesOverride { get; init; }

    public int? NeuralControllerThreadCountOverride { get; init; }

    public bool EnableExtinctPayloadPruning { get; init; }

    public int? ExtinctPayloadPruneIntervalTicksOverride { get; init; }

    public IReadOnlyList<string> InjectSpeciesPaths { get; init; } = Array.Empty<string>();

    public int InjectSpeciesCount { get; init; } = 10;

    public InitialCreatureSpawnRegion InjectSpeciesRegion { get; init; } = InitialCreatureSpawnRegion.Uniform;

    public float? InjectSpeciesEnergy { get; init; }

    public string? ExportSpeciesPath { get; init; }

    public int? ExportSpeciesCreatureId { get; init; }

    public int? ExportSpeciesFounderId { get; init; }

    public string? ExportSpeciesClusterKey { get; init; }

    public string? ExportSpeciesName { get; init; }

    public string? ExportSpeciesNotes { get; init; }

    public bool ExportSpeciesPairedBrain { get; init; }

    public string? ExportSpeciesPairedBrainPath { get; init; }

    public string? ExportBrainPath { get; init; }

    public int? ExportBrainCreatureId { get; init; }

    public string? ExportBrainName { get; init; }

    public string? ExportBrainNotes { get; init; }

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

    public bool IsBiomeMapExport => ExportBiomeMapPath is not null;

    public bool ExportSpeciesPairedBrainEnabled => ExportSpeciesPairedBrain
        || ExportSpeciesPairedBrainPath is not null;

    public string? ScenarioDirectory => ScenarioPath is null
        ? null
        : Path.GetDirectoryName(Path.GetFullPath(ScenarioPath));

    public RunOptions ExpandProcessIdToken()
    {
        return this with
        {
            SaveScenarioPath = ExpandProcessIdToken(SaveScenarioPath),
            ExportBiomeMapPath = ExpandProcessIdToken(ExportBiomeMapPath),
            OutputPath = ExpandProcessIdToken(OutputPath),
            LineageOutputPath = ExpandProcessIdToken(LineageOutputPath),
            TraitSummaryOutputPath = ExpandProcessIdToken(TraitSummaryOutputPath),
            SpeciesSummaryOutputPath = ExpandProcessIdToken(SpeciesSummaryOutputPath),
            SpeciesTrendOutputPath = ExpandProcessIdToken(SpeciesTrendOutputPath),
            FounderSummaryOutputPath = ExpandProcessIdToken(FounderSummaryOutputPath),
            ThermalEcotypeSummaryOutputPath = ExpandProcessIdToken(ThermalEcotypeSummaryOutputPath),
            GenerationSummaryOutputPath = ExpandProcessIdToken(GenerationSummaryOutputPath),
            LineageTrendOutputPath = ExpandProcessIdToken(LineageTrendOutputPath),
            RosterSummaryOutputPath = ExpandProcessIdToken(RosterSummaryOutputPath),
            ReportPath = ExpandProcessIdToken(ReportPath),
            ProfileOutputPath = ExpandProcessIdToken(ProfileOutputPath),
            SaveSnapshotPath = ExpandProcessIdToken(SaveSnapshotPath),
            CheckpointDirectory = ExpandProcessIdToken(CheckpointDirectory),
            StatusPath = ExpandProcessIdToken(StatusPath),
            ControlPath = ExpandProcessIdToken(ControlPath),
            StdoutLogPath = ExpandProcessIdToken(StdoutLogPath),
            StderrLogPath = ExpandProcessIdToken(StderrLogPath),
            ExportSpeciesPath = ExpandProcessIdToken(ExportSpeciesPath),
            ExportSpeciesPairedBrainPath = ExpandProcessIdToken(ExportSpeciesPairedBrainPath),
            ExportBrainPath = ExpandProcessIdToken(ExportBrainPath),
            BatchReportPath = ExpandProcessIdToken(BatchReportPath),
            BatchOutputDirectory = ExpandProcessIdToken(BatchOutputDirectory) ?? BatchOutputDirectory,
            ProbeOutputPath = ExpandProcessIdToken(ProbeOutputPath),
            ProbeReportPath = ExpandProcessIdToken(ProbeReportPath)
        };
    }

    public SimulationScenario CreateScenario()
    {
        var scenario = ScenarioPath is null
            ? new SimulationScenario()
            : SimulationScenarioJson.Load(ScenarioPath);

        scenario = scenario with
        {
            Seed = SeedOverride ?? scenario.Seed,
            PipelineKind = PipelineKindOverride ?? scenario.PipelineKind,
            InitialCreatureCount = InitialCreatureCountOverride ?? scenario.InitialCreatureCount,
            SpatialCellSize = SpatialCellSizeOverride ?? scenario.SpatialCellSize,
            StatsSnapshotIntervalTicks = SnapshotIntervalTicksOverride ?? scenario.StatsSnapshotIntervalTicks,
            CloseSenseRefreshMinimumTicks = CloseSenseRefreshMinimumTicksOverride
                ?? scenario.CloseSenseRefreshMinimumTicks,
            SensingThreadCount = SensingThreadCountOverride
                ?? scenario.SensingThreadCount,
            ReuseNeuralActionsOnSkippedWorldSenses = ReuseNeuralActionsOnSkippedWorldSensesOverride
                ?? scenario.ReuseNeuralActionsOnSkippedWorldSenses,
            NeuralControllerThreadCount = NeuralControllerThreadCountOverride
                ?? scenario.NeuralControllerThreadCount,
            EnableExtinctPayloadPruning = EnableExtinctPayloadPruning || scenario.EnableExtinctPayloadPruning,
            ExtinctPayloadPruneIntervalTicks = ExtinctPayloadPruneIntervalTicksOverride
                ?? scenario.ExtinctPayloadPruneIntervalTicks
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

    public SimulationScenario ApplySnapshotRuntimeOverrides(SimulationScenario scenario)
    {
        return scenario with
        {
            CloseSenseRefreshMinimumTicks = CloseSenseRefreshMinimumTicksOverride
                ?? scenario.CloseSenseRefreshMinimumTicks,
            SensingThreadCount = SensingThreadCountOverride
                ?? scenario.SensingThreadCount,
            ReuseNeuralActionsOnSkippedWorldSenses = ReuseNeuralActionsOnSkippedWorldSensesOverride
                ?? scenario.ReuseNeuralActionsOnSkippedWorldSenses,
            NeuralControllerThreadCount = NeuralControllerThreadCountOverride
                ?? scenario.NeuralControllerThreadCount,
            EnableExtinctPayloadPruning = EnableExtinctPayloadPruning || scenario.EnableExtinctPayloadPruning,
            ExtinctPayloadPruneIntervalTicks = ExtinctPayloadPruneIntervalTicksOverride
                ?? scenario.ExtinctPayloadPruneIntervalTicks
        };
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
            SpeciesSummaryOutputPath ?? AddSuffix(statsPath, "species"),
            SpeciesTrendOutputPath ?? AddSuffix(statsPath, "species_trends"),
            FounderSummaryOutputPath ?? AddSuffix(statsPath, "founders"),
            ThermalEcotypeSummaryOutputPath ?? AddSuffix(statsPath, "thermal_ecotypes"),
            GenerationSummaryOutputPath ?? AddSuffix(statsPath, "generations"),
            LineageTrendOutputPath ?? AddSuffix(statsPath, "lineage_trends"),
            RosterSummaryOutputPath ?? AddSuffix(statsPath, "roster"),
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
            ExportBiomeMapPath = null,
            OutputPath = DisableOutput ? null : statsPath,
            LineageOutputPath = null,
            TraitSummaryOutputPath = null,
            SpeciesSummaryOutputPath = null,
            SpeciesTrendOutputPath = null,
            FounderSummaryOutputPath = null,
            ThermalEcotypeSummaryOutputPath = null,
            GenerationSummaryOutputPath = null,
            LineageTrendOutputPath = null,
            RosterSummaryOutputPath = null,
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
                case "--export-biome-map":
                    options = options with { ExportBiomeMapPath = ReadValue(args, ref i, arg) };
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
                case "--species-output":
                    options = options with { SpeciesSummaryOutputPath = ReadValue(args, ref i, arg), DisableOutput = false };
                    break;
                case "--species-trends-output":
                    options = options with { SpeciesTrendOutputPath = ReadValue(args, ref i, arg), DisableOutput = false };
                    break;
                case "--founders-output":
                    options = options with { FounderSummaryOutputPath = ReadValue(args, ref i, arg), DisableOutput = false };
                    break;
                case "--thermal-ecotypes-output":
                    options = options with { ThermalEcotypeSummaryOutputPath = ReadValue(args, ref i, arg), DisableOutput = false };
                    break;
                case "--generations-output":
                    options = options with { GenerationSummaryOutputPath = ReadValue(args, ref i, arg), DisableOutput = false };
                    break;
                case "--lineage-trends-output":
                    options = options with { LineageTrendOutputPath = ReadValue(args, ref i, arg), DisableOutput = false };
                    break;
                case "--roster-output":
                    options = options with { RosterSummaryOutputPath = ReadValue(args, ref i, arg), DisableOutput = false };
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
                case "--status":
                    options = options with { StatusPath = ReadValue(args, ref i, arg) };
                    break;
                case "--control":
                    options = options with { ControlPath = ReadValue(args, ref i, arg) };
                    break;
                case "--stdout-log":
                    options = options with { StdoutLogPath = ReadValue(args, ref i, arg) };
                    break;
                case "--stderr-log":
                    options = options with { StderrLogPath = ReadValue(args, ref i, arg) };
                    break;
                case "--status-interval":
                    options = options with { StatusIntervalTicks = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--status-detail-interval":
                    options = options with { StatusDetailIntervalTicks = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--stop-on-extinction":
                    options = options with { StopOnExtinction = true };
                    break;
                case "--close-sense-refresh-minimum-ticks":
                    options = options with { CloseSenseRefreshMinimumTicksOverride = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--sensing-threads":
                    options = options with { SensingThreadCountOverride = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--reuse-neural-actions-on-skipped-world-senses":
                    options = options with { ReuseNeuralActionsOnSkippedWorldSensesOverride = true };
                    break;
                case "--no-reuse-neural-actions-on-skipped-world-senses":
                    options = options with { ReuseNeuralActionsOnSkippedWorldSensesOverride = false };
                    break;
                case "--neural-controller-threads":
                    options = options with { NeuralControllerThreadCountOverride = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--prune-extinct-payloads":
                    options = options with { EnableExtinctPayloadPruning = true };
                    break;
                case "--prune-extinct-payload-interval":
                    options = options with { ExtinctPayloadPruneIntervalTicksOverride = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
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
                case "--export-species-cluster":
                    options = options with { ExportSpeciesClusterKey = ReadValue(args, ref i, arg) };
                    break;
                case "--export-species-name":
                    options = options with { ExportSpeciesName = ReadValue(args, ref i, arg) };
                    break;
                case "--export-species-notes":
                    options = options with { ExportSpeciesNotes = ReadValue(args, ref i, arg) };
                    break;
                case "--export-species-paired-brain":
                    options = options with { ExportSpeciesPairedBrain = true };
                    break;
                case "--export-species-paired-brain-path":
                    options = options with { ExportSpeciesPairedBrainPath = ReadValue(args, ref i, arg) };
                    break;
                case "--export-brain":
                    options = options with { ExportBrainPath = ReadValue(args, ref i, arg) };
                    break;
                case "--export-brain-creature":
                    options = options with { ExportBrainCreatureId = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--export-brain-name":
                    options = options with { ExportBrainName = ReadValue(args, ref i, arg) };
                    break;
                case "--export-brain-notes":
                    options = options with { ExportBrainNotes = ReadValue(args, ref i, arg) };
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

    private static string? ExpandProcessIdToken(string? value)
    {
        return string.IsNullOrEmpty(value)
            ? value
            : value.Replace("{pid}", Environment.ProcessId.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
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

internal sealed class ConsoleLogRedirect : IDisposable
{
    private readonly StreamWriter? _stdout;
    private readonly StreamWriter? _stderr;

    private ConsoleLogRedirect(StreamWriter? stdout, StreamWriter? stderr)
    {
        _stdout = stdout;
        _stderr = stderr;
    }

    public static ConsoleLogRedirect? Create(string? stdoutPath, string? stderrPath)
    {
        var stdout = string.IsNullOrWhiteSpace(stdoutPath) ? null : OpenAppendWriter(stdoutPath);
        var stderr = string.IsNullOrWhiteSpace(stderrPath) ? null : OpenAppendWriter(stderrPath);

        if (stdout is not null)
        {
            Console.SetOut(stdout);
        }

        if (stderr is not null)
        {
            Console.SetError(stderr);
        }

        return stdout is null && stderr is null ? null : new ConsoleLogRedirect(stdout, stderr);
    }

    public void Dispose()
    {
        _stdout?.Dispose();
        _stderr?.Dispose();
    }

    private static StreamWriter OpenAppendWriter(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            AutoFlush = true
        };
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
    SpeciesProfile? ExportedSpecies,
    string? ExportedSpeciesPairedBrainPath,
    BrainProfile? ExportedSpeciesPairedBrain);

internal readonly record struct OutputPaths(
    string? StatsPath,
    string? LineagePath,
    string? TraitSummaryPath,
    string? SpeciesSummaryPath,
    string? SpeciesTrendPath,
    string? FounderSummaryPath,
    string? ThermalEcotypeSummaryPath,
    string? GenerationSummaryPath,
    string? LineageTrendPath,
    string? RosterSummaryPath,
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
    int SnapshotIntervalTicks,
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
    int RottenMeatDeaths,
    int OldAgeDeaths,
    int MaxGeneration,
    int RtNeatBrainCount,
    float RtNeatBrainShare,
    float AverageRtNeatHiddenNodeCount,
    int MaxRtNeatHiddenNodeCount,
    float AverageRtNeatConnectionCount,
    int MaxRtNeatConnectionCount,
    float AverageRtNeatEnabledConnectionCount,
    int MaxRtNeatEnabledConnectionCount,
    float AverageRtNeatFunctionalHiddenNodeCount,
    int MaxRtNeatFunctionalHiddenNodeCount,
    float AverageRtNeatFunctionalConnectionCount,
    int MaxRtNeatFunctionalConnectionCount,
    float AverageRtNeatDisabledConnectionCount,
    int MaxRtNeatDisabledConnectionCount,
    float AverageRtNeatLongestPathLength,
    int MaxRtNeatLongestPathLength,
    float FinalResourceRatio,
    float TotalCreatureEnergy,
    float TotalLivingStoredEnergy,
    float TotalEggEnergy,
    float TotalResourceCalories,
    float TotalPlantCalories,
    float TotalMeatCalories,
    float TotalFatCalories,
    float AverageFatRatio,
    float AverageMassBurdenRatio,
    float AverageFatSpeedMultiplier,
    float AverageFatStorageCapacityCalories,
    float AverageFatStorageEfficiency,
    float FatStoredCaloriesPerSecond,
    float FatReleasedCaloriesPerSecond,
    float AverageMetabolicPace,
    float MinimumMetabolicPace,
    float P10MetabolicPace,
    float MedianMetabolicPace,
    float P90MetabolicPace,
    float MaximumMetabolicPace,
    float MetabolicPaceStdDev,
    int LowMetabolicPaceCreatureCount,
    int NormalMetabolicPaceCreatureCount,
    int HighMetabolicPaceCreatureCount,
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
    float CreatureContactShare,
    float AttackIntentShare,
    float AttackIntentTouchingShare,
    float AttackNoIntentContactShare,
    float RawAttackPositiveShare,
    float RawAttackNearGateShare,
    float RawAttackNearGateTouchingShare,
    float AverageAttackOutput,
    float AverageTouchingAttackOutput,
    float GrabIntentShare,
    float CanGrabShare,
    float GrabIntentCanGrabShare,
    float GrabIntentNoContactShare,
    float HoldingShare,
    float GrabbedShare,
    float AverageGrabOutput,
    float AverageCanGrabGrabOutput,
    float AverageGrabPressure,
    float AverageGrabStrength,
    float SoundEmittingShare,
    float SoundHeardShare,
    float AverageSoundAmplitude,
    float AverageSoundDensity,
    float AverageSoundToneClarity,
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
    int PlantDepletions,
    int PlantLocalDispersals,
    int PlantClusterRelocations,
    int PlantGlobalRelocations,
    int PlantDormancyStarted,
    int PlantDormancyCompleted,
    float PlantPatchOccupiedCellShare,
    float PlantPatchTopDecileCaloriesShare,
    float PlantPatchiness,
    int LocalFertilityCellCount,
    float AverageLocalFertilityMultiplier,
    float MinimumLocalFertilityMultiplier,
    float DepletedLocalFertilityCellShare,
    int TailSnapshotCount,
    long TailStartTick,
    long TailEndTick,
    double TailSeconds,
    float TailAverageCreatures,
    float TailAverageCreatureEnergy,
    float TailAverageLivingStoredEnergy,
    float TailAverageEggEnergy,
    float TailAverageFatRatio,
    float TailAverageMassBurdenRatio,
    float TailAverageFatSpeedMultiplier,
    float TailAverageFatStorageCapacityCalories,
    float TailAverageFatStorageEfficiency,
    float TailFatStoredCaloriesPerSecond,
    float TailFatReleasedCaloriesPerSecond,
    float TailAverageMetabolicPace,
    float TailLowMetabolicPaceCreatures,
    float TailNormalMetabolicPaceCreatures,
    float TailHighMetabolicPaceCreatures,
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
    float TailCreatureContactShare,
    float TailAttackIntentShare,
    float TailAttackIntentTouchingShare,
    float TailAttackNoIntentContactShare,
    float TailRawAttackNearGateTouchingShare,
    float TailAverageAttackOutput,
    float TailAverageTouchingAttackOutput,
    float TailGrabIntentShare,
    float TailCanGrabShare,
    float TailGrabIntentCanGrabShare,
    float TailGrabIntentNoContactShare,
    float TailHoldingShare,
    float TailGrabbedShare,
    float TailAverageGrabOutput,
    float TailAverageCanGrabGrabOutput,
    float TailAverageGrabPressure,
    float TailAverageGrabStrength,
    float TailSoundEmittingShare,
    float TailSoundHeardShare,
    float TailAverageSoundAmplitude,
    float TailAverageSoundDensity,
    float TailAverageSoundToneClarity,
    float TailDeathsPerSecond,
    float TailStarvationDeathsPerSecond,
    float TailInjuryDeathsPerSecond,
    float TailRottenMeatDeathsPerSecond,
    float TailOldAgeDeathsPerSecond,
    float TailCaloriesEatenPerDistance,
    float TailAverageSecondsSinceLastMeal,
    float TailPlantPatchOccupiedCellShare,
    float TailPlantPatchTopDecileCaloriesShare,
    float TailPlantPatchiness,
    float TailAverageLocalFertilityMultiplier,
    float TailMinimumLocalFertilityMultiplier,
    float TailDepletedLocalFertilityCellShare)
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
        var livingEnergy = ProbeLivingEnergySummary.From(state);
        var metabolicPace = ProbeMetabolicPaceSummary.From(state);
        var behavior = BehaviorAssay.Analyze(state);

        return new ProbeRunResult(
            scenarioName,
            scenarioPath,
            variant.Name,
            variant.OverrideSummary,
            scenario.Seed,
            status,
            requestedTicks,
            scenario.StatsSnapshotIntervalTicks,
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
            state.Stats.RottenMeatDeathCount,
            state.Stats.OldAgeDeathCount,
            snapshot.MaxGeneration,
            snapshot.RtNeatBrainCount,
            snapshot.RtNeatBrainShare,
            snapshot.AverageRtNeatHiddenNodeCount,
            snapshot.MaxRtNeatHiddenNodeCount,
            snapshot.AverageRtNeatConnectionCount,
            snapshot.MaxRtNeatConnectionCount,
            snapshot.AverageRtNeatEnabledConnectionCount,
            snapshot.MaxRtNeatEnabledConnectionCount,
            snapshot.AverageRtNeatFunctionalHiddenNodeCount,
            snapshot.MaxRtNeatFunctionalHiddenNodeCount,
            snapshot.AverageRtNeatFunctionalConnectionCount,
            snapshot.MaxRtNeatFunctionalConnectionCount,
            snapshot.AverageRtNeatDisabledConnectionCount,
            snapshot.MaxRtNeatDisabledConnectionCount,
            snapshot.AverageRtNeatLongestPathLength,
            snapshot.MaxRtNeatLongestPathLength,
            resourceCapacity > 0f ? resourceCalories / resourceCapacity : 0f,
            livingEnergy.CreatureEnergy,
            livingEnergy.StoredEnergy,
            livingEnergy.EggEnergy,
            snapshot.TotalResourceCalories,
            snapshot.TotalPlantCalories,
            snapshot.TotalMeatCalories,
            snapshot.TotalFatCalories,
            snapshot.AverageFatRatio,
            snapshot.AverageMassBurdenRatio,
            snapshot.AverageFatSpeedMultiplier,
            snapshot.AverageFatStorageCapacityCalories,
            snapshot.AverageFatStorageEfficiency,
            snapshot.TotalFatStoredCaloriesPerSecond,
            snapshot.TotalFatReleasedCaloriesPerSecond,
            metabolicPace.Average,
            metabolicPace.Minimum,
            metabolicPace.P10,
            metabolicPace.Median,
            metabolicPace.P90,
            metabolicPace.Maximum,
            metabolicPace.StdDev,
            metabolicPace.Low,
            metabolicPace.Normal,
            metabolicPace.High,
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
            Share(snapshot.CreatureContactCreatureCount, snapshot.CreatureCount),
            Share(snapshot.AttackIntentCreatureCount, snapshot.CreatureCount),
            Share(snapshot.AttackIntentWhileTouchingCreatureCount, snapshot.CreatureCount),
            Share(snapshot.AttackNoIntentContactCreatureCount, snapshot.CreatureCount),
            Share(snapshot.RawAttackPositiveCreatureCount, snapshot.CreatureCount),
            Share(snapshot.RawAttackNearGateCreatureCount, snapshot.CreatureCount),
            Share(snapshot.RawAttackNearGateWhileTouchingCreatureCount, snapshot.CreatureCount),
            snapshot.AverageAttackOutput,
            snapshot.AverageTouchingAttackOutput,
            Share(snapshot.GrabIntentCreatureCount, snapshot.CreatureCount),
            Share(snapshot.CanGrabCreatureCount, snapshot.CreatureCount),
            Share(snapshot.GrabIntentWhileCanGrabCreatureCount, snapshot.CreatureCount),
            Share(snapshot.GrabIntentWithoutCanGrabCreatureCount, snapshot.CreatureCount),
            Share(snapshot.HoldingCreatureCount, snapshot.CreatureCount),
            Share(snapshot.GrabbedCreatureCount, snapshot.CreatureCount),
            snapshot.AverageGrabOutput,
            snapshot.AverageCanGrabGrabOutput,
            snapshot.AverageGrabPressure,
            snapshot.AverageGrabStrength,
            Share(snapshot.SoundEmittingCreatureCount, snapshot.CreatureCount),
            Share(snapshot.SoundHeardCreatureCount, snapshot.CreatureCount),
            snapshot.AverageSoundAmplitude,
            snapshot.AverageSoundDensity,
            snapshot.AverageSoundToneClarity,
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
            snapshot.PlantDepletionCount,
            snapshot.PlantLocalDispersalCount,
            snapshot.PlantClusterRelocationCount,
            snapshot.PlantGlobalRelocationCount,
            snapshot.PlantDormancyStartedCount,
            snapshot.PlantDormancyCompletedCount,
            snapshot.PlantPatchOccupiedCellShare,
            snapshot.PlantPatchTopDecileCaloriesShare,
            snapshot.PlantPatchiness,
            snapshot.LocalFertilityCellCount,
            snapshot.AverageLocalFertilityMultiplier,
            snapshot.MinimumLocalFertilityMultiplier,
            snapshot.DepletedLocalFertilityCellShare,
            tail.SnapshotCount,
            tail.StartTick,
            tail.EndTick,
            tail.Seconds,
            tail.AverageCreatures,
            tail.AverageCreatureEnergy,
            tail.AverageLivingStoredEnergy,
            tail.AverageEggEnergy,
            tail.AverageFatRatio,
            tail.AverageMassBurdenRatio,
            tail.AverageFatSpeedMultiplier,
            tail.AverageFatStorageCapacityCalories,
            tail.AverageFatStorageEfficiency,
            tail.FatStoredCaloriesPerSecond,
            tail.FatReleasedCaloriesPerSecond,
            tail.AverageMetabolicPace,
            tail.LowMetabolicPaceCreatures,
            tail.NormalMetabolicPaceCreatures,
            tail.HighMetabolicPaceCreatures,
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
            tail.CreatureContactShare,
            tail.AttackIntentShare,
            tail.AttackIntentTouchingShare,
            tail.AttackNoIntentContactShare,
            tail.RawAttackNearGateTouchingShare,
            tail.AverageAttackOutput,
            tail.AverageTouchingAttackOutput,
            tail.GrabIntentShare,
            tail.CanGrabShare,
            tail.GrabIntentCanGrabShare,
            tail.GrabIntentNoContactShare,
            tail.HoldingShare,
            tail.GrabbedShare,
            tail.AverageGrabOutput,
            tail.AverageCanGrabGrabOutput,
            tail.AverageGrabPressure,
            tail.AverageGrabStrength,
            tail.SoundEmittingShare,
            tail.SoundHeardShare,
            tail.AverageSoundAmplitude,
            tail.AverageSoundDensity,
            tail.AverageSoundToneClarity,
            tail.DeathsPerSecond,
            tail.StarvationDeathsPerSecond,
            tail.InjuryDeathsPerSecond,
            tail.RottenMeatDeathsPerSecond,
            tail.OldAgeDeathsPerSecond,
            tail.CaloriesEatenPerDistance,
            tail.AverageSecondsSinceLastMeal,
            tail.PlantPatchOccupiedCellShare,
            tail.PlantPatchTopDecileCaloriesShare,
            tail.PlantPatchiness,
            tail.AverageLocalFertilityMultiplier,
            tail.MinimumLocalFertilityMultiplier,
            tail.DepletedLocalFertilityCellShare);
    }

    private static float Share(int count, int total)
    {
        return total > 0 ? count / (float)total : 0f;
    }
}

internal readonly record struct ProbeMetabolicPaceSummary(
    float Average,
    float Minimum,
    float P10,
    float Median,
    float P90,
    float Maximum,
    float StdDev,
    int Low,
    int Normal,
    int High)
{
    public static ProbeMetabolicPaceSummary From(WorldState state)
    {
        if (state.Creatures.Count == 0)
        {
            return default;
        }

        var total = 0f;
        var low = 0;
        var normal = 0;
        var high = 0;
        var paces = new float[state.Creatures.Count];
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            var pace = CreatureMetabolism.NormalizePace(genome.MetabolicPace);
            paces[i] = pace;
            total += pace;
            switch (CreatureMetabolism.PaceBand(genome))
            {
                case MetabolicPaceBand.Low:
                    low++;
                    break;
                case MetabolicPaceBand.High:
                    high++;
                    break;
                default:
                    normal++;
                    break;
            }
        }

        Array.Sort(paces);
        var average = total / paces.Length;
        var variance = 0f;
        foreach (var pace in paces)
        {
            var delta = pace - average;
            variance += delta * delta;
        }

        return new ProbeMetabolicPaceSummary(
            average,
            paces[0],
            Quantile(paces, 0.1f),
            Quantile(paces, 0.5f),
            Quantile(paces, 0.9f),
            paces[^1],
            MathF.Sqrt(variance / paces.Length),
            low,
            normal,
            high);
    }

    private static float Quantile(float[] sortedValues, float quantile)
    {
        if (sortedValues.Length == 1)
        {
            return sortedValues[0];
        }

        var position = Math.Clamp(quantile, 0f, 1f) * (sortedValues.Length - 1);
        var lowerIndex = (int)MathF.Floor(position);
        var upperIndex = (int)MathF.Ceiling(position);
        if (lowerIndex == upperIndex)
        {
            return sortedValues[lowerIndex];
        }

        var fraction = position - lowerIndex;
        return sortedValues[lowerIndex] + (sortedValues[upperIndex] - sortedValues[lowerIndex]) * fraction;
    }
}

internal readonly record struct ProbeLivingEnergySummary(float CreatureEnergy, float StoredEnergy, float EggEnergy)
{
    public static ProbeLivingEnergySummary From(WorldState state)
    {
        var creatureEnergy = 0f;
        var fatCalories = 0f;
        foreach (var creature in state.Creatures)
        {
            creatureEnergy += creature.Energy;
            fatCalories += creature.FatCalories;
        }

        var eggEnergy = 0f;
        foreach (var egg in state.Eggs)
        {
            eggEnergy += egg.Energy;
        }

        return new ProbeLivingEnergySummary(creatureEnergy, creatureEnergy + fatCalories, eggEnergy);
    }
}

internal readonly record struct ProbeTailSummary(
    int SnapshotCount,
    long StartTick,
    long EndTick,
    double Seconds,
    float AverageCreatures,
    float AverageCreatureEnergy,
    float AverageLivingStoredEnergy,
    float AverageEggEnergy,
    float AverageFatRatio,
    float AverageMassBurdenRatio,
    float AverageFatSpeedMultiplier,
    float AverageFatStorageCapacityCalories,
    float AverageFatStorageEfficiency,
    float FatStoredCaloriesPerSecond,
    float FatReleasedCaloriesPerSecond,
    float AverageMetabolicPace,
    float LowMetabolicPaceCreatures,
    float NormalMetabolicPaceCreatures,
    float HighMetabolicPaceCreatures,
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
    float CreatureContactShare,
    float AttackIntentShare,
    float AttackIntentTouchingShare,
    float AttackNoIntentContactShare,
    float RawAttackNearGateTouchingShare,
    float AverageAttackOutput,
    float AverageTouchingAttackOutput,
    float GrabIntentShare,
    float CanGrabShare,
    float GrabIntentCanGrabShare,
    float GrabIntentNoContactShare,
    float HoldingShare,
    float GrabbedShare,
    float AverageGrabOutput,
    float AverageCanGrabGrabOutput,
    float AverageGrabPressure,
    float AverageGrabStrength,
    float SoundEmittingShare,
    float SoundHeardShare,
    float AverageSoundAmplitude,
    float AverageSoundDensity,
    float AverageSoundToneClarity,
    float DeathsPerSecond,
    float StarvationDeathsPerSecond,
    float InjuryDeathsPerSecond,
    float RottenMeatDeathsPerSecond,
    float OldAgeDeathsPerSecond,
    float CaloriesEatenPerDistance,
    float AverageSecondsSinceLastMeal,
    float PlantPatchOccupiedCellShare,
    float PlantPatchTopDecileCaloriesShare,
    float PlantPatchiness,
    float AverageLocalFertilityMultiplier,
    float MinimumLocalFertilityMultiplier,
    float DepletedLocalFertilityCellShare)
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
        var averageCreatureEnergy = 0f;
        var averageLivingStoredEnergy = 0f;
        var averageEggEnergy = 0f;
        var averageFatRatio = 0f;
        var averageMassBurdenRatio = 0f;
        var averageFatSpeedMultiplier = 0f;
        var averageFatStorageCapacityCalories = 0f;
        var averageFatStorageEfficiency = 0f;
        var fatStoredCaloriesPerSecond = 0f;
        var fatReleasedCaloriesPerSecond = 0f;
        var averageMetabolicPace = 0f;
        var lowMetabolicPaceCreatures = 0f;
        var normalMetabolicPaceCreatures = 0f;
        var highMetabolicPaceCreatures = 0f;
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
        var creatureContactShare = 0f;
        var attackIntentShare = 0f;
        var attackIntentTouchingShare = 0f;
        var attackNoIntentContactShare = 0f;
        var rawAttackNearGateTouchingShare = 0f;
        var averageAttackOutput = 0f;
        var averageTouchingAttackOutput = 0f;
        var grabIntentShare = 0f;
        var canGrabShare = 0f;
        var grabIntentCanGrabShare = 0f;
        var grabIntentNoContactShare = 0f;
        var holdingShare = 0f;
        var grabbedShare = 0f;
        var averageGrabOutput = 0f;
        var averageCanGrabGrabOutput = 0f;
        var averageGrabPressure = 0f;
        var averageGrabStrength = 0f;
        var soundEmittingShare = 0f;
        var soundHeardShare = 0f;
        var averageSoundAmplitude = 0f;
        var averageSoundDensity = 0f;
        var averageSoundToneClarity = 0f;
        var caloriesEatenPerDistance = 0f;
        var averageSecondsSinceLastMeal = 0f;
        var plantPatchOccupiedCellShare = 0f;
        var plantPatchTopDecileCaloriesShare = 0f;
        var plantPatchiness = 0f;
        var averageLocalFertilityMultiplier = 0f;
        var minimumLocalFertilityMultiplier = 0f;
        var depletedLocalFertilityCellShare = 0f;

        for (var i = startIndex; i < snapshots.Count; i++)
        {
            var snapshot = snapshots[i];
            averageCreatures += snapshot.CreatureCount;
            averageCreatureEnergy += snapshot.TotalCreatureEnergy;
            averageLivingStoredEnergy += snapshot.TotalCreatureEnergy + snapshot.TotalFatCalories;
            averageEggEnergy += snapshot.TotalEggEnergy;
            averageFatRatio += snapshot.AverageFatRatio;
            averageMassBurdenRatio += snapshot.AverageMassBurdenRatio;
            averageFatSpeedMultiplier += snapshot.AverageFatSpeedMultiplier;
            averageFatStorageCapacityCalories += snapshot.AverageFatStorageCapacityCalories;
            averageFatStorageEfficiency += snapshot.AverageFatStorageEfficiency;
            fatStoredCaloriesPerSecond += snapshot.TotalFatStoredCaloriesPerSecond;
            fatReleasedCaloriesPerSecond += snapshot.TotalFatReleasedCaloriesPerSecond;
            averageMetabolicPace += snapshot.AverageMetabolicPace;
            lowMetabolicPaceCreatures += snapshot.LowMetabolicPaceCreatureCount;
            normalMetabolicPaceCreatures += snapshot.NormalMetabolicPaceCreatureCount;
            highMetabolicPaceCreatures += snapshot.HighMetabolicPaceCreatureCount;
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
            creatureContactShare += Share(snapshot.CreatureContactCreatureCount, snapshot.CreatureCount);
            attackIntentShare += Share(snapshot.AttackIntentCreatureCount, snapshot.CreatureCount);
            attackIntentTouchingShare += Share(snapshot.AttackIntentWhileTouchingCreatureCount, snapshot.CreatureCount);
            attackNoIntentContactShare += Share(snapshot.AttackNoIntentContactCreatureCount, snapshot.CreatureCount);
            rawAttackNearGateTouchingShare += Share(snapshot.RawAttackNearGateWhileTouchingCreatureCount, snapshot.CreatureCount);
            averageAttackOutput += snapshot.AverageAttackOutput;
            averageTouchingAttackOutput += snapshot.AverageTouchingAttackOutput;
            grabIntentShare += Share(snapshot.GrabIntentCreatureCount, snapshot.CreatureCount);
            canGrabShare += Share(snapshot.CanGrabCreatureCount, snapshot.CreatureCount);
            grabIntentCanGrabShare += Share(snapshot.GrabIntentWhileCanGrabCreatureCount, snapshot.CreatureCount);
            grabIntentNoContactShare += Share(snapshot.GrabIntentWithoutCanGrabCreatureCount, snapshot.CreatureCount);
            holdingShare += Share(snapshot.HoldingCreatureCount, snapshot.CreatureCount);
            grabbedShare += Share(snapshot.GrabbedCreatureCount, snapshot.CreatureCount);
            averageGrabOutput += snapshot.AverageGrabOutput;
            averageCanGrabGrabOutput += snapshot.AverageCanGrabGrabOutput;
            averageGrabPressure += snapshot.AverageGrabPressure;
            averageGrabStrength += snapshot.AverageGrabStrength;
            soundEmittingShare += Share(snapshot.SoundEmittingCreatureCount, snapshot.CreatureCount);
            soundHeardShare += Share(snapshot.SoundHeardCreatureCount, snapshot.CreatureCount);
            averageSoundAmplitude += snapshot.AverageSoundAmplitude;
            averageSoundDensity += snapshot.AverageSoundDensity;
            averageSoundToneClarity += snapshot.AverageSoundToneClarity;
            caloriesEatenPerDistance += snapshot.CaloriesEatenPerDistance;
            averageSecondsSinceLastMeal += snapshot.AverageSecondsSinceLastMeal;
            plantPatchOccupiedCellShare += snapshot.PlantPatchOccupiedCellShare;
            plantPatchTopDecileCaloriesShare += snapshot.PlantPatchTopDecileCaloriesShare;
            plantPatchiness += snapshot.PlantPatchiness;
            averageLocalFertilityMultiplier += snapshot.AverageLocalFertilityMultiplier;
            minimumLocalFertilityMultiplier += snapshot.MinimumLocalFertilityMultiplier;
            depletedLocalFertilityCellShare += snapshot.DepletedLocalFertilityCellShare;
        }

        var divisor = tailCount;
        var deathRateDivisor = seconds > 0.0 ? (float)seconds : 0f;
        return new ProbeTailSummary(
            tailCount,
            first.Tick,
            last.Tick,
            seconds,
            averageCreatures / divisor,
            averageCreatureEnergy / divisor,
            averageLivingStoredEnergy / divisor,
            averageEggEnergy / divisor,
            averageFatRatio / divisor,
            averageMassBurdenRatio / divisor,
            averageFatSpeedMultiplier / divisor,
            averageFatStorageCapacityCalories / divisor,
            averageFatStorageEfficiency / divisor,
            fatStoredCaloriesPerSecond / divisor,
            fatReleasedCaloriesPerSecond / divisor,
            averageMetabolicPace / divisor,
            lowMetabolicPaceCreatures / divisor,
            normalMetabolicPaceCreatures / divisor,
            highMetabolicPaceCreatures / divisor,
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
            creatureContactShare / divisor,
            attackIntentShare / divisor,
            attackIntentTouchingShare / divisor,
            attackNoIntentContactShare / divisor,
            rawAttackNearGateTouchingShare / divisor,
            averageAttackOutput / divisor,
            averageTouchingAttackOutput / divisor,
            grabIntentShare / divisor,
            canGrabShare / divisor,
            grabIntentCanGrabShare / divisor,
            grabIntentNoContactShare / divisor,
            holdingShare / divisor,
            grabbedShare / divisor,
            averageGrabOutput / divisor,
            averageCanGrabGrabOutput / divisor,
            averageGrabPressure / divisor,
            averageGrabStrength / divisor,
            soundEmittingShare / divisor,
            soundHeardShare / divisor,
            averageSoundAmplitude / divisor,
            averageSoundDensity / divisor,
            averageSoundToneClarity / divisor,
            Rate(last.CreatureDeathCount - first.CreatureDeathCount, deathRateDivisor),
            Rate(last.StarvationDeathCount - first.StarvationDeathCount, deathRateDivisor),
            Rate(last.InjuryDeathCount - first.InjuryDeathCount, deathRateDivisor),
            Rate(last.RottenMeatDeathCount - first.RottenMeatDeathCount, deathRateDivisor),
            Rate(last.OldAgeDeathCount - first.OldAgeDeathCount, deathRateDivisor),
            caloriesEatenPerDistance / divisor,
            averageSecondsSinceLastMeal / divisor,
            plantPatchOccupiedCellShare / divisor,
            plantPatchTopDecileCaloriesShare / divisor,
            plantPatchiness / divisor,
            averageLocalFertilityMultiplier / divisor,
            minimumLocalFertilityMultiplier / divisor,
            depletedLocalFertilityCellShare / divisor);
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
        writer.WriteLine("scenario,scenario_path,variant,variant_overrides,seed,status,requested_ticks,final_tick,simulated_seconds,wall_seconds,ticks_per_second,pipeline,initial_brain,initial_creatures,initial_resources,resource_density_per_million,resource_cluster_strength,resource_cluster_radius,final_creatures,final_eggs,final_resources,final_plants,final_meat,births,eggs_laid,eggs_hatched,egg_deaths,egg_predation_deaths,deaths,starvation_deaths,injury_deaths,rotten_meat_deaths,old_age_deaths,max_generation,rtneat_brains,rtneat_brain_share,avg_rtneat_hidden_nodes,max_rtneat_hidden_nodes,avg_rtneat_connections,max_rtneat_connections,avg_rtneat_enabled_connections,max_rtneat_enabled_connections,avg_rtneat_functional_hidden_nodes,max_rtneat_functional_hidden_nodes,avg_rtneat_functional_connections,max_rtneat_functional_connections,avg_rtneat_disabled_connections,max_rtneat_disabled_connections,avg_rtneat_longest_path,max_rtneat_longest_path,final_resource_ratio,total_creature_energy,total_living_stored_energy,total_egg_energy,total_resource_calories,total_plant_calories,total_meat_calories,total_fat_calories,avg_fat_ratio,avg_mass_burden,avg_fat_speed_multiplier,avg_fat_storage_capacity,avg_fat_storage_efficiency,fat_stored_calories_per_second,fat_released_calories_per_second,avg_metabolic_pace,min_metabolic_pace,p10_metabolic_pace,median_metabolic_pace,p90_metabolic_pace,max_metabolic_pace,metabolic_pace_stddev,low_metabolic_pace_creatures,normal_metabolic_pace_creatures,high_metabolic_pace_creatures,barren_creatures,sparse_creatures,grassland_creatures,rich_creatures,avg_biome_movement_cost,avg_biome_basal_cost,avg_biome_speed,barren_calories_eaten_per_second,sparse_calories_eaten_per_second,grassland_calories_eaten_per_second,rich_calories_eaten_per_second,barren_deaths,sparse_deaths,grassland_deaths,rich_deaths,current_east_progress_share,run_east_progress_share,middle_region_creatures,right_region_creatures,behavior_movement_style,behavior_search_tendency,behavior_ecotype,behavior_terrain_response,behavior_rotten_meat_response,behavior_fresh_meat_preference_score,behavior_rotten_scent_avoidance_score,food_detected_share,plant_detected_share,meat_detected_share,fresh_meat_detected_share,stale_meat_detected_share,stale_meat_avoided_share,avg_visible_meat_freshness,meat_scent_detected_share,rotten_meat_scent_detected_share,avg_rotten_meat_scent_density,creature_detected_share,food_contact_share,eating_share,attacking_share,visible_food_density,calories_eaten_per_second,meat_calories_eaten_share,fresh_kill_calories_eaten_share,avg_meat_freshness,avg_carrion_adaptation,fresh_meat_calories_eaten_share,stale_meat_calories_eaten_share,fresh_meat_calories_eaten_per_second,stale_meat_calories_eaten_per_second,rotten_meat_damage_per_second,rotten_meat_damaged_share,meat_digested_energy_share,calories_eaten_per_distance,calories_digested_per_distance,calories_eaten_per_food_vision_event,avg_seconds_since_last_meal,avg_distance_since_last_meal,plant_depletions,plant_local_dispersals,plant_cluster_relocations,plant_global_relocations,plant_dormancy_started,plant_dormancy_completed,plant_patch_occupied_cell_share,plant_patch_top_decile_calories_share,plant_patchiness,local_fertility_cells,avg_local_fertility_multiplier,min_local_fertility_multiplier,depleted_local_fertility_cell_share,tail_snapshot_count,tail_start_tick,tail_end_tick,tail_seconds,tail_avg_creatures,tail_avg_creature_energy,tail_avg_living_stored_energy,tail_avg_egg_energy,tail_avg_fat_ratio,tail_avg_mass_burden,tail_avg_fat_speed_multiplier,tail_avg_fat_storage_capacity,tail_avg_fat_storage_efficiency,tail_fat_stored_calories_per_second,tail_fat_released_calories_per_second,tail_avg_metabolic_pace,tail_low_metabolic_pace_creatures,tail_normal_metabolic_pace_creatures,tail_high_metabolic_pace_creatures,tail_avg_dietary_adaptation,tail_avg_carrion_adaptation,tail_fresh_meat_detected_share,tail_stale_meat_detected_share,tail_stale_meat_avoided_share,tail_avg_visible_meat_freshness,tail_rotten_meat_scent_detected_share,tail_avg_rotten_meat_scent_density,tail_meat_calories_eaten_share,tail_fresh_kill_calories_eaten_share,tail_avg_meat_freshness,tail_fresh_meat_calories_eaten_share,tail_stale_meat_calories_eaten_share,tail_rotten_meat_damage_per_second,tail_rotten_meat_damaged_share,tail_meat_digested_energy_share,tail_attacking_share,tail_deaths_per_second,tail_starvation_deaths_per_second,tail_injury_deaths_per_second,tail_rotten_meat_deaths_per_second,tail_old_age_deaths_per_second,tail_calories_eaten_per_distance,tail_avg_seconds_since_last_meal,tail_plant_patch_occupied_cell_share,tail_plant_patch_top_decile_calories_share,tail_plant_patchiness,tail_avg_local_fertility_multiplier,tail_min_local_fertility_multiplier,tail_depleted_local_fertility_cell_share,creature_contact_share,attack_intent_share,attack_intent_touching_share,attack_no_intent_contact_share,raw_attack_positive_share,raw_attack_near_gate_share,raw_attack_near_gate_touching_share,avg_attack_output,avg_touching_attack_output,tail_creature_contact_share,tail_attack_intent_share,tail_attack_intent_touching_share,tail_attack_no_intent_contact_share,tail_raw_attack_near_gate_touching_share,tail_avg_attack_output,tail_avg_touching_attack_output,grab_intent_share,can_grab_share,grab_intent_can_grab_share,grab_intent_no_contact_share,holding_share,grabbed_share,avg_grab_output,avg_can_grab_grab_output,avg_grab_pressure,avg_grab_strength,sound_emitting_share,sound_heard_share,avg_sound_amplitude,avg_sound_density,avg_sound_tone_clarity,tail_grab_intent_share,tail_can_grab_share,tail_grab_intent_can_grab_share,tail_grab_intent_no_contact_share,tail_holding_share,tail_grabbed_share,tail_avg_grab_output,tail_avg_can_grab_grab_output,tail_avg_grab_pressure,tail_avg_grab_strength,tail_sound_emitting_share,tail_sound_heard_share,tail_avg_sound_amplitude,tail_avg_sound_density,tail_avg_sound_tone_clarity");

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
                result.RottenMeatDeaths.ToString(CultureInfo.InvariantCulture),
                result.OldAgeDeaths.ToString(CultureInfo.InvariantCulture),
                result.MaxGeneration.ToString(CultureInfo.InvariantCulture),
                result.RtNeatBrainCount.ToString(CultureInfo.InvariantCulture),
                Format(result.RtNeatBrainShare),
                Format(result.AverageRtNeatHiddenNodeCount),
                result.MaxRtNeatHiddenNodeCount.ToString(CultureInfo.InvariantCulture),
                Format(result.AverageRtNeatConnectionCount),
                result.MaxRtNeatConnectionCount.ToString(CultureInfo.InvariantCulture),
                Format(result.AverageRtNeatEnabledConnectionCount),
                result.MaxRtNeatEnabledConnectionCount.ToString(CultureInfo.InvariantCulture),
                Format(result.AverageRtNeatFunctionalHiddenNodeCount),
                result.MaxRtNeatFunctionalHiddenNodeCount.ToString(CultureInfo.InvariantCulture),
                Format(result.AverageRtNeatFunctionalConnectionCount),
                result.MaxRtNeatFunctionalConnectionCount.ToString(CultureInfo.InvariantCulture),
                Format(result.AverageRtNeatDisabledConnectionCount),
                result.MaxRtNeatDisabledConnectionCount.ToString(CultureInfo.InvariantCulture),
                Format(result.AverageRtNeatLongestPathLength),
                result.MaxRtNeatLongestPathLength.ToString(CultureInfo.InvariantCulture),
                Format(result.FinalResourceRatio),
                Format(result.TotalCreatureEnergy),
                Format(result.TotalLivingStoredEnergy),
                Format(result.TotalEggEnergy),
                Format(result.TotalResourceCalories),
                Format(result.TotalPlantCalories),
                Format(result.TotalMeatCalories),
                Format(result.TotalFatCalories),
                Format(result.AverageFatRatio),
                Format(result.AverageMassBurdenRatio),
                Format(result.AverageFatSpeedMultiplier),
                Format(result.AverageFatStorageCapacityCalories),
                Format(result.AverageFatStorageEfficiency),
                Format(result.FatStoredCaloriesPerSecond),
                Format(result.FatReleasedCaloriesPerSecond),
                Format(result.AverageMetabolicPace),
                Format(result.MinimumMetabolicPace),
                Format(result.P10MetabolicPace),
                Format(result.MedianMetabolicPace),
                Format(result.P90MetabolicPace),
                Format(result.MaximumMetabolicPace),
                Format(result.MetabolicPaceStdDev),
                result.LowMetabolicPaceCreatureCount.ToString(CultureInfo.InvariantCulture),
                result.NormalMetabolicPaceCreatureCount.ToString(CultureInfo.InvariantCulture),
                result.HighMetabolicPaceCreatureCount.ToString(CultureInfo.InvariantCulture),
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
                result.PlantDepletions.ToString(CultureInfo.InvariantCulture),
                result.PlantLocalDispersals.ToString(CultureInfo.InvariantCulture),
                result.PlantClusterRelocations.ToString(CultureInfo.InvariantCulture),
                result.PlantGlobalRelocations.ToString(CultureInfo.InvariantCulture),
                result.PlantDormancyStarted.ToString(CultureInfo.InvariantCulture),
                result.PlantDormancyCompleted.ToString(CultureInfo.InvariantCulture),
                Format(result.PlantPatchOccupiedCellShare),
                Format(result.PlantPatchTopDecileCaloriesShare),
                Format(result.PlantPatchiness),
                result.LocalFertilityCellCount.ToString(CultureInfo.InvariantCulture),
                Format(result.AverageLocalFertilityMultiplier),
                Format(result.MinimumLocalFertilityMultiplier),
                Format(result.DepletedLocalFertilityCellShare),
                result.TailSnapshotCount.ToString(CultureInfo.InvariantCulture),
                result.TailStartTick.ToString(CultureInfo.InvariantCulture),
                result.TailEndTick.ToString(CultureInfo.InvariantCulture),
                Format(result.TailSeconds),
                Format(result.TailAverageCreatures),
                Format(result.TailAverageCreatureEnergy),
                Format(result.TailAverageLivingStoredEnergy),
                Format(result.TailAverageEggEnergy),
                Format(result.TailAverageFatRatio),
                Format(result.TailAverageMassBurdenRatio),
                Format(result.TailAverageFatSpeedMultiplier),
                Format(result.TailAverageFatStorageCapacityCalories),
                Format(result.TailAverageFatStorageEfficiency),
                Format(result.TailFatStoredCaloriesPerSecond),
                Format(result.TailFatReleasedCaloriesPerSecond),
                Format(result.TailAverageMetabolicPace),
                Format(result.TailLowMetabolicPaceCreatures),
                Format(result.TailNormalMetabolicPaceCreatures),
                Format(result.TailHighMetabolicPaceCreatures),
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
                Format(result.TailRottenMeatDeathsPerSecond),
                Format(result.TailOldAgeDeathsPerSecond),
                Format(result.TailCaloriesEatenPerDistance),
                Format(result.TailAverageSecondsSinceLastMeal),
                Format(result.TailPlantPatchOccupiedCellShare),
                Format(result.TailPlantPatchTopDecileCaloriesShare),
                Format(result.TailPlantPatchiness),
                Format(result.TailAverageLocalFertilityMultiplier),
                Format(result.TailMinimumLocalFertilityMultiplier),
                Format(result.TailDepletedLocalFertilityCellShare),
                Format(result.CreatureContactShare),
                Format(result.AttackIntentShare),
                Format(result.AttackIntentTouchingShare),
                Format(result.AttackNoIntentContactShare),
                Format(result.RawAttackPositiveShare),
                Format(result.RawAttackNearGateShare),
                Format(result.RawAttackNearGateTouchingShare),
                Format(result.AverageAttackOutput),
                Format(result.AverageTouchingAttackOutput),
                Format(result.TailCreatureContactShare),
                Format(result.TailAttackIntentShare),
                Format(result.TailAttackIntentTouchingShare),
                Format(result.TailAttackNoIntentContactShare),
                Format(result.TailRawAttackNearGateTouchingShare),
                Format(result.TailAverageAttackOutput),
                Format(result.TailAverageTouchingAttackOutput),
                Format(result.GrabIntentShare),
                Format(result.CanGrabShare),
                Format(result.GrabIntentCanGrabShare),
                Format(result.GrabIntentNoContactShare),
                Format(result.HoldingShare),
                Format(result.GrabbedShare),
                Format(result.AverageGrabOutput),
                Format(result.AverageCanGrabGrabOutput),
                Format(result.AverageGrabPressure),
                Format(result.AverageGrabStrength),
                Format(result.SoundEmittingShare),
                Format(result.SoundHeardShare),
                Format(result.AverageSoundAmplitude),
                Format(result.AverageSoundDensity),
                Format(result.AverageSoundToneClarity),
                Format(result.TailGrabIntentShare),
                Format(result.TailCanGrabShare),
                Format(result.TailGrabIntentCanGrabShare),
                Format(result.TailGrabIntentNoContactShare),
                Format(result.TailHoldingShare),
                Format(result.TailGrabbedShare),
                Format(result.TailAverageGrabOutput),
                Format(result.TailAverageCanGrabGrabOutput),
                Format(result.TailAverageGrabPressure),
                Format(result.TailAverageGrabStrength),
                Format(result.TailSoundEmittingShare),
                Format(result.TailSoundHeardShare),
                Format(result.TailAverageSoundAmplitude),
                Format(result.TailAverageSoundDensity),
                Format(result.TailAverageSoundToneClarity)));
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
        WriteMetric(writer, "Snapshot interval", FormatSnapshotIntervals(results));
        WriteMetric(writer, "Stop on extinction", options.ProbeStopOnExtinction ? "Yes" : "No");
        WriteMetric(writer, "Max population stop", options.ProbeMaxPopulation?.ToString(CultureInfo.InvariantCulture) ?? "Off");
        writer.WriteLine("</div></section>");

        writer.WriteLine("<section><h2>Scenario Summary</h2><div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Scenario</th><th>Variant</th><th>Overrides</th><th>Runs</th><th>Status</th><th>Avg final</th><th>Range</th><th>Tail pop</th><th>Energy</th><th>Stored</th><th>Tail energy</th><th>Tail stored</th><th>Pace</th><th>Pace p10-p90</th><th>Pace min-max</th><th>Pace sd</th><th>L/N/H</th><th>Tail pace</th><th>Tail L/N/H</th><th>Tail fat</th><th>Tail burden</th><th>Tail fat speed</th><th>Tail fat cap</th><th>Tail fat eff</th><th>Tail fat in</th><th>Tail fat out</th><th>Avg eggs</th><th>Avg deaths</th><th>Avg starved</th><th>Avg injury</th><th>Avg rotten</th><th>Avg old age</th><th>Graph share</th><th>Graph hidden</th><th>Graph conn</th><th>Graph enabled</th><th>Graph functional</th><th>Graph disabled</th><th>Graph path</th><th>East max</th><th>Right now</th><th>Biome speed</th><th>Rough kcal/s</th><th>Rich kcal/s</th><th>Rough deaths</th><th>Terrain assay</th><th>Rot assay</th><th>Fresh pref</th><th>Rot avoid</th><th>Final meat</th><th>Tail meat</th><th>Tail fresh</th><th>Tail stale</th><th>Tail stale seen</th><th>Tail stale avoided</th><th>Tail rot scent</th><th>Tail rot dmg/s</th><th>Tail diet</th><th>Tail carrion</th><th>Tail attack</th><th>Tail contact</th><th>Tail intent</th><th>Tail touch intent</th><th>Tail near touch</th><th>Tail raw</th><th>Tail can grab</th><th>Tail grab</th><th>Tail grab+touch</th><th>Tail off-touch grab</th><th>Tail held</th><th>Tail sound</th><th>Tail heard</th><th>Tail deaths/s</th><th>Tail old age/s</th><th>kcal/distance</th><th>Plant dep</th><th>Tail patch</th><th>Tail avg fert</th><th>Tail min fert</th><th>Tail dep fert</th><th>Ticks/s</th></tr></thead><tbody>");
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
                $"<td>{Html(group.Average(result => result.TotalCreatureEnergy).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.TotalLivingStoredEnergy).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.TailAverageCreatureEnergy).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.TailAverageLivingStoredEnergy).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.AverageMetabolicPace).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatRange(group.Average(result => result.P10MetabolicPace), group.Average(result => result.P90MetabolicPace)))}</td>" +
                $"<td>{Html(FormatRange(group.Average(result => result.MinimumMetabolicPace), group.Average(result => result.MaximumMetabolicPace)))}</td>" +
                $"<td>{Html(group.Average(result => result.MetabolicPaceStdDev).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPaceCounts(group.Average(result => result.LowMetabolicPaceCreatureCount), group.Average(result => result.NormalMetabolicPaceCreatureCount), group.Average(result => result.HighMetabolicPaceCreatureCount)))}</td>" +
                $"<td>{Html(group.Average(result => result.TailAverageMetabolicPace).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPaceCounts(group.Average(result => result.TailLowMetabolicPaceCreatures), group.Average(result => result.TailNormalMetabolicPaceCreatures), group.Average(result => result.TailHighMetabolicPaceCreatures)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailAverageFatRatio)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailAverageMassBurdenRatio)))}</td>" +
                $"<td>{Html($"{group.Average(result => result.TailAverageFatSpeedMultiplier):0.###}x")}</td>" +
                $"<td>{Html(group.Average(result => result.TailAverageFatStorageCapacityCalories).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailAverageFatStorageEfficiency)))}</td>" +
                $"<td>{Html(group.Average(result => result.TailFatStoredCaloriesPerSecond).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.TailFatReleasedCaloriesPerSecond).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.FinalEggs).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.Deaths).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.StarvationDeaths).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.InjuryDeaths).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.RottenMeatDeaths).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.OldAgeDeaths).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.RtNeatBrainShare)))}</td>" +
                $"<td>{Html(group.Average(result => result.AverageRtNeatHiddenNodeCount).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.AverageRtNeatConnectionCount).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.AverageRtNeatEnabledConnectionCount).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html($"{group.Average(result => result.AverageRtNeatFunctionalHiddenNodeCount):0.###}/{group.Average(result => result.AverageRtNeatFunctionalConnectionCount):0.###}")}</td>" +
                $"<td>{Html(group.Average(result => result.AverageRtNeatDisabledConnectionCount).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.AverageRtNeatLongestPathLength).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
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
                $"<td>{Html(FormatPercent(group.Average(result => result.TailCreatureContactShare)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailAttackIntentShare)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailAttackIntentTouchingShare)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailRawAttackNearGateTouchingShare)))}</td>" +
                $"<td>{Html(group.Average(result => result.TailAverageAttackOutput).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailCanGrabShare)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailGrabIntentShare)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailGrabIntentCanGrabShare)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailGrabIntentNoContactShare)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailHoldingShare)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailSoundEmittingShare)))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailSoundHeardShare)))}</td>" +
                $"<td>{Html(group.Average(result => result.TailDeathsPerSecond).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.TailOldAgeDeathsPerSecond).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.CaloriesEatenPerDistance).ToString("0.####", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.PlantDepletions).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.TailPlantPatchiness).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.TailAverageLocalFertilityMultiplier).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(group.Average(result => result.TailMinimumLocalFertilityMultiplier).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(group.Average(result => result.TailDepletedLocalFertilityCellShare)))}</td>" +
                $"<td>{Html(group.Average(result => result.TicksPerSecond).ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div></section>");

        writer.WriteLine("<section><h2>Run Rows</h2><div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Scenario</th><th>Variant</th><th>Seed</th><th>Status</th><th>Tick</th><th>Wall</th><th>Ticks/s</th><th>Final pop</th><th>Tail pop</th><th>Energy</th><th>Stored</th><th>Tail energy</th><th>Tail stored</th><th>Pace</th><th>Pace p10-p90</th><th>Pace min-max</th><th>Pace sd</th><th>L/N/H</th><th>Tail pace</th><th>Tail L/N/H</th><th>Tail fat</th><th>Tail burden</th><th>Tail fat speed</th><th>Tail fat cap</th><th>Tail fat eff</th><th>Tail fat in</th><th>Tail fat out</th><th>Eggs</th><th>Deaths</th><th>Starved</th><th>Injury</th><th>Rotten</th><th>Old Age</th><th>Max gen</th><th>Graph share</th><th>Graph hidden</th><th>Graph conn</th><th>Graph enabled</th><th>Graph functional</th><th>Graph disabled</th><th>Graph path</th><th>East now</th><th>East max</th><th>Middle</th><th>Right</th><th>Biome speed</th><th>Rough kcal/s</th><th>Rich kcal/s</th><th>Rough deaths</th><th>Terrain assay</th><th>Rot assay</th><th>Fresh pref</th><th>Rot avoid</th><th>Tail window</th><th>Food seen</th><th>Final meat</th><th>Tail meat</th><th>Tail fresh</th><th>Tail stale</th><th>Tail stale seen</th><th>Tail stale avoided</th><th>Tail rot scent</th><th>Tail rot dmg/s</th><th>Tail diet</th><th>Tail carrion</th><th>Tail attack</th><th>Tail contact</th><th>Tail intent</th><th>Tail touch intent</th><th>Tail near touch</th><th>Tail raw</th><th>Tail can grab</th><th>Tail grab</th><th>Tail grab+touch</th><th>Tail off-touch grab</th><th>Tail held</th><th>Tail sound</th><th>Tail heard</th><th>Tail deaths/s</th><th>Tail old age/s</th><th>kcal/distance</th><th>Plant dep</th><th>Patch</th><th>Avg fert</th><th>Min fert</th><th>Dep fert</th></tr></thead><tbody>");
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
                $"<td>{Html(result.TotalCreatureEnergy.ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.TotalLivingStoredEnergy.ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.TailAverageCreatureEnergy.ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.TailAverageLivingStoredEnergy.ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.AverageMetabolicPace.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatRange(result.P10MetabolicPace, result.P90MetabolicPace))}</td>" +
                $"<td>{Html(FormatRange(result.MinimumMetabolicPace, result.MaximumMetabolicPace))}</td>" +
                $"<td>{Html(result.MetabolicPaceStdDev.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPaceCounts(result.LowMetabolicPaceCreatureCount, result.NormalMetabolicPaceCreatureCount, result.HighMetabolicPaceCreatureCount))}</td>" +
                $"<td>{Html(result.TailAverageMetabolicPace.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPaceCounts(result.TailLowMetabolicPaceCreatures, result.TailNormalMetabolicPaceCreatures, result.TailHighMetabolicPaceCreatures))}</td>" +
                $"<td>{Html(FormatPercent(result.TailAverageFatRatio))}</td>" +
                $"<td>{Html(FormatPercent(result.TailAverageMassBurdenRatio))}</td>" +
                $"<td>{Html($"{result.TailAverageFatSpeedMultiplier:0.###}x")}</td>" +
                $"<td>{Html(result.TailAverageFatStorageCapacityCalories.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(result.TailAverageFatStorageEfficiency))}</td>" +
                $"<td>{Html(result.TailFatStoredCaloriesPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.TailFatReleasedCaloriesPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.FinalEggs)}</td>" +
                $"<td>{Html(result.Deaths)}</td>" +
                $"<td>{Html(result.StarvationDeaths)}</td>" +
                $"<td>{Html(result.InjuryDeaths)}</td>" +
                $"<td>{Html(result.RottenMeatDeaths)}</td>" +
                $"<td>{Html(result.OldAgeDeaths)}</td>" +
                $"<td>{Html(result.MaxGeneration)}</td>" +
                $"<td>{Html(FormatPercent(result.RtNeatBrainShare))}</td>" +
                $"<td>{Html(result.AverageRtNeatHiddenNodeCount.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.AverageRtNeatConnectionCount.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.AverageRtNeatEnabledConnectionCount.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html($"{result.AverageRtNeatFunctionalHiddenNodeCount:0.###}/{result.AverageRtNeatFunctionalConnectionCount:0.###}")}</td>" +
                $"<td>{Html(result.AverageRtNeatDisabledConnectionCount.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.AverageRtNeatLongestPathLength.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
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
                $"<td>{Html(FormatPercent(result.TailCreatureContactShare))}</td>" +
                $"<td>{Html(FormatPercent(result.TailAttackIntentShare))}</td>" +
                $"<td>{Html(FormatPercent(result.TailAttackIntentTouchingShare))}</td>" +
                $"<td>{Html(FormatPercent(result.TailRawAttackNearGateTouchingShare))}</td>" +
                $"<td>{Html(result.TailAverageAttackOutput.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(result.TailCanGrabShare))}</td>" +
                $"<td>{Html(FormatPercent(result.TailGrabIntentShare))}</td>" +
                $"<td>{Html(FormatPercent(result.TailGrabIntentCanGrabShare))}</td>" +
                $"<td>{Html(FormatPercent(result.TailGrabIntentNoContactShare))}</td>" +
                $"<td>{Html(FormatPercent(result.TailHoldingShare))}</td>" +
                $"<td>{Html(FormatPercent(result.TailSoundEmittingShare))}</td>" +
                $"<td>{Html(FormatPercent(result.TailSoundHeardShare))}</td>" +
                $"<td>{Html(result.TailDeathsPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.TailOldAgeDeathsPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.CaloriesEatenPerDistance.ToString("0.####", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.PlantDepletions)}</td>" +
                $"<td>{Html(result.TailPlantPatchiness.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.TailAverageLocalFertilityMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.TailMinimumLocalFertilityMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(result.TailDepletedLocalFertilityCellShare))}</td>" +
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

    private static string FormatSnapshotIntervals(IEnumerable<ProbeRunResult> results)
    {
        var intervals = results
            .Select(result => result.SnapshotIntervalTicks)
            .Distinct()
            .Order()
            .ToArray();
        return intervals.Length == 0
            ? "-"
            : string.Join(", ", intervals.Select(interval => interval.ToString(CultureInfo.InvariantCulture)));
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

    private static string FormatPaceCounts(double low, double normal, double high)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{low:0.#}/{normal:0.#}/{high:0.#}");
    }

    private static string FormatRange(double minimum, double maximum)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{minimum:0.###}-{maximum:0.###}");
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
        writer.WriteLine("tick,elapsed_seconds,season_phase,season_fertility_multiplier,creatures,eggs,resources,plant_resources,meat_resources,dormant_plant_resources,total_dormant_plant_seconds_remaining,avg_dormant_plant_seconds_remaining,plant_patch_occupied_cell_share,plant_patch_top_decile_calories_share,plant_patchiness,local_fertility_cells,avg_local_fertility_multiplier,min_local_fertility_multiplier,depleted_local_fertility_cell_share,genomes,brains,avg_brain_hidden_nodes,max_brain_hidden_nodes,avg_hidden_input_weight_magnitude,avg_hidden_output_weight_magnitude,active_hidden_output_share,rtneat_brains,rtneat_brain_share,avg_rtneat_hidden_nodes,max_rtneat_hidden_nodes,avg_rtneat_connections,max_rtneat_connections,avg_rtneat_enabled_connections,max_rtneat_enabled_connections,avg_rtneat_functional_hidden_nodes,max_rtneat_functional_hidden_nodes,avg_rtneat_functional_connections,max_rtneat_functional_connections,avg_rtneat_disabled_connections,max_rtneat_disabled_connections,avg_rtneat_longest_path,max_rtneat_longest_path,max_generation,total_creature_energy,total_fat_calories,total_egg_energy,total_egg_health,total_resource_calories,total_plant_calories,tender_plant_type_resources,rich_plant_type_resources,tough_plant_type_resources,tender_plant_type_calories,rich_plant_type_calories,tough_plant_type_calories,total_meat_calories,barren_creatures,barren_creature_share,sparse_creatures,sparse_creature_share,grassland_creatures,grassland_creature_share,rich_creatures,rich_creature_share,forest_creatures,forest_creature_share,wetland_creatures,wetland_creature_share,tundra_creatures,tundra_creature_share,highland_creatures,highland_creature_share,avg_biome_movement_cost,avg_biome_basal_cost,avg_biome_speed,obstacle_blocked_creatures,obstacle_blocked_share,obstacle_sensed_creatures,obstacle_sensed_share,avg_forward_obstacle,avg_left_obstacle,avg_right_obstacle,barren_plant_calories,sparse_plant_calories,grassland_plant_calories,rich_plant_calories,forest_plant_calories,wetland_plant_calories,tundra_plant_calories,highland_plant_calories,barren_meat_calories,sparse_meat_calories,grassland_meat_calories,rich_meat_calories,forest_meat_calories,wetland_meat_calories,tundra_meat_calories,highland_meat_calories,barren_calories_eaten_per_second,sparse_calories_eaten_per_second,grassland_calories_eaten_per_second,rich_calories_eaten_per_second,forest_calories_eaten_per_second,wetland_calories_eaten_per_second,tundra_calories_eaten_per_second,highland_calories_eaten_per_second,barren_deaths,sparse_deaths,grassland_deaths,rich_deaths,forest_deaths,wetland_deaths,tundra_deaths,highland_deaths,avg_creature_x,max_creature_x,avg_max_creature_x_reached,max_creature_x_reached,run_max_creature_x_reached,current_east_progress_share,run_east_progress_share,food_detected_creatures,food_detected_share,plant_detected_creatures,plant_detected_share,meat_detected_creatures,meat_detected_share,meat_scent_detected_creatures,meat_scent_detected_share,creature_detected_creatures,creature_detected_share,food_contact_creatures,food_contact_share,meat_contact_creatures,meat_contact_share,fresh_meat_contact_creatures,fresh_meat_contact_share,stale_meat_contact_creatures,stale_meat_contact_share,meat_contact_not_eating_creatures,meat_contact_not_eating_share,meat_contact_no_eat_no_intent_creatures,meat_contact_no_eat_no_intent_share,meat_contact_no_eat_gut_full_creatures,meat_contact_no_eat_gut_full_share,meat_contact_no_eat_storage_full_creatures,meat_contact_no_eat_storage_full_share,meat_contact_no_eat_stale_creatures,meat_contact_no_eat_stale_share,meat_contact_no_eat_other_creatures,meat_contact_no_eat_other_share,eating_creatures,eating_share,attacking_creatures,attacking_share,avg_visible_food_density,avg_visible_plant_density,avg_visible_meat_density,fresh_meat_detected_creatures,fresh_meat_detected_share,stale_meat_detected_creatures,stale_meat_detected_share,stale_meat_avoided_creatures,stale_meat_avoided_share,avg_visible_meat_freshness,avg_meat_scent_density,rotten_meat_scent_detected_creatures,rotten_meat_scent_detected_share,avg_rotten_meat_scent_density,avg_visible_creature_density,creature_similarity_scent_detected_creatures,creature_similarity_scent_detected_share,avg_creature_similarity_scent_density,creature_lineage_scent_detected_creatures,creature_lineage_scent_detected_share,avg_creature_lineage_scent_density,egg_lineage_scent_detected_creatures,egg_lineage_scent_detected_share,avg_egg_lineage_scent_density,creature_identity_scent_detected_creatures,creature_identity_scent_detected_share,avg_creature_identity_scent_density,egg_identity_scent_detected_creatures,egg_identity_scent_detected_share,avg_egg_identity_scent_density,total_calories_eaten_per_second,plant_calories_eaten_per_second,tender_plant_calories_eaten_per_second,rich_plant_calories_eaten_per_second,tough_plant_calories_eaten_per_second,carcass_calories_eaten_per_second,egg_calories_eaten_per_second,live_prey_calories_eaten_per_second,meat_calories_eaten_share,fresh_kill_calories_eaten_share,total_calories_digested_per_second,plant_digested_energy_per_second,tender_plant_digested_energy_per_second,rich_plant_digested_energy_per_second,tough_plant_digested_energy_per_second,meat_digested_energy_per_second,meat_digested_energy_share,avg_gut_fill_ratio,avg_gut_plant_share,avg_gut_meat_share,avg_dietary_adaptation,avg_carrion_adaptation,avg_tender_plant_adaptation,avg_rich_plant_adaptation,avg_tough_plant_adaptation,avg_bite_strength,avg_damage_resistance,attacker_avg_dietary_adaptation,attacker_avg_bite_strength,attacker_avg_damage_resistance,non_attacker_avg_dietary_adaptation,non_attacker_avg_bite_strength,non_attacker_avg_damage_resistance,total_attack_damage_per_second,creature_collision_pairs,creature_collision_creatures,creature_collision_damaged_creatures,total_creature_collision_damage_per_second,avg_creature_collision_impact_speed,max_creature_collision_impact_speed,health_healed_per_second,healing_creatures,healing_creature_share,healing_energy_spent_per_second,avg_seconds_since_last_meal,total_distance_traveled_per_second,avg_distance_since_last_meal,calories_eaten_per_distance,calories_digested_per_distance,calories_eaten_per_food_vision_event,avg_birth_investment_ratio,avg_maturity_progress,adult_creatures,adult_creature_share,avg_egg_health_ratio,avg_vision_range,avg_vision_angle_degrees,births,eggs_laid,reproduction_attempts,eggs_hatched,egg_deaths,egg_predation_deaths,deaths,starvation_deaths,injury_deaths,rotten_meat_deaths,old_age_deaths,plant_depletions,plant_local_dispersals,plant_cluster_relocations,plant_global_relocations,plant_dormancy_started,plant_dormancy_completed,avg_plant_dormancy_scheduled_seconds,avg_plant_dormancy_completed_seconds,avg_meat_freshness,fresh_meat_calories_eaten_per_second,stale_meat_calories_eaten_per_second,fresh_meat_calories_eaten_share,stale_meat_calories_eaten_share,rotten_meat_damage_per_second,rotten_meat_damaged_creatures,rotten_meat_damaged_share,avg_lifespan_seconds,median_lifespan_seconds,reproduction_ready_creatures,reproduction_ready_share,reproduction_intent_creatures,reproduction_intent_share,avg_egg_reserve_ratio,avg_energy_surplus_ratio,avg_energy_fullness_ratio,avg_energy_capacity,energy_overflow_calories_per_second,avg_fat_ratio,avg_mass_burden,avg_fat_speed_multiplier,avg_fat_storage_capacity,avg_fat_storage_efficiency,fat_stored_calories_per_second,fat_released_calories_per_second,avg_recent_food_success,avg_recent_food_energy_yield,avg_tender_plant_payoff_trace,avg_rich_plant_payoff_trace,avg_tough_plant_payoff_trace,active_memory_creatures,active_memory_share,avg_memory_strength,active_injury_memory_creatures,active_injury_memory_share,avg_injury_memory_strength,memory_food_contact_share,non_memory_food_contact_share,memory_eating_share,non_memory_eating_share,memory_calories_eaten_per_distance,non_memory_calories_eaten_per_distance,memory_avg_seconds_since_last_meal,non_memory_avg_seconds_since_last_meal,memory_avg_distance_since_last_meal,non_memory_avg_distance_since_last_meal,memory_avg_recent_food_success,non_memory_avg_recent_food_success,memory_avg_generation,non_memory_avg_generation,memory_avg_max_x_progress_share,non_memory_avg_max_x_progress_share,memory_right_region_share,non_memory_right_region_share,left_region_creatures,left_region_creature_share,middle_region_creatures,middle_region_creature_share,right_region_creatures,right_region_creature_share,left_region_eggs,middle_region_eggs,right_region_eggs,left_region_plant_calories,middle_region_plant_calories,right_region_plant_calories,left_region_meat_calories,middle_region_meat_calories,right_region_meat_calories,left_region_avg_generation,middle_region_avg_generation,right_region_avg_generation,left_region_season_fertility,middle_region_season_fertility,right_region_season_fertility,creature_contact_creatures,creature_contact_share,similar_creature_contact_creatures,similar_creature_contact_share,avg_creature_contact_similarity,lineage_creature_contact_creatures,lineage_creature_contact_share,avg_creature_contact_lineage_similarity,egg_lineage_contact_creatures,egg_lineage_contact_share,avg_egg_contact_lineage_similarity,identity_creature_contact_creatures,identity_creature_contact_share,avg_creature_contact_identity_similarity,egg_identity_contact_creatures,egg_identity_contact_share,avg_egg_contact_identity_similarity,attack_intent_creatures,attack_intent_share,attack_intent_touching_creatures,attack_intent_touching_share,attack_intent_touching_similar_creatures,attack_intent_touching_similar_share,attack_intent_touching_lineage_creatures,attack_intent_touching_lineage_share,attack_intent_touching_unrelated_creatures,attack_intent_touching_unrelated_share,attack_no_intent_contact_creatures,attack_no_intent_contact_share,raw_attack_positive_creatures,raw_attack_positive_share,raw_attack_near_gate_creatures,raw_attack_near_gate_share,raw_attack_near_gate_touching_creatures,raw_attack_near_gate_touching_share,avg_attack_output,avg_touching_attack_output,grab_intent_creatures,grab_intent_share,can_grab_creatures,can_grab_share,grab_intent_can_grab_creatures,grab_intent_can_grab_share,grab_intent_no_contact_creatures,grab_intent_no_contact_share,holding_creatures,holding_share,grabbed_creatures,grabbed_share,avg_grab_output,avg_can_grab_grab_output,avg_grab_pressure,avg_grab_strength,sound_emitting_creatures,sound_emitting_share,sound_heard_creatures,sound_heard_share,avg_sound_amplitude,avg_sound_density,avg_sound_tone_clarity,temperature_cells,avg_map_temperature,min_map_temperature,max_map_temperature,avg_creature_temperature,avg_thermal_optimum,avg_thermal_tolerance,avg_creature_thermal_mismatch,hot_thermal_mismatch_creatures,cold_thermal_mismatch_creatures,avg_plant_temperature,avg_small_prey_temperature,thermal_basal_energy_per_second,comfortable_thermal_creatures,cold_thermal_stress_creatures,hot_thermal_stress_creatures,cold_temp_creatures,temperate_temp_creatures,hot_temp_creatures,cold_temp_plant_calories,temperate_temp_plant_calories,hot_temp_plant_calories,cold_temp_births,temperate_temp_births,hot_temp_births,cold_temp_deaths,temperate_temp_deaths,hot_temp_deaths,avg_metabolic_pace,low_metabolic_pace_creatures,normal_metabolic_pace_creatures,high_metabolic_pace_creatures," + GenomeTraitAveragesCsv.Header);

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
                snapshot.LocalFertilityCellCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageLocalFertilityMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MinimumLocalFertilityMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.DepletedLocalFertilityCellShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.GenomeCount.ToString(CultureInfo.InvariantCulture),
                snapshot.BrainCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageBrainHiddenNodeCount.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxBrainHiddenNodeCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageBrainHiddenInputWeightMagnitude.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageBrainHiddenOutputWeightMagnitude.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ActiveBrainHiddenOutputShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RtNeatBrainCount.ToString(CultureInfo.InvariantCulture),
                snapshot.RtNeatBrainShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageRtNeatHiddenNodeCount.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxRtNeatHiddenNodeCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageRtNeatConnectionCount.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxRtNeatConnectionCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageRtNeatEnabledConnectionCount.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxRtNeatEnabledConnectionCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageRtNeatFunctionalHiddenNodeCount.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxRtNeatFunctionalHiddenNodeCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageRtNeatFunctionalConnectionCount.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxRtNeatFunctionalConnectionCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageRtNeatDisabledConnectionCount.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxRtNeatDisabledConnectionCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageRtNeatLongestPathLength.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxRtNeatLongestPathLength.ToString(CultureInfo.InvariantCulture),
                snapshot.MaxGeneration.ToString(CultureInfo.InvariantCulture),
                snapshot.TotalCreatureEnergy.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalFatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalEggEnergy.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalEggHealth.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalResourceCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TenderPlantTypeResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.RichPlantTypeResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.ToughPlantTypeResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.TenderPlantTypeCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RichPlantTypeCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ToughPlantTypeCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.BarrenCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.BarrenCreatureCount, snapshot.CreatureCount),
                snapshot.SparseCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.SparseCreatureCount, snapshot.CreatureCount),
                snapshot.GrasslandCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.GrasslandCreatureCount, snapshot.CreatureCount),
                snapshot.RichCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RichCreatureCount, snapshot.CreatureCount),
                snapshot.ForestCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.ForestCreatureCount, snapshot.CreatureCount),
                snapshot.WetlandCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.WetlandCreatureCount, snapshot.CreatureCount),
                snapshot.TundraCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.TundraCreatureCount, snapshot.CreatureCount),
                snapshot.HighlandCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.HighlandCreatureCount, snapshot.CreatureCount),
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
                snapshot.ForestPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.WetlandPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TundraPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.HighlandPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.BarrenMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.SparseMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.GrasslandMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RichMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ForestMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.WetlandMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TundraMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.HighlandMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.BarrenCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.SparseCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.GrasslandCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RichCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ForestCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.WetlandCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TundraCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.HighlandCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.BarrenDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.SparseDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.GrasslandDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.RichDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.ForestDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.WetlandDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.TundraDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.HighlandDeathCount.ToString(CultureInfo.InvariantCulture),
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
                snapshot.MeatContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.MeatContactCreatureCount, snapshot.CreatureCount),
                snapshot.FreshMeatContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.FreshMeatContactCreatureCount, snapshot.CreatureCount),
                snapshot.StaleMeatContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.StaleMeatContactCreatureCount, snapshot.CreatureCount),
                snapshot.MeatContactNotEatingCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.MeatContactNotEatingCreatureCount, snapshot.CreatureCount),
                snapshot.MeatContactNotEatingNoIntentCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.MeatContactNotEatingNoIntentCreatureCount, snapshot.CreatureCount),
                snapshot.MeatContactNotEatingGutFullCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.MeatContactNotEatingGutFullCreatureCount, snapshot.CreatureCount),
                snapshot.MeatContactNotEatingStorageFullCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.MeatContactNotEatingStorageFullCreatureCount, snapshot.CreatureCount),
                snapshot.MeatContactNotEatingStaleCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.MeatContactNotEatingStaleCreatureCount, snapshot.CreatureCount),
                snapshot.MeatContactNotEatingOtherCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.MeatContactNotEatingOtherCreatureCount, snapshot.CreatureCount),
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
                snapshot.CreatureSimilarityScentDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.CreatureSimilarityScentDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.AverageCreatureSimilarityScentDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CreatureLineageScentDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.CreatureLineageScentDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.AverageCreatureLineageScentDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.EggLineageScentDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.EggLineageScentDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.AverageEggLineageScentDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CreatureIdentityScentDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.CreatureIdentityScentDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.AverageCreatureIdentityScentDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.EggIdentityScentDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.EggIdentityScentDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.AverageEggIdentityScentDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalPlantCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TenderPlantCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RichPlantCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ToughPlantCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalCarcassCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalEggCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalLivePreyCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MeatCaloriesEatenShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.FreshKillCaloriesEatenShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalCaloriesDigestedPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalPlantDigestedEnergyPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TenderPlantDigestedEnergyPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RichPlantDigestedEnergyPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ToughPlantDigestedEnergyPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalMeatDigestedEnergyPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MeatDigestedEnergyShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageGutFillRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageGutPlantShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageGutMeatShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageDietaryAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageCarrionAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageTenderPlantAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageRichPlantAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageToughPlantAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageBiteStrength.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageDamageResistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AttackerAverageDietaryAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AttackerAverageBiteStrength.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AttackerAverageDamageResistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonAttackerAverageDietaryAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonAttackerAverageBiteStrength.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonAttackerAverageDamageResistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalAttackDamagePerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CreatureCollisionPairCount.ToString(CultureInfo.InvariantCulture),
                snapshot.CreatureCollisionCreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.CreatureCollisionDamagedCreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.TotalCreatureCollisionDamagePerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageCreatureCollisionImpactSpeed.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxCreatureCollisionImpactSpeed.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalHealthHealedPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.HealingCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.HealingCreatureCount, snapshot.CreatureCount),
                snapshot.TotalHealingEnergySpentPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageSecondsSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalDistanceTraveledPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageDistanceSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CaloriesEatenPerDistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CaloriesDigestedPerDistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CaloriesEatenPerFoodVisionEvent.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageBirthInvestmentRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageMaturityProgress.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AdultCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.AdultCreatureCount, snapshot.CreatureCount),
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
                snapshot.OldAgeDeathCount.ToString(CultureInfo.InvariantCulture),
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
                snapshot.AverageEnergyFullnessRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageEnergyCapacityCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalEnergyOverflowCaloriesPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageFatRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageMassBurdenRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageFatSpeedMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageFatStorageCapacityCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageFatStorageEfficiency.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalFatStoredCaloriesPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalFatReleasedCaloriesPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageRecentFoodSuccess.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageRecentFoodEnergyYield.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageTenderPlantPayoffTrace.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageRichPlantPayoffTrace.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageToughPlantPayoffTrace.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ActiveMemoryCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.ActiveMemoryCreatureCount, snapshot.CreatureCount),
                snapshot.AverageMemoryStrength.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ActiveInjuryMemoryCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.ActiveInjuryMemoryCreatureCount, snapshot.CreatureCount),
                snapshot.AverageInjuryMemoryStrength.ToString("0.######", CultureInfo.InvariantCulture),
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
                snapshot.RightRegionSeasonFertilityMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CreatureContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.CreatureContactCreatureCount, snapshot.CreatureCount),
                snapshot.SimilarCreatureContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.SimilarCreatureContactCreatureCount, snapshot.CreatureCount),
                snapshot.AverageCreatureContactSimilarity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.LineageCreatureContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.LineageCreatureContactCreatureCount, snapshot.CreatureCount),
                snapshot.AverageCreatureContactLineageSimilarity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.EggLineageContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.EggLineageContactCreatureCount, snapshot.CreatureCount),
                snapshot.AverageEggContactLineageSimilarity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.IdentityCreatureContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.IdentityCreatureContactCreatureCount, snapshot.CreatureCount),
                snapshot.AverageCreatureContactIdentitySimilarity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.EggIdentityContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.EggIdentityContactCreatureCount, snapshot.CreatureCount),
                snapshot.AverageEggContactIdentitySimilarity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AttackIntentCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.AttackIntentCreatureCount, snapshot.CreatureCount),
                snapshot.AttackIntentWhileTouchingCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.AttackIntentWhileTouchingCreatureCount, snapshot.CreatureCount),
                snapshot.AttackIntentWhileTouchingSimilarCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.AttackIntentWhileTouchingSimilarCreatureCount, snapshot.CreatureCount),
                snapshot.AttackIntentWhileTouchingLineageCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.AttackIntentWhileTouchingLineageCreatureCount, snapshot.CreatureCount),
                snapshot.AttackIntentWhileTouchingUnrelatedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.AttackIntentWhileTouchingUnrelatedCreatureCount, snapshot.CreatureCount),
                snapshot.AttackNoIntentContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.AttackNoIntentContactCreatureCount, snapshot.CreatureCount),
                snapshot.RawAttackPositiveCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RawAttackPositiveCreatureCount, snapshot.CreatureCount),
                snapshot.RawAttackNearGateCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RawAttackNearGateCreatureCount, snapshot.CreatureCount),
                snapshot.RawAttackNearGateWhileTouchingCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RawAttackNearGateWhileTouchingCreatureCount, snapshot.CreatureCount),
                snapshot.AverageAttackOutput.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageTouchingAttackOutput.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.GrabIntentCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.GrabIntentCreatureCount, snapshot.CreatureCount),
                snapshot.CanGrabCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.CanGrabCreatureCount, snapshot.CreatureCount),
                snapshot.GrabIntentWhileCanGrabCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.GrabIntentWhileCanGrabCreatureCount, snapshot.CreatureCount),
                snapshot.GrabIntentWithoutCanGrabCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.GrabIntentWithoutCanGrabCreatureCount, snapshot.CreatureCount),
                snapshot.HoldingCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.HoldingCreatureCount, snapshot.CreatureCount),
                snapshot.GrabbedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.GrabbedCreatureCount, snapshot.CreatureCount),
                snapshot.AverageGrabOutput.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageCanGrabGrabOutput.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageGrabPressure.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageGrabStrength.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.SoundEmittingCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.SoundEmittingCreatureCount, snapshot.CreatureCount),
                snapshot.SoundHeardCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.SoundHeardCreatureCount, snapshot.CreatureCount),
                snapshot.AverageSoundAmplitude.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageSoundDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageSoundToneClarity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TemperatureCellCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageMapTemperature.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MinimumMapTemperature.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaximumMapTemperature.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageCreatureTemperature.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageThermalOptimum.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageThermalTolerance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageCreatureThermalMismatch.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.HotThermalMismatchCreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.ColdThermalMismatchCreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AveragePlantTemperature.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageSmallPreyTemperature.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ThermalBasalEnergyPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ComfortableThermalCreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.ColdThermalStressCreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.HotThermalStressCreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.ColdTemperatureCreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.TemperateTemperatureCreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.HotTemperatureCreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.ColdTemperaturePlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TemperateTemperaturePlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.HotTemperaturePlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ColdTemperatureBirths.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TemperateTemperatureBirths.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.HotTemperatureBirths.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ColdTemperatureDeaths.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TemperateTemperatureDeaths.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.HotTemperatureDeaths.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageMetabolicPace.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.LowMetabolicPaceCreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.NormalMetabolicPaceCreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.HighMetabolicPaceCreatureCount.ToString(CultureInfo.InvariantCulture),
                GenomeTraitAveragesCsv.Values(snapshot.AverageGenomeTraits)));
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
        writer.WriteLine("id,parent_id,birth_tick,birth_elapsed_seconds,generation,genome_id,brain_id,birth_energy,birth_temperature,max_x_reached,death_tick,death_elapsed_seconds,death_temperature,death_reason,death_attacker_id,is_founder,is_alive,telemetry_living_seconds,avg_occupied_temperature,avg_thermal_mismatch,cold_temperature_share,temperate_temperature_share,hot_temperature_share,comfortable_thermal_share,cold_thermal_stress_share,hot_thermal_stress_share");

        foreach (var record in records)
        {
            var livingSeconds = Math.Max(0f, record.TelemetryLivingSeconds);
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
                record.BirthTemperature.ToString("0.######", CultureInfo.InvariantCulture),
                record.MaxXReached.ToString("0.######", CultureInfo.InvariantCulture),
                record.DeathTick?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                record.DeathElapsedSeconds?.ToString("0.######", CultureInfo.InvariantCulture) ?? string.Empty,
                record.DeathTick is null
                    ? string.Empty
                    : record.DeathTemperature.ToString("0.######", CultureInfo.InvariantCulture),
                record.DeathReason?.ToString() ?? string.Empty,
                record.DeathAttackerId == default
                    ? string.Empty
                    : record.DeathAttackerId.Value.ToString(CultureInfo.InvariantCulture),
                record.IsFounder.ToString(CultureInfo.InvariantCulture),
                record.IsAlive.ToString(CultureInfo.InvariantCulture),
                record.TelemetryLivingSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                FormatRate(record.TelemetryTemperatureExposure, livingSeconds),
                FormatRate(record.TelemetryThermalMismatchExposure, livingSeconds),
                FormatRate(record.TelemetryColdTemperatureSeconds, livingSeconds),
                FormatRate(record.TelemetryTemperateTemperatureSeconds, livingSeconds),
                FormatRate(record.TelemetryHotTemperatureSeconds, livingSeconds),
                FormatRate(record.TelemetryComfortableThermalSeconds, livingSeconds),
                FormatRate(record.TelemetryColdThermalStressSeconds, livingSeconds),
                FormatRate(record.TelemetryHotThermalStressSeconds, livingSeconds)));
        }
    }

    private static string FormatRate(float value, float divisor)
    {
        return (divisor > 0f ? value / divisor : 0f).ToString("0.######", CultureInfo.InvariantCulture);
    }
}

internal static class TraitSummaryCsvWriter
{
    public static void Write(string path, WorldState state)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("scope,count,avg_body_radius,min_body_radius,max_body_radius,avg_max_speed,min_max_speed,max_max_speed,avg_vision_range,min_vision_range,max_vision_range,avg_vision_angle_degrees,min_vision_angle_degrees,max_vision_angle_degrees,avg_reproduction_threshold,min_reproduction_threshold,max_reproduction_threshold,avg_offspring_investment,min_offspring_investment,max_offspring_investment,avg_egg_production_per_second,min_egg_production_per_second,max_egg_production_per_second,avg_egg_incubation_seconds,min_egg_incubation_seconds,max_egg_incubation_seconds,avg_maturity_age_seconds,min_maturity_age_seconds,max_maturity_age_seconds,avg_dietary_adaptation,min_dietary_adaptation,max_dietary_adaptation,avg_carrion_adaptation,min_carrion_adaptation,max_carrion_adaptation,avg_tender_plant_adaptation,min_tender_plant_adaptation,max_tender_plant_adaptation,avg_rich_plant_adaptation,min_rich_plant_adaptation,max_rich_plant_adaptation,avg_tough_plant_adaptation,min_tough_plant_adaptation,max_tough_plant_adaptation,avg_plant_digestion,min_plant_digestion,max_plant_digestion,avg_meat_digestion,min_meat_digestion,max_meat_digestion,avg_fresh_meat_digestion,min_fresh_meat_digestion,max_fresh_meat_digestion,avg_stale_meat_digestion,min_stale_meat_digestion,max_stale_meat_digestion,avg_gut_capacity,min_gut_capacity,max_gut_capacity,avg_digestion_rate,min_digestion_rate,max_digestion_rate,avg_bite_strength,min_bite_strength,max_bite_strength,avg_damage_resistance,min_damage_resistance,max_damage_resistance,avg_thermal_optimum,min_thermal_optimum,max_thermal_optimum,avg_thermal_tolerance,min_thermal_tolerance,max_thermal_tolerance,avg_mutation_strength,min_mutation_strength,max_mutation_strength,avg_trait_mutation_rate,min_trait_mutation_rate,max_trait_mutation_rate,avg_brain_mutation_rate,min_brain_mutation_rate,max_brain_mutation_rate,avg_metabolic_pace,min_metabolic_pace,max_metabolic_pace");

        if (state.Creatures.Count == 0)
        {
            writer.WriteLine("living_creatures,0" + new string(',', 81));
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
            Format(summary.TenderPlantAdaptation.Average),
            Format(summary.TenderPlantAdaptation.Min),
            Format(summary.TenderPlantAdaptation.Max),
            Format(summary.RichPlantAdaptation.Average),
            Format(summary.RichPlantAdaptation.Min),
            Format(summary.RichPlantAdaptation.Max),
            Format(summary.ToughPlantAdaptation.Average),
            Format(summary.ToughPlantAdaptation.Min),
            Format(summary.ToughPlantAdaptation.Max),
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
            Format(summary.ThermalOptimum.Average),
            Format(summary.ThermalOptimum.Min),
            Format(summary.ThermalOptimum.Max),
            Format(summary.ThermalTolerance.Average),
            Format(summary.ThermalTolerance.Min),
            Format(summary.ThermalTolerance.Max),
            Format(summary.MutationStrength.Average),
            Format(summary.MutationStrength.Min),
            Format(summary.MutationStrength.Max),
            Format(summary.TraitMutationRate.Average),
            Format(summary.TraitMutationRate.Min),
            Format(summary.TraitMutationRate.Max),
            Format(summary.BrainMutationRate.Average),
            Format(summary.BrainMutationRate.Min),
            Format(summary.BrainMutationRate.Max),
            Format(summary.MetabolicPace.Average),
            Format(summary.MetabolicPace.Min),
            Format(summary.MetabolicPace.Max)));
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

internal static class SpeciesClusterCsvWriter
{
    public static void Write(string path, WorldState state)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("rank,species_id,name,living_creatures,living_share,founder_count,dominant_founder_id,dominant_founder_living,representative_creature_id,representative_distance,min_generation,avg_generation,max_generation,avg_energy,avg_age_seconds,avg_genome_distance,avg_brain_distance,avg_body_radius,avg_max_speed,avg_vision_range,avg_metabolic_pace,avg_dietary_adaptation,avg_carrion_adaptation,avg_tender_plant_adaptation,avg_rich_plant_adaptation,avg_tough_plant_adaptation,avg_plant_digestion,avg_meat_digestion,avg_fresh_meat_digestion,avg_stale_meat_digestion,avg_bite_strength,avg_damage_resistance,avg_thermal_optimum,min_thermal_optimum,max_thermal_optimum,avg_thermal_tolerance,min_thermal_tolerance,max_thermal_tolerance,avg_current_temperature,avg_current_thermal_mismatch,avg_occupied_temperature,avg_occupied_thermal_mismatch,cold_temp_living,temperate_temp_living,hot_temp_living,comfortable_thermal_living,cold_thermal_stress_living,hot_thermal_stress_living,cold_temp_lifetime_share,temperate_temp_lifetime_share,hot_temp_lifetime_share,comfortable_thermal_lifetime_share,cold_thermal_stress_lifetime_share,hot_thermal_stress_lifetime_share,cold_temp_births,temperate_temp_births,hot_temp_births,cold_temp_deaths,temperate_temp_deaths,hot_temp_deaths,thermal_niche_label,recent_plant_kcal,recent_meat_kcal,eating_share,attack_share,current_east_progress_share,right_region_share,diet_label,tactic_label,region_label");

        foreach (var summary in SpeciesClusterAnalyzer.Analyze(state))
        {
            writer.WriteLine(string.Join(
                ',',
                summary.Rank.ToString(CultureInfo.InvariantCulture),
                summary.SpeciesId.ToString(CultureInfo.InvariantCulture),
                Escape(summary.Name),
                summary.LivingCreatures.ToString(CultureInfo.InvariantCulture),
                Format(summary.LivingShare),
                summary.FounderCount.ToString(CultureInfo.InvariantCulture),
                summary.DominantFounderId.Value.ToString(CultureInfo.InvariantCulture),
                summary.DominantFounderLivingCreatures.ToString(CultureInfo.InvariantCulture),
                summary.RepresentativeCreatureId.Value.ToString(CultureInfo.InvariantCulture),
                Format(summary.RepresentativeDistance),
                summary.MinGeneration.ToString(CultureInfo.InvariantCulture),
                Format(summary.AverageGeneration),
                summary.MaxGeneration.ToString(CultureInfo.InvariantCulture),
                Format(summary.AverageEnergy),
                Format(summary.AverageAgeSeconds),
                Format(summary.AverageGenomeDistance),
                Format(summary.AverageBrainDistance),
                Format(summary.AverageBodyRadius),
                Format(summary.AverageMaxSpeed),
                Format(summary.AverageSenseRadius),
                Format(summary.AverageMetabolicPace),
                Format(summary.AverageDietaryAdaptation),
                Format(summary.AverageCarrionAdaptation),
                Format(summary.AverageTenderPlantAdaptation),
                Format(summary.AverageRichPlantAdaptation),
                Format(summary.AverageToughPlantAdaptation),
                Format(summary.AveragePlantDigestion),
                Format(summary.AverageMeatDigestion),
                Format(summary.AverageFreshMeatDigestion),
                Format(summary.AverageStaleMeatDigestion),
                Format(summary.AverageBiteStrength),
                Format(summary.AverageDamageResistance),
                Format(summary.AverageThermalOptimum),
                Format(summary.MinimumThermalOptimum),
                Format(summary.MaximumThermalOptimum),
                Format(summary.AverageThermalTolerance),
                Format(summary.MinimumThermalTolerance),
                Format(summary.MaximumThermalTolerance),
                Format(summary.AverageCurrentTemperature),
                Format(summary.AverageCurrentThermalMismatch),
                Format(summary.AverageOccupiedTemperature),
                Format(summary.AverageOccupiedThermalMismatch),
                summary.ColdTemperatureLivingCreatures.ToString(CultureInfo.InvariantCulture),
                summary.TemperateTemperatureLivingCreatures.ToString(CultureInfo.InvariantCulture),
                summary.HotTemperatureLivingCreatures.ToString(CultureInfo.InvariantCulture),
                summary.ComfortableThermalLivingCreatures.ToString(CultureInfo.InvariantCulture),
                summary.ColdThermalStressLivingCreatures.ToString(CultureInfo.InvariantCulture),
                summary.HotThermalStressLivingCreatures.ToString(CultureInfo.InvariantCulture),
                Format(summary.ColdTemperatureLifetimeShare),
                Format(summary.TemperateTemperatureLifetimeShare),
                Format(summary.HotTemperatureLifetimeShare),
                Format(summary.ComfortableThermalLifetimeShare),
                Format(summary.ColdThermalStressLifetimeShare),
                Format(summary.HotThermalStressLifetimeShare),
                summary.ColdTemperatureBirths.ToString(CultureInfo.InvariantCulture),
                summary.TemperateTemperatureBirths.ToString(CultureInfo.InvariantCulture),
                summary.HotTemperatureBirths.ToString(CultureInfo.InvariantCulture),
                summary.ColdTemperatureDeaths.ToString(CultureInfo.InvariantCulture),
                summary.TemperateTemperatureDeaths.ToString(CultureInfo.InvariantCulture),
                summary.HotTemperatureDeaths.ToString(CultureInfo.InvariantCulture),
                Escape(summary.ThermalNicheLabel),
                Format(summary.RecentPlantCaloriesEaten),
                Format(summary.RecentMeatCaloriesEaten),
                Format(summary.EatingShare),
                Format(summary.AttackShare),
                Format(summary.CurrentEastProgressShare),
                Format(summary.RightRegionShare),
                Escape(summary.DietLabel),
                Escape(summary.TacticLabel),
                Escape(summary.RegionLabel)));
        }
    }

    private static string Format(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}

internal static class SpeciesClusterTrendCsvWriter
{
    public static void Write(
        string path,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        WorldState state)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("tick,elapsed_seconds,rank,species_id,name,living_creatures,total_living,living_share,min_generation,avg_generation,max_generation");

        foreach (var row in SpeciesClusterAnalyzer.AnalyzeHistory(state, snapshots).Rows)
        {
            writer.WriteLine(string.Join(
                ',',
                row.Tick.ToString(CultureInfo.InvariantCulture),
                row.ElapsedSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                row.Rank.ToString(CultureInfo.InvariantCulture),
                row.SpeciesId.ToString(CultureInfo.InvariantCulture),
                Escape(row.Name),
                row.LivingCreatures.ToString(CultureInfo.InvariantCulture),
                row.TotalLiving.ToString(CultureInfo.InvariantCulture),
                row.LivingShare.ToString("0.######", CultureInfo.InvariantCulture),
                row.MinGeneration.ToString(CultureInfo.InvariantCulture),
                row.AverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                row.MaxGeneration.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static string Escape(string value)
    {
        return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}

internal static class FounderSummaryCsvWriter
{
    public static void Write(string path, IReadOnlyList<CreatureLineageRecord> records)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("founder_id,total_creatures,descendant_count,living_creatures,dead_creatures,max_generation,avg_occupied_temperature,avg_thermal_mismatch,cold_temperature_share,temperate_temperature_share,hot_temperature_share,comfortable_thermal_share,cold_thermal_stress_share,hot_thermal_stress_share,cold_temperature_births,temperate_temperature_births,hot_temperature_births,cold_temperature_deaths,temperate_temperature_deaths,hot_temperature_deaths,thermal_niche_label");

        foreach (var summary in Summarize(records).OrderBy(summary => summary.FounderId.Value))
        {
            writer.WriteLine(string.Join(
                ',',
                summary.FounderId.Value.ToString(CultureInfo.InvariantCulture),
                summary.TotalCreatures.ToString(CultureInfo.InvariantCulture),
                summary.DescendantCount.ToString(CultureInfo.InvariantCulture),
                summary.LivingCreatures.ToString(CultureInfo.InvariantCulture),
                summary.DeadCreatures.ToString(CultureInfo.InvariantCulture),
                summary.MaxGeneration.ToString(CultureInfo.InvariantCulture),
                Format(summary.ThermalNiche.AverageOccupiedTemperature),
                Format(summary.ThermalNiche.AverageThermalMismatch),
                Format(summary.ThermalNiche.ColdTemperatureShare),
                Format(summary.ThermalNiche.TemperateTemperatureShare),
                Format(summary.ThermalNiche.HotTemperatureShare),
                Format(summary.ThermalNiche.ComfortableThermalShare),
                Format(summary.ThermalNiche.ColdThermalStressShare),
                Format(summary.ThermalNiche.HotThermalStressShare),
                summary.ThermalNiche.ColdTemperatureBirths.ToString(CultureInfo.InvariantCulture),
                summary.ThermalNiche.TemperateTemperatureBirths.ToString(CultureInfo.InvariantCulture),
                summary.ThermalNiche.HotTemperatureBirths.ToString(CultureInfo.InvariantCulture),
                summary.ThermalNiche.ColdTemperatureDeaths.ToString(CultureInfo.InvariantCulture),
                summary.ThermalNiche.TemperateTemperatureDeaths.ToString(CultureInfo.InvariantCulture),
                summary.ThermalNiche.HotTemperatureDeaths.ToString(CultureInfo.InvariantCulture),
                Escape(summary.ThermalNiche.NicheLabel)));
        }
    }

    public static IReadOnlyList<FounderSummary> Summarize(IReadOnlyList<CreatureLineageRecord> records)
    {
        var byId = records.ToDictionary(record => record.Id);
        var summaries = new Dictionary<EntityId, List<CreatureLineageRecord>>();

        foreach (var record in records)
        {
            var founderId = FindFounderId(record, byId);
            if (!summaries.TryGetValue(founderId, out var founderRecords))
            {
                founderRecords = [];
                summaries[founderId] = founderRecords;
            }

            founderRecords.Add(record);
        }

        return summaries
            .Select(pair =>
            {
                var founderRecords = pair.Value;
                var totalCreatures = founderRecords.Count;
                var livingCreatures = founderRecords.Count(record => record.IsAlive);
                return new FounderSummary(
                    pair.Key,
                    totalCreatures,
                    Math.Max(0, totalCreatures - 1),
                    livingCreatures,
                    Math.Max(0, totalCreatures - livingCreatures),
                    founderRecords.Count == 0 ? 0 : founderRecords.Max(record => record.Generation),
                    ThermalNicheTelemetry.SummarizeRecords(founderRecords));
            })
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

    private static string Format(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}

internal readonly record struct FounderSummary(
    EntityId FounderId,
    int TotalCreatures,
    int DescendantCount,
    int LivingCreatures,
    int DeadCreatures,
    int MaxGeneration,
    ThermalLineageNicheSummary ThermalNiche);

internal static class ThermalEcotypeCsvWriter
{
    public static void Write(string path, WorldState state)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("thermal_ecotype,founder_lineages,total_creatures,living_creatures,dead_creatures,max_generation,dominant_founder,dominant_founder_living,avg_living_thermal_optimum,avg_living_thermal_tolerance,avg_occupied_temperature,avg_thermal_mismatch,cold_temperature_share,temperate_temperature_share,hot_temperature_share,comfortable_thermal_share,cold_thermal_stress_share,hot_thermal_stress_share,cold_temperature_births,temperate_temperature_births,hot_temperature_births,cold_temperature_deaths,temperate_temperature_deaths,hot_temperature_deaths,top_founders");

        foreach (var summary in ThermalEcotypeAnalyzer.Analyze(state).OrderBy(summary => summary.Label, StringComparer.Ordinal))
        {
            writer.WriteLine(string.Join(
                ',',
                Escape(summary.Label),
                summary.FounderLineageCount.ToString(CultureInfo.InvariantCulture),
                summary.TotalCreatures.ToString(CultureInfo.InvariantCulture),
                summary.LivingCreatures.ToString(CultureInfo.InvariantCulture),
                summary.DeadCreatures.ToString(CultureInfo.InvariantCulture),
                summary.MaxGeneration.ToString(CultureInfo.InvariantCulture),
                summary.DominantFounderId.Value.ToString(CultureInfo.InvariantCulture),
                summary.DominantFounderLivingCreatures.ToString(CultureInfo.InvariantCulture),
                Format(summary.AverageLivingThermalOptimum),
                Format(summary.AverageLivingThermalTolerance),
                Format(summary.AverageOccupiedTemperature),
                Format(summary.AverageThermalMismatch),
                Format(summary.ColdTemperatureShare),
                Format(summary.TemperateTemperatureShare),
                Format(summary.HotTemperatureShare),
                Format(summary.ComfortableThermalShare),
                Format(summary.ColdThermalStressShare),
                Format(summary.HotThermalStressShare),
                summary.ColdTemperatureBirths.ToString(CultureInfo.InvariantCulture),
                summary.TemperateTemperatureBirths.ToString(CultureInfo.InvariantCulture),
                summary.HotTemperatureBirths.ToString(CultureInfo.InvariantCulture),
                summary.ColdTemperatureDeaths.ToString(CultureInfo.InvariantCulture),
                summary.TemperateTemperatureDeaths.ToString(CultureInfo.InvariantCulture),
                summary.HotTemperatureDeaths.ToString(CultureInfo.InvariantCulture),
                Escape(FormatTopFounders(summary.TopFounders))));
        }
    }

    private static string Format(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static string FormatTopFounders(IReadOnlyList<ThermalEcotypeFounderSummary> founders)
    {
        return string.Join(
            "; ",
            founders.Select(founder => $"#{founder.FounderId.Value} living {founder.LivingCreatures}"));
    }

    private static string Escape(string value)
    {
        return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r') || value.Contains(';')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}

internal static class RosterLineageSummaryCsvWriter
{
    public static void Write(
        string path,
        IReadOnlyList<CreatureLineageRecord> records,
        IReadOnlyList<SpeciesInjectionResult> injections,
        long? finalTick = null)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("profile_name,founder_count,total_creatures,descendant_count,living_creatures,dead_creatures,max_generation,starvation_deaths,injury_deaths,rotten_meat_deaths,old_age_deaths,unknown_deaths,tail_avg_living_creatures,extinction_tick,extinction_elapsed_seconds,injury_deaths_from_same_profile,injury_deaths_from_other_profile,injury_deaths_unattributed,same_profile_injury_kills_dealt,cross_profile_injury_kills_dealt,telemetry_living_seconds,calories_eaten_per_second,plant_calories_eaten_per_second,meat_calories_eaten_per_second,carcass_calories_eaten_per_second,egg_calories_eaten_per_second,fresh_kill_calories_eaten_per_second,fresh_meat_calories_eaten_per_second,stale_meat_calories_eaten_per_second,meat_calories_eaten_share,fresh_kill_calories_eaten_share,fresh_meat_calories_eaten_share,stale_meat_calories_eaten_share,rotten_meat_damage_per_second,attack_damage_dealt_per_second,attack_damage_taken_per_second,eating_share,meat_eating_share,food_contact_share,meat_detected_share,fresh_meat_detected_share,stale_meat_detected_share,rotten_meat_scent_detected_share,creature_contact_share,similar_creature_contact_share,lineage_creature_contact_share,egg_lineage_contact_share,attack_intent_share,attack_intent_touching_share,attack_intent_lineage_touching_share,attack_intent_unrelated_touching_share,attack_damage_dealing_share,genome_ids,brain_ids");

        foreach (var summary in RosterLineageAnalyzer.Analyze(records, injections, finalTick))
        {
            writer.WriteLine(string.Join(
                ',',
                EscapeCsv(summary.ProfileName),
                summary.FounderCount.ToString(CultureInfo.InvariantCulture),
                summary.TotalCreatures.ToString(CultureInfo.InvariantCulture),
                summary.DescendantCount.ToString(CultureInfo.InvariantCulture),
                summary.LivingCreatures.ToString(CultureInfo.InvariantCulture),
                summary.DeadCreatures.ToString(CultureInfo.InvariantCulture),
                summary.MaxGeneration.ToString(CultureInfo.InvariantCulture),
                summary.StarvationDeaths.ToString(CultureInfo.InvariantCulture),
                summary.InjuryDeaths.ToString(CultureInfo.InvariantCulture),
                summary.RottenMeatDeaths.ToString(CultureInfo.InvariantCulture),
                summary.OldAgeDeaths.ToString(CultureInfo.InvariantCulture),
                summary.UnknownDeaths.ToString(CultureInfo.InvariantCulture),
                summary.TailAverageLivingCreatures.ToString("0.######", CultureInfo.InvariantCulture),
                summary.ExtinctionTick?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                summary.ExtinctionElapsedSeconds?.ToString("0.######", CultureInfo.InvariantCulture) ?? string.Empty,
                summary.InjuryDeathsFromSameProfile.ToString(CultureInfo.InvariantCulture),
                summary.InjuryDeathsFromOtherProfile.ToString(CultureInfo.InvariantCulture),
                summary.InjuryDeathsFromUnknownProfile.ToString(CultureInfo.InvariantCulture),
                summary.SameProfileInjuryKillsDealt.ToString(CultureInfo.InvariantCulture),
                summary.CrossProfileInjuryKillsDealt.ToString(CultureInfo.InvariantCulture),
                Format(summary.TelemetryLivingSeconds),
                Format(summary.CaloriesEatenPerSecond),
                Format(summary.PlantCaloriesEatenPerSecond),
                Format(summary.MeatCaloriesEatenPerSecond),
                Format(summary.CarcassCaloriesEatenPerSecond),
                Format(summary.EggCaloriesEatenPerSecond),
                Format(summary.FreshKillCaloriesEatenPerSecond),
                Format(summary.FreshMeatCaloriesEatenPerSecond),
                Format(summary.StaleMeatCaloriesEatenPerSecond),
                Format(summary.MeatCaloriesEatenShare),
                Format(summary.FreshKillCaloriesEatenShare),
                Format(summary.FreshMeatCaloriesEatenShare),
                Format(summary.StaleMeatCaloriesEatenShare),
                Format(summary.RottenMeatDamagePerSecond),
                Format(summary.AttackDamageDealtPerSecond),
                Format(summary.AttackDamageTakenPerSecond),
                Format(summary.EatingShare),
                Format(summary.MeatEatingShare),
                Format(summary.FoodContactShare),
                Format(summary.MeatDetectedShare),
                Format(summary.FreshMeatDetectedShare),
                Format(summary.StaleMeatDetectedShare),
                Format(summary.RottenMeatScentDetectedShare),
                Format(summary.CreatureContactShare),
                Format(summary.SimilarCreatureContactShare),
                Format(summary.LineageCreatureContactShare),
                Format(summary.EggLineageContactShare),
                Format(summary.AttackIntentShare),
                Format(summary.AttackIntentTouchingShare),
                Format(summary.AttackIntentLineageTouchingShare),
                Format(summary.AttackIntentUnrelatedTouchingShare),
                Format(summary.AttackDamageDealingShare),
                EscapeCsv(string.Join("|", summary.GenomeIds)),
                EscapeCsv(string.Join("|", summary.BrainIds))));
        }
    }

    private static string Format(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static string EscapeCsv(string value)
    {
        return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}

internal static class GenerationSummaryCsvWriter
{
    public static void Write(string path, IReadOnlyList<CreatureLineageRecord> records)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        writer.WriteLine("generation,births,living,dead,starvation_deaths,injury_deaths,rotten_meat_deaths,old_age_deaths,survival_rate");

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
                summary.OldAgeDeaths.ToString(CultureInfo.InvariantCulture),
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
                var oldAgeDeaths = group.Count(record => record.DeathReason == CreatureDeathReason.OldAge);

                return new GenerationSummary(
                    group.Key,
                    births,
                    living,
                    births - living,
                    starvationDeaths,
                    injuryDeaths,
                    rottenMeatDeaths,
                    oldAgeDeaths);
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
    int RottenMeatDeaths,
    int OldAgeDeaths)
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
        writer.WriteLine("phase,queries,candidates,plant_candidates,meat_candidates,visible,total_ms,avg_candidates_per_query,avg_ms_per_query,cells_visited,non_empty_cells,distance_rejects,self_rejects,nonviable_rejects,range_rejects,vision_rejects,body_radius_cache_misses,scheduled_refreshes,close_refreshes,immediate_close_refreshes,proximity_close_refreshes,forced_refreshes,skipped_updates");
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
            immediateCloseRefreshes: profile.WorldSenseImmediateCloseRefreshes,
            proximityCloseRefreshes: profile.WorldSenseProximityCloseRefreshes,
            forcedRefreshes: profile.WorldSenseForcedRefreshes,
            skippedUpdates: profile.WorldSenseSkippedUpdates);
        WriteRow(
            writer,
            "creature_setup",
            profile.CreatureSetupSamples,
            profile.CreatureSetupSamples,
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
            profile.InternalStateSamples,
            profile.InternalStateSamples,
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
            "plant_resource_scan",
            profile.PlantResourceScanSamples,
            profile.PlantCandidates,
            profile.PlantCandidates,
            0,
            profile.VisiblePlantCandidates,
            profile.PlantResourceScanMilliseconds,
            0,
            0,
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
            "meat_resource_scan",
            profile.MeatResourceScanSamples,
            profile.MeatResourceCandidates,
            0,
            profile.MeatResourceCandidates,
            profile.VisibleMeatResourceCandidates,
            profile.MeatResourceScanMilliseconds,
            0,
            0,
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
            "egg_lineage_scent",
            queries: profile.EggQueries,
            candidates: profile.EggLineageScentCandidates,
            plantCandidates: 0,
            meatCandidates: 0,
            visible: profile.EggLineageScentHits,
            totalMilliseconds: 0,
            cellsVisited: 0,
            nonEmptyCells: 0,
            distanceRejects: 0,
            selfRejects: 0,
            nonviableRejects: 0,
            rangeRejects: 0,
            visionRejects: 0,
            bodyRadiusCacheMisses: 0);
        WriteRow(
            writer,
            "egg_identity_scent",
            queries: profile.EggQueries,
            candidates: profile.EggIdentityScentCandidates,
            plantCandidates: 0,
            meatCandidates: 0,
            visible: profile.EggIdentityScentHits,
            totalMilliseconds: 0,
            cellsVisited: 0,
            nonEmptyCells: 0,
            distanceRejects: 0,
            selfRejects: 0,
            nonviableRejects: 0,
            rangeRejects: 0,
            visionRejects: 0,
            bodyRadiusCacheMisses: 0);
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
            profile.TerrainSenseSamples,
            profile.TerrainSenseSamples,
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
            profile.MemorySenseSamples,
            profile.MemorySenseSamples,
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
            profile.SenseFinalizationSamples,
            profile.SenseFinalizationSamples,
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
        WriteRow(
            writer,
            "sense_finalization_refreshed",
            profile.SenseFinalizationRefreshedSamples,
            profile.SenseFinalizationRefreshedSamples,
            0,
            0,
            0,
            profile.SenseFinalizationRefreshedMilliseconds,
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
            "sense_finalization_skipped",
            profile.SenseFinalizationSkippedSamples,
            profile.SenseFinalizationSkippedSamples,
            0,
            0,
            0,
            profile.SenseFinalizationSkippedMilliseconds,
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
            "plant_preference_bridge",
            profile.PlantPreferenceBridgeSamples,
            profile.PlantPreferenceBridgeSamples,
            0,
            0,
            0,
            profile.PlantPreferenceBridgeMilliseconds,
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
        long immediateCloseRefreshes = 0,
        long proximityCloseRefreshes = 0,
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
            immediateCloseRefreshes.ToString(CultureInfo.InvariantCulture),
            proximityCloseRefreshes.ToString(CultureInfo.InvariantCulture),
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
        writer.WriteLine("<thead><tr><th>Scenario</th><th>Seed</th><th>Final Pop</th><th>Eggs</th><th>Pop Change</th><th>Births</th><th>Eggs Laid</th><th>Hatched</th><th>Egg Deaths</th><th>Egg Pred</th><th>Deaths</th><th>Starved</th><th>Injury</th><th>Old Age</th><th>Max Gen</th><th>Resource Final</th><th>Dominant Founder</th><th>Report</th></tr></thead>");
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
                $"<td>{Html(summary.OldAgeDeaths)}</td>" +
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
            state.Stats.OldAgeDeathCount,
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
    int OldAgeDeaths,
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
    private const int ReportTimelineSampleLimit = 1200;
    private const int SurvivorLineageTreeNodeRenderLimit = 260;
    private const int RtNeatGraphRenderLimit = 3;

    private readonly record struct SpatialHeatmapLayer(
        string Title,
        string Units,
        IReadOnlyList<float> Values,
        string Color,
        string Description);

    private readonly record struct RtNeatBrainGraphCandidate(
        int BrainId,
        int LivingCreatures,
        int Eggs,
        RtNeatBrainGenome Brain);

    public static void Write(
        string path,
        RunOptions options,
        SimulationScenario scenario,
        Simulation simulation,
        TimeSpan elapsed,
        OutputPaths outputPaths,
        IReadOnlyList<SpeciesInjectionResult> speciesInjections,
        IReadOnlyList<CheckpointArtifact> checkpoints)
    {
        using var writer = StatsCsvWriter.CreateWriter(path);
        var state = simulation.State;
        var snapshots = state.Stats.Snapshots;
        var reportSnapshots = SelectReportSnapshots(snapshots);
        var finalSnapshot = snapshots.Count > 0 ? snapshots[^1] : default;
        var timingEnabled = string.Equals(
            Environment.GetEnvironmentVariable("LINEAGE_REPORT_TIMING"),
            "1",
            StringComparison.Ordinal);
        var timingStopwatch = Stopwatch.StartNew();
        void TraceTiming(string label)
        {
            if (!timingEnabled)
            {
                return;
            }

            Console.Error.WriteLine($"report_timing,{label},{timingStopwatch.Elapsed.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture)}");
            timingStopwatch.Restart();
        }

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
        var collisionDamageTrend = Trend.From(
            snapshots,
            snapshot => snapshot.TotalCreatureCollisionDamagePerSecond,
            finalSnapshot.TotalCreatureCollisionDamagePerSecond);
        var healingTrend = Trend.From(
            snapshots,
            snapshot => snapshot.TotalHealthHealedPerSecond,
            finalSnapshot.TotalHealthHealedPerSecond);
        var healingEnergyTrend = Trend.From(
            snapshots,
            snapshot => snapshot.TotalHealingEnergySpentPerSecond,
            finalSnapshot.TotalHealingEnergySpentPerSecond);
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
        TraceTiming("trends");
        var speciesSummaries = SpeciesClusterAnalyzer.Analyze(state, 10);
        TraceTiming("species_summaries");
        var speciesBehaviorFingerprints = SpeciesClusterAnalyzer.AnalyzeBehaviorFingerprints(state, 10);
        TraceTiming("species_behavior_fingerprints");
        var speciesBrainInputDiagnostics = SpeciesClusterAnalyzer.AnalyzeBrainInputDiagnostics(state, 10);
        TraceTiming("species_brain_inputs");
        var speciesHistory = SpeciesClusterAnalyzer.AnalyzeHistory(state, reportSnapshots, 10);
        TraceTiming("species_history");
        var speciesBehaviorChanges = SpeciesClusterAnalyzer.AnalyzeBehaviorChanges(state, speciesHistory, 10);
        TraceTiming("species_behavior_changes");
        var behaviorSummary = BehaviorAssay.Analyze(state);
        TraceTiming("behavior_assay");
        var lineageBehaviorSummaries = BehaviorAssay.AnalyzeTopFounderLineages(state, 10);
        TraceTiming("lineage_behavior_assay");
        var brainInputDiagnostics = BrainInputDiagnostics.Analyze(state);
        TraceTiming("brain_inputs");
        var lineageBrainInputDiagnostics = BrainInputDiagnostics.AnalyzeTopFounderLineages(state, 10);
        TraceTiming("lineage_brain_inputs");
        var founderSummaries = FounderSummaryCsvWriter.Summarize(state.LineageRecords)
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenByDescending(summary => summary.TotalCreatures)
            .ThenBy(summary => summary.FounderId.Value)
            .ToArray();
        TraceTiming("founder_summaries");
        var rosterSummaries = RosterLineageAnalyzer.Analyze(state.LineageRecords, speciesInjections, state.Tick)
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenByDescending(summary => summary.TotalCreatures)
            .ThenBy(summary => summary.ProfileName, StringComparer.Ordinal)
            .ToArray();
        TraceTiming("roster_summaries");
        var founderExplorationSummaries = SummarizeFounderExploration(state, 10);
        TraceTiming("founder_exploration");
        var generationSummaries = GenerationSummaryCsvWriter.Summarize(state.LineageRecords);
        TraceTiming("generation_summaries");
        var dominantLineageRows = LineageTrendCsvWriter.Summarize(reportSnapshots, state.LineageRecords, maxRowsPerSnapshot: 1);
        TraceTiming("dominant_lineages");
        var survivorAncestry = SurvivorLineageAnalyzer.Analyze(state);
        TraceTiming("survivor_ancestry");
        var traitSummary = state.Creatures.Count > 0
            ? TraitAccumulator.FromLivingCreatures(state)
            : default;
        TraceTiming("trait_summary");
        var biomeSummaries = state.Biomes.SummarizeResources(state.Resources);
        TraceTiming("biome_summaries");
        var seasonPressure = SeasonPressureAnalysis.Analyze(scenario, snapshots);
        TraceTiming("season_pressure");

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
        var hasSpeciesRoster = scenario.EnabledSpeciesSeeds().Any();
        WriteMetric(writer, "Scenario", scenario.Name);
        WriteMetric(writer, "Pipeline", scenario.PipelineKind.ToString());
        WriteMetric(writer, hasSpeciesRoster ? "Default brain architecture" : "Brain architecture", FormatBrainArchitectureKind(scenario.BrainArchitectureKind));
        WriteMetric(writer, hasSpeciesRoster ? "Default initial brain" : "Initial brain", FormatInitialBrainKind(scenario.InitialBrainKind));
        WriteMetric(writer, hasSpeciesRoster ? "Default brain hidden nodes" : "Brain hidden nodes", scenario.BrainHiddenNodeCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Seed", scenario.Seed.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Ticks requested", options.Ticks.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Final tick", state.Tick.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Simulated seconds", state.ElapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Wall time", $"{elapsed.TotalSeconds:0.###} seconds");
        WriteMetric(writer, "Snapshot interval", $"{scenario.StatsSnapshotIntervalTicks} ticks");
        WriteMetric(
            writer,
            "Extinct payload pruning",
            scenario.EnableExtinctPayloadPruning
                ? $"Every {scenario.ExtinctPayloadPruneIntervalTicks} ticks"
                : "Off");
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

        WriteMetric(writer, "Starting roster", FormatScenarioSpeciesSeeds(scenario, options.ScenarioPath));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        WriteScenarioSpeciesRosterSection(writer, scenario, options.ScenarioPath);

        var startingGenome = CreatureGenome.Baseline with
        {
            DietaryAdaptation = scenario.DietaryAdaptation,
            CarrionAdaptation = scenario.CarrionAdaptation,
            TenderPlantAdaptation = scenario.TenderPlantAdaptation,
            RichPlantAdaptation = scenario.RichPlantAdaptation,
            ToughPlantAdaptation = scenario.ToughPlantAdaptation,
            ThermalOptimum = scenario.ThermalOptimum,
            ThermalTolerance = scenario.ThermalTolerance
        };

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Pressure Settings</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Initial creatures", scenario.InitialCreatureCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Initial creature spawn", scenario.InitialCreatureSpawnRegion.ToString());
        WriteMetric(writer, "World sense interval", $"{scenario.WorldSenseIntervalTicks} ticks");
        WriteMetric(writer, "Close sense refresh", FormatPercent(scenario.CloseSenseRefreshProximity));
        WriteMetric(writer, "Close refresh minimum", $"{scenario.CloseSenseRefreshMinimumTicks} ticks");
        WriteMetric(writer, "Plant payoff trace half-life", $"{scenario.PlantPayoffTraceHalfLifeSeconds:0.###} seconds");
        WriteMetric(writer, "Initial resource density", $"{scenario.InitialResourcesPerMillionArea:0.###} per 1M area");
        WriteMetric(writer, "Initial resource patches", scenario.CalculateInitialResourceCount().ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Biomes", scenario.EnableBiomes ? "Enabled" : "Disabled");
        WriteMetric(writer, "Biome map", scenario.BiomeMapKind.ToString());
        WriteMetric(writer, "Biome cell size", scenario.BiomeCellSize.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Temperature", scenario.EnableTemperature ? "Enabled" : "Disabled");
        WriteMetric(writer, "Obstacles", scenario.EnableObstacles ? "Enabled" : "Disabled");
        WriteMetric(writer, "Obstacle map", scenario.ObstacleMapKind.ToString());
        WriteMetric(writer, "Obstacle cell size", scenario.ObstacleCellSize.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Resource void border", $"{scenario.ResourceVoidBorderWidth:0.###} world units");
        WriteMetric(writer, "Resource calories", FormatRange(scenario.ResourceCaloriesMin, scenario.ResourceCaloriesMax));
        WriteMetric(writer, "Resource regrowth", $"{FormatRange(scenario.ResourceRegrowthMin, scenario.ResourceRegrowthMax)} kcal/s");
        WriteMetric(writer, "Plant type mix", FormatPlantTypeMix(scenario));
        WriteMetric(writer, "Plant habitat affinity", scenario.EnablePlantTypeHabitatAffinity ? "Enabled" : "Disabled");
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
        WriteMetric(writer, "Local fertility", scenario.EnableLocalFertility ? "Enabled" : "Disabled");
        WriteMetric(writer, "Local fertility cell size", $"{scenario.LocalFertilityCellSize:0.###} world units");
        WriteMetric(writer, "Local fertility minimum", $"{scenario.LocalFertilityMinimumMultiplier:0.###}x");
        WriteMetric(writer, "Local fertility recovery", $"{scenario.LocalFertilityRecoveryPerSecond:0.######}/s");
        WriteMetric(writer, "Local fertility depletion", $"{scenario.LocalFertilityDepletionPerPlant:0.###}x per plant");
        WriteMetric(writer, "Local fertility spread", FormatPercent(scenario.LocalFertilityNeighborDepletionShare));
        WriteMetric(writer, "Biome movement costs", FormatBiomePressureProfile(scenario.CreateBiomeMovementCostProfile()));
        WriteMetric(writer, "Biome basal costs", FormatBiomePressureProfile(scenario.CreateBiomeBasalCostProfile()));
        WriteMetric(writer, "Biome speed", FormatBiomePressureProfile(scenario.CreateBiomeSpeedProfile()));
        WriteMetric(writer, "Biome vision range", FormatBiomePressureProfile(scenario.CreateBiomeVisionRangeProfile()));
        WriteMetric(writer, "Basal upkeep", $"{scenario.BasalEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Starting metabolic pace", $"{scenario.MetabolicPace:0.###}x");
        WriteMetric(writer, "Thermal mismatch basal cost", $"{scenario.ThermalMismatchBasalCostMultiplier:0.###}x at full mismatch");
        WriteMetric(writer, "Body radius upkeep", $"{scenario.BodyRadiusEnergyCostPerSecond:0.###} energy/radius/s");
        WriteMetric(writer, "Max speed upkeep", $"{scenario.MaxSpeedEnergyCostPerSecond:0.######} energy/speed/s");
        WriteMetric(writer, "Turn rate upkeep", $"{scenario.TurnRateEnergyCostPerSecond:0.######} energy/rad/s/s");
        WriteMetric(writer, "Sense radius upkeep", $"{scenario.SenseRadiusEnergyCostPerSecond:0.######} energy/radius/s");
        WriteMetric(writer, "Vision angle", $"{ToDegrees(scenario.VisionAngleRadians):0.###} degrees");
        WriteMetric(writer, "Vision angle upkeep", $"{scenario.VisionAngleEnergyCostPerSecond:0.######} cubic baseline/radian/s");
        WriteMetric(writer, "Eat rate upkeep", $"{scenario.EatRateEnergyCostPerSecond:0.######} energy/rate/s");
        WriteMetric(writer, "Gut capacity upkeep", $"{scenario.GutCapacityEnergyCostPerSecond:0.######} energy/capacity/s");
        WriteMetric(writer, "Digestion rate upkeep", $"{scenario.DigestionRateEnergyCostPerSecond:0.######} energy/rate/s");
        WriteMetric(writer, "Bite strength upkeep", $"{scenario.BiteStrengthEnergyCostPerSecond:0.######} energy/strength/s");
        WriteMetric(writer, "Damage resistance upkeep", $"{scenario.DamageResistanceEnergyCostPerSecond:0.######} energy/resistance/s");
        WriteMetric(writer, "Plant specialization upkeep", $"{scenario.PlantSpecializationEnergyCostPerSecond:0.######} energy/unit/s");
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
        WriteMetric(writer, "Starting plant adaptation", $"T {scenario.TenderPlantAdaptation:0.###}, R {scenario.RichPlantAdaptation:0.###}, Tough {scenario.ToughPlantAdaptation:0.###}");
        WriteMetric(writer, "Starting thermal genes", $"opt {FormatTemperatureIndex(scenario.ThermalOptimum)}, tol {FormatTemperatureIndex(scenario.ThermalTolerance)}");
        WriteMetric(writer, "Starting fat storage", $"capacity {scenario.FatStorageCapacityCalories:0.###}, efficiency {FormatPercent(scenario.FatStorageEfficiency)}");
        WriteMetric(writer, "Starting bite strength", scenario.BiteStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Starting damage resistance", scenario.DamageResistance.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Starting plant digestion", FormatPercent(CreatureDigestion.PlantEfficiency(startingGenome)));
        WriteMetric(writer, "Starting tender digestion", FormatPercent(CreatureDigestion.PlantTypeEnergyEfficiency(startingGenome, PlantResourceKind.Tender)));
        WriteMetric(writer, "Starting rich digestion", FormatPercent(CreatureDigestion.PlantTypeEnergyEfficiency(startingGenome, PlantResourceKind.Rich)));
        WriteMetric(writer, "Starting tough digestion", FormatPercent(CreatureDigestion.PlantTypeEnergyEfficiency(startingGenome, PlantResourceKind.Tough)));
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

        WriteEcologicalEventsSection(writer, state);

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Outcome</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Final population", state.Creatures.Count.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Eggs", state.Eggs.Count.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plants", finalSnapshot.PlantResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Meat", finalSnapshot.MeatResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plant calories", finalSnapshot.TotalPlantCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plant type calories", FormatPlantTypeCalories(finalSnapshot));
        WriteMetric(writer, "Meat calories", finalSnapshot.TotalMeatCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Dormant plants", finalSnapshot.DormantPlantResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg dormancy remaining", $"{finalSnapshot.AverageDormantPlantSecondsRemaining:0.###} seconds");
        WriteMetric(writer, "Plant patch occupied", FormatPercent(finalSnapshot.PlantPatchOccupiedCellShare));
        WriteMetric(writer, "Plant top-decile calories", FormatPercent(finalSnapshot.PlantPatchTopDecileCaloriesShare));
        WriteMetric(writer, "Plant patchiness", finalSnapshot.PlantPatchiness.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg local fertility", $"{finalSnapshot.AverageLocalFertilityMultiplier:0.###}x");
        WriteMetric(writer, "Min local fertility", $"{finalSnapshot.MinimumLocalFertilityMultiplier:0.###}x");
        WriteMetric(writer, "Depleted fertility cells", FormatPercent(finalSnapshot.DepletedLocalFertilityCellShare));
        WriteMetric(writer, "Temperature cells", finalSnapshot.TemperatureCellCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg map temperature", FormatTemperatureIndex(finalSnapshot.AverageMapTemperature));
        WriteMetric(writer, "Map temperature range", $"{FormatTemperatureIndex(finalSnapshot.MinimumMapTemperature)} - {FormatTemperatureIndex(finalSnapshot.MaximumMapTemperature)}");
        WriteMetric(writer, "Avg creature temperature", FormatTemperatureIndex(finalSnapshot.AverageCreatureTemperature));
        WriteMetric(writer, "Avg thermal optimum", FormatTemperatureIndex(finalSnapshot.AverageThermalOptimum));
        WriteMetric(writer, "Avg thermal tolerance", FormatTemperatureIndex(finalSnapshot.AverageThermalTolerance));
        WriteMetric(writer, "Avg thermal mismatch", FormatPercent(finalSnapshot.AverageCreatureThermalMismatch));
        WriteMetric(writer, "Hot/cold mismatch", $"{finalSnapshot.HotThermalMismatchCreatureCount} hot / {finalSnapshot.ColdThermalMismatchCreatureCount} cold");
        WriteMetric(writer, "Thermal basal cost", $"{finalSnapshot.ThermalBasalEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Thermal stress mix", $"{finalSnapshot.ComfortableThermalCreatureCount} comfortable / {finalSnapshot.ColdThermalStressCreatureCount} cold / {finalSnapshot.HotThermalStressCreatureCount} hot");
        WriteMetric(writer, "Temp-band creatures", $"{finalSnapshot.ColdTemperatureCreatureCount} cold / {finalSnapshot.TemperateTemperatureCreatureCount} temperate / {finalSnapshot.HotTemperatureCreatureCount} hot");
        WriteMetric(writer, "Temp-band plant kcal", $"{finalSnapshot.ColdTemperaturePlantCalories:0.#} cold / {finalSnapshot.TemperateTemperaturePlantCalories:0.#} temperate / {finalSnapshot.HotTemperaturePlantCalories:0.#} hot");
        WriteMetric(writer, "Temp-band births", $"{finalSnapshot.ColdTemperatureBirths:0.#} cold / {finalSnapshot.TemperateTemperatureBirths:0.#} temperate / {finalSnapshot.HotTemperatureBirths:0.#} hot");
        WriteMetric(writer, "Temp-band deaths", $"{finalSnapshot.ColdTemperatureDeaths:0.#} cold / {finalSnapshot.TemperateTemperatureDeaths:0.#} temperate / {finalSnapshot.HotTemperatureDeaths:0.#} hot");
        WriteMetric(writer, "Avg plant temperature", FormatTemperatureIndex(finalSnapshot.AveragePlantTemperature));
        WriteMetric(writer, "Avg small prey temperature", FormatTemperatureIndex(finalSnapshot.AverageSmallPreyTemperature));
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
        WriteMetric(writer, "Maturity", $"{FormatPercent(finalSnapshot.AverageMaturityProgress)} avg; {FormatPercent(Share(finalSnapshot.AdultCreatureCount, finalSnapshot.CreatureCount))} adult");
        WriteMetric(writer, "Reproduction intent", FormatPercent(Share(finalSnapshot.ReproductionIntentCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Ready to lay", FormatPercent(Share(finalSnapshot.ReproductionReadyCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Egg reserve", FormatPercent(finalSnapshot.AverageEggReserveRatio));
        WriteMetric(writer, "Energy surplus", FormatPercent(finalSnapshot.AverageEnergySurplusRatio));
        WriteMetric(writer, "Energy fullness", $"{FormatPercent(finalSnapshot.AverageEnergyFullnessRatio)} of {finalSnapshot.AverageEnergyCapacityCalories:0.###} kcal cap; {finalSnapshot.TotalEnergyOverflowCaloriesPerSecond:0.###}/s overflow");
        WriteMetric(writer, "Metabolic pace", $"{finalSnapshot.AverageMetabolicPace:0.###} avg; {finalSnapshot.LowMetabolicPaceCreatureCount} low / {finalSnapshot.NormalMetabolicPaceCreatureCount} normal / {finalSnapshot.HighMetabolicPaceCreatureCount} high");
        WriteMetric(writer, "Fat reserve", FormatPercent(finalSnapshot.AverageFatRatio));
        WriteMetric(writer, "Fat calories", finalSnapshot.TotalFatCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Fat mass burden", FormatPercent(finalSnapshot.AverageMassBurdenRatio));
        WriteMetric(writer, "Fat speed retained", $"{finalSnapshot.AverageFatSpeedMultiplier:0.###}x");
        WriteMetric(writer, "Fat genes", $"capacity {finalSnapshot.AverageFatStorageCapacityCalories:0.###}, efficiency {FormatPercent(finalSnapshot.AverageFatStorageEfficiency)}");
        WriteMetric(writer, "Fat flow", $"{finalSnapshot.TotalFatStoredCaloriesPerSecond:0.###}/s stored, {finalSnapshot.TotalFatReleasedCaloriesPerSecond:0.###}/s released");
        WriteMetric(writer, "Food success", FormatPercent(finalSnapshot.AverageRecentFoodSuccess));
        WriteMetric(writer, "Food energy yield", FormatPercent(finalSnapshot.AverageRecentFoodEnergyYield));
        WriteMetric(writer, "Plant payoff traces", FormatPlantPayoffTraces(finalSnapshot));
        WriteMetric(writer, "Active memory", $"{FormatPercent(Share(finalSnapshot.ActiveMemoryCreatureCount, finalSnapshot.CreatureCount))} ({finalSnapshot.ActiveMemoryCreatureCount})");
        WriteMetric(writer, "Memory strength", finalSnapshot.AverageMemoryStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Injury memory", $"{FormatPercent(Share(finalSnapshot.ActiveInjuryMemoryCreatureCount, finalSnapshot.CreatureCount))} ({finalSnapshot.ActiveInjuryMemoryCreatureCount}) @ {finalSnapshot.AverageInjuryMemoryStrength:0.###}");
        WriteMetric(writer, "Deaths", state.Stats.CreatureDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Starvation deaths", state.Stats.StarvationDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Injury deaths", state.Stats.InjuryDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Rotten meat deaths", state.Stats.RottenMeatDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Old age deaths", state.Stats.OldAgeDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average lifespan", $"{finalSnapshot.AverageLifespanSeconds:0.###} seconds");
        WriteMetric(writer, "Median lifespan", $"{finalSnapshot.MedianLifespanSeconds:0.###} seconds");
        WriteMetric(writer, "Max generation", finalSnapshot.MaxGeneration.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg hidden input weight", finalSnapshot.AverageBrainHiddenInputWeightMagnitude.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg hidden output weight", finalSnapshot.AverageBrainHiddenOutputWeightMagnitude.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Active hidden outputs", FormatPercent(finalSnapshot.ActiveBrainHiddenOutputShare));
        if (finalSnapshot.RtNeatBrainCount > 0)
        {
            WriteMetric(writer, "rtNEAT brains", $"{FormatPercent(finalSnapshot.RtNeatBrainShare)} ({finalSnapshot.RtNeatBrainCount})");
            WriteMetric(writer, "rtNEAT hidden nodes", $"{finalSnapshot.AverageRtNeatHiddenNodeCount:0.###} avg / {finalSnapshot.MaxRtNeatHiddenNodeCount} max");
            WriteMetric(writer, "rtNEAT connections", $"{finalSnapshot.AverageRtNeatConnectionCount:0.###} avg / {finalSnapshot.MaxRtNeatConnectionCount} max");
            WriteMetric(writer, "rtNEAT enabled conn", $"{finalSnapshot.AverageRtNeatEnabledConnectionCount:0.###} avg / {finalSnapshot.MaxRtNeatEnabledConnectionCount} max");
            WriteMetric(writer, "rtNEAT functional hidden", $"{finalSnapshot.AverageRtNeatFunctionalHiddenNodeCount:0.###} avg / {finalSnapshot.MaxRtNeatFunctionalHiddenNodeCount} max");
            WriteMetric(writer, "rtNEAT functional conn", $"{finalSnapshot.AverageRtNeatFunctionalConnectionCount:0.###} avg / {finalSnapshot.MaxRtNeatFunctionalConnectionCount} max");
            WriteMetric(writer, "rtNEAT disabled conn", $"{finalSnapshot.AverageRtNeatDisabledConnectionCount:0.###} avg / {finalSnapshot.MaxRtNeatDisabledConnectionCount} max");
            WriteMetric(writer, "rtNEAT longest path", $"{finalSnapshot.AverageRtNeatLongestPathLength:0.###} avg / {finalSnapshot.MaxRtNeatLongestPathLength} max");
        }
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
        WriteMetric(writer, "Touching meat", FormatPercent(Share(finalSnapshot.MeatContactCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Touching fresh meat", FormatPercent(Share(finalSnapshot.FreshMeatContactCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Touching stale meat", FormatPercent(Share(finalSnapshot.StaleMeatContactCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Meat contact not eating", FormatPercent(Share(finalSnapshot.MeatContactNotEatingCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "No meat eat: no intent", FormatPercent(Share(finalSnapshot.MeatContactNotEatingNoIntentCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "No meat eat: gut full", FormatPercent(Share(finalSnapshot.MeatContactNotEatingGutFullCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "No meat eat: storage full", FormatPercent(Share(finalSnapshot.MeatContactNotEatingStorageFullCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "No meat eat: stale", FormatPercent(Share(finalSnapshot.MeatContactNotEatingStaleCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "No meat eat: other", FormatPercent(Share(finalSnapshot.MeatContactNotEatingOtherCreatureCount, finalSnapshot.CreatureCount)));
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
        WriteMetric(writer, "Movement blocked (all)", FormatPercent(Share(finalSnapshot.ObstacleBlockedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Obstacle pressure", $"{finalSnapshot.AverageForwardObstacle:0.###} fwd / {finalSnapshot.AverageLeftObstacle:0.###} left / {finalSnapshot.AverageRightObstacle:0.###} right");
        WriteMetric(writer, "Calories eaten", $"{finalSnapshot.TotalCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Plant eaten", $"{finalSnapshot.TotalPlantCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Plant type eaten", FormatPlantTypeIntake(finalSnapshot));
        WriteMetric(writer, "Carcass eaten", $"{finalSnapshot.TotalCarcassCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Fresh meat eaten", $"{finalSnapshot.TotalFreshMeatCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Stale meat eaten", $"{finalSnapshot.TotalStaleMeatCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Rotten damage", $"{finalSnapshot.TotalRottenMeatDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Rotten affected", FormatPercent(Share(finalSnapshot.RottenMeatDamagedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Healing", $"{finalSnapshot.TotalHealthHealedPerSecond:0.###} health/s");
        WriteMetric(writer, "Healing energy", $"{finalSnapshot.TotalHealingEnergySpentPerSecond:0.###} energy/s");
        WriteMetric(writer, "Egg eaten", $"{finalSnapshot.TotalEggCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Fresh kill eaten", $"{finalSnapshot.TotalLivePreyCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Calories digested", $"{finalSnapshot.TotalCaloriesDigestedPerSecond:0.###} energy/s");
        WriteMetric(writer, "Plant energy", $"{finalSnapshot.TotalPlantDigestedEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Plant type energy", FormatPlantTypeDigestion(finalSnapshot));
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

        WritePlantTypeDiagnosticsSection(writer, finalSnapshot);

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Memory Diagnostics</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Active memory", $"{FormatPercent(Share(finalSnapshot.ActiveMemoryCreatureCount, finalSnapshot.CreatureCount))} ({finalSnapshot.ActiveMemoryCreatureCount})");
        WriteMetric(writer, "Avg memory strength", finalSnapshot.AverageMemoryStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Active injury memory", $"{FormatPercent(Share(finalSnapshot.ActiveInjuryMemoryCreatureCount, finalSnapshot.CreatureCount))} ({finalSnapshot.ActiveInjuryMemoryCreatureCount})");
        WriteMetric(writer, "Avg injury memory strength", finalSnapshot.AverageInjuryMemoryStrength.ToString("0.###", CultureInfo.InvariantCulture));
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
        WriteMetric(writer, "Similarity scent", $"{FormatPercent(Share(finalSnapshot.CreatureSimilarityScentDetectedCreatureCount, finalSnapshot.CreatureCount))} @ {finalSnapshot.AverageCreatureSimilarityScentDensity:0.###}");
        WriteMetric(writer, "Lineage scent", $"{FormatPercent(Share(finalSnapshot.CreatureLineageScentDetectedCreatureCount, finalSnapshot.CreatureCount))} @ {finalSnapshot.AverageCreatureLineageScentDensity:0.###}");
        WriteMetric(writer, "Lineage egg scent", $"{FormatPercent(Share(finalSnapshot.EggLineageScentDetectedCreatureCount, finalSnapshot.CreatureCount))} @ {finalSnapshot.AverageEggLineageScentDensity:0.###}");
        WriteMetric(writer, "Identity scent", $"{FormatPercent(Share(finalSnapshot.CreatureIdentityScentDetectedCreatureCount, finalSnapshot.CreatureCount))} @ {finalSnapshot.AverageCreatureIdentityScentDensity:0.###}");
        WriteMetric(writer, "Identity egg scent", $"{FormatPercent(Share(finalSnapshot.EggIdentityScentDetectedCreatureCount, finalSnapshot.CreatureCount))} @ {finalSnapshot.AverageEggIdentityScentDensity:0.###}");
        WriteMetric(writer, "Creature contact", FormatPercent(Share(finalSnapshot.CreatureContactCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Similar contact", $"{FormatPercent(Share(finalSnapshot.SimilarCreatureContactCreatureCount, finalSnapshot.CreatureCount))} avg {finalSnapshot.AverageCreatureContactSimilarity:0.###}");
        WriteMetric(writer, "Lineage contact", $"{FormatPercent(Share(finalSnapshot.LineageCreatureContactCreatureCount, finalSnapshot.CreatureCount))} avg {finalSnapshot.AverageCreatureContactLineageSimilarity:0.###}");
        WriteMetric(writer, "Lineage egg touch", $"{FormatPercent(Share(finalSnapshot.EggLineageContactCreatureCount, finalSnapshot.CreatureCount))} avg {finalSnapshot.AverageEggContactLineageSimilarity:0.###}");
        WriteMetric(writer, "Identity contact", $"{FormatPercent(Share(finalSnapshot.IdentityCreatureContactCreatureCount, finalSnapshot.CreatureCount))} avg {finalSnapshot.AverageCreatureContactIdentitySimilarity:0.###}");
        WriteMetric(writer, "Identity egg touch", $"{FormatPercent(Share(finalSnapshot.EggIdentityContactCreatureCount, finalSnapshot.CreatureCount))} avg {finalSnapshot.AverageEggContactIdentitySimilarity:0.###}");
        WriteMetric(writer, "Attack intent", FormatPercent(Share(finalSnapshot.AttackIntentCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Intent while touching", FormatPercent(Share(finalSnapshot.AttackIntentWhileTouchingCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Intent on similar touch", FormatPercent(Share(finalSnapshot.AttackIntentWhileTouchingSimilarCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Intent on lineage touch", FormatPercent(Share(finalSnapshot.AttackIntentWhileTouchingLineageCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Intent on unrelated touch", FormatPercent(Share(finalSnapshot.AttackIntentWhileTouchingUnrelatedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Touch no intent", FormatPercent(Share(finalSnapshot.AttackNoIntentContactCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Raw attack > 0", FormatPercent(Share(finalSnapshot.RawAttackPositiveCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Raw attack near gate", FormatPercent(Share(finalSnapshot.RawAttackNearGateCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Near gate while touching", FormatPercent(Share(finalSnapshot.RawAttackNearGateWhileTouchingCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Avg raw attack", finalSnapshot.AverageAttackOutput.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg touching attack", finalSnapshot.AverageTouchingAttackOutput.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Damage-dealing this tick", FormatPercent(Share(finalSnapshot.AttackingCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Attack damage", $"{finalSnapshot.TotalAttackDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Damage per attacker", $"{attackDamagePerAttacker:0.###} health/s");
        WriteMetric(writer, "Healing", $"{finalSnapshot.TotalHealthHealedPerSecond:0.###} health/s");
        WriteMetric(writer, "Healing creatures", $"{FormatPercent(Share(finalSnapshot.HealingCreatureCount, finalSnapshot.CreatureCount))} ({finalSnapshot.HealingCreatureCount})");
        WriteMetric(writer, "Healing energy", $"{finalSnapshot.TotalHealingEnergySpentPerSecond:0.###} energy/s");
        WriteMetric(writer, "Grab intent", FormatPercent(Share(finalSnapshot.GrabIntentCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Can grab", FormatPercent(Share(finalSnapshot.CanGrabCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Grab while touching", FormatPercent(Share(finalSnapshot.GrabIntentWhileCanGrabCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Grab off contact", FormatPercent(Share(finalSnapshot.GrabIntentWithoutCanGrabCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Holding", $"{FormatPercent(Share(finalSnapshot.HoldingCreatureCount, finalSnapshot.CreatureCount))} ({finalSnapshot.HoldingCreatureCount})");
        WriteMetric(writer, "Grabbed", $"{FormatPercent(Share(finalSnapshot.GrabbedCreatureCount, finalSnapshot.CreatureCount))} ({finalSnapshot.GrabbedCreatureCount})");
        WriteMetric(writer, "Avg grab output", finalSnapshot.AverageGrabOutput.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg touch grab output", finalSnapshot.AverageCanGrabGrabOutput.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg grab pressure", finalSnapshot.AverageGrabPressure.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg grab strength", finalSnapshot.AverageGrabStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Sound emitting", FormatPercent(Share(finalSnapshot.SoundEmittingCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Sound heard", FormatPercent(Share(finalSnapshot.SoundHeardCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Avg sound amp", finalSnapshot.AverageSoundAmplitude.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg sound density", finalSnapshot.AverageSoundDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg sound clarity", finalSnapshot.AverageSoundToneClarity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Injury deaths", state.Stats.InjuryDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Rotten meat deaths", state.Stats.RottenMeatDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Old age deaths", state.Stats.OldAgeDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Fresh kill share", FormatPercent(finalSnapshot.FreshKillCaloriesEatenShare));
        WriteMetric(writer, "Meat raw share", FormatPercent(finalSnapshot.MeatCaloriesEatenShare));
        WriteMetric(writer, "Fresh meat share", FormatPercent(finalSnapshot.FreshMeatCaloriesEatenShare));
        WriteMetric(writer, "Stale meat share", FormatPercent(finalSnapshot.StaleMeatCaloriesEatenShare));
        WriteMetric(writer, "Rotten damage", $"{finalSnapshot.TotalRottenMeatDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Rotten affected", FormatPercent(Share(finalSnapshot.RottenMeatDamagedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Meat energy share", FormatPercent(finalSnapshot.MeatDigestedEnergyShare));
        WriteMetric(writer, "Average diet", finalSnapshot.AverageDietaryAdaptation.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average carrion", finalSnapshot.AverageCarrionAdaptation.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average plant adaptation", $"T {finalSnapshot.AverageTenderPlantAdaptation:0.###}, R {finalSnapshot.AverageRichPlantAdaptation:0.###}, Tough {finalSnapshot.AverageToughPlantAdaptation:0.###}");
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

        WriteCollisionDiagnosticsSection(writer, snapshots, finalSnapshot);

        WriteBiomeMapSection(writer, state.Biomes);
        WriteTemperatureMapSection(writer, state);

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Biomes</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Biome</th><th>Area</th><th>Area Share</th><th>Density Mult</th><th>Regrowth Mult</th><th>Season Amp</th><th>Move Cost</th><th>Basal Cost</th><th>Speed</th><th>Vision</th><th>Resources</th><th>Resources/M</th><th>Plant kcal</th><th>Meat kcal</th><th>Eaten/s</th><th>Deaths</th><th>Living</th><th>Living Share</th></tr></thead>");
        writer.WriteLine("<tbody>");
        var movementCostProfile = scenario.CreateBiomeMovementCostProfile();
        var basalCostProfile = scenario.CreateBiomeBasalCostProfile();
        var speedProfile = scenario.CreateBiomeSpeedProfile();
        var visionProfile = scenario.CreateBiomeVisionRangeProfile();
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
            var vision = visionProfile.For(summary.Kind).ToString("0.###", CultureInfo.InvariantCulture);
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
                $"<td>{Html($"{vision}x")}</td>" +
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

        WriteBiomePreferenceSection(
            writer,
            snapshots,
            biomeSummaries,
            MathF.Max(1f, state.Bounds.Width * state.Bounds.Height));
        WriteBiomeExposureSection(
            writer,
            biomeSummaries,
            state.Stats.SpatialHeatmaps,
            MathF.Max(1f, state.Bounds.Width * state.Bounds.Height));
        WriteBiomeRiskRewardSection(
            writer,
            snapshots,
            biomeSummaries,
            MathF.Max(1f, state.Bounds.Width * state.Bounds.Height));
        WriteBiomePreferenceByGenerationSection(
            writer,
            state.Creatures,
            state.Biomes,
            biomeSummaries,
            MathF.Max(1f, state.Bounds.Width * state.Bounds.Height));

        WriteDeathCausesByBiomeSection(writer, state.Stats.CreatureDeathCausesByBiome);
        WriteSpatialHeatmapSection(writer, state.Biomes, state.Stats.SpatialHeatmaps);

        WriteChartsSection(writer, reportSnapshots, snapshots.Count);
        WriteThermalNicheSection(writer, state, founderSummaries, speciesSummaries, finalSnapshot);

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
        WriteTrendRow(writer, "Collision damage", collisionDamageTrend, "health/s");
        WriteTrendRow(writer, "Healing", healingTrend, "health/s");
        WriteTrendRow(writer, "Healing energy", healingEnergyTrend, "energy/s");
        WriteTrendRow(writer, "Time since meal", mealGapTrend, "s avg");
        WriteTrendRow(writer, "Distance since meal", mealDistanceTrend, "units avg");
        WriteTrendRow(writer, "Distance moved", distanceTraveledTrend, "units/s");
        WriteTrendRow(writer, "Calories per distance", caloriesPerDistanceTrend, "kcal/unit");
        WriteTrendRow(writer, "Calories per food vision", caloriesPerFoodVisionTrend, "kcal/event");
        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        WriteSpeciesClusterSection(writer, speciesSummaries);
        WriteSpeciesBehaviorFingerprintSection(writer, speciesBehaviorFingerprints);
        WriteSpeciesBrainInputDiagnosticsSection(writer, speciesBrainInputDiagnostics);
        WriteSpeciesBehaviorChangeSection(writer, speciesBehaviorChanges);
        WriteSpeciesClusterInterpretationSection(writer, SpeciesClusterAnalyzer.InterpretClusters(speciesSummaries, speciesHistory, 10));
        WriteSpeciesClusterHistorySection(writer, speciesHistory);
        WriteBehaviorAssaySection(writer, behaviorSummary);
        WriteLineageBehaviorAssaySection(writer, lineageBehaviorSummaries);
        WriteBrainInputDiagnosticsSection(writer, brainInputDiagnostics);
        WriteLineageBrainInputDiagnosticsSection(writer, lineageBrainInputDiagnostics);
        WriteRtNeatBrainGraphSection(writer, state);

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

        WriteSurvivorLineageTreeSection(writer, survivorAncestry, state, speciesHistory);

        if (rosterSummaries.Length > 0)
        {
            writer.WriteLine("<section>");
            writer.WriteLine("<h2>Injected Profile Lineages</h2>");
            writer.WriteLine("<div class=\"table-wrap\"><table>");
            writer.WriteLine("<thead><tr><th>Profile</th><th>Founders</th><th>Total</th><th>Descendants</th><th>Living</th><th>Tail Living</th><th>kcal/s</th><th>Meat</th><th>Fresh Kill</th><th>Meat Seen</th><th>Attack</th><th>Touch Attack</th><th>Damage/s</th><th>Rot Damage/s</th><th>Extinct At</th><th>Dead</th><th>Max Generation</th><th>Starved</th><th>Injury</th><th>Same-Profile Injury</th><th>Other-Profile Injury</th><th>Unattributed Injury</th><th>Cross Kills Dealt</th><th>Same Kills Dealt</th><th>Rotten</th><th>Old Age</th><th>Other</th></tr></thead>");
            writer.WriteLine("<tbody>");
            foreach (var summary in rosterSummaries)
            {
                writer.WriteLine(
                    "<tr>" +
                    $"<td>{Html(summary.ProfileName)}</td>" +
                    $"<td>{Html(summary.FounderCount)}</td>" +
                    $"<td>{Html(summary.TotalCreatures)}</td>" +
                    $"<td>{Html(summary.DescendantCount)}</td>" +
                    $"<td>{Html(summary.LivingCreatures)}</td>" +
                    $"<td>{Html(summary.TailAverageLivingCreatures.ToString("0.0", CultureInfo.InvariantCulture))}</td>" +
                    $"<td>{Html(summary.CaloriesEatenPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                    $"<td>{Html(FormatPercent(summary.MeatCaloriesEatenShare))}</td>" +
                    $"<td>{Html(FormatPercent(summary.FreshKillCaloriesEatenShare))}</td>" +
                    $"<td>{Html(FormatPercent(summary.MeatDetectedShare))}</td>" +
                    $"<td>{Html(FormatPercent(summary.AttackIntentShare))}</td>" +
                    $"<td>{Html(FormatPercent(summary.AttackIntentTouchingShare))}</td>" +
                    $"<td>{Html(summary.AttackDamageDealtPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                    $"<td>{Html(summary.RottenMeatDamagePerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                    $"<td>{Html(FormatOptionalTick(summary.ExtinctionTick))}</td>" +
                    $"<td>{Html(summary.DeadCreatures)}</td>" +
                    $"<td>{Html(summary.MaxGeneration)}</td>" +
                    $"<td>{Html(summary.StarvationDeaths)}</td>" +
                    $"<td>{Html(summary.InjuryDeaths)}</td>" +
                    $"<td>{Html(summary.InjuryDeathsFromSameProfile)}</td>" +
                    $"<td>{Html(summary.InjuryDeathsFromOtherProfile)}</td>" +
                    $"<td>{Html(summary.InjuryDeathsFromUnknownProfile)}</td>" +
                    $"<td>{Html(summary.CrossProfileInjuryKillsDealt)}</td>" +
                    $"<td>{Html(summary.SameProfileInjuryKillsDealt)}</td>" +
                    $"<td>{Html(summary.RottenMeatDeaths)}</td>" +
                    $"<td>{Html(summary.OldAgeDeaths)}</td>" +
                    $"<td>{Html(summary.UnknownDeaths)}</td>" +
                    "</tr>");
            }

            writer.WriteLine("</tbody></table></div>");
            writer.WriteLine("</section>");
        }

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Top Founder Lineages</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Founder</th><th>Total</th><th>Descendants</th><th>Living</th><th>Dead</th><th>Max Generation</th><th>Thermal Niche</th><th>Avg Temp</th><th>Mismatch</th><th>Cold/Temp/Hot</th><th>Cold/Hot Stress</th></tr></thead>");
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
                $"<td>{Html(summary.ThermalNiche.NicheLabel)}</td>" +
                $"<td>{Html(summary.ThermalNiche.AverageOccupiedTemperature.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ThermalNiche.AverageThermalMismatch.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatThermalShares(summary.ThermalNiche))}</td>" +
                $"<td>{Html(FormatStressShares(summary.ThermalNiche))}</td>" +
                "</tr>");
        }

        if (founderSummaries.Length == 0)
        {
            WriteEmptyRow(writer, 11, "No founder records were present.");
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
        writer.WriteLine("<thead><tr><th>Generation</th><th>Births</th><th>Living</th><th>Dead</th><th>Starvation Deaths</th><th>Injury Deaths</th><th>Rotten Meat Deaths</th><th>Old Age Deaths</th><th>Survival Rate</th></tr></thead>");
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
                $"<td>{Html(summary.OldAgeDeaths)}</td>" +
                $"<td>{Html(FormatPercent(summary.SurvivalRate))}</td>" +
                "</tr>");
        }

        if (generationSummaries.Count == 0)
        {
            WriteEmptyRow(writer, 9, "No generation records were present.");
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
            WriteTraitRow(writer, "Metabolic pace", traitSummary.MetabolicPace);
            WriteTraitRow(writer, "Reproduction threshold", traitSummary.ReproductionThreshold);
            WriteTraitRow(writer, "Offspring investment", traitSummary.OffspringInvestment);
            WriteTraitRow(writer, "Egg production per second", traitSummary.EggProductionEnergyPerSecond);
            WriteTraitRow(writer, "Egg incubation seconds", traitSummary.EggIncubationSeconds);
            WriteTraitRow(writer, "Maturity age seconds", traitSummary.MaturityAgeSeconds);
            WriteTraitRow(writer, "Dietary adaptation meat bias", traitSummary.DietaryAdaptation);
            WriteTraitRow(writer, "Carrion adaptation stale bias", traitSummary.CarrionAdaptation);
            WriteTraitRow(writer, "Tender plant adaptation", traitSummary.TenderPlantAdaptation);
            WriteTraitRow(writer, "Rich plant adaptation", traitSummary.RichPlantAdaptation);
            WriteTraitRow(writer, "Tough plant adaptation", traitSummary.ToughPlantAdaptation);
            WriteTraitRow(writer, "Plant digestion efficiency", traitSummary.PlantDigestion);
            WriteTraitRow(writer, "Meat digestion efficiency", traitSummary.MeatDigestion);
            WriteTraitRow(writer, "Fresh meat digestion efficiency", traitSummary.FreshMeatDigestion);
            WriteTraitRow(writer, "Stale meat digestion efficiency", traitSummary.StaleMeatDigestion);
            WriteTraitRow(writer, "Bite strength", traitSummary.BiteStrength);
            WriteTraitRow(writer, "Damage resistance", traitSummary.DamageResistance);
            WriteTraitRow(writer, "Thermal optimum", traitSummary.ThermalOptimum);
            WriteTraitRow(writer, "Thermal tolerance", traitSummary.ThermalTolerance);
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
        WriteOptionalPath(writer, "Species clusters CSV", outputPaths.SpeciesSummaryPath);
        WriteOptionalPath(writer, "Species trends CSV", outputPaths.SpeciesTrendPath);
        WriteOptionalPath(writer, "Founders CSV", outputPaths.FounderSummaryPath);
        WriteOptionalPath(writer, "Thermal ecotypes CSV", outputPaths.ThermalEcotypeSummaryPath);
        WriteOptionalPath(writer, "Generations CSV", outputPaths.GenerationSummaryPath);
        WriteOptionalPath(writer, "Lineage trends CSV", outputPaths.LineageTrendPath);
        WriteOptionalPath(writer, "Roster lineages CSV", outputPaths.RosterSummaryPath);
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
              width: min(1760px, calc(100% - 32px));
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
            .biome-map-note {
              margin: 0 0 12px;
              color: var(--muted);
              font-size: 0.9rem;
            }
            .biome-map-frame {
              overflow: auto;
              padding: 10px;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #eef3e8;
            }
            .biome-map {
              display: block;
              width: 100%;
              max-height: 620px;
              height: auto;
            }
            .biome-map-void {
              fill: none;
              stroke: rgba(31, 38, 31, 0.68);
              stroke-width: 8;
              stroke-dasharray: 26 18;
              vector-effect: non-scaling-stroke;
            }
            .biome-legend {
              display: flex;
              flex-wrap: wrap;
              gap: 8px 14px;
              margin-top: 10px;
              color: var(--muted);
              font-size: 0.86rem;
            }
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
              cursor: zoom-in;
            }
            .chart-card:focus-visible {
              outline: 2px solid var(--accent);
              outline-offset: 3px;
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
            .rtneat-panel {
              display: grid;
              grid-template-columns: minmax(0, 1fr) minmax(220px, 320px);
              gap: 14px;
              align-items: start;
              margin-top: 14px;
            }
            .rtneat-graph-frame {
              overflow: auto;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #fbfcf8;
            }
            .rtneat-graph {
              display: block;
              min-width: 920px;
              width: 100%;
              height: auto;
              font-family: "Segoe UI", system-ui, sans-serif;
            }
            .rtneat-graph text {
              font-size: 10px;
              fill: var(--ink);
              pointer-events: none;
            }
            .rtneat-node-label { font-weight: 650; }
            .rtneat-node-kind {
              fill: var(--muted);
              font-size: 8px;
              text-transform: uppercase;
            }
            .rtneat-detail {
              border: 1px solid var(--line);
              border-radius: 6px;
              padding: 12px;
              background: #fbfcf8;
            }
            .rtneat-detail h3 {
              margin: 0 0 10px;
              font-size: 1rem;
            }
            .rtneat-detail dl {
              display: grid;
              grid-template-columns: auto 1fr;
              gap: 6px 12px;
              margin: 0;
            }
            .rtneat-detail dt {
              color: var(--muted);
              font-size: 0.78rem;
              text-transform: uppercase;
            }
            .rtneat-detail dd {
              margin: 0;
              font-weight: 650;
              overflow-wrap: anywhere;
            }
            .lineage-tree-frame {
              position: relative;
              overflow: auto;
              padding: 10px;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #fbfcf8;
            }
            .lineage-toolbar {
              display: flex;
              flex-wrap: wrap;
              gap: 8px;
              align-items: center;
              justify-content: space-between;
              margin: 0 0 8px;
              color: var(--muted);
              font-size: 0.82rem;
            }
            .lineage-toolbar button {
              padding: 5px 9px;
              border: 1px solid var(--line);
              border-radius: 5px;
              background: #fff;
              color: var(--ink);
              font: inherit;
              cursor: pointer;
            }
            .lineage-report-grid {
              display: grid;
              grid-template-columns: minmax(0, 1fr) minmax(300px, 380px);
              gap: 14px;
              margin-top: 14px;
              align-items: start;
            }
            .lineage-tree {
              display: block;
              width: 100%;
              max-width: 100%;
              height: auto;
              font-family: "Segoe UI", system-ui, sans-serif;
              touch-action: none;
              user-select: none;
            }
            .lineage-tree text {
              fill: var(--muted);
              font-size: 11px;
            }
            .lineage-segment-node {
              cursor: pointer;
            }
            .lineage-segment-node text {
              pointer-events: none;
            }
            .lineage-segment-node rect {
              transition: stroke-width 0.12s ease, filter 0.12s ease;
            }
            .lineage-segment-node.is-selected rect,
            .lineage-segment-node:focus-visible rect {
              stroke: #172015;
              stroke-width: 3;
              filter: drop-shadow(0 2px 4px rgba(23, 32, 21, 0.24));
            }
            .lineage-detail-panel {
              padding: 12px;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #fbfcf8;
            }
            .lineage-detail-panel h3 {
              margin: 0 0 8px;
              font-size: 1rem;
            }
            .lineage-detail-panel dl {
              display: grid;
              grid-template-columns: auto 1fr;
              gap: 6px 10px;
              margin: 0;
            }
            .lineage-detail-panel dt {
              color: var(--muted);
              font-size: 0.78rem;
              text-transform: uppercase;
            }
            .lineage-detail-panel dd {
              margin: 0;
              font-weight: 650;
              overflow-wrap: anywhere;
            }
            .lineage-detail-panel dd + dt {
              margin-top: 3px;
            }
            .lineage-tree-legend {
              display: flex;
              flex-wrap: wrap;
              gap: 8px 14px;
              margin-top: 10px;
              color: var(--muted);
              font-size: 0.84rem;
            }
            @media (max-width: 820px) {
              .lineage-report-grid { grid-template-columns: 1fr; }
            }
            .heatmap-grid {
              display: grid;
              grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
              gap: 16px;
            }
            .heatmap-card p {
              margin: 0 0 8px;
              color: var(--muted);
              font-size: 0.84rem;
            }
            .heatmap {
              width: 100%;
              max-height: 420px;
              height: auto;
              display: block;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #eef3e8;
            }
            .heatmap-legend {
              display: flex;
              flex-wrap: wrap;
              gap: 8px 12px;
              margin-top: 8px;
              color: var(--muted);
              font-size: 0.82rem;
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
            .chart-series-line {
              transition: opacity 0.12s ease, stroke-width 0.12s ease;
            }
            .chart-legend-item {
              padding: 1px 3px;
              border-radius: 4px;
              transition: opacity 0.12s ease, background-color 0.12s ease, color 0.12s ease;
            }
            .chart-card.is-series-highlighted .chart-series-line {
              opacity: 0.18;
              stroke-width: 1.6;
            }
            .chart-card.is-series-highlighted .chart-series-line.is-highlighted {
              opacity: 1;
              stroke-width: 4.8;
            }
            .chart-card.is-series-highlighted .chart-legend-item {
              opacity: 0.48;
            }
            .chart-card.is-series-highlighted .chart-legend-item.is-highlighted {
              opacity: 1;
              color: var(--accent-dark, var(--ink));
              background: #eef2e9;
            }
            .legend-swatch {
              display: inline-block;
              width: 10px;
              height: 10px;
              margin-right: 4px;
              border-radius: 2px;
              vertical-align: -1px;
            }
            body.chart-lightbox-open { overflow: hidden; }
            .chart-lightbox[hidden] { display: none; }
            .chart-lightbox {
              position: fixed;
              inset: 0;
              z-index: 1000;
              display: flex;
              align-items: center;
              justify-content: center;
              padding: clamp(16px, 3vw, 36px);
              background: rgba(22, 32, 21, 0.72);
            }
            .chart-lightbox-panel {
              width: min(1180px, 100%);
              max-height: calc(100vh - 48px);
              overflow: auto;
              padding: 16px;
              border: 1px solid var(--line);
              border-radius: 8px;
              background: var(--panel);
              box-shadow: 0 24px 80px rgba(0, 0, 0, 0.28);
            }
            .chart-lightbox-close {
              display: block;
              margin: 0 0 12px auto;
              padding: 6px 10px;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #fbfcf8;
              color: var(--ink);
              font: inherit;
              cursor: pointer;
            }
            .chart-lightbox-content .chart-card {
              padding: 0;
              border: 0;
              background: transparent;
              cursor: default;
            }
            .chart-lightbox-content .chart-card svg {
              width: 100%;
              max-height: 72vh;
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
        writer.WriteLine(
            """
            <script>
            (() => {
              const cards = Array.from(document.querySelectorAll(".chart-card"));
              if (cards.length === 0) {
                return;
              }

              const overlay = document.createElement("div");
              overlay.className = "chart-lightbox";
              overlay.hidden = true;
              overlay.setAttribute("role", "dialog");
              overlay.setAttribute("aria-modal", "true");
              overlay.innerHTML = "<div class=\"chart-lightbox-panel\"><button class=\"chart-lightbox-close\" type=\"button\" aria-label=\"Close enlarged chart\">Close</button><div class=\"chart-lightbox-content\"></div></div>";
              document.body.appendChild(overlay);

              const content = overlay.querySelector(".chart-lightbox-content");
              const closeButton = overlay.querySelector(".chart-lightbox-close");
              let previousFocus = null;

              function setSeriesHighlight(legendItem) {
                const card = legendItem.closest(".chart-card");
                if (!card) {
                  return;
                }

                const seriesIndex = legendItem.getAttribute("data-series-index");
                card.classList.add("is-series-highlighted");
                for (const line of card.querySelectorAll(".chart-series-line")) {
                  line.classList.toggle("is-highlighted", line.getAttribute("data-series-index") === seriesIndex);
                }

                for (const item of card.querySelectorAll(".chart-legend-item")) {
                  item.classList.toggle("is-highlighted", item.getAttribute("data-series-index") === seriesIndex);
                }
              }

              function clearSeriesHighlight(legendItem) {
                const card = legendItem.closest(".chart-card");
                if (!card) {
                  return;
                }

                card.classList.remove("is-series-highlighted");
                for (const active of card.querySelectorAll(".is-highlighted")) {
                  active.classList.remove("is-highlighted");
                }
              }

              function openChart(card) {
                previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
                content.replaceChildren(card.cloneNode(true));
                const clone = content.querySelector(".chart-card");
                if (clone) {
                  clone.removeAttribute("role");
                  clone.removeAttribute("tabindex");
                  clone.removeAttribute("aria-label");
                }

                overlay.hidden = false;
                document.body.classList.add("chart-lightbox-open");
                closeButton.focus();
              }

              function closeChart() {
                overlay.hidden = true;
                content.replaceChildren();
                document.body.classList.remove("chart-lightbox-open");
                if (previousFocus) {
                  previousFocus.focus();
                }
              }

              for (const card of cards) {
                card.addEventListener("click", () => openChart(card));
                card.addEventListener("keydown", event => {
                  if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    openChart(card);
                  }
                });
              }

              document.addEventListener("pointerover", event => {
                const item = event.target.closest(".chart-legend-item");
                if (item) {
                  setSeriesHighlight(item);
                }
              });
              document.addEventListener("pointerout", event => {
                const item = event.target.closest(".chart-legend-item");
                if (item && !item.contains(event.relatedTarget)) {
                  clearSeriesHighlight(item);
                }
              });
              closeButton.addEventListener("click", closeChart);
              overlay.addEventListener("click", event => {
                if (event.target === overlay) {
                  closeChart();
                }
              });
              document.addEventListener("keydown", event => {
                if (!overlay.hidden && event.key === "Escape") {
                  closeChart();
                }
              });
            })();
            </script>
            """);
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

    private static void WriteEcologicalEventsSection(StreamWriter writer, WorldState state)
    {
        var events = state.EcologicalEvents;
        var activeCount = events.Count(ecologicalEvent => ecologicalEvent.IsActive(state.ElapsedSeconds));

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Ecological Events</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Scheduled events", events.Count.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Active now", activeCount.ToString(CultureInfo.InvariantCulture));
        writer.WriteLine("</div>");

        if (events.Count == 0)
        {
            writer.WriteLine("<p>No ecological events scheduled.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<table>");
        writer.WriteLine("<thead><tr><th>Name</th><th>Kind</th><th>Status</th><th>Start</th><th>Duration</th><th>Region</th><th>Strength</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var ecologicalEvent in events.OrderBy(ecologicalEvent => ecologicalEvent.StartSeconds).ThenBy(ecologicalEvent => ecologicalEvent.Name, StringComparer.Ordinal))
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(ecologicalEvent.Name)}</td>" +
                $"<td>{Html(ecologicalEvent.Kind)}</td>" +
                $"<td>{Html(FormatEcologicalEventStatus(ecologicalEvent, state.ElapsedSeconds))}</td>" +
                $"<td>{Html($"{ecologicalEvent.StartSeconds:0.###} s")}</td>" +
                $"<td>{Html($"{ecologicalEvent.DurationSeconds:0.###} s")}</td>" +
                $"<td>{Html(FormatEcologicalEventRegion(ecologicalEvent))}</td>" +
                $"<td>{Html(FormatEcologicalEventStrength(ecologicalEvent))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody>");
        writer.WriteLine("</table>");
        writer.WriteLine("</section>");
    }

    private static string FormatEcologicalEventStatus(EcologicalEventDefinition ecologicalEvent, double elapsedSeconds)
    {
        if (ecologicalEvent.IsActive(elapsedSeconds))
        {
            return $"active ({Math.Max(0d, ecologicalEvent.EndSeconds - elapsedSeconds):0.#} s left)";
        }

        if (elapsedSeconds < ecologicalEvent.StartSeconds)
        {
            return $"pending ({ecologicalEvent.StartSeconds - elapsedSeconds:0.#} s)";
        }

        return "completed";
    }

    private static string FormatEcologicalEventRegion(EcologicalEventDefinition ecologicalEvent)
    {
        return $"x {FormatPercent(ecologicalEvent.RegionX)}-{FormatPercent(ecologicalEvent.RegionX + ecologicalEvent.RegionWidth)}, y {FormatPercent(ecologicalEvent.RegionY)}-{FormatPercent(ecologicalEvent.RegionY + ecologicalEvent.RegionHeight)}";
    }

    private static string FormatEcologicalEventStrength(EcologicalEventDefinition ecologicalEvent)
    {
        if (ecologicalEvent.AffectsFertility)
        {
            return $"{ecologicalEvent.Strength:0.###}x fertility";
        }

        var sign = ecologicalEvent.Kind == EcologicalEventKind.ColdSnap ? "-" : "+";
        return $"{sign}{ecologicalEvent.Strength * 100f:0.#} temperature index";
    }

    private static void WriteBiomeMapSection(TextWriter writer, BiomeMap map)
    {
        var width = MathF.Max(1f, map.Bounds.Width);
        var height = MathF.Max(1f, map.Bounds.Height);
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Biome Layout</h2>");
        writer.WriteLine(
            $"<p class=\"biome-map-note\">{Html(map.CellCountX)} x {Html(map.CellCountY)} cells at {Html(map.CellSize.ToString("0.###", CultureInfo.InvariantCulture))} world units per cell. Dashed outline marks the resource spawn area when a void border is configured.</p>");
        writer.WriteLine("<div class=\"biome-map-frame\">");
        writer.WriteLine($"<svg class=\"biome-map\" viewBox=\"0 0 {SvgNumber(width)} {SvgNumber(height)}\" role=\"img\" aria-label=\"Biome map layout\" preserveAspectRatio=\"xMidYMid meet\" shape-rendering=\"crispEdges\">");
        for (var y = 0; y < map.CellCountY; y++)
        {
            for (var x = 0; x < map.CellCountX; x++)
            {
                var cell = map.GetCellBounds(x, y);
                if (cell.Width <= 0f || cell.Height <= 0f)
                {
                    continue;
                }

                var kind = map.GetKind(x, y);
                writer.WriteLine(
                    $"<rect x=\"{SvgNumber(cell.X)}\" y=\"{SvgNumber(cell.Y)}\" width=\"{SvgNumber(cell.Width)}\" height=\"{SvgNumber(cell.Height)}\" fill=\"{Html(BiomeColor(kind))}\" />");
            }
        }

        if (map.ResourceVoidBorderWidth > 0f
            && map.ResourceVoidBorderWidth * 2f < width
            && map.ResourceVoidBorderWidth * 2f < height)
        {
            var border = map.ResourceVoidBorderWidth;
            writer.WriteLine(
                $"<rect class=\"biome-map-void\" x=\"{SvgNumber(border)}\" y=\"{SvgNumber(border)}\" width=\"{SvgNumber(width - border * 2f)}\" height=\"{SvgNumber(height - border * 2f)}\" />");
        }

        writer.WriteLine("</svg>");
        writer.WriteLine("</div>");
        writer.WriteLine("<div class=\"biome-legend\">");
        foreach (var biome in BiomeKinds.All)
        {
            writer.WriteLine(
                $"<span><span class=\"legend-swatch\" style=\"background:{Html(BiomeColor(biome))}\"></span>{Html(FormatBiomeKind(biome))}</span>");
        }

        writer.WriteLine("</div>");
        writer.WriteLine("</section>");
    }

    private static void WriteTemperatureMapSection(TextWriter writer, WorldState state)
    {
        var map = state.Temperature;
        var width = MathF.Max(1f, map.Bounds.Width);
        var height = MathF.Max(1f, map.Bounds.Height);
        var summary = state.SummarizeEffectiveTemperature();
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Temperature Layout</h2>");
        writer.WriteLine(
            $"<p class=\"biome-map-note\">{Html(map.CellCountX)} x {Html(map.CellCountY)} cells at {Html(map.CellSize.ToString("0.###", CultureInfo.InvariantCulture))} world units per cell. Temperature index runs 0 cold, 50 temperate, 100 hot. Values include currently active ecological temperature events. Average {Html(FormatTemperatureIndex(summary.AverageTemperature))}, range {Html(FormatTemperatureIndex(summary.MinimumTemperature))} - {Html(FormatTemperatureIndex(summary.MaximumTemperature))}.</p>");
        writer.WriteLine("<div class=\"biome-map-frame\">");
        writer.WriteLine($"<svg class=\"biome-map\" viewBox=\"0 0 {SvgNumber(width)} {SvgNumber(height)}\" role=\"img\" aria-label=\"Temperature map layout\" preserveAspectRatio=\"xMidYMid meet\" shape-rendering=\"crispEdges\">");
        for (var y = 0; y < map.CellCountY; y++)
        {
            for (var x = 0; x < map.CellCountX; x++)
            {
                var cell = map.GetCellBounds(x, y);
                if (cell.Width <= 0f || cell.Height <= 0f)
                {
                    continue;
                }

                var position = new SimVector2(cell.X + cell.Width * 0.5f, cell.Y + cell.Height * 0.5f);
                writer.WriteLine(
                    $"<rect x=\"{SvgNumber(cell.X)}\" y=\"{SvgNumber(cell.Y)}\" width=\"{SvgNumber(cell.Width)}\" height=\"{SvgNumber(cell.Height)}\" fill=\"{Html(TemperatureColor(state.GetTemperatureAt(position)))}\" />");
            }
        }

        writer.WriteLine("</svg>");
        writer.WriteLine("</div>");
        writer.WriteLine("<div class=\"biome-legend\">");
        writer.WriteLine($"<span><span class=\"legend-swatch\" style=\"background:{Html(TemperatureColor(0f))}\"></span>cold</span>");
        writer.WriteLine($"<span><span class=\"legend-swatch\" style=\"background:{Html(TemperatureColor(0.5f))}\"></span>temperate</span>");
        writer.WriteLine($"<span><span class=\"legend-swatch\" style=\"background:{Html(TemperatureColor(1f))}\"></span>hot</span>");
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpatialHeatmapSection(
        TextWriter writer,
        BiomeMap biomeMap,
        SimulationSpatialHeatmaps heatmaps)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Spatial Heatmaps</h2>");
        if (heatmaps.CellCountX <= 0
            || heatmaps.CellCountY <= 0
            || !heatmaps.HasData)
        {
            writer.WriteLine("<p class=\"empty\">No spatial event heatmap data was recorded for this run.</p>");
            writer.WriteLine("</section>");
            return;
        }

        var meatCalories = CombineHeatmaps(heatmaps.MeatCaloriesEaten, heatmaps.EggCaloriesEaten);
        var exposureHours = ScaleHeatmap(heatmaps.CreatureExposureSeconds, 1f / 3600f);
        var layers = new[]
        {
            new SpatialHeatmapLayer(
                "Creature Exposure",
                "creature-hr",
                exposureHours,
                "#255f85",
                "Sampled creature-hours by location, based on the stats snapshot interval."),
            new SpatialHeatmapLayer(
                "Births",
                "births",
                heatmaps.Births,
                "#2f8f43",
                "Creature birth locations, including founders and hatched offspring."),
            new SpatialHeatmapLayer(
                "Deaths",
                "deaths",
                heatmaps.Deaths,
                "#b42318",
                "All creature death locations, regardless of cause."),
            new SpatialHeatmapLayer(
                "Starvation Deaths",
                "deaths",
                heatmaps.StarvationDeaths,
                "#d78325",
                "Creature death locations where starvation was the recorded cause."),
            new SpatialHeatmapLayer(
                "Injury Deaths",
                "deaths",
                heatmaps.InjuryDeaths,
                "#932f6d",
                "Creature death locations where attack injury was the recorded cause."),
            new SpatialHeatmapLayer(
                "Rotten Meat Deaths",
                "deaths",
                heatmaps.RottenMeatDeaths,
                "#5f4b8b",
                "Creature death locations attributed to rotten meat damage."),
            new SpatialHeatmapLayer(
                "Old Age Deaths",
                "deaths",
                heatmaps.OldAgeDeaths,
                "#d1a23a",
                "Creature death locations where old age was the recorded cause."),
            new SpatialHeatmapLayer(
                "Plant Eating",
                "raw kcal",
                heatmaps.PlantCaloriesEaten,
                "#6aaa2a",
                "Raw plant calories eaten at the plant patch location."),
            new SpatialHeatmapLayer(
                "Meat and Egg Eating",
                "raw kcal",
                meatCalories,
                "#c64b35",
                "Raw meat and egg calories eaten at the food location."),
            new SpatialHeatmapLayer(
                "Attack Damage",
                "damage",
                heatmaps.AttackDamage,
                "#3c5aa6",
                "Bite damage applied at the target creature location."),
            new SpatialHeatmapLayer(
                "Births per Creature Hour",
                "births/creature-hr",
                DivideHeatmaps(heatmaps.Births, exposureHours),
                "#1f7f4c",
                "Birth intensity normalized by sampled creature-hours in each cell."),
            new SpatialHeatmapLayer(
                "Deaths per Creature Hour",
                "deaths/creature-hr",
                DivideHeatmaps(heatmaps.Deaths, exposureHours),
                "#9d1f1f",
                "Death risk normalized by sampled creature-hours in each cell."),
            new SpatialHeatmapLayer(
                "Plant Eating per Creature Hour",
                "raw kcal/creature-hr",
                DivideHeatmaps(heatmaps.PlantCaloriesEaten, exposureHours),
                "#5f9d1f",
                "Plant calories eaten normalized by sampled creature-hours in each cell."),
            new SpatialHeatmapLayer(
                "Meat and Egg Eating per Creature Hour",
                "raw kcal/creature-hr",
                DivideHeatmaps(meatCalories, exposureHours),
                "#a8442f",
                "Meat and egg calories eaten normalized by sampled creature-hours in each cell."),
            new SpatialHeatmapLayer(
                "Attack Damage per Creature Hour",
                "damage/creature-hr",
                DivideHeatmaps(heatmaps.AttackDamage, exposureHours),
                "#2f4f9d",
                "Bite damage normalized by sampled creature-hours in each cell.")
        }.Where(layer => HeatmapTotal(layer.Values) > 0f).ToArray();

        if (layers.Length == 0)
        {
            writer.WriteLine("<p class=\"empty\">Spatial heatmaps were initialized, but every event layer is empty.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine(
            $"<p class=\"biome-map-note\">Events are aggregated into a {Html(heatmaps.CellCountX)} x {Html(heatmaps.CellCountY)} report grid. Creature exposure is sampled on the stats snapshot interval; per-creature-hour layers are estimates from those samples. Biome colors are shown faintly under each heat layer.</p>");
        writer.WriteLine("<div class=\"heatmap-grid\">");
        foreach (var layer in layers)
        {
            WriteSpatialHeatmapCard(writer, biomeMap, heatmaps, layer);
        }

        writer.WriteLine("</div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpatialHeatmapCard(
        TextWriter writer,
        BiomeMap biomeMap,
        SimulationSpatialHeatmaps heatmaps,
        SpatialHeatmapLayer layer)
    {
        var total = HeatmapTotal(layer.Values);
        var max = HeatmapMax(layer.Values);
        var width = MathF.Max(1f, heatmaps.WorldWidth);
        var height = MathF.Max(1f, heatmaps.WorldHeight);
        var cellWidth = width / Math.Max(1, heatmaps.CellCountX);
        var cellHeight = height / Math.Max(1, heatmaps.CellCountY);
        writer.WriteLine(
            $"<article class=\"chart-card heatmap-card\" role=\"button\" tabindex=\"0\" aria-label=\"Open {Html(layer.Title)} heatmap\">");
        writer.WriteLine($"<h3>{Html(layer.Title)}</h3>");
        writer.WriteLine($"<p>{Html(layer.Description)}</p>");
        writer.WriteLine($"<svg class=\"heatmap\" viewBox=\"0 0 {SvgNumber(width)} {SvgNumber(height)}\" role=\"img\" aria-label=\"{Html(layer.Title)} spatial heatmap\" preserveAspectRatio=\"xMidYMid meet\" shape-rendering=\"crispEdges\">");
        WriteBiomeHeatmapBackground(writer, biomeMap);
        for (var y = 0; y < heatmaps.CellCountY; y++)
        {
            for (var x = 0; x < heatmaps.CellCountX; x++)
            {
                var index = y * heatmaps.CellCountX + x;
                if (index < 0 || index >= layer.Values.Count)
                {
                    continue;
                }

                var value = layer.Values[index];
                if (value <= 0f)
                {
                    continue;
                }

                var opacity = 0.14f + 0.78f * MathF.Sqrt(value / MathF.Max(0.000001f, max));
                writer.WriteLine(
                    $"<rect x=\"{SvgNumber(x * cellWidth)}\" y=\"{SvgNumber(y * cellHeight)}\" width=\"{SvgNumber(cellWidth)}\" height=\"{SvgNumber(cellHeight)}\" fill=\"{Html(layer.Color)}\" fill-opacity=\"{SvgNumber(opacity)}\">" +
                    $"<title>{Html(FormatHeatmapValue(value, layer.Units))}</title></rect>");
            }
        }

        writer.WriteLine("</svg>");
        writer.WriteLine("<div class=\"heatmap-legend\">");
        if (IsRateHeatmapUnit(layer.Units))
        {
            var activeCellCount = Math.Max(1, HeatmapActiveCellCount(layer.Values));
            writer.WriteLine(
                $"<span><span class=\"legend-swatch\" style=\"background:{Html(layer.Color)}\"></span>Mean active cell {Html(FormatHeatmapValue(total / activeCellCount, layer.Units))}</span>");
        }
        else
        {
            writer.WriteLine(
                $"<span><span class=\"legend-swatch\" style=\"background:{Html(layer.Color)}\"></span>Total {Html(FormatHeatmapValue(total, layer.Units))}</span>");
        }

        writer.WriteLine($"<span>Peak cell {Html(FormatHeatmapValue(max, layer.Units))}</span>");
        writer.WriteLine("</div>");
        writer.WriteLine("</article>");
    }

    private static void WriteBiomeHeatmapBackground(TextWriter writer, BiomeMap map)
    {
        for (var y = 0; y < map.CellCountY; y++)
        {
            for (var x = 0; x < map.CellCountX; x++)
            {
                var cell = map.GetCellBounds(x, y);
                if (cell.Width <= 0f || cell.Height <= 0f)
                {
                    continue;
                }

                writer.WriteLine(
                    $"<rect x=\"{SvgNumber(cell.X)}\" y=\"{SvgNumber(cell.Y)}\" width=\"{SvgNumber(cell.Width)}\" height=\"{SvgNumber(cell.Height)}\" fill=\"{Html(BiomeColor(map.GetKind(x, y)))}\" fill-opacity=\"0.32\" />");
            }
        }
    }

    private static float[] CombineHeatmaps(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        var length = Math.Max(left.Count, right.Count);
        var combined = new float[length];
        for (var i = 0; i < combined.Length; i++)
        {
            combined[i] = (i < left.Count ? left[i] : 0f) + (i < right.Count ? right[i] : 0f);
        }

        return combined;
    }

    private static float[] ScaleHeatmap(IReadOnlyList<float> values, float scale)
    {
        var scaled = new float[values.Count];
        if (!float.IsFinite(scale) || scale <= 0f)
        {
            return scaled;
        }

        for (var i = 0; i < scaled.Length; i++)
        {
            var value = values[i];
            scaled[i] = float.IsFinite(value) && value > 0f ? value * scale : 0f;
        }

        return scaled;
    }

    private static float[] DivideHeatmaps(IReadOnlyList<float> numerator, IReadOnlyList<float> denominator)
    {
        var length = Math.Max(numerator.Count, denominator.Count);
        var divided = new float[length];
        for (var i = 0; i < divided.Length; i++)
        {
            var top = i < numerator.Count ? numerator[i] : 0f;
            var bottom = i < denominator.Count ? denominator[i] : 0f;
            divided[i] = float.IsFinite(top) && top > 0f && float.IsFinite(bottom) && bottom > 0f
                ? top / bottom
                : 0f;
        }

        return divided;
    }

    private static float HeatmapTotal(IReadOnlyList<float> values)
    {
        var total = 0f;
        foreach (var value in values)
        {
            if (float.IsFinite(value) && value > 0f)
            {
                total += value;
            }
        }

        return total;
    }

    private static float HeatmapMax(IReadOnlyList<float> values)
    {
        var max = 0f;
        foreach (var value in values)
        {
            if (float.IsFinite(value) && value > max)
            {
                max = value;
            }
        }

        return max;
    }

    private static int HeatmapActiveCellCount(IReadOnlyList<float> values)
    {
        var count = 0;
        foreach (var value in values)
        {
            if (float.IsFinite(value) && value > 0f)
            {
                count++;
            }
        }

        return count;
    }

    private static string FormatHeatmapValue(float value, string units)
    {
        var formatted = units is "births" or "deaths"
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.###", CultureInfo.InvariantCulture);
        return $"{formatted} {units}";
    }

    private static bool IsRateHeatmapUnit(string units)
    {
        return units.Contains('/', StringComparison.Ordinal);
    }

    private static void WriteSurvivorLineageTreeSection(
        StreamWriter writer,
        SurvivorLineageAnalysis analysis,
        WorldState state,
        SpeciesClusterHistory speciesHistory)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Survivor Ancestry Tree</h2>");
        if (analysis.LivingCreatureCount == 0 || analysis.Segments.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living creatures remain, so there is no survivor ancestry tree to draw.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine(
            "<p class=\"biome-map-note\">This view collapses straight creature ancestry into survivor lineage segments. Oldest ancestry is at the top, youngest survivors are at the bottom, and short extinct side branches are omitted.</p>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Living endpoints", analysis.LivingCreatureCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Ancestor nodes", analysis.AncestorCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Lineage segments", analysis.SegmentCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Founder roots", analysis.FounderCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Max generation", analysis.MaxGeneration.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Dominant founder", analysis.DominantFounderId == default ? "n/a" : $"#{analysis.DominantFounderId.Value}");
        WriteMetric(writer, "Dominant living", analysis.DominantFounderLivingDescendants.ToString(CultureInfo.InvariantCulture));
        writer.WriteLine("</div>");

        var graphSegments = SelectLineageSegmentsForGraph(analysis);
        var graphTruncated = false;
        if (graphSegments.Count > SurvivorLineageTreeNodeRenderLimit)
        {
            graphTruncated = true;
            graphSegments = analysis.Segments
                .Where(segment => segment.IsDominantPath)
                .ToArray();
            writer.WriteLine(
                $"<p class=\"empty\">The complete survivor ancestry has {Html(analysis.Segments.Count)} lineage segments, so the graph is limited to the dominant path. The lineage records still retain the full ancestry data.</p>");
        }
        else if (graphSegments.Count < analysis.Segments.Count)
        {
            writer.WriteLine(
                $"<p class=\"empty\">Displaying {Html(graphSegments.Count)} major lineage segments. Single-survivor terminal twigs are hidden to keep the tree readable; the dominant path and major surviving branches are retained.</p>");
        }

        WriteSurvivorLineageSegmentGraph(writer, graphSegments, analysis, state, speciesHistory, graphTruncated);
        WriteDominantLineagePathTable(writer, analysis);
        WriteLineageSegmentScript(writer);
        writer.WriteLine("</section>");
    }

    private static void WriteSurvivorLineageSegmentGraph(
        TextWriter writer,
        IReadOnlyList<SurvivorLineageSegment> segments,
        SurvivorLineageAnalysis analysis,
        WorldState state,
        SpeciesClusterHistory speciesHistory,
        bool graphTruncated)
    {
        if (segments.Count == 0)
        {
            return;
        }

        var layout = LayoutLineageSegments(segments, analysis.MaxGeneration);
        var width = layout.Width;
        var height = layout.Height;
        var generationStride = analysis.MaxGeneration <= 24 ? 1 : analysis.MaxGeneration <= 120 ? 5 : 25;

        writer.WriteLine("<div class=\"lineage-report-grid\" data-lineage-section>");
        writer.WriteLine("<div>");
        writer.WriteLine("<div class=\"lineage-tree-frame\">");
        writer.WriteLine("<div class=\"lineage-toolbar\"><span>Drag to pan. Wheel to zoom. Click a card for details.</span><button type=\"button\" data-lineage-reset>Reset view</button></div>");
        writer.WriteLine($"<svg class=\"lineage-tree\" data-lineage-panzoom data-lineage-viewbox=\"0 0 {SvgNumber(width)} {SvgNumber(height)}\" viewBox=\"0 0 {SvgNumber(width)} {SvgNumber(height)}\" role=\"img\" aria-label=\"Survivor lineage segment tree\">");
        writer.WriteLine("<rect x=\"0\" y=\"0\" width=\"100%\" height=\"100%\" fill=\"#fbfcf8\" />");
        for (var generation = 0; generation <= analysis.MaxGeneration; generation += generationStride)
        {
            var y = layout.YForGeneration(generation);
            writer.WriteLine($"<line x1=\"24\" y1=\"{SvgNumber(y)}\" x2=\"{SvgNumber(width - 24f)}\" y2=\"{SvgNumber(y)}\" stroke=\"#e3e8dc\" stroke-width=\"1\" />");
            writer.WriteLine($"<text x=\"28\" y=\"{SvgNumber(y - 4f)}\">g{Html(generation)}</text>");
        }

        foreach (var segment in segments)
        {
            if (segment.ParentSegmentId is null || !layout.ById.TryGetValue(segment.ParentSegmentId, out var parent))
            {
                continue;
            }

            var child = layout.ById[segment.SegmentId];
            var midY = (parent.BoxBottomY + child.BoxTopY) * 0.5f;
            var stroke = parent.Segment.IsDominantPath && child.Segment.IsDominantPath ? "#172015" : "#aab5a4";
            var strokeWidth = parent.Segment.IsDominantPath && child.Segment.IsDominantPath ? 2.4f : 1.2f;
            writer.WriteLine(
                $"<path d=\"M {SvgNumber(parent.X)} {SvgNumber(parent.BoxBottomY)} C {SvgNumber(parent.X)} {SvgNumber(midY)} {SvgNumber(child.X)} {SvgNumber(midY)} {SvgNumber(child.X)} {SvgNumber(child.BoxTopY)}\" fill=\"none\" stroke=\"{stroke}\" stroke-width=\"{SvgNumber(strokeWidth)}\" stroke-opacity=\"0.8\" />");
        }

        foreach (var item in layout.Items)
        {
            var segment = item.Segment;
            var strokeWidth = segment.IsDominantPath ? 2.4f : 1.1f;
            var branchWidth = 1.8f + 10f * MathF.Sqrt(segment.LivingDescendantCount / MathF.Max(1f, analysis.LivingCreatureCount));
            writer.WriteLine(
                $"<line x1=\"{SvgNumber(item.X)}\" y1=\"{SvgNumber(item.StartY)}\" x2=\"{SvgNumber(item.X)}\" y2=\"{SvgNumber(item.BoxTopY)}\" stroke=\"{Html(LineageSegmentColor(segment))}\" stroke-width=\"{SvgNumber(branchWidth)}\" stroke-linecap=\"round\" stroke-opacity=\"0.34\" />");
            var fill = segment.IsDominantPath
                ? "#172015"
                : segment.IsLivingEndpoint
                    ? "#2f8f43"
                    : segment.ChildSegmentCount > 1
                        ? "#d69d2f"
                        : "#6a8fce";
            var stroke = segment.HasGenomePayload && segment.HasBrainPayload ? "#ffffff" : "#b45309";
            var title = FormatLineageSegmentGraphTitle(segment);
            var speciesLabel = TryResolveLineageSpeciesName(segment.EndRecord, speciesHistory, out var speciesName)
                ? speciesName
                : "Species unclustered";
            var detail = FormatLineageSegmentDetailData(segment, state, speciesHistory);
            var tooltip = FormatLineageSegmentPlainDetail(segment, state, speciesHistory);
            writer.WriteLine(
                $"<g class=\"lineage-segment-node{(segment.IsDominantPath ? " is-dominant" : string.Empty)}\" tabindex=\"0\" role=\"button\" data-lineage-title=\"{Html(title)}\" data-lineage-detail=\"{Html(detail)}\">");
            writer.WriteLine(
                $"<rect x=\"{SvgNumber(item.BoxX)}\" y=\"{SvgNumber(item.BoxY)}\" width=\"{SvgNumber(item.BoxWidth)}\" height=\"{SvgNumber(item.BoxHeight)}\" rx=\"6\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{SvgNumber(strokeWidth)}\"><title>{Html(tooltip)}</title></rect>");
            writer.WriteLine($"<text x=\"{SvgNumber(item.BoxX + 8f)}\" y=\"{SvgNumber(item.BoxY + 17f)}\" style=\"fill:#fff\">{Html(TrimLineageGraphLabel(title, 22))}</text>");
            writer.WriteLine($"<text x=\"{SvgNumber(item.BoxX + 8f)}\" y=\"{SvgNumber(item.BoxY + 33f)}\" style=\"fill:#fff; opacity:0.82\">{Html(TrimLineageGraphLabel(speciesLabel, 22))}</text>");
            writer.WriteLine($"<text x=\"{SvgNumber(item.BoxX + 8f)}\" y=\"{SvgNumber(item.BoxY + 49f)}\" style=\"fill:#fff\">{Html($"g{segment.StartRecord.Generation}-{segment.EndRecord.Generation}, {segment.LivingDescendantCount} living")}</text>");
            writer.WriteLine("</g>");
        }

        writer.WriteLine("</svg>");
        writer.WriteLine("</div>");
        writer.WriteLine("<div class=\"lineage-tree-legend\">");
        writer.WriteLine("<span><span class=\"legend-swatch\" style=\"background:#172015\"></span>Representative path from dominant founder</span>");
        writer.WriteLine("<span><span class=\"legend-swatch\" style=\"background:#2f8f43\"></span>Living endpoint segment</span>");
        writer.WriteLine("<span><span class=\"legend-swatch\" style=\"background:#d69d2f\"></span>Branching segment</span>");
        writer.WriteLine("<span><span class=\"legend-swatch\" style=\"background:#6a8fce\"></span>Linear segment</span>");
        writer.WriteLine("<span><span class=\"legend-swatch\" style=\"background:#b45309\"></span>Orange border: missing genome/brain payload</span>");
        if (graphTruncated)
        {
            writer.WriteLine("<span>Graph is dominant-path only because the complete tree is too large for an inline SVG.</span>");
        }

        writer.WriteLine("</div>");
        writer.WriteLine("</div>");
        writer.WriteLine("<aside class=\"lineage-detail-panel\" aria-live=\"polite\">");
        writer.WriteLine("<h3 data-lineage-detail-title>Lineage detail</h3>");
        writer.WriteLine("<dl data-lineage-detail-body><dt>Select</dt><dd>Click a lineage box in the graph.</dd></dl>");
        writer.WriteLine("</aside>");
        writer.WriteLine("</div>");
    }

    private static IReadOnlyList<SurvivorLineageSegment> SelectLineageSegmentsForGraph(SurvivorLineageAnalysis analysis)
    {
        if (analysis.Segments.Count <= 48)
        {
            return analysis.Segments;
        }

        var byId = analysis.Segments.ToDictionary(segment => segment.SegmentId, StringComparer.Ordinal);
        var keep = new HashSet<string>(StringComparer.Ordinal);
        var minLivingDescendants = Math.Max(2, (int)MathF.Ceiling(analysis.LivingCreatureCount * 0.015f));

        foreach (var segment in analysis.Segments
            .Where(segment => segment.IsDominantPath
                || segment.ChildSegmentCount > 0
                || segment.LivingDescendantCount >= minLivingDescendants))
        {
            AddLineageSegmentAndParents(segment, byId, keep);
        }

        var rootSegments = analysis.Segments
            .Where(segment => segment.ParentSegmentId is null)
            .OrderByDescending(segment => segment.LivingDescendantCount)
            .ThenBy(segment => segment.SegmentId, StringComparer.Ordinal)
            .Take(16);
        foreach (var root in rootSegments)
        {
            AddLineageSegmentAndParents(root, byId, keep);
        }

        var selected = analysis.Segments
            .Where(segment => keep.Contains(segment.SegmentId))
            .ToArray();
        if (selected.Length <= SurvivorLineageTreeNodeRenderLimit)
        {
            return selected;
        }

        keep.Clear();
        foreach (var segment in analysis.Segments
            .Where(segment => segment.IsDominantPath || segment.ChildSegmentCount > 0)
            .Concat(analysis.Segments
                .OrderByDescending(segment => segment.LivingDescendantCount)
                .ThenBy(segment => segment.SegmentId, StringComparer.Ordinal)
                .Take(SurvivorLineageTreeNodeRenderLimit / 2)))
        {
            AddLineageSegmentAndParents(segment, byId, keep);
        }

        return analysis.Segments
            .Where(segment => keep.Contains(segment.SegmentId))
            .Take(SurvivorLineageTreeNodeRenderLimit)
            .ToArray();
    }

    private static void AddLineageSegmentAndParents(
        SurvivorLineageSegment segment,
        IReadOnlyDictionary<string, SurvivorLineageSegment> byId,
        ISet<string> keep)
    {
        var current = segment;
        while (keep.Add(current.SegmentId)
            && current.ParentSegmentId is not null
            && byId.TryGetValue(current.ParentSegmentId, out var parent))
        {
            current = parent;
        }
    }

    private static LineageSegmentLayout LayoutLineageSegments(
        IReadOnlyList<SurvivorLineageSegment> segments,
        int maxGeneration)
    {
        var byId = segments.ToDictionary(segment => segment.SegmentId);
        var childrenByParent = segments
            .Where(segment => segment.ParentSegmentId is not null && byId.ContainsKey(segment.ParentSegmentId))
            .GroupBy(segment => segment.ParentSegmentId!)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(segment => segment.LivingDescendantCount)
                    .ThenBy(segment => segment.EndRecord.Generation)
                    .ThenBy(segment => segment.SegmentId, StringComparer.Ordinal)
                    .ToArray());
        var roots = segments
            .Where(segment => segment.ParentSegmentId is null || !byId.ContainsKey(segment.ParentSegmentId))
            .OrderByDescending(segment => segment.LivingDescendantCount)
            .ThenBy(segment => segment.SegmentId, StringComparer.Ordinal)
            .ToArray();
        var laneById = new Dictionary<string, float>(StringComparer.Ordinal);
        var nextLane = 0;

        float AssignLane(SurvivorLineageSegment segment)
        {
            if (laneById.TryGetValue(segment.SegmentId, out var existing))
            {
                return existing;
            }

            if (!childrenByParent.TryGetValue(segment.SegmentId, out var children) || children.Length == 0)
            {
                var leafLane = nextLane++;
                laneById[segment.SegmentId] = leafLane;
                return leafLane;
            }

            var total = 0f;
            foreach (var child in children)
            {
                total += AssignLane(child);
            }

            var lane = total / children.Length;
            laneById[segment.SegmentId] = lane;
            return lane;
        }

        foreach (var root in roots)
        {
            AssignLane(root);
        }

        foreach (var segment in segments)
        {
            AssignLane(segment);
        }

        var laneCount = Math.Max(1, nextLane);
        const float plotLeft = 112f;
        const float laneStride = 156f;
        var plotWidth = MathF.Max(900f, MathF.Max(1f, laneCount - 1f) * laneStride);
        const float top = 48f;
        var plotHeight = MathF.Max(640f, Math.Max(1, maxGeneration) * 72f);
        const float boxWidth = 146f;
        const float boxHeight = 58f;
        var width = plotLeft + plotWidth + 112f;
        var items = segments
            .Select(segment =>
            {
                var x = laneCount == 1
                    ? width * 0.5f
                    : plotLeft + laneById[segment.SegmentId] / MathF.Max(1f, laneCount - 1f) * plotWidth;
                var startY = top + segment.StartRecord.Generation / MathF.Max(1f, maxGeneration) * plotHeight;
                var endY = top + segment.EndRecord.Generation / MathF.Max(1f, maxGeneration) * plotHeight;
                var boxY = endY - boxHeight * 0.5f;
                return new LineageSegmentLayoutItem(
                    segment,
                    x,
                    startY,
                    endY,
                    x - boxWidth * 0.5f,
                    boxY,
                    boxWidth,
                    boxHeight);
            })
            .OrderBy(item => item.Segment.StartRecord.Generation)
            .ThenByDescending(item => item.Segment.LivingDescendantCount)
            .ThenBy(item => item.Segment.SegmentId, StringComparer.Ordinal)
            .ToArray();
        return new LineageSegmentLayout(
            items,
            items.ToDictionary(item => item.Segment.SegmentId, StringComparer.Ordinal),
            width,
            top + plotHeight + 64f,
            generation => top + generation / MathF.Max(1f, maxGeneration) * plotHeight);
    }

    private static void WriteDominantLineagePathTable(
        StreamWriter writer,
        SurvivorLineageAnalysis analysis)
    {
        writer.WriteLine("<h3>Dominant Ancestor Path</h3>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Step</th><th>Creature</th><th>Generation</th><th>Living Descendants</th><th>Surviving Children</th><th>Born</th><th>Status</th><th>Payload</th></tr></thead>");
        writer.WriteLine("<tbody>");
        for (var i = 0; i < analysis.DominantPath.Count; i++)
        {
            var step = analysis.DominantPath[i];
            var record = step.Record;
            var payload = record.GenomeId >= 0 && record.BrainId >= 0 ? "Genome+brain kept" : "Payload pruned";
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(i + 1)}</td>" +
                $"<td>#{Html(record.Id.Value)}</td>" +
                $"<td>{Html(record.Generation)}</td>" +
                $"<td>{Html(step.LivingDescendantCount)}</td>" +
                $"<td>{Html(step.ChildCount)}</td>" +
                $"<td>{Html($"tick {record.BirthTick}")}</td>" +
                $"<td>{Html(FormatLineageStatus(record))}</td>" +
                $"<td>{Html(payload)}</td>" +
                "</tr>");
        }

        if (analysis.DominantPath.Count == 0)
        {
            WriteEmptyRow(writer, 8, "No dominant path could be reconstructed.");
        }

        writer.WriteLine("</tbody></table></div>");
    }

    private static string FormatLineageNodeTitle(SurvivorLineageNode node)
    {
        return $"Creature #{node.Record.Id.Value}, generation {node.Record.Generation}, {node.LivingDescendantCount} living descendants, {FormatLineageStatus(node.Record)}";
    }

    private static string FormatLineageSegmentName(
        SurvivorLineageSegment segment,
        SpeciesClusterHistory speciesHistory)
    {
        if (TryResolveLineageSpeciesName(segment.EndRecord, speciesHistory, out var name))
        {
            return name;
        }

        return segment.StartRecord.Id == segment.EndRecord.Id
            ? $"Lineage #{segment.EndRecord.Id.Value}"
            : $"Lineage #{segment.StartRecord.Id.Value}-{segment.EndRecord.Id.Value}";
    }

    private static string FormatLineageSegmentGraphTitle(SurvivorLineageSegment segment)
    {
        return segment.ParentSegmentId is null && segment.StartRecord.Generation == 0
            ? $"Founder #{segment.StartRecord.Id.Value}"
            : FormatLineageSegmentFallbackId(segment);
    }

    private static bool TryResolveLineageSpeciesName(
        CreatureLineageRecord record,
        SpeciesClusterHistory speciesHistory,
        out string name)
    {
        if (speciesHistory.RecordClusterById.TryGetValue(record.Id, out var speciesId))
        {
            name = SpeciesClusterAnalyzer.GenerateName(speciesId);
            return true;
        }

        name = string.Empty;
        return false;
    }

    private static string TrimLineageGraphLabel(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, Math.Max(1, maxLength - 1)), "...");
    }

    private static string FormatLineageSegmentFallbackId(SurvivorLineageSegment segment)
    {
        return segment.StartRecord.Id == segment.EndRecord.Id
            ? $"L {segment.EndRecord.Id.Value}"
            : $"L {segment.StartRecord.Id.Value}-{segment.EndRecord.Id.Value}";
    }

    private static string FormatLineageSegmentDetailData(
        SurvivorLineageSegment segment,
        WorldState state,
        SpeciesClusterHistory speciesHistory)
    {
        return string.Join(
            "||",
            BuildLineageSegmentDetailEntries(segment, state, speciesHistory)
                .Select(entry => $"{entry.Label}::{entry.Value}"));
    }

    private static string FormatLineageSegmentPlainDetail(
        SurvivorLineageSegment segment,
        WorldState state,
        SpeciesClusterHistory speciesHistory)
    {
        return string.Join(
            " | ",
            BuildLineageSegmentDetailEntries(segment, state, speciesHistory)
                .Select(entry => $"{entry.Label}: {entry.Value}"));
    }

    private static IReadOnlyList<LineageDetailEntry> BuildLineageSegmentDetailEntries(
        SurvivorLineageSegment segment,
        WorldState state,
        SpeciesClusterHistory speciesHistory)
    {
        var payload = segment.HasGenomePayload && segment.HasBrainPayload ? "kept" : "partly pruned";
        var entries = new List<LineageDetailEntry>
        {
            new("Species", FormatLineageSegmentName(segment, speciesHistory)),
            new("Segment", FormatLineageSegmentFallbackId(segment)),
            new("Starts at", $"#{segment.StartRecord.Id.Value}, generation {segment.StartRecord.Generation}"),
            new("Endpoint", $"#{segment.EndRecord.Id.Value} ({FormatLineageStatus(segment.EndRecord)})"),
            new("Generations", $"{segment.StartRecord.Generation}-{segment.EndRecord.Generation}"),
            new("Ancestor nodes", segment.AncestorCount.ToString(CultureInfo.InvariantCulture)),
            new("Living descendants", segment.LivingDescendantCount.ToString(CultureInfo.InvariantCulture)),
            new("Child segments", segment.ChildSegmentCount.ToString(CultureInfo.InvariantCulture)),
            new("Graph role", FormatLineageSegmentGraphRole(segment)),
            new("Birth ticks", $"{segment.StartRecord.BirthTick}-{segment.EndRecord.BirthTick}"),
            new("Payload", payload)
        };

        if (state.TryGetGenome(segment.EndRecord.GenomeId, out var genome))
        {
            entries.Add(new("Body genes", $"radius {FormatCompactNumber(genome.BodyRadius)}, speed {FormatCompactNumber(genome.MaxSpeed)}, turn {FormatCompactNumber(genome.MaxTurnRadiansPerSecond)} rad/s"));
            entries.Add(new("Sense genes", $"range {FormatCompactNumber(genome.SenseRadius)}, vision {FormatCompactNumber(genome.VisionAngleRadians * 180f / MathF.PI)} deg"));
            entries.Add(new("Energy genes", $"pace {FormatCompactNumber(genome.MetabolicPace)}x, basal {FormatCompactNumber(genome.BasalEnergyPerSecond)}/s, move {FormatCompactNumber(genome.MovementEnergyPerSecond)}/s, eat {FormatCompactNumber(genome.EatCaloriesPerSecond)}/s"));
            entries.Add(new("Repro genes", $"threshold {FormatCompactNumber(genome.ReproductionEnergyThreshold)}, investment {FormatCompactNumber(genome.OffspringEnergyInvestment)}, cooldown {FormatCompactNumber(genome.ReproductionCooldownSeconds)}s"));
            entries.Add(new("Diet genes", $"diet {FormatCompactNumber(genome.DietaryAdaptation)}, carrion {FormatCompactNumber(genome.CarrionAdaptation)}, tender/rich/tough {FormatCompactNumber(genome.TenderPlantAdaptation)}/{FormatCompactNumber(genome.RichPlantAdaptation)}/{FormatCompactNumber(genome.ToughPlantAdaptation)}"));
            entries.Add(new("Combat genes", $"bite {FormatCompactNumber(genome.BiteStrength)}, resist {FormatCompactNumber(genome.DamageResistance)}"));
            entries.Add(new("Digest genes", $"gut {FormatCompactNumber(genome.GutCapacityCalories)}, digest {FormatCompactNumber(genome.DigestionCaloriesPerSecond)}/s"));
            entries.Add(new("Fat genes", $"capacity {FormatCompactNumber(genome.FatStorageCapacityCalories)}, efficiency {FormatCompactNumber(genome.FatStorageEfficiency)}"));
        }
        else
        {
            entries.Add(new("Genome", "pruned or unavailable"));
        }

        if (state.TryGetBrain(segment.EndRecord.BrainId, out var brain) && brain is not null)
        {
            var architecture = state.GetBrainArchitectureKind(segment.EndRecord.BrainId);
            var directMean = brain.Weights.Length > 0
                ? brain.Weights.Take(NeuralBrainGenome.DirectWeightCount).Average(weight => Math.Abs(weight))
                : 0f;
            entries.Add(new("Brain", $"{FormatBrainArchitectureKind(architecture)}, hidden {brain.HiddenNodeCount}, weights {brain.Weights.Length}"));
            entries.Add(new("Brain magnitude", $"direct mean |w| {FormatCompactNumber((float)directMean)}, hidden in {FormatCompactNumber(brain.SumAbsoluteHiddenInputWeights())}, hidden out {FormatCompactNumber(brain.SumAbsoluteHiddenOutputWeights())}"));
            entries.Add(new("Forage weights", $"plant forward {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.PlantDirectionForwardInput))}, meat forward {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.MeatDirectionForwardInput))}, eat freshness {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.VisibleMeatFreshnessInput))}"));
            entries.Add(new("Risk weights", $"rot move {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RottenMeatScentForwardInput))}, terrain drag {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.ForwardTerrainDragInput))}"));
            entries.Add(new("Attack weights", $"creature proximity {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.AttackOutput, NeuralBrainSchema.CreatureProximityInput))}, contact {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.AttackOutput, NeuralBrainSchema.CreatureContactInput))}, approach {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.AttackOutput, NeuralBrainSchema.CreatureApproachRateInput))}"));
        }
        else
        {
            entries.Add(new("Brain", "pruned or unavailable"));
        }

        return entries;
    }

    private static string FormatCompactNumber(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string LineageSegmentColor(SurvivorLineageSegment segment)
    {
        return segment.IsDominantPath
            ? "#172015"
            : segment.IsLivingEndpoint
                ? "#2f8f43"
                : segment.ChildSegmentCount > 1
                    ? "#d69d2f"
                    : "#6a8fce";
    }

    private static string FormatLineageSegmentGraphRole(SurvivorLineageSegment segment)
    {
        if (segment.IsDominantPath)
        {
            return "representative path from dominant founder";
        }

        if (segment.IsLivingEndpoint)
        {
            return "living endpoint segment";
        }

        return segment.ChildSegmentCount > 1
            ? "branching segment"
            : "linear segment";
    }

    private static void WriteLineageSegmentScript(TextWriter writer)
    {
        writer.WriteLine(
            """
            <script>
            (() => {
              for (const section of document.querySelectorAll('[data-lineage-section]')) {
                const title = section.querySelector('[data-lineage-detail-title]');
                const body = section.querySelector('[data-lineage-detail-body]');
                const nodes = Array.from(section.querySelectorAll('.lineage-segment-node'));
                const select = node => {
                  for (const other of nodes) {
                    other.classList.toggle('is-selected', other === node);
                  }
                  if (title) {
                    title.textContent = node.getAttribute('data-lineage-title') || 'Lineage detail';
                  }
                  if (body) {
                    body.replaceChildren();
                    const parts = (node.getAttribute('data-lineage-detail') || '').split('||').filter(Boolean);
                    for (const part of parts) {
                      const index = part.indexOf('::');
                      const label = index > 0 ? part.slice(0, index) : 'Detail';
                      const value = index > 0 ? part.slice(index + 2) : part;
                      const dt = document.createElement('dt');
                      const dd = document.createElement('dd');
                      dt.textContent = label;
                      dd.textContent = value;
                      body.append(dt, dd);
                    }
                  }
                };
                for (const node of nodes) {
                  node.addEventListener('pointerdown', event => event.stopPropagation());
                  node.addEventListener('click', event => {
                    event.stopPropagation();
                    select(node);
                  });
                  node.addEventListener('keydown', event => {
                    if (event.key === 'Enter' || event.key === ' ') {
                      event.preventDefault();
                      select(node);
                    }
                  });
                }
                const initial = section.querySelector('.lineage-segment-node.is-dominant') || nodes[0];
                if (initial) {
                  select(initial);
                }

                const resetButtons = Array.from(section.querySelectorAll('[data-lineage-reset]'));
                const svgs = Array.from(section.querySelectorAll('[data-lineage-panzoom]'));
                const resetSvg = svg => {
                  const raw = svg.getAttribute('data-lineage-viewbox') || svg.getAttribute('viewBox');
                  if (raw) {
                    svg.setAttribute('viewBox', raw);
                  }
                };
                resetButtons.forEach(button => {
                  button.addEventListener('click', () => svgs.forEach(resetSvg));
                });
                for (const svg of svgs) {
                  let viewBox = (svg.getAttribute('viewBox') || '0 0 1 1').split(/\s+/).map(Number);
                  let dragStart = null;
                  const apply = () => svg.setAttribute('viewBox', viewBox.map(value => Number.isFinite(value) ? value.toFixed(3) : '0').join(' '));
                  const point = event => {
                    const rect = svg.getBoundingClientRect();
                    return {
                      x: viewBox[0] + (event.clientX - rect.left) / Math.max(1, rect.width) * viewBox[2],
                      y: viewBox[1] + (event.clientY - rect.top) / Math.max(1, rect.height) * viewBox[3]
                    };
                  };
                  svg.addEventListener('wheel', event => {
                    event.preventDefault();
                    viewBox = (svg.getAttribute('viewBox') || '0 0 1 1').split(/\s+/).map(Number);
                    const before = point(event);
                    const factor = event.deltaY < 0 ? 0.82 : 1.22;
                    const nextWidth = Math.min(Math.max(viewBox[2] * factor, 220), 12000);
                    const nextHeight = Math.min(Math.max(viewBox[3] * factor, 180), 12000);
                    viewBox[0] = before.x - (before.x - viewBox[0]) * (nextWidth / viewBox[2]);
                    viewBox[1] = before.y - (before.y - viewBox[1]) * (nextHeight / viewBox[3]);
                    viewBox[2] = nextWidth;
                    viewBox[3] = nextHeight;
                    apply();
                  }, { passive: false });
                  svg.addEventListener('pointerdown', event => {
                    if (event.button !== 0) return;
                    if (event.target.closest && event.target.closest('.lineage-segment-node')) return;
                    viewBox = (svg.getAttribute('viewBox') || '0 0 1 1').split(/\s+/).map(Number);
                    dragStart = { x: event.clientX, y: event.clientY, viewBox: [...viewBox] };
                    svg.setPointerCapture(event.pointerId);
                  });
                  svg.addEventListener('pointermove', event => {
                    if (!dragStart) return;
                    const rect = svg.getBoundingClientRect();
                    viewBox[0] = dragStart.viewBox[0] - (event.clientX - dragStart.x) / Math.max(1, rect.width) * dragStart.viewBox[2];
                    viewBox[1] = dragStart.viewBox[1] - (event.clientY - dragStart.y) / Math.max(1, rect.height) * dragStart.viewBox[3];
                    apply();
                  });
                  const clearDrag = () => { dragStart = null; };
                  svg.addEventListener('pointerup', clearDrag);
                  svg.addEventListener('pointercancel', clearDrag);
                }
              }
            })();
            </script>
            """);
    }

    private sealed record LineageSegmentLayout(
        IReadOnlyList<LineageSegmentLayoutItem> Items,
        IReadOnlyDictionary<string, LineageSegmentLayoutItem> ById,
        float Width,
        float Height,
        Func<int, float> YForGeneration);

    private sealed record LineageSegmentLayoutItem(
        SurvivorLineageSegment Segment,
        float X,
        float StartY,
        float EndY,
        float BoxX,
        float BoxY,
        float BoxWidth,
        float BoxHeight)
    {
        public float BoxTopY => BoxY;

        public float BoxBottomY => BoxY + BoxHeight;
    }

    private readonly record struct LineageDetailEntry(string Label, string Value);

    private static string FormatLineageStatus(CreatureLineageRecord record)
    {
        if (record.IsAlive)
        {
            return "alive";
        }

        var reason = record.DeathReason?.ToString() ?? "dead";
        return record.DeathTick.HasValue
            ? $"{reason} at tick {record.DeathTick.Value}"
            : reason;
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

    private static string FormatOptionalTick(long? tick)
    {
        return tick?.ToString(CultureInfo.InvariantCulture) ?? "survived";
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

    private static void WriteChartsSection(
        StreamWriter writer,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        int sourceSnapshotCount)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Graphs</h2>");
        if (snapshots.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No stats snapshots were recorded, so no graphs are available.</p>");
            writer.WriteLine("</section>");
            return;
        }

        if (sourceSnapshotCount > snapshots.Count)
        {
            writer.WriteLine($"<p class=\"empty\">Graphs and timeline report sections are sampled to {snapshots.Count.ToString("N0", CultureInfo.InvariantCulture)} of {sourceSnapshotCount.ToString("N0", CultureInfo.InvariantCulture)} stats snapshots. CSV sidecars retain the full-resolution data.</p>");
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
            new ChartSeries("Adult", "#4b8f83", snapshots.Select(snapshot => Share(snapshot.AdultCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Maturity", "#7a6bb0", snapshots.Select(snapshot => snapshot.AverageMaturityProgress * 100f).ToArray()),
            new ChartSeries("Reserve", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageEggReserveRatio * 100f).ToArray()),
            new ChartSeries("Surplus", "#2f7d4f", snapshots.Select(snapshot => snapshot.AverageEnergySurplusRatio * 100f).ToArray()),
            new ChartSeries("Energy full", "#b84a4a", snapshots.Select(snapshot => snapshot.AverageEnergyFullnessRatio * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Fat storage",
            "%",
            snapshots,
            new ChartSeries("Reserve", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageFatRatio * 100f).ToArray()),
            new ChartSeries("Mass burden", "#b84a4a", snapshots.Select(snapshot => snapshot.AverageMassBurdenRatio * 100f).ToArray()),
            new ChartSeries("Speed retained", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageFatSpeedMultiplier * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Fat Storage Genes",
            " value",
            snapshots,
            new ChartSeries("Capacity", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageFatStorageCapacityCalories).ToArray()),
            new ChartSeries("Efficiency %", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageFatStorageEfficiency * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Resource calories",
            " kcal",
            snapshots,
            new ChartSeries("Plants", "#35a862", snapshots.Select(snapshot => snapshot.TotalPlantCalories).ToArray()),
            new ChartSeries("Meat", "#b84a4a", snapshots.Select(snapshot => snapshot.TotalMeatCalories).ToArray()));
        WriteLineChart(
            writer,
            "Plant type calories",
            " kcal",
            snapshots,
            new ChartSeries("Generic", "#35a862", snapshots.Select(GenericPlantTypeCalories).ToArray()),
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => snapshot.TenderPlantTypeCalories).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => snapshot.RichPlantTypeCalories).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => snapshot.ToughPlantTypeCalories).ToArray()));
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
            "Local fertility",
            "%",
            snapshots,
            new ChartSeries("Average", "#35a862", snapshots.Select(snapshot => snapshot.AverageLocalFertilityMultiplier * 100f).ToArray()),
            new ChartSeries("Minimum", "#d69d2f", snapshots.Select(snapshot => snapshot.MinimumLocalFertilityMultiplier * 100f).ToArray()),
            new ChartSeries("Depleted cells", "#8f4cb8", snapshots.Select(snapshot => snapshot.DepletedLocalFertilityCellShare * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Temperature exposure",
            "",
            snapshots,
            new ChartSeries("Map avg", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageMapTemperature * 100f).ToArray()),
            new ChartSeries("Creatures", "#c9492e", snapshots.Select(snapshot => snapshot.AverageCreatureTemperature * 100f).ToArray()),
            new ChartSeries("Plants", "#4b9b44", snapshots.Select(snapshot => snapshot.AveragePlantTemperature * 100f).ToArray()),
            new ChartSeries("Small prey", "#1b91a8", snapshots.Select(snapshot => snapshot.AverageSmallPreyTemperature * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Thermal adaptation",
            "",
            snapshots,
            new ChartSeries("Optimum", "#c9492e", snapshots.Select(snapshot => snapshot.AverageThermalOptimum * 100f).ToArray()),
            new ChartSeries("Tolerance", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageThermalTolerance * 100f).ToArray()),
            new ChartSeries("Mismatch", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageCreatureThermalMismatch * 100f).ToArray()),
            new ChartSeries("Hot mismatch", "#b83a2e", snapshots.Select(snapshot => Share(snapshot.HotThermalMismatchCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Cold mismatch", "#2f74bc", snapshots.Select(snapshot => Share(snapshot.ColdThermalMismatchCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
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
            new ChartSeries("Desert", "#9a6b3b", snapshots.Select(snapshot => Share(snapshot.BarrenCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Scrubland", "#7f8f3a", snapshots.Select(snapshot => Share(snapshot.SparseCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Grassland", "#35a862", snapshots.Select(snapshot => Share(snapshot.GrasslandCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Fertile", "#178a4a", snapshots.Select(snapshot => Share(snapshot.RichCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Forest", "#0b5f2a", snapshots.Select(snapshot => Share(snapshot.ForestCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Wetland", "#15807b", snapshots.Select(snapshot => Share(snapshot.WetlandCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Tundra", "#9ab1b6", snapshots.Select(snapshot => Share(snapshot.TundraCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Highland", "#817565", snapshots.Select(snapshot => Share(snapshot.HighlandCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
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
            new ChartSeries("Desert", "#9a6b3b", snapshots.Select(snapshot => snapshot.BarrenCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Scrubland", "#7f8f3a", snapshots.Select(snapshot => snapshot.SparseCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Grassland", "#35a862", snapshots.Select(snapshot => snapshot.GrasslandCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Fertile", "#178a4a", snapshots.Select(snapshot => snapshot.RichCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Forest", "#0b5f2a", snapshots.Select(snapshot => snapshot.ForestCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Wetland", "#15807b", snapshots.Select(snapshot => snapshot.WetlandCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Tundra", "#9ab1b6", snapshots.Select(snapshot => snapshot.TundraCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Highland", "#817565", snapshots.Select(snapshot => snapshot.HighlandCaloriesEatenPerSecond).ToArray()));
        WriteLineChart(
            writer,
            "Biome deaths",
            "",
            snapshots,
            new ChartSeries("Desert", "#9a6b3b", snapshots.Select(snapshot => (float)snapshot.BarrenDeathCount).ToArray()),
            new ChartSeries("Scrubland", "#7f8f3a", snapshots.Select(snapshot => (float)snapshot.SparseDeathCount).ToArray()),
            new ChartSeries("Grassland", "#35a862", snapshots.Select(snapshot => (float)snapshot.GrasslandDeathCount).ToArray()),
            new ChartSeries("Fertile", "#178a4a", snapshots.Select(snapshot => (float)snapshot.RichDeathCount).ToArray()),
            new ChartSeries("Forest", "#0b5f2a", snapshots.Select(snapshot => (float)snapshot.ForestDeathCount).ToArray()),
            new ChartSeries("Wetland", "#15807b", snapshots.Select(snapshot => (float)snapshot.WetlandDeathCount).ToArray()),
            new ChartSeries("Tundra", "#9ab1b6", snapshots.Select(snapshot => (float)snapshot.TundraDeathCount).ToArray()),
            new ChartSeries("Highland", "#817565", snapshots.Select(snapshot => (float)snapshot.HighlandDeathCount).ToArray()));
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
            new ChartSeries("Movement blocked", "#d96b3b", snapshots.Select(snapshot => Share(snapshot.ObstacleBlockedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Forward cue", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageForwardObstacle * 100f).ToArray()),
            new ChartSeries("Side cue", "#2f7d4f", snapshots.Select(snapshot => MathF.Max(snapshot.AverageLeftObstacle, snapshot.AverageRightObstacle) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Creature collision contact",
            "%",
            snapshots,
            new ChartSeries("Body blocked", "#c24a8a", snapshots.Select(snapshot => Share(snapshot.CreatureCollisionCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Damaged", "#9d3434", snapshots.Select(snapshot => Share(snapshot.CreatureCollisionDamagedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Creature collision impacts",
            "",
            snapshots,
            new ChartSeries("Damage/s", "#c24a8a", snapshots.Select(snapshot => snapshot.TotalCreatureCollisionDamagePerSecond).ToArray()),
            new ChartSeries("Avg impact", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageCreatureCollisionImpactSpeed).ToArray()),
            new ChartSeries("Max impact", "#d69d2f", snapshots.Select(snapshot => snapshot.MaxCreatureCollisionImpactSpeed).ToArray()));
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
            new ChartSeries("Avg strength", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageMemoryStrength * 100f).ToArray()),
            new ChartSeries("Injury active", "#c24a8a", snapshots.Select(snapshot => Share(snapshot.ActiveInjuryMemoryCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Injury strength", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageInjuryMemoryStrength * 100f).ToArray()));
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
            "Plant type digestion",
            " energy/s",
            snapshots,
            new ChartSeries("Generic", "#35a862", snapshots.Select(GenericPlantDigestedEnergyPerSecond).ToArray()),
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => snapshot.TenderPlantDigestedEnergyPerSecond).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => snapshot.RichPlantDigestedEnergyPerSecond).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => snapshot.ToughPlantDigestedEnergyPerSecond).ToArray()));
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
            "Plant type intake",
            " kcal/s",
            snapshots,
            new ChartSeries("Generic", "#35a862", snapshots.Select(GenericPlantCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => snapshot.TenderPlantCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => snapshot.RichPlantCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => snapshot.ToughPlantCaloriesEatenPerSecond).ToArray()));
        WriteLineChart(
            writer,
            "Plant type intake share",
            "%",
            snapshots,
            new ChartSeries("Generic", "#35a862", snapshots.Select(snapshot => PlantTypeShare(GenericPlantCaloriesEatenPerSecond(snapshot), snapshot.TotalPlantCaloriesEatenPerSecond) * 100f).ToArray()),
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => PlantTypeShare(snapshot.TenderPlantCaloriesEatenPerSecond, snapshot.TotalPlantCaloriesEatenPerSecond) * 100f).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => PlantTypeShare(snapshot.RichPlantCaloriesEatenPerSecond, snapshot.TotalPlantCaloriesEatenPerSecond) * 100f).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => PlantTypeShare(snapshot.ToughPlantCaloriesEatenPerSecond, snapshot.TotalPlantCaloriesEatenPerSecond) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Plant type intake per resource",
            " kcal/s/resource",
            snapshots,
            new ChartSeries("Generic", "#35a862", snapshots.Select(snapshot => PlantTypeIntakePerResource(GenericPlantCaloriesEatenPerSecond(snapshot), GenericPlantTypeResourceCount(snapshot))).ToArray()),
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => PlantTypeIntakePerResource(snapshot.TenderPlantCaloriesEatenPerSecond, snapshot.TenderPlantTypeResourceCount)).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => PlantTypeIntakePerResource(snapshot.RichPlantCaloriesEatenPerSecond, snapshot.RichPlantTypeResourceCount)).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => PlantTypeIntakePerResource(snapshot.ToughPlantCaloriesEatenPerSecond, snapshot.ToughPlantTypeResourceCount)).ToArray()));
        WriteLineChart(
            writer,
            "Plant payoff trace",
            "",
            snapshots,
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => snapshot.AverageTenderPlantPayoffTrace).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => snapshot.AverageRichPlantPayoffTrace).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => snapshot.AverageToughPlantPayoffTrace).ToArray()));
        WriteLineChart(
            writer,
            "Predation Diagnostics",
            "%",
            snapshots,
            new ChartSeries("Seeing creatures", "#6a8fce", snapshots.Select(snapshot => Share(snapshot.CreatureDetectedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Contact", "#d69d2f", snapshots.Select(snapshot => Share(snapshot.CreatureContactCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Intent", "#e05a47", snapshots.Select(snapshot => Share(snapshot.AttackIntentCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Intent touch", "#9d3434", snapshots.Select(snapshot => Share(snapshot.AttackIntentWhileTouchingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Near gate touch", "#8f4cb8", snapshots.Select(snapshot => Share(snapshot.RawAttackNearGateWhileTouchingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Fresh kill share", "#8f4cb8", snapshots.Select(snapshot => snapshot.FreshKillCaloriesEatenShare * 100f).ToArray()),
            new ChartSeries("Fresh meat share", "#d69d2f", snapshots.Select(snapshot => snapshot.FreshMeatCaloriesEatenShare * 100f).ToArray()),
            new ChartSeries("Meat energy share", "#b84a4a", snapshots.Select(snapshot => snapshot.MeatDigestedEnergyShare * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Grab State",
            "%",
            snapshots,
            new ChartSeries("Can grab", "#f5c26b", snapshots.Select(snapshot => Share(snapshot.CanGrabCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Grab intent", "#ff8a30", snapshots.Select(snapshot => Share(snapshot.GrabIntentCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Grab+touch", "#ffcc66", snapshots.Select(snapshot => Share(snapshot.GrabIntentWhileCanGrabCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Off-touch grab", "#b96cff", snapshots.Select(snapshot => Share(snapshot.GrabIntentWithoutCanGrabCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Holding", "#d96b3b", snapshots.Select(snapshot => Share(snapshot.HoldingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Grabbed", "#9d3434", snapshots.Select(snapshot => Share(snapshot.GrabbedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Sound",
            "%",
            snapshots,
            new ChartSeries("Emitting", "#29b6f6", snapshots.Select(snapshot => Share(snapshot.SoundEmittingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Heard", "#8f4cb8", snapshots.Select(snapshot => Share(snapshot.SoundHeardCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Avg amp", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageSoundAmplitude * 100f).ToArray()),
            new ChartSeries("Avg density", "#2f7d4f", snapshots.Select(snapshot => snapshot.AverageSoundDensity * 100f).ToArray()),
            new ChartSeries("Avg clarity", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageSoundToneClarity * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Meat Opportunity Gap",
            "%",
            snapshots,
            new ChartSeries("Meat seen", "#d69d2f", snapshots.Select(snapshot => Share(snapshot.MeatDetectedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Meat scent", "#8f4cb8", snapshots.Select(snapshot => Share(snapshot.MeatScentDetectedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Meat contact", "#35a862", snapshots.Select(snapshot => Share(snapshot.MeatContactCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Contact no eat", "#b84a4a", snapshots.Select(snapshot => Share(snapshot.MeatContactNotEatingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Stale contact", "#7d5546", snapshots.Select(snapshot => Share(snapshot.StaleMeatContactCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
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
            "Meat No-Eat Causes",
            "%",
            snapshots,
            new ChartSeries("No intent", "#b84a4a", snapshots.Select(snapshot => Share(snapshot.MeatContactNotEatingNoIntentCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Gut full", "#6a8fce", snapshots.Select(snapshot => Share(snapshot.MeatContactNotEatingGutFullCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Storage full", "#35a862", snapshots.Select(snapshot => Share(snapshot.MeatContactNotEatingStorageFullCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Stale", "#7d5546", snapshots.Select(snapshot => Share(snapshot.MeatContactNotEatingStaleCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Other", "#8f4cb8", snapshots.Select(snapshot => Share(snapshot.MeatContactNotEatingOtherCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Genome Drift: body and senses",
            "",
            snapshots,
            new ChartSeries("Body radius", "#2f7d4f", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.BodyRadius).ToArray()),
            new ChartSeries("Max speed", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.MaxSpeed).ToArray()),
            new ChartSeries("Turn rate", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.MaxTurnRadiansPerSecond).ToArray()),
            new ChartSeries("Sense radius", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.SenseRadius).ToArray()),
            new ChartSeries("Vision angle deg", "#b84a4a", snapshots.Select(snapshot => ToDegrees(snapshot.AverageGenomeTraits.VisionAngleRadians)).ToArray()));
        WriteLineChart(
            writer,
            "Genome Drift: energy and feeding",
            "",
            snapshots,
            new ChartSeries("Basal", "#7d5546", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.BasalEnergyPerSecond).ToArray()),
            new ChartSeries("Move cost", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.MovementEnergyPerSecond).ToArray()),
            new ChartSeries("Eat rate", "#35a862", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.EatCaloriesPerSecond).ToArray()),
            new ChartSeries("Gut cap", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.GutCapacityCalories).ToArray()),
            new ChartSeries("Digest rate", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.DigestionCaloriesPerSecond).ToArray()),
            new ChartSeries("Fat cap", "#b84a4a", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.FatStorageCapacityCalories).ToArray()),
            new ChartSeries("Fat efficiency %", "#4b8f83", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.FatStorageEfficiency * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Genome Drift: reproduction and lifespan",
            " s/kcal",
            snapshots,
            new ChartSeries("Repro threshold", "#2f7d4f", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.ReproductionEnergyThreshold).ToArray()),
            new ChartSeries("Offspring invest", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.OffspringEnergyInvestment).ToArray()),
            new ChartSeries("Egg prod/s", "#35a862", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.EggProductionEnergyPerSecond).ToArray()),
            new ChartSeries("Incubation", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.EggIncubationSeconds).ToArray()),
            new ChartSeries("Maturity age", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.MaturityAgeSeconds).ToArray()),
            new ChartSeries("Cooldown", "#7d5546", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.ReproductionCooldownSeconds).ToArray()),
            new ChartSeries("Life expectancy", "#b84a4a", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.MaxLifeExpectancySeconds).ToArray()));
        WriteLineChart(
            writer,
            "Genome Drift: diet and combat",
            "",
            snapshots,
            new ChartSeries("Diet meat bias", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.DietaryAdaptation).ToArray()),
            new ChartSeries("Carrion bias", "#7d5546", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.CarrionAdaptation).ToArray()),
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.TenderPlantAdaptation).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.RichPlantAdaptation).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.ToughPlantAdaptation).ToArray()),
            new ChartSeries("Bite", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.BiteStrength).ToArray()),
            new ChartSeries("Resistance", "#b84a4a", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.DamageResistance).ToArray()));
        WriteLineChart(
            writer,
            "Genome Drift: thermal, scent, mutation",
            "",
            snapshots,
            new ChartSeries("Thermal opt", "#c9492e", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.ThermalOptimum).ToArray()),
            new ChartSeries("Thermal tol", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.ThermalTolerance).ToArray()),
            new ChartSeries("Scent A", "#35a862", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.ScentSignatureA).ToArray()),
            new ChartSeries("Scent B", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.ScentSignatureB).ToArray()),
            new ChartSeries("Scent C", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.ScentSignatureC).ToArray()),
            new ChartSeries("Mutation strength", "#7d5546", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.MutationStrength).ToArray()),
            new ChartSeries("Trait rate", "#4b8f83", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.TraitMutationRate).ToArray()),
            new ChartSeries("Brain rate", "#b84a4a", snapshots.Select(snapshot => snapshot.AverageGenomeTraits.BrainMutationRate).ToArray()));
        WriteLineChart(
            writer,
            "Diet Traits",
            "",
            snapshots,
            new ChartSeries("Diet meat bias", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageDietaryAdaptation).ToArray()),
            new ChartSeries("Carrion bias", "#7d5546", snapshots.Select(snapshot => snapshot.AverageCarrionAdaptation).ToArray()));
        WriteLineChart(
            writer,
            "Plant adaptation traits",
            "",
            snapshots,
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => snapshot.AverageTenderPlantAdaptation).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => snapshot.AverageRichPlantAdaptation).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => snapshot.AverageToughPlantAdaptation).ToArray()));
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
            new ChartSeries("Damage-dealing %", "#d96b3b", snapshots.Select(snapshot => Share(snapshot.AttackingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Avg raw attack", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageAttackOutput).ToArray()),
            new ChartSeries("Avg touch attack", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageTouchingAttackOutput).ToArray()),
            new ChartSeries("Avg grab output", "#ff8a30", snapshots.Select(snapshot => snapshot.AverageGrabOutput).ToArray()),
            new ChartSeries("Avg touch grab", "#ffcc66", snapshots.Select(snapshot => snapshot.AverageCanGrabGrabOutput).ToArray()),
            new ChartSeries("Attack damage", "#9d3434", snapshots.Select(snapshot => snapshot.TotalAttackDamagePerSecond).ToArray()),
            new ChartSeries("Healing", "#2f9e73", snapshots.Select(snapshot => snapshot.TotalHealthHealedPerSecond).ToArray()),
            new ChartSeries("Healing %", "#62b6cb", snapshots.Select(snapshot => Share(snapshot.HealingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
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
        series = DownsampleChartSeries(series, ReportTimelineSampleLimit);

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

        writer.WriteLine($"<div class=\"chart-card\" role=\"button\" tabindex=\"0\" aria-label=\"Open larger {Html(title)} chart\">");
        writer.WriteLine($"<h3>{Html(title)}</h3>");
        writer.WriteLine($"<svg viewBox=\"0 0 {width:0} {height:0}\" role=\"img\" aria-label=\"{Html(title)} chart\">");
        writer.WriteLine($"<line class=\"chart-axis\" x1=\"{left:0}\" y1=\"{top:0}\" x2=\"{left:0}\" y2=\"{height - bottom:0}\" />");
        writer.WriteLine($"<line class=\"chart-axis\" x1=\"{left:0}\" y1=\"{height - bottom:0}\" x2=\"{width - right:0}\" y2=\"{height - bottom:0}\" />");
        writer.WriteLine($"<text class=\"chart-label\" x=\"4\" y=\"{top + 4:0}\">{Html(FormatChartValue(max, unit))}</text>");
        writer.WriteLine($"<text class=\"chart-label\" x=\"4\" y=\"{height - bottom:0}\">{Html(FormatChartValue(min, unit))}</text>");

        for (var seriesIndex = 0; seriesIndex < series.Length; seriesIndex++)
        {
            var chartSeries = series[seriesIndex];
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

            writer.WriteLine($"<polyline class=\"chart-series-line\" data-series-index=\"{seriesIndex}\" points=\"{Html(string.Join(' ', points))}\" fill=\"none\" stroke=\"{Html(chartSeries.Color)}\" stroke-width=\"2.4\" stroke-linejoin=\"round\" stroke-linecap=\"round\" />");
        }

        writer.WriteLine("</svg>");
        writer.WriteLine("<div class=\"chart-legend\">");
        for (var seriesIndex = 0; seriesIndex < series.Length; seriesIndex++)
        {
            var chartSeries = series[seriesIndex];
            var final = chartSeries.Values.Length > 0 ? chartSeries.Values[^1] : 0f;
            writer.WriteLine(
                $"<span class=\"chart-legend-item\" data-series-index=\"{seriesIndex}\" title=\"Highlight {Html(chartSeries.Label)}\"><span class=\"legend-swatch\" style=\"background:{Html(chartSeries.Color)}\"></span>{Html(chartSeries.Label)} {Html(FormatChartValue(final, unit))}</span>");
        }

        writer.WriteLine("</div>");
        writer.WriteLine("</div>");
    }

    private static void WriteThermalNicheSection(
        StreamWriter writer,
        WorldState state,
        IReadOnlyList<FounderSummary> founderSummaries,
        IReadOnlyList<SpeciesClusterSummary> speciesSummaries,
        SimulationStatsSnapshot snapshot)
    {
        var livingFounders = founderSummaries
            .Where(summary => summary.LivingCreatures > 0)
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenByDescending(summary => summary.ThermalNiche.AverageThermalMismatch)
            .ThenBy(summary => summary.FounderId.Value)
            .ToArray();
        var livingSpecies = speciesSummaries
            .Where(summary => summary.LivingCreatures > 0)
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenBy(summary => summary.Rank)
            .ToArray();
        var ecotypes = ThermalEcotypeAnalyzer.Analyze(state);

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Thermal Niches</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Current living C/T/H", FormatThermalBandCounts(snapshot.ColdTemperatureCreatureCount, snapshot.TemperateTemperatureCreatureCount, snapshot.HotTemperatureCreatureCount));
        WriteMetric(writer, "Current stress", FormatThermalStressCounts(snapshot));
        WriteMetric(writer, "Founder niches", FormatFounderNicheLabelCounts(livingFounders));
        WriteMetric(writer, "Species niches", FormatSpeciesNicheLabelCounts(livingSpecies));
        WriteMetric(writer, "Avg creature temp index", FormatTemperatureIndex(snapshot.AverageCreatureTemperature));
        WriteMetric(writer, "Avg mismatch", FormatPercent(snapshot.AverageCreatureThermalMismatch));
        WriteMetric(writer, "Births C/T/H", FormatThermalBandValues(snapshot.ColdTemperatureBirths, snapshot.TemperateTemperatureBirths, snapshot.HotTemperatureBirths));
        WriteMetric(writer, "Deaths C/T/H", FormatThermalBandValues(snapshot.ColdTemperatureDeaths, snapshot.TemperateTemperatureDeaths, snapshot.HotTemperatureDeaths));
        writer.WriteLine("</div>");

        writer.WriteLine("<h3>Thermal Ecotypes</h3>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Ecotype</th><th>Founders</th><th>Living</th><th>Total</th><th>Max Gen</th><th>Dominant Founder</th><th>Opt/Tol</th><th>Avg Temp</th><th>Mismatch</th><th>Lifetime C/T/H</th><th>Stress C/H</th><th>Births C/T/H</th><th>Deaths C/T/H</th><th>Top Founders</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var ecotype in ecotypes)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(ecotype.Label)}</td>" +
                $"<td>{Html(ecotype.FounderLineageCount)}</td>" +
                $"<td>{Html(ecotype.LivingCreatures)}</td>" +
                $"<td>{Html(ecotype.TotalCreatures)}</td>" +
                $"<td>{Html(ecotype.MaxGeneration)}</td>" +
                $"<td>#{Html(ecotype.DominantFounderId.Value)} ({Html(ecotype.DominantFounderLivingCreatures)})</td>" +
                $"<td>{Html($"{ecotype.AverageLivingThermalOptimum:0.###} / {ecotype.AverageLivingThermalTolerance:0.###}")}</td>" +
                $"<td>{Html(ecotype.AverageOccupiedTemperature.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(ecotype.AverageThermalMismatch.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatEcotypeThermalShares(ecotype))}</td>" +
                $"<td>{Html(FormatEcotypeStressShares(ecotype))}</td>" +
                $"<td>{Html(FormatThermalBandCounts(ecotype.ColdTemperatureBirths, ecotype.TemperateTemperatureBirths, ecotype.HotTemperatureBirths))}</td>" +
                $"<td>{Html(FormatThermalBandCounts(ecotype.ColdTemperatureDeaths, ecotype.TemperateTemperatureDeaths, ecotype.HotTemperatureDeaths))}</td>" +
                $"<td>{Html(FormatEcotypeTopFounders(ecotype.TopFounders))}</td>" +
                "</tr>");
        }

        if (ecotypes.Count == 0)
        {
            WriteEmptyRow(writer, 14, "No living thermal ecotypes were present.");
        }

        writer.WriteLine("</tbody></table></div>");

        writer.WriteLine("<h3>Founder Niches</h3>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Founder</th><th>Living</th><th>Niche</th><th>Avg Temp</th><th>Mismatch</th><th>Lifetime C/T/H</th><th>Stress C/H</th><th>Births C/T/H</th><th>Deaths C/T/H</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in livingFounders.Take(12))
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>#{Html(summary.FounderId.Value)}</td>" +
                $"<td>{Html(summary.LivingCreatures)}</td>" +
                $"<td>{Html(summary.ThermalNiche.NicheLabel)}</td>" +
                $"<td>{Html(summary.ThermalNiche.AverageOccupiedTemperature.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ThermalNiche.AverageThermalMismatch.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatThermalShares(summary.ThermalNiche))}</td>" +
                $"<td>{Html(FormatStressShares(summary.ThermalNiche))}</td>" +
                $"<td>{Html(FormatThermalBandCounts(summary.ThermalNiche.ColdTemperatureBirths, summary.ThermalNiche.TemperateTemperatureBirths, summary.ThermalNiche.HotTemperatureBirths))}</td>" +
                $"<td>{Html(FormatThermalBandCounts(summary.ThermalNiche.ColdTemperatureDeaths, summary.ThermalNiche.TemperateTemperatureDeaths, summary.ThermalNiche.HotTemperatureDeaths))}</td>" +
                "</tr>");
        }

        if (livingFounders.Length == 0)
        {
            WriteEmptyRow(writer, 9, "No living founder lineages were present.");
        }

        writer.WriteLine("</tbody></table></div>");

        writer.WriteLine("<h3>Species Niches</h3>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Living</th><th>Niche</th><th>Current Temp</th><th>Lifetime Temp</th><th>Mismatch</th><th>Living C/T/H</th><th>Lifetime C/T/H</th><th>Stress C/H</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in livingSpecies.Take(10))
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.Rank)}</td>" +
                $"<td>{Html(summary.Name)}</td>" +
                $"<td>{Html(summary.LivingCreatures)}</td>" +
                $"<td>{Html(summary.ThermalNicheLabel)}</td>" +
                $"<td>{Html(summary.AverageCurrentTemperature.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.AverageOccupiedTemperature.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.AverageOccupiedThermalMismatch.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatThermalBandCounts(summary.ColdTemperatureLivingCreatures, summary.TemperateTemperatureLivingCreatures, summary.HotTemperatureLivingCreatures))}</td>" +
                $"<td>{Html(FormatSpeciesThermalShares(summary))}</td>" +
                $"<td>{Html(FormatSpeciesStressShares(summary))}</td>" +
                "</tr>");
        }

        if (livingSpecies.Length == 0)
        {
            WriteEmptyRow(writer, 10, "No living species clusters were available.");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
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
        WriteMetric(writer, "Collision response", summary.CollisionResponse);
        WriteMetric(writer, "Injury memory response", summary.InjuryMemoryResponse);
        WriteMetric(writer, "Maturity response", summary.MaturityResponse);
        WriteMetric(writer, "Egg familiarity response", summary.EggFamiliarityResponse);
        WriteMetric(writer, "Egg laying", summary.ReproductionTendency);
        WriteMetric(writer, "Rotten meat response", summary.RottenMeatResponse);
        WriteMetric(writer, "Meat contact eat", FormatPercent(summary.MeatContact.EatShare));
        WriteMetric(writer, "Egg contact eat", FormatPercent(summary.EggContact.EatShare));
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

    private static void WriteCollisionDiagnosticsSection(
        StreamWriter writer,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        SimulationStatsSnapshot snapshot)
    {
        var peakPairs = snapshot.CreatureCollisionPairCount;
        var peakCollisionCreatures = snapshot.CreatureCollisionCreatureCount;
        var peakDamagedCreatures = snapshot.CreatureCollisionDamagedCreatureCount;
        var peakDamagePerSecond = snapshot.TotalCreatureCollisionDamagePerSecond;
        var peakImpactSpeed = snapshot.MaxCreatureCollisionImpactSpeed;
        var contactSampleCount = snapshot.CreatureCollisionPairCount > 0 ? 1 : 0;
        var damageSampleCount = snapshot.TotalCreatureCollisionDamagePerSecond > 0f ? 1 : 0;

        if (snapshots.Count > 0)
        {
            peakPairs = snapshots.Max(row => row.CreatureCollisionPairCount);
            peakCollisionCreatures = snapshots.Max(row => row.CreatureCollisionCreatureCount);
            peakDamagedCreatures = snapshots.Max(row => row.CreatureCollisionDamagedCreatureCount);
            peakDamagePerSecond = snapshots.Max(row => row.TotalCreatureCollisionDamagePerSecond);
            peakImpactSpeed = snapshots.Max(row => row.MaxCreatureCollisionImpactSpeed);
            contactSampleCount = snapshots.Count(row => row.CreatureCollisionPairCount > 0);
            damageSampleCount = snapshots.Count(row => row.TotalCreatureCollisionDamagePerSecond > 0f);
        }

        var sampleCount = Math.Max(1, snapshots.Count);
        var damagePerDamagedCreature = snapshot.CreatureCollisionDamagedCreatureCount > 0
            ? snapshot.TotalCreatureCollisionDamagePerSecond / snapshot.CreatureCollisionDamagedCreatureCount
            : 0f;

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Collision Diagnostics</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Movement blocked (all)", $"{FormatPercent(Share(snapshot.ObstacleBlockedCreatureCount, snapshot.CreatureCount))} ({snapshot.ObstacleBlockedCreatureCount})");
        WriteMetric(writer, "Body-blocked creatures", $"{FormatPercent(Share(snapshot.CreatureCollisionCreatureCount, snapshot.CreatureCount))} ({snapshot.CreatureCollisionCreatureCount})");
        WriteMetric(writer, "Collision pairs", snapshot.CreatureCollisionPairCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Damaged creatures", $"{FormatPercent(Share(snapshot.CreatureCollisionDamagedCreatureCount, snapshot.CreatureCount))} ({snapshot.CreatureCollisionDamagedCreatureCount})");
        WriteMetric(writer, "Collision damage", $"{snapshot.TotalCreatureCollisionDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Damage per damaged", $"{damagePerDamagedCreature:0.###} health/s");
        WriteMetric(writer, "Impact speed", $"avg {snapshot.AverageCreatureCollisionImpactSpeed:0.###} / max {snapshot.MaxCreatureCollisionImpactSpeed:0.###}");
        WriteMetric(writer, "Contact samples", $"{FormatPercent(contactSampleCount / (float)sampleCount)} ({contactSampleCount}/{sampleCount})");
        WriteMetric(writer, "Damage samples", $"{FormatPercent(damageSampleCount / (float)sampleCount)} ({damageSampleCount}/{sampleCount})");
        WriteMetric(writer, "Peak pairs", peakPairs.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Peak body-blocked", peakCollisionCreatures.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Peak damaged", peakDamagedCreatures.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Peak damage", $"{peakDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Peak impact", peakImpactSpeed.ToString("0.###", CultureInfo.InvariantCulture));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpeciesClusterSection(
        StreamWriter writer,
        IReadOnlyList<SpeciesClusterSummary> summaries)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Top Species Clusters</h2>");
        if (summaries.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living creatures were available for species clustering.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Living</th><th>Share</th><th>Founders</th><th>Dominant Founder</th><th>Representative</th><th>Generation</th><th>Diet</th><th>Tactic</th><th>Region</th><th>Thermal Niche</th><th>Current Temp</th><th>Lifetime Temp</th><th>Mismatch</th><th>Living C/T/H</th><th>Pace</th><th>Genome Div</th><th>Brain Div</th><th>Plant Adapt</th><th>Plant Digest</th><th>Meat Digest</th><th>Attack</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in summaries)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.Rank)}</td>" +
                $"<td>{Html(summary.Name)}</td>" +
                $"<td>{Html(summary.LivingCreatures)}</td>" +
                $"<td>{Html(FormatPercent(summary.LivingShare))}</td>" +
                $"<td>{Html(summary.FounderCount)}</td>" +
                $"<td>#{Html(summary.DominantFounderId.Value)} ({Html(summary.DominantFounderLivingCreatures)})</td>" +
                $"<td>#{Html(summary.RepresentativeCreatureId.Value)} ({Html(summary.RepresentativeDistance.ToString("0.###", CultureInfo.InvariantCulture))})</td>" +
                $"<td>{Html(FormatGenerationRange(summary.MinGeneration, summary.AverageGeneration, summary.MaxGeneration))}</td>" +
                $"<td>{Html(summary.DietLabel)}</td>" +
                $"<td>{Html(summary.TacticLabel)}</td>" +
                $"<td>{Html(summary.RegionLabel)}</td>" +
                $"<td>{Html(summary.ThermalNicheLabel)}</td>" +
                $"<td>{Html(summary.AverageCurrentTemperature.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.AverageOccupiedTemperature.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.AverageOccupiedThermalMismatch.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html($"{summary.ColdTemperatureLivingCreatures}/{summary.TemperateTemperatureLivingCreatures}/{summary.HotTemperatureLivingCreatures}")}</td>" +
                $"<td>{Html(summary.AverageMetabolicPace.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.AverageGenomeDistance.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.AverageBrainDistance.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPlantAdaptation(summary))}</td>" +
                $"<td>{Html(FormatPercent(summary.AveragePlantDigestion))}</td>" +
                $"<td>{Html(FormatPercent(summary.AverageMeatDigestion))}</td>" +
                $"<td>{Html(FormatPercent(summary.AttackShare))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpeciesBehaviorFingerprintSection(
        StreamWriter writer,
        IReadOnlyList<SpeciesClusterBehaviorFingerprint> fingerprints)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Species Behavior Fingerprints</h2>");
        if (fingerprints.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living neural creatures were available for species behavior fingerprints.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Living</th><th>Evaluated</th><th>Ecotype</th><th>Food</th><th>Rotten Meat</th><th>Egg Familiarity</th><th>Risk</th><th>Terrain</th><th>Collision</th><th>Attack</th><th>Movement</th><th>Search</th><th>Egg Laying</th><th>Plant Move</th><th>Meat Move</th><th>Rot Scent Move</th><th>Body Block Move</th><th>Body Block Attack</th><th>Small Attack</th><th>Large Attack</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var fingerprint in fingerprints)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(fingerprint.Rank)}</td>" +
                $"<td>{Html(fingerprint.Name)}</td>" +
                $"<td>{Html($"{fingerprint.LivingCreatures} ({FormatPercent(fingerprint.LivingShare)})")}</td>" +
                $"<td>{Html(fingerprint.EvaluatedCreatureCount)}</td>" +
                $"<td>{Html(fingerprint.Ecotype)}</td>" +
                $"<td>{Html(fingerprint.ForagingBias)}</td>" +
                $"<td>{Html(fingerprint.RottenMeatResponse)}</td>" +
                $"<td>{Html(fingerprint.EggFamiliarityResponse)}</td>" +
                $"<td>{Html(fingerprint.RiskResponse)}</td>" +
                $"<td>{Html(fingerprint.TerrainResponse)}</td>" +
                $"<td>{Html(fingerprint.CollisionResponse)}</td>" +
                $"<td>{Html(fingerprint.PredatorTendency)}</td>" +
                $"<td>{Html(fingerprint.MovementStyle)}</td>" +
                $"<td>{Html(fingerprint.SearchTendency)}</td>" +
                $"<td>{Html(fingerprint.ReproductionTendency)}</td>" +
                $"<td>{Html(fingerprint.PlantAheadMoveForward.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(fingerprint.MeatAheadMoveForward.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(fingerprint.RottenScentAheadMoveForward.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(fingerprint.CreatureBlockedMoveForward.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(fingerprint.CreatureBlockedAttackShare))}</td>" +
                $"<td>{Html(FormatPercent(fingerprint.SmallCreatureAttackShare))}</td>" +
                $"<td>{Html(FormatPercent(fingerprint.LargeApproachAttackShare))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpeciesBehaviorChangeSection(
        StreamWriter writer,
        IReadOnlyList<SpeciesClusterBehaviorChange> changes)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Species Behavior Change</h2>");
        if (changes.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No species behavior change comparison was available.</p>");
            writer.WriteLine("</section>");
            return;
        }

        var notableChanges = SpeciesClusterAnalyzer.FindNotableBehaviorChanges(changes);
        writer.WriteLine("<h3>Notable Shifts</h3>");
        if (notableChanges.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No notable behavior shifts crossed the report thresholds.</p>");
        }
        else
        {
            writer.WriteLine("<div class=\"table-wrap\"><table>");
            writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Score</th><th>Change</th></tr></thead>");
            writer.WriteLine("<tbody>");
            foreach (var change in notableChanges)
            {
                writer.WriteLine(
                    "<tr>" +
                    $"<td>{Html(change.Rank)}</td>" +
                    $"<td>{Html(change.Name)}</td>" +
                    $"<td>{Html(change.Score.ToString("0.##", CultureInfo.InvariantCulture))}</td>" +
                    $"<td>{Html(change.Summary)}</td>" +
                    "</tr>");
            }

            writer.WriteLine("</tbody></table></div>");
        }

        writer.WriteLine("<h3>All Cluster Comparisons</h3>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Samples</th><th>Ecotype</th><th>Food</th><th>Rotten Meat</th><th>Risk</th><th>Terrain</th><th>Attack</th><th>Movement</th><th>Eggs</th><th>Plant Move</th><th>Meat Move</th><th>Rot Move</th><th>Small Attack</th><th>Egg Laying</th><th>Summary</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var change in changes)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(change.Rank)}</td>" +
                $"<td>{Html(change.Name)}</td>" +
                $"<td>{Html($"{change.EarlySampleCount} early / {change.FinalSampleCount} {change.FinalSampleKind}")}</td>" +
                $"<td>{Html(FormatChange(change.EarlyEcotype, change.FinalEcotype))}</td>" +
                $"<td>{Html(FormatChange(change.EarlyForagingBias, change.FinalForagingBias))}</td>" +
                $"<td>{Html(FormatChange(change.EarlyRottenMeatResponse, change.FinalRottenMeatResponse))}</td>" +
                $"<td>{Html(FormatChange(change.EarlyRiskResponse, change.FinalRiskResponse))}</td>" +
                $"<td>{Html(FormatChange(change.EarlyTerrainResponse, change.FinalTerrainResponse))}</td>" +
                $"<td>{Html(FormatChange(change.EarlyPredatorTendency, change.FinalPredatorTendency))}</td>" +
                $"<td>{Html(FormatChange(change.EarlyMovementStyle, change.FinalMovementStyle))}</td>" +
                $"<td>{Html(FormatChange(change.EarlyReproductionTendency, change.FinalReproductionTendency))}</td>" +
                $"<td>{Html(FormatDelta(change.PlantMoveDelta))}</td>" +
                $"<td>{Html(FormatDelta(change.MeatMoveDelta))}</td>" +
                $"<td>{Html(FormatDelta(change.RotScentMoveDelta))}</td>" +
                $"<td>{Html(FormatDelta(change.SmallAttackDelta))}</td>" +
                $"<td>{Html(FormatDelta(change.EggLayingDelta))}</td>" +
                $"<td>{Html(change.Summary)}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpeciesClusterInterpretationSection(
        StreamWriter writer,
        IReadOnlyList<SpeciesClusterInterpretation> interpretations)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Why These Clusters Matter</h2>");
        if (interpretations.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No species cluster interpretation was available.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Role</th><th>Ancestry</th><th>Trend</th><th>Why It Matters</th><th>Evidence</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var interpretation in interpretations)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(interpretation.Rank)}</td>" +
                $"<td>{Html(interpretation.Name)}</td>" +
                $"<td>{Html(interpretation.RoleLabel)}</td>" +
                $"<td>{Html(interpretation.AncestryLabel)}</td>" +
                $"<td>{Html(interpretation.TrendLabel)}</td>" +
                $"<td>{Html(interpretation.ImportanceLabel)}</td>" +
                $"<td>{Html(interpretation.EvidenceLabel)}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpeciesClusterHistorySection(StreamWriter writer, SpeciesClusterHistory history)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Species Cluster History</h2>");
        if (history.Clusters.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No lineage records were available for species history reconstruction.</p>");
            writer.WriteLine("</section>");
            return;
        }

        if (history.Notes.Count > 0)
        {
            writer.WriteLine("<ul>");
            foreach (var note in history.Notes)
            {
                writer.WriteLine($"<li>{Html(note)}</li>");
            }

            writer.WriteLine("</ul>");
        }

        if (history.DiversityRows.Count > 0)
        {
            var finalDiversity = history.DiversityRows[^1];
            var peakDiversity = history.DiversityRows
                .OrderByDescending(row => row.ActiveClusterCount)
                .ThenBy(row => row.Tick)
                .First();
            var totalTurnover = history.DiversityRows.Sum(row => row.TurnoverClusters);

            writer.WriteLine("<div class=\"metric-grid\">");
            WriteMetric(writer, "Final active clusters", finalDiversity.ActiveClusterCount.ToString(CultureInfo.InvariantCulture));
            WriteMetric(writer, "Peak active clusters", $"{peakDiversity.ActiveClusterCount} at tick {peakDiversity.Tick}");
            WriteMetric(writer, "Final dominant cluster", $"{finalDiversity.DominantName} ({FormatPercent(finalDiversity.DominantLivingShare)})");
            WriteMetric(writer, "Sampled turnover", totalTurnover.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine("</div>");

            writer.WriteLine("<div class=\"chart-grid\">");
            WriteLineChart(
                writer,
                "Species Diversity",
                "",
                Array.Empty<SimulationStatsSnapshot>(),
                new ChartSeries("Active clusters", "#6a8fce", history.DiversityRows.Select(row => (float)row.ActiveClusterCount).ToArray()),
                new ChartSeries("Dominant share %", "#2f7d4f", history.DiversityRows.Select(row => row.DominantLivingShare * 100f).ToArray()),
                new ChartSeries("Turnover", "#d69d2f", history.DiversityRows.Select(row => (float)row.TurnoverClusters).ToArray()));
            writer.WriteLine("</div>");
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Status</th><th>Lifecycle</th><th>Births</th><th>Deaths</th><th>Final</th><th>Peak</th><th>First Birth</th><th>Peak Tick</th><th>Last Seen</th><th>Generation</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in history.Clusters)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.Rank)}</td>" +
                $"<td>{Html(summary.Name)}</td>" +
                $"<td>{Html(summary.Status)}</td>" +
                $"<td>{Html(summary.LifecycleLabel)}</td>" +
                $"<td>{Html(summary.Births)}</td>" +
                $"<td>{Html(summary.Deaths)}</td>" +
                $"<td>{Html($"{summary.FinalLivingCreatures} ({FormatPercent(summary.FinalLivingShare)})")}</td>" +
                $"<td>{Html($"{summary.PeakLivingCreatures} ({FormatPercent(summary.PeakLivingShare)})")}</td>" +
                $"<td>{Html(summary.FirstBirthTick)}</td>" +
                $"<td>{Html(summary.PeakTick)}</td>" +
                $"<td>{Html(summary.LastLivingTick)}</td>" +
                $"<td>{Html(FormatGenerationRange(summary.MinGeneration, summary.AverageGeneration, summary.MaxGeneration))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");

        var selectedClusters = history.Clusters.Take(5).ToArray();
        var selectedTicks = SelectReportTicks(history.DiversityRows
            .Select(row => row.Tick)
            .OrderBy(tick => tick)
            .ToArray());
        if (selectedTicks.Count > 0)
        {
            var diversityByTick = history.DiversityRows.ToDictionary(row => row.Tick);
            writer.WriteLine("<h3>Diversity Over Time</h3>");
            writer.WriteLine("<div class=\"table-wrap\"><table>");
            writer.WriteLine("<thead><tr><th>Tick</th><th>Time</th><th>Active Clusters</th><th>Total Living</th><th>Dominant</th><th>Dominant Share</th><th>Entering</th><th>Exiting</th></tr></thead>");
            writer.WriteLine("<tbody>");
            foreach (var tick in selectedTicks)
            {
                if (!diversityByTick.TryGetValue(tick, out var row))
                {
                    continue;
                }

                writer.WriteLine(
                    "<tr>" +
                    $"<td>{Html(row.Tick)}</td>" +
                    $"<td>{Html(row.ElapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                    $"<td>{Html(row.ActiveClusterCount)}</td>" +
                    $"<td>{Html(row.TotalLiving)}</td>" +
                    $"<td>{Html(row.DominantName)}</td>" +
                    $"<td>{Html(FormatPercent(row.DominantLivingShare))}</td>" +
                    $"<td>{Html(row.EnteringClusters)}</td>" +
                    $"<td>{Html(row.ExitingClusters)}</td>" +
                    "</tr>");
            }

            writer.WriteLine("</tbody></table></div>");
        }

        if (selectedClusters.Length > 0 && selectedTicks.Count > 0)
        {
            var rowByTickSpecies = history.Rows.ToDictionary(row => (row.Tick, row.SpeciesId));
            var rowsByTick = history.Rows
                .GroupBy(row => row.Tick)
                .ToDictionary(group => group.Key, group => group.First().ElapsedSeconds);

            writer.WriteLine("<h3>Cluster Counts Over Time</h3>");
            writer.WriteLine("<div class=\"table-wrap\"><table>");
            writer.Write("<thead><tr><th>Tick</th><th>Time</th><th>Total</th>");
            foreach (var cluster in selectedClusters)
            {
                writer.Write($"<th>{Html(cluster.Name)}</th>");
            }

            writer.WriteLine("</tr></thead>");
            writer.WriteLine("<tbody>");
            foreach (var tick in selectedTicks)
            {
                var totalLiving = history.Rows
                    .Where(row => row.Tick == tick)
                    .Select(row => row.TotalLiving)
                    .FirstOrDefault();
                rowsByTick.TryGetValue(tick, out var elapsedSeconds);
                writer.Write(
                    "<tr>" +
                    $"<td>{Html(tick)}</td>" +
                    $"<td>{Html(elapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                    $"<td>{Html(totalLiving)}</td>");

                foreach (var cluster in selectedClusters)
                {
                    if (rowByTickSpecies.TryGetValue((tick, cluster.SpeciesId), out var row))
                    {
                        writer.Write($"<td>{Html($"{row.LivingCreatures} ({FormatPercent(row.LivingShare)})")}</td>");
                    }
                    else
                    {
                        writer.Write("<td>0</td>");
                    }
                }

                writer.WriteLine("</tr>");
            }

            writer.WriteLine("</tbody></table></div>");
        }

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
        writer.WriteLine("<thead><tr><th>Founder</th><th>Living</th><th>Share</th><th>Ecotype</th><th>Food</th><th>Rotten Meat</th><th>Egg Familiarity</th><th>Risk</th><th>Terrain</th><th>Collision</th><th>Attack</th><th>Movement</th><th>Egg Laying</th><th>Body Block Move</th><th>Body Block Attack</th><th>Small Attack</th><th>Large Approach Attack</th></tr></thead>");
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
                $"<td>{Html(behavior.EggFamiliarityResponse)}</td>" +
                $"<td>{Html(behavior.RiskResponse)}</td>" +
                $"<td>{Html(behavior.TerrainResponse)}</td>" +
                $"<td>{Html(behavior.CollisionResponse)}</td>" +
                $"<td>{Html(behavior.PredatorTendency)}</td>" +
                $"<td>{Html(behavior.MovementStyle)}</td>" +
                $"<td>{Html(behavior.ReproductionTendency)}</td>" +
                $"<td>{Html(behavior.CreatureBlocked.MoveForward.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(behavior.CreatureBlocked.AttackShare))}</td>" +
                $"<td>{Html(FormatPercent(behavior.SmallCreatureAhead.AttackShare))}</td>" +
                $"<td>{Html(FormatPercent(behavior.LargeCreatureApproaching.AttackShare))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteRtNeatBrainGraphSection(StreamWriter writer, WorldState state)
    {
        var candidates = SelectRtNeatBrainGraphs(state, RtNeatGraphRenderLimit);
        if (candidates.Count == 0)
        {
            return;
        }

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>rtNEAT Brain Graphs</h2>");
        writer.WriteLine("<p class=\"biome-map-note\">Representative living graph brains. Connected inputs are shown on the left, hidden nodes in the middle, and physical action outputs on the right. Green links are positive weights, red links are negative weights, and gray links are disabled.</p>");
        foreach (var candidate in candidates)
        {
            writer.WriteLine("<div class=\"rtneat-panel\">");
            writer.WriteLine("<div class=\"rtneat-graph-frame\">");
            WriteRtNeatBrainGraphSvg(writer, candidate.Brain);
            writer.WriteLine("</div>");
            writer.WriteLine("<aside class=\"rtneat-detail\">");
            writer.WriteLine($"<h3>Brain #{Html(candidate.BrainId)}</h3>");
            writer.WriteLine("<dl>");
            WriteDefinition(writer, "Living", candidate.LivingCreatures.ToString(CultureInfo.InvariantCulture));
            WriteDefinition(writer, "Eggs", candidate.Eggs.ToString(CultureInfo.InvariantCulture));
            WriteDefinition(writer, "Nodes", candidate.Brain.Nodes.Length.ToString(CultureInfo.InvariantCulture));
            WriteDefinition(writer, "Hidden", candidate.Brain.HiddenNodeCount.ToString(CultureInfo.InvariantCulture));
            WriteDefinition(writer, "Connections", candidate.Brain.ConnectionCount.ToString(CultureInfo.InvariantCulture));
            WriteDefinition(writer, "Enabled", candidate.Brain.EnabledConnectionCount.ToString(CultureInfo.InvariantCulture));
            WriteDefinition(writer, "Weights", candidate.Brain.WeightCount.ToString(CultureInfo.InvariantCulture));
            WriteDefinition(writer, "Schema", $"input v{candidate.Brain.InputSchemaVersion}, output v{candidate.Brain.OutputSchemaVersion}");
            writer.WriteLine("</dl>");
            writer.WriteLine("</aside>");
            writer.WriteLine("</div>");
        }

        writer.WriteLine("</section>");
    }

    private static IReadOnlyList<RtNeatBrainGraphCandidate> SelectRtNeatBrainGraphs(WorldState state, int limit)
    {
        var livingByBrain = new Dictionary<int, int>();
        foreach (var creature in state.Creatures)
        {
            if (creature.BrainId >= 0)
            {
                livingByBrain[creature.BrainId] = livingByBrain.GetValueOrDefault(creature.BrainId) + 1;
            }
        }

        var eggsByBrain = new Dictionary<int, int>();
        foreach (var egg in state.Eggs)
        {
            if (egg.BrainId >= 0)
            {
                eggsByBrain[egg.BrainId] = eggsByBrain.GetValueOrDefault(egg.BrainId) + 1;
            }
        }

        return livingByBrain.Keys
            .Concat(eggsByBrain.Keys)
            .Distinct()
            .Select(brainId =>
            {
                if (!state.TryGetBrain(brainId, out var brain) || brain?.RtNeat is null)
                {
                    return default;
                }

                return new RtNeatBrainGraphCandidate(
                    brainId,
                    livingByBrain.GetValueOrDefault(brainId),
                    eggsByBrain.GetValueOrDefault(brainId),
                    brain.RtNeat);
            })
            .Where(candidate => candidate.Brain is not null)
            .OrderByDescending(candidate => candidate.LivingCreatures)
            .ThenByDescending(candidate => candidate.Eggs)
            .ThenByDescending(candidate => candidate.Brain.HiddenNodeCount)
            .ThenByDescending(candidate => candidate.Brain.ConnectionCount)
            .ThenByDescending(candidate => candidate.Brain.EnabledConnectionCount)
            .ThenBy(candidate => candidate.BrainId)
            .Take(limit)
            .ToArray();
    }

    private static void WriteRtNeatBrainGraphSvg(TextWriter writer, RtNeatBrainGenome brain)
    {
        const float width = 980f;
        const float leftX = 145f;
        const float outputX = 835f;
        const float topPadding = 54f;
        const float bottomPadding = 38f;

        var nodeById = brain.Nodes.ToDictionary(node => node.Id);
        var visibleNodeIds = new HashSet<int>();
        foreach (var connection in brain.Connections)
        {
            visibleNodeIds.Add(connection.SourceNodeId);
            visibleNodeIds.Add(connection.TargetNodeId);
        }

        foreach (var node in brain.Nodes)
        {
            if (node.Kind != RtNeatNodeKind.Input)
            {
                visibleNodeIds.Add(node.Id);
            }
        }

        var visibleNodes = brain.Nodes
            .Where(node => visibleNodeIds.Contains(node.Id))
            .ToArray();
        var inputs = visibleNodes
            .Where(node => node.Kind == RtNeatNodeKind.Input)
            .OrderBy(node => node.Key, StringComparer.Ordinal)
            .ToArray();
        var hidden = visibleNodes
            .Where(node => node.Kind == RtNeatNodeKind.Hidden)
            .OrderBy(node => node.Depth)
            .ThenBy(node => node.Id)
            .ToArray();
        var outputs = visibleNodes
            .Where(node => node.Kind == RtNeatNodeKind.Output)
            .OrderBy(node => node.Key, StringComparer.Ordinal)
            .ToArray();
        var rowCount = Math.Max(1, Math.Max(inputs.Length, Math.Max(hidden.Length, outputs.Length)));
        var height = MathF.Max(360f, topPadding + bottomPadding + rowCount * 44f);
        var positions = new Dictionary<int, (float X, float Y)>();

        for (var i = 0; i < inputs.Length; i++)
        {
            positions[inputs[i].Id] = (leftX, DistributeY(i, inputs.Length, height, topPadding, bottomPadding));
        }

        for (var i = 0; i < hidden.Length; i++)
        {
            var depth = Math.Clamp(hidden[i].Depth, 0.05f, 0.95f);
            positions[hidden[i].Id] = (250f + depth * 470f, DistributeY(i, hidden.Length, height, topPadding, bottomPadding));
        }

        for (var i = 0; i < outputs.Length; i++)
        {
            positions[outputs[i].Id] = (outputX, DistributeY(i, outputs.Length, height, topPadding, bottomPadding));
        }

        writer.WriteLine($"<svg class=\"rtneat-graph\" viewBox=\"0 0 {Svg(width)} {Svg(height)}\" role=\"img\" aria-label=\"rtNEAT graph brain\">");
        writer.WriteLine($"<rect x=\"0\" y=\"0\" width=\"{Svg(width)}\" height=\"{Svg(height)}\" fill=\"#fbfcf8\"/>");
        writer.WriteLine("<text x=\"50\" y=\"26\" class=\"rtneat-node-kind\">inputs</text>");
        writer.WriteLine("<text x=\"470\" y=\"26\" text-anchor=\"middle\" class=\"rtneat-node-kind\">hidden</text>");
        writer.WriteLine("<text x=\"760\" y=\"26\" class=\"rtneat-node-kind\">actions</text>");

        foreach (var connection in brain.Connections.OrderBy(connection => connection.Enabled ? 1 : 0).ThenBy(connection => Math.Abs(connection.Weight)))
        {
            if (!positions.TryGetValue(connection.SourceNodeId, out var source)
                || !positions.TryGetValue(connection.TargetNodeId, out var target)
                || !nodeById.TryGetValue(connection.SourceNodeId, out var sourceNode)
                || !nodeById.TryGetValue(connection.TargetNodeId, out var targetNode))
            {
                continue;
            }

            var weightMagnitude = Math.Abs(connection.Weight);
            var color = !connection.Enabled
                ? "#9ca3af"
                : connection.Weight >= 0f ? "#15803d" : "#dc2626";
            var opacity = !connection.Enabled
                ? 0.18f
                : Math.Clamp(0.25f + weightMagnitude / 5f, 0.25f, 0.82f);
            var strokeWidth = !connection.Enabled
                ? 1f
                : Math.Clamp(1f + weightMagnitude * 0.85f, 1f, 5f);
            var curveA = source.X + MathF.Max(70f, (target.X - source.X) * 0.42f);
            var curveB = target.X - MathF.Max(70f, (target.X - source.X) * 0.42f);

            writer.WriteLine(
                $"<path d=\"M {Svg(source.X)} {Svg(source.Y)} C {Svg(curveA)} {Svg(source.Y)}, {Svg(curveB)} {Svg(target.Y)}, {Svg(target.X)} {Svg(target.Y)}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"{Svg(strokeWidth)}\" opacity=\"{Svg(opacity)}\">" +
                $"<title>{Html(ShortRtNeatNodeLabel(sourceNode))} -> {Html(ShortRtNeatNodeLabel(targetNode))}: {Html(connection.Weight.ToString("0.###", CultureInfo.InvariantCulture))}{(connection.Enabled ? string.Empty : " disabled")}</title></path>");
        }

        foreach (var node in inputs)
        {
            WriteRtNeatRectNode(writer, node, positions[node.Id], 190f, "#e0f2fe", "#0369a1");
        }

        foreach (var node in outputs)
        {
            WriteRtNeatRectNode(writer, node, positions[node.Id], 190f, "#ecfdf5", "#15803d");
        }

        foreach (var node in hidden)
        {
            var position = positions[node.Id];
            writer.WriteLine($"<circle cx=\"{Svg(position.X)}\" cy=\"{Svg(position.Y)}\" r=\"18\" fill=\"#fef3c7\" stroke=\"#b45309\" stroke-width=\"2\"><title>{Html(ShortRtNeatNodeLabel(node))}, bias {Html(node.Bias.ToString("0.###", CultureInfo.InvariantCulture))}, {Html(node.Activation)}</title></circle>");
            writer.WriteLine($"<text x=\"{Svg(position.X)}\" y=\"{Svg(position.Y - 2f)}\" text-anchor=\"middle\" class=\"rtneat-node-label\">h{Html(node.Id)}</text>");
            writer.WriteLine($"<text x=\"{Svg(position.X)}\" y=\"{Svg(position.Y + 10f)}\" text-anchor=\"middle\" class=\"rtneat-node-kind\">{Html(ShortRtNeatActivationLabel(node.Activation))} b{Html(FormatRtNeatBias(node.Bias))}</text>");
        }

        writer.WriteLine("</svg>");
    }

    private static void WriteRtNeatRectNode(
        TextWriter writer,
        RtNeatNodeGene node,
        (float X, float Y) position,
        float width,
        string fill,
        string stroke)
    {
        var x = position.X - width / 2f;
        var y = position.Y - 17f;
        writer.WriteLine($"<rect x=\"{Svg(x)}\" y=\"{Svg(y)}\" width=\"{Svg(width)}\" height=\"34\" rx=\"6\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"1.5\"><title>{Html(node.Key)}, bias {Html(node.Bias.ToString("0.###", CultureInfo.InvariantCulture))}</title></rect>");
        writer.WriteLine($"<text x=\"{Svg(position.X)}\" y=\"{Svg(position.Y - 1f)}\" text-anchor=\"middle\" class=\"rtneat-node-label\">{Html(ShortRtNeatNodeLabel(node))}</text>");
        writer.WriteLine($"<text x=\"{Svg(position.X)}\" y=\"{Svg(position.Y + 11f)}\" text-anchor=\"middle\" class=\"rtneat-node-kind\">{Html(node.Kind)}</text>");
    }

    private static void WriteDefinition(TextWriter writer, string term, string value)
    {
        writer.WriteLine($"<dt>{Html(term)}</dt><dd>{Html(value)}</dd>");
    }

    private static float DistributeY(int index, int count, float height, float topPadding, float bottomPadding)
    {
        if (count <= 1)
        {
            return (topPadding + height - bottomPadding) * 0.5f;
        }

        return topPadding + index * ((height - topPadding - bottomPadding) / (count - 1));
    }

    private static string ShortRtNeatNodeLabel(RtNeatNodeGene node)
    {
        if (node.Kind == RtNeatNodeKind.Hidden)
        {
            return $"h{node.Id}";
        }

        var key = node.Key
            .Replace("vision.", "vis.", StringComparison.Ordinal)
            .Replace("internal.", "int.", StringComparison.Ordinal)
            .Replace("contact.", "touch.", StringComparison.Ordinal)
            .Replace("terrain.", "ter.", StringComparison.Ordinal)
            .Replace("habitat.", "hab.", StringComparison.Ordinal)
            .Replace("obstacle.", "obs.", StringComparison.Ordinal)
            .Replace("action.", string.Empty, StringComparison.Ordinal);
        return key.Length <= 24 ? key : $"{key[..21]}...";
    }

    private static string ShortRtNeatActivationLabel(RtNeatActivationKind activation)
    {
        return activation switch
        {
            RtNeatActivationKind.Tanh => "tanh",
            RtNeatActivationKind.Sigmoid => "sig",
            RtNeatActivationKind.Relu => "relu",
            RtNeatActivationKind.Linear => "lin",
            _ => activation.ToString()
        };
    }

    private static string FormatRtNeatBias(float bias)
    {
        return bias.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture);
    }

    private static string Svg(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static void WriteBrainInputDiagnosticsSection(StreamWriter writer, BrainInputDiagnosticSummary summary)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Sensory Brain Wiring</h2>");
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
        WriteMetric(writer, "Direct small-creature sectors", FormatBrainWeight(summary.DirectSmallerCreatureSectorWeightMagnitude));
        WriteMetric(writer, "Direct similar-creature sectors", FormatBrainWeight(summary.DirectSimilarCreatureSectorWeightMagnitude));
        WriteMetric(writer, "Direct large-creature sectors", FormatBrainWeight(summary.DirectLargerCreatureSectorWeightMagnitude));
        WriteMetric(writer, "Direct approach sectors", FormatBrainWeight(summary.DirectCreatureApproachSectorWeightMagnitude));
        WriteMetric(writer, "Direct facing sectors", FormatBrainWeight(summary.DirectCreatureFacingSectorWeightMagnitude));
        WriteMetric(writer, "Hidden freshness magnitude", FormatBrainWeight(summary.HiddenFreshnessWeightMagnitude));
        WriteMetric(writer, "Hidden rot-scent magnitude", FormatBrainWeight(summary.HiddenRotScentWeightMagnitude));
        WriteMetric(writer, "Hidden small-creature sectors", FormatBrainWeight(summary.HiddenSmallerCreatureSectorWeightMagnitude));
        WriteMetric(writer, "Hidden similar-creature sectors", FormatBrainWeight(summary.HiddenSimilarCreatureSectorWeightMagnitude));
        WriteMetric(writer, "Hidden large-creature sectors", FormatBrainWeight(summary.HiddenLargerCreatureSectorWeightMagnitude));
        WriteMetric(writer, "Hidden approach sectors", FormatBrainWeight(summary.HiddenCreatureApproachSectorWeightMagnitude));
        WriteMetric(writer, "Hidden facing sectors", FormatBrainWeight(summary.HiddenCreatureFacingSectorWeightMagnitude));
        WriteMetric(writer, "Move from freshness", FormatSignedBrainWeight(summary.MoveFreshnessWeight));
        WriteMetric(writer, "Eat from freshness", FormatSignedBrainWeight(summary.EatFreshnessWeight));
        WriteMetric(writer, "Move from rot ahead", FormatSignedBrainWeight(summary.MoveRotScentForwardWeight));
        WriteMetric(writer, "Turn from rot right", FormatSignedBrainWeight(summary.TurnRotScentRightWeight));
        WriteMetric(writer, "Attack small-creature sectors", FormatSignedBrainWeight(summary.AttackSmallerCreatureSectorWeight));
        WriteMetric(writer, "Attack large-creature sectors", FormatSignedBrainWeight(summary.AttackLargerCreatureSectorWeight));
        WriteMetric(writer, "Attack approach sectors", FormatSignedBrainWeight(summary.AttackCreatureApproachSectorWeight));
        WriteMetric(writer, "Attack facing sectors", FormatSignedBrainWeight(summary.AttackCreatureFacingSectorWeight));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");
    }

    private static void WriteLineageBrainInputDiagnosticsSection(
        StreamWriter writer,
        IReadOnlyList<LineageBrainInputDiagnosticSummary> summaries)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Lineage Sensory Wiring</h2>");
        if (summaries.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living founder lineages were available for brain-input diagnostics.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Founder</th><th>Living</th><th>Share</th><th>Fresh Direct</th><th>Rot Direct</th><th>Small Direct</th><th>Similar Direct</th><th>Large Direct</th><th>Approach Direct</th><th>Facing Direct</th><th>Fresh Hidden</th><th>Rot Hidden</th><th>Small Hidden</th><th>Similar Hidden</th><th>Large Hidden</th><th>Approach Hidden</th><th>Facing Hidden</th><th>Move Fresh</th><th>Eat Fresh</th><th>Move Rot Ahead</th><th>Turn Rot Right</th><th>Attack Small</th><th>Attack Large</th><th>Attack Approach</th><th>Attack Facing</th></tr></thead>");
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
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectSmallerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectSimilarCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectLargerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectCreatureApproachSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectCreatureFacingSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenFreshnessWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenRotScentWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenSmallerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenSimilarCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenLargerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenCreatureApproachSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenCreatureFacingSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveFreshnessWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.EatFreshnessWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveRotScentForwardWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.TurnRotScentRightWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackSmallerCreatureSectorWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackLargerCreatureSectorWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackCreatureApproachSectorWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackCreatureFacingSectorWeight))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpeciesBrainInputDiagnosticsSection(
        StreamWriter writer,
        IReadOnlyList<SpeciesClusterBrainInputDiagnosticSummary> summaries)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Species Sensory Wiring</h2>");
        if (summaries.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living neural species clusters were available for brain-input diagnostics.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Living</th><th>Evaluated</th><th>Fresh Direct</th><th>Rot Direct</th><th>Small Direct</th><th>Similar Direct</th><th>Large Direct</th><th>Approach Direct</th><th>Facing Direct</th><th>Fresh Hidden</th><th>Rot Hidden</th><th>Small Hidden</th><th>Similar Hidden</th><th>Large Hidden</th><th>Approach Hidden</th><th>Facing Hidden</th><th>Move Fresh</th><th>Eat Fresh</th><th>Move Rot Density</th><th>Turn Rot Density</th><th>Move Rot Ahead</th><th>Turn Rot Right</th><th>Attack Small</th><th>Attack Large</th><th>Attack Approach</th><th>Attack Facing</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in summaries)
        {
            var diagnostics = summary.Diagnostics;
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.Rank)}</td>" +
                $"<td>{Html(summary.Name)}</td>" +
                $"<td>{Html($"{summary.LivingCreatures} ({FormatPercent(summary.LivingShare)})")}</td>" +
                $"<td>{Html(diagnostics.EvaluatedCreatureCount)}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectFreshnessWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectRotScentWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectSmallerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectSimilarCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectLargerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectCreatureApproachSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectCreatureFacingSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenFreshnessWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenRotScentWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenSmallerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenSimilarCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenLargerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenCreatureApproachSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenCreatureFacingSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveFreshnessWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.EatFreshnessWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveRotScentDensityWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.TurnRotScentDensityWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveRotScentForwardWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.TurnRotScentRightWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackSmallerCreatureSectorWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackLargerCreatureSectorWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackCreatureApproachSectorWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackCreatureFacingSectorWeight))}</td>" +
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

    private static ChartSeries[] DownsampleChartSeries(IReadOnlyList<ChartSeries> series, int maxPoints)
    {
        if (maxPoints < 2)
        {
            return series.ToArray();
        }

        var result = new ChartSeries[series.Count];
        for (var i = 0; i < series.Count; i++)
        {
            var chartSeries = series[i];
            result[i] = chartSeries.Values.Length <= maxPoints
                ? chartSeries
                : chartSeries with { Values = DownsampleValues(chartSeries.Values, maxPoints) };
        }

        return result;
    }

    private static float[] DownsampleValues(float[] values, int maxPoints)
    {
        if (values.Length <= maxPoints)
        {
            return values;
        }

        var selected = new float[maxPoints];
        var lastIndex = -1;
        var selectedCount = 0;
        for (var i = 0; i < maxPoints; i++)
        {
            var index = (int)Math.Round(i * (values.Length - 1) / (double)(maxPoints - 1));
            if (index == lastIndex)
            {
                continue;
            }

            selected[selectedCount++] = values[index];
            lastIndex = index;
        }

        if (selectedCount == selected.Length)
        {
            return selected;
        }

        Array.Resize(ref selected, selectedCount);
        return selected;
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

    private static IReadOnlyList<SimulationStatsSnapshot> SelectReportSnapshots(
        IReadOnlyList<SimulationStatsSnapshot> snapshots)
    {
        if (snapshots.Count <= ReportTimelineSampleLimit)
        {
            return snapshots;
        }

        var selected = new List<SimulationStatsSnapshot>(ReportTimelineSampleLimit);
        var lastIndex = -1;
        for (var i = 0; i < ReportTimelineSampleLimit; i++)
        {
            var index = (int)Math.Round(i * (snapshots.Count - 1) / (double)(ReportTimelineSampleLimit - 1));
            if (index == lastIndex)
            {
                continue;
            }

            selected.Add(snapshots[index]);
            lastIndex = index;
        }

        return selected;
    }

    private static IReadOnlyList<long> SelectReportTicks(IReadOnlyList<long> ticks)
    {
        if (ticks.Count <= ReportTrendRowCount)
        {
            return ticks;
        }

        var selected = new List<long>();
        long? lastTick = null;
        for (var i = 0; i < ReportTrendRowCount; i++)
        {
            var index = (int)Math.Round(i * (ticks.Count - 1) / (double)(ReportTrendRowCount - 1));
            var tick = ticks[index];
            if (tick == lastTick)
            {
                continue;
            }

            selected.Add(tick);
            lastTick = tick;
        }

        return selected;
    }

    private static string FormatGenerationRange(int min, float average, int max)
    {
        return min == max
            ? $"{min} avg {average:0.##}"
            : $"{min}-{max} avg {average:0.##}";
    }

    private static string FormatThermalShares(ThermalLineageNicheSummary summary)
    {
        return $"{FormatPercent(summary.ColdTemperatureShare)} / {FormatPercent(summary.TemperateTemperatureShare)} / {FormatPercent(summary.HotTemperatureShare)}";
    }

    private static string FormatStressShares(ThermalLineageNicheSummary summary)
    {
        return $"{FormatPercent(summary.ColdThermalStressShare)} / {FormatPercent(summary.HotThermalStressShare)}";
    }

    private static string FormatSpeciesThermalShares(SpeciesClusterSummary summary)
    {
        return $"{FormatPercent(summary.ColdTemperatureLifetimeShare)} / {FormatPercent(summary.TemperateTemperatureLifetimeShare)} / {FormatPercent(summary.HotTemperatureLifetimeShare)}";
    }

    private static string FormatSpeciesStressShares(SpeciesClusterSummary summary)
    {
        return $"{FormatPercent(summary.ColdThermalStressLifetimeShare)} / {FormatPercent(summary.HotThermalStressLifetimeShare)}";
    }

    private static string FormatEcotypeThermalShares(ThermalEcotypeSummary summary)
    {
        return $"{FormatPercent(summary.ColdTemperatureShare)} / {FormatPercent(summary.TemperateTemperatureShare)} / {FormatPercent(summary.HotTemperatureShare)}";
    }

    private static string FormatEcotypeStressShares(ThermalEcotypeSummary summary)
    {
        return $"{FormatPercent(summary.ColdThermalStressShare)} / {FormatPercent(summary.HotThermalStressShare)}";
    }

    private static string FormatEcotypeTopFounders(IReadOnlyList<ThermalEcotypeFounderSummary> founders)
    {
        return founders.Count == 0
            ? "n/a"
            : string.Join(", ", founders.Select(founder => $"#{founder.FounderId.Value} ({founder.LivingCreatures})"));
    }

    private static string FormatThermalBandCounts(int cold, int temperate, int hot)
    {
        return $"{cold} cold / {temperate} temp / {hot} hot";
    }

    private static string FormatThermalBandValues(float cold, float temperate, float hot)
    {
        return $"{cold:0.#} cold / {temperate:0.#} temp / {hot:0.#} hot";
    }

    private static string FormatThermalStressCounts(SimulationStatsSnapshot snapshot)
    {
        return $"{snapshot.ComfortableThermalCreatureCount} comfortable / {snapshot.ColdThermalStressCreatureCount} cold / {snapshot.HotThermalStressCreatureCount} hot";
    }

    private static string FormatFounderNicheLabelCounts(IReadOnlyList<FounderSummary> summaries)
    {
        if (summaries.Count == 0)
        {
            return "No living founders";
        }

        return string.Join(
            ", ",
            summaries
                .GroupBy(summary => summary.ThermalNiche.NicheLabel)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => $"{group.Key} {group.Count()}"));
    }

    private static string FormatSpeciesNicheLabelCounts(IReadOnlyList<SpeciesClusterSummary> summaries)
    {
        if (summaries.Count == 0)
        {
            return "No living species";
        }

        return string.Join(
            ", ",
            summaries
                .GroupBy(summary => summary.ThermalNicheLabel)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => $"{group.Key} {group.Count()}"));
    }

    private static string FormatPlantAdaptation(SpeciesClusterSummary summary)
    {
        return $"T {summary.AverageTenderPlantAdaptation:0.##} / R {summary.AverageRichPlantAdaptation:0.##} / Tough {summary.AverageToughPlantAdaptation:0.##}";
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
            InitialBrainKind.SectorForager => "Sector forager",
            InitialBrainKind.ScavengerForager => "Scavenger forager",
            InitialBrainKind.FreshnessAwareScavenger => "Freshness-aware scavenger",
            InitialBrainKind.ForagerPredator => "Forager predator",
            InitialBrainKind.SparseGraphForager => "Sparse graph forager",
            InitialBrainKind.SparseGraphScavenger => "Sparse graph scavenger",
            InitialBrainKind.SparseGraphPredator => "Sparse graph predator",
            InitialBrainKind.RandomPerFounder => "Per-founder random weights",
            _ => kind.ToString()
        };
    }

    private static string FormatBrainArchitectureKind(BrainArchitectureKind kind)
    {
        return kind switch
        {
            BrainArchitectureKind.HybridNeural => "Hybrid neural",
            BrainArchitectureKind.HiddenLayerNeural => "Hidden-layer neural",
            BrainArchitectureKind.RtNeatGraph => "rtNEAT graph",
            BrainArchitectureKind.HybridDeep8x8Neural => "Hybrid deep 8x8 neural",
            BrainArchitectureKind.HiddenDeep8x8Neural => "Hidden deep 8x8 neural",
            _ => kind.ToString()
        };
    }

    private static void WriteScenarioSpeciesRosterSection(StreamWriter writer, SimulationScenario scenario, string? scenarioPath)
    {
        var seeds = scenario.EnabledSpeciesSeeds().ToArray();
        if (seeds.Length == 0)
        {
            return;
        }

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Starting Roster</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Profile</th><th>Brain</th><th>Count</th><th>Spawn region</th><th>Energy</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var seed in seeds)
        {
            writer.WriteLine("<tr>");
            writer.WriteLine($"<td>{Html(FormatScenarioSpeciesSeedName(seed, scenarioPath))}<br><small>{Html(seed.ProfilePath)}</small></td>");
            writer.WriteLine($"<td>{Html(FormatScenarioSpeciesSeedBrain(seed, scenarioPath))}</td>");
            writer.WriteLine($"<td>{Html(seed.Count.ToString(CultureInfo.InvariantCulture))}</td>");
            writer.WriteLine($"<td>{Html(seed.SpawnRegion.ToString())}</td>");
            writer.WriteLine($"<td>{Html(FormatScenarioSpeciesSeedEnergy(seed))}</td>");
            writer.WriteLine("</tr>");
        }

        writer.WriteLine("</tbody>");
        writer.WriteLine("</table></div>");
        writer.WriteLine("</section>");
    }

    private static string FormatScenarioSpeciesSeeds(SimulationScenario scenario, string? scenarioPath = null)
    {
        var seeds = scenario.EnabledSpeciesSeeds().ToArray();
        if (seeds.Length == 0)
        {
            return "None";
        }

        if (seeds.Length == 1)
        {
            var seed = seeds[0];
            return $"{seed.Count} x {FormatScenarioSpeciesSeedName(seed, scenarioPath)} using {FormatScenarioSpeciesSeedBrain(seed, scenarioPath)}";
        }

        var total = seeds.Sum(seed => seed.Count);
        return $"{total} creatures across {seeds.Length} roster entries";
    }

    private static string FormatSpeciesProfileName(string profilePath, string? scenarioPath)
    {
        try
        {
            var profile = SpeciesProfileJson.Load(SimulationScenarioSpeciesSeeder.ResolveProfilePath(profilePath, scenarioPath));
            return string.IsNullOrWhiteSpace(profile.Name)
                ? Path.GetFileName(profilePath)
                : profile.Name;
        }
        catch
        {
            return Path.GetFileName(profilePath);
        }
    }

    private static string FormatScenarioSpeciesSeedName(SpeciesScenarioSeed seed, string? scenarioPath)
    {
        return string.IsNullOrWhiteSpace(seed.Label)
            ? FormatSpeciesProfileName(seed.ProfilePath, scenarioPath)
            : seed.Label;
    }

    private static string FormatScenarioSpeciesSeedBrain(SpeciesScenarioSeed seed, string? scenarioPath)
    {
        if (!string.IsNullOrWhiteSpace(seed.BrainProfilePath))
        {
            return $"{FormatBrainProfileName(seed.BrainProfilePath, seed.ProfilePath, scenarioPath)} brain profile";
        }

        if (seed.BrainOverrideKind is not null)
        {
            return $"{FormatInitialBrainKind(seed.BrainOverrideKind.Value)} generated brain";
        }

        try
        {
            var speciesPath = SimulationScenarioSpeciesSeeder.ResolveProfilePath(seed.ProfilePath, scenarioPath);
            var profile = SpeciesProfileJson.Load(speciesPath);
            if (!string.IsNullOrWhiteSpace(profile.DefaultBrainPath))
            {
                return $"{FormatBrainProfileName(profile.DefaultBrainPath, seed.ProfilePath, scenarioPath)} default brain profile";
            }
        }
        catch
        {
            // Fall through to the embedded profile brain label.
        }

        return "embedded profile brain";
    }

    private static string FormatBrainProfileName(string brainProfilePath, string speciesProfilePath, string? scenarioPath)
    {
        try
        {
            var resolvedSpeciesPath = SimulationScenarioSpeciesSeeder.ResolveProfilePath(speciesProfilePath, scenarioPath);
            var resolvedBrainPath = SimulationScenarioSpeciesSeeder.ResolveBrainProfilePath(
                brainProfilePath,
                resolvedSpeciesPath,
                scenarioPath);
            var profile = BrainProfileJson.Load(resolvedBrainPath);
            return string.IsNullOrWhiteSpace(profile.Name)
                ? Path.GetFileName(brainProfilePath)
                : profile.Name;
        }
        catch
        {
            return Path.GetFileName(brainProfilePath);
        }
    }

    private static string FormatScenarioSpeciesSeedEnergy(SpeciesScenarioSeed seed)
    {
        return seed.EnergyOverride is null
            ? "profile default"
            : seed.EnergyOverride.Value.ToString("0.###", CultureInfo.InvariantCulture);
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

    private static string FormatTemperatureIndex(float temperature)
    {
        return $"{Math.Clamp(temperature, 0f, 1f) * 100f:0.#}";
    }

    private static string FormatChange(string earlyValue, string finalValue)
    {
        return string.Equals(earlyValue, finalValue, StringComparison.Ordinal)
            ? finalValue
            : $"{earlyValue} -> {finalValue}";
    }

    private static string FormatDelta(float value)
    {
        return value.ToString("+0.###;-0.###;0", CultureInfo.InvariantCulture);
    }

    private static string FormatPlantRelocations(SimulationStats stats)
    {
        return $"local {stats.PlantLocalDispersalCount}, cluster {stats.PlantClusterRelocationCount}, global {stats.PlantGlobalRelocationCount}";
    }

    private static void WritePlantTypeDiagnosticsSection(StreamWriter writer, SimulationStatsSnapshot snapshot)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Plant Type Diagnostics</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Type</th><th>Resources</th><th>Plant kcal</th><th>Raw eaten/s</th><th>Intake share</th><th>Raw/resource</th><th>Digested energy/s</th><th>Adaptation</th><th>Payoff trace</th></tr></thead>");
        writer.WriteLine("<tbody>");
        WritePlantTypeDiagnosticsRow(
            writer,
            "Generic",
            GenericPlantTypeResourceCount(snapshot),
            GenericPlantTypeCalories(snapshot),
            GenericPlantCaloriesEatenPerSecond(snapshot),
            snapshot.TotalPlantCaloriesEatenPerSecond,
            GenericPlantDigestedEnergyPerSecond(snapshot),
            null,
            null);
        WritePlantTypeDiagnosticsRow(
            writer,
            "Tender",
            snapshot.TenderPlantTypeResourceCount,
            snapshot.TenderPlantTypeCalories,
            snapshot.TenderPlantCaloriesEatenPerSecond,
            snapshot.TotalPlantCaloriesEatenPerSecond,
            snapshot.TenderPlantDigestedEnergyPerSecond,
            snapshot.AverageTenderPlantAdaptation,
            snapshot.AverageTenderPlantPayoffTrace);
        WritePlantTypeDiagnosticsRow(
            writer,
            "Rich",
            snapshot.RichPlantTypeResourceCount,
            snapshot.RichPlantTypeCalories,
            snapshot.RichPlantCaloriesEatenPerSecond,
            snapshot.TotalPlantCaloriesEatenPerSecond,
            snapshot.RichPlantDigestedEnergyPerSecond,
            snapshot.AverageRichPlantAdaptation,
            snapshot.AverageRichPlantPayoffTrace);
        WritePlantTypeDiagnosticsRow(
            writer,
            "Tough",
            snapshot.ToughPlantTypeResourceCount,
            snapshot.ToughPlantTypeCalories,
            snapshot.ToughPlantCaloriesEatenPerSecond,
            snapshot.TotalPlantCaloriesEatenPerSecond,
            snapshot.ToughPlantDigestedEnergyPerSecond,
            snapshot.AverageToughPlantAdaptation,
            snapshot.AverageToughPlantPayoffTrace);
        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WritePlantTypeDiagnosticsRow(
        StreamWriter writer,
        string label,
        int resources,
        float plantCalories,
        float rawEatenPerSecond,
        float totalPlantEatenPerSecond,
        float digestedEnergyPerSecond,
        float? adaptation,
        float? payoffTrace)
    {
        writer.WriteLine(
            "<tr>" +
            $"<td>{Html(label)}</td>" +
            $"<td>{Html(resources)}</td>" +
            $"<td>{Html(plantCalories.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(rawEatenPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(FormatPercent(PlantTypeShare(rawEatenPerSecond, totalPlantEatenPerSecond)))}</td>" +
            $"<td>{Html(FormatPlantTypeIntakePerResource(rawEatenPerSecond, resources))}</td>" +
            $"<td>{Html(digestedEnergyPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(adaptation.HasValue ? adaptation.Value.ToString("0.###", CultureInfo.InvariantCulture) : "n/a")}</td>" +
            $"<td>{Html(payoffTrace.HasValue ? payoffTrace.Value.ToString("0.###", CultureInfo.InvariantCulture) : "n/a")}</td>" +
            "</tr>");
    }

    private static string FormatPlantTypeMix(SimulationScenario scenario)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"generic {scenario.GenericPlantWeight:0.###}, tender {scenario.TenderPlantWeight:0.###}, rich {scenario.RichPlantWeight:0.###}, tough {scenario.ToughPlantWeight:0.###}");
    }

    private static string FormatPlantTypeCalories(SimulationStatsSnapshot snapshot)
    {
        return FormatPlantTypeValues(
            GenericPlantTypeCalories(snapshot),
            snapshot.TenderPlantTypeCalories,
            snapshot.RichPlantTypeCalories,
            snapshot.ToughPlantTypeCalories,
            "0");
    }

    private static string FormatPlantTypeIntake(SimulationStatsSnapshot snapshot)
    {
        return FormatPlantTypeValues(
            GenericPlantCaloriesEatenPerSecond(snapshot),
            snapshot.TenderPlantCaloriesEatenPerSecond,
            snapshot.RichPlantCaloriesEatenPerSecond,
            snapshot.ToughPlantCaloriesEatenPerSecond,
            "0.###");
    }

    private static string FormatPlantTypeDigestion(SimulationStatsSnapshot snapshot)
    {
        return FormatPlantTypeValues(
            GenericPlantDigestedEnergyPerSecond(snapshot),
            snapshot.TenderPlantDigestedEnergyPerSecond,
            snapshot.RichPlantDigestedEnergyPerSecond,
            snapshot.ToughPlantDigestedEnergyPerSecond,
            "0.###");
    }

    private static string FormatPlantPayoffTraces(SimulationStatsSnapshot snapshot)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"tender {snapshot.AverageTenderPlantPayoffTrace:0.###}, rich {snapshot.AverageRichPlantPayoffTrace:0.###}, tough {snapshot.AverageToughPlantPayoffTrace:0.###}");
    }

    private static string FormatPlantTypeValues(float generic, float tender, float rich, float tough, string format)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"generic {generic.ToString(format, CultureInfo.InvariantCulture)}, tender {tender.ToString(format, CultureInfo.InvariantCulture)}, rich {rich.ToString(format, CultureInfo.InvariantCulture)}, tough {tough.ToString(format, CultureInfo.InvariantCulture)}");
    }

    private static int GenericPlantTypeResourceCount(SimulationStatsSnapshot snapshot)
    {
        return Math.Max(
            0,
            snapshot.PlantResourceCount
            - snapshot.TenderPlantTypeResourceCount
            - snapshot.RichPlantTypeResourceCount
            - snapshot.ToughPlantTypeResourceCount);
    }

    private static float GenericPlantTypeCalories(SimulationStatsSnapshot snapshot)
    {
        return Math.Max(
            0f,
            snapshot.TotalPlantCalories
            - snapshot.TenderPlantTypeCalories
            - snapshot.RichPlantTypeCalories
            - snapshot.ToughPlantTypeCalories);
    }

    private static float GenericPlantCaloriesEatenPerSecond(SimulationStatsSnapshot snapshot)
    {
        return Math.Max(
            0f,
            snapshot.TotalPlantCaloriesEatenPerSecond
            - snapshot.TenderPlantCaloriesEatenPerSecond
            - snapshot.RichPlantCaloriesEatenPerSecond
            - snapshot.ToughPlantCaloriesEatenPerSecond);
    }

    private static float GenericPlantDigestedEnergyPerSecond(SimulationStatsSnapshot snapshot)
    {
        return Math.Max(
            0f,
            snapshot.TotalPlantDigestedEnergyPerSecond
            - snapshot.TenderPlantDigestedEnergyPerSecond
            - snapshot.RichPlantDigestedEnergyPerSecond
            - snapshot.ToughPlantDigestedEnergyPerSecond);
    }

    private static float PlantTypeShare(float plantTypeCaloriesPerSecond, float totalPlantCaloriesPerSecond)
    {
        return totalPlantCaloriesPerSecond > 0f
            ? Math.Clamp(plantTypeCaloriesPerSecond / totalPlantCaloriesPerSecond, 0f, 1f)
            : 0f;
    }

    private static float PlantTypeIntakePerResource(float plantTypeCaloriesPerSecond, int resourceCount)
    {
        return resourceCount > 0
            ? plantTypeCaloriesPerSecond / resourceCount
            : 0f;
    }

    private static string FormatPlantTypeIntakePerResource(float plantTypeCaloriesPerSecond, int resourceCount)
    {
        return resourceCount > 0
            ? PlantTypeIntakePerResource(plantTypeCaloriesPerSecond, resourceCount).ToString("0.###", CultureInfo.InvariantCulture)
            : "n/a";
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

    private static string FormatIndex(float value)
    {
        return float.IsFinite(value)
            ? $"{value.ToString("0.##", CultureInfo.InvariantCulture)}x"
            : "n/a";
    }

    private static float EastProgressShare(float x, WorldBounds bounds)
    {
        return bounds.Width > 0f
            ? Math.Clamp(x / bounds.Width, 0f, 1f)
            : 0f;
    }

    private static string FormatBiomeKind(BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome).ToString();
    }

    private static string BiomeColor(BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => "#c7b56f",
            BiomeKind.Scrubland => "#8d8d49",
            BiomeKind.Grassland => "#58ad57",
            BiomeKind.Fertile => "#2f8f43",
            BiomeKind.Forest => "#123d22",
            BiomeKind.Wetland => "#2e8a8a",
            BiomeKind.Tundra => "#b4c2c5",
            BiomeKind.Highland => "#887b68",
            _ => "#58ad57"
        };
    }

    private static string TemperatureColor(float temperature)
    {
        var value = Math.Clamp(temperature, 0f, 1f);
        if (value < 0.30f)
        {
            return InterpolateHexColor(0x2e, 0x57, 0xd3, 0x1b, 0x91, 0xa8, value / 0.30f);
        }

        if (value < 0.55f)
        {
            return InterpolateHexColor(0x1b, 0x91, 0xa8, 0x4b, 0x9b, 0x44, (value - 0.30f) / 0.25f);
        }

        if (value < 0.75f)
        {
            return InterpolateHexColor(0x4b, 0x9b, 0x44, 0xd6, 0x9b, 0x2f, (value - 0.55f) / 0.20f);
        }

        return InterpolateHexColor(0xd6, 0x9b, 0x2f, 0xc9, 0x49, 0x2e, (value - 0.75f) / 0.25f);
    }

    private static string InterpolateHexColor(
        int fromR,
        int fromG,
        int fromB,
        int toR,
        int toG,
        int toB,
        float amount)
    {
        var t = Math.Clamp(amount, 0f, 1f);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"#{LerpByte(fromR, toR, t):x2}{LerpByte(fromG, toG, t):x2}{LerpByte(fromB, toB, t):x2}");
    }

    private static int LerpByte(int from, int to, float amount)
    {
        return Math.Clamp((int)MathF.Round(from + (to - from) * amount), 0, 255);
    }

    private static string SvgNumber(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatBiomePressureProfile(BiomePressureProfile profile)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Desert {profile.Desert:0.###}x, Scrubland {profile.Scrubland:0.###}x, Grassland {profile.Grassland:0.###}x, Fertile {profile.Fertile:0.###}x, Forest {profile.Forest:0.###}x, Wetland {profile.Wetland:0.###}x, Tundra {profile.Tundra:0.###}x, Highland {profile.Highland:0.###}x");
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
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => snapshot.BarrenCreatureCount,
            BiomeKind.Scrubland => snapshot.SparseCreatureCount,
            BiomeKind.Grassland => snapshot.GrasslandCreatureCount,
            BiomeKind.Fertile => snapshot.RichCreatureCount,
            BiomeKind.Forest => snapshot.ForestCreatureCount,
            BiomeKind.Wetland => snapshot.WetlandCreatureCount,
            BiomeKind.Tundra => snapshot.TundraCreatureCount,
            BiomeKind.Highland => snapshot.HighlandCreatureCount,
            _ => 0
        };
    }

    private static float MeatCaloriesForBiome(SimulationStatsSnapshot snapshot, BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => snapshot.BarrenMeatCalories,
            BiomeKind.Scrubland => snapshot.SparseMeatCalories,
            BiomeKind.Grassland => snapshot.GrasslandMeatCalories,
            BiomeKind.Fertile => snapshot.RichMeatCalories,
            BiomeKind.Forest => snapshot.ForestMeatCalories,
            BiomeKind.Wetland => snapshot.WetlandMeatCalories,
            BiomeKind.Tundra => snapshot.TundraMeatCalories,
            BiomeKind.Highland => snapshot.HighlandMeatCalories,
            _ => 0f
        };
    }

    private static float PlantCaloriesForBiome(SimulationStatsSnapshot snapshot, BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => snapshot.BarrenPlantCalories,
            BiomeKind.Scrubland => snapshot.SparsePlantCalories,
            BiomeKind.Grassland => snapshot.GrasslandPlantCalories,
            BiomeKind.Fertile => snapshot.RichPlantCalories,
            BiomeKind.Forest => snapshot.ForestPlantCalories,
            BiomeKind.Wetland => snapshot.WetlandPlantCalories,
            BiomeKind.Tundra => snapshot.TundraPlantCalories,
            BiomeKind.Highland => snapshot.HighlandPlantCalories,
            _ => 0f
        };
    }

    private static float CaloriesEatenForBiome(SimulationStatsSnapshot snapshot, BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => snapshot.BarrenCaloriesEatenPerSecond,
            BiomeKind.Scrubland => snapshot.SparseCaloriesEatenPerSecond,
            BiomeKind.Grassland => snapshot.GrasslandCaloriesEatenPerSecond,
            BiomeKind.Fertile => snapshot.RichCaloriesEatenPerSecond,
            BiomeKind.Forest => snapshot.ForestCaloriesEatenPerSecond,
            BiomeKind.Wetland => snapshot.WetlandCaloriesEatenPerSecond,
            BiomeKind.Tundra => snapshot.TundraCaloriesEatenPerSecond,
            BiomeKind.Highland => snapshot.HighlandCaloriesEatenPerSecond,
            _ => 0f
        };
    }

    private static int DeathCountForBiome(SimulationStatsSnapshot snapshot, BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => snapshot.BarrenDeathCount,
            BiomeKind.Scrubland => snapshot.SparseDeathCount,
            BiomeKind.Grassland => snapshot.GrasslandDeathCount,
            BiomeKind.Fertile => snapshot.RichDeathCount,
            BiomeKind.Forest => snapshot.ForestDeathCount,
            BiomeKind.Wetland => snapshot.WetlandDeathCount,
            BiomeKind.Tundra => snapshot.TundraDeathCount,
            BiomeKind.Highland => snapshot.HighlandDeathCount,
            _ => 0
        };
    }

    private static void WriteBiomePreferenceSection(
        TextWriter writer,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        IReadOnlyList<BiomeSummary> biomeSummaries,
        float worldArea)
    {
        const int TailSnapshotCount = 100;

        var tailCount = Math.Min(TailSnapshotCount, snapshots.Count);
        var tailSnapshots = tailCount > 0
            ? snapshots.Skip(snapshots.Count - tailCount).ToArray()
            : Array.Empty<SimulationStatsSnapshot>();

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Biome Preference</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Biome</th><th>Area Share</th><th>Living Share</th><th>Preference</th><th>Plant kcal Share</th><th>Eaten Share</th><th>Death Share</th><th>Late Deaths</th></tr></thead>");
        writer.WriteLine("<tbody>");

        if (tailSnapshots.Length == 0)
        {
            writer.WriteLine("<tr><td colspan=\"8\" class=\"empty\">No stat snapshots were recorded.</td></tr>");
            writer.WriteLine("</tbody></table></div>");
            writer.WriteLine("</section>");
            return;
        }

        var first = tailSnapshots[0];
        var last = tailSnapshots[^1];
        var averageCreatures = Average(tailSnapshots, snapshot => snapshot.CreatureCount);
        var averagePlantCalories = Average(tailSnapshots, snapshot => snapshot.TotalPlantCalories);
        var averageCaloriesEaten = Average(tailSnapshots, snapshot => snapshot.TotalCaloriesEatenPerSecond);
        var lateDeaths = Math.Max(0, last.CreatureDeathCount - first.CreatureDeathCount);
        var activeBiomeSummaries = ActiveBiomeSummaries(biomeSummaries);

        foreach (var summary in activeBiomeSummaries)
        {
            var areaShare = summary.Area / worldArea;
            var averageLiving = Average(tailSnapshots, snapshot => CreatureCountForBiome(snapshot, summary.Kind));
            var livingShare = averageCreatures > 0f ? averageLiving / averageCreatures : 0f;
            var preference = areaShare > 0f ? livingShare / areaShare : 0f;
            var plantCaloriesShare = averagePlantCalories > 0f
                ? Average(tailSnapshots, snapshot => PlantCaloriesForBiome(snapshot, summary.Kind)) / averagePlantCalories
                : 0f;
            var eatenShare = averageCaloriesEaten > 0f
                ? Average(tailSnapshots, snapshot => CaloriesEatenForBiome(snapshot, summary.Kind)) / averageCaloriesEaten
                : 0f;
            var biomeLateDeaths = Math.Max(
                0,
                DeathCountForBiome(last, summary.Kind) - DeathCountForBiome(first, summary.Kind));
            var deathShare = lateDeaths > 0 ? Share(biomeLateDeaths, lateDeaths) : 0f;

            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(FormatBiomeKind(summary.Kind))}</td>" +
                $"<td>{Html(FormatPercent(areaShare))}</td>" +
                $"<td>{Html(FormatPercent(livingShare))}</td>" +
                $"<td>{Html(preference.ToString("0.##", CultureInfo.InvariantCulture))}x</td>" +
                $"<td>{Html(FormatPercent(plantCaloriesShare))}</td>" +
                $"<td>{Html(FormatPercent(eatenShare))}</td>" +
                $"<td>{Html(FormatPercent(deathShare))}</td>" +
                $"<td>{Html(biomeLateDeaths)}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteBiomeExposureSection(
        TextWriter writer,
        IReadOnlyList<BiomeSummary> biomeSummaries,
        SimulationSpatialHeatmaps heatmaps,
        float worldArea)
    {
        var totalExposureSeconds = HeatmapTotal(heatmaps.BiomeCreatureExposureSeconds);
        var activeBiomeSummaries = ActiveBiomeSummaries(biomeSummaries)
            .Where(summary => summary.Area > 0f || BiomeExposureSecondsFor(heatmaps, summary.Kind) > 0f)
            .ToArray();

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Biome Exposure</h2>");
        writer.WriteLine("<p class=\"biome-map-note\">Creature exposure is sampled on the stats snapshot interval. Lowering that interval makes these estimates closer to exact path occupancy.</p>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Biome</th><th>Area Share</th><th>Creature Hours</th><th>Exposure Share</th><th>Exposure Index</th></tr></thead>");
        writer.WriteLine("<tbody>");

        if (!heatmaps.HasExposure || totalExposureSeconds <= 0f || activeBiomeSummaries.Length == 0)
        {
            writer.WriteLine("<tr><td colspan=\"5\" class=\"empty\">No sampled creature exposure was recorded.</td></tr>");
            writer.WriteLine("</tbody></table></div>");
            writer.WriteLine("</section>");
            return;
        }

        foreach (var summary in activeBiomeSummaries)
        {
            var areaShare = summary.Area / worldArea;
            var exposureSeconds = BiomeExposureSecondsFor(heatmaps, summary.Kind);
            var exposureShare = exposureSeconds / totalExposureSeconds;
            var exposureIndex = areaShare > 0f ? exposureShare / areaShare : float.NaN;

            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(FormatBiomeKind(summary.Kind))}</td>" +
                $"<td>{Html(FormatPercent(areaShare))}</td>" +
                $"<td>{Html((exposureSeconds / 3600f).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(exposureShare))}</td>" +
                $"<td>{Html(FormatIndex(exposureIndex))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteBiomeRiskRewardSection(
        TextWriter writer,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        IReadOnlyList<BiomeSummary> biomeSummaries,
        float worldArea)
    {
        const int TailSnapshotCount = 100;

        var tailCount = Math.Min(TailSnapshotCount, snapshots.Count);
        var tailSnapshots = tailCount > 0
            ? snapshots.Skip(snapshots.Count - tailCount).ToArray()
            : Array.Empty<SimulationStatsSnapshot>();

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Biome Risk and Reward</h2>");
        writer.WriteLine("<p class=\"biome-map-note\">Uses the last up to 100 stat snapshots. Reward index compares food-eaten share to living share; risk index compares death share to living share. Values above 1x are overrepresented for the creatures using that biome.</p>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Biome</th><th>Area Share</th><th>Living Share</th><th>Preference</th><th>Plant Availability</th><th>Food / Creature / s</th><th>Reward Index</th><th>Late Deaths</th><th>Deaths / Creature Hr</th><th>Risk Index</th></tr></thead>");
        writer.WriteLine("<tbody>");

        if (tailSnapshots.Length == 0)
        {
            writer.WriteLine("<tr><td colspan=\"10\" class=\"empty\">No stat snapshots were recorded.</td></tr>");
            writer.WriteLine("</tbody></table></div>");
            writer.WriteLine("</section>");
            return;
        }

        var first = tailSnapshots[0];
        var last = tailSnapshots[^1];
        var tailHours = Math.Max(0d, last.ElapsedSeconds - first.ElapsedSeconds) / 3600d;
        var averageCreatures = Average(tailSnapshots, snapshot => snapshot.CreatureCount);
        var averagePlantCalories = Average(tailSnapshots, snapshot => snapshot.TotalPlantCalories);
        var averageCaloriesEaten = Average(tailSnapshots, snapshot => snapshot.TotalCaloriesEatenPerSecond);
        var lateDeaths = Math.Max(0, last.CreatureDeathCount - first.CreatureDeathCount);
        var activeBiomeSummaries = ActiveBiomeSummaries(biomeSummaries);

        foreach (var summary in activeBiomeSummaries)
        {
            var areaShare = summary.Area / worldArea;
            var averageLiving = Average(tailSnapshots, snapshot => CreatureCountForBiome(snapshot, summary.Kind));
            var livingShare = averageCreatures > 0f ? averageLiving / averageCreatures : 0f;
            var preference = areaShare > 0f ? livingShare / areaShare : 0f;
            var plantCalories = Average(tailSnapshots, snapshot => PlantCaloriesForBiome(snapshot, summary.Kind));
            var plantCaloriesShare = averagePlantCalories > 0f ? plantCalories / averagePlantCalories : 0f;
            var plantAvailability = areaShare > 0f ? plantCaloriesShare / areaShare : 0f;
            var eatenPerSecond = Average(tailSnapshots, snapshot => CaloriesEatenForBiome(snapshot, summary.Kind));
            var eatenShare = averageCaloriesEaten > 0f ? eatenPerSecond / averageCaloriesEaten : 0f;
            var foodPerCreaturePerSecond = averageLiving > 0f ? eatenPerSecond / averageLiving : 0f;
            var rewardIndex = livingShare > 0f ? eatenShare / livingShare : float.NaN;
            var biomeLateDeaths = Math.Max(
                0,
                DeathCountForBiome(last, summary.Kind) - DeathCountForBiome(first, summary.Kind));
            var deathShare = lateDeaths > 0 ? Share(biomeLateDeaths, lateDeaths) : 0f;
            var deathsPerCreatureHour = averageLiving > 0f && tailHours > 0d
                ? biomeLateDeaths / (averageLiving * (float)tailHours)
                : 0f;
            var riskIndex = livingShare > 0f ? deathShare / livingShare : float.NaN;

            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(FormatBiomeKind(summary.Kind))}</td>" +
                $"<td>{Html(FormatPercent(areaShare))}</td>" +
                $"<td>{Html(FormatPercent(livingShare))}</td>" +
                $"<td>{Html(FormatIndex(preference))}</td>" +
                $"<td>{Html(FormatIndex(plantAvailability))}</td>" +
                $"<td>{Html(foodPerCreaturePerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatIndex(rewardIndex))}</td>" +
                $"<td>{Html(biomeLateDeaths)}</td>" +
                $"<td>{Html(deathsPerCreatureHour.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatIndex(riskIndex))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteBiomePreferenceByGenerationSection(
        TextWriter writer,
        IReadOnlyList<CreatureState> creatures,
        BiomeMap biomes,
        IReadOnlyList<BiomeSummary> biomeSummaries,
        float worldArea)
    {
        var bands = GenerationBiomePreferenceBand.Defaults;
        var activeBiomeSummaries = ActiveBiomeSummaries(biomeSummaries);

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Biome Preference by Generation</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Generation Band</th><th>Living</th><th>Biome</th><th>Band Share</th><th>Area Share</th><th>Preference</th></tr></thead>");
        writer.WriteLine("<tbody>");

        if (creatures.Count == 0)
        {
            writer.WriteLine("<tr><td colspan=\"6\" class=\"empty\">No living creatures remain.</td></tr>");
            writer.WriteLine("</tbody></table></div>");
            writer.WriteLine("</section>");
            return;
        }

        foreach (var band in bands)
        {
            var bandCreatures = creatures
                .Where(creature => band.Contains(creature.Generation))
                .ToArray();

            if (bandCreatures.Length == 0)
            {
                writer.WriteLine(
                    "<tr>" +
                    $"<td>{Html(band.Label)}</td>" +
                    "<td>0</td>" +
                    "<td colspan=\"4\" class=\"empty\">No living creatures in this generation band.</td>" +
                    "</tr>");
                continue;
            }

            foreach (var summary in activeBiomeSummaries)
            {
                var canonicalBiome = BiomeKinds.Canonicalize(summary.Kind);
                var livingInBiome = bandCreatures.Count(creature =>
                    BiomeKinds.Canonicalize(biomes.GetKindAt(creature.Position)) == canonicalBiome);
                var bandShare = Share(livingInBiome, bandCreatures.Length);
                var areaShare = summary.Area / worldArea;
                var preference = areaShare > 0f ? bandShare / areaShare : 0f;

                writer.WriteLine(
                    "<tr>" +
                    $"<td>{Html(band.Label)}</td>" +
                    $"<td>{Html(livingInBiome)} / {Html(bandCreatures.Length)}</td>" +
                    $"<td>{Html(FormatBiomeKind(summary.Kind))}</td>" +
                    $"<td>{Html(FormatPercent(bandShare))}</td>" +
                    $"<td>{Html(FormatPercent(areaShare))}</td>" +
                    $"<td>{Html(preference.ToString("0.##", CultureInfo.InvariantCulture))}x</td>" +
                    "</tr>");
            }
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static BiomeSummary[] ActiveBiomeSummaries(IReadOnlyList<BiomeSummary> biomeSummaries)
    {
        var active = biomeSummaries
            .Where(summary => summary.Area > 0f)
            .ToArray();

        return active.Length > 0
            ? active
            : biomeSummaries.ToArray();
    }

    private static float BiomeExposureSecondsFor(SimulationSpatialHeatmaps heatmaps, BiomeKind biome)
    {
        var canonical = BiomeKinds.Canonicalize(biome);
        for (var i = 0; i < BiomeKinds.All.Count && i < heatmaps.BiomeCreatureExposureSeconds.Count; i++)
        {
            if (BiomeKinds.All[i] == canonical)
            {
                var value = heatmaps.BiomeCreatureExposureSeconds[i];
                return float.IsFinite(value) && value > 0f ? value : 0f;
            }
        }

        return 0f;
    }

    private static float Average(IReadOnlyList<SimulationStatsSnapshot> snapshots, Func<SimulationStatsSnapshot, float> selector)
    {
        if (snapshots.Count == 0)
        {
            return 0f;
        }

        var total = 0f;
        foreach (var snapshot in snapshots)
        {
            total += selector(snapshot);
        }

        return total / snapshots.Count;
    }

    private readonly record struct GenerationBiomePreferenceBand(string Label, int MinGeneration, int? MaxGeneration)
    {
        public static readonly GenerationBiomePreferenceBand[] Defaults =
        [
            new("0-5", 0, 5),
            new("6-15", 6, 15),
            new("16+", 16, null)
        ];

        public bool Contains(int generation)
        {
            return generation >= MinGeneration
                && (!MaxGeneration.HasValue || generation <= MaxGeneration.Value);
        }
    }

    private static void WriteDeathCausesByBiomeSection(TextWriter writer, BiomeDeathCauseCounts counts)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Deaths by Biome and Cause</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Biome</th><th>Total</th><th>Share</th><th>Starvation</th><th>Injury</th><th>Rotten Meat</th><th>Old Age</th><th>Unknown</th></tr></thead>");
        writer.WriteLine("<tbody>");

        foreach (var biome in BiomeKinds.All)
        {
            var row = counts.For(biome);
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(FormatBiomeKind(biome))}</td>" +
                $"<td>{Html(row.Total)}</td>" +
                $"<td>{Html(FormatPercent(Share(row.Total, counts.Total)))}</td>" +
                $"<td>{Html(row.Starvation)}</td>" +
                $"<td>{Html(row.Injury)}</td>" +
                $"<td>{Html(row.RottenMeat)}</td>" +
                $"<td>{Html(row.OldAge)}</td>" +
                $"<td>{Html(row.Unknown)}</td>" +
                "</tr>");
        }

        writer.WriteLine(
            "<tr>" +
            "<td><strong>Total</strong></td>" +
            $"<td>{Html(counts.Total)}</td>" +
            $"<td>{Html(FormatPercent(counts.Total > 0 ? 1f : 0f))}</td>" +
            $"<td>{Html(SumDeathCause(counts, CreatureDeathReason.Starvation))}</td>" +
            $"<td>{Html(SumDeathCause(counts, CreatureDeathReason.Injury))}</td>" +
            $"<td>{Html(SumDeathCause(counts, CreatureDeathReason.RottenMeat))}</td>" +
            $"<td>{Html(SumDeathCause(counts, CreatureDeathReason.OldAge))}</td>" +
            $"<td>{Html(SumUnknownDeaths(counts))}</td>" +
            "</tr>");

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static int SumDeathCause(BiomeDeathCauseCounts counts, CreatureDeathReason reason)
    {
        return BiomeKinds.All.Sum(biome => counts.For(biome).For(reason));
    }

    private static int SumUnknownDeaths(BiomeDeathCauseCounts counts)
    {
        return BiomeKinds.All.Sum(biome => counts.For(biome).Unknown);
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
    FloatSummary MetabolicPace,
    FloatSummary OffspringInvestment,
    FloatSummary EggProductionEnergyPerSecond,
    FloatSummary EggIncubationSeconds,
    FloatSummary MaturityAgeSeconds,
    FloatSummary DietaryAdaptation,
    FloatSummary CarrionAdaptation,
    FloatSummary TenderPlantAdaptation,
    FloatSummary RichPlantAdaptation,
    FloatSummary ToughPlantAdaptation,
    FloatSummary PlantDigestion,
    FloatSummary MeatDigestion,
    FloatSummary FreshMeatDigestion,
    FloatSummary StaleMeatDigestion,
    FloatSummary GutCapacityCalories,
    FloatSummary DigestionCaloriesPerSecond,
    FloatSummary BiteStrength,
    FloatSummary DamageResistance,
    FloatSummary ThermalOptimum,
    FloatSummary ThermalTolerance,
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
        var metabolicPace = new FloatAccumulator();
        var offspringInvestment = new FloatAccumulator();
        var eggProductionEnergyPerSecond = new FloatAccumulator();
        var eggIncubationSeconds = new FloatAccumulator();
        var maturityAgeSeconds = new FloatAccumulator();
        var dietaryAdaptation = new FloatAccumulator();
        var carrionAdaptation = new FloatAccumulator();
        var tenderPlantAdaptation = new FloatAccumulator();
        var richPlantAdaptation = new FloatAccumulator();
        var toughPlantAdaptation = new FloatAccumulator();
        var plantDigestion = new FloatAccumulator();
        var meatDigestion = new FloatAccumulator();
        var freshMeatDigestion = new FloatAccumulator();
        var staleMeatDigestion = new FloatAccumulator();
        var gutCapacityCalories = new FloatAccumulator();
        var digestionCaloriesPerSecond = new FloatAccumulator();
        var biteStrength = new FloatAccumulator();
        var damageResistance = new FloatAccumulator();
        var thermalOptimum = new FloatAccumulator();
        var thermalTolerance = new FloatAccumulator();
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
            metabolicPace.Add(CreatureMetabolism.NormalizePace(genome.MetabolicPace));
            offspringInvestment.Add(genome.OffspringEnergyInvestment);
            eggProductionEnergyPerSecond.Add(genome.EggProductionEnergyPerSecond);
            eggIncubationSeconds.Add(genome.EggIncubationSeconds);
            maturityAgeSeconds.Add(genome.MaturityAgeSeconds);
            dietaryAdaptation.Add(genome.DietaryAdaptation);
            carrionAdaptation.Add(genome.CarrionAdaptation);
            tenderPlantAdaptation.Add(genome.TenderPlantAdaptation);
            richPlantAdaptation.Add(genome.RichPlantAdaptation);
            toughPlantAdaptation.Add(genome.ToughPlantAdaptation);
            plantDigestion.Add(CreatureDigestion.PlantEfficiency(genome));
            meatDigestion.Add(CreatureDigestion.MeatEfficiency(genome));
            freshMeatDigestion.Add(CreatureDigestion.FreshMeatEnergyEfficiency(genome));
            staleMeatDigestion.Add(CreatureDigestion.StaleMeatEnergyEfficiency(genome));
            gutCapacityCalories.Add(genome.GutCapacityCalories);
            digestionCaloriesPerSecond.Add(genome.DigestionCaloriesPerSecond);
            biteStrength.Add(genome.BiteStrength);
            damageResistance.Add(genome.DamageResistance);
            thermalOptimum.Add(CreatureThermal.NormalizeOptimum(genome.ThermalOptimum));
            thermalTolerance.Add(CreatureThermal.NormalizeTolerance(genome.ThermalTolerance));
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
            metabolicPace.ToSummary(),
            offspringInvestment.ToSummary(),
            eggProductionEnergyPerSecond.ToSummary(),
            eggIncubationSeconds.ToSummary(),
            maturityAgeSeconds.ToSummary(),
            dietaryAdaptation.ToSummary(),
            carrionAdaptation.ToSummary(),
            tenderPlantAdaptation.ToSummary(),
            richPlantAdaptation.ToSummary(),
            toughPlantAdaptation.ToSummary(),
            plantDigestion.ToSummary(),
            meatDigestion.ToSummary(),
            freshMeatDigestion.ToSummary(),
            staleMeatDigestion.ToSummary(),
            gutCapacityCalories.ToSummary(),
            digestionCaloriesPerSecond.ToSummary(),
            biteStrength.ToSummary(),
            damageResistance.ToSummary(),
            thermalOptimum.ToSummary(),
            thermalTolerance.ToSummary(),
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
