namespace Lineage.Core;

/// <summary>
/// Reconstructs the active ancestry backbone from current living creatures.
/// </summary>
public static class SurvivorLineageAnalyzer
{
    public static SurvivorLineageAnalysis Analyze(WorldState state)
    {
        var livingIds = state.Creatures
            .Select(creature => creature.Id)
            .Where(id => id != default)
            .Distinct()
            .ToArray();
        var ancestorIds = CollectAncestorIds(state.LineageRecords, livingIds);
        if (ancestorIds.Count == 0)
        {
            return SurvivorLineageAnalysis.Empty;
        }

        var recordsById = state.LineageRecords.ToDictionary(record => record.Id);
        var livingById = state.Creatures.ToDictionary(creature => creature.Id);
        var childrenById = BuildChildrenById(recordsById, ancestorIds);
        var livingDescendantCounts = CountLivingDescendants(recordsById, ancestorIds, livingIds);
        var roots = ancestorIds
            .Select(id => recordsById[id])
            .Where(record => record.ParentId == default || !ancestorIds.Contains(record.ParentId))
            .OrderByDescending(record => livingDescendantCounts.GetValueOrDefault(record.Id))
            .ThenBy(record => record.Id.Value)
            .ToArray();
        var dominantFounderId = roots.Length > 0 ? roots[0].Id : default;
        var dominantPathIds = BuildDominantPath(dominantFounderId, recordsById, childrenById, livingDescendantCounts);
        var dominantPathSet = dominantPathIds.ToHashSet();
        var nodes = ancestorIds
            .Select(id =>
            {
                var record = recordsById[id];
                return new SurvivorLineageNode(
                    record,
                    livingDescendantCounts.GetValueOrDefault(id),
                    childrenById.TryGetValue(id, out var children) ? children.Count : 0,
                    livingById.ContainsKey(id),
                    dominantPathSet.Contains(id),
                    record.GenomeId >= 0,
                    record.BrainId >= 0);
            })
            .OrderBy(node => node.Record.Generation)
            .ThenBy(node => node.Record.Id.Value)
            .ToArray();
        var edges = ancestorIds
            .Select(id => recordsById[id])
            .Where(record => record.ParentId != default && ancestorIds.Contains(record.ParentId))
            .Select(record => new SurvivorLineageEdge(record.ParentId, record.Id))
            .OrderBy(edge => recordsById[edge.ChildId].Generation)
            .ThenBy(edge => edge.ParentId.Value)
            .ThenBy(edge => edge.ChildId.Value)
            .ToArray();
        var dominantPath = dominantPathIds
            .Select(id => new SurvivorLineagePathStep(
                recordsById[id],
                livingDescendantCounts.GetValueOrDefault(id),
                childrenById.TryGetValue(id, out var children) ? children.Count : 0,
                livingById.ContainsKey(id)))
            .ToArray();
        var dominantSurvivorId = dominantPath.LastOrDefault().Record.Id;
        var segments = BuildSegments(
            roots,
            recordsById,
            childrenById,
            livingDescendantCounts,
            livingById,
            dominantPathSet);

        return new SurvivorLineageAnalysis(
            state.Creatures.Count,
            nodes.Length,
            segments.Count,
            roots.Length,
            nodes.Max(node => node.Record.Generation),
            dominantFounderId,
            livingDescendantCounts.GetValueOrDefault(dominantFounderId),
            dominantSurvivorId,
            nodes,
            edges,
            segments,
            dominantPath);
    }

    public static HashSet<EntityId> CollectAncestorIds(
        IReadOnlyList<CreatureLineageRecord> records,
        IEnumerable<EntityId> descendantIds)
    {
        var recordsById = records.ToDictionary(record => record.Id);
        var ancestors = new HashSet<EntityId>();
        foreach (var descendantId in descendantIds)
        {
            if (descendantId == default)
            {
                continue;
            }

            var currentId = descendantId;
            var path = new HashSet<EntityId>();
            while (currentId != default && recordsById.TryGetValue(currentId, out var record))
            {
                if (!path.Add(currentId))
                {
                    break;
                }

                if (!ancestors.Add(currentId))
                {
                    break;
                }

                currentId = record.ParentId;
            }
        }

        return ancestors;
    }

    private static Dictionary<EntityId, List<EntityId>> BuildChildrenById(
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> recordsById,
        HashSet<EntityId> ancestorIds)
    {
        var childrenById = new Dictionary<EntityId, List<EntityId>>();
        foreach (var id in ancestorIds)
        {
            var record = recordsById[id];
            if (record.ParentId == default || !ancestorIds.Contains(record.ParentId))
            {
                continue;
            }

            if (!childrenById.TryGetValue(record.ParentId, out var children))
            {
                children = [];
                childrenById.Add(record.ParentId, children);
            }

            children.Add(id);
        }

        foreach (var children in childrenById.Values)
        {
            children.Sort((left, right) =>
            {
                var leftRecord = recordsById[left];
                var rightRecord = recordsById[right];
                var generationComparison = leftRecord.Generation.CompareTo(rightRecord.Generation);
                return generationComparison != 0
                    ? generationComparison
                    : left.Value.CompareTo(right.Value);
            });
        }

        return childrenById;
    }

    private static Dictionary<EntityId, int> CountLivingDescendants(
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> recordsById,
        HashSet<EntityId> ancestorIds,
        IReadOnlyList<EntityId> livingIds)
    {
        var counts = new Dictionary<EntityId, int>();
        foreach (var livingId in livingIds)
        {
            var currentId = livingId;
            var path = new HashSet<EntityId>();
            while (currentId != default
                && ancestorIds.Contains(currentId)
                && recordsById.TryGetValue(currentId, out var record))
            {
                if (!path.Add(currentId))
                {
                    break;
                }

                counts[currentId] = counts.GetValueOrDefault(currentId) + 1;
                currentId = record.ParentId;
            }
        }

        return counts;
    }

    private static IReadOnlyList<EntityId> BuildDominantPath(
        EntityId rootId,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> recordsById,
        IReadOnlyDictionary<EntityId, List<EntityId>> childrenById,
        IReadOnlyDictionary<EntityId, int> livingDescendantCounts)
    {
        if (rootId == default)
        {
            return Array.Empty<EntityId>();
        }

        var path = new List<EntityId>();
        var seen = new HashSet<EntityId>();
        var currentId = rootId;
        while (currentId != default && recordsById.ContainsKey(currentId) && seen.Add(currentId))
        {
            path.Add(currentId);
            if (!childrenById.TryGetValue(currentId, out var children) || children.Count == 0)
            {
                break;
            }

            currentId = children
                .OrderByDescending(childId => livingDescendantCounts.GetValueOrDefault(childId))
                .ThenByDescending(childId => recordsById[childId].Generation)
                .ThenBy(childId => childId.Value)
                .First();
        }

        return path;
    }

    private static IReadOnlyList<SurvivorLineageSegment> BuildSegments(
        IReadOnlyList<CreatureLineageRecord> roots,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> recordsById,
        IReadOnlyDictionary<EntityId, List<EntityId>> childrenById,
        IReadOnlyDictionary<EntityId, int> livingDescendantCounts,
        IReadOnlyDictionary<EntityId, CreatureState> livingById,
        HashSet<EntityId> dominantPathSet)
    {
        var drafts = new List<SegmentDraft>();
        var nextSegmentIndex = 1;

        void AddSegment(EntityId startId, string? parentSegmentId)
        {
            if (!recordsById.ContainsKey(startId))
            {
                return;
            }

            var ids = new List<EntityId>();
            var currentId = startId;
            var path = new HashSet<EntityId>();
            while (recordsById.ContainsKey(currentId) && path.Add(currentId))
            {
                ids.Add(currentId);
                if (!childrenById.TryGetValue(currentId, out var children) || children.Count != 1)
                {
                    break;
                }

                currentId = children[0];
            }

            if (ids.Count == 0)
            {
                return;
            }

            var endId = ids[^1];
            var segmentId = $"L{nextSegmentIndex++}";
            var childCount = childrenById.TryGetValue(endId, out var endChildren) ? endChildren.Count : 0;
            drafts.Add(new SegmentDraft(
                segmentId,
                parentSegmentId,
                ids,
                recordsById[ids[0]],
                recordsById[endId],
                livingDescendantCounts.GetValueOrDefault(endId),
                childCount,
                ids.Any(dominantPathSet.Contains),
                livingById.ContainsKey(endId),
                ids.All(id => recordsById[id].GenomeId >= 0),
                ids.All(id => recordsById[id].BrainId >= 0)));

            if (childrenById.TryGetValue(endId, out var childrenToStart))
            {
                foreach (var childId in childrenToStart
                    .OrderByDescending(id => livingDescendantCounts.GetValueOrDefault(id))
                    .ThenBy(id => recordsById[id].Generation)
                    .ThenBy(id => id.Value))
                {
                    AddSegment(childId, segmentId);
                }
            }
        }

        foreach (var root in roots)
        {
            AddSegment(root.Id, parentSegmentId: null);
        }

        var childSegmentCounts = drafts
            .Where(segment => segment.ParentSegmentId is not null)
            .GroupBy(segment => segment.ParentSegmentId!)
            .ToDictionary(group => group.Key, group => group.Count());
        return drafts
            .Select(segment => new SurvivorLineageSegment(
                segment.SegmentId,
                segment.ParentSegmentId,
                segment.StartRecord,
                segment.EndRecord,
                segment.AncestorIds.Count,
                segment.LivingDescendantCount,
                childSegmentCounts.GetValueOrDefault(segment.SegmentId),
                segment.IsDominantPath,
                segment.IsLivingEndpoint,
                segment.HasGenomePayload,
                segment.HasBrainPayload))
            .ToArray();
    }

    private sealed record SegmentDraft(
        string SegmentId,
        string? ParentSegmentId,
        IReadOnlyList<EntityId> AncestorIds,
        CreatureLineageRecord StartRecord,
        CreatureLineageRecord EndRecord,
        int LivingDescendantCount,
        int ChildRecordCount,
        bool IsDominantPath,
        bool IsLivingEndpoint,
        bool HasGenomePayload,
        bool HasBrainPayload);
}

public sealed record SurvivorLineageAnalysis(
    int LivingCreatureCount,
    int AncestorCount,
    int SegmentCount,
    int FounderCount,
    int MaxGeneration,
    EntityId DominantFounderId,
    int DominantFounderLivingDescendants,
    EntityId DominantSurvivorId,
    IReadOnlyList<SurvivorLineageNode> Nodes,
    IReadOnlyList<SurvivorLineageEdge> Edges,
    IReadOnlyList<SurvivorLineageSegment> Segments,
    IReadOnlyList<SurvivorLineagePathStep> DominantPath)
{
    public static readonly SurvivorLineageAnalysis Empty = new(
        0,
        0,
        0,
        0,
        0,
        default,
        0,
        default,
        Array.Empty<SurvivorLineageNode>(),
        Array.Empty<SurvivorLineageEdge>(),
        Array.Empty<SurvivorLineageSegment>(),
        Array.Empty<SurvivorLineagePathStep>());
}

public readonly record struct SurvivorLineageNode(
    CreatureLineageRecord Record,
    int LivingDescendantCount,
    int ChildCount,
    bool IsLiving,
    bool IsDominantPath,
    bool HasGenomePayload,
    bool HasBrainPayload);

public readonly record struct SurvivorLineageEdge(EntityId ParentId, EntityId ChildId);

public readonly record struct SurvivorLineageSegment(
    string SegmentId,
    string? ParentSegmentId,
    CreatureLineageRecord StartRecord,
    CreatureLineageRecord EndRecord,
    int AncestorCount,
    int LivingDescendantCount,
    int ChildSegmentCount,
    bool IsDominantPath,
    bool IsLivingEndpoint,
    bool HasGenomePayload,
    bool HasBrainPayload);

public readonly record struct SurvivorLineagePathStep(
    CreatureLineageRecord Record,
    int LivingDescendantCount,
    int ChildCount,
    bool IsLiving);
