namespace Lineage.Core;

/// <summary>
/// Injects exported species representatives into an existing world as new founders.
/// </summary>
public static class SpeciesProfileInjector
{
    public static SpeciesInjectionResult Inject(
        WorldState state,
        SpeciesProfile profile,
        SpeciesInjectionOptions options)
    {
        profile = profile.Validated();
        options = options.Validated(profile);

        var genomeId = state.AddGenome(profile.Genome);
        var sharedBrainId = CreateSharedBrainId(state, profile, options);
        var randomBrain = options.BrainOverrideKind == InitialBrainKind.RandomPerFounder
            ? state.Random.Fork()
            : null;
        var creatureIds = new EntityId[options.Count];
        var reportedBrainId = sharedBrainId;

        for (var i = 0; i < options.Count; i++)
        {
            var brainId = options.BrainOverrideKind == InitialBrainKind.RandomPerFounder
                ? state.AddBrain(
                    BrainFactory.CreateRandom(
                        options.BrainArchitectureKind,
                        randomBrain ?? throw new InvalidOperationException("Random brain source was not created."),
                        hiddenNodeCount: options.BrainHiddenNodeCount),
                    options.BrainArchitectureKind)
                : sharedBrainId;
            if (reportedBrainId < 0)
            {
                reportedBrainId = brainId;
            }

            creatureIds[i] = state.SpawnCreature(
                genomeId,
                RandomCreaturePosition(state, options.SpawnRegion, profile.Genome.BodyRadius),
                options.Energy,
                health: 1f,
                generation: 0,
                parentId: default,
                brainId: brainId);
        }

        return new SpeciesInjectionResult(profile.Name, genomeId, reportedBrainId, creatureIds);
    }

    private static int CreateSharedBrainId(
        WorldState state,
        SpeciesProfile profile,
        SpeciesInjectionOptions options)
    {
        if (options.BrainOverrideKind is null)
        {
            return options.BrainOverrideProfile is null
                ? state.AddBrain(profile.CreateBrain(), profile.BrainArchitectureKind)
                : state.AddBrain(
                    options.BrainOverrideProfile.CreateBrain(),
                    options.BrainOverrideProfile.BrainArchitectureKind);
        }

        if (options.BrainOverrideKind == InitialBrainKind.RandomPerFounder)
        {
            return -1;
        }

        return state.AddBrain(
            BrainFactory.CreateStarter(
                options.BrainArchitectureKind,
                options.BrainOverrideKind.Value,
                options.BrainHiddenNodeCount),
            options.BrainArchitectureKind);
    }

    private static SimVector2 RandomCreaturePosition(
        WorldState state,
        InitialCreatureSpawnRegion spawnRegion,
        float bodyRadius)
    {
        var bounds = ResolveCreatureSpawnBounds(state, spawnRegion);
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var candidate = new SimVector2(
                RandomRange(state, bounds.Left, bounds.Right),
                RandomRange(state, bounds.Top, bounds.Bottom));

            if (!state.Obstacles.IsBlockedForCircle(candidate, bodyRadius))
            {
                return candidate;
            }
        }

        return new SimVector2(
            RandomRange(state, bounds.Left, bounds.Right),
            RandomRange(state, bounds.Top, bounds.Bottom));
    }

    private static CreatureSpawnBounds ResolveCreatureSpawnBounds(WorldState state, InitialCreatureSpawnRegion spawnRegion)
    {
        var left = 0f;
        var top = 0f;
        var right = state.Bounds.Width;
        var bottom = state.Bounds.Height;
        var thirdWidth = state.Bounds.Width / 3f;
        var thirdHeight = state.Bounds.Height / 3f;

        switch (spawnRegion)
        {
            case InitialCreatureSpawnRegion.LeftThird:
                right = thirdWidth;
                break;
            case InitialCreatureSpawnRegion.MiddleThird:
                left = thirdWidth;
                right = thirdWidth * 2f;
                break;
            case InitialCreatureSpawnRegion.RightThird:
                left = thirdWidth * 2f;
                break;
            case InitialCreatureSpawnRegion.TopThird:
                bottom = thirdHeight;
                break;
            case InitialCreatureSpawnRegion.BottomThird:
                top = thirdHeight * 2f;
                break;
        }

        if (spawnRegion != InitialCreatureSpawnRegion.Uniform)
        {
            var padding = MathF.Min(
                state.Biomes.ResourceVoidBorderWidth,
                MathF.Min(state.Bounds.Width, state.Bounds.Height) * 0.45f);
            left = MathF.Max(left, padding);
            top = MathF.Max(top, padding);
            right = MathF.Min(right, state.Bounds.Width - padding);
            bottom = MathF.Min(bottom, state.Bounds.Height - padding);
        }

        if (right <= left)
        {
            left = 0f;
            right = state.Bounds.Width;
        }

        if (bottom <= top)
        {
            top = 0f;
            bottom = state.Bounds.Height;
        }

        return new CreatureSpawnBounds(left, top, right, bottom);
    }

    private static float RandomRange(WorldState state, float inclusiveMin, float exclusiveMax)
    {
        return Math.Abs(exclusiveMax - inclusiveMin) <= float.Epsilon
            ? inclusiveMin
            : state.Random.NextSingle(inclusiveMin, exclusiveMax);
    }

    private readonly record struct CreatureSpawnBounds(float Left, float Top, float Right, float Bottom);
}

public readonly record struct SpeciesInjectionOptions(
    int Count,
    InitialCreatureSpawnRegion SpawnRegion = InitialCreatureSpawnRegion.Uniform,
    float? EnergyOverride = null,
    InitialBrainKind? BrainOverrideKind = null,
    BrainProfile? BrainOverrideProfile = null,
    BrainArchitectureKind BrainArchitectureKind = BrainArchitectureKind.HybridNeural,
    int BrainHiddenNodeCount = 0)
{
    public float Energy { get; private init; }

    public SpeciesInjectionOptions Validated(SpeciesProfile profile)
    {
        if (Count <= 0)
        {
            throw new InvalidOperationException("Species injection count must be positive.");
        }

        if (!Enum.IsDefined(SpawnRegion))
        {
            throw new InvalidOperationException("Species injection spawn region must be defined.");
        }

        if (BrainOverrideKind is not null && !Enum.IsDefined(BrainOverrideKind.Value))
        {
            throw new InvalidOperationException("Species injection brain override kind must be defined.");
        }

        if (BrainOverrideKind is not null && BrainOverrideProfile is not null)
        {
            throw new InvalidOperationException("Species injection can choose either a starter brain override or a brain profile, not both.");
        }

        var brainOverrideProfile = BrainOverrideProfile?.Validated();
        _ = BrainFactory.Describe(BrainArchitectureKind);
        var resolvedHiddenNodeCount = BrainFactory.ResolveHiddenNodeCount(
            BrainArchitectureKind,
            BrainHiddenNodeCount);

        var defaultEnergy = MathF.Max(
            profile.Genome.OffspringEnergyInvestment,
            profile.Genome.ReproductionEnergyThreshold * 0.75f);
        var energy = EnergyOverride ?? defaultEnergy;
        if (!float.IsFinite(energy) || energy <= 0f)
        {
            throw new InvalidOperationException("Species injection energy must be finite and positive.");
        }

        return this with
        {
            Energy = energy,
            BrainOverrideProfile = brainOverrideProfile,
            BrainHiddenNodeCount = resolvedHiddenNodeCount
        };
    }
}

public readonly record struct SpeciesInjectionResult(
    string SpeciesName,
    int GenomeId,
    int BrainId,
    IReadOnlyList<EntityId> CreatureIds);
