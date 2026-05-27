namespace Lineage.Core;

/// <summary>
/// Coarse per-area plant fertility that falls when plants are depleted and recovers over time.
/// </summary>
///
/// <remarks>
/// This is deliberately separate from biome fertility. Biomes describe the long-term
/// ecology of an area; local fertility is short-lived pressure from recent feeding.
/// Creatures do not sense this map by name, but it contributes to local habitat quality
/// and they experience its effect through plant density and regrowth.
/// </remarks>
public sealed class LocalFertilityMap
{
    private readonly float[] _multipliers;
    private readonly bool[] _recoveringFlags;
    private readonly List<int> _recoveringCellIndices = [];

    private LocalFertilityMap(
        WorldBounds bounds,
        bool enabled,
        float cellSize,
        int cellCountX,
        int cellCountY,
        float minimumMultiplier,
        float recoveryPerSecond,
        float depletionPerPlant,
        float neighborDepletionShare,
        float[] multipliers)
    {
        Bounds = bounds;
        Enabled = enabled;
        CellSize = cellSize;
        CellCountX = cellCountX;
        CellCountY = cellCountY;
        MinimumMultiplier = minimumMultiplier;
        RecoveryPerSecond = recoveryPerSecond;
        DepletionPerPlant = depletionPerPlant;
        NeighborDepletionShare = neighborDepletionShare;
        _multipliers = multipliers;
        _recoveringFlags = new bool[_multipliers.Length];

        for (var i = 0; i < _multipliers.Length; i++)
        {
            _multipliers[i] = Math.Clamp(_multipliers[i], MinimumMultiplier, 1f);
            if (_multipliers[i] < 1f)
            {
                TrackRecoveringCell(i);
            }
        }
    }

    public WorldBounds Bounds { get; }

    public bool Enabled { get; }

    public float CellSize { get; }

    public int CellCountX { get; }

    public int CellCountY { get; }

    public float MinimumMultiplier { get; }

    public float RecoveryPerSecond { get; }

    public float DepletionPerPlant { get; }

    public float NeighborDepletionShare { get; }

    public static LocalFertilityMap CreateDisabled(WorldBounds bounds)
    {
        ValidateBounds(bounds);
        return new LocalFertilityMap(
            bounds,
            false,
            MathF.Max(bounds.Width, bounds.Height),
            1,
            1,
            1f,
            0f,
            0f,
            0f,
            [1f]);
    }

    public static LocalFertilityMap Create(
        WorldBounds bounds,
        float cellSize,
        float minimumMultiplier,
        float recoveryPerSecond,
        float depletionPerPlant,
        float neighborDepletionShare)
    {
        ValidateBounds(bounds);
        ValidateCellSize(cellSize);
        ValidateParameters(minimumMultiplier, recoveryPerSecond, depletionPerPlant, neighborDepletionShare);

        var cellCountX = Math.Max(1, (int)MathF.Ceiling(bounds.Width / cellSize));
        var cellCountY = Math.Max(1, (int)MathF.Ceiling(bounds.Height / cellSize));
        return new LocalFertilityMap(
            bounds,
            true,
            cellSize,
            cellCountX,
            cellCountY,
            minimumMultiplier,
            recoveryPerSecond,
            depletionPerPlant,
            neighborDepletionShare,
            Enumerable.Repeat(1f, cellCountX * cellCountY).ToArray());
    }

    public static LocalFertilityMap CreateFromCells(
        WorldBounds bounds,
        bool enabled,
        float cellSize,
        int cellCountX,
        int cellCountY,
        float minimumMultiplier,
        float recoveryPerSecond,
        float depletionPerPlant,
        float neighborDepletionShare,
        IReadOnlyList<float> multipliers)
    {
        ValidateBounds(bounds);
        ValidateCellSize(cellSize);
        ValidateParameters(minimumMultiplier, recoveryPerSecond, depletionPerPlant, neighborDepletionShare);

        if (cellCountX <= 0 || cellCountY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellCountX), "Local fertility cell dimensions must be positive.");
        }

        if (multipliers.Count != cellCountX * cellCountY)
        {
            throw new ArgumentException("Local fertility cell list must match the requested map dimensions.", nameof(multipliers));
        }

        return new LocalFertilityMap(
            bounds,
            enabled,
            cellSize,
            cellCountX,
            cellCountY,
            minimumMultiplier,
            recoveryPerSecond,
            depletionPerPlant,
            neighborDepletionShare,
            multipliers.ToArray());
    }

    public float[] GetCellsCopy()
    {
        return _multipliers.ToArray();
    }

    public float GetMultiplierAt(SimVector2 position)
    {
        if (!Enabled)
        {
            return 1f;
        }

        return _multipliers[ToIndex(position)];
    }

    public void ApplyPlantDepletion(SimVector2 position)
    {
        if (!Enabled || DepletionPerPlant <= 0f)
        {
            return;
        }

        var (centerX, centerY) = ToCell(position);
        for (var y = centerY - 1; y <= centerY + 1; y++)
        {
            if ((uint)y >= (uint)CellCountY)
            {
                continue;
            }

            for (var x = centerX - 1; x <= centerX + 1; x++)
            {
                if ((uint)x >= (uint)CellCountX)
                {
                    continue;
                }

                var factor = DepletionFactor(x - centerX, y - centerY);
                DepleteCell(ToIndex(x, y), DepletionPerPlant * factor);
            }
        }
    }

    public void Recover(float deltaSeconds)
    {
        if (!Enabled || RecoveryPerSecond <= 0f || deltaSeconds <= 0f || _recoveringCellIndices.Count == 0)
        {
            return;
        }

        var recovery = RecoveryPerSecond * deltaSeconds;
        for (var i = _recoveringCellIndices.Count - 1; i >= 0; i--)
        {
            var index = _recoveringCellIndices[i];
            var updated = Math.Min(1f, _multipliers[index] + recovery);
            _multipliers[index] = updated;
            if (updated >= 1f)
            {
                _recoveringFlags[index] = false;
                var last = _recoveringCellIndices.Count - 1;
                _recoveringCellIndices[i] = _recoveringCellIndices[last];
                _recoveringCellIndices.RemoveAt(last);
            }
        }
    }

    public LocalFertilitySummary Summarize()
    {
        if (!Enabled)
        {
            return new LocalFertilitySummary(0, 1f, 1f, 0f);
        }

        var total = 0f;
        var minimum = 1f;
        var depletedCount = 0;
        for (var i = 0; i < _multipliers.Length; i++)
        {
            var value = _multipliers[i];
            total += value;
            minimum = Math.Min(minimum, value);
            if (value < 0.99f)
            {
                depletedCount++;
            }
        }

        return new LocalFertilitySummary(
            _multipliers.Length,
            _multipliers.Length > 0 ? total / _multipliers.Length : 1f,
            minimum,
            _multipliers.Length > 0 ? depletedCount / (float)_multipliers.Length : 0f);
    }

    private void DepleteCell(int index, float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        var updated = Math.Max(MinimumMultiplier, _multipliers[index] - amount);
        if (updated >= _multipliers[index])
        {
            return;
        }

        _multipliers[index] = updated;
        TrackRecoveringCell(index);
    }

    private void TrackRecoveringCell(int index)
    {
        if (_recoveringFlags[index])
        {
            return;
        }

        _recoveringFlags[index] = true;
        _recoveringCellIndices.Add(index);
    }

    private float DepletionFactor(int dx, int dy)
    {
        if (dx == 0 && dy == 0)
        {
            return 1f;
        }

        if (dx == 0 || dy == 0)
        {
            return NeighborDepletionShare;
        }

        return NeighborDepletionShare * 0.5f;
    }

    private int ToIndex(SimVector2 position)
    {
        var (cellX, cellY) = ToCell(position);
        return ToIndex(cellX, cellY);
    }

    private (int X, int Y) ToCell(SimVector2 position)
    {
        var clamped = Bounds.Clamp(position);
        var cellX = Math.Min(CellCountX - 1, Math.Max(0, (int)(clamped.X / CellSize)));
        var cellY = Math.Min(CellCountY - 1, Math.Max(0, (int)(clamped.Y / CellSize)));
        return (cellX, cellY);
    }

    private int ToIndex(int cellX, int cellY)
    {
        return cellY * CellCountX + cellX;
    }

    private static void ValidateBounds(WorldBounds bounds)
    {
        if (!float.IsFinite(bounds.Width) || bounds.Width <= 0f || !float.IsFinite(bounds.Height) || bounds.Height <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(bounds), "Local fertility map bounds must be finite and positive.");
        }
    }

    private static void ValidateCellSize(float cellSize)
    {
        if (!float.IsFinite(cellSize) || cellSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Local fertility cell size must be finite and positive.");
        }
    }

    private static void ValidateParameters(
        float minimumMultiplier,
        float recoveryPerSecond,
        float depletionPerPlant,
        float neighborDepletionShare)
    {
        if (!float.IsFinite(minimumMultiplier) || minimumMultiplier <= 0f || minimumMultiplier > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumMultiplier), "Local fertility minimum must be finite and in (0, 1].");
        }

        if (!float.IsFinite(recoveryPerSecond) || recoveryPerSecond < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(recoveryPerSecond), "Local fertility recovery must be finite and non-negative.");
        }

        if (!float.IsFinite(depletionPerPlant) || depletionPerPlant < 0f || depletionPerPlant > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(depletionPerPlant), "Local fertility depletion must be finite and between 0 and 1.");
        }

        if (!float.IsFinite(neighborDepletionShare) || neighborDepletionShare < 0f || neighborDepletionShare > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(neighborDepletionShare), "Local fertility neighbor depletion share must be finite and between 0 and 1.");
        }
    }
}

public readonly record struct LocalFertilitySummary(
    int CellCount,
    float AverageMultiplier,
    float MinimumMultiplier,
    float DepletedCellShare);
