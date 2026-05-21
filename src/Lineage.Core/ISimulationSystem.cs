namespace Lineage.Core;

/// <summary>
/// One deterministic pass in the simulation update pipeline.
/// </summary>
///
/// <remarks>
/// Systems should operate over <see cref="WorldState"/> collections by index. That
/// keeps the door open for data-oriented storage while still giving Phase 1 a simple
/// extension point.
/// </remarks>
public interface ISimulationSystem
{
    void Update(WorldState state, float deltaSeconds);
}
