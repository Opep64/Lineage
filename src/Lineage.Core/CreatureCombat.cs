namespace Lineage.Core;

/// <summary>
/// Shared combat formulas for living-creature predation.
/// </summary>
public static class CreatureCombat
{
    public static float BiteDamagePerSecond(CreatureState attacker, CreatureGenome attackerGenome, float baseDamagePerSecond)
    {
        var adultScale = CreatureGrowth.EffectiveBodyRadius(attacker, attackerGenome) / CreatureGenome.Baseline.BodyRadius;
        var biteStrength = MathF.Max(0.05f, CreatureGrowth.EffectiveBiteStrength(attacker, attackerGenome));
        return baseDamagePerSecond * biteStrength * MathF.Sqrt(MathF.Max(0.1f, adultScale));
    }

    public static float BiteEnergyCostPerSecond(CreatureState attacker, CreatureGenome attackerGenome, float baseCostPerSecond)
    {
        var biteStrength = MathF.Max(0.05f, CreatureGrowth.EffectiveBiteStrength(attacker, attackerGenome));
        return baseCostPerSecond * biteStrength;
    }

    public static float ApplyDamageResistance(float incomingDamage, CreatureState target, CreatureGenome targetGenome)
    {
        var resistance = MathF.Max(0.1f, CreatureGrowth.EffectiveDamageResistance(target, targetGenome));
        return incomingDamage / MathF.Sqrt(resistance);
    }
}
