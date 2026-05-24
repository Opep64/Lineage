using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Lineage.Runner;

public sealed partial class LineageRunManager
{
    private const string ProcessIdToken = "{pid}";

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
        var id = $"{createdAt:yyyyMMdd_HHmmss}_{Slugify(scenarioName)}_{ProcessIdToken}";
        var runDirectory = Path.Combine(_runsRoot, id);

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

        var managedRun = new ManagedRun(manifest, process);
        try
        {
            process.Start();
            ApplyProcessId(manifest, process.Id);
            manifest.Status = "running";
            manifest.CommandLine = FormatCommandLine(
                launchTarget.FileName,
                launchTarget.PrefixArguments.Concat(BuildCliArguments(manifest)));
            if (!_runs.TryAdd(manifest.Id, managedRun))
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
            Seed: status?.Seed ?? manifest.Seed,
            Ticks: manifest.Ticks,
            CreatedAtUtc: manifest.CreatedAtUtc,
            StartedAtUtc: manifest.StartedAtUtc,
            EndedAtUtc: manifest.EndedAtUtc,
            ExitCode: manifest.ExitCode,
            ProcessId: manifest.ProcessId,
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

    private static void ApplyProcessId(RunManifest manifest, int processId)
    {
        var processIdText = processId.ToString(CultureInfo.InvariantCulture);
        manifest.ProcessId = processId;
        manifest.Id = ReplaceProcessIdToken(manifest.Id, processIdText);
        manifest.RunDirectory = ReplaceProcessIdToken(manifest.RunDirectory, processIdText);
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

    private string ResolveScenarioPath(string scenarioPath)
    {
        var path = Path.IsPathRooted(scenarioPath)
            ? scenarioPath
            : Path.Combine(_repoRoot, scenarioPath);
        return Path.GetFullPath(path);
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
        var args = new List<string>
        {
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
            "--stdout-log",
            manifest.StdoutPath,
            "--stderr-log",
            manifest.StderrPath,
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

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex SlugRegex();

    private sealed class ManagedRun(RunManifest manifest, Process? process)
    {
        private int _exitRecorded;

        public RunManifest Manifest { get; } = manifest;

        public Process? Process { get; private set; } = process;

        public bool IsRunning => Process is { HasExited: false };

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
}
