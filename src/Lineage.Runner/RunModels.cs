using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Lineage.Runner;

public sealed record ScenarioOption(
    string Name,
    string Path,
    bool IsUserCreated,
    bool CanDelete);

public sealed record ScenarioEditorDefinition(
    string ScenarioPath,
    JsonObject Scenario,
    IReadOnlyList<ScenarioFieldDefinition> Fields);

public sealed record ScenarioFieldDefinition(
    string Name,
    string JsonName,
    string Label,
    string Group,
    string Type,
    IReadOnlyList<string> EnumValues,
    bool Advanced,
    double? Minimum,
    double? Maximum,
    double? Step,
    string? Units,
    string? Description);

public sealed record ScenarioSaveRequest(
    string Name,
    JsonElement Scenario);

public sealed record ScenarioSaveResult(
    ScenarioOption Scenario,
    ScenarioEditorDefinition ScenarioEditor);

public sealed record ScenarioDeleteResult(
    string Path,
    string ArchivedPath);

public sealed record BiomeMapPreviewRequest(
    JsonElement Scenario,
    ulong? Seed,
    string? ScenarioPath = null);

public sealed record BiomeMapPreview(
    bool Enabled,
    string MapKind,
    ulong Seed,
    double WorldWidth,
    double WorldHeight,
    double CellSize,
    int CellCountX,
    int CellCountY,
    double ResourceVoidBorderWidth,
    IReadOnlyList<string> Cells,
    bool ObstaclesEnabled,
    string ObstacleMapKind,
    double ObstacleCellSize,
    int ObstacleCellCountX,
    int ObstacleCellCountY,
    int ObstacleBlockedCellCount,
    IReadOnlyList<bool> ObstacleCells,
    IReadOnlyList<BiomeMapPreviewSummary> Biomes);

public sealed record BiomeMapPreviewSummary(
    string Name,
    string Color,
    int CellCount,
    double AreaShare);

public sealed record MapArtifactOption(
    string Name,
    string Path,
    bool CanDelete,
    double WorldWidth,
    double WorldHeight,
    double BiomeCellSize,
    int BiomeCellCountX,
    int BiomeCellCountY,
    double ResourceVoidBorderWidth,
    double ObstacleCellSize,
    int ObstacleCellCountX,
    int ObstacleCellCountY,
    int ObstacleBlockedCellCount,
    ulong? SourceSeed,
    string? SourceBiomeMapKind,
    string? SourceObstacleMapKind,
    IReadOnlyList<BiomeMapPreviewSummary> Biomes);

public sealed record MapArtifactSaveRequest(
    string Name,
    JsonElement Scenario,
    ulong? Seed,
    string? ScenarioPath,
    IReadOnlyList<string>? Cells,
    IReadOnlyList<bool>? ObstacleCells);

public sealed record MapArtifactSaveResult(
    MapArtifactOption Map,
    string WorldMapPath);

public sealed record MapArtifactRenameRequest(
    string Path,
    string Name);

public sealed record MapArtifactDuplicateRequest(
    string Path,
    string Name);

public sealed record MapArtifactDeleteRequest(
    string Path);

public sealed record MapArtifactDeleteResult(
    string Path,
    string ArchivedPath);

public sealed record ScenarioRecipe(
    string Name,
    string Path,
    string Description,
    IReadOnlyList<string> Tags,
    JsonObject Changes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ScenarioRecipeSaveRequest(
    string Name,
    string? Description,
    IReadOnlyList<string>? Tags,
    JsonElement Changes);

public sealed record ScenarioRecipeSaveResult(
    ScenarioRecipe Recipe);

public sealed record ScenarioRecipeArchiveResult(
    string Path,
    string ArchivedPath);

public sealed record ScenarioRecipeDeleteResult(
    string Path);

public sealed record SpeciesCatalogEntry(
    string Name,
    string Path,
    bool CanDelete,
    string Notes,
    string? DefaultBrainPath,
    string BrainArchitectureKind,
    int BrainHiddenNodeCount,
    int BrainWeightCount,
    double BodyRadius,
    double MaxSpeed,
    double SenseRadius,
    double VisionAngleDegrees,
    double BasalEnergyPerSecond,
    double MovementEnergyPerSecond,
    double EatCaloriesPerSecond,
    double ReproductionEnergyThreshold,
    double OffspringEnergyInvestment,
    string SourceScenarioName,
    ulong SourceSeed,
    long SourceTick,
    int SourceCreatureId,
    int SourceFounderId,
    int SourceGeneration,
    DateTimeOffset ExportedAtUtc);

public sealed record BrainCatalogEntry(
    string Name,
    string Path,
    bool CanDelete,
    string Notes,
    string BrainArchitectureKind,
    int InputSchemaVersion,
    int OutputSchemaVersion,
    int InputCount,
    int OutputCount,
    int HiddenNodeCount,
    int WeightCount,
    string SourceScenarioName,
    ulong SourceSeed,
    long SourceTick,
    int SourceCreatureId,
    int SourceFounderId,
    int SourceGeneration,
    DateTimeOffset ExportedAtUtc);

public sealed record SpeciesCatalogExportRequest(
    string Name,
    string? Notes,
    int? CreatureId,
    int? FounderId,
    string? ClusterKey,
    bool ExportPairedBrain);

public sealed record SpeciesCatalogExportResult(
    SpeciesCatalogEntry Species,
    BrainCatalogEntry? Brain = null);

public sealed record BrainCatalogExportRequest(
    string Name,
    string? Notes,
    int? CreatureId);

public sealed record BrainCatalogExportResult(
    BrainCatalogEntry Brain);

public sealed record SpeciesCatalogDeleteRequest(
    string Path);

public sealed record SpeciesCatalogDeleteResult(
    string Path,
    string ArchivedPath);

public sealed record BrainCatalogDeleteRequest(
    string Path);

public sealed record BrainCatalogDeleteResult(
    string Path,
    string ArchivedPath);

public sealed record RunCreateRequest(
    string ScenarioPath,
    int Ticks,
    ulong? Seed,
    int? CheckpointIntervalTicks,
    bool StopOnExtinction,
    JsonElement? Scenario,
    string? LoadSnapshotPath = null);

public sealed record RunCommandRequest(string Command);

public sealed record RunRenameRequest(string Name);

public sealed record RunBulkDeleteRequest(IReadOnlyList<string> Ids);

public sealed record RunExportRequest(IReadOnlyList<string> Ids);

public sealed record RunBulkDeleteResult(
    int Requested,
    int Deleted,
    IReadOnlyList<string> Skipped);

public sealed record RunRerunResult(
    RunSummary Run,
    bool DeletedOriginal);

public sealed record RunContinueRequest(
    int Ticks,
    string? SnapshotPath = null);

public sealed record RunContinueResult(
    RunSummary Run,
    string SnapshotPath);

public sealed record RunDetails(
    RunSummary Run,
    string CommandLine,
    string? Error,
    IReadOnlyList<RunArtifact> Artifacts,
    IReadOnlyList<string> StdoutTail,
    IReadOnlyList<string> StderrTail);

public sealed record RunArtifact(
    string Type,
    string Label,
    string Path,
    bool Exists,
    long? SizeBytes,
    DateTimeOffset? ModifiedAtUtc,
    long? Tick,
    bool IsContinuationSource,
    bool IsLatestCheckpoint);

public sealed record RunCloneSettings(
    string SourceRunId,
    string SourceRunName,
    string ScenarioPath,
    int Ticks,
    ulong? Seed,
    int? CheckpointIntervalTicks,
    bool StopOnExtinction,
    ScenarioEditorDefinition ScenarioEditor);

public sealed record RunScenarioSummary(
    string Path,
    bool IsResolvedSnapshot,
    string? Name,
    ulong? Seed,
    string? PipelineKind,
    string? BrainArchitectureKind,
    string? InitialBrainKind,
    int? BrainHiddenNodeCount,
    bool? EnableSectorVision,
    bool? EnableLegacyNearestFoodVisionInputs,
    bool? EnableLegacyNearestCreatureVisionInputs,
    double? WorldWidth,
    double? WorldHeight,
    int? InitialCreatureCount,
    double? InitialResourcesPerMillionArea,
    int? InitialResourceCount,
    string? BiomeMapKind,
    string? WorldMapPath,
    bool? EnableObstacles,
    string? ObstacleMapKind,
    double? ResourceVoidBorderWidth,
    double? VisionAngleDegrees,
    double? DeathMeatCaloriesPerBodyRadius,
    double? DeathMeatEnergyFraction,
    double? MeatDecayCaloriesPerSecond,
    double? RottenMeatDamagePerRawKcal,
    int SpeciesSeedCount);

public sealed record RunSummary(
    string Id,
    string Name,
    string Status,
    string ScenarioPath,
    string ScenarioName,
    string LaunchScenarioPath,
    string ResolvedScenarioPath,
    RunScenarioSummary? ScenarioSummary,
    ulong? Seed,
    int Ticks,
    int? CheckpointIntervalTicks,
    bool StopOnExtinction,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    int? ExitCode,
    int? ProcessId,
    string? FailureReason,
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
    long ArtifactSizeBytes,
    int ArtifactFileCount,
    bool IsRunning,
    bool HasReport);

public sealed class RunManifest
{
    public string SchemaVersion { get; set; } = "lineage.runner.manifest.v1";

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = "created";

    public string ScenarioPath { get; set; } = string.Empty;

    public string LoadSnapshotPath { get; set; } = string.Empty;

    public string ScenarioName { get; set; } = string.Empty;

    public string LaunchScenarioPath { get; set; } = string.Empty;

    public string ResolvedScenarioPath { get; set; } = string.Empty;

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
