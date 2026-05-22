namespace Lineage.Core;

/// <summary>
/// Shared plant placement rules for initial world seeding and depleted-plant relocation.
/// </summary>
///
/// <remarks>
/// Biomes decide where plants are allowed and how attractive each area is. Clustering
/// adds patchiness by sometimes placing a new plant near an existing live plant,
/// which creates local food islands without giving creatures any direct knowledge
/// about the underlying fertility map.
/// </remarks>
public static class ResourcePlacement
{
    private const int ClusterAttemptCount = 8;

    public static SimVector2 SamplePlantPosition(
        WorldState state,
        float clusterStrength,
        float clusterRadius)
    {
        if (clusterStrength > 0f
            && clusterRadius > 0f
            && state.Resources.Count > 0
            && state.Random.NextSingle() < clusterStrength)
        {
            for (var attempt = 0; attempt < ClusterAttemptCount; attempt++)
            {
                if (!TryGetRandomLivePlantAnchor(state, out var anchor))
                {
                    break;
                }

                var angle = state.Random.NextSingle(0f, MathF.Tau);
                var radius = clusterRadius * MathF.Sqrt(state.Random.NextSingle());
                var candidate = state.Bounds.Clamp(anchor.Position + SimVector2.FromAngle(angle) * radius);

                if (CanPlacePlant(state, candidate))
                {
                    return candidate;
                }
            }
        }

        return state.Biomes.SampleResourcePosition(state.Random);
    }

    private static bool TryGetRandomLivePlantAnchor(WorldState state, out ResourcePatchState anchor)
    {
        var startIndex = state.Random.NextInt32(state.Resources.Count);

        for (var offset = 0; offset < state.Resources.Count; offset++)
        {
            var resource = state.Resources[(startIndex + offset) % state.Resources.Count];
            if (resource.Kind == ResourceKind.Plant && resource.Calories > 0f)
            {
                anchor = resource;
                return true;
            }
        }

        anchor = default;
        return false;
    }

    private static bool CanPlacePlant(WorldState state, SimVector2 position)
    {
        return !state.Biomes.IsInResourceVoid(position)
            && state.Biomes.GetResourceDensityMultiplierAt(position) > 0f;
    }
}
