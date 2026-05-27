namespace Lineage.Core;

/// <summary>
/// Per-biome multipliers for environmental pressure that creatures experience indirectly.
/// </summary>
public readonly record struct BiomePressureProfile
{
    public BiomePressureProfile(float desert, float scrubland, float grassland, float fertile)
        : this(
            desert,
            scrubland,
            grassland,
            fertile,
            grassland,
            fertile,
            desert,
            scrubland)
    {
    }

    public BiomePressureProfile(
        float desert,
        float scrubland,
        float grassland,
        float fertile,
        float forest,
        float wetland,
        float tundra,
        float highland)
    {
        Desert = desert;
        Scrubland = scrubland;
        Grassland = grassland;
        Fertile = fertile;
        Forest = forest;
        Wetland = wetland;
        Tundra = tundra;
        Highland = highland;
    }

    public float Desert { get; init; }

    public float Scrubland { get; init; }

    public float Grassland { get; init; }

    public float Fertile { get; init; }

    public float Forest { get; init; }

    public float Wetland { get; init; }

    public float Tundra { get; init; }

    public float Highland { get; init; }

    public float Barren => Desert;

    public float Sparse => Scrubland;

    public float Rich => Fertile;

    public static BiomePressureProfile Neutral => new(1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f);

    public float For(BiomeKind kind)
    {
        return BiomeKinds.Canonicalize(kind) switch
        {
            BiomeKind.Desert => Desert,
            BiomeKind.Scrubland => Scrubland,
            BiomeKind.Grassland => Grassland,
            BiomeKind.Fertile => Fertile,
            BiomeKind.Forest => Forest,
            BiomeKind.Wetland => Wetland,
            BiomeKind.Tundra => Tundra,
            BiomeKind.Highland => Highland,
            _ => Grassland
        };
    }

    public static BiomePressureProfile Validate(BiomePressureProfile profile, string name)
    {
        ValidateMultiplier(profile.Desert, $"{name}.{nameof(Desert)}");
        ValidateMultiplier(profile.Scrubland, $"{name}.{nameof(Scrubland)}");
        ValidateMultiplier(profile.Grassland, $"{name}.{nameof(Grassland)}");
        ValidateMultiplier(profile.Fertile, $"{name}.{nameof(Fertile)}");
        ValidateMultiplier(profile.Forest, $"{name}.{nameof(Forest)}");
        ValidateMultiplier(profile.Wetland, $"{name}.{nameof(Wetland)}");
        ValidateMultiplier(profile.Tundra, $"{name}.{nameof(Tundra)}");
        ValidateMultiplier(profile.Highland, $"{name}.{nameof(Highland)}");
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
