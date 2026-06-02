namespace Lineage.Core;

/// <summary>
/// Central construction point for brain architectures.
/// </summary>
///
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
        1,
        NeuralBrainSchema.MaxHiddenNodeCount,
        SupportsHiddenNodes: true,
        SupportsDirectInputOutputWeights: false);

    private static readonly BrainArchitectureDescriptor HybridDeep8x8NeuralDescriptor = new(
        BrainArchitectureKind.HybridDeep8x8Neural,
        "Hybrid deep 8x8 neural",
        "Direct input/output neural weights plus two hidden layers of eight nodes each.",
        NeuralBrainSchema.InputCount,
        NeuralBrainSchema.OutputCount,
        NeuralBrainSchema.HybridDeep8x8HiddenNodeCount,
        NeuralBrainSchema.HybridDeep8x8HiddenNodeCount,
        NeuralBrainSchema.HybridDeep8x8HiddenNodeCount,
        SupportsHiddenNodes: true,
        SupportsDirectInputOutputWeights: true);

    private static readonly BrainArchitectureDescriptor RtNeatGraphDescriptor = new(
        BrainArchitectureKind.RtNeatGraph,
        "rtNEAT graph",
        "Sparse topology-evolving graph controller with semantic input and action nodes.",
        RtNeatBrainIoRegistry.Inputs.Count,
        RtNeatBrainIoRegistry.Outputs.Count,
        0,
        0,
        0,
        SupportsHiddenNodes: true,
        SupportsDirectInputOutputWeights: false);

    public static BrainArchitectureDescriptor Describe(BrainArchitectureKind kind)
    {
        return kind switch
        {
            BrainArchitectureKind.HybridNeural => HybridNeuralDescriptor,
            BrainArchitectureKind.HiddenLayerNeural => HiddenLayerNeuralDescriptor,
            BrainArchitectureKind.RtNeatGraph => RtNeatGraphDescriptor,
            BrainArchitectureKind.HybridDeep8x8Neural => HybridDeep8x8NeuralDescriptor,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported brain architecture kind.")
        };
    }

    public static BrainGenome CreateZero(BrainArchitectureKind kind, int hiddenNodeCount = 0)
    {
        var resolvedHiddenNodeCount = ResolveHiddenNodeCount(kind, hiddenNodeCount);
        return kind switch
        {
            BrainArchitectureKind.HybridNeural => BrainGenome.FromNeural(
                kind,
                NeuralBrainGenome.CreateZero(resolvedHiddenNodeCount)),
            BrainArchitectureKind.HiddenLayerNeural => BrainGenome.FromNeural(
                kind,
                NeuralBrainGenome.CreateZero(resolvedHiddenNodeCount)),
            BrainArchitectureKind.HybridDeep8x8Neural => BrainGenome.FromNeural(
                kind,
                NeuralBrainGenome.CreateHybridDeep8x8Zero()),
            BrainArchitectureKind.RtNeatGraph => BrainGenome.FromRtNeat(RtNeatBrainGenome.CreateZero()),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported brain architecture kind.")
        };
    }

    public static BrainGenome CreateRandom(
        BrainArchitectureKind kind,
        DeterministicRandom random,
        float scale = 1f,
        int hiddenNodeCount = 0)
    {
        ArgumentNullException.ThrowIfNull(random);
        var resolvedHiddenNodeCount = ResolveHiddenNodeCount(kind, hiddenNodeCount);
        return kind switch
        {
            BrainArchitectureKind.HybridNeural => BrainGenome.FromNeural(
                kind,
                NeuralBrainGenome.CreateRandom(random, scale, resolvedHiddenNodeCount)),
            BrainArchitectureKind.HiddenLayerNeural => BrainGenome.FromNeural(
                kind,
                NeuralBrainGenome.CreateHiddenLayerRandom(
                    random,
                    scale,
                    resolvedHiddenNodeCount)),
            BrainArchitectureKind.HybridDeep8x8Neural => BrainGenome.FromNeural(
                kind,
                NeuralBrainGenome.CreateHybridDeep8x8Random(random, scale)),
            BrainArchitectureKind.RtNeatGraph => BrainGenome.FromRtNeat(RtNeatBrainGenome.CreateRandom(random, scale)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported brain architecture kind.")
        };
    }

    public static BrainGenome CreateStarter(
        BrainArchitectureKind kind,
        InitialBrainKind initialBrainKind,
        int hiddenNodeCount = 0)
    {
        if (kind == BrainArchitectureKind.RtNeatGraph)
        {
            if (initialBrainKind == InitialBrainKind.RandomPerFounder)
            {
                throw new ArgumentException(
                    "Random-per-founder brains are created individually.",
                    nameof(initialBrainKind));
            }

            return BrainGenome.FromRtNeat(initialBrainKind switch
            {
                InitialBrainKind.SparseGraphForager => RtNeatBrainGenome.CreateStarterForager(),
                InitialBrainKind.SparseGraphScavenger => RtNeatBrainGenome.CreateStarterScavenger(),
                InitialBrainKind.SparseGraphPredator => RtNeatBrainGenome.CreateStarterPredator(),
                InitialBrainKind.SeedForager
                    or InitialBrainKind.ExplorerForager
                    or InitialBrainKind.SectorForager
                    or InitialBrainKind.OpportunisticForager => RtNeatBrainGenome.CreateStarterForager(),
                InitialBrainKind.ScavengerForager
                    or InitialBrainKind.FreshnessAwareScavenger => RtNeatBrainGenome.CreateStarterScavenger(),
                InitialBrainKind.ForagerPredator => RtNeatBrainGenome.CreateStarterPredator(),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(initialBrainKind),
                    initialBrainKind,
                    "Unsupported initial brain kind.")
            });
        }

        var resolvedHiddenNodeCount = ResolveHiddenNodeCount(kind, hiddenNodeCount);
        var starterHiddenNodeCount = kind switch
        {
            BrainArchitectureKind.HybridNeural => resolvedHiddenNodeCount,
            BrainArchitectureKind.HybridDeep8x8Neural => NeuralBrainSchema.HybridDeep8x8FirstLayerNodeCount,
            _ => 0
        };
        var starter = initialBrainKind switch
        {
            InitialBrainKind.SeedForager => NeuralBrainGenome.CreateSeedForager(starterHiddenNodeCount),
            InitialBrainKind.ExplorerForager => NeuralBrainGenome.CreateExplorerForager(starterHiddenNodeCount),
            InitialBrainKind.SectorForager => NeuralBrainGenome.CreateSectorForager(starterHiddenNodeCount),
            InitialBrainKind.OpportunisticForager => NeuralBrainGenome.CreateOpportunisticForager(starterHiddenNodeCount),
            InitialBrainKind.ScavengerForager => NeuralBrainGenome.CreateScavengerForager(starterHiddenNodeCount),
            InitialBrainKind.FreshnessAwareScavenger => NeuralBrainGenome.CreateFreshnessAwareScavenger(starterHiddenNodeCount),
            InitialBrainKind.ForagerPredator => NeuralBrainGenome.CreateForagerPredator(starterHiddenNodeCount),
            InitialBrainKind.SparseGraphForager => NeuralBrainGenome.CreateSectorForager(starterHiddenNodeCount),
            InitialBrainKind.SparseGraphScavenger => NeuralBrainGenome.CreateScavengerForager(starterHiddenNodeCount),
            InitialBrainKind.SparseGraphPredator => NeuralBrainGenome.CreateForagerPredator(starterHiddenNodeCount),
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
            BrainArchitectureKind.HybridNeural => BrainGenome.FromNeural(kind, starter),
            BrainArchitectureKind.HybridDeep8x8Neural => BrainGenome.FromNeural(
                kind,
                NeuralBrainGenome.CreateHybridDeep8x8FromHybrid(starter)),
            BrainArchitectureKind.HiddenLayerNeural => BrainGenome.FromNeural(
                kind,
                NeuralBrainGenome.CreateHiddenLayerFromDirect(
                    starter,
                    resolvedHiddenNodeCount)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported brain architecture kind.")
        };
    }

    public static BrainGenome Mutate(
        BrainArchitectureKind kind,
        BrainGenome brain,
        DeterministicRandom random,
        float mutationStrength,
        float mutationRate,
        RtNeatMutationPolicy? rtNeatMutationPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(brain);
        ArgumentNullException.ThrowIfNull(random);
        return kind switch
        {
            BrainArchitectureKind.HybridNeural => BrainGenome.FromNeural(
                kind,
                brain.Neural?.Mutated(random, mutationStrength, mutationRate)
                    ?? throw new InvalidOperationException("Hybrid neural mutation requires a dense brain payload.")),
            BrainArchitectureKind.HybridDeep8x8Neural => BrainGenome.FromNeural(
                kind,
                brain.Neural?.Mutated(random, mutationStrength, mutationRate)
                    ?? throw new InvalidOperationException("Hybrid deep 8x8 neural mutation requires a dense brain payload.")),
            BrainArchitectureKind.HiddenLayerNeural => BrainGenome.FromNeural(
                kind,
                brain.Neural?.MutatedHiddenLayer(
                    random,
                    mutationStrength,
                    mutationRate)
                    ?? throw new InvalidOperationException("Hidden-layer neural mutation requires a dense brain payload.")),
            BrainArchitectureKind.RtNeatGraph => BrainGenome.FromRtNeat(
                brain.RtNeat?.Mutated(random, mutationStrength, mutationRate, rtNeatMutationPolicy)
                    ?? throw new InvalidOperationException("rtNEAT mutation requires a graph brain payload.")),
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

        if (kind == BrainArchitectureKind.RtNeatGraph)
        {
            return 0;
        }

        if (requestedHiddenNodeCount > descriptor.MaxHiddenNodeCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedHiddenNodeCount),
                $"Hidden node count cannot exceed {descriptor.MaxHiddenNodeCount}.");
        }

        if (kind == BrainArchitectureKind.HiddenLayerNeural
            && requestedHiddenNodeCount == NeuralBrainSchema.DefaultHiddenNodeCount)
        {
            return descriptor.DefaultHiddenNodeCount;
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
