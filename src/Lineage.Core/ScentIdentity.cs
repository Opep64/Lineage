namespace Lineage.Core;

/// <summary>
/// Heritable scent-signature comparison independent of trait similarity or recorded lineage.
/// </summary>
internal static class ScentIdentity
{
    public const float IdentityContactThreshold = 0.86f;

    private const float ScentSimilarityFloor = 0.88f;
    private const float MaximumSignatureDistance = 1.7320508f;

    public static float SignatureSimilarity(CreatureGenome left, CreatureGenome right)
    {
        var dx = Math.Clamp(left.ScentSignatureA, 0f, 1f) - Math.Clamp(right.ScentSignatureA, 0f, 1f);
        var dy = Math.Clamp(left.ScentSignatureB, 0f, 1f) - Math.Clamp(right.ScentSignatureB, 0f, 1f);
        var dz = Math.Clamp(left.ScentSignatureC, 0f, 1f) - Math.Clamp(right.ScentSignatureC, 0f, 1f);
        var distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        return Math.Clamp(1f - distance / MaximumSignatureDistance, 0f, 1f);
    }

    public static float ScentWeight(float similarity)
    {
        var t = Math.Clamp((similarity - ScentSimilarityFloor) / (1f - ScentSimilarityFloor), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
