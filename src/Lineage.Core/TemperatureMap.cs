namespace Lineage.Core;

/// <summary>
/// Coarse cached temperature field for a rectangular world.
/// </summary>
///
/// <remarks>
/// Temperature is intentionally a scalar map lookup, not a simulated entity layer.
/// Values are normalized: 0 is very cold, 0.5 is temperate, and 1 is very hot.
/// </remarks>
public sealed class TemperatureMap
{
    private const ulong TemperatureSalt = 0x74656D705F6D6170UL;
    private const ulong TemperatureDetailSalt = 0x74656D705F643174UL;
    public const float NeutralTemperature = 0.5f;

    private readonly float[] _temperatures;
    private readonly TemperatureMapSummary _summary;

    private TemperatureMap(
        WorldBounds bounds,
        bool enabled,
        float cellSize,
        int cellCountX,
        int cellCountY,
        float[] temperatures)
    {
        Bounds = bounds;
        Enabled = enabled;
        CellSize = cellSize;
        CellCountX = cellCountX;
        CellCountY = cellCountY;
        _temperatures = temperatures;

        for (var i = 0; i < _temperatures.Length; i++)
        {
            _temperatures[i] = Clamp01(_temperatures[i]);
        }

        _summary = CalculateSummary(_temperatures);
    }

    public WorldBounds Bounds { get; }

    public bool Enabled { get; }

    public float CellSize { get; }

    public int CellCountX { get; }

    public int CellCountY { get; }

    public static TemperatureMap CreateNeutral(WorldBounds bounds)
    {
        ValidateBounds(bounds);
        return new TemperatureMap(
            bounds,
            enabled: false,
            MathF.Max(bounds.Width, bounds.Height),
            1,
            1,
            [NeutralTemperature]);
    }

    public static TemperatureMap GenerateFromBiomes(BiomeMap biomes, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(biomes);

        var temperatures = new float[biomes.CellCountX * biomes.CellCountY];
        for (var y = 0; y < biomes.CellCountY; y++)
        {
            for (var x = 0; x < biomes.CellCountX; x++)
            {
                var cell = biomes.GetCellBounds(x, y);
                var centerY = cell.Y + cell.Height * 0.5f;
                var yPhase = biomes.Bounds.Height > 0f
                    ? Math.Clamp(centerY / biomes.Bounds.Height, 0f, 1f)
                    : 0.5f;
                var latitudeTemperature = 0.18f + yPhase * 0.64f;
                var regionalNoise = FractalNoise(
                    x * 0.055f + 31.0f,
                    y * 0.055f + 5.0f,
                    seed ^ TemperatureSalt);
                var detailNoise = FractalNoise(
                    x * 0.17f - 8.0f,
                    y * 0.17f + 19.0f,
                    seed ^ TemperatureDetailSalt);
                var variation = (regionalNoise - 0.5f) * 0.16f + (detailNoise - 0.5f) * 0.05f;
                var biomeAdjustment = TemperatureAdjustmentForBiome(biomes.GetKind(x, y));
                temperatures[y * biomes.CellCountX + x] = Clamp01(latitudeTemperature + variation + biomeAdjustment);
            }
        }

        return new TemperatureMap(
            biomes.Bounds,
            enabled: true,
            biomes.CellSize,
            biomes.CellCountX,
            biomes.CellCountY,
            temperatures);
    }

    public static TemperatureMap CreateFromCells(
        WorldBounds bounds,
        bool enabled,
        float cellSize,
        int cellCountX,
        int cellCountY,
        IReadOnlyList<float> temperatures)
    {
        ValidateBounds(bounds);
        ValidateCellSize(cellSize);

        if (cellCountX <= 0 || cellCountY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellCountX), "Temperature cell dimensions must be positive.");
        }

        if (temperatures.Count != cellCountX * cellCountY)
        {
            throw new ArgumentException("Temperature cell list must match the requested map dimensions.", nameof(temperatures));
        }

        return new TemperatureMap(
            bounds,
            enabled,
            cellSize,
            cellCountX,
            cellCountY,
            temperatures.ToArray());
    }

    public float[] GetCellsCopy()
    {
        return _temperatures.ToArray();
    }

    public float GetTemperature(int cellX, int cellY)
    {
        if ((uint)cellX >= (uint)CellCountX || (uint)cellY >= (uint)CellCountY)
        {
            throw new ArgumentOutOfRangeException(nameof(cellX), "Temperature cell coordinate is outside the map.");
        }

        return _temperatures[ToIndex(cellX, cellY)];
    }

    public float GetTemperatureAt(SimVector2 position)
    {
        return _temperatures[ToIndex(position)];
    }

    public BiomeCellBounds GetCellBounds(int cellX, int cellY)
    {
        if ((uint)cellX >= (uint)CellCountX || (uint)cellY >= (uint)CellCountY)
        {
            throw new ArgumentOutOfRangeException(nameof(cellX), "Temperature cell coordinate is outside the map.");
        }

        var x = cellX * CellSize;
        var y = cellY * CellSize;
        return new BiomeCellBounds(
            x,
            y,
            Math.Min(CellSize, Bounds.Width - x),
            Math.Min(CellSize, Bounds.Height - y));
    }

    public TemperatureMapSummary Summarize()
    {
        return _summary;
    }

    private int ToIndex(SimVector2 position)
    {
        var clamped = Bounds.Clamp(position);
        var cellX = Math.Min(CellCountX - 1, Math.Max(0, (int)(clamped.X / CellSize)));
        var cellY = Math.Min(CellCountY - 1, Math.Max(0, (int)(clamped.Y / CellSize)));
        return ToIndex(cellX, cellY);
    }

    private int ToIndex(int cellX, int cellY)
    {
        return cellY * CellCountX + cellX;
    }

    private static float TemperatureAdjustmentForBiome(BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => 0.18f,
            BiomeKind.Scrubland => 0.08f,
            BiomeKind.Forest => -0.05f,
            BiomeKind.Wetland => -0.02f,
            BiomeKind.Tundra => -0.30f,
            BiomeKind.Highland => -0.18f,
            _ => 0f
        };
    }

    private static TemperatureMapSummary CalculateSummary(IReadOnlyList<float> temperatures)
    {
        if (temperatures.Count == 0)
        {
            return new TemperatureMapSummary(0, NeutralTemperature, NeutralTemperature, NeutralTemperature);
        }

        var total = 0f;
        var minimum = 1f;
        var maximum = 0f;
        for (var i = 0; i < temperatures.Count; i++)
        {
            var value = temperatures[i];
            total += value;
            minimum = Math.Min(minimum, value);
            maximum = Math.Max(maximum, value);
        }

        return new TemperatureMapSummary(
            temperatures.Count,
            total / temperatures.Count,
            minimum,
            maximum);
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

    private static float Clamp01(float value)
    {
        return float.IsFinite(value)
            ? Math.Clamp(value, 0f, 1f)
            : NeutralTemperature;
    }

    private static void ValidateBounds(WorldBounds bounds)
    {
        if (!float.IsFinite(bounds.Width) || bounds.Width <= 0f || !float.IsFinite(bounds.Height) || bounds.Height <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(bounds), "Temperature map bounds must be finite and positive.");
        }
    }

    private static void ValidateCellSize(float cellSize)
    {
        if (!float.IsFinite(cellSize) || cellSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Temperature cell size must be finite and positive.");
        }
    }
}

public readonly record struct TemperatureMapSummary(
    int CellCount,
    float AverageTemperature,
    float MinimumTemperature,
    float MaximumTemperature);
