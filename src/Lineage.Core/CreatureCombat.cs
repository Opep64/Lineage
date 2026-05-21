namespace Lineage.Core;

/// <summary>
/// Shared combat formulas for living-creature predation.
/// </summary>
public static class CreatureCombat
{
    public static float BiteDamagePerSecond(CreatureState attacker, CreatureGenome attackerGenome, float baseDamagePerSecond)
    {
        var adultScale = CreatureGrowth.EffectiveBodyRadius(attacker, attackerGenome) / CreatureGenome.Baseline.BodyRadius;
        return baseDamagePerSecond * MathF.Sqrt(MathF.Max(0.1f, adultScale));
    }
}
