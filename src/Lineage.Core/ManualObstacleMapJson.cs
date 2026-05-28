using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lineage.Core;

public sealed record ManualObstacleMapDocument
{
    public const string CurrentSchemaVersion = "lineage.manualObstacleMap.v1";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string? Name { get; init; }

    public ObstacleMapKind? SourceMapKind { get; init; }

    public ulong? SourceSeed { get; init; }

    public float WorldWidth { get; init; }

    public float WorldHeight { get; init; }

    public float CellSize { get; init; }

    public int CellCountX { get; init; }

    public int CellCountY { get; init; }

    public bool[] BlockedCells { get; init; } = [];

    public static ManualObstacleMapDocument FromObstacleMap(
        ObstacleMap map,
        string? name = null,
        ObstacleMapKind? sourceMapKind = null,
        ulong? sourceSeed = null)
    {
        return new ManualObstacleMapDocument
        {
            Name = name,
            SourceMapKind = sourceMapKind,
            SourceSeed = sourceSeed,
            WorldWidth = map.Bounds.Width,
            WorldHeight = map.Bounds.Height,
            CellSize = map.CellSize,
            CellCountX = map.CellCountX,
            CellCountY = map.CellCountY,
            BlockedCells = map.GetCellsCopy()
        };
    }

    public ManualObstacleMapDocument Validated()
    {
        if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Manual obstacle map schema version must be '{CurrentSchemaVersion}'.");
        }

        EnsurePositive(WorldWidth, nameof(WorldWidth));
        EnsurePositive(WorldHeight, nameof(WorldHeight));
        EnsurePositive(CellSize, nameof(CellSize));
        if (CellCountX <= 0 || CellCountY <= 0)
        {
            throw new InvalidOperationException("Manual obstacle map cell dimensions must be positive.");
        }

        if (BlockedCells is null)
        {
            throw new InvalidOperationException("Manual obstacle map cells are required.");
        }

        var expectedCellCount = CellCountX * CellCountY;
        if (BlockedCells.Length != expectedCellCount)
        {
            throw new InvalidOperationException(
                $"Manual obstacle map cell count must be {expectedCellCount}, but was {BlockedCells.Length}.");
        }

        return this with { BlockedCells = BlockedCells.ToArray() };
    }

    public ObstacleMap ToObstacleMap()
    {
        var validated = Validated();
        return ObstacleMap.CreateFromCells(
            new WorldBounds(validated.WorldWidth, validated.WorldHeight),
            validated.CellSize,
            validated.CellCountX,
            validated.CellCountY,
            validated.BlockedCells);
    }

    private static void EnsurePositive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0f)
        {
            throw new InvalidOperationException($"{name} must be finite and positive.");
        }
    }
}

public static class ManualObstacleMapJson
{
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    public static string ToJson(ManualObstacleMapDocument document)
    {
        return JsonSerializer.Serialize(document.Validated(), JsonOptions);
    }

    public static ManualObstacleMapDocument FromJson(string json)
    {
        var document = JsonSerializer.Deserialize<ManualObstacleMapDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("Manual obstacle map JSON did not contain a map object.");
        return document.Validated();
    }

    public static ManualObstacleMapDocument Load(string path)
    {
        return FromJson(File.ReadAllText(path));
    }

    public static void Save(string path, ManualObstacleMapDocument document)
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
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
