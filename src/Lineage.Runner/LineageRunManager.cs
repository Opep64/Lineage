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
    private const int CliStatusIntervalTicks = 100;
    private const int CliStatusDetailIntervalTicks = 5000;
    private const int MaxBiomePreviewCells = 40_000;

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

    public IReadOnlyList<MapArtifactOption> ListMapArtifacts()
    {
        var mapRoot = MapArtifactRoot();
        if (!Directory.Exists(mapRoot))
        {
            return Array.Empty<MapArtifactOption>();
        }

        return Directory
            .EnumerateFiles(mapRoot, "*.lineage-map.json", SearchOption.AllDirectories)
            .Select(TryReadMapArtifact)
            .OfType<MapArtifactOption>()
            .OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(map => map.Path, StringComparer.OrdinalIgnoreCase)
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

    public IReadOnlyList<SpeciesCatalogEntry> ListSpeciesCatalog()
    {
        var speciesRoot = SpeciesCatalogRoot();
        if (!Directory.Exists(speciesRoot))
        {
            return Array.Empty<SpeciesCatalogEntry>();
        }

        return Directory
            .EnumerateFiles(speciesRoot, SpeciesProfileJson.FilePattern, SearchOption.AllDirectories)
            .Select(TryReadSpeciesCatalogEntry)
            .OfType<SpeciesCatalogEntry>()
            .OrderBy(species => species.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(species => species.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<BrainCatalogEntry> ListBrainCatalog()
    {
        var brainRoot = BrainCatalogRoot();
        if (!Directory.Exists(brainRoot))
        {
            return Array.Empty<BrainCatalogEntry>();
        }

        return Directory
            .EnumerateFiles(brainRoot, BrainProfileJson.FilePattern, SearchOption.AllDirectories)
            .Select(TryReadBrainCatalogEntry)
            .OfType<BrainCatalogEntry>()
            .OrderBy(brain => brain.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(brain => brain.Path, StringComparer.OrdinalIgnoreCase)
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

    public BiomeMapPreview GetBiomeMapPreview(BiomeMapPreviewRequest request)
    {
        if (request.Scenario.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            throw new ArgumentException("Scenario JSON is required.");
        }

        var scenario = SimulationScenarioJson.FromJson(request.Scenario.GetRawText());
        if (request.Seed is { } seed)
        {
            scenario = scenario with { Seed = seed };
        }

        scenario = scenario.Validated();
        var scenarioDirectory = ResolveScenarioDirectory(request.ScenarioPath);
        var map = SimulationScenarioFactory.CreateBiomeMap(
            scenario,
            scenarioDirectory);
        var obstacleMap = SimulationScenarioFactory.CreateObstacleMap(scenario, scenarioDirectory);
        var previewCellCount = (long)map.CellCountX * map.CellCountY;
        if (previewCellCount > MaxBiomePreviewCells)
        {
            throw new ArgumentException(
                $"Biome preview would contain {previewCellCount.ToString("N0", CultureInfo.InvariantCulture)} cells. Increase biomeCellSize or reduce world size for launcher preview.");
        }

        var obstaclePreviewCellCount = (long)obstacleMap.CellCountX * obstacleMap.CellCountY;
        if (obstaclePreviewCellCount > MaxBiomePreviewCells)
        {
            throw new ArgumentException(
                $"Obstacle preview would contain {obstaclePreviewCellCount.ToString("N0", CultureInfo.InvariantCulture)} cells. Increase obstacleCellSize or reduce world size for launcher preview.");
        }

        var cells = map.GetCellsCopy()
            .Select(FormatBiomeKind)
            .ToArray();
        var areaByBiome = BiomeKinds.All.ToDictionary(
            static biome => biome,
            static _ => 0f);
        var countByBiome = BiomeKinds.All.ToDictionary(
            static biome => biome,
            static _ => 0);
        for (var y = 0; y < map.CellCountY; y++)
        {
            for (var x = 0; x < map.CellCountX; x++)
            {
                var kind = BiomeKinds.Canonicalize(map.GetKind(x, y));
                countByBiome[kind]++;
                areaByBiome[kind] += map.GetCellBounds(x, y).Area;
            }
        }

        var worldArea = MathF.Max(1f, map.Bounds.Width * map.Bounds.Height);
        var biomes = BiomeKinds.All
            .Select(biome => new BiomeMapPreviewSummary(
                FormatBiomeKind(biome),
                BiomeColor(biome),
                countByBiome[biome],
                areaByBiome[biome] / worldArea))
            .ToArray();

        return new BiomeMapPreview(
            scenario.EnableBiomes,
            scenario.BiomeMapKind.ToString(),
            scenario.Seed,
            map.Bounds.Width,
            map.Bounds.Height,
            map.CellSize,
            map.CellCountX,
            map.CellCountY,
            map.ResourceVoidBorderWidth,
            cells,
            scenario.EnableObstacles,
            scenario.ObstacleMapKind.ToString(),
            obstacleMap.CellSize,
            obstacleMap.CellCountX,
            obstacleMap.CellCountY,
            obstacleMap.BlockedCellCount,
            obstacleMap.GetCellsCopy(),
            biomes);
    }

    public MapArtifactSaveResult SaveMapArtifact(MapArtifactSaveRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Map name is required.");
        }

        if (request.Scenario.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            throw new ArgumentException("Scenario JSON is required.");
        }

        var scenario = SimulationScenarioJson.FromJson(request.Scenario.GetRawText());
        if (request.Seed is { } seed)
        {
            scenario = scenario with { Seed = seed };
        }

        var scenarioDirectory = ResolveScenarioDirectory(request.ScenarioPath);
        scenario = scenario.Validated();
        var biomeMap = request.Cells is { Count: > 0 }
            ? CreateBiomeMapFromEditedCells(scenario, request.Cells)
            : SimulationScenarioFactory.CreateBiomeMap(scenario, scenarioDirectory);
        var obstacleMap = request.ObstacleCells is { Count: > 0 }
            ? CreateObstacleMapFromEditedCells(scenario, request.ObstacleCells)
            : SimulationScenarioFactory.CreateObstacleMap(scenario, scenarioDirectory);

        var mapRoot = UserMapArtifactRoot();
        Directory.CreateDirectory(mapRoot);
        var slug = Slugify(name);
        var mapPath = GetUniquePath(Path.Combine(mapRoot, $"{slug}.lineage-map.json"));
        EnsurePathInside(mapPath, mapRoot);
        WorldMapArtifactJson.Save(
            mapPath,
            WorldMapArtifactDocument.FromMaps(
                biomeMap,
                obstacleMap,
                name,
                scenario.BiomeMapKind,
                scenario.ObstacleMapKind,
                scenario.Seed));

        var option = ToMapArtifactOption(mapPath, WorldMapArtifactJson.Load(mapPath));
        return new MapArtifactSaveResult(option, option.Path);
    }

    public MapArtifactSaveResult RenameMapArtifact(MapArtifactRenameRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Map name is required.");
        }

        var path = ResolveUserMapArtifactPath(request.Path);
        var document = WorldMapArtifactJson.Load(path).Validated() with { Name = name };
        var targetPath = Path.Combine(UserMapArtifactRoot(), $"{Slugify(name)}.lineage-map.json");
        if (!PathsEqual(path, targetPath))
        {
            targetPath = GetUniquePath(targetPath);
        }

        EnsurePathInside(targetPath, UserMapArtifactRoot());
        WorldMapArtifactJson.Save(targetPath, document);
        if (!PathsEqual(path, targetPath))
        {
            File.Delete(path);
        }

        var option = ToMapArtifactOption(targetPath, WorldMapArtifactJson.Load(targetPath));
        return new MapArtifactSaveResult(option, option.Path);
    }

    public MapArtifactSaveResult DuplicateMapArtifact(MapArtifactDuplicateRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Map name is required.");
        }

        var sourcePath = ResolveUserMapArtifactPath(request.Path);
        var document = WorldMapArtifactJson.Load(sourcePath).Validated() with { Name = name };
        var targetPath = GetUniquePath(Path.Combine(UserMapArtifactRoot(), $"{Slugify(name)}.lineage-map.json"));
        EnsurePathInside(targetPath, UserMapArtifactRoot());
        WorldMapArtifactJson.Save(targetPath, document);

        var option = ToMapArtifactOption(targetPath, WorldMapArtifactJson.Load(targetPath));
        return new MapArtifactSaveResult(option, option.Path);
    }

    public MapArtifactDeleteResult DeleteMapArtifact(MapArtifactDeleteRequest request)
    {
        var path = ResolveUserMapArtifactPath(request.Path);
        var archiveRoot = Path.Combine(_repoRoot, "out", "map-trash");
        Directory.CreateDirectory(archiveRoot);
        var archivePath = GetUniquePath(Path.Combine(
            archiveRoot,
            $"{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Path.GetFileName(path)}"));
        File.Move(path, archivePath);

        return new MapArtifactDeleteResult(
            NormalizeArtifactRelativePath(path),
            NormalizeArtifactRelativePath(archivePath));
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

    public SpeciesCatalogExportResult? ExportRunSpeciesProfile(string id, SpeciesCatalogExportRequest request)
    {
        if (!_runs.TryGetValue(id, out var run))
        {
            return null;
        }

        if (run.IsRunning)
        {
            throw new InvalidOperationException("Stop the run before exporting a species profile from it.");
        }

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Species profile name is required.");
        }

        var selectorCount = new[]
        {
            request.CreatureId.HasValue,
            request.FounderId.HasValue,
            !string.IsNullOrWhiteSpace(request.ClusterKey)
        }.Count(static selected => selected);
        if (selectorCount > 1)
        {
            throw new ArgumentException("Choose only one species export selector.");
        }

        var snapshotPath = ResolveSpeciesExportSnapshotPath(run.Manifest);
        var restored = SimulationSnapshotJson.LoadSimulation(snapshotPath);
        var notes = string.IsNullOrWhiteSpace(request.Notes)
            ? $"Exported from launcher run {run.Manifest.Id} using {NormalizeArtifactRelativePath(snapshotPath)}."
            : request.Notes.Trim();

        var profile = request switch
        {
            { CreatureId: { } creatureId } => SpeciesProfileExporter.ExportCreature(
                restored.Scenario,
                restored.Simulation.State,
                new EntityId(creatureId),
                name,
                notes),
            { FounderId: { } founderId } => SpeciesProfileExporter.ExportFounderLineageRepresentative(
                restored.Scenario,
                restored.Simulation.State,
                new EntityId(founderId),
                name,
                notes),
            { ClusterKey: { } clusterKey } when !string.IsNullOrWhiteSpace(clusterKey) => SpeciesProfileExporter.ExportSpeciesClusterRepresentative(
                restored.Scenario,
                restored.Simulation.State,
                clusterKey.Trim(),
                name,
                notes),
            _ => SpeciesProfileExporter.ExportDominantLivingLineageRepresentative(
                restored.Scenario,
                restored.Simulation.State,
                name,
                notes)
        };

        var speciesRoot = UserSpeciesCatalogRoot();
        Directory.CreateDirectory(speciesRoot);
        BrainCatalogEntry? brainEntry = null;
        if (request.ExportPairedBrain)
        {
            var brainName = $"{profile.Name} Brain";
            var brainNotes = $"Paired controller exported with species profile {profile.Name}.";
            var brainProfile = BrainProfileExporter.ExportCreatureBrain(
                restored.Scenario,
                restored.Simulation.State,
                new EntityId(profile.Source.CreatureId),
                brainName,
                brainNotes);
            var brainRoot = UserBrainCatalogRoot();
            Directory.CreateDirectory(brainRoot);
            var brainPath = GetUniquePath(Path.Combine(brainRoot, $"{Slugify(brainName)}{BrainProfileJson.FileExtension}"));
            EnsurePathInside(brainPath, brainRoot);
            BrainProfileJson.Save(brainPath, brainProfile);
            brainEntry = ToBrainCatalogEntry(brainPath, brainProfile);
            profile = profile with { DefaultBrainPath = NormalizeArtifactRelativePath(brainPath) };
        }

        var path = GetUniquePath(Path.Combine(speciesRoot, $"{Slugify(name)}{SpeciesProfileJson.FileExtension}"));
        EnsurePathInside(path, speciesRoot);
        SpeciesProfileJson.Save(path, profile);
        return new SpeciesCatalogExportResult(ToSpeciesCatalogEntry(path, profile), brainEntry);
    }

    public BrainCatalogExportResult? ExportRunBrainProfile(string id, BrainCatalogExportRequest request)
    {
        if (!_runs.TryGetValue(id, out var run))
        {
            return null;
        }

        if (run.IsRunning)
        {
            throw new InvalidOperationException("Stop the run before exporting a brain profile from it.");
        }

        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Brain profile name is required.");
        }

        var snapshotPath = ResolveSpeciesExportSnapshotPath(run.Manifest);
        var restored = SimulationSnapshotJson.LoadSimulation(snapshotPath);
        var notes = string.IsNullOrWhiteSpace(request.Notes)
            ? $"Exported from launcher run {run.Manifest.Id} using {NormalizeArtifactRelativePath(snapshotPath)}."
            : request.Notes.Trim();

        var profile = request.CreatureId is { } creatureId
            ? BrainProfileExporter.ExportCreatureBrain(
                restored.Scenario,
                restored.Simulation.State,
                new EntityId(creatureId),
                name,
                notes)
            : BrainProfileExporter.ExportDominantLivingLineageBrain(
                restored.Scenario,
                restored.Simulation.State,
                name,
                notes);

        var brainRoot = UserBrainCatalogRoot();
        Directory.CreateDirectory(brainRoot);
        var path = GetUniquePath(Path.Combine(brainRoot, $"{Slugify(name)}{BrainProfileJson.FileExtension}"));
        EnsurePathInside(path, brainRoot);
        BrainProfileJson.Save(path, profile);
        return new BrainCatalogExportResult(ToBrainCatalogEntry(path, profile));
    }

    public SpeciesCatalogDeleteResult DeleteSpeciesCatalogEntry(SpeciesCatalogDeleteRequest request)
    {
        var path = ResolveUserSpeciesCatalogPath(request.Path);
        var archiveRoot = Path.Combine(_repoRoot, "out", "species-trash");
        Directory.CreateDirectory(archiveRoot);
        var archivePath = GetUniquePath(Path.Combine(
            archiveRoot,
            $"{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Path.GetFileName(path)}"));
        File.Move(path, archivePath);

        return new SpeciesCatalogDeleteResult(
            NormalizeArtifactRelativePath(path),
            NormalizeArtifactRelativePath(archivePath));
    }

    public BrainCatalogDeleteResult DeleteBrainCatalogEntry(BrainCatalogDeleteRequest request)
    {
        var path = ResolveUserBrainCatalogPath(request.Path);
        var archiveRoot = Path.Combine(_repoRoot, "out", "brain-trash");
        Directory.CreateDirectory(archiveRoot);
        var archivePath = GetUniquePath(Path.Combine(
            archiveRoot,
            $"{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Path.GetFileName(path)}"));
        File.Move(path, archivePath);

        return new BrainCatalogDeleteResult(
            NormalizeArtifactRelativePath(path),
            NormalizeArtifactRelativePath(archivePath));
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
        var launchScenarioPath = WriteLaunchScenarioIfProvided(request.Scenario, createdAt, scenarioName, scenarioPath);
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

    private string WriteLaunchScenarioIfProvided(
        JsonElement? scenarioElement,
        DateTimeOffset createdAt,
        string scenarioName,
        string sourceScenarioPath)
    {
        if (scenarioElement is null
            || scenarioElement.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return string.Empty;
        }

        var scenario = SimulationScenarioJson.FromJson(scenarioElement.Value.GetRawText());
        if (!string.IsNullOrWhiteSpace(scenario.WorldMapPath))
        {
            scenario = scenario with
            {
                WorldMapPath = SimulationScenarioFactory.ResolveWorldMapPath(
                    scenario.WorldMapPath,
                    Path.GetDirectoryName(sourceScenarioPath))
            };
        }

        if (!string.IsNullOrWhiteSpace(scenario.ManualBiomeMapPath))
        {
            scenario = scenario with
            {
                ManualBiomeMapPath = SimulationScenarioFactory.ResolveManualBiomeMapPath(
                    scenario.ManualBiomeMapPath,
                    Path.GetDirectoryName(sourceScenarioPath))
            };
        }

        if (!string.IsNullOrWhiteSpace(scenario.ManualObstacleMapPath))
        {
            scenario = scenario with
            {
                ManualObstacleMapPath = SimulationScenarioFactory.ResolveManualObstacleMapPath(
                    scenario.ManualObstacleMapPath,
                    Path.GetDirectoryName(sourceScenarioPath))
            };
        }

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
        var artifactInventory = run.GetArtifactInventory(MeasureRunArtifacts, DateTimeOffset.UtcNow);

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
            ArtifactSizeBytes: artifactInventory.SizeBytes,
            ArtifactFileCount: artifactInventory.FileCount,
            IsRunning: isRunning,
            HasReport: File.Exists(manifest.ReportPath));
    }

    private RunArtifactInventory MeasureRunArtifacts(RunManifest manifest)
    {
        var seenPaths = new HashSet<string>(PathComparer());
        long sizeBytes = 0;
        var fileCount = 0;

        AddDirectory(manifest.RunDirectory);
        AddFile(manifest.LaunchScenarioPath);

        return new RunArtifactInventory(sizeBytes, fileCount);

        void AddDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                var resolvedPath = ResolveArtifactPath(path);
                if (!Directory.Exists(resolvedPath))
                {
                    return;
                }

                foreach (var filePath in Directory.EnumerateFiles(resolvedPath, "*", SearchOption.AllDirectories))
                {
                    AddFile(filePath);
                }
            }
            catch
            {
                // The runner is a live view; ignore files that move while a run is writing artifacts.
            }
        }

        void AddFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                var resolvedPath = Path.GetFullPath(ResolveArtifactPath(path));
                if (!seenPaths.Add(resolvedPath) || !File.Exists(resolvedPath))
                {
                    return;
                }

                var info = new FileInfo(resolvedPath);
                sizeBytes += info.Length;
                fileCount++;
            }
            catch
            {
            }
        }
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
        run.InvalidateArtifactInventory();
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
                WorldMapPath: GetString(root, "worldMapPath"),
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

    private string? ResolveScenarioDirectory(string? scenarioPath)
    {
        return string.IsNullOrWhiteSpace(scenarioPath)
            ? null
            : Path.GetDirectoryName(ResolveScenarioPath(scenarioPath));
    }

    private static BiomeMap CreateBiomeMapFromEditedCells(
        SimulationScenario scenario,
        IReadOnlyList<string> cells)
    {
        var cellCountX = Math.Max(1, (int)MathF.Ceiling(scenario.WorldWidth / scenario.BiomeCellSize));
        var cellCountY = Math.Max(1, (int)MathF.Ceiling(scenario.WorldHeight / scenario.BiomeCellSize));
        var expectedCellCount = cellCountX * cellCountY;
        if (cells.Count != expectedCellCount)
        {
            throw new ArgumentException(
                $"Edited biome map has {cells.Count} cells, but the scenario expects {expectedCellCount}.");
        }

        var parsedCells = cells
            .Select(ParseBiomeKindName)
            .ToArray();
        return BiomeMap.CreateFromCells(
            new WorldBounds(scenario.WorldWidth, scenario.WorldHeight),
            scenario.BiomeCellSize,
            cellCountX,
            cellCountY,
            parsedCells,
            scenario.ResourceVoidBorderWidth);
    }

    private static ObstacleMap CreateObstacleMapFromEditedCells(
        SimulationScenario scenario,
        IReadOnlyList<bool> cells)
    {
        var cellCountX = Math.Max(1, (int)MathF.Ceiling(scenario.WorldWidth / scenario.ObstacleCellSize));
        var cellCountY = Math.Max(1, (int)MathF.Ceiling(scenario.WorldHeight / scenario.ObstacleCellSize));
        var expectedCellCount = cellCountX * cellCountY;
        if (cells.Count != expectedCellCount)
        {
            throw new ArgumentException(
                $"Edited obstacle map has {cells.Count} cells, but the scenario expects {expectedCellCount}.");
        }

        return ObstacleMap.CreateFromCells(
            new WorldBounds(scenario.WorldWidth, scenario.WorldHeight),
            scenario.ObstacleCellSize,
            cellCountX,
            cellCountY,
            cells);
    }

    private static BiomeKind ParseBiomeKindName(string value)
    {
        if (Enum.TryParse<BiomeKind>(value, ignoreCase: true, out var kind))
        {
            return BiomeKinds.Canonicalize(kind);
        }

        throw new ArgumentException($"Unknown biome kind '{value}'.");
    }

    private string UserScenarioRoot()
    {
        return Path.Combine(_repoRoot, "scenarios", UserScenarioFolderName);
    }

    private string MapArtifactRoot()
    {
        return Path.Combine(_repoRoot, "maps");
    }

    private string UserMapArtifactRoot()
    {
        return Path.Combine(MapArtifactRoot(), UserScenarioFolderName);
    }

    private string SpeciesCatalogRoot()
    {
        return Path.Combine(_repoRoot, "species");
    }

    private string UserSpeciesCatalogRoot()
    {
        return Path.Combine(SpeciesCatalogRoot(), UserScenarioFolderName);
    }

    private string BrainCatalogRoot()
    {
        return Path.Combine(_repoRoot, "brains");
    }

    private string UserBrainCatalogRoot()
    {
        return Path.Combine(BrainCatalogRoot(), UserScenarioFolderName);
    }

    private string ResolveUserMapArtifactPath(string artifactPath)
    {
        if (string.IsNullOrWhiteSpace(artifactPath))
        {
            throw new ArgumentException("Map path is required.");
        }

        var path = ResolveArtifactPath(artifactPath);
        var mapRoot = UserMapArtifactRoot();
        EnsurePathInside(path, mapRoot);
        if (!path.EndsWith(".lineage-map.json", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only reusable Lineage map artifacts can be managed.");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Map artifact was not found.", artifactPath);
        }

        return path;
    }

    private string ResolveUserSpeciesCatalogPath(string speciesPath)
    {
        if (string.IsNullOrWhiteSpace(speciesPath))
        {
            throw new ArgumentException("Species profile path is required.");
        }

        var path = ResolveArtifactPath(speciesPath);
        var speciesRoot = UserSpeciesCatalogRoot();
        EnsurePathInside(path, speciesRoot);
        if (!path.EndsWith(SpeciesProfileJson.FileExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only Lineage species profile artifacts can be managed.");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Species profile was not found.", speciesPath);
        }

        return path;
    }

    private string ResolveUserBrainCatalogPath(string brainPath)
    {
        if (string.IsNullOrWhiteSpace(brainPath))
        {
            throw new ArgumentException("Brain profile path is required.");
        }

        var path = ResolveArtifactPath(brainPath);
        var brainRoot = UserBrainCatalogRoot();
        EnsurePathInside(path, brainRoot);
        if (!path.EndsWith(BrainProfileJson.FileExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only Lineage brain profile artifacts can be managed.");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Brain profile was not found.", brainPath);
        }

        return path;
    }

    private string ResolveSpeciesExportSnapshotPath(RunManifest manifest)
    {
        if (!string.IsNullOrWhiteSpace(manifest.SnapshotPath))
        {
            var snapshotPath = ResolveArtifactPath(manifest.SnapshotPath);
            if (File.Exists(snapshotPath))
            {
                return snapshotPath;
            }
        }

        return ResolveContinuationSnapshotPath(manifest);
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

    private MapArtifactOption? TryReadMapArtifact(string path)
    {
        try
        {
            return ToMapArtifactOption(path, WorldMapArtifactJson.Load(path));
        }
        catch
        {
            return null;
        }
    }

    private MapArtifactOption ToMapArtifactOption(string path, WorldMapArtifactDocument document)
    {
        var validated = document.Validated();
        var blockedCells = validated.ObstacleBlockedCells.Count(static blocked => blocked);
        var biomes = validated.BiomeCells
            .Select(BiomeKinds.Canonicalize)
            .GroupBy(static biome => biome)
            .OrderBy(static group => group.Key.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new BiomeMapPreviewSummary(
                FormatBiomeKind(group.Key),
                BiomeColor(group.Key),
                group.Count(),
                group.Count() / (double)validated.BiomeCells.Length))
            .ToArray();
        return new MapArtifactOption(
            Name: string.IsNullOrWhiteSpace(validated.Name)
                ? Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path))
                : validated.Name,
            Path: NormalizeArtifactRelativePath(path),
            CanDelete: IsPathUnderDirectory(path, UserMapArtifactRoot()),
            WorldWidth: validated.WorldWidth,
            WorldHeight: validated.WorldHeight,
            BiomeCellSize: validated.BiomeCellSize,
            BiomeCellCountX: validated.BiomeCellCountX,
            BiomeCellCountY: validated.BiomeCellCountY,
            ResourceVoidBorderWidth: validated.ResourceVoidBorderWidth,
            ObstacleCellSize: validated.ObstacleCellSize,
            ObstacleCellCountX: validated.ObstacleCellCountX,
            ObstacleCellCountY: validated.ObstacleCellCountY,
            ObstacleBlockedCellCount: blockedCells,
            SourceSeed: validated.SourceSeed,
            SourceBiomeMapKind: validated.SourceBiomeMapKind?.ToString(),
            SourceObstacleMapKind: validated.SourceObstacleMapKind?.ToString(),
            Biomes: biomes);
    }

    private SpeciesCatalogEntry? TryReadSpeciesCatalogEntry(string path)
    {
        try
        {
            return ToSpeciesCatalogEntry(path, SpeciesProfileJson.Load(path));
        }
        catch
        {
            return null;
        }
    }

    private BrainCatalogEntry? TryReadBrainCatalogEntry(string path)
    {
        try
        {
            return ToBrainCatalogEntry(path, BrainProfileJson.Load(path));
        }
        catch
        {
            return null;
        }
    }

    private SpeciesCatalogEntry ToSpeciesCatalogEntry(string path, SpeciesProfile profile)
    {
        var validated = profile.Validated();
        var genome = validated.Genome;
        return new SpeciesCatalogEntry(
            Name: validated.Name,
            Path: NormalizeArtifactRelativePath(path),
            CanDelete: IsPathUnderDirectory(path, UserSpeciesCatalogRoot()),
            Notes: validated.Notes,
            DefaultBrainPath: validated.DefaultBrainPath,
            BrainArchitectureKind: validated.BrainArchitectureKind.ToString(),
            BrainHiddenNodeCount: validated.BrainHiddenNodeCount,
            BrainWeightCount: validated.BrainWeights.Length,
            BodyRadius: genome.BodyRadius,
            MaxSpeed: genome.MaxSpeed,
            SenseRadius: genome.SenseRadius,
            VisionAngleDegrees: genome.VisionAngleRadians * 180d / Math.PI,
            BasalEnergyPerSecond: genome.BasalEnergyPerSecond,
            MovementEnergyPerSecond: genome.MovementEnergyPerSecond,
            EatCaloriesPerSecond: genome.EatCaloriesPerSecond,
            ReproductionEnergyThreshold: genome.ReproductionEnergyThreshold,
            OffspringEnergyInvestment: genome.OffspringEnergyInvestment,
            SourceScenarioName: validated.Source.ScenarioName,
            SourceSeed: validated.Source.Seed,
            SourceTick: validated.Source.Tick,
            SourceCreatureId: validated.Source.CreatureId,
            SourceFounderId: validated.Source.FounderId,
            SourceGeneration: validated.Source.Generation,
            ExportedAtUtc: validated.Source.ExportedAtUtc);
    }

    private BrainCatalogEntry ToBrainCatalogEntry(string path, BrainProfile profile)
    {
        var validated = profile.Validated();
        return new BrainCatalogEntry(
            Name: validated.Name,
            Path: NormalizeArtifactRelativePath(path),
            CanDelete: IsPathUnderDirectory(path, UserBrainCatalogRoot()),
            Notes: validated.Notes,
            BrainArchitectureKind: validated.BrainArchitectureKind.ToString(),
            InputSchemaVersion: validated.InputSchemaVersion,
            OutputSchemaVersion: validated.OutputSchemaVersion,
            InputCount: validated.InputCount,
            OutputCount: validated.OutputCount,
            HiddenNodeCount: validated.HiddenNodeCount,
            WeightCount: validated.Weights.Length,
            SourceScenarioName: validated.Source.ScenarioName,
            SourceSeed: validated.Source.Seed,
            SourceTick: validated.Source.Tick,
            SourceCreatureId: validated.Source.CreatureId,
            SourceFounderId: validated.Source.FounderId,
            SourceGeneration: validated.Source.Generation,
            ExportedAtUtc: validated.Source.ExportedAtUtc);
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
            throw new InvalidOperationException("Path is outside the managed folder.");
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

    private string NormalizeArtifactRelativePath(string path)
    {
        return Path.GetRelativePath(_repoRoot, path)
            .Trim()
            .Replace('\\', '/')
            .TrimStart('/');
    }

    private static string GetUniqueArchivePath(string preferredPath)
    {
        return GetUniquePath(preferredPath, "Could not choose a unique scenario archive path.");
    }

    private static string GetUniquePath(
        string preferredPath,
        string failureMessage = "Could not choose a unique managed file path.")
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

        throw new IOException(failureMessage);
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
        var cliOutputDirectory = Path.Combine(_repoRoot, "src", "Lineage.Cli", "bin", "Release", "net8.0");
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

        var cliProjectPath = Path.Combine(_repoRoot, "src", "Lineage.Cli", "Lineage.Cli.csproj");
        if (File.Exists(cliProjectPath))
        {
            return ("dotnet", ["run", "-c", "Release", "--project", cliProjectPath, "--"]);
        }

        throw new InvalidOperationException(
            "Could not find Lineage.Cli.csproj. Cannot launch a Release CLI run.");
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
            CliStatusIntervalTicks.ToString(CultureInfo.InvariantCulture),
            "--status-detail-interval",
            CliStatusDetailIntervalTicks.ToString(CultureInfo.InvariantCulture)
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
                summary.WorldMapPath is null ? null : $"world map {summary.WorldMapPath}",
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
            .Where(field => field.Name is not "ManualBiomeMapPath" and not "ManualObstacleMapPath")
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

    private readonly record struct RunArtifactInventory(long SizeBytes, int FileCount);

    private sealed class ManagedRun(RunManifest manifest, Process? process)
    {
        private static readonly TimeSpan ArtifactInventoryRefreshInterval = TimeSpan.FromSeconds(10);

        private int _exitRecorded;
        private readonly object _artifactInventoryLock = new();
        private RunArtifactInventory _artifactInventory;
        private DateTimeOffset _artifactInventoryMeasuredAtUtc;

        public RunManifest Manifest { get; } = manifest;

        public Process? Process { get; private set; } = process;

        public bool IsRunning => Process is { HasExited: false };

        public void AttachProcess(Process process)
        {
            DisposeProcess();
            Process = process;
            Interlocked.Exchange(ref _exitRecorded, 0);
            InvalidateArtifactInventory();
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

        public RunArtifactInventory GetArtifactInventory(
            Func<RunManifest, RunArtifactInventory> measure,
            DateTimeOffset now)
        {
            lock (_artifactInventoryLock)
            {
                if (_artifactInventoryMeasuredAtUtc != default
                    && now - _artifactInventoryMeasuredAtUtc < ArtifactInventoryRefreshInterval)
                {
                    return _artifactInventory;
                }

                _artifactInventory = measure(Manifest);
                _artifactInventoryMeasuredAtUtc = now;
                return _artifactInventory;
            }
        }

        public void InvalidateArtifactInventory()
        {
            lock (_artifactInventoryLock)
            {
                _artifactInventory = default;
                _artifactInventoryMeasuredAtUtc = default;
            }
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
