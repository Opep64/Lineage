namespace Lineage.Core;

/// <summary>
/// Per-biome cost multipliers for environmental pressure that creatures experience indirectly.
/// </summary>
public readonly record struct BiomePressureProfile(
    float Barren,
    float Sparse,
    float Grassland,
    float Rich)
{
    public static BiomePressureProfile Neutral => new(1f, 1f, 1f, 1f);

    public float For(BiomeKind kind)
    {
        return kind switch
        {
            BiomeKind.Barren => Barren,
            BiomeKind.Sparse => Sparse,
            BiomeKind.Rich => Rich,
            _ => Grassland
        };
    }

    public static BiomePressureProfile Validate(BiomePressureProfile profile, string name)
    {
        ValidateMultiplier(profile.Barren, $"{name}.{nameof(Barren)}");
        ValidateMultiplier(profile.Sparse, $"{name}.{nameof(Sparse)}");
        ValidateMultiplier(profile.Grassland, $"{name}.{nameof(Grassland)}");
        ValidateMultiplier(profile.Rich, $"{name}.{nameof(Rich)}");
        return profile;
    }

    private static void ValidateMultiplier(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f)
        {
            throw new ArgumentOutOfRangeException(name, "Biome pressure multipliers must be finite and non-negative.");
        }
    }
}
