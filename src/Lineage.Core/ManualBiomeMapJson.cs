using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lineage.Core;

public sealed record ManualBiomeMapDocument
{
    public const string CurrentSchemaVersion = "lineage.manualBiomeMap.v1";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string? Name { get; init; }

    public BiomeMapKind? SourceMapKind { get; init; }

    public ulong? SourceSeed { get; init; }

    public float WorldWidth { get; init; }

    public float WorldHeight { get; init; }

    public float CellSize { get; init; }

    public int CellCountX { get; init; }

    public int CellCountY { get; init; }

    public float ResourceVoidBorderWidth { get; init; }

    public BiomeKind[] Cells { get; init; } = [];

    public static ManualBiomeMapDocument FromBiomeMap(
        BiomeMap map,
        string? name = null,
        BiomeMapKind? sourceMapKind = null,
        ulong? sourceSeed = null)
    {
        return new ManualBiomeMapDocument
        {
            Name = name,
            SourceMapKind = sourceMapKind,
            SourceSeed = sourceSeed,
            WorldWidth = map.Bounds.Width,
            WorldHeight = map.Bounds.Height,
            CellSize = map.CellSize,
            CellCountX = map.CellCountX,
            CellCountY = map.CellCountY,
            ResourceVoidBorderWidth = map.ResourceVoidBorderWidth,
            Cells = map.GetCellsCopy()
        };
    }

    public ManualBiomeMapDocument Validated()
    {
        if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Manual biome map schema version must be '{CurrentSchemaVersion}'.");
        }

        EnsurePositive(WorldWidth, nameof(WorldWidth));
        EnsurePositive(WorldHeight, nameof(WorldHeight));
        EnsurePositive(CellSize, nameof(CellSize));
        if (CellCountX <= 0 || CellCountY <= 0)
        {
            throw new InvalidOperationException("Manual biome map cell dimensions must be positive.");
        }

        EnsureNonNegative(ResourceVoidBorderWidth, nameof(ResourceVoidBorderWidth));
        if (ResourceVoidBorderWidth * 2f >= Math.Min(WorldWidth, WorldHeight))
        {
            throw new InvalidOperationException(
                "Manual biome map resource void border width must leave a positive resource-spawn area.");
        }

        if (Cells is null)
        {
            throw new InvalidOperationException("Manual biome map cells are required.");
        }

        var expectedCellCount = CellCountX * CellCountY;
        if (Cells.Length != expectedCellCount)
        {
            throw new InvalidOperationException(
                $"Manual biome map cell count must be {expectedCellCount}, but was {Cells.Length}.");
        }

        var cells = Cells
            .Select(BiomeKinds.Canonicalize)
            .ToArray();
        return this with { Cells = cells };
    }

    public BiomeMap ToBiomeMap()
    {
        var validated = Validated();
        return BiomeMap.CreateFromCells(
            new WorldBounds(validated.WorldWidth, validated.WorldHeight),
            validated.CellSize,
            validated.CellCountX,
            validated.CellCountY,
            validated.Cells,
            validated.ResourceVoidBorderWidth);
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
}

public static class ManualBiomeMapJson
{
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    public static string ToJson(ManualBiomeMapDocument document)
    {
        return JsonSerializer.Serialize(document.Validated(), JsonOptions);
    }

    public static ManualBiomeMapDocument FromJson(string json)
    {
        var document = JsonSerializer.Deserialize<ManualBiomeMapDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("Manual biome map JSON did not contain a map object.");
        return document.Validated();
    }

    public static ManualBiomeMapDocument Load(string path)
    {
        return FromJson(File.ReadAllText(path));
    }

    public static void Save(string path, ManualBiomeMapDocument document)
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
