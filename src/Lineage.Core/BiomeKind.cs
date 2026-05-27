namespace Lineage.Core;

/// <summary>
/// Coarse ecological regions used by resource spawning and viewer overlays.
/// </summary>
public enum BiomeKind
{
    Desert,
    Scrubland,
    Grassland,
    Fertile,
    Forest,
    Wetland,
    Tundra,
    Highland,

    // Legacy names kept so older snapshots/scenarios and existing authored maps
    // continue to resolve to the renamed ecological categories.
    Barren = Desert,
    Sparse = Scrubland,
    Rich = Fertile
}

public static class BiomeKinds
{
    public static IReadOnlyList<BiomeKind> All { get; } =
    [
        BiomeKind.Desert,
        BiomeKind.Scrubland,
        BiomeKind.Grassland,
        BiomeKind.Fertile,
        BiomeKind.Forest,
        BiomeKind.Wetland,
        BiomeKind.Tundra,
        BiomeKind.Highland
    ];

    public static BiomeKind Canonicalize(BiomeKind kind)
    {
        return kind switch
        {
            BiomeKind.Desert => BiomeKind.Desert,
            BiomeKind.Scrubland => BiomeKind.Scrubland,
            BiomeKind.Grassland => BiomeKind.Grassland,
            BiomeKind.Fertile => BiomeKind.Fertile,
            BiomeKind.Forest => BiomeKind.Forest,
            BiomeKind.Wetland => BiomeKind.Wetland,
            BiomeKind.Tundra => BiomeKind.Tundra,
            BiomeKind.Highland => BiomeKind.Highland,
            _ => BiomeKind.Grassland
        };
    }
}
