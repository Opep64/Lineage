using System.Diagnostics;
using System.Globalization;
using System.Net;
using Lineage.Core;

try
{
    var options = RunOptions.Parse(args);
    if (options.ShowHelp)
    {
        PrintHelp();
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
          --save-scenario <path>     Save the resolved scenario JSON before running.
          --ticks <n>                Number of simulation ticks to run. Default: 5000
          --seed <n>                 Override scenario seed.
          --pipeline <neural|simple> Override controller pipeline.
          --creatures <n>            Override initial creature count.
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
          --save-snapshot <path>     Save final simulation snapshot JSON.
          --checkpoint-interval <n>  Save loadable snapshot checkpoints every n ticks.
          --checkpoint-dir <dir>      Directory for checkpoint JSON files.
          --batch-scenario <path>    Add a scenario to a batch comparison. Can repeat.
          --batch-report <path>      HTML comparison report output path.
          --batch-output-dir <dir>   Per-run batch output directory. Default: out/batch
          --no-output                Run without writing CSV.
          --help                     Show this help.

        Examples:
          dotnet run --project .\src\Lineage.Cli -- --scenario .\scenarios\balanced-foraging.json --ticks 20000
          dotnet run --project .\src\Lineage.Cli -- --ticks 20000 --seed 42 --output .\out\seed42_stats.csv --report .\out\seed42_report.html
          dotnet run --project .\src\Lineage.Cli -- --batch-report .\out\preset_comparison.html --ticks 20000 --seed 42
        """);
}

static RunResult RunSingle(RunOptions options)
{
    var scenario = options.CreateScenario();
    if (options.SaveScenarioPath is not null)
    {
        SimulationScenarioJson.Save(options.SaveScenarioPath, scenario);
    }

    var simulation = SimulationScenarioFactory.CreateSimulation(scenario);
    var stopwatch = Stopwatch.StartNew();

    var outputPaths = options.ResolveOutputPaths(scenario);
    var checkpoints = RunSimulation(options, scenario, simulation, outputPaths);
    stopwatch.Stop();

    if (options.SaveSnapshotPath is not null)
    {
        SimulationSnapshotJson.Save(options.SaveSnapshotPath, SimulationSnapshot.Capture(scenario, simulation));
    }

    WriteRunOutputs(options, scenario, simulation, stopwatch.Elapsed, outputPaths, checkpoints);

    return new RunResult(options, scenario, simulation, stopwatch.Elapsed, outputPaths, checkpoints);
}

static IReadOnlyList<CheckpointArtifact> RunSimulation(
    RunOptions options,
    SimulationScenario scenario,
    Simulation simulation,
    OutputPaths outputPaths)
{
    if (options.CheckpointIntervalTicks is null)
    {
        simulation.RunSteps(options.Ticks);
        return Array.Empty<CheckpointArtifact>();
    }

    var checkpointDirectory = outputPaths.CheckpointDirectory
        ?? throw new InvalidOperationException("Checkpoint interval was set without a checkpoint directory.");
    Directory.CreateDirectory(checkpointDirectory);

    var checkpoints = new List<CheckpointArtifact>();
    var interval = options.CheckpointIntervalTicks.Value;
    for (var i = 0; i < options.Ticks; i++)
    {
        simulation.Step();
        if (simulation.State.Tick % interval == 0)
        {
            var path = Path.Combine(checkpointDirectory, $"tick_{simulation.State.Tick:D10}.json");
            SimulationSnapshotJson.Save(path, SimulationSnapshot.Capture(scenario, simulation));
            checkpoints.Add(new CheckpointArtifact(simulation.State.Tick, path));
        }
    }

    return checkpoints;
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
    Console.WriteLine($"Max generation: {snapshot.MaxGeneration}");
    Console.WriteLine($"Snapshots: {state.Stats.Snapshots.Count}");

    if (options.SaveScenarioPath is not null)
    {
        Console.WriteLine($"Saved scenario: {Path.GetFullPath(options.SaveScenarioPath)}");
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

    if (outputPaths.ReportPath is not null)
    {
        Console.WriteLine($"Report: {Path.GetFullPath(outputPaths.ReportPath)}");
    }

    if (options.SaveSnapshotPath is not null)
    {
        Console.WriteLine($"Snapshot: {Path.GetFullPath(options.SaveSnapshotPath)}");
    }

    if (outputPaths.CheckpointDirectory is not null)
    {
        Console.WriteLine($"Checkpoint directory: {Path.GetFullPath(outputPaths.CheckpointDirectory)}");
        Console.WriteLine($"Checkpoints: {result.Checkpoints.Count}");
    }
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
            $"deaths {state.Stats.CreatureDeathCount}, starved {state.Stats.StarvationDeathCount}, max gen {snapshot.MaxGeneration}");
    }
}

internal sealed record RunOptions
{
    public int Ticks { get; init; } = 5_000;

    public string? ScenarioPath { get; init; }

    public IReadOnlyList<string> BatchScenarioPaths { get; init; } = Array.Empty<string>();

    public string? SaveScenarioPath { get; init; }

    public ulong? SeedOverride { get; init; }

    public SimulationPipelineKind? PipelineKindOverride { get; init; }

    public int? InitialCreatureCountOverride { get; init; }

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

    public string? SaveSnapshotPath { get; init; }

    public int? CheckpointIntervalTicks { get; init; }

    public string? CheckpointDirectory { get; init; }

    public string? BatchReportPath { get; init; }

    public string BatchOutputDirectory { get; init; } = Path.Combine("out", "batch");

    public bool DisableOutput { get; init; }

    public bool ShowHelp { get; init; }

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
            var disabledPaths = new OutputPaths(null, null, null, null, null, null, ReportPath, null);
            return disabledPaths with { CheckpointDirectory = ResolveCheckpointDirectory(scenario, disabledPaths) };
        }

        var statsPath = OutputPath ?? Path.Combine("out", $"lineage_run_{scenario.Seed}_stats.csv");
        var paths = new OutputPaths(
            statsPath,
            LineageOutputPath ?? AddSuffix(statsPath, "lineage"),
            TraitSummaryOutputPath ?? AddSuffix(statsPath, "traits"),
            FounderSummaryOutputPath ?? AddSuffix(statsPath, "founders"),
            GenerationSummaryOutputPath ?? AddSuffix(statsPath, "generations"),
            LineageTrendOutputPath ?? AddSuffix(statsPath, "lineage_trends"),
            ReportPath,
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
                case "--batch-scenario":
                    options = options with { BatchScenarioPaths = Append(options.BatchScenarioPaths, ReadValue(args, ref i, arg)) };
                    break;
                case "--save-scenario":
                    options = options with { SaveScenarioPath = ReadValue(args, ref i, arg) };
                    break;
                case "--ticks":
                    options = options with { Ticks = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
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
                case "--save-snapshot":
                    options = options with { SaveSnapshotPath = ReadValue(args, ref i, arg) };
                    break;
                case "--checkpoint-interval":
                    options = options with { CheckpointIntervalTicks = ParsePositiveInt(ReadValue(args, ref i, arg), arg) };
                    break;
                case "--checkpoint-dir":
                    options = options with { CheckpointDirectory = ReadValue(args, ref i, arg) };
                    break;
                case "--batch-report":
                    options = options with { BatchReportPath = ReadValue(args, ref i, arg) };
                    break;
                case "--batch-output-dir":
                    options = options with { BatchOutputDirectory = ReadValue(args, ref i, arg) };
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

    private static IReadOnlyList<string> Append(IReadOnlyList<string> values, string value)
    {
        var copy = new string[values.Count + 1];
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

    private static ulong ParseSeed(string value, string optionName)
    {
        if (!ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new ArgumentException($"{optionName} must be an unsigned integer.");
        }

        return parsed;
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
}

internal readonly record struct RunResult(
    RunOptions Options,
    SimulationScenario Scenario,
    Simulation Simulation,
    TimeSpan Elapsed,
    OutputPaths OutputPaths,
    IReadOnlyList<CheckpointArtifact> Checkpoints);

internal readonly record struct OutputPaths(
    string? StatsPath,
    string? LineagePath,
    string? TraitSummaryPath,
    string? FounderSummaryPath,
    string? GenerationSummaryPath,
    string? LineageTrendPath,
    string? ReportPath,
    string? CheckpointDirectory);

internal readonly record struct CheckpointArtifact(long Tick, string Path);

internal static class StatsCsvWriter
{
    public static void Write(string path, IReadOnlyList<SimulationStatsSnapshot> snapshots)
    {
        using var writer = CreateWriter(path);
        writer.WriteLine("tick,elapsed_seconds,creatures,eggs,resources,plant_resources,meat_resources,genomes,brains,max_generation,total_creature_energy,total_egg_energy,total_egg_health,total_resource_calories,total_plant_calories,total_meat_calories,food_detected_creatures,food_detected_share,plant_detected_creatures,plant_detected_share,meat_detected_creatures,meat_detected_share,food_contact_creatures,food_contact_share,eating_creatures,eating_share,attacking_creatures,attacking_share,avg_visible_food_density,avg_visible_plant_density,avg_visible_meat_density,avg_visible_prey_density,total_calories_eaten_per_second,total_attack_damage_per_second,avg_seconds_since_last_meal,avg_birth_investment_ratio,avg_egg_health_ratio,avg_vision_range,avg_vision_angle_degrees,births,eggs_laid,eggs_hatched,egg_deaths,egg_predation_deaths,deaths,starvation_deaths,injury_deaths");

        foreach (var snapshot in snapshots)
        {
            writer.WriteLine(string.Join(
                ',',
                snapshot.Tick.ToString(CultureInfo.InvariantCulture),
                snapshot.ElapsedSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggCount.ToString(CultureInfo.InvariantCulture),
                snapshot.ResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.MeatResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.GenomeCount.ToString(CultureInfo.InvariantCulture),
                snapshot.BrainCount.ToString(CultureInfo.InvariantCulture),
                snapshot.MaxGeneration.ToString(CultureInfo.InvariantCulture),
                snapshot.TotalCreatureEnergy.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalEggEnergy.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalEggHealth.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalResourceCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.FoodDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.FoodDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.PlantDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.PlantDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.MeatDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.MeatDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.FoodContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.FoodContactCreatureCount, snapshot.CreatureCount),
                snapshot.EatingCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.EatingCreatureCount, snapshot.CreatureCount),
                snapshot.AttackingCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.AttackingCreatureCount, snapshot.CreatureCount),
                snapshot.AverageVisibleFoodDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageVisiblePlantDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageVisibleMeatDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageVisiblePreyDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalAttackDamagePerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageSecondsSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageBirthInvestmentRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageEggHealthRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageVisionRange.ToString("0.######", CultureInfo.InvariantCulture),
                ToDegrees(snapshot.AverageVisionAngleRadians).ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CreatureBirthCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggLaidCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggHatchedCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggPredationDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.CreatureDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.StarvationDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.InjuryDeathCount.ToString(CultureInfo.InvariantCulture)));
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
        writer.WriteLine("id,parent_id,birth_tick,birth_elapsed_seconds,generation,genome_id,brain_id,birth_energy,death_tick,death_elapsed_seconds,death_reason,is_founder,is_alive");

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
        writer.WriteLine("scope,count,avg_body_radius,min_body_radius,max_body_radius,avg_max_speed,min_max_speed,max_max_speed,avg_vision_range,min_vision_range,max_vision_range,avg_vision_angle_degrees,min_vision_angle_degrees,max_vision_angle_degrees,avg_reproduction_threshold,min_reproduction_threshold,max_reproduction_threshold,avg_offspring_investment,min_offspring_investment,max_offspring_investment,avg_egg_production_per_second,min_egg_production_per_second,max_egg_production_per_second,avg_egg_incubation_seconds,min_egg_incubation_seconds,max_egg_incubation_seconds,avg_maturity_age_seconds,min_maturity_age_seconds,max_maturity_age_seconds,avg_dietary_adaptation,min_dietary_adaptation,max_dietary_adaptation,avg_plant_digestion,min_plant_digestion,max_plant_digestion,avg_meat_digestion,min_meat_digestion,max_meat_digestion,avg_mutation_strength,min_mutation_strength,max_mutation_strength,avg_trait_mutation_rate,min_trait_mutation_rate,max_trait_mutation_rate,avg_brain_mutation_rate,min_brain_mutation_rate,max_brain_mutation_rate");

        if (state.Creatures.Count == 0)
        {
            writer.WriteLine("living_creatures,0" + new string(',', 45));
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
            Format(summary.PlantDigestion.Average),
            Format(summary.PlantDigestion.Min),
            Format(summary.PlantDigestion.Max),
            Format(summary.MeatDigestion.Average),
            Format(summary.MeatDigestion.Min),
            Format(summary.MeatDigestion.Max),
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
        writer.WriteLine("generation,births,living,dead,starvation_deaths,injury_deaths,survival_rate");

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

                return new GenerationSummary(
                    group.Key,
                    births,
                    living,
                    births - living,
                    starvationDeaths,
                    injuryDeaths);
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
    int InjuryDeaths)
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
        writer.WriteLine("<thead><tr><th>Scenario</th><th>Seed</th><th>Final Pop</th><th>Eggs</th><th>Pop Change</th><th>Births</th><th>Eggs Laid</th><th>Hatched</th><th>Egg Deaths</th><th>Egg Pred</th><th>Deaths</th><th>Starved</th><th>Max Gen</th><th>Resource Final</th><th>Dominant Founder</th><th>Report</th></tr></thead>");
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
        writer.WriteLine("<thead><tr><th>Scenario</th><th>Seeing Food</th><th>Touching Food</th><th>Eating</th><th>Visible Density</th><th>Calories Eaten</th><th>Time Since Meal</th></tr></thead>");
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
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Final Trait Comparison</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Scenario</th><th>Living</th><th>Body Radius</th><th>Max Speed</th><th>Vision Range</th><th>Vision Angle</th><th>Repro Threshold</th><th>Offspring Investment</th><th>Egg Production</th><th>Egg Incubation</th><th>Maturity</th><th>Mutation Strength</th><th>Trait Mut Rate</th><th>Brain Mut Rate</th></tr></thead>");
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
            finalSnapshot.MaxGeneration,
            finalResourceRatio,
            Share(finalSnapshot.FoodDetectedCreatureCount, finalSnapshot.CreatureCount),
            Share(finalSnapshot.FoodContactCreatureCount, finalSnapshot.CreatureCount),
            Share(finalSnapshot.EatingCreatureCount, finalSnapshot.CreatureCount),
            finalSnapshot.AverageVisibleFoodDensity,
            finalSnapshot.TotalCaloriesEatenPerSecond,
            finalSnapshot.AverageSecondsSinceLastMeal,
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
    int MaxGeneration,
    float FinalResourceRatio,
    float FoodSeenShare,
    float FoodContactShare,
    float EatingShare,
    float VisibleFoodDensity,
    float CaloriesEatenPerSecond,
    float AverageSecondsSinceLastMeal,
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
            0f);
        var visibleFoodDensityTrend = Trend.From(
            snapshots,
            snapshot => snapshot.AverageVisibleFoodDensity,
            0f);
        var eatingContactTrend = Trend.From(
            snapshots,
            snapshot => Share(snapshot.FoodContactCreatureCount, snapshot.CreatureCount) * 100f,
            0f);
        var caloriesEatenTrend = Trend.From(
            snapshots,
            snapshot => snapshot.TotalCaloriesEatenPerSecond,
            0f);
        var mealGapTrend = Trend.From(
            snapshots,
            snapshot => snapshot.AverageSecondsSinceLastMeal,
            0f);
        var founderSummaries = FounderSummaryCsvWriter.Summarize(state.LineageRecords)
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenByDescending(summary => summary.TotalCreatures)
            .ThenBy(summary => summary.FounderId.Value)
            .ToArray();
        var generationSummaries = GenerationSummaryCsvWriter.Summarize(state.LineageRecords);
        var dominantLineageRows = LineageTrendCsvWriter.Summarize(snapshots, state.LineageRecords, maxRowsPerSnapshot: 1);
        var traitSummary = state.Creatures.Count > 0
            ? TraitAccumulator.FromLivingCreatures(state)
            : default;
        var biomeSummaries = state.Biomes.SummarizeResources(state.Resources);

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
        WriteMetric(writer, "Initial brain", scenario.RandomizeInitialBrainWeights ? "Per-founder random weights" : "Seed forager");
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

        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Pressure Settings</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Initial creatures", scenario.InitialCreatureCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Initial resource density", $"{scenario.InitialResourcesPerMillionArea:0.###} per 1M area");
        WriteMetric(writer, "Initial resource patches", scenario.CalculateInitialResourceCount().ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Biomes", scenario.EnableBiomes ? "Enabled" : "Disabled");
        WriteMetric(writer, "Biome cell size", scenario.BiomeCellSize.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Resource void border", $"{scenario.ResourceVoidBorderWidth:0.###} world units");
        WriteMetric(writer, "Resource calories", FormatRange(scenario.ResourceCaloriesMin, scenario.ResourceCaloriesMax));
        WriteMetric(writer, "Resource regrowth", $"{FormatRange(scenario.ResourceRegrowthMin, scenario.ResourceRegrowthMax)} kcal/s");
        WriteMetric(writer, "Depleted resources relocate", scenario.RelocateDepletedResources ? "Yes" : "No");
        WriteMetric(writer, "Basal upkeep", $"{scenario.BasalEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Body radius upkeep", $"{scenario.BodyRadiusEnergyCostPerSecond:0.###} energy/radius/s");
        WriteMetric(writer, "Max speed upkeep", $"{scenario.MaxSpeedEnergyCostPerSecond:0.######} energy/speed/s");
        WriteMetric(writer, "Turn rate upkeep", $"{scenario.TurnRateEnergyCostPerSecond:0.######} energy/rad/s/s");
        WriteMetric(writer, "Sense radius upkeep", $"{scenario.SenseRadiusEnergyCostPerSecond:0.######} energy/radius/s");
        WriteMetric(writer, "Vision angle", $"{ToDegrees(scenario.VisionAngleRadians):0.###} degrees");
        WriteMetric(writer, "Vision angle upkeep", $"{scenario.VisionAngleEnergyCostPerSecond:0.######} energy/radian/s");
        WriteMetric(writer, "Eat rate upkeep", $"{scenario.EatRateEnergyCostPerSecond:0.######} energy/rate/s");
        WriteMetric(writer, "Egg upkeep", $"{scenario.EggEnergyCostPerSecond:0.######} energy/egg/s");
        WriteMetric(writer, "Egg exposure damage", $"{scenario.EggEnvironmentalDamagePerSecond:0.######} health/s");
        WriteMetric(writer, "Movement upkeep", $"{scenario.MovementEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Eat rate", $"{scenario.EatCaloriesPerSecond:0.###} kcal/s");
        WriteMetric(writer, "Reproduction threshold", scenario.ReproductionEnergyThreshold.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Offspring investment", scenario.OffspringEnergyInvestment.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Egg production", $"{scenario.EggProductionEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Egg incubation", $"{scenario.EggIncubationSeconds:0.###} seconds");
        WriteMetric(writer, "Maturity age", $"{scenario.MaturityAgeSeconds:0.###} seconds");
        WriteMetric(writer, "Starting diet", $"{scenario.DietaryAdaptation:0.###} meat bias");
        WriteMetric(writer, "Starting plant digestion", FormatPercent(CreatureDigestion.PlantEfficiency(CreatureGenome.Baseline with { DietaryAdaptation = scenario.DietaryAdaptation })));
        WriteMetric(writer, "Starting meat digestion", FormatPercent(CreatureDigestion.MeatEfficiency(CreatureGenome.Baseline with { DietaryAdaptation = scenario.DietaryAdaptation })));
        WriteMetric(writer, "Death meat body calories", $"{scenario.DeathMeatCaloriesPerBodyRadius:0.###} kcal/radius");
        WriteMetric(writer, "Death meat energy fraction", FormatPercent(scenario.DeathMeatEnergyFraction));
        WriteMetric(writer, "Meat decay", $"{scenario.MeatDecayCaloriesPerSecond:0.###} kcal/s");
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
        WriteMetric(writer, "Births", state.Stats.CreatureBirthCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Eggs laid", state.Stats.EggLaidCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Eggs hatched", state.Stats.EggHatchedCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Egg deaths", state.Stats.EggDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Egg predation deaths", state.Stats.EggPredationDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Egg survival", FormatPercent(Share(state.Stats.EggHatchedCount, state.Stats.EggLaidCount)));
        WriteMetric(writer, "Offspring alive", FormatPercent(Share(state.Creatures.Count(creature => creature.Generation > 0), state.Stats.EggHatchedCount)));
        WriteMetric(writer, "Egg health", $"{finalSnapshot.AverageEggHealthRatio * 100f:0.0}%");
        WriteMetric(writer, "Birth investment", $"{finalSnapshot.AverageBirthInvestmentRatio:0.###}x");
        WriteMetric(writer, "Deaths", state.Stats.CreatureDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Starvation deaths", state.Stats.StarvationDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Injury deaths", state.Stats.InjuryDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Max generation", finalSnapshot.MaxGeneration.ToString(CultureInfo.InvariantCulture));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Foraging Diagnostics</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Seeing food", FormatPercent(Share(finalSnapshot.FoodDetectedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Seeing plants", FormatPercent(Share(finalSnapshot.PlantDetectedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Seeing meat", FormatPercent(Share(finalSnapshot.MeatDetectedCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Touching food", FormatPercent(Share(finalSnapshot.FoodContactCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Eating this tick", FormatPercent(Share(finalSnapshot.EatingCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Attacking this tick", FormatPercent(Share(finalSnapshot.AttackingCreatureCount, finalSnapshot.CreatureCount)));
        WriteMetric(writer, "Visible food density", finalSnapshot.AverageVisibleFoodDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible plant density", finalSnapshot.AverageVisiblePlantDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible meat density", finalSnapshot.AverageVisibleMeatDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible prey density", finalSnapshot.AverageVisiblePreyDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Calories eaten", $"{finalSnapshot.TotalCaloriesEatenPerSecond:0.###} kcal/s");
        WriteMetric(writer, "Attack damage", $"{finalSnapshot.TotalAttackDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Time since meal", $"{finalSnapshot.AverageSecondsSinceLastMeal:0.###} s avg");
        WriteMetric(writer, "Average vision range", finalSnapshot.AverageVisionRange.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average vision angle", $"{ToDegrees(finalSnapshot.AverageVisionAngleRadians):0.###} degrees");
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Biomes</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Biome</th><th>Area</th><th>Area Share</th><th>Density Mult</th><th>Regrowth Mult</th><th>Resources</th><th>Resources/M</th><th>Calories</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in biomeSummaries)
        {
            var resourcesPerMillion = summary.Area > 0f
                ? summary.ResourceCount / summary.Area * SimulationScenario.ResourceDensityAreaUnits
                : 0f;
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(FormatBiomeKind(summary.Kind))}</td>" +
                $"<td>{Html(summary.Area.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(summary.Area / MathF.Max(1f, state.Bounds.Width * state.Bounds.Height)))}</td>" +
                $"<td>{Html(summary.ResourceDensityMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ResourceRegrowthMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ResourceCount)}</td>" +
                $"<td>{Html(resourcesPerMillion.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ResourceCalories.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

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
        WriteTrendRow(writer, "Calories eaten", caloriesEatenTrend, "kcal/s");
        WriteTrendRow(writer, "Time since meal", mealGapTrend, "s avg");
        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

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
        writer.WriteLine("<h2>Generation Survival</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Generation</th><th>Births</th><th>Living</th><th>Dead</th><th>Starvation Deaths</th><th>Injury Deaths</th><th>Survival Rate</th></tr></thead>");
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
                $"<td>{Html(FormatPercent(summary.SurvivalRate))}</td>" +
                "</tr>");
        }

        if (generationSummaries.Count == 0)
        {
            WriteEmptyRow(writer, 7, "No generation records were present.");
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
            WriteTraitRow(writer, "Plant digestion efficiency", traitSummary.PlantDigestion);
            WriteTraitRow(writer, "Meat digestion efficiency", traitSummary.MeatDigestion);
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

    private static string FormatPercent(float value)
    {
        return $"{value * 100f:0.0}%";
    }

    private static float Share(int count, int total)
    {
        return total > 0 ? count / (float)total : 0f;
    }

    private static string FormatBiomeKind(BiomeKind biome)
    {
        return biome.ToString();
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
    FloatSummary PlantDigestion,
    FloatSummary MeatDigestion,
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
        var plantDigestion = new FloatAccumulator();
        var meatDigestion = new FloatAccumulator();
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
            plantDigestion.Add(CreatureDigestion.PlantEfficiency(genome));
            meatDigestion.Add(CreatureDigestion.MeatEfficiency(genome));
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
            plantDigestion.ToSummary(),
            meatDigestion.ToSummary(),
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
