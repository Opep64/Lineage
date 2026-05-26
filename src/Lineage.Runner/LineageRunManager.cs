using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Lineage.Core;

namespace Lineage.Runner;

public sealed partial class LineageRunManager
{
    private const string ProcessIdToken = "{pid}";
    private const string UserScenarioFolderName = "user";
    private const string ScenarioRecipeFolderName = "recipes";
    private const string UserScenarioRegistryFileName = ".lineage-runner-scenarios.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static readonly IReadOnlyList<ScenarioFieldDefinition> ScenarioFieldDefinitions = BuildScenarioFieldDefinitions();

    private readonly ConcurrentDictionary<string, ManagedRun> _runs = [];
    private readonly string _repoRoot;
    private readonly string _runsRoot;

    public LineageRunManager()
    {
        _repoRoot = FindRepositoryRoot();
        _runsRoot = Path.Combine(_repoRoot, "out", "runs");
        Directory.CreateDirectory(_runsRoot);
        LoadExistingRunManifests();
    }

    public string RepositoryRoot => _repoRoot;

    public IReadOnlyList<ScenarioOption> ListScenarios()
    {
        var scenarioRoot = Path.Combine(_repoRoot, "scenarios");
        if (!Directory.Exists(scenarioRoot))
        {
            return Array.Empty<ScenarioOption>();
        }

        var registry = LoadUserScenarioRegistry();
        var rootScenarios = Directory
            .EnumerateFiles(scenarioRoot, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new ScenarioOption(
                Path.GetFileNameWithoutExtension(path),
                Path.GetRelativePath(_repoRoot, path),
                IsUserCreated: false,
                CanDelete: false));

        var userScenarioRoot = UserScenarioRoot();
        var userScenarios = Directory.Exists(userScenarioRoot)
            ? Directory
                .EnumerateFiles(userScenarioRoot, "*.json", SearchOption.TopDirectoryOnly)
                .Where(path => !string.Equals(Path.GetFileName(path), UserScenarioRegistryFileName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path =>
                {
                    var relativePath = NormalizeRelativePath(Path.GetRelativePath(_repoRoot, path));
                    var isRegistered = registry.Scenarios.TryGetValue(relativePath, out var entry);
                    return new ScenarioOption(
                        string.IsNullOrWhiteSpace(entry?.Name) ? Path.GetFileNameWithoutExtension(path) : entry.Name,
                        relativePath,
                        IsUserCreated: isRegistered,
                        CanDelete: isRegistered);
                })
            : Array.Empty<ScenarioOption>();

        return rootScenarios
            .Concat(userScenarios)
            .ToArray();
    }

    public IReadOnlyList<ScenarioRecipe> ListScenarioRecipes()
    {
        var recipeRoot = ScenarioRecipeRoot();
        if (!Directory.Exists(recipeRoot))
        {
            return Array.Empty<ScenarioRecipe>();
        }

        return Directory
            .EnumerateFiles(recipeRoot, "*.json", SearchOption.TopDirectoryOnly)
            .Select(TryReadScenarioRecipe)
            .OfType<ScenarioRecipe>()
            .OrderBy(recipe => recipe.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public ScenarioRecipeSaveResult SaveScenarioRecipe(ScenarioRecipeSaveRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Recipe name is required.");
        }

        if (request.Changes.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Recipe changes must be a JSON object.");
        }

        var changes = JsonNode.Parse(request.Changes.GetRawText())?.AsObject()
            ?? throw new ArgumentException("Recipe changes must be a JSON object.");
        ValidateRecipeChanges(changes);

        var slug = SlugRegex().Replace(name.ToLowerInvariant(), "_").Trim('_');
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Recipe name must contain letters or numbers.");
        }

        var recipeRoot = ScenarioRecipeRoot();
        Directory.CreateDirectory(recipeRoot);
        var path = Path.GetFullPath(Path.Combine(recipeRoot, $"{slug}.json"));
        EnsurePathInside(path, recipeRoot);
        if (File.Exists(path))
        {
            throw new InvalidOperationException($"A recipe named {slug}.json already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var file = new ScenarioRecipeFile
        {
            Name = name,
            Description = (request.Description ?? string.Empty).Trim(),
            Tags = NormalizeTags(request.Tags),
            Changes = changes,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        SaveScenarioRecipeFile(path, file);
        return new ScenarioRecipeSaveResult(ToScenarioRecipe(path, file));
    }

    public ScenarioRecipeArchiveResult ArchiveScenarioRecipe(string recipePath)
    {
        var path = ResolveRecipePath(recipePath);

        var archiveRoot = Path.Combine(_repoRoot, "out", "recipe-trash");
        Directory.CreateDirectory(archiveRoot);
        var archivePath = GetUniqueArchivePath(Path.Combine(
            archiveRoot,
            $"{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Path.GetFileName(path)}"));
        File.Move(path, archivePath);

        return new ScenarioRecipeArchiveResult(
            NormalizeRelativePath(Path.GetRelativePath(_repoRoot, path)),
            NormalizeRelativePath(Path.GetRelativePath(_repoRoot, archivePath)));
    }

    public ScenarioRecipeDeleteResult DeleteScenarioRecipe(string recipePath)
    {
        var path = ResolveRecipePath(recipePath);
        var relativePath = NormalizeRelativePath(Path.GetRelativePath(_repoRoot, path));
        File.Delete(path);
        return new ScenarioRecipeDeleteResult(relativePath);
    }

    public ScenarioEditorDefinition GetScenarioEditor(string scenarioPath)
    {
        if (string.IsNullOrWhiteSpace(scenarioPath))
        {
            throw new ArgumentException("Scenario path is required.");
        }

        var resolvedPath = ResolveScenarioPath(scenarioPath);
        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException("Scenario file was not found.", scenarioPath);
        }

        return BuildScenarioEditor(resolvedPath);
    }

    public ScenarioSaveResult SaveUserScenario(ScenarioSaveRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Scenario name is required.");
        }

        if (request.Scenario.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            throw new ArgumentException("Scenario JSON is required.");
        }

        var slug = SlugRegex().Replace(name.ToLowerInvariant(), "_").Trim('_');
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Scenario name must contain letters or numbers.");
        }

        var fileName = $"{slug}.json";
        var userScenarioRoot = UserScenarioRoot();
        Directory.CreateDirectory(userScenarioRoot);
        var path = Path.GetFullPath(Path.Combine(userScenarioRoot, fileName));
        EnsurePathInside(path, userScenarioRoot);
        if (File.Exists(path))
        {
            throw new InvalidOperationException($"A user scenario named {fileName} already exists.");
        }

        var scenario = SimulationScenarioJson.FromJson(request.Scenario.GetRawText()) with { Name = name };
        SimulationScenarioJson.Save(path, scenario);

        var relativePath = NormalizeRelativePath(Path.GetRelativePath(_repoRoot, path));
        var registry = LoadUserScenarioRegistry();
        registry.Scenarios[relativePath] = new UserScenarioRegistryEntry
        {
            Path = relativePath,
            Name = name,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        SaveUserScenarioRegistry(registry);

        var option = new ScenarioOption(
            name,
            relativePath,
            IsUserCreated: true,
            CanDelete: true);
        return new ScenarioSaveResult(option, BuildScenarioEditor(path));
    }

    public ScenarioDeleteResult DeleteUserScenario(string scenarioPath)
    {
        if (string.IsNullOrWhiteSpace(scenarioPath))
        {
            throw new ArgumentException("Scenario path is required.");
        }

        var path = ResolveScenarioPath(scenarioPath);
        var userScenarioRoot = UserScenarioRoot();
        EnsurePathInside(path, userScenarioRoot);

        var relativePath = NormalizeRelativePath(Path.GetRelativePath(_repoRoot, path));
        var registry = LoadUserScenarioRegistry();
        if (!registry.Scenarios.ContainsKey(relativePath))
        {
            throw new InvalidOperationException("Only launcher-created user scenarios can be deleted.");
        }

        if (!File.Exists(path))
        {
            registry.Scenarios.Remove(relativePath);
            SaveUserScenarioRegistry(registry);
            return new ScenarioDeleteResult(relativePath, string.Empty);
        }

        var archiveRoot = Path.Combine(_repoRoot, "out", "scenario-trash");
        Directory.CreateDirectory(archiveRoot);
        var archivePath = GetUniqueArchivePath(Path.Combine(
            archiveRoot,
            $"{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Path.GetFileName(path)}"));
        File.Move(path, archivePath);

        registry.Scenarios.Remove(relativePath);
        SaveUserScenarioRegistry(registry);
        return new ScenarioDeleteResult(
            relativePath,
            NormalizeRelativePath(Path.GetRelativePath(_repoRoot, archivePath)));
    }

    public IReadOnlyList<RunSummary> ListRuns()
    {
        return _runs.Values
            .Select(ToSummary)
            .OrderByDescending(run => run.CreatedAtUtc)
            .ToArray();
    }

    public RunSummary? GetRun(string id)
    {
        return _runs.TryGetValue(id, out var run) ? ToSummary(run) : null;
    }

    public RunDetails? GetRunDetails(string id, int lineCount)
    {
        if (!_runs.TryGetValue(id, out var run))
        {
            return null;
        }

        var summary = ToSummary(run);
        var manifest = run.Manifest;
        var maxLines = Math.Clamp(lineCount, 10, 300);
        return new RunDetails(
            Run: summary,
            CommandLine: manifest.CommandLine,
            Error: manifest.Error ?? summary.FailureReason,
            Artifacts: ListRunArtifacts(manifest),
            StdoutTail: ReadTail(manifest.StdoutPath, maxLines),
            StderrTail: ReadTail(manifest.StderrPath, maxLines));
    }

    public RunCloneSettings? GetRunCloneSettings(string id)
    {
        if (!_runs.TryGetValue(id, out var run))
        {
            return null;
        }

        run = RefreshRunLifecycle(run);
        var manifest = run.Manifest;
        var cloneScenarioPath = ResolveCloneScenarioPath(manifest);
        var launchScenarioPath = File.Exists(ResolveScenarioPath(manifest.ScenarioPath))
            ? manifest.ScenarioPath
            : Path.GetRelativePath(_repoRoot, cloneScenarioPath);

        return new RunCloneSettings(
            SourceRunId: manifest.Id,
            SourceRunName: manifest.Name,
            ScenarioPath: launchScenarioPath,
            Ticks: manifest.Ticks,
            Seed: manifest.Seed,
            CheckpointIntervalTicks: manifest.CheckpointIntervalTicks,
            StopOnExtinction: manifest.StopOnExtinction,
            ScenarioEditor: BuildScenarioEditor(cloneScenarioPath));
    }

    public async Task<RunRerunResult?> RerunRunAsync(string id)
    {
        if (!_runs.TryGetValue(id, out var run))
        {
            return null;
        }

        run = RefreshRunLifecycle(run);
        if (run.IsRunning)
        {
            throw new InvalidOperationException("Running simulations cannot be rerun in place.");
        }

        var manifest = run.Manifest;
        var cloneScenarioPath = ResolveCloneScenarioPath(manifest);
        using var document = JsonDocument.Parse(File.ReadAllText(cloneScenarioPath));
        var scenario = document.RootElement.Clone();
        var launchScenarioPath = File.Exists(ResolveScenarioPath(manifest.ScenarioPath))
            ? manifest.ScenarioPath
            : Path.GetRelativePath(_repoRoot, cloneScenarioPath);

        var replacement = await StartRunAsync(new RunCreateRequest(
            ScenarioPath: launchScenarioPath,
            Ticks: manifest.Ticks,
            Seed: manifest.Seed,
            CheckpointIntervalTicks: manifest.CheckpointIntervalTicks,
            StopOnExtinction: manifest.StopOnExtinction,
            Scenario: scenario));

        if (!string.Equals(replacement.Name, manifest.Name, StringComparison.Ordinal))
        {
            replacement = RenameRun(replacement.Id, manifest.Name) ?? replacement;
        }

        var deletedOriginal = DeleteRun(id, deleteArtifacts: true);
        return new RunRerunResult(replacement, deletedOriginal);
    }

    public async Task<RunContinueResult?> ContinueRunAsync(string id, RunContinueRequest request)
    {
        if (!_runs.TryGetValue(id, out var run))
        {
            return null;
        }

        if (request.Ticks <= 0)
        {
            throw new ArgumentException("Additional ticks must be positive.");
        }

        run = RefreshRunLifecycle(run);
        if (run.IsRunning)
        {
            throw new InvalidOperationException("Running simulations cannot be continued from a checkpoint.");
        }

        var manifest = run.Manifest;
        var snapshotPath = ResolveContinuationSnapshotPath(manifest, request.SnapshotPath);
        manifest.LoadSnapshotPath = snapshotPath;
        manifest.Ticks = request.Ticks;
        manifest.Status = "starting";
        manifest.EndedAtUtc = null;
        manifest.ExitCode = null;
        manifest.Error = null;
        TryDeleteFile(manifest.StatusPath);
        TryDeleteFile($"{manifest.StatusPath}.tmp");
        TryDeleteFile(manifest.ControlPath);

        var continued = StartManagedRun(run, addToRuns: false);
        return new RunContinueResult(continued, Path.GetRelativePath(_repoRoot, snapshotPath));
    }

    public async Task<RunSummary> StartRunAsync(RunCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ScenarioPath))
        {
            throw new ArgumentException("Scenario path is required.");
        }

        if (request.Ticks <= 0)
        {
            throw new ArgumentException("Ticks must be positive.");
        }

        var scenarioPath = ResolveScenarioPath(request.ScenarioPath);
        if (!File.Exists(scenarioPath))
        {
            throw new FileNotFoundException("Scenario file was not found.", request.ScenarioPath);
        }

        var createdAt = DateTimeOffset.UtcNow;
        var scenarioName = Path.GetFileNameWithoutExtension(scenarioPath);
        var seed = request.Seed ?? TryReadScenarioSeed(request.Scenario) ?? TryReadScenarioSeed(scenarioPath);
        var id = $"{createdAt:yyyyMMdd_HHmmss}_{Slugify(scenarioName)}_{ProcessIdToken}";
        var runDirectory = Path.Combine(_runsRoot, id);
        var launchScenarioPath = WriteLaunchScenarioIfProvided(request.Scenario, createdAt, scenarioName);
        var loadSnapshotPath = ResolveLoadSnapshotPath(request.LoadSnapshotPath);

        var manifest = new RunManifest
        {
            Id = id,
            Name = $"{scenarioName} {createdAt:yyyy-MM-dd HH:mm:ss}",
            Status = "starting",
            ScenarioPath = Path.GetRelativePath(_repoRoot, scenarioPath),
            LoadSnapshotPath = loadSnapshotPath,
            ScenarioName = scenarioName,
            LaunchScenarioPath = launchScenarioPath,
            ResolvedScenarioPath = Path.Combine(runDirectory, "resolved_scenario.json"),
            Seed = seed,
            Ticks = request.Ticks,
            CheckpointIntervalTicks = request.CheckpointIntervalTicks,
            StopOnExtinction = request.StopOnExtinction,
            CreatedAtUtc = createdAt,
            StartedAtUtc = createdAt,
            WorkingDirectory = _repoRoot,
            RunDirectory = runDirectory,
            StatsPath = Path.Combine(runDirectory, "stats.csv"),
            ReportPath = Path.Combine(runDirectory, "report.html"),
            SnapshotPath = Path.Combine(runDirectory, "final_snapshot.json"),
            CheckpointDirectory = Path.Combine(runDirectory, "checkpoints"),
            StatusPath = Path.Combine(runDirectory, "status.json"),
            ControlPath = Path.Combine(runDirectory, "control.json"),
            StdoutPath = Path.Combine(runDirectory, "stdout.log"),
            StderrPath = Path.Combine(runDirectory, "stderr.log")
        };

        var managedRun = new ManagedRun(manifest, null);
        try
        {
            return StartManagedRun(managedRun, addToRuns: true);
        }
        catch
        {
            DeleteLaunchScenario(manifest);
            _runs.TryRemove(manifest.Id, out _);
            throw;
        }
    }

    public bool SendControl(string id, string command)
    {
        if (!_runs.TryGetValue(id, out var run) || !RefreshRunLifecycle(run).IsRunning)
        {
            return false;
        }

        var request = new RunCommandRequest(command);
        File.WriteAllText(run.Manifest.ControlPath, JsonSerializer.Serialize(request, JsonOptions));
        return true;
    }

    private RunSummary StartManagedRun(ManagedRun managedRun, bool addToRuns)
    {
        var manifest = managedRun.Manifest;
        var launchTarget = ResolveCliLaunchTarget();
        var arguments = BuildCliArguments(manifest);
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = launchTarget.FileName,
                WorkingDirectory = _repoRoot,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        foreach (var argument in launchTarget.PrefixArguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        managedRun.AttachProcess(process);
        try
        {
            process.Start();
            ApplyProcessId(manifest, process.Id);
            manifest.StartedAtUtc = DateTimeOffset.UtcNow;
            manifest.EndedAtUtc = null;
            manifest.ExitCode = null;
            manifest.Error = null;
            manifest.Status = "running";
            manifest.CommandLine = FormatCommandLine(
                launchTarget.FileName,
                launchTarget.PrefixArguments.Concat(BuildCliArguments(manifest)));

            if (addToRuns && !_runs.TryAdd(manifest.Id, managedRun))
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                throw new InvalidOperationException($"Run id collision for {manifest.Id}.");
            }

            SaveManifest(manifest);
            process.Exited += (_, _) => MarkProcessExited(managedRun);
            if (process.HasExited)
            {
                MarkProcessExited(managedRun);
            }

            return ToSummary(managedRun);
        }
        catch
        {
            managedRun.DisposeProcess();
            throw;
        }
    }

    private string WriteLaunchScenarioIfProvided(JsonElement? scenarioElement, DateTimeOffset createdAt, string scenarioName)
    {
        if (scenarioElement is null
            || scenarioElement.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return string.Empty;
        }

        var scenario = SimulationScenarioJson.FromJson(scenarioElement.Value.GetRawText());
        var launchScenarioDirectory = Path.Combine(_runsRoot, "_launch_scenarios");
        Directory.CreateDirectory(launchScenarioDirectory);
        var launchScenarioPath = Path.Combine(
            launchScenarioDirectory,
            $"{createdAt:yyyyMMdd_HHmmss}_{Slugify(scenarioName)}_{Guid.NewGuid():N}.json");
        SimulationScenarioJson.Save(launchScenarioPath, scenario);
        return launchScenarioPath;
    }

    public RunSummary? RenameRun(string id, string name)
    {
        if (!_runs.TryGetValue(id, out var run))
        {
            return null;
        }

        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new ArgumentException("Run name is required.");
        }

        if (trimmedName.Length > 160)
        {
            throw new ArgumentException("Run name must be 160 characters or fewer.");
        }

        run.Manifest.Name = trimmedName;
        SaveManifest(run.Manifest);
        return ToSummary(run);
    }

    public RunBulkDeleteResult DeleteRuns(IReadOnlyList<string> ids, bool deleteArtifacts)
    {
        var requestedIds = ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var deleted = 0;
        var skipped = new List<string>();
        foreach (var id in requestedIds)
        {
            if (DeleteRun(id, deleteArtifacts))
            {
                deleted++;
            }
            else
            {
                skipped.Add(id);
            }
        }

        return new RunBulkDeleteResult(requestedIds.Length, deleted, skipped);
    }

    public string ExportRunsMarkdown(IReadOnlyList<string>? ids)
    {
        var requestedIds = (ids ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var selectedRuns = requestedIds
            .Select(id => _runs.TryGetValue(id, out var run) ? run : null)
            .Where(run => run is not null)
            .Cast<ManagedRun>()
            .Select(run => (Run: run, Summary: ToSummary(run)))
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine("# Lineage Run Comparison Export");
        builder.AppendLine();
        builder.AppendLine($"Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine($"Requested runs: {requestedIds.Length.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine($"Included runs: {selectedRuns.Length.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine();

        if (selectedRuns.Length == 0)
        {
            builder.AppendLine("No matching runs were found.");
            return builder.ToString();
        }

        builder.AppendLine("## Summary Table");
        builder.AppendLine();
        AppendMarkdownTable(
            builder,
            [
                "Run",
                "Status",
                "Scenario",
                "Brain",
                "Starter",
                "World",
                "Plants/M",
                "Seed",
                "Requested ticks",
                "Final tick",
                "Progress",
                "Creatures",
                "Eggs",
                "Species",
                "Births",
                "Deaths",
                "Max gen",
                "Checkpoints",
                "Stop reason"
            ],
            selectedRuns
                .Select(run =>
                {
                    var summary = run.Summary;
                    return new[]
                    {
                        MarkdownCell(summary.Name),
                        MarkdownCell(summary.Status),
                        MarkdownCell(summary.ScenarioName),
                        MarkdownCell(summary.ScenarioSummary?.BrainArchitectureKind),
                        MarkdownCell(summary.ScenarioSummary?.InitialBrainKind),
                        MarkdownCell(FormatScenarioWorld(summary.ScenarioSummary)),
                        MarkdownCell(FormatScenarioDensity(summary.ScenarioSummary)),
                        MarkdownCell(summary.Seed),
                        MarkdownCell(summary.Ticks),
                        MarkdownCell(summary.CurrentTick),
                        MarkdownCell(FormatPercent(summary.Progress)),
                        MarkdownCell(summary.CreatureCount),
                        MarkdownCell(summary.EggCount),
                        MarkdownCell(summary.SpeciesClusterCount),
                        MarkdownCell(summary.CreatureBirthCount),
                        MarkdownCell(summary.CreatureDeathCount),
                        MarkdownCell(summary.MaxGeneration),
                        MarkdownCell(summary.CheckpointCount),
                        MarkdownCell(summary.StopReason)
                    };
                })
                .ToArray(),
            [
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                false
            ]);

        builder.AppendLine();
        builder.AppendLine("## Run Details");

        foreach (var (run, summary) in selectedRuns)
        {
            var manifest = run.Manifest;
            builder.AppendLine();
            builder.AppendLine($"### {EscapeMarkdownHeading(summary.Name)}");
            builder.AppendLine();
            AppendDetail(builder, "Id", summary.Id);
            AppendDetail(builder, "Status", summary.Status);
            AppendDetail(builder, "Scenario", $"{summary.ScenarioName} ({summary.ScenarioPath})");
            AppendDetail(builder, "Loaded snapshot", manifest.LoadSnapshotPath);
            AppendDetail(builder, "Launch scenario", summary.LaunchScenarioPath);
            AppendDetail(builder, "Resolved scenario", summary.ResolvedScenarioPath);
            AppendDetail(builder, "Scenario source", summary.ScenarioSummary?.IsResolvedSnapshot == true ? "resolved run snapshot" : "current scenario file fallback");
            AppendDetail(builder, "Scenario brain", FormatScenarioBrain(summary.ScenarioSummary));
            AppendDetail(builder, "Scenario vision", FormatScenarioVision(summary.ScenarioSummary));
            AppendDetail(builder, "Scenario world", FormatScenarioWorld(summary.ScenarioSummary));
            AppendDetail(builder, "Scenario resources", FormatScenarioResources(summary.ScenarioSummary));
            AppendDetail(builder, "Scenario terrain", FormatScenarioTerrain(summary.ScenarioSummary));
            AppendDetail(builder, "Scenario meat", FormatScenarioMeat(summary.ScenarioSummary));
            AppendDetail(builder, "Seed", summary.Seed);
            AppendDetail(builder, "Requested ticks", summary.Ticks);
            AppendDetail(builder, "Completed steps", summary.CompletedSteps);
            AppendDetail(builder, "Current tick", summary.CurrentTick);
            AppendDetail(builder, "Progress", FormatPercent(summary.Progress));
            AppendDetail(builder, "Creatures", summary.CreatureCount);
            AppendDetail(builder, "Eggs", summary.EggCount);
            AppendDetail(builder, "Species clusters", summary.SpeciesClusterCount);
            AppendDetail(builder, "Births", summary.CreatureBirthCount);
            AppendDetail(builder, "Deaths", summary.CreatureDeathCount);
            AppendDetail(builder, "Max generation", summary.MaxGeneration);
            AppendDetail(builder, "Checkpoints", summary.CheckpointCount);
            AppendDetail(builder, "Stop reason", summary.StopReason);
            AppendDetail(builder, "Exit code", summary.ExitCode);
            AppendDetail(builder, "Process id", summary.ProcessId);
            AppendDetail(builder, "Created", FormatDate(summary.CreatedAtUtc));
            AppendDetail(builder, "Started", FormatDate(summary.StartedAtUtc));
            AppendDetail(builder, "Ended", FormatDate(summary.EndedAtUtc));
            AppendDetail(builder, "Wall time", FormatDuration(summary.StartedAtUtc, summary.EndedAtUtc));
            AppendDetail(builder, "Run directory", summary.RunDirectory);
            AppendDetail(builder, "Report", summary.ReportPath);
            AppendDetail(builder, "Stats", summary.StatsPath);
            AppendDetail(builder, "Final snapshot", summary.SnapshotPath);
            AppendDetail(builder, "Checkpoint directory", summary.CheckpointDirectory);
            AppendDetail(builder, "Latest checkpoint", summary.LatestCheckpointPath);
            AppendDetail(builder, "Status file", summary.StatusPath);
            AppendDetail(builder, "Stdout log", summary.StdoutPath);
            AppendDetail(builder, "Stderr log", summary.StderrPath);
            AppendDetail(builder, "Command", manifest.CommandLine);
            AppendDetail(builder, "Error", manifest.Error);
        }

        return builder.ToString();
    }

    public bool DeleteRun(string id, bool deleteArtifacts)
    {
        if (!_runs.TryGetValue(id, out var run))
        {
            return false;
        }

        if (RefreshRunLifecycle(run).IsRunning)
        {
            return false;
        }

        if (deleteArtifacts)
        {
            try
            {
                if (Directory.Exists(run.Manifest.RunDirectory))
                {
                    Directory.Delete(run.Manifest.RunDirectory, recursive: true);
                }

                DeleteLaunchScenario(run.Manifest);
            }
            catch
            {
                return false;
            }
        }

        _runs.TryRemove(id, out _);
        return true;
    }

    private void DeleteLaunchScenario(RunManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.LaunchScenarioPath)
            || !File.Exists(manifest.LaunchScenarioPath))
        {
            return;
        }

        try
        {
            var fullPath = Path.GetFullPath(manifest.LaunchScenarioPath);
            var launchScenarioRoot = Path.GetFullPath(Path.Combine(_runsRoot, "_launch_scenarios"));
            var relativePath = Path.GetRelativePath(launchScenarioRoot, fullPath);
            if (relativePath.StartsWith("..", StringComparison.Ordinal)
                || Path.IsPathRooted(relativePath))
            {
                return;
            }

            File.Delete(fullPath);
        }
        catch
        {
        }
    }

    public string? GetReportPath(string id)
    {
        if (!_runs.TryGetValue(id, out var run) || !File.Exists(run.Manifest.ReportPath))
        {
            return null;
        }

        return run.Manifest.ReportPath;
    }

    private RunSummary ToSummary(ManagedRun run)
    {
        run = RefreshRunLifecycle(run);
        var manifest = run.Manifest;
        var status = ReadStatus(manifest.StatusPath);
        var isRunning = run.IsRunning;
        var statusText = isRunning
            ? status?.State ?? manifest.Status
            : manifest.Status;

        return new RunSummary(
            Id: manifest.Id,
            Name: manifest.Name,
            Status: statusText,
            ScenarioPath: manifest.ScenarioPath,
            ScenarioName: manifest.ScenarioName,
            LaunchScenarioPath: manifest.LaunchScenarioPath,
            ResolvedScenarioPath: EffectiveResolvedScenarioPath(manifest),
            ScenarioSummary: ReadScenarioSummary(manifest),
            Seed: status?.Seed ?? manifest.Seed,
            Ticks: manifest.Ticks,
            CheckpointIntervalTicks: manifest.CheckpointIntervalTicks,
            StopOnExtinction: manifest.StopOnExtinction,
            CreatedAtUtc: manifest.CreatedAtUtc,
            StartedAtUtc: manifest.StartedAtUtc,
            EndedAtUtc: manifest.EndedAtUtc,
            ExitCode: manifest.ExitCode,
            ProcessId: manifest.ProcessId,
            FailureReason: BuildFailureReason(manifest, statusText),
            RunDirectory: manifest.RunDirectory,
            StatsPath: manifest.StatsPath,
            ReportPath: manifest.ReportPath,
            SnapshotPath: manifest.SnapshotPath,
            CheckpointDirectory: manifest.CheckpointDirectory,
            StatusPath: manifest.StatusPath,
            ControlPath: manifest.ControlPath,
            StdoutPath: manifest.StdoutPath,
            StderrPath: manifest.StderrPath,
            CurrentTick: status?.CurrentTick ?? 0,
            CompletedSteps: status?.CompletedSteps ?? 0,
            Progress: status?.Progress ?? 0d,
            CreatureCount: status?.CreatureCount ?? 0,
            EggCount: status?.EggCount ?? 0,
            SpeciesClusterCount: status?.SpeciesClusterCount ?? 0,
            MaxGeneration: status?.MaxGeneration ?? 0,
            CreatureBirthCount: status?.CreatureBirthCount ?? 0,
            CreatureDeathCount: status?.CreatureDeathCount ?? 0,
            StopReason: status?.StopReason,
            LatestCheckpointPath: status?.LatestCheckpointPath,
            CheckpointCount: status?.CheckpointCount ?? 0,
            IsRunning: isRunning,
            HasReport: File.Exists(manifest.ReportPath));
    }

    private static string? BuildFailureReason(RunManifest manifest, string status)
    {
        if (!IsProblemStatus(status))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(manifest.Error))
        {
            return manifest.Error.Trim();
        }

        var stderrLine = ReadLastNonEmptyLine(manifest.StderrPath);
        if (!string.IsNullOrWhiteSpace(stderrLine))
        {
            return stderrLine;
        }

        return manifest.ExitCode is { } exitCode && exitCode != 0
            ? $"Process exited with code {exitCode.ToString(CultureInfo.InvariantCulture)}."
            : null;
    }

    private static bool IsProblemStatus(string status)
    {
        return status.Equals("failed", StringComparison.OrdinalIgnoreCase)
            || status.Equals("lost", StringComparison.OrdinalIgnoreCase)
            || status.Equals("unknown", StringComparison.OrdinalIgnoreCase);
    }

    private void MarkProcessExited(ManagedRun run)
    {
        if (!run.TryRecordExit())
        {
            return;
        }

        var manifest = run.Manifest;
        var process = run.Process;
        manifest.ExitCode = process?.ExitCode;
        manifest.EndedAtUtc = DateTimeOffset.UtcNow;
        manifest.Status = manifest.ExitCode == 0 ? "completed" : "failed";
        SaveManifest(manifest);
        run.DisposeProcess();
    }

    private void LoadExistingRunManifests()
    {
        if (!Directory.Exists(_runsRoot))
        {
            return;
        }

        foreach (var manifestPath in Directory.EnumerateFiles(_runsRoot, "run.manifest.json", SearchOption.AllDirectories))
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<RunManifest>(File.ReadAllText(manifestPath), JsonOptions);
                if (manifest is null || string.IsNullOrWhiteSpace(manifest.Id))
                {
                    continue;
                }

                EnsureManifestDefaults(manifest);

                if (manifest.Status is "running" or "starting")
                {
                    var restored = TryRestoreRunningProcess(manifest);
                    if (restored is not null)
                    {
                        _runs.TryAdd(manifest.Id, restored);
                        continue;
                    }

                    MarkMissingProcess(manifest);
                }

                _runs.TryAdd(manifest.Id, new ManagedRun(manifest, null));
            }
            catch
            {
                // Ignore malformed imported manifests for now; the library can gain diagnostics later.
            }
        }
    }

    private ManagedRun RefreshRunLifecycle(ManagedRun run)
    {
        if (run.Process is not null)
        {
            if (run.IsRunning)
            {
                return run;
            }

            MarkProcessExited(run);
        }

        return run;
    }

    private static void EnsureManifestDefaults(RunManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.ResolvedScenarioPath)
            && !string.IsNullOrWhiteSpace(manifest.RunDirectory))
        {
            manifest.ResolvedScenarioPath = Path.Combine(manifest.RunDirectory, "resolved_scenario.json");
        }

        manifest.LoadSnapshotPath ??= string.Empty;
        manifest.LaunchScenarioPath ??= string.Empty;
    }

    private ManagedRun? TryRestoreRunningProcess(RunManifest manifest)
    {
        var process = TryOpenLiveProcess(manifest);
        if (process is null)
        {
            return null;
        }

        manifest.Status = "running";
        manifest.EndedAtUtc = null;
        manifest.ExitCode = null;
        manifest.Error = null;
        SaveManifest(manifest);

        var run = new ManagedRun(manifest, process);
        process.Exited += (_, _) => MarkProcessExited(run);
        if (process.HasExited)
        {
            MarkProcessExited(run);
        }

        return run;
    }

    private static Process? TryOpenLiveProcess(RunManifest manifest)
    {
        if (manifest.ProcessId is not > 0)
        {
            return null;
        }

        try
        {
            var process = Process.GetProcessById(manifest.ProcessId.Value);
            if (process.HasExited || !ProcessMatchesManifest(process, manifest))
            {
                process.Dispose();
                return null;
            }

            process.EnableRaisingEvents = true;
            return process;
        }
        catch
        {
            return null;
        }
    }

    private static bool ProcessMatchesManifest(Process process, RunManifest manifest)
    {
        try
        {
            var name = process.ProcessName;
            if (!name.Contains("Lineage.Cli", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(name, "dotnet", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (manifest.StartedAtUtc is not { } startedAt)
            {
                return true;
            }

            var startedAtUtc = new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero);
            return startedAtUtc >= startedAt.AddSeconds(-10)
                && startedAtUtc <= startedAt.AddMinutes(2);
        }
        catch
        {
            return false;
        }
    }

    private void MarkMissingProcess(RunManifest manifest)
    {
        var status = ReadStatus(manifest.StatusPath);
        if (status is not null && string.Equals(status.State, "completed", StringComparison.OrdinalIgnoreCase))
        {
            manifest.Status = "completed";
            manifest.EndedAtUtc ??= status.UpdatedAtUtc == default ? DateTimeOffset.UtcNow : status.UpdatedAtUtc;
            manifest.Error ??= "Runner restarted after the CLI completed; exit code was not captured.";
        }
        else
        {
            manifest.Status = "lost";
            manifest.EndedAtUtc ??= DateTimeOffset.UtcNow;
            manifest.Error = manifest.ProcessId is null
                ? "Runner restarted with no recorded CLI process id."
                : $"Runner restarted, but CLI process {manifest.ProcessId.Value.ToString(CultureInfo.InvariantCulture)} was not found.";
        }

        SaveManifest(manifest);
    }

    private static CliStatusFile? ReadStatus(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return JsonSerializer.Deserialize<CliStatusFile>(stream, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private RunScenarioSummary? ReadScenarioSummary(RunManifest manifest)
    {
        var resolvedScenarioPath = EffectiveResolvedScenarioPath(manifest);
        if (File.Exists(resolvedScenarioPath))
        {
            return ReadScenarioSummary(resolvedScenarioPath, isResolvedSnapshot: true);
        }

        var sourceScenarioPath = ResolveScenarioPath(manifest.ScenarioPath);
        return File.Exists(sourceScenarioPath)
            ? ReadScenarioSummary(sourceScenarioPath, isResolvedSnapshot: false)
            : null;
    }

    private static RunScenarioSummary? ReadScenarioSummary(string path, bool isResolvedSnapshot)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var worldWidth = GetDouble(root, "worldWidth");
            var worldHeight = GetDouble(root, "worldHeight");
            var resourceDensity = GetDouble(root, "initialResourcesPerMillionArea");
            var calculatedResourceCount = worldWidth is not null && worldHeight is not null && resourceDensity is not null
                ? (int?)Math.Max(0, (int)Math.Round(resourceDensity.Value * worldWidth.Value * worldHeight.Value / 1_000_000d))
                : null;

            var brainArchitectureKind = GetString(root, "brainArchitectureKind") ?? "hybridNeural";
            var initialBrainKind = GetString(root, "initialBrainKind") ?? "sectorForager";
            var brainHiddenNodeCount = GetInt32(root, "brainHiddenNodeCount")
                ?? (string.Equals(brainArchitectureKind, "hiddenLayerNeural", StringComparison.OrdinalIgnoreCase) ? 8 : 4);

            return new RunScenarioSummary(
                Path: path,
                IsResolvedSnapshot: isResolvedSnapshot,
                Name: GetString(root, "name"),
                Seed: GetUInt64(root, "seed"),
                PipelineKind: GetString(root, "pipelineKind"),
                BrainArchitectureKind: brainArchitectureKind,
                InitialBrainKind: initialBrainKind,
                BrainHiddenNodeCount: brainHiddenNodeCount,
                EnableSectorVision: GetBoolean(root, "enableSectorVision"),
                EnableLegacyNearestFoodVisionInputs: GetBoolean(root, "enableLegacyNearestFoodVisionInputs"),
                EnableLegacyNearestCreatureVisionInputs: GetBoolean(root, "enableLegacyNearestCreatureVisionInputs"),
                WorldWidth: worldWidth,
                WorldHeight: worldHeight,
                InitialCreatureCount: GetInt32(root, "initialCreatureCount"),
                InitialResourcesPerMillionArea: resourceDensity,
                InitialResourceCount: calculatedResourceCount ?? GetInt32(root, "initialResourceCount"),
                BiomeMapKind: GetString(root, "biomeMapKind"),
                EnableObstacles: GetBoolean(root, "enableObstacles"),
                ObstacleMapKind: GetString(root, "obstacleMapKind"),
                ResourceVoidBorderWidth: GetDouble(root, "resourceVoidBorderWidth"),
                VisionAngleDegrees: GetDouble(root, "visionAngleRadians") is { } radians
                    ? radians * 180d / Math.PI
                    : null,
                DeathMeatCaloriesPerBodyRadius: GetDouble(root, "deathMeatCaloriesPerBodyRadius"),
                DeathMeatEnergyFraction: GetDouble(root, "deathMeatEnergyFraction"),
                MeatDecayCaloriesPerSecond: GetDouble(root, "meatDecayCaloriesPerSecond"),
                RottenMeatDamagePerRawKcal: GetDouble(root, "rottenMeatDamagePerRawKcal"),
                SpeciesSeedCount: CountEnabledSpeciesSeeds(root));
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<string> ReadTail(string path, int maxLines)
    {
        if (!File.Exists(path) || maxLines <= 0)
        {
            return Array.Empty<string>();
        }

        try
        {
            var lines = new Queue<string>(maxLines);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            while (reader.ReadLine() is { } line)
            {
                if (lines.Count == maxLines)
                {
                    lines.Dequeue();
                }

                lines.Enqueue(line);
            }

            return lines.ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string? ReadLastNonEmptyLine(string path)
    {
        return ReadTail(path, 20)
            .Reverse()
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line))
            ?.Trim();
    }

    private static void TryDeleteFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch
        {
        }
    }

    private void SaveManifest(RunManifest manifest)
    {
        Directory.CreateDirectory(manifest.RunDirectory);
        var path = Path.Combine(manifest.RunDirectory, "run.manifest.json");
        var tempPath = $"{path}.tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(manifest, JsonOptions));
        File.Move(tempPath, path, overwrite: true);
    }

    private static void ApplyProcessId(RunManifest manifest, int processId)
    {
        var processIdText = processId.ToString(CultureInfo.InvariantCulture);
        manifest.ProcessId = processId;
        manifest.Id = ReplaceProcessIdToken(manifest.Id, processIdText);
        manifest.RunDirectory = ReplaceProcessIdToken(manifest.RunDirectory, processIdText);
        manifest.ResolvedScenarioPath = ReplaceProcessIdToken(manifest.ResolvedScenarioPath, processIdText);
        manifest.StatsPath = ReplaceProcessIdToken(manifest.StatsPath, processIdText);
        manifest.ReportPath = ReplaceProcessIdToken(manifest.ReportPath, processIdText);
        manifest.SnapshotPath = ReplaceProcessIdToken(manifest.SnapshotPath, processIdText);
        manifest.CheckpointDirectory = ReplaceProcessIdToken(manifest.CheckpointDirectory, processIdText);
        manifest.StatusPath = ReplaceProcessIdToken(manifest.StatusPath, processIdText);
        manifest.ControlPath = ReplaceProcessIdToken(manifest.ControlPath, processIdText);
        manifest.StdoutPath = ReplaceProcessIdToken(manifest.StdoutPath, processIdText);
        manifest.StderrPath = ReplaceProcessIdToken(manifest.StderrPath, processIdText);
    }

    private static string ReplaceProcessIdToken(string value, string processId)
    {
        return value.Replace(ProcessIdToken, processId, StringComparison.OrdinalIgnoreCase);
    }

    private static string EffectiveResolvedScenarioPath(RunManifest manifest)
    {
        return string.IsNullOrWhiteSpace(manifest.ResolvedScenarioPath)
            ? Path.Combine(manifest.RunDirectory, "resolved_scenario.json")
            : manifest.ResolvedScenarioPath;
    }

    private static string EffectiveCliScenarioPath(RunManifest manifest)
    {
        return string.IsNullOrWhiteSpace(manifest.LaunchScenarioPath)
            ? manifest.ScenarioPath
            : manifest.LaunchScenarioPath;
    }

    private string ResolveLoadSnapshotPath(string? loadSnapshotPath)
    {
        if (string.IsNullOrWhiteSpace(loadSnapshotPath))
        {
            return string.Empty;
        }

        var resolvedPath = ResolveArtifactPath(loadSnapshotPath);
        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException("Snapshot file was not found.", loadSnapshotPath);
        }

        return resolvedPath;
    }

    private IReadOnlyList<RunArtifact> ListRunArtifacts(RunManifest manifest)
    {
        var status = ReadStatus(manifest.StatusPath);
        var latestCheckpointPath = string.IsNullOrWhiteSpace(status?.LatestCheckpointPath)
            ? string.Empty
            : ResolveArtifactPath(status.LatestCheckpointPath);
        var artifacts = new List<RunArtifact>();
        var seenPaths = new HashSet<string>(PathComparer());

        AddArtifact("snapshot", "Final snapshot", manifest.SnapshotPath, tick: null, isContinuationSource: true);

        if (!string.IsNullOrWhiteSpace(manifest.CheckpointDirectory))
        {
            var checkpointDirectory = ResolveArtifactPath(manifest.CheckpointDirectory);
            if (Directory.Exists(checkpointDirectory))
            {
                foreach (var checkpointPath in Directory
                    .EnumerateFiles(checkpointDirectory, "*.json", SearchOption.TopDirectoryOnly)
                    .Select(path => new
                    {
                        Path = path,
                        Tick = TryParseCheckpointTick(path),
                        ModifiedAtUtc = File.GetLastWriteTimeUtc(path)
                    })
                    .OrderByDescending(item => item.Tick ?? long.MinValue)
                    .ThenByDescending(item => item.ModifiedAtUtc))
                {
                    var isLatest = !string.IsNullOrWhiteSpace(latestCheckpointPath)
                        && PathsEqual(checkpointPath.Path, latestCheckpointPath);
                    AddArtifact(
                        "checkpoint",
                        isLatest ? "Checkpoint (latest)" : "Checkpoint",
                        checkpointPath.Path,
                        checkpointPath.Tick,
                        isContinuationSource: true,
                        isLatestCheckpoint: isLatest);
                }
            }
        }

        AddArtifact("report", "HTML report", manifest.ReportPath);
        AddArtifact("stats", "Stats CSV", manifest.StatsPath);
        AddArtifact("scenario", "Resolved scenario", EffectiveResolvedScenarioPath(manifest));
        AddArtifact("scenario", "Launch scenario", manifest.LaunchScenarioPath);
        AddArtifact("manifest", "Run manifest", Path.Combine(manifest.RunDirectory, "run.manifest.json"));
        AddArtifact("log", "stdout.log", manifest.StdoutPath);
        AddArtifact("log", "stderr.log", manifest.StderrPath);

        return artifacts;

        void AddArtifact(
            string type,
            string label,
            string path,
            long? tick = null,
            bool isContinuationSource = false,
            bool isLatestCheckpoint = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var resolvedPath = ResolveArtifactPath(path);
            if (string.IsNullOrWhiteSpace(resolvedPath))
            {
                return;
            }

            resolvedPath = Path.GetFullPath(resolvedPath);
            if (!seenPaths.Add(resolvedPath))
            {
                return;
            }

            var file = new FileInfo(resolvedPath);
            artifacts.Add(new RunArtifact(
                Type: type,
                Label: label,
                Path: resolvedPath,
                Exists: file.Exists,
                SizeBytes: file.Exists ? file.Length : null,
                ModifiedAtUtc: file.Exists
                    ? new DateTimeOffset(DateTime.SpecifyKind(file.LastWriteTimeUtc, DateTimeKind.Utc))
                    : null,
                Tick: tick,
                IsContinuationSource: isContinuationSource,
                IsLatestCheckpoint: isLatestCheckpoint));
        }
    }

    private string ResolveContinuationSnapshotPath(RunManifest manifest, string? selectedSnapshotPath = null)
    {
        if (!string.IsNullOrWhiteSpace(selectedSnapshotPath))
        {
            var resolvedSelectedPath = ResolveArtifactPath(selectedSnapshotPath);
            if (!File.Exists(resolvedSelectedPath))
            {
                throw new FileNotFoundException("Selected snapshot file was not found.", selectedSnapshotPath);
            }

            if (!IsContinuationSourcePath(manifest, resolvedSelectedPath))
            {
                throw new InvalidOperationException("Selected snapshot is not a final snapshot or checkpoint for this run.");
            }

            return resolvedSelectedPath;
        }

        var status = ReadStatus(manifest.StatusPath);
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(status?.LatestCheckpointPath))
        {
            candidates.Add(status.LatestCheckpointPath);
        }

        if (!string.IsNullOrWhiteSpace(manifest.CheckpointDirectory))
        {
            var checkpointDirectory = ResolveArtifactPath(manifest.CheckpointDirectory);
            if (Directory.Exists(checkpointDirectory))
            {
                candidates.AddRange(Directory
                    .EnumerateFiles(checkpointDirectory, "*.json", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(File.GetLastWriteTimeUtc));
            }
        }

        if (!string.IsNullOrWhiteSpace(manifest.SnapshotPath))
        {
            candidates.Add(manifest.SnapshotPath);
        }

        foreach (var candidate in candidates)
        {
            var path = ResolveArtifactPath(candidate);
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException("No checkpoint or final snapshot was found for this run.", manifest.Id);
    }

    private bool IsContinuationSourcePath(RunManifest manifest, string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!string.IsNullOrWhiteSpace(manifest.SnapshotPath)
            && PathsEqual(fullPath, ResolveArtifactPath(manifest.SnapshotPath)))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(manifest.CheckpointDirectory)
            || !string.Equals(Path.GetExtension(fullPath), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var checkpointDirectory = ResolveArtifactPath(manifest.CheckpointDirectory);
        return !string.IsNullOrWhiteSpace(checkpointDirectory)
            && IsPathUnderDirectory(fullPath, checkpointDirectory);
    }

    private static bool IsPathUnderDirectory(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = Path.GetFullPath(directory);
        var relativePath = Path.GetRelativePath(fullDirectory, fullPath);
        return !relativePath.StartsWith("..", StringComparison.Ordinal)
            && !Path.IsPathRooted(relativePath);
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), PathComparison());
    }

    private static StringComparer PathComparer()
    {
        return OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    }

    private static StringComparison PathComparison()
    {
        return OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    private static long? TryParseCheckpointTick(string path)
    {
        var match = CheckpointTickRegex().Match(Path.GetFileName(path));
        return match.Success && long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tick)
            ? tick
            : null;
    }

    private string ResolveScenarioPath(string scenarioPath)
    {
        var path = Path.IsPathRooted(scenarioPath)
            ? scenarioPath
            : Path.Combine(_repoRoot, scenarioPath);
        return Path.GetFullPath(path);
    }

    private string UserScenarioRoot()
    {
        return Path.Combine(_repoRoot, "scenarios", UserScenarioFolderName);
    }

    private string UserScenarioRegistryPath()
    {
        return Path.Combine(UserScenarioRoot(), UserScenarioRegistryFileName);
    }

    private string ScenarioRecipeRoot()
    {
        return Path.Combine(_repoRoot, "scenarios", ScenarioRecipeFolderName);
    }

    private string ResolveRecipePath(string recipePath)
    {
        if (string.IsNullOrWhiteSpace(recipePath))
        {
            throw new ArgumentException("Recipe path is required.");
        }

        var path = ResolveScenarioPath(recipePath);
        EnsurePathInside(path, ScenarioRecipeRoot());
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Recipe file was not found.", recipePath);
        }

        return path;
    }

    private ScenarioRecipe? TryReadScenarioRecipe(string path)
    {
        try
        {
            var file = JsonSerializer.Deserialize<ScenarioRecipeFile>(File.ReadAllText(path), JsonOptions);
            if (file is null || string.IsNullOrWhiteSpace(file.Name))
            {
                return null;
            }

            file.Tags ??= [];
            file.Changes ??= new JsonObject();
            return ToScenarioRecipe(path, file);
        }
        catch
        {
            return null;
        }
    }

    private ScenarioRecipe ToScenarioRecipe(string path, ScenarioRecipeFile file)
    {
        return new ScenarioRecipe(
            Name: file.Name,
            Path: NormalizeRelativePath(Path.GetRelativePath(_repoRoot, path)),
            Description: file.Description ?? string.Empty,
            Tags: file.Tags ?? [],
            Changes: CloneJsonObject(file.Changes ?? new JsonObject()),
            CreatedAtUtc: file.CreatedAtUtc,
            UpdatedAtUtc: file.UpdatedAtUtc);
    }

    private static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string>? tags)
    {
        if (tags is null)
        {
            return Array.Empty<string>();
        }

        return tags
            .Select(tag => (tag ?? string.Empty).Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static JsonObject CloneJsonObject(JsonObject value)
    {
        return JsonNode.Parse(value.ToJsonString())?.AsObject() ?? new JsonObject();
    }

    private static void ValidateRecipeChanges(JsonObject changes)
    {
        if (changes.Count == 0)
        {
            throw new ArgumentException("Recipe changes cannot be empty.");
        }

        var knownFields = ScenarioFieldDefinitions
            .Select(field => field.JsonName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknownFields = changes
            .Select(pair => pair.Key)
            .Where(field => !knownFields.Contains(field))
            .OrderBy(field => field, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unknownFields.Length > 0)
        {
            throw new ArgumentException($"Recipe contains unknown scenario field(s): {string.Join(", ", unknownFields)}.");
        }
    }

    private static void SaveScenarioRecipeFile(string path, ScenarioRecipeFile file)
    {
        var tempPath = $"{path}.tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(file, JsonOptions));
        File.Move(tempPath, path, overwrite: true);
    }

    private UserScenarioRegistry LoadUserScenarioRegistry()
    {
        var registryPath = UserScenarioRegistryPath();
        if (!File.Exists(registryPath))
        {
            return new UserScenarioRegistry();
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<UserScenarioRegistry>(
                File.ReadAllText(registryPath),
                JsonOptions) ?? new UserScenarioRegistry();
            var normalized = new UserScenarioRegistry
            {
                SchemaVersion = loaded.SchemaVersion
            };

            foreach (var pair in loaded.Scenarios)
            {
                var entry = pair.Value;
                var relativePath = NormalizeRelativePath(string.IsNullOrWhiteSpace(entry.Path) ? pair.Key : entry.Path);
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    continue;
                }

                normalized.Scenarios[relativePath] = new UserScenarioRegistryEntry
                {
                    Path = relativePath,
                    Name = string.IsNullOrWhiteSpace(entry.Name)
                        ? Path.GetFileNameWithoutExtension(relativePath)
                        : entry.Name,
                    CreatedAtUtc = entry.CreatedAtUtc == default ? DateTimeOffset.UtcNow : entry.CreatedAtUtc,
                    UpdatedAtUtc = entry.UpdatedAtUtc == default ? DateTimeOffset.UtcNow : entry.UpdatedAtUtc
                };
            }

            return normalized;
        }
        catch
        {
            return new UserScenarioRegistry();
        }
    }

    private void SaveUserScenarioRegistry(UserScenarioRegistry registry)
    {
        Directory.CreateDirectory(UserScenarioRoot());
        var normalized = new UserScenarioRegistry
        {
            SchemaVersion = registry.SchemaVersion
        };

        foreach (var pair in registry.Scenarios)
        {
            var relativePath = NormalizeRelativePath(string.IsNullOrWhiteSpace(pair.Value.Path) ? pair.Key : pair.Value.Path);
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                continue;
            }

            normalized.Scenarios[relativePath] = new UserScenarioRegistryEntry
            {
                Path = relativePath,
                Name = pair.Value.Name,
                CreatedAtUtc = pair.Value.CreatedAtUtc,
                UpdatedAtUtc = pair.Value.UpdatedAtUtc
            };
        }

        File.WriteAllText(UserScenarioRegistryPath(), JsonSerializer.Serialize(normalized, JsonOptions));
    }

    private static void EnsurePathInside(string path, string root)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.GetFullPath(root);
        if (!fullRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            fullRoot += Path.DirectorySeparatorChar;
        }

        if (!fullPath.StartsWith(fullRoot, comparison))
        {
            throw new InvalidOperationException("Scenario path is outside the managed user scenario folder.");
        }
    }

    private static string NormalizeRelativePath(string path)
    {
        return path
            .Trim()
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);
    }

    private static string GetUniqueArchivePath(string preferredPath)
    {
        if (!File.Exists(preferredPath))
        {
            return preferredPath;
        }

        var directory = Path.GetDirectoryName(preferredPath) ?? ".";
        var fileName = Path.GetFileNameWithoutExtension(preferredPath);
        var extension = Path.GetExtension(preferredPath);
        for (var index = 2; index < 1000; index++)
        {
            var candidate = Path.Combine(directory, $"{fileName}_{index}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException("Could not choose a unique scenario archive path.");
    }

    private ScenarioEditorDefinition BuildScenarioEditor(string resolvedPath)
    {
        var scenario = SimulationScenarioJson.Load(resolvedPath);
        var scenarioJson = SimulationScenarioJson.ToJson(scenario);
        var scenarioObject = JsonNode.Parse(scenarioJson)?.AsObject()
            ?? throw new InvalidOperationException("Scenario JSON did not contain an object.");

        return new ScenarioEditorDefinition(
            Path.GetRelativePath(_repoRoot, resolvedPath),
            scenarioObject,
            ScenarioFieldDefinitions);
    }

    private string ResolveCloneScenarioPath(RunManifest manifest)
    {
        var candidates = new[]
        {
            ResolveArtifactPath(EffectiveResolvedScenarioPath(manifest)),
            ResolveArtifactPath(manifest.LaunchScenarioPath),
            ResolveScenarioPath(manifest.ScenarioPath)
        };

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("No scenario artifact was found for this run.", manifest.Id);
    }

    private string ResolveArtifactPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.IsPathRooted(path) ? Path.GetFullPath(path) : ResolveScenarioPath(path);
    }

    private static ulong? TryReadScenarioSeed(string scenarioPath)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(scenarioPath));
            if (document.RootElement.TryGetProperty("seed", out var seedElement)
                && seedElement.ValueKind == JsonValueKind.Number
                && seedElement.TryGetUInt64(out var seed))
            {
                return seed;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static ulong? TryReadScenarioSeed(JsonElement? scenarioElement)
    {
        if (scenarioElement is null
            || scenarioElement.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        try
        {
            if (TryGetProperty(scenarioElement.Value, "seed", out var seedElement)
                && seedElement.ValueKind == JsonValueKind.Number
                && seedElement.TryGetUInt64(out var seed))
            {
                return seed;
            }
        }
        catch
        {
        }

        return null;
    }

    private (string FileName, IReadOnlyList<string> PrefixArguments) ResolveCliLaunchTarget()
    {
        var configuration = ResolveBuildConfiguration();
        var cliOutputDirectory = Path.Combine(_repoRoot, "src", "Lineage.Cli", "bin", configuration, "net8.0");
        var executablePath = Path.Combine(
            cliOutputDirectory,
            OperatingSystem.IsWindows() ? "Lineage.Cli.exe" : "Lineage.Cli");
        if (File.Exists(executablePath))
        {
            return (executablePath, Array.Empty<string>());
        }

        var dllPath = Path.Combine(cliOutputDirectory, "Lineage.Cli.dll");
        if (File.Exists(dllPath))
        {
            return ("dotnet", [dllPath]);
        }

        throw new InvalidOperationException(
            $"Could not find a built Lineage.Cli executable under {cliOutputDirectory}. Build the solution before launching runs.");
    }

    private IReadOnlyList<string> BuildCliArguments(RunManifest manifest)
    {
        var args = new List<string>();
        if (string.IsNullOrWhiteSpace(manifest.LoadSnapshotPath))
        {
            args.Add("--scenario");
            args.Add(EffectiveCliScenarioPath(manifest));
        }
        else
        {
            args.Add("--load-snapshot");
            args.Add(manifest.LoadSnapshotPath);
        }

        args.AddRange(new[]
        {
            "--save-scenario",
            EffectiveResolvedScenarioPath(manifest),
            "--ticks",
            manifest.Ticks.ToString(CultureInfo.InvariantCulture),
            "--output",
            manifest.StatsPath,
            "--report",
            manifest.ReportPath,
            "--save-snapshot",
            manifest.SnapshotPath,
            "--checkpoint-dir",
            manifest.CheckpointDirectory,
            "--status",
            manifest.StatusPath,
            "--control",
            manifest.ControlPath,
            "--stdout-log",
            manifest.StdoutPath,
            "--stderr-log",
            manifest.StderrPath,
            "--status-interval",
            "100"
        });

        if (manifest.Seed is not null)
        {
            args.Add("--seed");
            args.Add(manifest.Seed.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (manifest.CheckpointIntervalTicks is > 0)
        {
            args.Add("--checkpoint-interval");
            args.Add(manifest.CheckpointIntervalTicks.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (manifest.StopOnExtinction)
        {
            args.Add("--stop-on-extinction");
        }

        return args;
    }

    private static string ResolveBuildConfiguration()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (string.Equals(directory.Name, "Debug", StringComparison.OrdinalIgnoreCase)
                || string.Equals(directory.Name, "Release", StringComparison.OrdinalIgnoreCase))
            {
                return directory.Name;
            }

            directory = directory.Parent;
        }

        return "Debug";
    }

    private static string FormatCommandLine(string fileName, IEnumerable<string> arguments)
    {
        return string.Join(' ', new[] { fileName }.Concat(arguments).Select(QuoteArgument));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Lineage.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate Lineage.slnx from the runner app directory.");
    }

    private static string Slugify(string value)
    {
        var slug = SlugRegex().Replace(value.ToLowerInvariant(), "_").Trim('_');
        return string.IsNullOrWhiteSpace(slug) ? "run" : slug;
    }

    private static string QuoteArgument(string argument)
    {
        return argument.Contains(' ') ? $"\"{argument}\"" : argument;
    }

    private static void AppendDetail(StringBuilder builder, string label, object? value)
    {
        var text = FormatMarkdownValue(value);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        builder.Append("- **").Append(label).Append(":** ").Append(text).AppendLine();
    }

    private static string FormatDate(DateTimeOffset? value)
    {
        return value?.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string FormatDuration(DateTimeOffset? startedAt, DateTimeOffset? endedAt)
    {
        if (startedAt is null)
        {
            return string.Empty;
        }

        var end = endedAt ?? DateTimeOffset.UtcNow;
        var duration = end - startedAt.Value;
        return duration.TotalSeconds < 60
            ? $"{duration.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture)}s"
            : $"{duration.TotalMinutes.ToString("0.0", CultureInfo.InvariantCulture)}m";
    }

    private static string FormatPercent(double value)
    {
        return (Math.Clamp(value, 0d, 1d) * 100d).ToString("0.0", CultureInfo.InvariantCulture) + "%";
    }

    private static string FormatScenarioBrain(RunScenarioSummary? summary)
    {
        if (summary is null)
        {
            return string.Empty;
        }

        var brain = summary.BrainArchitectureKind ?? "unknown";
        var starter = summary.InitialBrainKind ?? "unknown";
        var hidden = summary.BrainHiddenNodeCount is null
            ? string.Empty
            : $", hidden {summary.BrainHiddenNodeCount.Value.ToString(CultureInfo.InvariantCulture)}";
        return $"{brain}, {starter}{hidden}";
    }

    private static string FormatScenarioVision(RunScenarioSummary? summary)
    {
        if (summary is null)
        {
            return string.Empty;
        }

        return string.Join(
            ", ",
            new[]
            {
                summary.EnableSectorVision is null ? null : $"sector {FormatBoolean(summary.EnableSectorVision.Value)}",
                summary.EnableLegacyNearestFoodVisionInputs is null ? null : $"legacy food {FormatBoolean(summary.EnableLegacyNearestFoodVisionInputs.Value)}",
                summary.EnableLegacyNearestCreatureVisionInputs is null ? null : $"legacy creature {FormatBoolean(summary.EnableLegacyNearestCreatureVisionInputs.Value)}",
                summary.VisionAngleDegrees is null ? null : $"vision {summary.VisionAngleDegrees.Value.ToString("0.#", CultureInfo.InvariantCulture)} deg"
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string FormatScenarioWorld(RunScenarioSummary? summary)
    {
        if (summary?.WorldWidth is null || summary.WorldHeight is null)
        {
            return string.Empty;
        }

        return $"{summary.WorldWidth.Value.ToString("0.###", CultureInfo.InvariantCulture)} x {summary.WorldHeight.Value.ToString("0.###", CultureInfo.InvariantCulture)}";
    }

    private static string FormatScenarioDensity(RunScenarioSummary? summary)
    {
        return summary?.InitialResourcesPerMillionArea?.ToString("0.###", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string FormatScenarioResources(RunScenarioSummary? summary)
    {
        if (summary is null)
        {
            return string.Empty;
        }

        return string.Join(
            ", ",
            new[]
            {
                summary.InitialResourcesPerMillionArea is null ? null : $"{summary.InitialResourcesPerMillionArea.Value.ToString("0.###", CultureInfo.InvariantCulture)}/M",
                summary.InitialResourceCount is null ? null : $"{summary.InitialResourceCount.Value.ToString(CultureInfo.InvariantCulture)} initial plants",
                summary.ResourceVoidBorderWidth is null ? null : $"{summary.ResourceVoidBorderWidth.Value.ToString("0.###", CultureInfo.InvariantCulture)} void border",
                summary.InitialCreatureCount is null ? null : $"{summary.InitialCreatureCount.Value.ToString(CultureInfo.InvariantCulture)} starting creatures",
                summary.SpeciesSeedCount == 0 ? null : $"{summary.SpeciesSeedCount.ToString(CultureInfo.InvariantCulture)} species seed(s)"
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string FormatScenarioTerrain(RunScenarioSummary? summary)
    {
        if (summary is null)
        {
            return string.Empty;
        }

        return string.Join(
            ", ",
            new[]
            {
                summary.BiomeMapKind is null ? null : $"biomes {summary.BiomeMapKind}",
                summary.EnableObstacles is null ? null : $"obstacles {FormatBoolean(summary.EnableObstacles.Value)}",
                summary.ObstacleMapKind is null ? null : $"obstacle map {summary.ObstacleMapKind}"
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string FormatScenarioMeat(RunScenarioSummary? summary)
    {
        if (summary is null)
        {
            return string.Empty;
        }

        return string.Join(
            ", ",
            new[]
            {
                summary.DeathMeatCaloriesPerBodyRadius is null ? null : $"death meat/body-radius {summary.DeathMeatCaloriesPerBodyRadius.Value.ToString("0.###", CultureInfo.InvariantCulture)}",
                summary.DeathMeatEnergyFraction is null ? null : $"death energy {FormatPercent(summary.DeathMeatEnergyFraction.Value)}",
                summary.MeatDecayCaloriesPerSecond is null ? null : $"decay {summary.MeatDecayCaloriesPerSecond.Value.ToString("0.###", CultureInfo.InvariantCulture)} kcal/s",
                summary.RottenMeatDamagePerRawKcal is null ? null : $"rot damage {summary.RottenMeatDamagePerRawKcal.Value.ToString("0.###", CultureInfo.InvariantCulture)}"
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "on" : "off";
    }

    private static string MarkdownCell(object? value)
    {
        return FormatMarkdownValue(value)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
    }

    private static void AppendMarkdownTable(
        StringBuilder builder,
        string[] headers,
        string[][] rows,
        bool[] rightAlignedColumns)
    {
        var widths = new int[headers.Length];
        for (var column = 0; column < headers.Length; column++)
        {
            widths[column] = Math.Max(headers[column].Length, rightAlignedColumns[column] ? 4 : 3);
        }

        foreach (var row in rows)
        {
            for (var column = 0; column < headers.Length; column++)
            {
                widths[column] = Math.Max(widths[column], row[column].Length);
            }
        }

        AppendMarkdownTableRow(builder, headers, widths, rightAlignedColumns);
        builder.Append('|');
        for (var column = 0; column < headers.Length; column++)
        {
            var separator = rightAlignedColumns[column]
                ? new string('-', widths[column] - 1) + ":"
                : new string('-', widths[column]);
            builder.Append(' ').Append(separator).Append(" |");
        }

        builder.AppendLine();

        foreach (var row in rows)
        {
            AppendMarkdownTableRow(builder, row, widths, rightAlignedColumns);
        }
    }

    private static void AppendMarkdownTableRow(
        StringBuilder builder,
        string[] cells,
        int[] widths,
        bool[] rightAlignedColumns)
    {
        builder.Append('|');
        for (var column = 0; column < cells.Length; column++)
        {
            var cell = rightAlignedColumns[column]
                ? cells[column].PadLeft(widths[column])
                : cells[column].PadRight(widths[column]);
            builder.Append(' ').Append(cell).Append(" |");
        }

        builder.AppendLine();
    }

    private static string FormatMarkdownValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTimeOffset date => FormatDate(date),
            ulong unsigned => unsigned.ToString(CultureInfo.InvariantCulture),
            int number => number.ToString(CultureInfo.InvariantCulture),
            long number => number.ToString(CultureInfo.InvariantCulture),
            double number => number.ToString("0.###", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
        };
    }

    private static string EscapeMarkdownHeading(string value)
    {
        return value.Replace("#", "\\#", StringComparison.Ordinal).Trim();
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var property)
            ? property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            }
            : null;
    }

    private static bool? GetBoolean(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static int? GetInt32(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt32(out var value)
                ? value
                : null;
    }

    private static ulong? GetUInt64(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetUInt64(out var value)
                ? value
                : null;
    }

    private static double? GetDouble(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetDouble(out var value)
                ? value
                : null;
    }

    private static int CountEnabledSpeciesSeeds(JsonElement element)
    {
        if (!TryGetProperty(element, "speciesSeeds", out var speciesSeeds)
            || speciesSeeds.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        var count = 0;
        foreach (var speciesSeed in speciesSeeds.EnumerateArray())
        {
            if (speciesSeed.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (GetBoolean(speciesSeed, "enabled") != false)
            {
                count++;
            }
        }

        return count;
    }

    private static IReadOnlyList<ScenarioFieldDefinition> BuildScenarioFieldDefinitions()
    {
        return SimulationScenarioMetadata.Fields
            .Select(field => new ScenarioFieldDefinition(
                field.Name,
                field.JsonName,
                field.Label,
                field.Group,
                field.Type,
                field.EnumValues,
                field.Advanced,
                field.Minimum,
                field.Maximum,
                field.Step,
                field.Units,
                field.Description))
            .ToArray();
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var candidate in element.EnumerateObject())
            {
                if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    return true;
                }
            }
        }

        property = default;
        return false;
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex SlugRegex();

    [GeneratedRegex("(?:^|_)tick_(\\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex CheckpointTickRegex();

    private sealed class ManagedRun(RunManifest manifest, Process? process)
    {
        private int _exitRecorded;

        public RunManifest Manifest { get; } = manifest;

        public Process? Process { get; private set; } = process;

        public bool IsRunning => Process is { HasExited: false };

        public void AttachProcess(Process process)
        {
            DisposeProcess();
            Process = process;
            Interlocked.Exchange(ref _exitRecorded, 0);
        }

        public void DisposeProcess()
        {
            Process?.Dispose();
            Process = null;
        }

        public bool TryRecordExit()
        {
            return Interlocked.Exchange(ref _exitRecorded, 1) == 0;
        }
    }

    private sealed class UserScenarioRegistry
    {
        public string SchemaVersion { get; set; } = "lineage.runner.user-scenarios.v1";

        public Dictionary<string, UserScenarioRegistryEntry> Scenarios { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class UserScenarioRegistryEntry
    {
        public string Path { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }
    }

    private sealed class ScenarioRecipeFile
    {
        public string SchemaVersion { get; set; } = "lineage.runner.scenario-recipe.v1";

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

        public JsonObject Changes { get; set; } = new();

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }
    }
}
