namespace Lineage.Core;

/// <summary>
/// High-level obstacle layouts used when constructing a scenario's static collision map.
/// </summary>
public enum ObstacleMapKind
{
    None,
    VerticalBarrierWithGaps,
    HorizontalBarrierWithGaps,
    ScatteredRocks
}
