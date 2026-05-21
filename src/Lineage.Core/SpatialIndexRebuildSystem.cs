namespace Lineage.Core;

/// <summary>
/// Pipeline pass that refreshes shared local-query data.
/// </summary>
public sealed class SpatialIndexRebuildSystem(UniformSpatialIndex spatialIndex) : ISimulationSystem
{
    public void Update(WorldState state, float deltaSeconds)
    {
        spatialIndex.Rebuild(state);
    }
}
