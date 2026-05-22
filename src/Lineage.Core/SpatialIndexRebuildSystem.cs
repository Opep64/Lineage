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

/// <summary>
/// Refreshes only creature positions after movement when resources and eggs have not moved.
/// </summary>
public sealed class CreatureSpatialIndexRebuildSystem(UniformSpatialIndex spatialIndex) : ISimulationSystem
{
    public void Update(WorldState state, float deltaSeconds)
    {
        spatialIndex.RebuildCreatures(state);
    }
}
