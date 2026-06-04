namespace Lineage.Core;

/// <summary>
/// Shared juvenile-to-adult growth helpers for systems that need effective traits.
/// </summary>
///
/// <remarks>
/// Genomes describe adult traits. The growth factor scales traits that should be
/// weaker while a creature is still juvenile, giving offspring investment and
/// maturity age room to create r/K-style tradeoffs.
/// </remarks>
public static class CreatureGrowth
{
    private const float MinimumJuvenileScale = 0.35f;

    public static float MaturityProgress(CreatureState creature, CreatureGenome genome)
    {
        var effectiveMaturityAge = CreatureMetabolism.EffectiveMaturityAgeSeconds(genome);
        return effectiveMaturityAge <= 0f
            ? 1f
            : Math.Clamp(creature.AgeSeconds / effectiveMaturityAge, 0f, 1f);
    }

    public static float GrowthFactor(CreatureState creature, CreatureGenome genome)
    {
        var progress = MaturityProgress(creature, genome);
        var smoothed = progress * progress * (3f - 2f * progress);
        var newbornScale = Math.Clamp(
            MinimumJuvenileScale * OffspringDevelopment.JuvenileGrowthScale(creature.BirthInvestmentRatio),
            0.18f,
            0.7f);
        return newbornScale + (1f - newbornScale) * smoothed;
    }

    public static bool IsMature(CreatureState creature, CreatureGenome genome)
    {
        return MaturityProgress(creature, genome) >= 1f;
    }

    public static float EffectiveBodyRadius(CreatureState creature, CreatureGenome genome)
    {
        return genome.BodyRadius * GrowthFactor(creature, genome);
    }

    public static float EffectiveMaxSpeed(CreatureState creature, CreatureGenome genome)
    {
        return genome.MaxSpeed
            * MathF.Sqrt(GrowthFactor(creature, genome))
            * CreatureMetabolism.LocomotionRateMultiplier(genome)
            * FatSpeedMultiplier(creature, genome);
    }

    public static float EffectiveMaxTurnRadiansPerSecond(CreatureState creature, CreatureGenome genome)
    {
        return genome.MaxTurnRadiansPerSecond
            * MathF.Sqrt(GrowthFactor(creature, genome))
            * CreatureMetabolism.LocomotionRateMultiplier(genome);
    }

    public static float EffectiveSenseRadius(CreatureState creature, CreatureGenome genome)
    {
        return genome.SenseRadius * MathF.Sqrt(GrowthFactor(creature, genome));
    }

    public static float EffectiveVisionAngleRadians(CreatureState creature, CreatureGenome genome)
    {
        return genome.VisionAngleRadians;
    }

    public static float EffectiveEatCaloriesPerSecond(CreatureState creature, CreatureGenome genome)
    {
        return genome.EatCaloriesPerSecond * GrowthFactor(creature, genome);
    }

    public static float EffectiveGutCapacityCalories(CreatureState creature, CreatureGenome genome)
    {
        return genome.GutCapacityCalories * GrowthFactor(creature, genome);
    }

    public static float EffectiveFatStorageCapacityCalories(CreatureState creature, CreatureGenome genome)
    {
        return genome.FatStorageCapacityCalories * GrowthFactor(creature, genome);
    }

    public static float FatStorageRatio(CreatureState creature, CreatureGenome genome)
    {
        var capacity = EffectiveFatStorageCapacityCalories(creature, genome);
        return capacity > 0f
            ? Math.Clamp(creature.FatCalories / capacity, 0f, 1f)
            : 0f;
    }

    public static float FatMassBurdenRatio(CreatureState creature, CreatureGenome genome)
    {
        var reference = Math.Max(1f, genome.ReproductionEnergyThreshold);
        return Math.Clamp(creature.FatCalories / reference, 0f, 1f);
    }

    public static float FatSpeedMultiplier(CreatureState creature, CreatureGenome genome)
    {
        return Math.Clamp(1f - FatMassBurdenRatio(creature, genome) * 0.12f, 0.78f, 1f);
    }

    public static float FatMovementCostMultiplier(CreatureState creature, CreatureGenome genome)
    {
        return 1f + FatMassBurdenRatio(creature, genome) * 0.25f;
    }

    public static float EffectiveDigestionCaloriesPerSecond(CreatureState creature, CreatureGenome genome)
    {
        return genome.DigestionCaloriesPerSecond
            * GrowthFactor(creature, genome)
            * CreatureMetabolism.DigestionRateMultiplier(genome);
    }

    public static float EffectiveBiteStrength(CreatureState creature, CreatureGenome genome)
    {
        return genome.BiteStrength * GrowthFactor(creature, genome);
    }

    public static float EffectiveDamageResistance(CreatureState creature, CreatureGenome genome)
    {
        return genome.DamageResistance * GrowthFactor(creature, genome);
    }
}
