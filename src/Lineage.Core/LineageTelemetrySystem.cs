namespace Lineage.Core;

/// <summary>
/// Accumulates per-creature behavior and diet telemetry onto lineage records.
/// </summary>
public sealed class LineageTelemetrySystem : ISimulationSystem
{
    public void Update(WorldState state, float deltaSeconds)
    {
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            state.RecordCreatureTelemetry(state.Creatures[i], deltaSeconds);
        }
    }
}
