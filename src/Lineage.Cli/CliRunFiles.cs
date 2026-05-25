using System.Text.Json;
using Lineage.Core;

internal sealed class CliRunFiles
{
    private const int AtomicJsonWriteMaxAttempts = 10;
    private const int AtomicJsonWriteRetryDelayMilliseconds = 25;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly RunOptions _options;
    private readonly SimulationScenario _scenario;
    private readonly OutputPaths _outputPaths;
    private bool _statusWriteWarningEmitted;

    public CliRunFiles(RunOptions options, SimulationScenario scenario, OutputPaths outputPaths)
    {
        _options = options;
        _scenario = scenario;
        _outputPaths = outputPaths;
    }

    public bool HasStatus => !string.IsNullOrWhiteSpace(_options.StatusPath);

    public bool HasControl => !string.IsNullOrWhiteSpace(_options.ControlPath);

    public bool RequiresStepLoop => HasStatus || HasControl || _options.StopOnExtinction;

    public bool ShouldWriteStatus(long completedSteps)
    {
        if (!HasStatus)
        {
            return false;
        }

        var interval = Math.Max(1, _options.StatusIntervalTicks);
        return completedSteps == 0 || completedSteps % interval == 0;
    }

    public void WriteStatus(
        string state,
        Simulation simulation,
        long completedSteps,
        IReadOnlyList<CheckpointArtifact> checkpoints,
        string? stopReason = null,
        string? message = null)
    {
        if (string.IsNullOrWhiteSpace(_options.StatusPath))
        {
            return;
        }

        var world = simulation.State;
        var stats = world.Stats;
        var maxGeneration = 0;
        for (var i = 0; i < world.Creatures.Count; i++)
        {
            maxGeneration = Math.Max(maxGeneration, world.Creatures[i].Generation);
        }

        var speciesClusterCount = 0;
        if (world.Creatures.Count > 0)
        {
            speciesClusterCount = SpeciesClusterAnalyzer.Analyze(world).Count;
        }

        var progress = _options.Ticks <= 0
            ? 1d
            : Math.Clamp(completedSteps / (double)_options.Ticks, 0d, 1d);
        var latestCheckpoint = checkpoints.Count == 0 ? null : checkpoints[^1].Path;
        var status = new CliRunStatusFile(
            SchemaVersion: "lineage.run.status.v1",
            State: state,
            StopReason: stopReason,
            Message: message,
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            ScenarioName: _scenario.Name,
            ScenarioPath: _options.ScenarioPath,
            LoadSnapshotPath: _options.LoadSnapshotPath,
            Seed: _scenario.Seed,
            RequestedTicks: _options.Ticks,
            CompletedSteps: completedSteps,
            CurrentTick: world.Tick,
            ElapsedSeconds: world.ElapsedSeconds,
            Progress: progress,
            CreatureCount: world.Creatures.Count,
            EggCount: world.Eggs.Count,
            SpeciesClusterCount: speciesClusterCount,
            ResourceCount: world.Resources.Count,
            MaxGeneration: maxGeneration,
            CreatureBirthCount: stats.CreatureBirthCount,
            CreatureDeathCount: stats.CreatureDeathCount,
            StarvationDeathCount: stats.StarvationDeathCount,
            InjuryDeathCount: stats.InjuryDeathCount,
            EggLaidCount: stats.EggLaidCount,
            EggHatchedCount: stats.EggHatchedCount,
            EggDeathCount: stats.EggDeathCount,
            EggPredationDeathCount: stats.EggPredationDeathCount,
            StatsPath: _outputPaths.StatsPath,
            ReportPath: _outputPaths.ReportPath,
            SaveSnapshotPath: _options.SaveSnapshotPath,
            CheckpointDirectory: ResolveCheckpointDirectory(),
            LatestCheckpointPath: latestCheckpoint,
            CheckpointCount: checkpoints.Count);

        try
        {
            WriteAtomicJson(_options.StatusPath, status);
            _statusWriteWarningEmitted = false;
        }
        catch (Exception ex) when (IsRetryableAtomicWriteException(ex))
        {
            if (!_statusWriteWarningEmitted)
            {
                Console.Error.WriteLine($"Warning: could not update status file '{Path.GetFullPath(_options.StatusPath)}': {ex.Message}");
                _statusWriteWarningEmitted = true;
            }
        }
    }

    public CliRunControlCommand ReadControlCommand()
    {
        if (string.IsNullOrWhiteSpace(_options.ControlPath) || !File.Exists(_options.ControlPath))
        {
            return CliRunControlCommand.None;
        }

        try
        {
            var json = File.ReadAllText(_options.ControlPath);
            var commandFile = JsonSerializer.Deserialize<CliRunControlFile>(json, JsonOptions);
            TryDelete(_options.ControlPath);
            return CliRunControlCommand.Parse(commandFile?.Command);
        }
        catch
        {
            return CliRunControlCommand.None;
        }
    }

    public CheckpointArtifact SaveCheckpoint(SimulationScenario scenario, Simulation simulation, string prefix)
    {
        var directory = ResolveCheckpointDirectory();
        Directory.CreateDirectory(directory);

        var tick = simulation.State.Tick;
        var safePrefix = string.IsNullOrWhiteSpace(prefix) ? "checkpoint" : prefix;
        var path = Path.Combine(directory, $"{safePrefix}_tick_{tick:D10}.json");
        if (File.Exists(path))
        {
            path = Path.Combine(directory, $"{safePrefix}_tick_{tick:D10}_{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.json");
        }

        SimulationSnapshotJson.Save(path, SimulationSnapshot.Capture(scenario, simulation));
        return new CheckpointArtifact(tick, path);
    }

    private string ResolveCheckpointDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_outputPaths.CheckpointDirectory))
        {
            return _outputPaths.CheckpointDirectory;
        }

        if (!string.IsNullOrWhiteSpace(_options.CheckpointDirectory))
        {
            return _options.CheckpointDirectory;
        }

        if (!string.IsNullOrWhiteSpace(_options.SaveSnapshotPath))
        {
            return AddDirectorySuffix(_options.SaveSnapshotPath, "checkpoints");
        }

        if (!string.IsNullOrWhiteSpace(_outputPaths.ReportPath))
        {
            return AddDirectorySuffix(_outputPaths.ReportPath, "checkpoints");
        }

        if (!string.IsNullOrWhiteSpace(_outputPaths.StatsPath))
        {
            return AddDirectorySuffix(_outputPaths.StatsPath, "checkpoints");
        }

        return Path.Combine("out", $"lineage_run_{_scenario.Seed}_checkpoints");
    }

    private static string AddDirectorySuffix(string path, string suffix)
    {
        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileNameWithoutExtension(path);
        return Path.Combine(
            string.IsNullOrWhiteSpace(directory) ? string.Empty : directory,
            $"{fileName}_{suffix}");
    }

    private static void WriteAtomicJson<T>(string path, T value)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{path}.tmp";
        var json = JsonSerializer.Serialize(value, JsonOptions);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, path, overwrite: true);
                return;
            }
            catch (Exception ex) when (IsRetryableAtomicWriteException(ex) && attempt < AtomicJsonWriteMaxAttempts)
            {
                Thread.Sleep(AtomicJsonWriteRetryDelayMilliseconds * attempt);
            }
        }
    }

    private static bool IsRetryableAtomicWriteException(Exception ex)
    {
        return ex is IOException or UnauthorizedAccessException;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // A stale control file should not break the simulation run.
        }
    }
}

internal readonly record struct CliRunControlCommand(CliRunControlKind Kind)
{
    public static CliRunControlCommand None { get; } = new(CliRunControlKind.None);

    public bool RequestsCheckpoint => Kind is CliRunControlKind.Checkpoint or CliRunControlKind.CheckpointAndStop;

    public bool RequestsStop => Kind is CliRunControlKind.Stop or CliRunControlKind.CheckpointAndStop;

    public static CliRunControlCommand Parse(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "stop" => new CliRunControlCommand(CliRunControlKind.Stop),
            "checkpoint" or "checkpoint-now" => new CliRunControlCommand(CliRunControlKind.Checkpoint),
            "checkpoint-and-stop" or "snapshot-and-stop" => new CliRunControlCommand(CliRunControlKind.CheckpointAndStop),
            _ => None
        };
    }
}

internal enum CliRunControlKind
{
    None,
    Stop,
    Checkpoint,
    CheckpointAndStop
}

internal sealed record CliRunControlFile(string? Command);

internal sealed record CliRunStatusFile(
    string SchemaVersion,
    string State,
    string? StopReason,
    string? Message,
    DateTimeOffset UpdatedAtUtc,
    string ScenarioName,
    string? ScenarioPath,
    string? LoadSnapshotPath,
    ulong Seed,
    int RequestedTicks,
    long CompletedSteps,
    long CurrentTick,
    double ElapsedSeconds,
    double Progress,
    int CreatureCount,
    int EggCount,
    int SpeciesClusterCount,
    int ResourceCount,
    int MaxGeneration,
    int CreatureBirthCount,
    int CreatureDeathCount,
    int StarvationDeathCount,
    int InjuryDeathCount,
    int EggLaidCount,
    int EggHatchedCount,
    int EggDeathCount,
    int EggPredationDeathCount,
    string? StatsPath,
    string? ReportPath,
    string? SaveSnapshotPath,
    string CheckpointDirectory,
    string? LatestCheckpointPath,
    int CheckpointCount);

internal readonly record struct SimulationRunResult(
    IReadOnlyList<CheckpointArtifact> Checkpoints,
    long CompletedSteps,
    string? StopReason);
