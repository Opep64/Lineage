namespace Lineage.Core;

/// <summary>
/// Fuzzy self-to-other creature similarity derived from inherited traits.
/// </summary>
internal static class CreatureSimilarity
{
    public const float SimilarContactThreshold = 0.85f;

    private const float ScentSimilarityFloor = 0.82f;

    public static float GeneticSimilarity(CreatureGenome left, CreatureGenome right)
    {
        var weightedDistance = 0f;
        var totalWeight = 0f;

        AddLogScaledDifference(left.BodyRadius, right.BodyRadius, 1f, 12f, 1.0f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.MaxSpeed, right.MaxSpeed, 2f, 80f, 0.8f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.MaxTurnRadiansPerSecond, right.MaxTurnRadiansPerSecond, 0.1f, 12f, 0.35f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.SenseRadius, right.SenseRadius, 5f, 300f, 0.7f, ref weightedDistance, ref totalWeight);
        AddLinearDifference(left.VisionAngleRadians, right.VisionAngleRadians, MathF.PI / 12f, MathF.Tau, 0.35f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.BasalEnergyPerSecond, right.BasalEnergyPerSecond, 0.01f, 5f, 0.55f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.MovementEnergyPerSecond, right.MovementEnergyPerSecond, 0.01f, 5f, 0.55f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.EatCaloriesPerSecond, right.EatCaloriesPerSecond, 1f, 100f, 0.45f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.ReproductionEnergyThreshold, right.ReproductionEnergyThreshold, 5f, 500f, 0.45f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.OffspringEnergyInvestment, right.OffspringEnergyInvestment, 5f, 200f, 0.45f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.EggIncubationSeconds, right.EggIncubationSeconds, 1f, 300f, 0.25f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.MaturityAgeSeconds, right.MaturityAgeSeconds, 10f, 600f, 0.35f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.ReproductionCooldownSeconds, right.ReproductionCooldownSeconds, 1f, 60f, 0.3f, ref weightedDistance, ref totalWeight);
        AddUnitDifference(left.DietaryAdaptation, right.DietaryAdaptation, 3.0f, ref weightedDistance, ref totalWeight);
        AddUnitDifference(left.CarrionAdaptation, right.CarrionAdaptation, 1.0f, ref weightedDistance, ref totalWeight);
        AddUnitDifference(left.TenderPlantAdaptation, right.TenderPlantAdaptation, 0.5f, ref weightedDistance, ref totalWeight);
        AddUnitDifference(left.RichPlantAdaptation, right.RichPlantAdaptation, 0.5f, ref weightedDistance, ref totalWeight);
        AddUnitDifference(left.ToughPlantAdaptation, right.ToughPlantAdaptation, 0.5f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.GutCapacityCalories, right.GutCapacityCalories, 5f, 250f, 0.35f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.DigestionCaloriesPerSecond, right.DigestionCaloriesPerSecond, 1f, 60f, 0.35f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.BiteStrength, right.BiteStrength, 0.05f, 4f, 2.5f, ref weightedDistance, ref totalWeight);
        AddLogScaledDifference(left.DamageResistance, right.DamageResistance, 0.25f, 4f, 1.0f, ref weightedDistance, ref totalWeight);

        return totalWeight > 0f
            ? Math.Clamp(1f - weightedDistance / totalWeight, 0f, 1f)
            : 0f;
    }

    public static float ScentWeight(float similarity)
    {
        var t = Math.Clamp((similarity - ScentSimilarityFloor) / (1f - ScentSimilarityFloor), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private static void AddUnitDifference(
        float left,
        float right,
        float weight,
        ref float weightedDistance,
        ref float totalWeight)
    {
        AddDifference(Math.Abs(Math.Clamp(left, 0f, 1f) - Math.Clamp(right, 0f, 1f)), weight, ref weightedDistance, ref totalWeight);
    }

    private static void AddLinearDifference(
        float left,
        float right,
        float min,
        float max,
        float weight,
        ref float weightedDistance,
        ref float totalWeight)
    {
        var scale = max - min;
        if (scale <= 0f)
        {
            return;
        }

        var normalizedLeft = (Math.Clamp(left, min, max) - min) / scale;
        var normalizedRight = (Math.Clamp(right, min, max) - min) / scale;
        AddDifference(Math.Abs(normalizedLeft - normalizedRight), weight, ref weightedDistance, ref totalWeight);
    }

    private static void AddLogScaledDifference(
        float left,
        float right,
        float min,
        float max,
        float weight,
        ref float weightedDistance,
        ref float totalWeight)
    {
        var logMin = MathF.Log(min);
        var logScale = MathF.Log(max) - logMin;
        if (logScale <= 0f)
        {
            return;
        }

        var normalizedLeft = (MathF.Log(Math.Clamp(left, min, max)) - logMin) / logScale;
        var normalizedRight = (MathF.Log(Math.Clamp(right, min, max)) - logMin) / logScale;
        AddDifference(Math.Abs(normalizedLeft - normalizedRight), weight, ref weightedDistance, ref totalWeight);
    }

    private static void AddDifference(
        float distance,
        float weight,
        ref float weightedDistance,
        ref float totalWeight)
    {
        if (weight <= 0f)
        {
            return;
        }

        weightedDistance += Math.Clamp(distance, 0f, 1f) * weight;
        totalWeight += weight;
    }
}
