namespace Lineage.Core;

/// <summary>
/// Converts carcass age into nutritional quality. Stale meat still has value, but
/// it releases less usable energy than a fresh kill for the same gut space.
/// </summary>
public static class MeatQuality
{
    public const float StaleAgeSeconds = 300f;
    public const float MinimumFreshness = 0.35f;
    public const float FreshThreshold = 0.75f;

    public static float Freshness(ResourcePatchState resource)
    {
        if (resource.Kind != ResourceKind.Meat)
        {
            return 1f;
        }

        var age = float.IsFinite(resource.MeatAgeSeconds)
            ? Math.Max(0f, resource.MeatAgeSeconds)
            : 0f;
        var ageFactor = 1f - Math.Clamp(age / StaleAgeSeconds, 0f, 1f);
        return MinimumFreshness + (1f - MinimumFreshness) * ageFactor;
    }

    public static bool IsFresh(ResourcePatchState resource)
    {
        return IsFresh(Freshness(resource));
    }

    public static bool IsFresh(float freshness)
    {
        return freshness >= FreshThreshold;
    }

    public static float Staleness(ResourcePatchState resource)
    {
        return Staleness(Freshness(resource));
    }

    public static float Staleness(float freshness)
    {
        return Math.Clamp((1f - freshness) / (1f - MinimumFreshness), 0f, 1f);
    }
}
