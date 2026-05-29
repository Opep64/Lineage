namespace Lineage.Core;

/// <summary>
/// Mutation pressure applied by the world at reproduction time.
/// </summary>
public readonly record struct MutationProfile(
    float MutationStrength,
    float TraitMutationRate,
    float BrainMutationRate)
{
    public static MutationProfile Default => new(0.06f, 0.2f, 0.08f);

    public static MutationProfile FromScenario(SimulationScenario scenario)
    {
        return new MutationProfile(
            scenario.MutationStrength,
            scenario.TraitMutationRate,
            scenario.BrainMutationRate).Validated();
    }

    public static MutationProfile FromLegacyGenome(CreatureGenome genome)
    {
        return new MutationProfile(
            genome.MutationStrength,
            genome.TraitMutationRate,
            genome.BrainMutationRate).Validated();
    }

    public MutationProfile Validated()
    {
        EnsureNonNegative(MutationStrength, nameof(MutationStrength));
        EnsureProbability(TraitMutationRate, nameof(TraitMutationRate));
        EnsureProbability(BrainMutationRate, nameof(BrainMutationRate));
        return this;
    }

    private static void EnsureNonNegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f)
        {
            throw new InvalidOperationException($"{name} must be finite and non-negative.");
        }
    }

    private static void EnsureProbability(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f || value > 1f)
        {
            throw new InvalidOperationException($"{name} must be finite and between 0 and 1.");
        }
    }
}

/// <summary>
/// Resolves the mutation profile imposed by the world for a reproducing parent.
/// </summary>
public sealed class WorldMutationPolicy
{
    private readonly MutationProfile _baseProfile;

    public WorldMutationPolicy(MutationProfile baseProfile)
    {
        _baseProfile = baseProfile.Validated();
    }

    public static WorldMutationPolicy Uniform(
        float mutationStrength,
        float traitMutationRate,
        float brainMutationRate)
    {
        return new WorldMutationPolicy(new MutationProfile(
            mutationStrength,
            traitMutationRate,
            brainMutationRate));
    }

    public static WorldMutationPolicy FromScenario(SimulationScenario scenario)
    {
        return new WorldMutationPolicy(MutationProfile.FromScenario(scenario));
    }

    public MutationProfile GetEffectiveProfile(WorldState state, CreatureState parent)
    {
        _ = state;
        _ = parent;
        return _baseProfile;
    }
}
