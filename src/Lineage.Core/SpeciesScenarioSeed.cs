namespace Lineage.Core;

/// <summary>
/// Scenario-level starter species entry.
/// </summary>
///
/// <remarks>
/// This keeps repeatable multi-species setup in scenario JSON while species profiles
/// remain portable files that can be exported from any interesting run.
/// </remarks>
public sealed record SpeciesScenarioSeed
{
    public string ProfilePath { get; init; } = string.Empty;

    public int Count { get; init; } = 10;

    public InitialCreatureSpawnRegion SpawnRegion { get; init; } = InitialCreatureSpawnRegion.Uniform;

    public float? EnergyOverride { get; init; }

    public bool Enabled { get; init; } = true;

    public SpeciesScenarioSeed Validated()
    {
        if (string.IsNullOrWhiteSpace(ProfilePath))
        {
            throw new InvalidOperationException("Species seed profile path cannot be empty.");
        }

        if (Count <= 0)
        {
            throw new InvalidOperationException("Species seed count must be positive.");
        }

        if (!Enum.IsDefined(SpawnRegion))
        {
            throw new InvalidOperationException("Species seed spawn region must be defined.");
        }

        if (EnergyOverride is not null
            && (!float.IsFinite(EnergyOverride.Value) || EnergyOverride.Value <= 0f))
        {
            throw new InvalidOperationException("Species seed energy override must be finite and positive.");
        }

        return this with { ProfilePath = ProfilePath.Trim() };
    }
}
