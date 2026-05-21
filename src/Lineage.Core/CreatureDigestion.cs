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

    public static float PlantEfficiency(CreatureGenome genome)
    {
        return Efficiency(1f - genome.DietaryAdaptation);
    }

    public static float MeatEfficiency(CreatureGenome genome)
    {
        return Efficiency(genome.DietaryAdaptation);
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
