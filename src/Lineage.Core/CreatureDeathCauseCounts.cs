namespace Lineage.Core;

/// <summary>
/// Death-cause totals for one biome.
/// </summary>
public readonly record struct CreatureDeathCauseCounts(
    int Starvation,
    int Injury,
    int RottenMeat,
    int Unknown)
{
    public int Total => Starvation + Injury + RottenMeat + Unknown;

    public int For(CreatureDeathReason reason)
    {
        return reason switch
        {
            CreatureDeathReason.Starvation => Starvation,
            CreatureDeathReason.Injury => Injury,
            CreatureDeathReason.RottenMeat => RottenMeat,
            _ => Unknown
        };
    }

    public CreatureDeathCauseCounts Add(CreatureDeathReason reason)
    {
        return reason switch
        {
            CreatureDeathReason.Starvation => this with { Starvation = Starvation + 1 },
            CreatureDeathReason.Injury => this with { Injury = Injury + 1 },
            CreatureDeathReason.RottenMeat => this with { RottenMeat = RottenMeat + 1 },
            _ => this with { Unknown = Unknown + 1 }
        };
    }
}

/// <summary>
/// Death-cause totals grouped by canonical biome.
/// </summary>
public readonly record struct BiomeDeathCauseCounts(
    CreatureDeathCauseCounts Desert,
    CreatureDeathCauseCounts Scrubland,
    CreatureDeathCauseCounts Grassland,
    CreatureDeathCauseCounts Fertile,
    CreatureDeathCauseCounts Forest,
    CreatureDeathCauseCounts Wetland,
    CreatureDeathCauseCounts Tundra,
    CreatureDeathCauseCounts Highland)
{
    public int Total =>
        Desert.Total
        + Scrubland.Total
        + Grassland.Total
        + Fertile.Total
        + Forest.Total
        + Wetland.Total
        + Tundra.Total
        + Highland.Total;

    public CreatureDeathCauseCounts For(BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => Desert,
            BiomeKind.Scrubland => Scrubland,
            BiomeKind.Fertile => Fertile,
            BiomeKind.Forest => Forest,
            BiomeKind.Wetland => Wetland,
            BiomeKind.Tundra => Tundra,
            BiomeKind.Highland => Highland,
            _ => Grassland
        };
    }

    public BiomeDeathCauseCounts Add(BiomeKind biome, CreatureDeathReason reason)
    {
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => this with { Desert = Desert.Add(reason) },
            BiomeKind.Scrubland => this with { Scrubland = Scrubland.Add(reason) },
            BiomeKind.Fertile => this with { Fertile = Fertile.Add(reason) },
            BiomeKind.Forest => this with { Forest = Forest.Add(reason) },
            BiomeKind.Wetland => this with { Wetland = Wetland.Add(reason) },
            BiomeKind.Tundra => this with { Tundra = Tundra.Add(reason) },
            BiomeKind.Highland => this with { Highland = Highland.Add(reason) },
            _ => this with { Grassland = Grassland.Add(reason) }
        };
    }
}
