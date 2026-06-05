namespace Lineage.Core;

/// <summary>
/// Same-founder familiarity derived from the recorded asexual lineage tree.
/// </summary>
internal static class LineageFamiliarity
{
    public const float SameLineageThreshold = 0.5f;

    public static float CreatureSimilarity(WorldState state, EntityId leftCreatureId, EntityId rightCreatureId)
    {
        if (leftCreatureId == default || rightCreatureId == default)
        {
            return 0f;
        }

        if (leftCreatureId == rightCreatureId)
        {
            return 1f;
        }

        return TryGetFounderId(state, leftCreatureId, out var leftFounderId)
            && TryGetFounderId(state, rightCreatureId, out var rightFounderId)
            && leftFounderId == rightFounderId
                ? 1f
                : 0f;
    }

    public static float EggSimilarity(WorldState state, EntityId creatureId, EggState egg)
    {
        return CreatureSimilarity(state, creatureId, egg.ParentId);
    }

    public static float ScentWeight(float similarity)
    {
        return similarity >= SameLineageThreshold ? 1f : 0f;
    }

    private static bool TryGetFounderId(WorldState state, EntityId creatureId, out EntityId founderId)
    {
        if (!state.TryGetLineageRecord(creatureId, out var record))
        {
            founderId = default;
            return false;
        }

        var current = record;
        while (!current.IsFounder && state.TryGetLineageRecord(current.ParentId, out var parent))
        {
            current = parent;
        }

        founderId = current.IsFounder ? current.Id : current.ParentId;
        return founderId != default;
    }
}
