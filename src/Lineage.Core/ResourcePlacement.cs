namespace Lineage.Core;

/// <summary>
/// Shared plant placement rules for initial world seeding and depleted-plant relocation.
/// </summary>
///
/// <remarks>
/// Biomes decide where plants are allowed and how attractive each area is. Clustering
/// adds patchiness by sometimes placing a new plant near an existing live plant,
/// while local dispersal lets depleted plants reseed near their previous patch before
/// falling back to broader placement rules. Both create food islands without giving
/// creatures any direct knowledge about the underlying fertility map.
/// </remarks>
public static class ResourcePlacement
{
    private const int ClusterAttemptCount = 8;
    private const int LocalDispersalAttemptCount = 12;

    public static SimVector2 SamplePlantPosition(
        WorldState state,
        float clusterStrength,
        float clusterRadius,
        SimVector2? localDispersalOrigin = null,
        float localDispersalChance = 0f,
        float localDispersalRadius = 0f)
    {
        return SamplePlantPosition(
            state,
            clusterStrength,
            clusterRadius,
            out _,
            localDispersalOrigin,
            localDispersalChance,
            localDispersalRadius);
    }

    public static SimVector2 SamplePlantPosition(
        WorldState state,
        float clusterStrength,
        float clusterRadius,
        out PlantPlacementMode placementMode,
        SimVector2? localDispersalOrigin = null,
        float localDispersalChance = 0f,
        float localDispersalRadius = 0f)
    {
        if (TrySamplePlantPosition(
            state,
            clusterStrength,
            clusterRadius,
            out var position,
            out placementMode,
            localDispersalOrigin,
            localDispersalChance,
            localDispersalRadius))
        {
            return position;
        }

        placementMode = PlantPlacementMode.Global;
        return state.Biomes.SampleResourcePosition(state.Random);
    }

    public static bool TrySamplePlantPosition(
        WorldState state,
        float clusterStrength,
        float clusterRadius,
        out SimVector2 position,
        out PlantPlacementMode placementMode,
        SimVector2? localDispersalOrigin = null,
        float localDispersalChance = 0f,
        float localDispersalRadius = 0f,
        BiomeKind? habitatBiomeKind = null)
    {
        if (localDispersalOrigin is { } origin
            && localDispersalChance > 0f
            && localDispersalRadius > 0f
            && state.Random.NextSingle() < localDispersalChance)
        {
            for (var attempt = 0; attempt < LocalDispersalAttemptCount; attempt++)
            {
                var angle = state.Random.NextSingle(0f, MathF.Tau);
                var radius = localDispersalRadius * MathF.Sqrt(state.Random.NextSingle());
                var candidate = state.Bounds.Clamp(origin + SimVector2.FromAngle(angle) * radius);

                if (CanPlacePlantAt(state, candidate, habitatBiomeKind))
                {
                    placementMode = PlantPlacementMode.LocalDispersal;
                    position = candidate;
                    return true;
                }
            }
        }

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

                if (CanPlacePlantAt(state, candidate, habitatBiomeKind))
                {
                    placementMode = PlantPlacementMode.Cluster;
                    position = candidate;
                    return true;
                }
            }
        }

        placementMode = PlantPlacementMode.Global;
        return TrySampleOpenBiomeResourcePosition(state, habitatBiomeKind, out position);
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

    internal static bool CanPlacePlantAt(
        WorldState state,
        SimVector2 position,
        BiomeKind? habitatBiomeKind = null)
    {
        if (state.Biomes.IsInResourceVoid(position)
            || state.Biomes.GetResourceDensityMultiplierAt(position) <= 0f
            || state.Obstacles.IsBlockedAt(position))
        {
            return false;
        }

        if (habitatBiomeKind is { } habitat
            && state.Biomes.GetKindAt(position) != habitat)
        {
            return false;
        }

        var localFertility = state.GetEffectiveLocalFertilityMultiplierAt(position);
        return localFertility >= 0.999f || state.Random.NextSingle() <= localFertility;
    }

    private static bool TrySampleOpenBiomeResourcePosition(
        WorldState state,
        BiomeKind? habitatBiomeKind,
        out SimVector2 position)
    {
        for (var attempt = 0; attempt < 64; attempt++)
        {
            SimVector2 sampled;
            if (habitatBiomeKind is { } habitat)
            {
                if (!state.Biomes.TrySampleResourcePosition(state.Random, habitat, out sampled))
                {
                    break;
                }
            }
            else
            {
                sampled = state.Biomes.SampleResourcePosition(state.Random);
            }

            if (CanPlacePlantAt(state, sampled, habitatBiomeKind))
            {
                position = sampled;
                return true;
            }
        }

        position = default;
        return false;
    }
}
