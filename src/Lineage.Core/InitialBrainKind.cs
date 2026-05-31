namespace Lineage.Core;

/// <summary>
/// Controls how founder neural brains are seeded from a scenario.
/// </summary>
public enum InitialBrainKind
{
    SeedForager,
    ExplorerForager,
    SectorForager,
    OpportunisticForager,
    ScavengerForager,
    FreshnessAwareScavenger,
    ForagerPredator,
    SparseGraphForager,
    RandomPerFounder
}
