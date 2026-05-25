using System.Runtime.InteropServices;

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
    private const float DirectionEpsilonSquared = 0.00000001f;

    private SpatialCell?[] _cells = [];
    private readonly List<SpatialCell> _resourceCells = [];
    private readonly List<SpatialCell> _eggCells = [];
    private readonly List<SpatialCell> _creatureCells = [];
    private int _cellCountX;
    private int _cellCountY;
    private long _indexedResourceVersion = -1;
    private long _indexedEggVersion = -1;

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
        EnsureGrid(state.Bounds);

        if (_indexedResourceVersion != state.ResourceIndexVersion)
        {
            RebuildResources(state);
            _indexedResourceVersion = state.ResourceIndexVersion;
        }

        if (_indexedEggVersion != state.EggIndexVersion)
        {
            RebuildEggs(state);
            _indexedEggVersion = state.EggIndexVersion;
        }

        RebuildCreatures(state);
    }

    private void RebuildResources(WorldState state)
    {
        foreach (var cell in _resourceCells)
        {
            cell.ClearResources();
        }

        _resourceCells.Clear();

        for (var i = 0; i < state.Resources.Count; i++)
        {
            var resource = state.Resources[i];
            if (!ShouldIndexResource(resource))
            {
                continue;
            }

            AddResourceToCells(i, resource.Kind, resource.Position, resource.Radius);
        }
    }

    private void RebuildEggs(WorldState state)
    {
        foreach (var cell in _eggCells)
        {
            cell.ClearEggs();
        }

        _eggCells.Clear();

        for (var i = 0; i < state.Eggs.Count; i++)
        {
            var egg = state.Eggs[i];
            AddEggToCells(i, egg.Position, EggPredation.ContactRadius(egg));
        }
    }

    public void RebuildCreatures(WorldState state)
    {
        EnsureGrid(state.Bounds);

        foreach (var cell in _creatureCells)
        {
            cell.ClearCreatures();
        }

        _creatureCells.Clear();

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

        if (!TryGetCellRange(position, radius, out var minCellX, out var maxCellX, out var minCellY, out var maxCellY))
        {
            return -1;
        }

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                var cell = GetCell(cellX, cellY);
                if (cell is null)
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

        if (!TryGetCellRange(position, radius, out var minCellX, out var maxCellX, out var minCellY, out var maxCellY))
        {
            return -1;
        }

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                var cell = GetCell(cellX, cellY);
                if (cell is null)
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

        if (!TryGetCellRange(position, radius, out var minCellX, out var maxCellX, out var minCellY, out var maxCellY))
        {
            return;
        }

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                var cell = GetCell(cellX, cellY);
                if (cell is null)
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

    internal void AddResourceCandidatesWithCalories(
        WorldState state,
        SimVector2 position,
        float radius,
        float minimumCalories,
        List<int> results,
        IndexStampSet seen)
    {
        AddResourceCandidatesWithKind(
            state,
            position,
            radius,
            minimumCalories,
            kind: null,
            results,
            seen);
    }

    public void AddPlantAndMeatResourceCandidatesWithCalories(
        WorldState state,
        SimVector2 position,
        float plantRadius,
        float meatRadius,
        float minimumCalories,
        List<int> plantResults,
        HashSet<int> seenPlants,
        List<int> meatResults,
        HashSet<int> seenMeat)
    {
        if (plantRadius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(plantRadius), "Query radius cannot be negative.");
        }

        if (meatRadius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(meatRadius), "Query radius cannot be negative.");
        }

        plantResults.Clear();
        seenPlants.Clear();
        meatResults.Clear();
        seenMeat.Clear();

        var maxRadius = MathF.Max(plantRadius, meatRadius);
        if (!TryGetCellRange(position, maxRadius, out var minCellX, out var maxCellX, out var minCellY, out var maxCellY))
        {
            return;
        }

        _ = TryGetCellRange(position, plantRadius, out var minPlantCellX, out var maxPlantCellX, out var minPlantCellY, out var maxPlantCellY);

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                if (cellX >= minPlantCellX
                    && cellX <= maxPlantCellX
                    && cellY >= minPlantCellY
                    && cellY <= maxPlantCellY)
                {
                    var cell = GetCell(cellX, cellY);
                    if (cell is null)
                    {
                        continue;
                    }

                    AddResourceCandidatesFromList(
                        state,
                        position,
                        plantRadius,
                        minimumCalories,
                        cell.PlantResourceIndices,
                        plantResults,
                        seenPlants);

                    AddResourceCandidatesFromList(
                        state,
                        position,
                        meatRadius,
                        minimumCalories,
                        cell.MeatResourceIndices,
                        meatResults,
                        seenMeat);
                    continue;
                }

                var meatCell = GetCell(cellX, cellY);
                if (meatCell is null || meatCell.MeatResourceIndices.Count == 0)
                {
                    continue;
                }

                AddResourceCandidatesFromList(
                    state,
                    position,
                    meatRadius,
                    minimumCalories,
                    meatCell.MeatResourceIndices,
                    meatResults,
                    seenMeat);
            }
        }
    }

    internal void AddPlantAndMeatResourceCandidatesWithCalories(
        WorldState state,
        SimVector2 position,
        float plantRadius,
        float meatRadius,
        float minimumCalories,
        List<int> plantResults,
        IndexStampSet seenPlants,
        List<int> meatResults,
        IndexStampSet seenMeat)
    {
        if (plantRadius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(plantRadius), "Query radius cannot be negative.");
        }

        if (meatRadius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(meatRadius), "Query radius cannot be negative.");
        }

        plantResults.Clear();
        seenPlants.Begin(state.Resources.Count);
        meatResults.Clear();
        seenMeat.Begin(state.Resources.Count);

        var maxRadius = MathF.Max(plantRadius, meatRadius);
        if (!TryGetCellRange(position, maxRadius, out var minCellX, out var maxCellX, out var minCellY, out var maxCellY))
        {
            return;
        }

        _ = TryGetCellRange(position, plantRadius, out var minPlantCellX, out var maxPlantCellX, out var minPlantCellY, out var maxPlantCellY);

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                if (cellX >= minPlantCellX
                    && cellX <= maxPlantCellX
                    && cellY >= minPlantCellY
                    && cellY <= maxPlantCellY)
                {
                    var cell = GetCell(cellX, cellY);
                    if (cell is null)
                    {
                        continue;
                    }

                    AddResourceCandidatesFromList(
                        state,
                        position,
                        plantRadius,
                        minimumCalories,
                        cell.PlantResourceIndices,
                        plantResults,
                        seenPlants);

                    AddResourceCandidatesFromList(
                        state,
                        position,
                        meatRadius,
                        minimumCalories,
                        cell.MeatResourceIndices,
                        meatResults,
                        seenMeat);
                    continue;
                }

                var meatCell = GetCell(cellX, cellY);
                if (meatCell is null || meatCell.MeatResourceIndices.Count == 0)
                {
                    continue;
                }

                AddResourceCandidatesFromList(
                    state,
                    position,
                    meatRadius,
                    minimumCalories,
                    meatCell.MeatResourceIndices,
                    meatResults,
                    seenMeat);
            }
        }
    }

    public void AddResourceCandidatesWithCalories(
        WorldState state,
        SimVector2 position,
        float radius,
        float minimumCalories,
        ResourceKind kind,
        List<int> results,
        HashSet<int> seen)
    {
        AddResourceCandidatesWithKind(
            state,
            position,
            radius,
            minimumCalories,
            kind,
            results,
            seen);
    }

    internal void AddResourceCandidatesWithCalories(
        WorldState state,
        SimVector2 position,
        float radius,
        float minimumCalories,
        ResourceKind kind,
        List<int> results,
        IndexStampSet seen)
    {
        AddResourceCandidatesWithKind(
            state,
            position,
            radius,
            minimumCalories,
            kind,
            results,
            seen);
    }

    private void AddResourceCandidatesWithKind(
        WorldState state,
        SimVector2 position,
        float radius,
        float minimumCalories,
        ResourceKind? kind,
        List<int> results,
        HashSet<int> seen)
    {
        if (radius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Query radius cannot be negative.");
        }

        results.Clear();
        seen.Clear();

        if (!TryGetCellRange(position, radius, out var minCellX, out var maxCellX, out var minCellY, out var maxCellY))
        {
            return;
        }

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                var cell = GetCell(cellX, cellY);
                if (cell is null)
                {
                    continue;
                }

                var resourceIndices = ResourceIndicesForKind(cell, kind);
                for (var i = 0; i < resourceIndices.Count; i++)
                {
                    var resourceIndex = resourceIndices[i];
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

    private void AddResourceCandidatesWithKind(
        WorldState state,
        SimVector2 position,
        float radius,
        float minimumCalories,
        ResourceKind? kind,
        List<int> results,
        IndexStampSet seen)
    {
        if (radius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Query radius cannot be negative.");
        }

        results.Clear();
        seen.Begin(state.Resources.Count);

        if (!TryGetCellRange(position, radius, out var minCellX, out var maxCellX, out var minCellY, out var maxCellY))
        {
            return;
        }

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                var cell = GetCell(cellX, cellY);
                if (cell is null)
                {
                    continue;
                }

                var resourceIndices = ResourceIndicesForKind(cell, kind);
                for (var i = 0; i < resourceIndices.Count; i++)
                {
                    var resourceIndex = resourceIndices[i];
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

        if (!TryGetCellRange(position, radius, out var minCellX, out var maxCellX, out var minCellY, out var maxCellY))
        {
            return;
        }

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                var cell = GetCell(cellX, cellY);
                if (cell is null)
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

    internal void AddEggCandidatesWithEnergy(
        WorldState state,
        SimVector2 position,
        float radius,
        float minimumEnergy,
        List<int> results,
        IndexStampSet seen)
    {
        if (radius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Query radius cannot be negative.");
        }

        results.Clear();
        seen.Begin(state.Eggs.Count);

        if (!TryGetCellRange(position, radius, out var minCellX, out var maxCellX, out var minCellY, out var maxCellY))
        {
            return;
        }

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                var cell = GetCell(cellX, cellY);
                if (cell is null)
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

        if (!TryGetCellRange(position, radius, out var minCellX, out var maxCellX, out var minCellY, out var maxCellY))
        {
            return;
        }

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                var cell = GetCell(cellX, cellY);
                if (cell is null)
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

    internal VisibleCreatureQueryResult FindNearestVisibleCreature(
        WorldState state,
        int selfIndex,
        EntityId selfId,
        SimVector2 position,
        float queryRadius,
        float senseRadius,
        SimVector2 forward,
        bool hasLimitedVision,
        float visionCosThreshold,
        float visionAngleRadians,
        float[] bodyRadii,
        float[] maxSpeeds,
        bool collectVisionSectors,
        ref VisionSectorSet visionSectors)
    {
        if (queryRadius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(queryRadius), "Query radius cannot be negative.");
        }

        if (senseRadius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(senseRadius), "Sense radius cannot be negative.");
        }

        if (!TryGetCellRange(position, queryRadius, out var minCellX, out var maxCellX, out var minCellY, out var maxCellY))
        {
            return VisibleCreatureQueryResult.Empty;
        }

        var creatures = CollectionsMarshal.AsSpan(state.Creatures);
        var selfCreature = creatures[selfIndex];
        var selfVelocityX = selfCreature.Velocity.X;
        var selfVelocityY = selfCreature.Velocity.Y;
        var queryRadiusSquared = queryRadius * queryRadius;
        var forwardX = forward.X;
        var forwardY = forward.Y;
        var rightX = -forwardY;
        var rightY = forwardX;
        var squaredVisionCosThreshold = visionCosThreshold * visionCosThreshold;
        var selfRadius = 0f;
        var selfMaxSpeed = 0f;
        if (collectVisionSectors)
        {
            selfRadius = bodyRadii[selfIndex];
            if (selfRadius < 0f)
            {
                selfRadius = CreatureGrowth.EffectiveBodyRadius(
                    selfCreature,
                    state.GetGenome(selfCreature.GenomeId));
                bodyRadii[selfIndex] = selfRadius;
            }

            selfMaxSpeed = maxSpeeds[selfIndex];
            if (selfMaxSpeed < 0f)
            {
                selfMaxSpeed = CreatureGrowth.EffectiveMaxSpeed(
                    selfCreature,
                    state.GetGenome(selfCreature.GenomeId));
                maxSpeeds[selfIndex] = selfMaxSpeed;
            }
        }

        var candidateCount = 0;
        var visibleCount = 0;
        var nearestIndex = -1;
        var nearestDistanceSquared = float.PositiveInfinity;
        var cellsVisited = 0;
        var nonEmptyCellsVisited = 0;
        var distanceRejectedCount = 0;
        var selfRejectedCount = 0;
        var nonviableRejectedCount = 0;
        var rangeRejectedCount = 0;
        var visionRejectedCount = 0;
        var bodyRadiusCacheMissCount = 0;

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                cellsVisited++;
                var cell = GetCell(cellX, cellY);
                if (cell is null || cell.CreatureIndices.Count == 0)
                {
                    continue;
                }

                nonEmptyCellsVisited++;
                for (var i = 0; i < cell.CreatureIndices.Count; i++)
                {
                    var otherCreatureIndex = cell.CreatureIndices[i];
                    var otherCreature = creatures[otherCreatureIndex];
                    var toOtherX = otherCreature.Position.X - position.X;
                    var toOtherY = otherCreature.Position.Y - position.Y;
                    var distanceSquared = toOtherX * toOtherX + toOtherY * toOtherY;
                    if (distanceSquared > queryRadiusSquared)
                    {
                        distanceRejectedCount++;
                        continue;
                    }

                    candidateCount++;

                    if (otherCreatureIndex == selfIndex
                        || otherCreature.Id == selfId)
                    {
                        selfRejectedCount++;
                        continue;
                    }

                    if (otherCreature.Health <= 0f
                        || otherCreature.Energy <= 0f)
                    {
                        nonviableRejectedCount++;
                        continue;
                    }

                    var otherRadius = bodyRadii[otherCreatureIndex];
                    if (otherRadius < 0f)
                    {
                        bodyRadiusCacheMissCount++;
                        otherRadius = CreatureGrowth.EffectiveBodyRadius(
                            otherCreature,
                            state.GetGenome(otherCreature.GenomeId));
                        bodyRadii[otherCreatureIndex] = otherRadius;
                    }

                    var maxCenterDistance = otherRadius + senseRadius;
                    if (distanceSquared > maxCenterDistance * maxCenterDistance)
                    {
                        rangeRejectedCount++;
                        continue;
                    }

                    if (!IsInsideVisionCone(
                        toOtherX,
                        toOtherY,
                        distanceSquared,
                        forwardX,
                        forwardY,
                        hasLimitedVision,
                        visionCosThreshold,
                        squaredVisionCosThreshold))
                    {
                        visionRejectedCount++;
                        continue;
                    }

                    visibleCount++;
                    if (collectVisionSectors
                        && VisionSectorSet.TryGetSectorIndex(
                            toOtherX,
                            toOtherY,
                            forwardX,
                            forwardY,
                            rightX,
                            rightY,
                            hasLimitedVision,
                            visionAngleRadians,
                            out var sectorIndex))
                    {
                        var centerDistance = MathF.Sqrt(distanceSquared);
                        var edgeDistance = Math.Max(0f, centerDistance - otherRadius);
                        var proximity = 1f - Math.Clamp(edgeDistance / senseRadius, 0f, 1f);
                        var radiusScale = MathF.Max(0.001f, MathF.Max(selfRadius, otherRadius));
                        var relativeBodySize = Math.Clamp((otherRadius - selfRadius) / radiusScale, -1f, 1f);

                        var otherMaxSpeed = maxSpeeds[otherCreatureIndex];
                        if (otherMaxSpeed < 0f)
                        {
                            otherMaxSpeed = CreatureGrowth.EffectiveMaxSpeed(
                                otherCreature,
                                state.GetGenome(otherCreature.GenomeId));
                            maxSpeeds[otherCreatureIndex] = otherMaxSpeed;
                        }

                        var directionToOtherX = centerDistance > 0.0001f
                            ? toOtherX / centerDistance
                            : forwardX;
                        var directionToOtherY = centerDistance > 0.0001f
                            ? toOtherY / centerDistance
                            : forwardY;
                        var relativeVelocityX = otherCreature.Velocity.X - selfVelocityX;
                        var relativeVelocityY = otherCreature.Velocity.Y - selfVelocityY;
                        var approachScale = MathF.Max(1f, MathF.Max(selfMaxSpeed, otherMaxSpeed));
                        var approachRate = Math.Clamp(
                            -((relativeVelocityX * directionToOtherX) + (relativeVelocityY * directionToOtherY)) / approachScale,
                            -1f,
                            1f);
                        var otherForward = SimVector2.FromAngle(otherCreature.HeadingRadians);
                        var facingAlignment = Math.Clamp(
                            -((otherForward.X * directionToOtherX) + (otherForward.Y * directionToOtherY)),
                            -1f,
                            1f);
                        visionSectors.AddCreature(
                            sectorIndex,
                            proximity,
                            relativeBodySize,
                            approachRate,
                            facingAlignment);
                    }

                    if (distanceSquared < nearestDistanceSquared)
                    {
                        nearestDistanceSquared = distanceSquared;
                        nearestIndex = otherCreatureIndex;
                    }
                }
            }
        }

        return new VisibleCreatureQueryResult(
            candidateCount,
            visibleCount,
            nearestIndex,
            nearestDistanceSquared,
            cellsVisited,
            nonEmptyCellsVisited,
            distanceRejectedCount,
            selfRejectedCount,
            nonviableRejectedCount,
            rangeRejectedCount,
            visionRejectedCount,
            bodyRadiusCacheMissCount);
    }

    private void AddResourceToCells(int resourceIndex, ResourceKind kind, SimVector2 position, float radius)
    {
        if (!TryGetCellRange(position, radius, out var minCellX, out var maxCellX, out var minCellY, out var maxCellY))
        {
            return;
        }

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                var cell = GetOrCreateCell(cellX, cellY);
                if (cell.ResourceIndices.Count == 0)
                {
                    _resourceCells.Add(cell);
                }

                cell.ResourceIndices.Add(resourceIndex);
                if (kind == ResourceKind.Meat)
                {
                    cell.MeatResourceIndices.Add(resourceIndex);
                }
                else
                {
                    cell.PlantResourceIndices.Add(resourceIndex);
                }
            }
        }
    }

    private void AddEggToCells(int eggIndex, SimVector2 position, float radius)
    {
        if (!TryGetCellRange(position, radius, out var minCellX, out var maxCellX, out var minCellY, out var maxCellY))
        {
            return;
        }

        for (var cellY = minCellY; cellY <= maxCellY; cellY++)
        {
            for (var cellX = minCellX; cellX <= maxCellX; cellX++)
            {
                var cell = GetOrCreateCell(cellX, cellY);
                if (cell.EggIndices.Count == 0)
                {
                    _eggCells.Add(cell);
                }

                cell.EggIndices.Add(eggIndex);
            }
        }
    }

    private void AddCreatureToCell(int creatureIndex, SimVector2 position)
    {
        var cellX = ToBoundedCell(position.X, _cellCountX);
        var cellY = ToBoundedCell(position.Y, _cellCountY);
        var cell = GetOrCreateCell(cellX, cellY);
        if (cell.CreatureIndices.Count == 0)
        {
            _creatureCells.Add(cell);
        }

        cell.CreatureIndices.Add(creatureIndex);
    }

    private void EnsureGrid(WorldBounds bounds)
    {
        var cellCountX = CalculateCellCount(bounds.Width, CellSize);
        var cellCountY = CalculateCellCount(bounds.Height, CellSize);
        if (_cells.Length > 0 && cellCountX == _cellCountX && cellCountY == _cellCountY)
        {
            return;
        }

        var cellCount = checked((long)cellCountX * cellCountY);
        if (cellCount > int.MaxValue)
        {
            throw new InvalidOperationException("Spatial index grid is too large for a single array.");
        }

        _cells = new SpatialCell?[cellCount];
        _cellCountX = cellCountX;
        _cellCountY = cellCountY;
        _resourceCells.Clear();
        _eggCells.Clear();
        _creatureCells.Clear();
        _indexedResourceVersion = -1;
        _indexedEggVersion = -1;
    }

    private bool TryGetCellRange(
        SimVector2 position,
        float radius,
        out int minCellX,
        out int maxCellX,
        out int minCellY,
        out int maxCellY)
    {
        minCellX = 0;
        maxCellX = 0;
        minCellY = 0;
        maxCellY = 0;

        if (_cells.Length == 0)
        {
            return false;
        }

        minCellX = ToBoundedCell(position.X - radius, _cellCountX);
        maxCellX = ToBoundedCell(position.X + radius, _cellCountX);
        minCellY = ToBoundedCell(position.Y - radius, _cellCountY);
        maxCellY = ToBoundedCell(position.Y + radius, _cellCountY);
        return true;
    }

    private SpatialCell? GetCell(int cellX, int cellY)
    {
        return _cells[cellY * _cellCountX + cellX];
    }

    private SpatialCell GetOrCreateCell(int cellX, int cellY)
    {
        var index = cellY * _cellCountX + cellX;
        var cell = _cells[index];
        if (cell is not null)
        {
            return cell;
        }

        cell = new SpatialCell();
        _cells[index] = cell;
        return cell;
    }

    private int ToCell(float coordinate)
    {
        return (int)MathF.Floor(coordinate / CellSize);
    }

    private int ToBoundedCell(float coordinate, int cellCount)
    {
        return Math.Clamp(ToCell(coordinate), 0, cellCount - 1);
    }

    private static int CalculateCellCount(float length, float cellSize)
    {
        return Math.Max(1, (int)MathF.Floor(length / cellSize) + 1);
    }

    private static bool IsInsideVisionCone(
        SimVector2 toTarget,
        float distanceSquared,
        SimVector2 forward,
        bool hasLimitedVision,
        float visionCosThreshold)
    {
        if (!hasLimitedVision || distanceSquared <= DirectionEpsilonSquared)
        {
            return true;
        }

        var forwardDot = SimVector2.Dot(toTarget, forward);
        var thresholdSquaredDistance = visionCosThreshold * visionCosThreshold * distanceSquared;
        if (visionCosThreshold >= 0f)
        {
            return forwardDot >= 0f && forwardDot * forwardDot >= thresholdSquaredDistance;
        }

        return forwardDot >= 0f || forwardDot * forwardDot <= thresholdSquaredDistance;
    }

    private static bool IsInsideVisionCone(
        float toTargetX,
        float toTargetY,
        float distanceSquared,
        float forwardX,
        float forwardY,
        bool hasLimitedVision,
        float visionCosThreshold,
        float squaredVisionCosThreshold)
    {
        if (!hasLimitedVision || distanceSquared <= DirectionEpsilonSquared)
        {
            return true;
        }

        var forwardDot = toTargetX * forwardX + toTargetY * forwardY;
        var thresholdSquaredDistance = squaredVisionCosThreshold * distanceSquared;
        if (visionCosThreshold >= 0f)
        {
            return forwardDot >= 0f && forwardDot * forwardDot >= thresholdSquaredDistance;
        }

        return forwardDot >= 0f || forwardDot * forwardDot <= thresholdSquaredDistance;
    }

    private static bool IsWithinEdgeRange(float distanceSquared, float targetRadius, float senseRadius)
    {
        var maxCenterDistance = targetRadius + senseRadius;
        return distanceSquared <= maxCenterDistance * maxCenterDistance;
    }

    private static void AddResourceCandidatesFromList(
        WorldState state,
        SimVector2 position,
        float radius,
        float minimumCalories,
        List<int> resourceIndices,
        List<int> results,
        HashSet<int> seen)
    {
        for (var i = 0; i < resourceIndices.Count; i++)
        {
            var resourceIndex = resourceIndices[i];
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

    private static void AddResourceCandidatesFromList(
        WorldState state,
        SimVector2 position,
        float radius,
        float minimumCalories,
        List<int> resourceIndices,
        List<int> results,
        IndexStampSet seen)
    {
        for (var i = 0; i < resourceIndices.Count; i++)
        {
            var resourceIndex = resourceIndices[i];
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

    private static List<int> ResourceIndicesForKind(SpatialCell cell, ResourceKind? kind)
    {
        return kind switch
        {
            ResourceKind.Plant => cell.PlantResourceIndices,
            ResourceKind.Meat => cell.MeatResourceIndices,
            _ => cell.ResourceIndices
        };
    }

    private static bool ShouldIndexResource(ResourcePatchState resource)
    {
        return resource.Calories > 0f
            && (resource.Kind != ResourceKind.Plant || resource.RespawnSecondsRemaining <= 0f);
    }

    private sealed class SpatialCell
    {
        public List<int> CreatureIndices { get; } = [];

        public List<int> ResourceIndices { get; } = [];

        public List<int> PlantResourceIndices { get; } = [];

        public List<int> MeatResourceIndices { get; } = [];

        public List<int> EggIndices { get; } = [];

        public void ClearCreatures()
        {
            CreatureIndices.Clear();
        }

        public void ClearResources()
        {
            ResourceIndices.Clear();
            PlantResourceIndices.Clear();
            MeatResourceIndices.Clear();
        }

        public void ClearEggs()
        {
            EggIndices.Clear();
        }
    }
}

internal readonly record struct VisibleCreatureQueryResult(
    int CandidateCount,
    int VisibleCount,
    int NearestIndex,
    float NearestDistanceSquared,
    int CellsVisited,
    int NonEmptyCellsVisited,
    int DistanceRejectedCount,
    int SelfRejectedCount,
    int NonviableRejectedCount,
    int RangeRejectedCount,
    int VisionRejectedCount,
    int BodyRadiusCacheMissCount)
{
    public static VisibleCreatureQueryResult Empty { get; } = new(0, 0, -1, float.PositiveInfinity, 0, 0, 0, 0, 0, 0, 0, 0);
}
