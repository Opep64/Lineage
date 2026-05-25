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
}
