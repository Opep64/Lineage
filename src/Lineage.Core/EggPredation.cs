namespace Lineage.Core;

/// <summary>
/// Shared egg predation helpers used by sensing, eating, and viewer code.
/// </summary>
public static class EggPredation
{
    public static float ContactRadius(EggState egg)
    {
        var healthScale = MathF.Sqrt(MathF.Max(0f, egg.MaxHealth));
        return Math.Clamp(2f + healthScale * 1.2f, 2f, 7f);
    }
}
