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
    IReadOnlyList<BiomeMapPreviewSummary> Biomes,
    IReadOnlyList<MapArtifactObstacleGroupCells> ObstacleGroups);

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
    IReadOnlyList<BiomeMapPreviewSummary> Biomes,
    IReadOnlyList<MapArtifactObstacleGroupSummary> ObstacleGroups);

public sealed record MapArtifactObstacleGroupSummary(
    string Id,
    string Name,
    bool DefaultBlocked,
    int CellCount);

public sealed record MapArtifactObstacleGroupCells(
    string Id,
    string Name,
    bool DefaultBlocked,
    IReadOnlyList<int> Cells);

public sealed record MapArtifactSaveRequest(
    string Name,
    JsonElement Scenario,
    ulong? Seed,
    string? ScenarioPath,
    IReadOnlyList<string>? Cells,
    IReadOnlyList<bool>? ObstacleCells,
    IReadOnlyList<MapArtifactObstacleGroupCells>? ObstacleGroups);

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
    string? SourceInitialBrainKind,
    string SourceScenarioName,
    ulong SourceSeed,
    long SourceTick,
    int SourceCreatureId,
    int SourceFounderId,
    int SourceGeneration,
    DateTimeOffset ExportedAtUtc,
    bool IsCompatible,
    bool RequiresNormalization,
    string CompatibilityStatus,
    IReadOnlyList<string> CompatibilityWarnings);

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

public sealed record BrainLabSnapshotOption(
    string Name,
    string Path,
    long SizeBytes,
    DateTimeOffset ModifiedAtUtc);

public sealed record BrainLabSnapshotDetails(
    string Path,
    string ScenarioName,
    ulong Seed,
    long Tick,
    double ElapsedSeconds,
    int CreatureCount,
    int ReturnedCreatureCount,
    bool CreatureListTruncated,
    IReadOnlyList<BrainLabCreatureOption> Creatures);

public sealed record BrainLabCreatureOption(
    int Id,
    int Generation,
    int BrainId,
    int GenomeId,
    string BrainArchitectureKind,
    double AgeSeconds,
    double EnergyRatio,
    double HealthRatio,
    double Hunger,
    bool HeardSound,
    double SoundDensity,
    double SoundAmplitude);

public sealed record BrainLabEvaluateRequest(
    string SnapshotPath,
    int CreatureId,
    Dictionary<string, float>? InputOverrides,
    BrainLabWorldProbeEditSet? WorldProbe = null,
    BrainLabWorldProbeEnvironment? WorldProbeEnvironment = null);

public sealed record BrainLabPopulationEvaluateRequest(
    string SnapshotPath,
    Dictionary<string, float>? InputOverrides,
    int? MaxCreatures);

public sealed record BrainLabPresetMatrixRequest(
    string SnapshotPath,
    int? MaxCreatures);

public sealed record BrainLabBehaviorProfileComparisonRequest(
    string SnapshotPath,
    Dictionary<string, float>? InputOverrides,
    BrainLabWorldProbeEnvironment? WorldProbeEnvironment = null,
    IReadOnlyList<string>? FixturePaths = null,
    int? MaxFixtures = null,
    int? MaxCreatures = null,
    int? CreatureOffset = null);

public sealed record BrainLabPresetMatrixResult(
    string SnapshotPath,
    int TotalCreatureCount,
    int MaxCreatures,
    IReadOnlyList<BrainLabPresetMatrixRow> Rows);

public sealed record BrainLabPresetMatrixRow(
    string Key,
    string Name,
    int OverrideCount,
    int EvaluatedCreatureCount,
    int SkippedCreatureCount,
    int UnsupportedOverrideCreatureCount,
    int ChangedCreatureCount,
    double ChangedCreatureShare,
    int GateFlipCreatureCount,
    double GateFlipCreatureShare,
    double MaxAbsoluteOutputDelta,
    IReadOnlyList<BrainLabPresetMatrixOutput> TopOutputs);

public sealed record BrainLabPresetMatrixOutput(
    string Key,
    string Name,
    double MeanAbsoluteDelta,
    int ChangedCreatureCount,
    double ChangedCreatureShare,
    int GateFlipCount,
    double GateFlipShare);

public sealed record BrainLabProbeTestRequest(
    string SnapshotPath,
    int CreatureId,
    Dictionary<string, float>? InputOverrides,
    BrainLabWorldProbeEnvironment? WorldProbeEnvironment = null,
    IReadOnlyList<string>? FixturePaths = null,
    int? MaxFixtures = null);

public sealed record BrainLabSpeciesCatalogExportRequest(
    string SnapshotPath,
    string Name,
    string? Notes,
    int CreatureId,
    bool ExportPairedBrain);

public sealed record BrainLabBrainCatalogExportRequest(
    string SnapshotPath,
    string Name,
    string? Notes,
    int CreatureId);

public sealed record BrainLabProbeTestResult(
    string SnapshotPath,
    int CreatureId,
    string BrainArchitectureKind,
    int TotalFixtureCount,
    int EvaluatedFixtureCount,
    int SkippedFixtureCount,
    BrainLabBehaviorProfile Profile,
    IReadOnlyList<BrainLabBehaviorFingerprint> Fingerprints,
    IReadOnlyList<BrainLabProbeTestRow> Rows);

public sealed record BrainLabProbeTestRow(
    string Path,
    string Name,
    bool IsBuiltIn,
    IReadOnlyList<string> Tags,
    int OverrideCount,
    int ChangedOutputCount,
    int GateFlipCount,
    double MaxAbsoluteOutputDelta,
    IReadOnlyList<BrainLabBehaviorLabel> Labels,
    IReadOnlyList<BrainLabProbeTestOutput> TopOutputs);

public sealed record BrainLabBehaviorProfileComparisonResult(
    string SnapshotPath,
    int TotalCreatureCount,
    int CreatureOffset,
    int MaxCreatures,
    int EvaluatedCreatureCount,
    int SkippedCreatureCount,
    int TotalFixtureCount,
    int EvaluatedFixtureCount,
    int SkippedFixtureCount,
    IReadOnlyList<BrainLabBehaviorProfileCohort> Cohorts,
    IReadOnlyList<BrainLabBehaviorProfileComparisonRow> Rows);

public sealed record BrainLabBehaviorProfileCohort(
    string Key,
    string Name,
    string Summary,
    int CreatureCount,
    int RepresentativeCreatureId,
    IReadOnlyList<int> CreatureIds,
    IReadOnlyList<string> Traits,
    IReadOnlyList<string> Fingerprints);

public sealed record BrainLabBehaviorProfileComparisonRow(
    int CreatureId,
    int Generation,
    int BrainId,
    int GenomeId,
    string BrainArchitectureKind,
    double AgeSeconds,
    double EnergyRatio,
    double HealthRatio,
    double Hunger,
    BrainLabBehaviorProfile Profile,
    IReadOnlyList<BrainLabBehaviorFingerprint> Fingerprints,
    string CohortKey,
    IReadOnlyList<string> CohortTraits,
    IReadOnlyList<string> CohortFingerprints);

public sealed record BrainLabBehaviorLabel(
    string Key,
    string Name,
    string Category,
    double Strength);

public sealed record BrainLabBehaviorFingerprint(
    string Key,
    string Name,
    string Description,
    int Score,
    IReadOnlyList<string> Evidence);

public sealed record BrainLabBehaviorProfile(
    string Summary,
    IReadOnlyList<BrainLabBehaviorProfileSection> Sections);

public sealed record BrainLabBehaviorProfileSection(
    string Key,
    string Name,
    string Summary,
    IReadOnlyList<string> Traits,
    IReadOnlyList<string> Evidence);

public sealed record BrainLabProbeTestOutput(
    string Key,
    string Name,
    double BaselineValue,
    double ModifiedValue,
    double Delta,
    bool Changed,
    bool? BaselineActive,
    bool? ModifiedActive);

public sealed record BrainLabWorldProbeRequest(
    string SnapshotPath,
    int CreatureId);

public sealed record BrainLabWorldProbeFixture(
    string Name,
    string Path,
    bool IsBuiltIn,
    bool CanDelete,
    string Description,
    IReadOnlyList<string> Tags,
    BrainLabWorldProbeEditSet WorldProbe,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record BrainLabWorldProbeFixtureSaveRequest(
    string Name,
    string? Description,
    IReadOnlyList<string>? Tags,
    BrainLabWorldProbeEditSet WorldProbe);

public sealed record BrainLabWorldProbeFixtureSaveResult(
    BrainLabWorldProbeFixture Fixture);

public sealed record BrainLabWorldProbeFixtureArchiveResult(
    string Path,
    string ArchivedPath);

public sealed record BrainLabWorldProbeEnvironment(
    string? BiomeKind,
    double? LocalFertility,
    string? ObstacleMode,
    BrainLabWorldProbeBiomeBoundary? BiomeBoundary = null);

public sealed record BrainLabWorldProbeBiomeBoundary(
    string? Direction,
    string? NearBiomeKind,
    string? FarBiomeKind,
    double? Offset);

public sealed record BrainLabWorldProbeEditSet(
    IReadOnlyList<BrainLabWorldProbeEditedResource>? Resources,
    IReadOnlyList<BrainLabWorldProbeEditedEgg>? Eggs,
    IReadOnlyList<BrainLabWorldProbeEditedCreature>? Creatures,
    IReadOnlyList<BrainLabWorldProbeEditedSmallPrey>? SmallPrey = null);

public sealed record BrainLabWorldProbeEditedResource(
    int Id,
    string Kind,
    string? PlantKind,
    double X,
    double Y,
    double Radius,
    double Calories,
    double MaxCalories,
    double Freshness);

public sealed record BrainLabWorldProbeEditedEgg(
    int Id,
    int Generation,
    double X,
    double Y,
    double Radius,
    double Energy,
    double Health);

public sealed record BrainLabWorldProbeEditedCreature(
    int Id,
    int Generation,
    string? BrainArchitectureKind,
    double X,
    double Y,
    double Radius,
    double HeadingRadians,
    double EnergyRatio,
    double HealthRatio,
    double Hunger,
    double SoundAmplitude,
    double SoundTone,
    bool IsProbeSoundOnly = false);

public sealed record BrainLabWorldProbeEditedSmallPrey(
    int Id,
    double X,
    double Y,
    double Radius,
    double Calories,
    double MaxCalories,
    double Health,
    double MaxHealth,
    double HeadingRadians,
    double Speed,
    double GrabPressure);

public sealed record BrainLabWorldProbeScene(
    string SnapshotPath,
    int CreatureId,
    double ProbeRadius,
    double SenseRadius,
    double SoundRadius,
    double WorldWidth,
    double WorldHeight,
    bool Truncated,
    BrainLabWorldProbeEnvironmentSample Environment,
    BrainLabWorldProbeCounts Counts,
    BrainLabWorldProbeCreature Focus,
    IReadOnlyList<BrainLabWorldProbeResource> Resources,
    IReadOnlyList<BrainLabWorldProbeEgg> Eggs,
    IReadOnlyList<BrainLabWorldProbeSmallPrey> SmallPrey,
    IReadOnlyList<BrainLabWorldProbeCreature> Creatures);

public sealed record BrainLabWorldProbeEnvironmentSample(
    string CurrentBiomeKind,
    string ForwardBiomeKind,
    string LeftBiomeKind,
    string RightBiomeKind,
    double LocalFertility,
    bool CurrentObstacleBlocked,
    int ObstacleBlockedCellCount);

public sealed record BrainLabWorldProbeCounts(
    int PlantCount,
    int MeatCount,
    int EggCount,
    int SmallPreyCount,
    int CreatureCount,
    int SoundSourceCount,
    int ReturnedResourceCount,
    int ReturnedEggCount,
    int ReturnedSmallPreyCount,
    int ReturnedCreatureCount);

public sealed record BrainLabWorldProbeResource(
    int Id,
    string Kind,
    string PlantKind,
    double X,
    double Y,
    double Distance,
    double Radius,
    double Calories,
    double MaxCalories,
    double Freshness);

public sealed record BrainLabWorldProbeEgg(
    int Id,
    int Generation,
    double X,
    double Y,
    double Distance,
    double Radius,
    double Energy,
    double Health);

public sealed record BrainLabWorldProbeSmallPrey(
    int Id,
    double X,
    double Y,
    double Distance,
    double Radius,
    double Calories,
    double MaxCalories,
    double Health,
    double MaxHealth,
    double HeadingRadians,
    double Speed,
    double AgeSeconds,
    bool IsHeld,
    double GrabPressure);

public sealed record BrainLabWorldProbeCreature(
    int Id,
    int Generation,
    string BrainArchitectureKind,
    double X,
    double Y,
    double Distance,
    double Radius,
    double HeadingRadians,
    double VisionAngleRadians,
    double EnergyRatio,
    double HealthRatio,
    double Hunger,
    double SoundAmplitude,
    double SoundTone,
    bool HeardSound,
    double SoundDensity,
    bool IsFocus);

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
    double? StaleMeatDecayMultiplier,
    double? RottenMeatDamagePerRawKcal,
    int SpeciesSeedCount,
    IReadOnlyList<RunScenarioSpeciesSeedSummary> SpeciesSeeds);

public sealed record RunScenarioSpeciesSeedSummary(
    string? Label,
    string? Tag,
    string ProfilePath,
    string ProfileName,
    int Count,
    string SpawnRegion,
    double? EnergyOverride,
    bool Enabled,
    string Brain,
    string? BrainProfilePath);

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
