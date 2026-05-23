namespace Lineage.Core;

/// <summary>
/// Coarse static collision map for terrain features that block movement.
/// </summary>
///
/// <remarks>
/// Obstacles are intentionally grid-based and immutable during a run. That keeps
/// checks cheap for thousands of creatures while still giving the world hard shape.
/// </remarks>
public sealed class ObstacleMap
{
    private const ulong ScatterSalt = 0x6F62737461636C65UL;
    private const float ScatteredRockProbability = 0.07f;

    private readonly bool[] _blockedCells;

    private ObstacleMap(
        WorldBounds bounds,
        float cellSize,
        int cellCountX,
        int cellCountY,
        bool[] blockedCells)
    {
        Bounds = bounds;
        CellSize = cellSize;
        CellCountX = cellCountX;
        CellCountY = cellCountY;
        _blockedCells = blockedCells;

        var count = 0;
        for (var i = 0; i < _blockedCells.Length; i++)
        {
            if (_blockedCells[i])
            {
                count++;
            }
        }

        BlockedCellCount = count;
    }

    public WorldBounds Bounds { get; }

    public float CellSize { get; }

    public int CellCountX { get; }

    public int CellCountY { get; }

    public int BlockedCellCount { get; }

    public bool HasObstacles => BlockedCellCount > 0;

    public static ObstacleMap CreateEmpty(WorldBounds bounds, float cellSize)
    {
        ValidateBounds(bounds);
        ValidateCellSize(cellSize);

        var cellCountX = Math.Max(1, (int)MathF.Ceiling(bounds.Width / cellSize));
        var cellCountY = Math.Max(1, (int)MathF.Ceiling(bounds.Height / cellSize));
        return new ObstacleMap(bounds, cellSize, cellCountX, cellCountY, new bool[cellCountX * cellCountY]);
    }

    public static ObstacleMap CreateFromCells(
        WorldBounds bounds,
        float cellSize,
        int cellCountX,
        int cellCountY,
        IReadOnlyList<bool> blockedCells)
    {
        ValidateBounds(bounds);
        ValidateCellSize(cellSize);

        if (cellCountX <= 0 || cellCountY <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cellCountX), "Obstacle cell dimensions must be positive.");
        }

        if (blockedCells.Count != cellCountX * cellCountY)
        {
            throw new ArgumentException("Obstacle cell list must match the requested map dimensions.", nameof(blockedCells));
        }

        return new ObstacleMap(bounds, cellSize, cellCountX, cellCountY, blockedCells.ToArray());
    }

    public static ObstacleMap Generate(
        WorldBounds bounds,
        float cellSize,
        ObstacleMapKind kind,
        ulong seed)
    {
        ValidateBounds(bounds);
        ValidateCellSize(cellSize);

        if (kind == ObstacleMapKind.None)
        {
            return CreateEmpty(bounds, cellSize);
        }

        var cellCountX = Math.Max(1, (int)MathF.Ceiling(bounds.Width / cellSize));
        var cellCountY = Math.Max(1, (int)MathF.Ceiling(bounds.Height / cellSize));
        var cells = new bool[cellCountX * cellCountY];

        switch (kind)
        {
            case ObstacleMapKind.VerticalBarrierWithGaps:
                AddVerticalBarrierWithGaps(cells, cellCountX, cellCountY);
                break;
            case ObstacleMapKind.HorizontalBarrierWithGaps:
                AddHorizontalBarrierWithGaps(cells, cellCountX, cellCountY);
                break;
            case ObstacleMapKind.ScatteredRocks:
                AddScatteredRocks(cells, cellCountX, cellCountY, seed);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported obstacle map kind.");
        }

        return new ObstacleMap(bounds, cellSize, cellCountX, cellCountY, cells);
    }

    public bool IsBlocked(int cellX, int cellY)
    {
        if ((uint)cellX >= (uint)CellCountX || (uint)cellY >= (uint)CellCountY)
        {
            throw new ArgumentOutOfRangeException(nameof(cellX), "Obstacle cell coordinate is outside the map.");
        }

        return _blockedCells[ToIndex(cellX, cellY)];
    }

    public bool[] GetCellsCopy()
    {
        return _blockedCells.ToArray();
    }

    public bool IsBlockedAt(SimVector2 position)
    {
        if (!Bounds.Contains(position))
        {
            return true;
        }

        var cellX = Math.Min(CellCountX - 1, Math.Max(0, (int)(position.X / CellSize)));
        var cellY = Math.Min(CellCountY - 1, Math.Max(0, (int)(position.Y / CellSize)));
        return IsBlocked(cellX, cellY);
    }

    public bool IsBlockedForCircle(SimVector2 center, float radius)
    {
        if (!center.IsFinite)
        {
            return true;
        }

        if (!Bounds.Contains(center))
        {
            return true;
        }

        if (!HasObstacles)
        {
            return false;
        }

        var safeRadius = MathF.Max(0f, radius);
        var minX = ClampCellIndex((int)MathF.Floor((center.X - safeRadius) / CellSize), CellCountX);
        var minY = ClampCellIndex((int)MathF.Floor((center.Y - safeRadius) / CellSize), CellCountY);
        var maxX = ClampCellIndex((int)MathF.Floor((center.X + safeRadius) / CellSize), CellCountX);
        var maxY = ClampCellIndex((int)MathF.Floor((center.Y + safeRadius) / CellSize), CellCountY);

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                if (!IsBlocked(x, y))
                {
                    continue;
                }

                var cell = GetCellBounds(x, y);
                if (CircleIntersectsCell(center, safeRadius, cell))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public BiomeCellBounds GetCellBounds(int cellX, int cellY)
    {
        if ((uint)cellX >= (uint)CellCountX || (uint)cellY >= (uint)CellCountY)
        {
            throw new ArgumentOutOfRangeException(nameof(cellX), "Obstacle cell coordinate is outside the map.");
        }

        var x = cellX * CellSize;
        var y = cellY * CellSize;
        return new BiomeCellBounds(
            x,
            y,
            Math.Min(CellSize, Bounds.Width - x),
            Math.Min(CellSize, Bounds.Height - y));
    }

    private int ToIndex(int cellX, int cellY)
    {
        return cellY * CellCountX + cellX;
    }

    private static void AddVerticalBarrierWithGaps(bool[] cells, int cellCountX, int cellCountY)
    {
        var barrierX = cellCountX / 2;
        var gapRadius = Math.Max(1, cellCountY / 14);

        for (var y = 0; y < cellCountY; y++)
        {
            if (IsNearGap(y, cellCountY, gapRadius))
            {
                continue;
            }

            cells[y * cellCountX + barrierX] = true;
        }
    }

    private static void AddHorizontalBarrierWithGaps(bool[] cells, int cellCountX, int cellCountY)
    {
        var barrierY = cellCountY / 2;
        var gapRadius = Math.Max(1, cellCountX / 14);

        for (var x = 0; x < cellCountX; x++)
        {
            if (IsNearGap(x, cellCountX, gapRadius))
            {
                continue;
            }

            cells[barrierY * cellCountX + x] = true;
        }
    }

    private static bool IsNearGap(int index, int count, int gapRadius)
    {
        if (count <= 3)
        {
            return true;
        }

        return IsNear(index, count / 4, gapRadius)
            || IsNear(index, count / 2, gapRadius)
            || IsNear(index, count * 3 / 4, gapRadius);
    }

    private static bool IsNear(int index, int center, int radius)
    {
        return Math.Abs(index - center) <= radius;
    }

    private static void AddScatteredRocks(bool[] cells, int cellCountX, int cellCountY, ulong seed)
    {
        for (var y = 0; y < cellCountY; y++)
        {
            for (var x = 0; x < cellCountX; x++)
            {
                // Keep the outer edge open so boundary pressure and obstacle pressure do not merge.
                if (x == 0 || y == 0 || x == cellCountX - 1 || y == cellCountY - 1)
                {
                    continue;
                }

                if (HashToUnit(x, y, seed ^ ScatterSalt) < ScatteredRockProbability)
                {
                    cells[y * cellCountX + x] = true;
                }
            }
        }
    }

    private static bool CircleIntersectsCell(SimVector2 center, float radius, BiomeCellBounds cell)
    {
        var closestX = Math.Clamp(center.X, cell.X, cell.X + cell.Width);
        var closestY = Math.Clamp(center.Y, cell.Y, cell.Y + cell.Height);
        var dx = center.X - closestX;
        var dy = center.Y - closestY;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static int ClampCellIndex(int value, int cellCount)
    {
        return Math.Clamp(value, 0, Math.Max(0, cellCount - 1));
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

    private static void ValidateBounds(WorldBounds bounds)
    {
        if (!float.IsFinite(bounds.Width) || bounds.Width <= 0f || !float.IsFinite(bounds.Height) || bounds.Height <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(bounds), "Obstacle map bounds must be finite and positive.");
        }
    }

    private static void ValidateCellSize(float cellSize)
    {
        if (!float.IsFinite(cellSize) || cellSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Obstacle cell size must be finite and positive.");
        }
    }
}
