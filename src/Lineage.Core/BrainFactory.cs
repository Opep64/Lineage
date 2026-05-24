namespace Lineage.Core;

/// <summary>
/// Central construction point for brain architectures.
/// </summary>
///
/// <remarks>
/// This is a transitional seam: world state still stores the current neural genome
/// type, but callers no longer need to know how starter, random, or mutated brains
/// are produced for the active architecture.
/// </remarks>
public static class BrainFactory
{
    private static readonly BrainArchitectureDescriptor HybridNeuralDescriptor = new(
        BrainArchitectureKind.HybridNeural,
        "Hybrid neural",
        "Direct input/output neural weights with optional hidden concept nodes.",
        NeuralBrainSchema.InputCount,
        NeuralBrainSchema.OutputCount,
        NeuralBrainSchema.MaxHiddenNodeCount,
        SupportsHiddenNodes: true,
        SupportsDirectInputOutputWeights: true);

    public static BrainArchitectureDescriptor Describe(BrainArchitectureKind kind)
    {
        return kind switch
        {
            BrainArchitectureKind.HybridNeural => HybridNeuralDescriptor,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported brain architecture kind.")
        };
    }

    public static NeuralBrainGenome CreateZero(BrainArchitectureKind kind, int hiddenNodeCount = 0)
    {
        _ = Describe(kind);
        return NeuralBrainGenome.CreateZero(hiddenNodeCount);
    }

    public static NeuralBrainGenome CreateRandom(
        BrainArchitectureKind kind,
        DeterministicRandom random,
        float scale = 1f,
        int hiddenNodeCount = 0)
    {
        ArgumentNullException.ThrowIfNull(random);
        _ = Describe(kind);
        return NeuralBrainGenome.CreateRandom(random, scale, hiddenNodeCount);
    }

    public static NeuralBrainGenome CreateStarter(
        BrainArchitectureKind kind,
        InitialBrainKind initialBrainKind,
        int hiddenNodeCount = 0)
    {
        _ = Describe(kind);

        return initialBrainKind switch
        {
            InitialBrainKind.SeedForager => NeuralBrainGenome.CreateSeedForager(hiddenNodeCount),
            InitialBrainKind.ExplorerForager => NeuralBrainGenome.CreateExplorerForager(hiddenNodeCount),
            InitialBrainKind.SectorForager => NeuralBrainGenome.CreateSectorForager(hiddenNodeCount),
            InitialBrainKind.ScavengerForager => NeuralBrainGenome.CreateScavengerForager(hiddenNodeCount),
            InitialBrainKind.FreshnessAwareScavenger => NeuralBrainGenome.CreateFreshnessAwareScavenger(hiddenNodeCount),
            InitialBrainKind.ForagerPredator => NeuralBrainGenome.CreateForagerPredator(hiddenNodeCount),
            InitialBrainKind.RandomPerFounder => throw new ArgumentException(
                "Random-per-founder brains are created individually.",
                nameof(initialBrainKind)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(initialBrainKind),
                initialBrainKind,
                "Unsupported initial brain kind.")
        };
    }

    public static NeuralBrainGenome Mutate(
        BrainArchitectureKind kind,
        NeuralBrainGenome brain,
        DeterministicRandom random,
        float mutationStrength,
        float mutationRate)
    {
        ArgumentNullException.ThrowIfNull(brain);
        ArgumentNullException.ThrowIfNull(random);
        _ = Describe(kind);
        return brain.Mutated(random, mutationStrength, mutationRate);
    }
}

public readonly record struct BrainArchitectureDescriptor(
    BrainArchitectureKind Kind,
    string Name,
    string Description,
    int InputCount,
    int OutputCount,
    int MaxHiddenNodeCount,
    bool SupportsHiddenNodes,
    bool SupportsDirectInputOutputWeights);
