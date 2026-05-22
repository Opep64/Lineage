namespace Lineage.Core;

/// <summary>
/// Low-resolution deterministic biome map for a rectangular world.
/// </summary>
///
/// <remarks>
/// The map is intentionally coarse: it gives large worlds ecological structure
/// without turning every simulation tick into terrain work.
/// </remarks>
public sealed class BiomeMap
{
    private const ulong FertilitySalt = 0x62696F6D655F6631UL;
    private const ulong MoistureSalt = 0x62696F6D655F6D32UL;

    private readonly BiomeKind[] _cells;
    private readonly float[] _resourceSpawnWeightPrefix;
    private readonly float _totalResourceSpawnWeight;

    private BiomeMap(
        WorldBounds bounds,
        float cellSize,
        int cellCountX,
        int cellCountY,
        BiomeKind[] cells,
        float resourceVoidBorderWidth)
    {
        Bounds = bounds;
        CellSize = cellSize;
        CellCountX = cellCountX;
        CellCountY = cellCountY;
        ResourceVoidBorderWidth = resourceVoidBorderWidth;
        _cells = cells;
        _resourceSpawnWeightPrefix = new float[_cells.Length];

        var totalWeight = 0f;
        for (var y = 0; y < CellCountY; y++)
        {
            for (var x = 0; x < CellCountX; x++)
            {
                var index = ToIndex(x, y);
                var cell = GetCellBounds(x, y);
                var fertileCell = GetResourceSpawnBounds(cell);
                totalWeight += fertileCell.Area * GetResourceDensityMultiplier(_cells[index]);
                _resourceSpawnWeightPrefix[index] = totalWeight;
            }
        }

        _totalResourceSpawnWeight = totalWeight;
    }

    public WorldBounds Bounds { get; }

    public float CellSize { get; }

    public int CellCountX { get; }

    public int CellCountY { get; }

    public float ResourceVoidBorderWidth { get; }

    public static BiomeMap CreateUniform(
        WorldBounds bounds,
        float cellSize,
        BiomeKind kind,
        float resourceVoidBorderWidth = 0f)
    {
        ValidateBounds(bounds);
        ValidateCellSize(cellSize);
        ValidateResourceVoidBorderWidth(bounds, resourceVoidBorderWidth);

        var cellCountX = Math.Max(1, (int)MathF.Ceiling(bounds.Width / cellSize));
        var cellCountY = Math.Max(1, (int)MathF.Ceiling(bounds.Height / cellSize));
        var cells = Enumerable.Repeat(kind, cellCountX * cellCountY).ToArray();
        return new BiomeMap(bounds, cellSize, cellCountX, cellCountY, cells, resourceVoidBorderWidth);
    }

    public static BiomeMap CreateFromCells(
        WorldBounds bounds,
        float cellSize,
        int cellCountX,
        int cellCountY,
        IReadOnlyList<BiomeKind> cells,
        float resourceVoidBorderWidth = 0f)
    {
        ValidateBounds(bounds);
        ValidateCellSize(cellSize);
        ValidateResourceVoidBorderWidth(bounds, resourceVoidBorderWidth);

        if (cellCountX <= 0 || cellCountY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellCountX), "Biome cell dimensions must be positive.");
        }

        if (cells.Count != cellCountX * cellCountY)
        {
            throw new ArgumentException("Biome cell list must match the requested map dimensions.", nameof(cells));
        }

        return new BiomeMap(bounds, cellSize, cellCountX, cellCountY, cells.ToArray(), resourceVoidBorderWidth);
    }

    public static BiomeMap Generate(WorldBounds bounds, float cellSize, ulong seed, float resourceVoidBorderWidth = 0f)
    {
        ValidateBounds(bounds);
        ValidateCellSize(cellSize);
        ValidateResourceVoidBorderWidth(bounds, resourceVoidBorderWidth);

        var cellCountX = Math.Max(1, (int)MathF.Ceiling(bounds.Width / cellSize));
        var cellCountY = Math.Max(1, (int)MathF.Ceiling(bounds.Height / cellSize));
        var scoredCells = new ScoredCell[cellCountX * cellCountY];
        var cells = new BiomeKind[cellCountX * cellCountY];

        for (var y = 0; y < cellCountY; y++)
        {
            for (var x = 0; x < cellCountX; x++)
            {
                var fertility = FractalNoise(x * 0.34f + 8.5f, y * 0.34f - 3.25f, seed ^ FertilitySalt);
                var moisture = FractalNoise(x * 0.27f - 5.0f, y * 0.27f + 11.0f, seed ^ MoistureSalt);
                var score = fertility * 0.72f + moisture * 0.28f;
                var index = y * cellCountX + x;
                scoredCells[index] = new ScoredCell(index, score);
            }
        }

        Array.Sort(scoredCells, static (left, right) => left.Score.CompareTo(right.Score));
        for (var rank = 0; rank < scoredCells.Length; rank++)
        {
            cells[scoredCells[rank].Index] = BiomeKindForRank(rank, scoredCells.Length);
        }

        return new BiomeMap(bounds, cellSize, cellCountX, cellCountY, cells, resourceVoidBorderWidth);
    }

    public BiomeKind GetKind(int cellX, int cellY)
    {
        if ((uint)cellX >= (uint)CellCountX || (uint)cellY >= (uint)CellCountY)
        {
            throw new ArgumentOutOfRangeException(nameof(cellX), "Biome cell coordinate is outside the map.");
        }

        return _cells[ToIndex(cellX, cellY)];
    }

    public BiomeKind[] GetCellsCopy()
    {
        return _cells.ToArray();
    }

    public BiomeKind GetKindAt(SimVector2 position)
    {
        var clamped = Bounds.Clamp(position);
        var cellX = Math.Min(CellCountX - 1, Math.Max(0, (int)(clamped.X / CellSize)));
        var cellY = Math.Min(CellCountY - 1, Math.Max(0, (int)(clamped.Y / CellSize)));
        return GetKind(cellX, cellY);
    }

    public bool IsInResourceVoid(SimVector2 position)
    {
        if (ResourceVoidBorderWidth <= 0f)
        {
            return false;
        }

        var clamped = Bounds.Clamp(position);
        return clamped.X < ResourceVoidBorderWidth
            || clamped.Y < ResourceVoidBorderWidth
            || clamped.X > Bounds.Width - ResourceVoidBorderWidth
            || clamped.Y > Bounds.Height - ResourceVoidBorderWidth;
    }

    public BiomeCellBounds GetResourceSpawnBounds()
    {
        return new BiomeCellBounds(
            ResourceVoidBorderWidth,
            ResourceVoidBorderWidth,
            Math.Max(0f, Bounds.Width - ResourceVoidBorderWidth * 2f),
            Math.Max(0f, Bounds.Height - ResourceVoidBorderWidth * 2f));
    }

    public BiomeCellBounds GetCellBounds(int cellX, int cellY)
    {
        if ((uint)cellX >= (uint)CellCountX || (uint)cellY >= (uint)CellCountY)
        {
            throw new ArgumentOutOfRangeException(nameof(cellX), "Biome cell coordinate is outside the map.");
        }

        var x = cellX * CellSize;
        var y = cellY * CellSize;
        return new BiomeCellBounds(
            x,
            y,
            Math.Min(CellSize, Bounds.Width - x),
            Math.Min(CellSize, Bounds.Height - y));
    }

    private BiomeCellBounds GetResourceSpawnBounds(BiomeCellBounds cell)
    {
        if (ResourceVoidBorderWidth <= 0f)
        {
            return cell;
        }

        var resourceArea = GetResourceSpawnBounds();
        var left = Math.Max(cell.X, resourceArea.X);
        var top = Math.Max(cell.Y, resourceArea.Y);
        var right = Math.Min(cell.X + cell.Width, resourceArea.X + resourceArea.Width);
        var bottom = Math.Min(cell.Y + cell.Height, resourceArea.Y + resourceArea.Height);
        return new BiomeCellBounds(
            left,
            top,
            Math.Max(0f, right - left),
            Math.Max(0f, bottom - top));
    }

    public SimVector2 SampleResourcePosition(DeterministicRandom random)
    {
        if (_totalResourceSpawnWeight <= 0f)
        {
            return new SimVector2(
                random.NextSingle(0f, Bounds.Width),
                random.NextSingle(0f, Bounds.Height));
        }

        var roll = random.NextSingle(0f, _totalResourceSpawnWeight);
        var index = Array.BinarySearch(_resourceSpawnWeightPrefix, roll);
        if (index < 0)
        {
            index = ~index;
        }

        index = Math.Clamp(index, 0, _cells.Length - 1);
        var cellX = index % CellCountX;
        var cellY = index / CellCountX;
        var bounds = GetResourceSpawnBounds(GetCellBounds(cellX, cellY));
        return new SimVector2(
            random.NextSingle(bounds.X, bounds.X + bounds.Width),
            random.NextSingle(bounds.Y, bounds.Y + bounds.Height));
    }

    public float GetResourceDensityMultiplierAt(SimVector2 position)
    {
        if (IsInResourceVoid(position))
        {
            return 0f;
        }

        return GetResourceDensityMultiplier(GetKindAt(position));
    }

    public float GetResourceRegrowthMultiplierAt(SimVector2 position)
    {
        if (IsInResourceVoid(position))
        {
            return 0f;
        }

        return GetResourceRegrowthMultiplier(GetKindAt(position));
    }

    public IReadOnlyList<BiomeSummary> SummarizeResources(IReadOnlyList<ResourcePatchState> resources)
    {
        var summaries = Enum.GetValues<BiomeKind>()
            .Select(kind => new MutableBiomeSummary(kind))
            .ToArray();

        for (var y = 0; y < CellCountY; y++)
        {
            for (var x = 0; x < CellCountX; x++)
            {
                var kind = GetKind(x, y);
                var cell = GetCellBounds(x, y);
                var summary = summaries[(int)kind];
                summary.CellCount++;
                summary.Area += cell.Area;
            }
        }

        foreach (var resource in resources)
        {
            if (resource.Kind != ResourceKind.Plant || resource.Calories <= 0f)
            {
                continue;
            }

            var summary = summaries[(int)GetKindAt(resource.Position)];
            summary.ResourceCount++;
            summary.ResourceCalories += resource.Calories;
            summary.ResourceCapacity += resource.MaxCalories;
        }

        return summaries
            .Select(summary => new BiomeSummary(
                summary.Kind,
                summary.CellCount,
                summary.Area,
                GetResourceDensityMultiplier(summary.Kind),
                GetResourceRegrowthMultiplier(summary.Kind),
                summary.ResourceCount,
                summary.ResourceCalories,
                summary.ResourceCapacity))
            .ToArray();
    }

    public static float GetResourceDensityMultiplier(BiomeKind kind)
    {
        return kind switch
        {
            BiomeKind.Barren => 0.05f,
            BiomeKind.Sparse => 0.35f,
            BiomeKind.Grassland => 1f,
            BiomeKind.Rich => 2.4f,
            _ => 1f
        };
    }

    public static float GetResourceRegrowthMultiplier(BiomeKind kind)
    {
        return kind switch
        {
            BiomeKind.Barren => 0.25f,
            BiomeKind.Sparse => 0.65f,
            BiomeKind.Grassland => 1f,
            BiomeKind.Rich => 1.5f,
            _ => 1f
        };
    }

    private int ToIndex(int cellX, int cellY)
    {
        return cellY * CellCountX + cellX;
    }

    private static BiomeKind BiomeKindForRank(int rank, int count)
    {
        if (count <= 1)
        {
            return BiomeKind.Grassland;
        }

        var percentile = (rank + 0.5f) / count;
        return percentile switch
        {
            < 0.16f => BiomeKind.Barren,
            < 0.42f => BiomeKind.Sparse,
            > 0.82f => BiomeKind.Rich,
            _ => BiomeKind.Grassland
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
            throw new ArgumentOutOfRangeException(nameof(bounds), "Biome map bounds must be finite and positive.");
        }
    }

    private static void ValidateCellSize(float cellSize)
    {
        if (!float.IsFinite(cellSize) || cellSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Biome cell size must be finite and positive.");
        }
    }

    private static void ValidateResourceVoidBorderWidth(WorldBounds bounds, float resourceVoidBorderWidth)
    {
        if (!float.IsFinite(resourceVoidBorderWidth) || resourceVoidBorderWidth < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(resourceVoidBorderWidth), "Resource void border width must be finite and non-negative.");
        }

        if (resourceVoidBorderWidth * 2f >= Math.Min(bounds.Width, bounds.Height))
        {
            throw new ArgumentOutOfRangeException(nameof(resourceVoidBorderWidth), "Resource void border width must leave a positive resource-spawn area.");
        }
    }

    private sealed class MutableBiomeSummary(BiomeKind kind)
    {
        public BiomeKind Kind { get; } = kind;

        public int CellCount;

        public float Area;

        public int ResourceCount;

        public float ResourceCalories;

        public float ResourceCapacity;
    }

    private readonly record struct ScoredCell(int Index, float Score);
}

public readonly record struct BiomeCellBounds(float X, float Y, float Width, float Height)
{
    public float Area => Width * Height;
}

public readonly record struct BiomeSummary(
    BiomeKind Kind,
    int CellCount,
    float Area,
    float ResourceDensityMultiplier,
    float ResourceRegrowthMultiplier,
    int ResourceCount,
    float ResourceCalories,
    float ResourceCapacity);
