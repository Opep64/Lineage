namespace Lineage.Core;

/// <summary>
/// High-level layout used when constructing a scenario's biome map.
/// </summary>
public enum BiomeMapKind
{
    GeneratedNoise,
    HorizontalBands,
    VerticalBands,
    HorizontalEdgeBands,
    VerticalEdgeBands,
    HorizontalEdgeLadderBands,
    VerticalEdgeLadderBands,
    VerticalEdgeCorridorBands,
    VerticalEdgeWideCorridorBands
}
