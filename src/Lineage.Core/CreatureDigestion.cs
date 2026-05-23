namespace Lineage.Core;

/// <summary>
/// Maps a creature's diet gene to plant/meat calorie extraction efficiency.
/// </summary>
///
/// <remarks>
/// The single diet axis keeps transitions gradual: plant specialists can still gain
/// a little from meat, omnivores can use both moderately, and meat specialists give
/// up plant efficiency instead of becoming universally optimal.
/// </remarks>
public static class CreatureDigestion
{
    public const float MinimumEfficiency = 0.2f;
    public const float MaximumEfficiency = 1f;
    public const float FullCarrionFreshMeatPenalty = 0.25f;
    public const float FullCarrionStaleMeatRecovery = 0.85f;
    public const float FullCarrionRottenMeatProtection = 0.9f;

    public static float PlantEfficiency(CreatureGenome genome)
    {
        return Efficiency(1f - genome.DietaryAdaptation);
    }

    public static float MeatEfficiency(CreatureGenome genome)
    {
        return Efficiency(genome.DietaryAdaptation);
    }

    public static float MeatEnergyEfficiency(CreatureGenome genome, float meatFreshness)
    {
        return MeatEfficiency(genome) * MeatFreshnessEfficiency(genome, meatFreshness);
    }

    public static float FreshMeatEnergyEfficiency(CreatureGenome genome)
    {
        return MeatEnergyEfficiency(genome, meatFreshness: 1f);
    }

    public static float StaleMeatEnergyEfficiency(CreatureGenome genome)
    {
        return MeatEnergyEfficiency(genome, MeatQuality.MinimumFreshness);
    }

    public static float RottenMeatDamageMultiplier(CreatureGenome genome, float meatFreshness)
    {
        var freshness = Math.Clamp(meatFreshness, MeatQuality.MinimumFreshness, 1f);
        var carrion = Math.Clamp(genome.CarrionAdaptation, 0f, 1f);
        var staleFactor = (1f - freshness) / (1f - MeatQuality.MinimumFreshness);
        return Math.Clamp(staleFactor * (1f - carrion * FullCarrionRottenMeatProtection), 0f, 1f);
    }

    public static float MeatFreshnessEfficiency(CreatureGenome genome, float meatFreshness)
    {
        var freshness = Math.Clamp(meatFreshness, MeatQuality.MinimumFreshness, 1f);
        var carrion = Math.Clamp(genome.CarrionAdaptation, 0f, 1f);
        var staleFactor = (1f - freshness) / (1f - MeatQuality.MinimumFreshness);
        var freshFactor = 1f - staleFactor;
        var recoveredStaleValue = staleFactor * FullCarrionStaleMeatRecovery * (1f - freshness);
        var lostFreshValue = freshFactor * FullCarrionFreshMeatPenalty * freshness;
        return Math.Clamp(freshness + carrion * (recoveredStaleValue - lostFreshValue), 0f, 1f);
    }

    public static float EfficiencyFor(CreatureGenome genome, ResourceKind kind)
    {
        return kind switch
        {
            ResourceKind.Meat => MeatEfficiency(genome),
            _ => PlantEfficiency(genome)
        };
    }

    private static float Efficiency(float specialization)
    {
        return MinimumEfficiency
            + (MaximumEfficiency - MinimumEfficiency) * Math.Clamp(specialization, 0f, 1f);
    }
}
