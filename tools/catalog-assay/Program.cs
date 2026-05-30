using System.Diagnostics;
using System.Globalization;
using System.Text;
using Lineage.Core;

var options = CatalogAssayOptions.Parse(args);
if (options.ShowHelp)
{
    PrintHelp();
    return;
}

try
{
    var workspaceRoot = FindWorkspaceRoot(Directory.GetCurrentDirectory());
    var scenarioPath = ResolvePath(workspaceRoot, options.ScenarioPath);
    var baseScenario = SimulationScenarioJson.Load(scenarioPath);
    var speciesEntries = ResolveSpeciesEntries(options, workspaceRoot);
    var brainEntries = ResolveBrainEntries(options, workspaceRoot);
    var runPlan = CreateRunPlan(options, speciesEntries, brainEntries, scenarioPath, workspaceRoot);

    if (runPlan.Count == 0)
    {
        throw new InvalidOperationException("Catalog assay did not find any runs to execute.");
    }

    Console.WriteLine(
        $"Running {runPlan.Count} catalog assay runs: {speciesEntries.Count} species, {options.Seeds.Count} seeds, {options.Ticks} ticks.");

    var results = new List<AssayRunResult>(runPlan.Count);
    for (var i = 0; i < runPlan.Count; i++)
    {
        var plan = runPlan[i];
        var result = RunAssay(plan, options, baseScenario, scenarioPath, workspaceRoot);
        results.Add(result);
        Console.WriteLine(FormatProgress(i + 1, runPlan.Count, result));
    }

    WriteText(options.OutputPath, AssayReport.ToCsv(results));
    WriteText(options.MarkdownPath, AssayReport.ToMarkdown(results, options, scenarioPath, workspaceRoot));
    Console.WriteLine($"Wrote {Path.GetFullPath(options.OutputPath)}");
    Console.WriteLine($"Wrote {Path.GetFullPath(options.MarkdownPath)}");
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
        Lineage.CatalogAssay - repeatable catalog species and brain viability assays

        Usage:
          dotnet run --project tools/catalog-assay -- --ticks 50000
          dotnet run --project tools/catalog-assay -- --species species/rookie-explorer-forager.species.json --seeds 20260519,20260520
          dotnet run --project tools/catalog-assay -- --all-brains --ticks 25000

        Options:
          --scenario <path>          Base scenario. Default: scenarios/balanced-foraging.json.
          --species <path>           Species profile to test. May be repeated. Default: every *.species.json under --species-dir.
          --species-dir <path>       Species catalog directory. Default: species.
          --brain <path>             Brain profile override to test against selected species. May be repeated.
          --all-brains               Test each selected species against every *.brain.json under --brain-dir.
          --brain-dir <path>         Brain catalog directory for --all-brains. Default: brains.
          --ticks <n>                Ticks per run. Default: 50000.
          --seeds <a,b,c>            Comma-separated seeds. Default: 20260519,20260520,20260521.
          --founders <n>             Starter creatures per species assay. Default: 40.
          --spawn-region <name>      Starter spawn region. Default: uniform.
          --energy <kcal>            Optional starter energy override.
          --snapshot-interval <n>    Stats snapshot interval. Default: ticks per run.
          --keep-running-after-extinction
                                    Continue to requested ticks after creatures and eggs are gone.
          --output <path>            CSV output. Default: out/catalog_assay/catalog_assay.csv.
          --markdown <path>          Markdown output. Default: out/catalog_assay/catalog_assay.md.
          --help                     Show this help.
        """);
}

static AssayRunResult RunAssay(
    AssayRunPlan plan,
    CatalogAssayOptions options,
    SimulationScenario baseScenario,
    string scenarioPath,
    string workspaceRoot)
{
    var scenarioDirectory = Path.GetDirectoryName(scenarioPath);
    var scenario = baseScenario with
    {
        Name = $"{baseScenario.Name} Catalog Assay",
        Seed = plan.Seed,
        InitialCreatureCount = 0,
        StatsSnapshotIntervalTicks = options.SnapshotIntervalTicks ?? options.Ticks,
        SpeciesSeeds =
        [
            new SpeciesScenarioSeed
            {
                ProfilePath = plan.Species.RelativePath,
                Count = options.Founders,
                SpawnRegion = options.SpawnRegion,
                EnergyOverride = options.EnergyOverride,
                BrainProfilePath = plan.Brain.OverridePath,
                Enabled = true
            }
        ]
    };

    var stopwatch = Stopwatch.StartNew();
    var completedTicks = 0;
    string status;
    try
    {
        var simulation = SimulationScenarioFactory.CreateSimulation(scenario, scenarioDirectory);
        var injections = SimulationScenarioSpeciesSeeder.InjectScenarioSpecies(
            scenario,
            simulation.State,
            scenarioPath,
            workspaceRoot);

        status = "completed";
        for (var step = 0; step < options.Ticks; step++)
        {
            simulation.Step();
            completedTicks++;
            if (options.StopOnExtinction
                && simulation.State.Creatures.Count == 0
                && simulation.State.Eggs.Count == 0)
            {
                status = "extinct";
                break;
            }
        }

        stopwatch.Stop();
        var state = simulation.State;
        var rosterSummary = RosterLineageAnalyzer
            .Analyze(state.LineageRecords, injections, state.Tick)
            .FirstOrDefault();
        var wallSeconds = Math.Max(0.000001d, stopwatch.Elapsed.TotalSeconds);
        var ticksPerSecond = completedTicks / wallSeconds;
        return new AssayRunResult(
            plan.Species.Profile.Name,
            plan.Species.RelativePath,
            plan.Brain.Name,
            plan.Brain.DisplayPath,
            plan.Brain.Mode,
            plan.Seed,
            status,
            options.Ticks,
            completedTicks,
            stopwatch.Elapsed.TotalSeconds,
            ticksPerSecond,
            options.Founders,
            rosterSummary.TotalCreatures,
            rosterSummary.DescendantCount,
            rosterSummary.LivingCreatures,
            state.Eggs.Count,
            state.Resources.Count,
            state.Stats.CreatureBirthCount,
            state.Stats.CreatureDeathCount,
            state.Stats.StarvationDeathCount,
            state.Stats.InjuryDeathCount,
            state.Stats.RottenMeatDeathCount,
            rosterSummary.MaxGeneration,
            rosterSummary.TailAverageLivingCreatures,
            rosterSummary.ExtinctionTick,
            Error: null);
    }
    catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException or IOException)
    {
        stopwatch.Stop();
        var wallSeconds = Math.Max(0.000001d, stopwatch.Elapsed.TotalSeconds);
        return AssayRunResult.FromError(
            plan.Species.Profile.Name,
            plan.Species.RelativePath,
            plan.Brain.Name,
            plan.Brain.DisplayPath,
            plan.Brain.Mode,
            plan.Seed,
            options.Ticks,
            completedTicks,
            stopwatch.Elapsed.TotalSeconds,
            completedTicks / wallSeconds,
            options.Founders,
            ex.Message);
    }
}

static IReadOnlyList<AssayRunPlan> CreateRunPlan(
    CatalogAssayOptions options,
    IReadOnlyList<SpeciesEntry> speciesEntries,
    IReadOnlyList<BrainEntry> brainEntries,
    string scenarioPath,
    string workspaceRoot)
{
    var plans = new List<AssayRunPlan>();
    foreach (var species in speciesEntries)
    {
        var brains = options.UseBrainOverrides
            ? brainEntries.Select(entry => new BrainChoice(
                entry.Profile.Name,
                entry.RelativePath,
                entry.RelativePath,
                entry.Compatibility.RequiresNormalization ? "catalog override, normalized" : "catalog override"))
            : [CreateProfileBrainChoice(species, scenarioPath, workspaceRoot)];

        foreach (var brain in brains)
        {
            foreach (var seed in options.Seeds)
            {
                plans.Add(new AssayRunPlan(species, brain, seed));
            }
        }
    }

    return plans;
}

static BrainChoice CreateProfileBrainChoice(SpeciesEntry species, string scenarioPath, string workspaceRoot)
{
    if (string.IsNullOrWhiteSpace(species.Profile.DefaultBrainPath))
    {
        return new BrainChoice(
            $"{species.Profile.BrainArchitectureKind} embedded profile brain",
            DisplayPath: "",
            OverridePath: null,
            Mode: "profile embedded");
    }

    var resolved = SimulationScenarioSpeciesSeeder.ResolveBrainProfilePath(
        species.Profile.DefaultBrainPath,
        species.Path,
        scenarioPath,
        workspaceRoot);
    if (!File.Exists(resolved))
    {
        return new BrainChoice(
            species.Profile.DefaultBrainPath,
            species.Profile.DefaultBrainPath,
            OverridePath: null,
            Mode: "missing profile default");
    }

    var raw = BrainProfileJson.LoadRaw(resolved);
    var compatibility = BrainProfileCompatibility.Assess(raw);
    var name = compatibility.IsCompatible
        ? raw.Validated().Name
        : species.Profile.DefaultBrainPath;
    var mode = compatibility.RequiresNormalization
        ? "profile default, normalized"
        : "profile default";
    return new BrainChoice(
        name,
        NormalizeRelativePath(workspaceRoot, resolved),
        OverridePath: null,
        mode);
}

static IReadOnlyList<SpeciesEntry> ResolveSpeciesEntries(CatalogAssayOptions options, string workspaceRoot)
{
    var paths = options.SpeciesPaths.Count > 0
        ? options.SpeciesPaths.Select(path => ResolvePath(workspaceRoot, path))
        : Directory.EnumerateFiles(
            ResolvePath(workspaceRoot, options.SpeciesDirectory),
            SpeciesProfileJson.FilePattern,
            SearchOption.AllDirectories);

    var entries = new List<SpeciesEntry>();
    foreach (var path in paths.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
    {
        var profile = SpeciesProfileJson.Load(path);
        entries.Add(new SpeciesEntry(path, NormalizeRelativePath(workspaceRoot, path), profile));
    }

    if (entries.Count == 0)
    {
        throw new InvalidOperationException("No species profiles were found.");
    }

    return entries;
}

static IReadOnlyList<BrainEntry> ResolveBrainEntries(CatalogAssayOptions options, string workspaceRoot)
{
    if (!options.UseBrainOverrides)
    {
        return Array.Empty<BrainEntry>();
    }

    var paths = options.BrainPaths.Count > 0
        ? options.BrainPaths.Select(path => ResolvePath(workspaceRoot, path))
        : Directory.EnumerateFiles(
            ResolvePath(workspaceRoot, options.BrainDirectory),
            BrainProfileJson.FilePattern,
            SearchOption.AllDirectories);

    var entries = new List<BrainEntry>();
    foreach (var path in paths.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))
    {
        var raw = BrainProfileJson.LoadRaw(path);
        var compatibility = BrainProfileCompatibility.Assess(raw);
        if (!compatibility.IsCompatible)
        {
            Console.WriteLine($"Skipping incompatible brain {NormalizeRelativePath(workspaceRoot, path)}: {compatibility.Status}");
            continue;
        }

        entries.Add(new BrainEntry(
            path,
            NormalizeRelativePath(workspaceRoot, path),
            raw.Validated(),
            compatibility));
    }

    if (entries.Count == 0)
    {
        throw new InvalidOperationException("Brain override mode was requested, but no compatible brain profiles were found.");
    }

    return entries;
}

static string FormatProgress(int completed, int total, AssayRunResult result)
{
    var final = result.Status == "error"
        ? result.Error
        : $"final {result.FinalLiving}, eggs {result.FinalEggs}, gen {result.MaxGeneration}, {result.TicksPerSecond:0} t/s";
    return string.Create(
        CultureInfo.InvariantCulture,
        $"[{completed}/{total}] {result.SpeciesName} / {result.BrainName} seed {result.Seed}: {result.Status} after {result.TicksCompleted} ticks ({final})");
}

static void WriteText(string outputPath, string text)
{
    var directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    File.WriteAllText(outputPath, text);
}

static string ResolvePath(string workspaceRoot, string path)
{
    return Path.IsPathRooted(path)
        ? Path.GetFullPath(path)
        : Path.GetFullPath(Path.Combine(workspaceRoot, path));
}

static string NormalizeRelativePath(string workspaceRoot, string path)
{
    var fullPath = Path.GetFullPath(path);
    var relative = Path.GetRelativePath(workspaceRoot, fullPath);
    return relative.StartsWith("..", StringComparison.Ordinal)
        ? fullPath
        : relative.Replace(Path.DirectorySeparatorChar, '/');
}

static string FindWorkspaceRoot(string startDirectory)
{
    var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "Lineage.slnx")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return Path.GetFullPath(startDirectory);
}

internal sealed record CatalogAssayOptions
{
    public string ScenarioPath { get; init; } = Path.Combine("scenarios", "balanced-foraging.json");

    public List<string> SpeciesPaths { get; init; } = [];

    public string SpeciesDirectory { get; init; } = "species";

    public List<string> BrainPaths { get; init; } = [];

    public string BrainDirectory { get; init; } = "brains";

    public bool AllBrains { get; init; }

    public int Ticks { get; init; } = 50_000;

    public int? SnapshotIntervalTicks { get; init; }

    public List<ulong> Seeds { get; init; } = [20260519UL, 20260520UL, 20260521UL];

    public int Founders { get; init; } = 40;

    public InitialCreatureSpawnRegion SpawnRegion { get; init; } = InitialCreatureSpawnRegion.Uniform;

    public float? EnergyOverride { get; init; }

    public bool StopOnExtinction { get; init; } = true;

    public string OutputPath { get; init; } = Path.Combine("out", "catalog_assay", "catalog_assay.csv");

    public string MarkdownPath { get; init; } = Path.Combine("out", "catalog_assay", "catalog_assay.md");

    public bool ShowHelp { get; init; }

    public bool UseBrainOverrides => AllBrains || BrainPaths.Count > 0;

    public static CatalogAssayOptions Parse(string[] args)
    {
        var options = new CatalogAssayOptions { ShowHelp = args.Length == 0 };
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--scenario":
                    options = options with { ScenarioPath = RequireValue(args, ref i, arg) };
                    break;
                case "--species":
                    options.SpeciesPaths.Add(RequireValue(args, ref i, arg));
                    break;
                case "--species-dir":
                    options = options with { SpeciesDirectory = RequireValue(args, ref i, arg) };
                    break;
                case "--brain":
                    options.BrainPaths.Add(RequireValue(args, ref i, arg));
                    break;
                case "--all-brains":
                    options = options with { AllBrains = true };
                    break;
                case "--brain-dir":
                    options = options with { BrainDirectory = RequireValue(args, ref i, arg) };
                    break;
                case "--ticks":
                    options = options with { Ticks = ParsePositiveInt(RequireValue(args, ref i, arg), arg) };
                    break;
                case "--snapshot-interval":
                    options = options with { SnapshotIntervalTicks = ParsePositiveInt(RequireValue(args, ref i, arg), arg) };
                    break;
                case "--seeds":
                    options = options with { Seeds = ParseSeeds(RequireValue(args, ref i, arg)) };
                    break;
                case "--founders":
                    options = options with { Founders = ParsePositiveInt(RequireValue(args, ref i, arg), arg) };
                    break;
                case "--spawn-region":
                    options = options with { SpawnRegion = ParseSpawnRegion(RequireValue(args, ref i, arg)) };
                    break;
                case "--energy":
                    options = options with { EnergyOverride = ParsePositiveFloat(RequireValue(args, ref i, arg), arg) };
                    break;
                case "--keep-running-after-extinction":
                    options = options with { StopOnExtinction = false };
                    break;
                case "--output":
                    options = options with { OutputPath = RequireValue(args, ref i, arg) };
                    break;
                case "--markdown":
                    options = options with { MarkdownPath = RequireValue(args, ref i, arg) };
                    break;
                case "--help":
                case "-h":
                    options = options with { ShowHelp = true };
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{arg}'. Use --help for usage.");
            }
        }

        if (options.Ticks <= 0)
        {
            throw new ArgumentException("--ticks must be positive.");
        }

        if (options.Founders <= 0)
        {
            throw new ArgumentException("--founders must be positive.");
        }

        if (options.Seeds.Count == 0)
        {
            throw new ArgumentException("--seeds must include at least one seed.");
        }

        return options;
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{option} requires a value.");
        }

        index++;
        return args[index];
    }

    private static int ParsePositiveInt(string value, string option)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            throw new ArgumentException($"{option} must be a positive integer.");
        }

        return parsed;
    }

    private static float ParsePositiveFloat(string value, string option)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || !float.IsFinite(parsed)
            || parsed <= 0f)
        {
            throw new ArgumentException($"{option} must be a finite positive number.");
        }

        return parsed;
    }

    private static List<ulong> ParseSeeds(string value)
    {
        var seeds = new List<ulong>();
        foreach (var token in value.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!ulong.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed))
            {
                throw new ArgumentException($"Invalid seed '{token}'.");
            }

            seeds.Add(seed);
        }

        return seeds;
    }

    private static InitialCreatureSpawnRegion ParseSpawnRegion(string value)
    {
        foreach (var name in Enum.GetNames<InitialCreatureSpawnRegion>())
        {
            if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
            {
                return Enum.Parse<InitialCreatureSpawnRegion>(name);
            }
        }

        throw new ArgumentException($"Unknown spawn region '{value}'.");
    }
}

internal sealed record SpeciesEntry(string Path, string RelativePath, SpeciesProfile Profile);

internal sealed record BrainEntry(
    string Path,
    string RelativePath,
    BrainProfile Profile,
    BrainProfileCompatibility Compatibility);

internal sealed record BrainChoice(
    string Name,
    string DisplayPath,
    string? OverridePath,
    string Mode);

internal sealed record AssayRunPlan(SpeciesEntry Species, BrainChoice Brain, ulong Seed);

internal sealed record AssayRunResult(
    string SpeciesName,
    string SpeciesPath,
    string BrainName,
    string BrainPath,
    string BrainMode,
    ulong Seed,
    string Status,
    int TicksRequested,
    int TicksCompleted,
    double WallSeconds,
    double TicksPerSecond,
    int FounderCount,
    int TotalCreatures,
    int DescendantCount,
    int FinalLiving,
    int FinalEggs,
    int FinalResources,
    int CreatureBirths,
    int CreatureDeaths,
    int StarvationDeaths,
    int InjuryDeaths,
    int RottenMeatDeaths,
    int MaxGeneration,
    float TailAverageLiving,
    long? ExtinctionTick,
    string? Error)
{
    public static AssayRunResult FromError(
        string speciesName,
        string speciesPath,
        string brainName,
        string brainPath,
        string brainMode,
        ulong seed,
        int ticksRequested,
        int ticksCompleted,
        double wallSeconds,
        double ticksPerSecond,
        int founderCount,
        string error)
    {
        return new AssayRunResult(
            speciesName,
            speciesPath,
            brainName,
            brainPath,
            brainMode,
            seed,
            "error",
            ticksRequested,
            ticksCompleted,
            wallSeconds,
            ticksPerSecond,
            founderCount,
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
            0f,
            null,
            error);
    }
}

internal static class AssayReport
{
    public static string ToCsv(IReadOnlyList<AssayRunResult> results)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        writer.WriteLine(
            "species_name,species_path,brain_name,brain_path,brain_mode,seed,status,ticks_requested,ticks_completed,wall_seconds,ticks_per_second,founders,total_creatures,descendants,final_living,final_eggs,final_resources,births,deaths,starvation_deaths,injury_deaths,rotten_meat_deaths,max_generation,tail_avg_living,extinction_tick,error");
        foreach (var result in results)
        {
            writer.WriteLine(string.Join(
                ',',
                Csv(result.SpeciesName),
                Csv(result.SpeciesPath),
                Csv(result.BrainName),
                Csv(result.BrainPath),
                Csv(result.BrainMode),
                result.Seed.ToString(CultureInfo.InvariantCulture),
                Csv(result.Status),
                result.TicksRequested.ToString(CultureInfo.InvariantCulture),
                result.TicksCompleted.ToString(CultureInfo.InvariantCulture),
                result.WallSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                result.TicksPerSecond.ToString("0.###", CultureInfo.InvariantCulture),
                result.FounderCount.ToString(CultureInfo.InvariantCulture),
                result.TotalCreatures.ToString(CultureInfo.InvariantCulture),
                result.DescendantCount.ToString(CultureInfo.InvariantCulture),
                result.FinalLiving.ToString(CultureInfo.InvariantCulture),
                result.FinalEggs.ToString(CultureInfo.InvariantCulture),
                result.FinalResources.ToString(CultureInfo.InvariantCulture),
                result.CreatureBirths.ToString(CultureInfo.InvariantCulture),
                result.CreatureDeaths.ToString(CultureInfo.InvariantCulture),
                result.StarvationDeaths.ToString(CultureInfo.InvariantCulture),
                result.InjuryDeaths.ToString(CultureInfo.InvariantCulture),
                result.RottenMeatDeaths.ToString(CultureInfo.InvariantCulture),
                result.MaxGeneration.ToString(CultureInfo.InvariantCulture),
                result.TailAverageLiving.ToString("0.###", CultureInfo.InvariantCulture),
                result.ExtinctionTick?.ToString(CultureInfo.InvariantCulture) ?? "",
                Csv(result.Error ?? "")));
        }

        return writer.ToString();
    }

    public static string ToMarkdown(
        IReadOnlyList<AssayRunResult> results,
        CatalogAssayOptions options,
        string scenarioPath,
        string workspaceRoot)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        writer.WriteLine("# Catalog Assay");
        writer.WriteLine();
        writer.WriteLine($"Scenario: `{NormalizeForMarkdown(Path.GetRelativePath(workspaceRoot, scenarioPath))}`");
        writer.WriteLine($"Ticks per run: `{options.Ticks}`");
        writer.WriteLine($"Founders per run: `{options.Founders}`");
        writer.WriteLine($"Seeds: `{string.Join(", ", options.Seeds)}`");
        writer.WriteLine($"Brain mode: `{(options.UseBrainOverrides ? "catalog overrides" : "profile defaults")}`");
        writer.WriteLine($"Stop on extinction: `{(options.StopOnExtinction ? "yes" : "no")}`");
        writer.WriteLine();
        writer.WriteLine("## Aggregate");
        writer.WriteLine();
        writer.WriteLine("| Species | Brain | Runs | Survived | Extinct | Errors | Avg final | Avg tail | Avg max gen | Avg ticks/s |");
        writer.WriteLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (var group in results
                     .GroupBy(result => (result.SpeciesName, result.BrainName, result.BrainMode))
                     .Select(group => new
                     {
                         group.Key.SpeciesName,
                         Brain = group.Key.BrainName,
                         Mode = group.Key.BrainMode,
                         Runs = group.ToArray()
                     })
                     .OrderByDescending(group => group.Runs.Count(result => result.FinalLiving > 0))
                     .ThenByDescending(group => Average(group.Runs, result => result.TailAverageLiving))
                     .ThenBy(group => group.SpeciesName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(group => group.Brain, StringComparer.OrdinalIgnoreCase))
        {
            var runs = group.Runs;
            var errorCount = runs.Count(result => result.Status == "error");
            var survived = runs.Count(result => result.FinalLiving > 0);
            var extinct = runs.Count(result => result.Status == "extinct");
            writer.WriteLine(
                $"| {Md(group.SpeciesName)} | {Md(group.Brain)} ({Md(group.Mode)}) | {runs.Length} | {survived} | {extinct} | {errorCount} | {Average(runs, result => result.FinalLiving):0.##} | {Average(runs, result => result.TailAverageLiving):0.##} | {Average(runs, result => result.MaxGeneration):0.##} | {Average(runs.Where(result => result.Status != "error"), result => result.TicksPerSecond):0} |");
        }

        writer.WriteLine();
        writer.WriteLine("## Runs");
        writer.WriteLine();
        writer.WriteLine("| Species | Brain | Seed | Status | Ticks | Ticks/s | Final | Eggs | Births | Deaths | Max gen | Tail avg | Extinction tick |");
        writer.WriteLine("|---|---|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var result in results)
        {
            writer.WriteLine(
                $"| {Md(result.SpeciesName)} | {Md(result.BrainName)} | {result.Seed} | {Md(result.Status)} | {result.TicksCompleted} | {result.TicksPerSecond:0} | {result.FinalLiving} | {result.FinalEggs} | {result.CreatureBirths} | {result.CreatureDeaths} | {result.MaxGeneration} | {result.TailAverageLiving:0.##} | {result.ExtinctionTick?.ToString(CultureInfo.InvariantCulture) ?? ""} |");
        }

        var errors = results.Where(result => result.Status == "error").ToArray();
        if (errors.Length > 0)
        {
            writer.WriteLine();
            writer.WriteLine("## Errors");
            writer.WriteLine();
            writer.WriteLine("| Species | Brain | Seed | Error |");
            writer.WriteLine("|---|---|---:|---|");
            foreach (var result in errors)
            {
                writer.WriteLine($"| {Md(result.SpeciesName)} | {Md(result.BrainName)} | {result.Seed} | {Md(result.Error ?? "")} |");
            }
        }

        return writer.ToString();
    }

    private static double Average(IEnumerable<AssayRunResult> results, Func<AssayRunResult, double> selector)
    {
        var values = results.Select(selector).ToArray();
        return values.Length == 0 ? 0d : values.Average();
    }

    private static string Csv(string value)
    {
        if (!value.Contains('"') && !value.Contains(',') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string Md(string value)
    {
        return NormalizeForMarkdown(value).Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static string NormalizeForMarkdown(string value)
    {
        return value.Replace('\\', '/');
    }
}
