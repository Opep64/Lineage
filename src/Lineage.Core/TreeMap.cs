namespace Lineage.Core;

/// <summary>
/// Coarse tree-cover layer used for soft terrain drag.
/// </summary>
public sealed class TreeMap
{
    public const float DefaultMovementSpeedMultiplierAtFullCover = 0.72f;

    private const ulong TreeCoverSalt = 0x747265655F6331UL;
    private const ulong TreeDetailSalt = 0x747265655F6432UL;

    private readonly float[] _coverCells;

    private TreeMap(
        WorldBounds bounds,
        float cellSize,
        int cellCountX,
        int cellCountY,
        float[] coverCells)
    {
        Bounds = bounds;
        CellSize = cellSize;
        CellCountX = cellCountX;
        CellCountY = cellCountY;
        _coverCells = coverCells;

        var total = 0f;
        var covered = 0;
        for (var i = 0; i < _coverCells.Length; i++)
        {
            total += _coverCells[i];
            if (_coverCells[i] > 0.001f)
            {
                covered++;
            }
        }

        CoveredCellCount = covered;
        AverageCover = _coverCells.Length > 0 ? total / _coverCells.Length : 0f;
    }

    public WorldBounds Bounds { get; }

    public float CellSize { get; }

    public int CellCountX { get; }

    public int CellCountY { get; }

    public int CoveredCellCount { get; }

    public float AverageCover { get; }

    public bool HasTrees => CoveredCellCount > 0;

    public static TreeMap CreateEmpty(WorldBounds bounds, float cellSize)
    {
        ValidateBounds(bounds);
        ValidateCellSize(cellSize);

        var cellCountX = Math.Max(1, (int)MathF.Ceiling(bounds.Width / cellSize));
        var cellCountY = Math.Max(1, (int)MathF.Ceiling(bounds.Height / cellSize));
        return new TreeMap(bounds, cellSize, cellCountX, cellCountY, new float[cellCountX * cellCountY]);
    }

    public static TreeMap CreateFromCells(
        WorldBounds bounds,
        float cellSize,
        int cellCountX,
        int cellCountY,
        IReadOnlyList<float> coverCells)
    {
        ValidateBounds(bounds);
        ValidateCellSize(cellSize);

        if (cellCountX <= 0 || cellCountY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellCountX), "Tree cell dimensions must be positive.");
        }

        if (coverCells.Count != cellCountX * cellCountY)
        {
            throw new ArgumentException("Tree cell list must match the requested map dimensions.", nameof(coverCells));
        }

        var cells = new float[coverCells.Count];
        for (var i = 0; i < cells.Length; i++)
        {
            var cover = coverCells[i];
            if (!float.IsFinite(cover) || cover < 0f || cover > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(coverCells), "Tree cover values must be finite and in [0, 1].");
            }

            cells[i] = cover;
        }

        return new TreeMap(bounds, cellSize, cellCountX, cellCountY, cells);
    }

    public static TreeMap GenerateFromBiomes(BiomeMap biomes, float cellSize, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(biomes);
        ValidateCellSize(cellSize);

        var cellCountX = Math.Max(1, (int)MathF.Ceiling(biomes.Bounds.Width / cellSize));
        var cellCountY = Math.Max(1, (int)MathF.Ceiling(biomes.Bounds.Height / cellSize));
        var cells = new float[cellCountX * cellCountY];

        for (var y = 0; y < cellCountY; y++)
        {
            for (var x = 0; x < cellCountX; x++)
            {
                var center = new SimVector2(
                    MathF.Min(biomes.Bounds.Width, (x + 0.5f) * cellSize),
                    MathF.Min(biomes.Bounds.Height, (y + 0.5f) * cellSize));
                var biome = biomes.GetKindAt(center);
                cells[y * cellCountX + x] = GenerateCoverForCell(x, y, biome, seed);
            }
        }

        return new TreeMap(biomes.Bounds, cellSize, cellCountX, cellCountY, cells);
    }

    public float GetCover(int cellX, int cellY)
    {
        if ((uint)cellX >= (uint)CellCountX || (uint)cellY >= (uint)CellCountY)
        {
            throw new ArgumentOutOfRangeException(nameof(cellX), "Tree cell coordinate is outside the map.");
        }

        return _coverCells[ToIndex(cellX, cellY)];
    }

    public float[] GetCellsCopy()
    {
        return _coverCells.ToArray();
    }

    public float GetCoverAt(SimVector2 position)
    {
        if (!Bounds.Contains(position))
        {
            return 0f;
        }

        var cellX = Math.Min(CellCountX - 1, Math.Max(0, (int)(position.X / CellSize)));
        var cellY = Math.Min(CellCountY - 1, Math.Max(0, (int)(position.Y / CellSize)));
        return GetCover(cellX, cellY);
    }

    public float GetMovementSpeedMultiplierAt(SimVector2 position, float fullCoverMultiplier)
    {
        ValidateFullCoverMovementSpeedMultiplier(fullCoverMultiplier, nameof(fullCoverMultiplier));
        var cover = GetCoverAt(position);
        return 1f - cover * (1f - fullCoverMultiplier);
    }

    public BiomeCellBounds GetCellBounds(int cellX, int cellY)
    {
        if ((uint)cellX >= (uint)CellCountX || (uint)cellY >= (uint)CellCountY)
        {
            throw new ArgumentOutOfRangeException(nameof(cellX), "Tree cell coordinate is outside the map.");
        }

        var x = cellX * CellSize;
        var y = cellY * CellSize;
        return new BiomeCellBounds(
            x,
            y,
            Math.Min(CellSize, Bounds.Width - x),
            Math.Min(CellSize, Bounds.Height - y));
    }

    public TreeBiomeSummary[] SummarizeByBiome(BiomeMap biomes, float fullCoverMultiplier)
    {
        ArgumentNullException.ThrowIfNull(biomes);
        ValidateFullCoverMovementSpeedMultiplier(fullCoverMultiplier, nameof(fullCoverMultiplier));

        if (biomes.Bounds.Width != Bounds.Width || biomes.Bounds.Height != Bounds.Height)
        {
            throw new ArgumentException("Biome map bounds must match the tree map bounds.", nameof(biomes));
        }

        var summaries = BiomeKinds.All
            .Select(kind => new MutableTreeBiomeSummary(kind))
            .ToArray();

        for (var y = 0; y < CellCountY; y++)
        {
            for (var x = 0; x < CellCountX; x++)
            {
                var cell = GetCellBounds(x, y);
                var center = new SimVector2(cell.X + cell.Width * 0.5f, cell.Y + cell.Height * 0.5f);
                var biome = BiomeKinds.Canonicalize(biomes.GetKindAt(center));
                var summary = summaries[(int)biome];
                var cover = GetCover(x, y);
                summary.CellCount++;
                summary.TotalCover += cover;
                summary.MaxCover = MathF.Max(summary.MaxCover, cover);
            }
        }

        return summaries
            .Select(summary =>
            {
                var averageCover = summary.CellCount > 0
                    ? summary.TotalCover / summary.CellCount
                    : 0f;
                var speed = 1f - averageCover * (1f - fullCoverMultiplier);
                return new TreeBiomeSummary(summary.Kind, summary.CellCount, averageCover, summary.MaxCover, speed);
            })
            .ToArray();
    }

    public static void ValidateFullCoverMovementSpeedMultiplier(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0f || value > 1f)
        {
            throw new ArgumentOutOfRangeException(name, "Full-cover tree speed multiplier must be finite and in (0, 1].");
        }
    }

    private int ToIndex(int cellX, int cellY)
    {
        return cellY * CellCountX + cellX;
    }

    private static float GenerateCoverForCell(int x, int y, BiomeKind biome, ulong seed)
    {
        var baseCover = BaseCoverForBiome(biome);
        if (baseCover <= 0f)
        {
            return 0f;
        }

        var broad = FractalNoise(x * 0.18f + 4.0f, y * 0.18f - 11.0f, seed ^ TreeCoverSalt);
        var detail = HashToUnit(x, y, seed ^ TreeDetailSalt);
        var cover = baseCover * (0.58f + broad * 0.68f);
        if (baseCover < 0.08f && detail > 0.92f)
        {
            cover += 0.04f * detail;
        }

        return Math.Clamp(cover, 0f, 1f);
    }

    private static float BaseCoverForBiome(BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Forest => 0.76f,
            BiomeKind.Wetland => 0.22f,
            BiomeKind.Scrubland => 0.16f,
            BiomeKind.Fertile => 0.05f,
            BiomeKind.Grassland => 0.03f,
            BiomeKind.Highland => 0.025f,
            BiomeKind.Tundra => 0.01f,
            BiomeKind.Desert => 0.005f,
            _ => 0f
        };
    }

    private static float FractalNoise(float x, float y, ulong seed)
    {
        var total = 0f;
        var amplitude = 1f;
        var amplitudeSum = 0f;
        var frequency = 1f;

        for (var octave = 0; octave < 3; octave++)
        {
            total += ValueNoise(x * frequency, y * frequency, seed + (ulong)octave * 0x9E3779B97F4A7C15UL) * amplitude;
            amplitudeSum += amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }

        return total / amplitudeSum;
    }

    private static float ValueNoise(float x, float y, ulong seed)
    {
        var x0 = (int)MathF.Floor(x);
        var y0 = (int)MathF.Floor(y);
        var tx = SmoothStep(x - x0);
        var ty = SmoothStep(y - y0);

        var a = HashToUnit(x0, y0, seed);
        var b = HashToUnit(x0 + 1, y0, seed);
        var c = HashToUnit(x0, y0 + 1, seed);
        var d = HashToUnit(x0 + 1, y0 + 1, seed);

        var top = Lerp(a, b, tx);
        var bottom = Lerp(c, d, tx);
        return Lerp(top, bottom, ty);
    }

    private static float HashToUnit(int x, int y, ulong seed)
    {
        unchecked
        {
            var h = seed;
            h ^= (uint)x * 0x9E3779B97F4A7C15UL;
            h = Mix(h);
            h ^= (uint)y * 0xBF58476D1CE4E5B9UL;
            h = Mix(h);
            return (h >> 40) * (1f / (1 << 24));
        }
    }

    private static ulong Mix(ulong value)
    {
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }

    private static float SmoothStep(float value)
    {
        return value * value * (3f - 2f * value);
    }

    private static float Lerp(float from, float to, float amount)
    {
        return from + (to - from) * amount;
    }

    private static void ValidateBounds(WorldBounds bounds)
    {
        if (!float.IsFinite(bounds.Width) || bounds.Width <= 0f || !float.IsFinite(bounds.Height) || bounds.Height <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(bounds), "Tree map bounds must be finite and positive.");
        }
    }

    private static void ValidateCellSize(float cellSize)
    {
        if (!float.IsFinite(cellSize) || cellSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Tree cell size must be finite and positive.");
        }
    }

    private sealed class MutableTreeBiomeSummary(BiomeKind kind)
    {
        public BiomeKind Kind { get; } = kind;

        public int CellCount { get; set; }

        public float TotalCover { get; set; }

        public float MaxCover { get; set; }
    }
}

public readonly record struct TreeBiomeSummary(
    BiomeKind Kind,
    int CellCount,
    float AverageCover,
    float MaxCover,
    float AverageMovementSpeedMultiplier);
