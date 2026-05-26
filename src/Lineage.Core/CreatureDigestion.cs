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
    private const float TenderPlantAdaptationBonus = 0.4f;
    private const float RichPlantAdaptationBonus = 0.55f;
    private const float ToughPlantAdaptationBonus = 0.85f;
    private const float CrossPlantAdaptationPenalty = 0.18f;
    private const float GenericPlantAdaptationPenalty = 0.08f;
    private const float MinimumPlantTypeAdaptationMultiplier = 0.55f;

    public static float PlantEfficiency(CreatureGenome genome)
    {
        return Efficiency(1f - genome.DietaryAdaptation);
    }

    public static float PlantTypeEnergyEfficiency(CreatureGenome genome, PlantResourceKind plantKind)
    {
        return PlantEfficiency(genome)
            * PlantResourceTraits.DigestionEnergyMultiplier(plantKind)
            * PlantTypeAdaptationMultiplier(genome, plantKind);
    }

    public static float PlantSpecializationUpkeepFactor(CreatureGenome genome)
    {
        var tender = Math.Clamp(genome.TenderPlantAdaptation, 0f, 1f);
        var rich = Math.Clamp(genome.RichPlantAdaptation, 0f, 1f);
        var tough = Math.Clamp(genome.ToughPlantAdaptation, 0f, 1f);
        return tender * tender + rich * rich + tough * tough;
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

    private static float PlantTypeAdaptationMultiplier(CreatureGenome genome, PlantResourceKind plantKind)
    {
        var tender = Math.Clamp(genome.TenderPlantAdaptation, 0f, 1f);
        var rich = Math.Clamp(genome.RichPlantAdaptation, 0f, 1f);
        var tough = Math.Clamp(genome.ToughPlantAdaptation, 0f, 1f);
        var multiplier = plantKind switch
        {
            PlantResourceKind.Tender => 1f
                + tender * TenderPlantAdaptationBonus
                - (rich + tough) * CrossPlantAdaptationPenalty,
            PlantResourceKind.Rich => 1f
                + rich * RichPlantAdaptationBonus
                - (tender + tough) * CrossPlantAdaptationPenalty,
            PlantResourceKind.Tough => 1f
                + tough * ToughPlantAdaptationBonus
                - (tender + rich) * CrossPlantAdaptationPenalty,
            PlantResourceKind.Generic => 1f
                - (tender + rich + tough) * GenericPlantAdaptationPenalty,
            _ => 1f
        };
        return Math.Clamp(multiplier, MinimumPlantTypeAdaptationMultiplier, 2f);
    }
}
