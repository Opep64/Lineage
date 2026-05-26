namespace Lineage.Core;

/// <summary>
/// Drops genome and brain payloads that are no longer referenced by living creatures or eggs.
/// </summary>
public sealed class ExtinctPayloadPruningSystem(int intervalTicks = 1_000) : ISimulationSystem
{
    private readonly int _intervalTicks = intervalTicks > 0
        ? intervalTicks
        : throw new ArgumentOutOfRangeException(nameof(intervalTicks), "Pruning interval must be positive.");

    public void Update(WorldState state, float deltaSeconds)
    {
        if (state.Tick % _intervalTicks != 0)
        {
            return;
        }

        state.PruneExtinctPayloads();
    }
}
