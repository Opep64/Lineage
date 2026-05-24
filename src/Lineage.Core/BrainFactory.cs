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
        NeuralBrainSchema.DefaultHiddenNodeCount,
        0,
        NeuralBrainSchema.MaxHiddenNodeCount,
        SupportsHiddenNodes: true,
        SupportsDirectInputOutputWeights: true);

    private static readonly BrainArchitectureDescriptor HiddenLayerNeuralDescriptor = new(
        BrainArchitectureKind.HiddenLayerNeural,
        "Hidden-layer neural",
        "Neural controller that routes all inputs through a hidden layer before outputs.",
        NeuralBrainSchema.InputCount,
        NeuralBrainSchema.OutputCount,
        NeuralBrainSchema.DefaultHiddenLayerNodeCount,
        NeuralBrainSchema.OutputCount,
        NeuralBrainSchema.MaxHiddenNodeCount,
        SupportsHiddenNodes: true,
        SupportsDirectInputOutputWeights: false);

    public static BrainArchitectureDescriptor Describe(BrainArchitectureKind kind)
    {
        return kind switch
        {
            BrainArchitectureKind.HybridNeural => HybridNeuralDescriptor,
            BrainArchitectureKind.HiddenLayerNeural => HiddenLayerNeuralDescriptor,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported brain architecture kind.")
        };
    }

    public static NeuralBrainGenome CreateZero(BrainArchitectureKind kind, int hiddenNodeCount = 0)
    {
        var resolvedHiddenNodeCount = ResolveHiddenNodeCount(kind, hiddenNodeCount);
        return kind switch
        {
            BrainArchitectureKind.HybridNeural => NeuralBrainGenome.CreateZero(resolvedHiddenNodeCount),
            BrainArchitectureKind.HiddenLayerNeural => NeuralBrainGenome.CreateZero(resolvedHiddenNodeCount),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported brain architecture kind.")
        };
    }

    public static NeuralBrainGenome CreateRandom(
        BrainArchitectureKind kind,
        DeterministicRandom random,
        float scale = 1f,
        int hiddenNodeCount = 0)
    {
        ArgumentNullException.ThrowIfNull(random);
        var resolvedHiddenNodeCount = ResolveHiddenNodeCount(kind, hiddenNodeCount);
        return kind switch
        {
            BrainArchitectureKind.HybridNeural => NeuralBrainGenome.CreateRandom(random, scale, resolvedHiddenNodeCount),
            BrainArchitectureKind.HiddenLayerNeural => NeuralBrainGenome.CreateHiddenLayerRandom(
                random,
                scale,
                resolvedHiddenNodeCount),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported brain architecture kind.")
        };
    }

    public static NeuralBrainGenome CreateStarter(
        BrainArchitectureKind kind,
        InitialBrainKind initialBrainKind,
        int hiddenNodeCount = 0)
    {
        var resolvedHiddenNodeCount = ResolveHiddenNodeCount(kind, hiddenNodeCount);
        var starter = initialBrainKind switch
        {
            InitialBrainKind.SeedForager => NeuralBrainGenome.CreateSeedForager(kind == BrainArchitectureKind.HybridNeural ? resolvedHiddenNodeCount : 0),
            InitialBrainKind.ExplorerForager => NeuralBrainGenome.CreateExplorerForager(kind == BrainArchitectureKind.HybridNeural ? resolvedHiddenNodeCount : 0),
            InitialBrainKind.SectorForager => NeuralBrainGenome.CreateSectorForager(kind == BrainArchitectureKind.HybridNeural ? resolvedHiddenNodeCount : 0),
            InitialBrainKind.ScavengerForager => NeuralBrainGenome.CreateScavengerForager(kind == BrainArchitectureKind.HybridNeural ? resolvedHiddenNodeCount : 0),
            InitialBrainKind.FreshnessAwareScavenger => NeuralBrainGenome.CreateFreshnessAwareScavenger(kind == BrainArchitectureKind.HybridNeural ? resolvedHiddenNodeCount : 0),
            InitialBrainKind.ForagerPredator => NeuralBrainGenome.CreateForagerPredator(kind == BrainArchitectureKind.HybridNeural ? resolvedHiddenNodeCount : 0),
            InitialBrainKind.RandomPerFounder => throw new ArgumentException(
                "Random-per-founder brains are created individually.",
                nameof(initialBrainKind)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(initialBrainKind),
                initialBrainKind,
                "Unsupported initial brain kind.")
        };

        return kind switch
        {
            BrainArchitectureKind.HybridNeural => starter,
            BrainArchitectureKind.HiddenLayerNeural => NeuralBrainGenome.CreateHiddenLayerFromDirect(
                starter,
                resolvedHiddenNodeCount),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported brain architecture kind.")
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
        return kind switch
        {
            BrainArchitectureKind.HybridNeural => brain.Mutated(random, mutationStrength, mutationRate),
            BrainArchitectureKind.HiddenLayerNeural => brain.MutatedHiddenLayer(
                random,
                mutationStrength,
                mutationRate),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported brain architecture kind.")
        };
    }

    public static int ResolveHiddenNodeCount(BrainArchitectureKind kind, int requestedHiddenNodeCount)
    {
        var descriptor = Describe(kind);
        if (requestedHiddenNodeCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedHiddenNodeCount), "Hidden node count cannot be negative.");
        }

        if (requestedHiddenNodeCount > descriptor.MaxHiddenNodeCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedHiddenNodeCount),
                $"Hidden node count cannot exceed {descriptor.MaxHiddenNodeCount}.");
        }

        return requestedHiddenNodeCount < descriptor.MinHiddenNodeCount
            ? descriptor.DefaultHiddenNodeCount
            : requestedHiddenNodeCount;
    }
}

public readonly record struct BrainArchitectureDescriptor(
    BrainArchitectureKind Kind,
    string Name,
    string Description,
    int InputCount,
    int OutputCount,
    int DefaultHiddenNodeCount,
    int MinHiddenNodeCount,
    int MaxHiddenNodeCount,
    bool SupportsHiddenNodes,
    bool SupportsDirectInputOutputWeights);
