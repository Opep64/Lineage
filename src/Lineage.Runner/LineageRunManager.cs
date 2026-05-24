using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Lineage.Runner;

public sealed partial class LineageRunManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

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

        return Directory
            .EnumerateFiles(scenarioRoot, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new ScenarioOption(Path.GetFileNameWithoutExtension(path), Path.GetRelativePath(_repoRoot, path)))
            .ToArray();
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
        var id = $"{createdAt:yyyyMMdd_HHmmss}_{Slugify(scenarioName)}_{Random.Shared.Next(1000, 9999)}";
        var runDirectory = Path.Combine(_runsRoot, id);
        Directory.CreateDirectory(runDirectory);

        var manifest = new RunManifest
        {
            Id = id,
            Name = $"{scenarioName} {createdAt:yyyy-MM-dd HH:mm:ss}",
            Status = "starting",
            ScenarioPath = Path.GetRelativePath(_repoRoot, scenarioPath),
            ScenarioName = scenarioName,
            Seed = request.Seed,
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

        var arguments = BuildCliArguments(manifest);
        manifest.CommandLine = $"dotnet {string.Join(' ', arguments.Select(QuoteArgument))}";
        SaveManifest(manifest);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = _repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        var managedRun = new ManagedRun(manifest, process);
        if (!_runs.TryAdd(manifest.Id, managedRun))
        {
            throw new InvalidOperationException($"Run id collision for {manifest.Id}.");
        }

        try
        {
            process.Exited += (_, _) => MarkProcessExited(managedRun);
            process.Start();
            manifest.Status = "running";
            SaveManifest(manifest);
            _ = PumpOutputAsync(process.StandardOutput, manifest.StdoutPath);
            _ = PumpOutputAsync(process.StandardError, manifest.StderrPath);
            return ToSummary(managedRun);
        }
        catch
        {
            _runs.TryRemove(manifest.Id, out _);
            throw;
        }
    }

    public bool SendControl(string id, string command)
    {
        if (!_runs.TryGetValue(id, out var run) || !run.IsRunning)
        {
            return false;
        }

        var request = new RunCommandRequest(command);
        File.WriteAllText(run.Manifest.ControlPath, JsonSerializer.Serialize(request, JsonOptions));
        return true;
    }

    public bool DeleteRun(string id, bool deleteArtifacts)
    {
        if (!_runs.TryGetValue(id, out var run))
        {
            return false;
        }

        if (run.IsRunning)
        {
            return false;
        }

        _runs.TryRemove(id, out _);
        if (deleteArtifacts && Directory.Exists(run.Manifest.RunDirectory))
        {
            Directory.Delete(run.Manifest.RunDirectory, recursive: true);
        }

        return true;
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
            Seed: status?.Seed ?? manifest.Seed,
            Ticks: manifest.Ticks,
            CreatedAtUtc: manifest.CreatedAtUtc,
            StartedAtUtc: manifest.StartedAtUtc,
            EndedAtUtc: manifest.EndedAtUtc,
            ExitCode: manifest.ExitCode,
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

    private void MarkProcessExited(ManagedRun run)
    {
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

                if (manifest.Status is "running" or "starting")
                {
                    manifest.Status = "unknown";
                    manifest.EndedAtUtc ??= DateTimeOffset.UtcNow;
                    SaveManifest(manifest);
                }

                _runs.TryAdd(manifest.Id, new ManagedRun(manifest, null));
            }
            catch
            {
                // Ignore malformed imported manifests for now; the library can gain diagnostics later.
            }
        }
    }

    private static async Task PumpOutputAsync(StreamReader reader, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        await using var writer = new StreamWriter(stream);
        while (await reader.ReadLineAsync() is { } line)
        {
            await writer.WriteLineAsync(line);
            await writer.FlushAsync();
        }
    }

    private static CliStatusFile? ReadStatus(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<CliStatusFile>(File.ReadAllText(path), JsonOptions);
        }
        catch
        {
            return null;
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

    private string ResolveScenarioPath(string scenarioPath)
    {
        var path = Path.IsPathRooted(scenarioPath)
            ? scenarioPath
            : Path.Combine(_repoRoot, scenarioPath);
        return Path.GetFullPath(path);
    }

    private IReadOnlyList<string> BuildCliArguments(RunManifest manifest)
    {
        var args = new List<string>
        {
            "run",
            "--project",
            Path.Combine(_repoRoot, "src", "Lineage.Cli", "Lineage.Cli.csproj"),
            "--",
            "--scenario",
            manifest.ScenarioPath,
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
            "--status-interval",
            "100"
        };

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

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex SlugRegex();

    private sealed class ManagedRun(RunManifest manifest, Process? process)
    {
        public RunManifest Manifest { get; } = manifest;

        public Process? Process { get; private set; } = process;

        public bool IsRunning => Process is { HasExited: false };

        public void DisposeProcess()
        {
            Process?.Dispose();
            Process = null;
        }
    }
}
