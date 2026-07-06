using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lineage.Core;

public sealed record WorldMapArtifactDocument
{
    public const string CurrentSchemaVersion = "lineage.worldMap.v1";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string? Name { get; init; }

    public BiomeMapKind? SourceBiomeMapKind { get; init; }

    public ObstacleMapKind? SourceObstacleMapKind { get; init; }

    public ulong? SourceSeed { get; init; }

    public float WorldWidth { get; init; }

    public float WorldHeight { get; init; }

    public float BiomeCellSize { get; init; }

    public int BiomeCellCountX { get; init; }

    public int BiomeCellCountY { get; init; }

    public float ResourceVoidBorderWidth { get; init; }

    public BiomeKind[] BiomeCells { get; init; } = [];

    public float ObstacleCellSize { get; init; }

    public int ObstacleCellCountX { get; init; }

    public int ObstacleCellCountY { get; init; }

    public bool[] ObstacleBlockedCells { get; init; } = [];

    public WorldMapObstacleGroup[] ObstacleGroups { get; init; } = [];

    public static WorldMapArtifactDocument FromMaps(
        BiomeMap biomeMap,
        ObstacleMap obstacleMap,
        string? name = null,
        BiomeMapKind? sourceBiomeMapKind = null,
        ObstacleMapKind? sourceObstacleMapKind = null,
        ulong? sourceSeed = null)
    {
        if (biomeMap.Bounds.Width != obstacleMap.Bounds.Width
            || biomeMap.Bounds.Height != obstacleMap.Bounds.Height)
        {
            throw new ArgumentException("Biome and obstacle maps must share world bounds.");
        }

        return new WorldMapArtifactDocument
        {
            Name = name,
            SourceBiomeMapKind = sourceBiomeMapKind,
            SourceObstacleMapKind = sourceObstacleMapKind,
            SourceSeed = sourceSeed,
            WorldWidth = biomeMap.Bounds.Width,
            WorldHeight = biomeMap.Bounds.Height,
            BiomeCellSize = biomeMap.CellSize,
            BiomeCellCountX = biomeMap.CellCountX,
            BiomeCellCountY = biomeMap.CellCountY,
            ResourceVoidBorderWidth = biomeMap.ResourceVoidBorderWidth,
            BiomeCells = biomeMap.GetCellsCopy(),
            ObstacleCellSize = obstacleMap.CellSize,
            ObstacleCellCountX = obstacleMap.CellCountX,
            ObstacleCellCountY = obstacleMap.CellCountY,
            ObstacleBlockedCells = obstacleMap.GetCellsCopy()
        };
    }

    public WorldMapArtifactDocument Validated()
    {
        if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"World map artifact schema version must be '{CurrentSchemaVersion}'.");
        }

        EnsurePositive(WorldWidth, nameof(WorldWidth));
        EnsurePositive(WorldHeight, nameof(WorldHeight));
        EnsurePositive(BiomeCellSize, nameof(BiomeCellSize));
        EnsurePositive(ObstacleCellSize, nameof(ObstacleCellSize));
        EnsurePositiveCellDimensions(BiomeCellCountX, BiomeCellCountY, "World map biome");
        EnsurePositiveCellDimensions(ObstacleCellCountX, ObstacleCellCountY, "World map obstacle");
        EnsureNonNegative(ResourceVoidBorderWidth, nameof(ResourceVoidBorderWidth));
        if (ResourceVoidBorderWidth * 2f >= Math.Min(WorldWidth, WorldHeight))
        {
            throw new InvalidOperationException(
                "World map resource void border width must leave a positive resource-spawn area.");
        }

        if (BiomeCells is null)
        {
            throw new InvalidOperationException("World map biome cells are required.");
        }

        if (ObstacleBlockedCells is null)
        {
            throw new InvalidOperationException("World map obstacle cells are required.");
        }

        var expectedBiomeCellCount = BiomeCellCountX * BiomeCellCountY;
        if (BiomeCells.Length != expectedBiomeCellCount)
        {
            throw new InvalidOperationException(
                $"World map biome cell count must be {expectedBiomeCellCount}, but was {BiomeCells.Length}.");
        }

        var expectedObstacleCellCount = ObstacleCellCountX * ObstacleCellCountY;
        if (ObstacleBlockedCells.Length != expectedObstacleCellCount)
        {
            throw new InvalidOperationException(
                $"World map obstacle cell count must be {expectedObstacleCellCount}, but was {ObstacleBlockedCells.Length}.");
        }

        var seenGroupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenGroupCells = new HashSet<int>();
        var obstacleGroups = (ObstacleGroups ?? [])
            .Select(group =>
            {
                var validated = (group ?? throw new InvalidOperationException("World map obstacle group entries cannot be null."))
                    .Validated(expectedObstacleCellCount);
                if (!seenGroupIds.Add(validated.Id))
                {
                    throw new InvalidOperationException($"World map obstacle group id '{validated.Id}' is duplicated.");
                }

                foreach (var cell in validated.Cells)
                {
                    if (!seenGroupCells.Add(cell))
                    {
                        throw new InvalidOperationException($"World map obstacle cell {cell} is assigned to multiple groups.");
                    }
                }

                return validated;
            })
            .ToArray();

        return this with
        {
            BiomeCells = BiomeCells.Select(BiomeKinds.Canonicalize).ToArray(),
            ObstacleBlockedCells = ObstacleBlockedCells.ToArray(),
            ObstacleGroups = obstacleGroups
        };
    }

    public BiomeMap ToBiomeMap()
    {
        var validated = Validated();
        return BiomeMap.CreateFromCells(
            new WorldBounds(validated.WorldWidth, validated.WorldHeight),
            validated.BiomeCellSize,
            validated.BiomeCellCountX,
            validated.BiomeCellCountY,
            validated.BiomeCells,
            validated.ResourceVoidBorderWidth);
    }

    public ObstacleMap ToObstacleMap()
    {
        var validated = Validated();
        return ObstacleMap.CreateFromCells(
            new WorldBounds(validated.WorldWidth, validated.WorldHeight),
            validated.ObstacleCellSize,
            validated.ObstacleCellCountX,
            validated.ObstacleCellCountY,
            validated.ObstacleBlockedCells);
    }

    private static void EnsurePositive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0f)
        {
            throw new InvalidOperationException($"{name} must be finite and positive.");
        }
    }

    private static void EnsureNonNegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f)
        {
            throw new InvalidOperationException($"{name} must be finite and non-negative.");
        }
    }

    private static void EnsurePositiveCellDimensions(int cellCountX, int cellCountY, string label)
    {
        if (cellCountX <= 0 || cellCountY <= 0)
        {
            throw new InvalidOperationException($"{label} cell dimensions must be positive.");
        }
    }
}

public sealed record WorldMapObstacleGroup
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool DefaultBlocked { get; init; } = true;

    public int[] Cells { get; init; } = [];

    public WorldMapObstacleGroup Validated(int obstacleCellCount)
    {
        if (obstacleCellCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(obstacleCellCount), "Obstacle cell count must be positive.");
        }

        var id = (Id ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("World map obstacle group id is required.");
        }

        for (var i = 0; i < id.Length; i++)
        {
            var ch = id[i];
            if (!char.IsLetterOrDigit(ch) && ch is not '_' and not '-')
            {
                throw new InvalidOperationException("World map obstacle group id can only contain letters, numbers, underscores, or dashes.");
            }
        }

        var name = string.IsNullOrWhiteSpace(Name)
            ? id
            : Name.Trim();
        var cells = (Cells ?? [])
            .Distinct()
            .Order()
            .ToArray();
        for (var i = 0; i < cells.Length; i++)
        {
            if (cells[i] < 0 || cells[i] >= obstacleCellCount)
            {
                throw new InvalidOperationException(
                    $"World map obstacle group '{id}' contains cell {cells[i]}, but valid cell indexes are 0 through {obstacleCellCount - 1}.");
            }
        }

        return this with
        {
            Id = id,
            Name = name,
            Cells = cells
        };
    }
}

public static class WorldMapArtifactJson
{
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    public static string ToJson(WorldMapArtifactDocument document)
    {
        return JsonSerializer.Serialize(document.Validated(), JsonOptions);
    }

    public static WorldMapArtifactDocument FromJson(string json)
    {
        var document = JsonSerializer.Deserialize<WorldMapArtifactDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("World map artifact JSON did not contain a map object.");
        return document.Validated();
    }

    public static WorldMapArtifactDocument Load(string path)
    {
        return FromJson(File.ReadAllText(path));
    }

    public static void Save(string path, WorldMapArtifactDocument document)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, ToJson(document));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        options.Converters.Add(new BiomeKindJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
