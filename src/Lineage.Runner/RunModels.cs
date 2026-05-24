using System.Text.Json.Serialization;

namespace Lineage.Runner;

public sealed record ScenarioOption(string Name, string Path);

public sealed record RunCreateRequest(
    string ScenarioPath,
    int Ticks,
    ulong? Seed,
    int? CheckpointIntervalTicks,
    bool StopOnExtinction);

public sealed record RunCommandRequest(string Command);

public sealed record RunRenameRequest(string Name);

public sealed record RunBulkDeleteRequest(IReadOnlyList<string> Ids);

public sealed record RunBulkDeleteResult(
    int Requested,
    int Deleted,
    IReadOnlyList<string> Skipped);

public sealed record RunDetails(
    RunSummary Run,
    string CommandLine,
    string? Error,
    IReadOnlyList<string> StdoutTail,
    IReadOnlyList<string> StderrTail);

public sealed record RunSummary(
    string Id,
    string Name,
    string Status,
    string ScenarioPath,
    string ScenarioName,
    ulong? Seed,
    int Ticks,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    int? ExitCode,
    int? ProcessId,
    string RunDirectory,
    string StatsPath,
    string ReportPath,
    string SnapshotPath,
    string CheckpointDirectory,
    string StatusPath,
    string ControlPath,
    string StdoutPath,
    string StderrPath,
    long CurrentTick,
    long CompletedSteps,
    double Progress,
    int CreatureCount,
    int EggCount,
    int SpeciesClusterCount,
    int MaxGeneration,
    int CreatureBirthCount,
    int CreatureDeathCount,
    string? StopReason,
    string? LatestCheckpointPath,
    int CheckpointCount,
    bool IsRunning,
    bool HasReport);

public sealed class RunManifest
{
    public string SchemaVersion { get; set; } = "lineage.runner.manifest.v1";

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = "created";

    public string ScenarioPath { get; set; } = string.Empty;

    public string ScenarioName { get; set; } = string.Empty;

    public ulong? Seed { get; set; }

    public int Ticks { get; set; }

    public int? CheckpointIntervalTicks { get; set; }

    public bool StopOnExtinction { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? EndedAtUtc { get; set; }

    public int? ExitCode { get; set; }

    public int? ProcessId { get; set; }

    public string? Error { get; set; }

    public string WorkingDirectory { get; set; } = string.Empty;

    public string CommandLine { get; set; } = string.Empty;

    public string RunDirectory { get; set; } = string.Empty;

    public string StatsPath { get; set; } = string.Empty;

    public string ReportPath { get; set; } = string.Empty;

    public string SnapshotPath { get; set; } = string.Empty;

    public string CheckpointDirectory { get; set; } = string.Empty;

    public string StatusPath { get; set; } = string.Empty;

    public string ControlPath { get; set; } = string.Empty;

    public string StdoutPath { get; set; } = string.Empty;

    public string StderrPath { get; set; } = string.Empty;
}

public sealed class CliStatusFile
{
    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("stopReason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }

    [JsonPropertyName("seed")]
    public ulong Seed { get; set; }

    [JsonPropertyName("requestedTicks")]
    public int RequestedTicks { get; set; }

    [JsonPropertyName("completedSteps")]
    public long CompletedSteps { get; set; }

    [JsonPropertyName("currentTick")]
    public long CurrentTick { get; set; }

    [JsonPropertyName("progress")]
    public double Progress { get; set; }

    [JsonPropertyName("creatureCount")]
    public int CreatureCount { get; set; }

    [JsonPropertyName("eggCount")]
    public int EggCount { get; set; }

    [JsonPropertyName("speciesClusterCount")]
    public int SpeciesClusterCount { get; set; }

    [JsonPropertyName("maxGeneration")]
    public int MaxGeneration { get; set; }

    [JsonPropertyName("creatureBirthCount")]
    public int CreatureBirthCount { get; set; }

    [JsonPropertyName("creatureDeathCount")]
    public int CreatureDeathCount { get; set; }

    [JsonPropertyName("latestCheckpointPath")]
    public string? LatestCheckpointPath { get; set; }

    [JsonPropertyName("checkpointCount")]
    public int CheckpointCount { get; set; }
}
