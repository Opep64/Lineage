namespace Lineage.Core;

/// <summary>
/// Root object for advancing a world by fixed or explicit time steps.
/// </summary>
///
/// <remarks>
/// This type is the boundary Godot and future CLI tools should call into. It owns no
/// rendering concepts and has no dependency on a game-engine update loop.
/// </remarks>
public sealed class Simulation
{
    private readonly ISimulationSystem[] _systems;

    public Simulation(SimulationConfig config, ulong seed, IEnumerable<ISimulationSystem>? systems = null)
    {
        Config = config.Validated();
        State = new WorldState(new WorldBounds(Config.WorldWidth, Config.WorldHeight), seed);
        _systems = systems?.ToArray() ?? [];
    }

    public SimulationConfig Config { get; }

    public WorldState State { get; }

    public void Step()
    {
        Step(Config.FixedDeltaSeconds);
    }

    public void Step(float deltaSeconds)
    {
        if (!float.IsFinite(deltaSeconds) || deltaSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Step duration must be finite and positive.");
        }

        foreach (var system in _systems)
        {
            system.Update(State, deltaSeconds);
        }

        State.AdvanceClock(deltaSeconds);
    }

    public void RunSteps(int stepCount)
    {
        if (stepCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stepCount), "Step count cannot be negative.");
        }

        for (var i = 0; i < stepCount; i++)
        {
            Step();
        }
    }
}
