namespace Lineage.Core;

/// <summary>
/// Uniform grid for local entity/resource queries.
/// </summary>
///
/// <remarks>
/// This is the first performance guardrail for the simulator. Systems that need
/// local perception should query this index instead of scanning the entire world.
/// </remarks>
public sealed class UniformSpatialIndex
{
    private readonly Dictionary<long, SpatialCell> _cells = [];

    public UniformSpatialIndex(float cellSize)
    {
        if (!float.IsFinite(cellSize) || cellSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be finite and positive.");
        }

        CellSize = cellSize;
    }

    public float CellSize { get; }

    public void Rebuild(WorldState state)
    {
        foreach (var cell in _cells.Values)
        {
            cell.Clear();
        }

        for (var i = 0; i < state.Resources.Count; i++)
        {
            var resource = state.Resources[i];
            AddResourceToCells(i, resource.Position, resource.Radius);
        }

        for (var i = 0; i < state.Eggs.Count; i++)
        {
            var egg = state.Eggs[i];
            AddEggToCells(i, egg.Position, EggPredation.ContactRadius(egg));
        }

        for (var i = 0; i < state.Creatures.Count; i++)
        {
            AddCreatureToCell(i, state.Creatures[i].Position);
        }
    }

    public void RebuildCreatures(WorldState state)
    {
        foreach (var cell in _cells.Values)
        {
            cell.CreatureIndices.Clear();
        }

        for (var i = 0; i < state.Creatures.Count; i++)
        {
            AddCreatureToCell(i, state.Creatures[i].Position);
        }
    }

    public int FindNearestResourceWithCalories(
        WorldState state,
        SimVector2 position,
        float radius,
        float minimumCalories = 0f)
    {
        if (radius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Query radius cannot be negative.");
        }

        var bestIndex = -1;
        var bestDistanceSquared = float.PositiveInfinity;

        var minCellX = ToCell(position.X - radius);
        var maxCellX = ToCell(position.X + radius);
        var minCellY = ToCell(position.Y - radius);
        var maxCellY = ToCell(position.Y + radius);

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                if (!_cells.TryGetValue(MakeKey(cellX, cellY), out var cell))
                {
                    continue;
                }

                for (var i = 0; i < cell.ResourceIndices.Count; i++)
                {
                    var resourceIndex = cell.ResourceIndices[i];
                    var resource = state.Resources[resourceIndex];

                    if (resource.Calories <= minimumCalories)
                    {
                        continue;
                    }

                    var distanceSquared = (resource.Position - position).LengthSquared;
                    var contactRadius = radius + resource.Radius;
                    if (distanceSquared > contactRadius * contactRadius)
                    {
                        continue;
                    }

                    if (distanceSquared < bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceSquared;
                        bestIndex = resourceIndex;
                    }
                }
            }
        }

        return bestIndex;
    }

    public int FindNearestEggWithEnergy(
        WorldState state,
        SimVector2 position,
        float radius,
        float minimumEnergy = 0f)
    {
        if (radius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Query radius cannot be negative.");
        }

        var bestIndex = -1;
        var bestDistanceSquared = float.PositiveInfinity;

        var minCellX = ToCell(position.X - radius);
        var maxCellX = ToCell(position.X + radius);
        var minCellY = ToCell(position.Y - radius);
        var maxCellY = ToCell(position.Y + radius);

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                if (!_cells.TryGetValue(MakeKey(cellX, cellY), out var cell))
                {
                    continue;
                }

                for (var i = 0; i < cell.EggIndices.Count; i++)
                {
                    var eggIndex = cell.EggIndices[i];
                    var egg = state.Eggs[eggIndex];
                    if (egg.Energy <= minimumEnergy || egg.Health <= 0f)
                    {
                        continue;
                    }

                    var contactRadius = radius + EggPredation.ContactRadius(egg);
                    var distanceSquared = (egg.Position - position).LengthSquared;
                    if (distanceSquared > contactRadius * contactRadius)
                    {
                        continue;
                    }

                    if (distanceSquared < bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceSquared;
                        bestIndex = eggIndex;
                    }
                }
            }
        }

        return bestIndex;
    }

    public void AddResourceCandidatesWithCalories(
        WorldState state,
        SimVector2 position,
        float radius,
        float minimumCalories,
        List<int> results,
        HashSet<int> seen)
    {
        if (radius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Query radius cannot be negative.");
        }

        results.Clear();
        seen.Clear();

        var minCellX = ToCell(position.X - radius);
        var maxCellX = ToCell(position.X + radius);
        var minCellY = ToCell(position.Y - radius);
        var maxCellY = ToCell(position.Y + radius);

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                if (!_cells.TryGetValue(MakeKey(cellX, cellY), out var cell))
                {
                    continue;
                }

                for (var i = 0; i < cell.ResourceIndices.Count; i++)
                {
                    var resourceIndex = cell.ResourceIndices[i];
                    if (!seen.Add(resourceIndex))
                    {
                        continue;
                    }

                    var resource = state.Resources[resourceIndex];
                    if (resource.Calories <= minimumCalories)
                    {
                        continue;
                    }

                    var contactRadius = radius + resource.Radius;
                    if ((resource.Position - position).LengthSquared <= contactRadius * contactRadius)
                    {
                        results.Add(resourceIndex);
                    }
                }
            }
        }
    }

    public void AddEggCandidatesWithEnergy(
        WorldState state,
        SimVector2 position,
        float radius,
        float minimumEnergy,
        List<int> results,
        HashSet<int> seen)
    {
        if (radius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Query radius cannot be negative.");
        }

        results.Clear();
        seen.Clear();

        var minCellX = ToCell(position.X - radius);
        var maxCellX = ToCell(position.X + radius);
        var minCellY = ToCell(position.Y - radius);
        var maxCellY = ToCell(position.Y + radius);

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                if (!_cells.TryGetValue(MakeKey(cellX, cellY), out var cell))
                {
                    continue;
                }

                for (var i = 0; i < cell.EggIndices.Count; i++)
                {
                    var eggIndex = cell.EggIndices[i];
                    if (!seen.Add(eggIndex))
                    {
                        continue;
                    }

                    var egg = state.Eggs[eggIndex];
                    if (egg.Energy <= minimumEnergy || egg.Health <= 0f)
                    {
                        continue;
                    }

                    var contactRadius = radius + EggPredation.ContactRadius(egg);
                    if ((egg.Position - position).LengthSquared <= contactRadius * contactRadius)
                    {
                        results.Add(eggIndex);
                    }
                }
            }
        }
    }

    public void AddCreatureCandidates(
        WorldState state,
        SimVector2 position,
        float radius,
        List<int> results)
    {
        if (radius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Query radius cannot be negative.");
        }

        results.Clear();
        var radiusSquared = radius * radius;

        var minCellX = ToCell(position.X - radius);
        var maxCellX = ToCell(position.X + radius);
        var minCellY = ToCell(position.Y - radius);
        var maxCellY = ToCell(position.Y + radius);

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                if (!_cells.TryGetValue(MakeKey(cellX, cellY), out var cell))
                {
                    continue;
                }

                for (var i = 0; i < cell.CreatureIndices.Count; i++)
                {
                    var creatureIndex = cell.CreatureIndices[i];
                    var creature = state.Creatures[creatureIndex];
                    var distanceSquared = (creature.Position - position).LengthSquared;
                    if (distanceSquared <= radiusSquared)
                    {
                        results.Add(creatureIndex);
                    }
                }
            }
        }
    }

    public void AddCreatureCandidates(
        WorldState state,
        SimVector2 position,
        float radius,
        List<int> results,
        HashSet<int> seen)
    {
        if (radius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Query radius cannot be negative.");
        }

        seen.Clear();
        AddCreatureCandidates(state, position, radius, results);
    }

    private void AddResourceToCells(int resourceIndex, SimVector2 position, float radius)
    {
        var minCellX = ToCell(position.X - radius);
        var maxCellX = ToCell(position.X + radius);
        var minCellY = ToCell(position.Y - radius);
        var maxCellY = ToCell(position.Y + radius);

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                GetOrCreateCell(cellX, cellY).ResourceIndices.Add(resourceIndex);
            }
        }
    }

    private void AddEggToCells(int eggIndex, SimVector2 position, float radius)
    {
        var minCellX = ToCell(position.X - radius);
        var maxCellX = ToCell(position.X + radius);
        var minCellY = ToCell(position.Y - radius);
        var maxCellY = ToCell(position.Y + radius);

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                GetOrCreateCell(cellX, cellY).EggIndices.Add(eggIndex);
            }
        }
    }

    private void AddCreatureToCell(int creatureIndex, SimVector2 position)
    {
        var cellX = ToCell(position.X);
        var cellY = ToCell(position.Y);
        GetOrCreateCell(cellX, cellY).CreatureIndices.Add(creatureIndex);
    }

    private SpatialCell GetOrCreateCell(int cellX, int cellY)
    {
        var key = MakeKey(cellX, cellY);
        if (!_cells.TryGetValue(key, out var cell))
        {
            cell = new SpatialCell();
            _cells.Add(key, cell);
        }

        return cell;
    }

    private int ToCell(float coordinate)
    {
        return (int)MathF.Floor(coordinate / CellSize);
    }

    private static long MakeKey(int cellX, int cellY)
    {
        return ((long)cellX << 32) ^ (uint)cellY;
    }

    private sealed class SpatialCell
    {
        public List<int> CreatureIndices { get; } = [];

        public List<int> ResourceIndices { get; } = [];

        public List<int> EggIndices { get; } = [];

        public void Clear()
        {
            CreatureIndices.Clear();
            ResourceIndices.Clear();
            EggIndices.Clear();
        }
    }
}
