namespace Lineage.Core;

/// <summary>
/// Stable scenario-level settings needed to create a simulation world.
/// </summary>
public sealed record SimulationConfig
{
    public float WorldWidth { get; init; } = 1_000f;

    public float WorldHeight { get; init; } = 1_000f;

    /// <summary>
    /// Default step duration used by <see cref="Simulation.Step()"/>.
    /// </summary>
    public float FixedDeltaSeconds { get; init; } = 1f / 30f;

    public SimulationConfig Validated()
    {
        if (!float.IsFinite(WorldWidth) || WorldWidth <= 0)
        {
            throw new InvalidOperationException("World width must be finite and positive.");
        }

        if (!float.IsFinite(WorldHeight) || WorldHeight <= 0)
        {
            throw new InvalidOperationException("World height must be finite and positive.");
        }

        if (!float.IsFinite(FixedDeltaSeconds) || FixedDeltaSeconds <= 0)
        {
            throw new InvalidOperationException("Fixed delta seconds must be finite and positive.");
        }

        return this;
    }
}
