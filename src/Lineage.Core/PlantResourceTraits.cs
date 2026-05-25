namespace Lineage.Core;

/// <summary>
/// Static food traits for a plant resource category.
/// </summary>
public readonly record struct PlantResourceTraitProfile(
    float InitialCaloriesMultiplier,
    float MaxCaloriesMultiplier,
    float RegrowthMultiplier,
    float EatRateMultiplier,
    float DigestionEnergyMultiplier,
    float RadiusMultiplier);

/// <summary>
/// Centralizes plant subtype tuning so spawning, eating, and digestion stay consistent.
/// </summary>
public static class PlantResourceTraits
{
    private const float MinimumDigestionQualityMultiplier = 0.75f;
    private const float MaximumDigestionQualityMultiplier = 1.1f;
    private const float MinimumBiteEaseMultiplier = 0.55f;
    private const float MaximumBiteEaseMultiplier = 1.3f;

    public static PlantResourceTraitProfile For(PlantResourceKind kind)
    {
        return kind switch
        {
            PlantResourceKind.Tender => new PlantResourceTraitProfile(
                InitialCaloriesMultiplier: 0.75f,
                MaxCaloriesMultiplier: 0.8f,
                RegrowthMultiplier: 1.45f,
                EatRateMultiplier: 1.25f,
                DigestionEnergyMultiplier: 1.0f,
                RadiusMultiplier: 0.9f),
            PlantResourceKind.Rich => new PlantResourceTraitProfile(
                InitialCaloriesMultiplier: 1.25f,
                MaxCaloriesMultiplier: 1.35f,
                RegrowthMultiplier: 0.65f,
                EatRateMultiplier: 0.9f,
                DigestionEnergyMultiplier: 1.05f,
                RadiusMultiplier: 1.1f),
            PlantResourceKind.Tough => new PlantResourceTraitProfile(
                InitialCaloriesMultiplier: 1.1f,
                MaxCaloriesMultiplier: 1.2f,
                RegrowthMultiplier: 0.85f,
                EatRateMultiplier: 0.6f,
                DigestionEnergyMultiplier: 0.78f,
                RadiusMultiplier: 1.15f),
            _ => new PlantResourceTraitProfile(
                InitialCaloriesMultiplier: 1f,
                MaxCaloriesMultiplier: 1f,
                RegrowthMultiplier: 1f,
                EatRateMultiplier: 1f,
                DigestionEnergyMultiplier: 1f,
                RadiusMultiplier: 1f)
        };
    }

    public static float EatingRateMultiplier(PlantResourceKind kind)
    {
        return For(kind).EatRateMultiplier;
    }

    public static float DigestionEnergyMultiplier(PlantResourceKind kind)
    {
        return For(kind).DigestionEnergyMultiplier;
    }

    /// <summary>
    /// Optional biome affinity used by scenarios that want plant types to form ecological niches.
    /// Generic plants stay broadly distributed; typed plants become more common in their favored biomes.
    /// </summary>
    public static float HabitatAffinityMultiplier(PlantResourceKind kind, BiomeKind biome)
    {
        return kind switch
        {
            PlantResourceKind.Tender => biome switch
            {
                BiomeKind.Barren => 0.2f,
                BiomeKind.Sparse => 1.25f,
                BiomeKind.Rich => 0.8f,
                _ => 1.8f
            },
            PlantResourceKind.Rich => biome switch
            {
                BiomeKind.Barren => 0.05f,
                BiomeKind.Sparse => 0.3f,
                BiomeKind.Rich => 2.8f,
                _ => 0.8f
            },
            PlantResourceKind.Tough => biome switch
            {
                BiomeKind.Barren => 3f,
                BiomeKind.Sparse => 1.8f,
                BiomeKind.Rich => 0.2f,
                _ => 0.55f
            },
            _ => 1f
        };
    }

    /// <summary>
    /// Normalized cue for how much useful energy this plant type tends to release after digestion.
    /// This is a sensory/taste signal, not a direct calorie conversion.
    /// </summary>
    public static float EnergyQualitySense(PlantResourceKind kind)
    {
        return NormalizeSense(
            DigestionEnergyMultiplier(kind),
            MinimumDigestionQualityMultiplier,
            MaximumDigestionQualityMultiplier);
    }

    /// <summary>
    /// Normalized cue for how easy this plant type is to bite and transfer into the gut.
    /// </summary>
    public static float BiteEaseSense(PlantResourceKind kind)
    {
        return NormalizeSense(
            EatingRateMultiplier(kind),
            MinimumBiteEaseMultiplier,
            MaximumBiteEaseMultiplier);
    }

    private static float NormalizeSense(float value, float minimum, float maximum)
    {
        return Math.Clamp((value - minimum) / (maximum - minimum), 0f, 1f);
    }
}
