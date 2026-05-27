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
    private const float MinimumDigestionQualityMultiplier = 0.5f;
    private const float MaximumDigestionQualityMultiplier = 1.05f;
    private const float MinimumBiteEaseMultiplier = 0.45f;
    private const float MaximumBiteEaseMultiplier = 1.5f;

    public static PlantResourceTraitProfile For(PlantResourceKind kind)
    {
        return kind switch
        {
            PlantResourceKind.Tender => new PlantResourceTraitProfile(
                InitialCaloriesMultiplier: 0.68f,
                MaxCaloriesMultiplier: 0.7f,
                RegrowthMultiplier: 1.6f,
                EatRateMultiplier: 1.5f,
                DigestionEnergyMultiplier: 0.85f,
                RadiusMultiplier: 0.85f),
            PlantResourceKind.Rich => new PlantResourceTraitProfile(
                InitialCaloriesMultiplier: 1.45f,
                MaxCaloriesMultiplier: 1.7f,
                RegrowthMultiplier: 0.55f,
                EatRateMultiplier: 0.65f,
                DigestionEnergyMultiplier: 1.05f,
                RadiusMultiplier: 1.15f),
            PlantResourceKind.Tough => new PlantResourceTraitProfile(
                InitialCaloriesMultiplier: 1.05f,
                MaxCaloriesMultiplier: 1.1f,
                RegrowthMultiplier: 0.75f,
                EatRateMultiplier: 0.45f,
                DigestionEnergyMultiplier: 0.5f,
                RadiusMultiplier: 1.1f),
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
                BiomeKind.Desert => 0.2f,
                BiomeKind.Scrubland => 1.25f,
                BiomeKind.Grassland => 1.7f,
                BiomeKind.Fertile => 1.4f,
                BiomeKind.Forest => 1.35f,
                BiomeKind.Wetland => 1.25f,
                BiomeKind.Tundra => 0.5f,
                BiomeKind.Highland => 0.9f,
                _ => 1.0f
            },
            PlantResourceKind.Rich => biome switch
            {
                BiomeKind.Desert => 0.05f,
                BiomeKind.Scrubland => 0.3f,
                BiomeKind.Grassland => 0.75f,
                BiomeKind.Fertile => 2.2f,
                BiomeKind.Forest => 1.85f,
                BiomeKind.Wetland => 1.65f,
                BiomeKind.Tundra => 0.15f,
                BiomeKind.Highland => 0.4f,
                _ => 0.8f
            },
            PlantResourceKind.Tough => biome switch
            {
                BiomeKind.Desert => 3f,
                BiomeKind.Scrubland => 1.8f,
                BiomeKind.Grassland => 0.85f,
                BiomeKind.Fertile => 0.35f,
                BiomeKind.Forest => 0.65f,
                BiomeKind.Wetland => 0.55f,
                BiomeKind.Tundra => 1.6f,
                BiomeKind.Highland => 1.4f,
                _ => 0.65f
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

    /// <summary>
    /// Fuzzy association cue between an observed plant profile and recent typed plant payoff.
    /// </summary>
    public static float PayoffPreferenceCue(
        float energyQualitySense,
        float biteEaseSense,
        float tenderPayoffTrace,
        float richPayoffTrace,
        float toughPayoffTrace)
    {
        var energyQuality = Math.Clamp(energyQualitySense, 0f, 1f);
        var biteEase = Math.Clamp(biteEaseSense, 0f, 1f);
        var tender = PlantKindProfileMatch(energyQuality, biteEase, PlantResourceKind.Tender)
            * Math.Clamp(tenderPayoffTrace, 0f, 1f);
        var rich = PlantKindProfileMatch(energyQuality, biteEase, PlantResourceKind.Rich)
            * Math.Clamp(richPayoffTrace, 0f, 1f);
        var tough = PlantKindProfileMatch(energyQuality, biteEase, PlantResourceKind.Tough)
            * Math.Clamp(toughPayoffTrace, 0f, 1f);

        return Math.Clamp(tender + rich + tough, 0f, 1f);
    }

    private static float NormalizeSense(float value, float minimum, float maximum)
    {
        return Math.Clamp((value - minimum) / (maximum - minimum), 0f, 1f);
    }

    private static float PlantKindProfileMatch(float energyQuality, float biteEase, PlantResourceKind kind)
    {
        var energyMatch = 1f - Math.Abs(energyQuality - EnergyQualitySense(kind));
        var biteMatch = 1f - Math.Abs(biteEase - BiteEaseSense(kind));
        var match = Math.Clamp(energyMatch, 0f, 1f) * Math.Clamp(biteMatch, 0f, 1f);
        return match * match;
    }
}
