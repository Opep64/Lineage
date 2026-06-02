namespace Lineage.Core;

/// <summary>
/// Architecture-neutral holder for evolvable brain payloads.
/// </summary>
public sealed record BrainGenome
{
    public BrainArchitectureKind ArchitectureKind { get; init; } = BrainArchitectureKind.HybridNeural;

    public NeuralBrainGenome? Neural { get; init; }

    public RtNeatBrainGenome? RtNeat { get; init; }

    public int HiddenNodeCount => ArchitectureKind == BrainArchitectureKind.RtNeatGraph
        ? RtNeatOrThrow.HiddenNodeCount
        : NeuralOrThrow.HiddenNodeCount;

    public int WeightCount => ArchitectureKind == BrainArchitectureKind.RtNeatGraph
        ? RtNeatOrThrow.WeightCount
        : NeuralOrThrow.Weights.Length;

    public float[] Weights => ArchitectureKind == BrainArchitectureKind.RtNeatGraph
        ? RtNeatOrThrow.FlattenWeights()
        : NeuralOrThrow.Weights;

    public int HiddenInputWeightCount => ArchitectureKind == BrainArchitectureKind.RtNeatGraph
        ? CountRtNeatConnectionsToHidden()
        : NeuralOrThrow.HiddenInputWeightCount;

    public int HiddenOutputWeightCount => ArchitectureKind == BrainArchitectureKind.RtNeatGraph
        ? CountRtNeatConnectionsFromHidden()
        : NeuralOrThrow.HiddenOutputWeightCount;

    private NeuralBrainGenome NeuralOrThrow => Neural
        ?? throw new InvalidOperationException("Brain payload does not contain a dense neural genome.");

    private RtNeatBrainGenome RtNeatOrThrow => RtNeat
        ?? throw new InvalidOperationException("Brain payload does not contain an rtNEAT graph genome.");

    public static BrainGenome FromNeural(BrainArchitectureKind architectureKind, NeuralBrainGenome neural)
    {
        ArgumentNullException.ThrowIfNull(neural);
        if (architectureKind is not (
            BrainArchitectureKind.HybridNeural
            or BrainArchitectureKind.HiddenLayerNeural
            or BrainArchitectureKind.HybridDeep8x8Neural))
        {
            throw new ArgumentOutOfRangeException(nameof(architectureKind), architectureKind, "Architecture requires a graph brain payload.");
        }

        if (architectureKind == BrainArchitectureKind.HybridDeep8x8Neural && !neural.HasSecondHiddenLayer)
        {
            throw new ArgumentException("Hybrid deep 8x8 neural architecture requires a two-layer dense brain payload.", nameof(neural));
        }

        if (architectureKind != BrainArchitectureKind.HybridDeep8x8Neural && neural.HasSecondHiddenLayer)
        {
            throw new ArgumentException("Two-layer dense brain payloads require the hybrid deep 8x8 neural architecture.", nameof(neural));
        }

        _ = BrainFactory.Describe(architectureKind);
        return new BrainGenome
        {
            ArchitectureKind = architectureKind,
            Neural = neural,
            RtNeat = null
        };
    }

    public static BrainGenome FromRtNeat(RtNeatBrainGenome rtNeat)
    {
        ArgumentNullException.ThrowIfNull(rtNeat);
        _ = BrainFactory.Describe(BrainArchitectureKind.RtNeatGraph);
        return new BrainGenome
        {
            ArchitectureKind = BrainArchitectureKind.RtNeatGraph,
            Neural = null,
            RtNeat = rtNeat.Validated()
        };
    }

    public BrainGenome Validated()
    {
        _ = BrainFactory.Describe(ArchitectureKind);
        return ArchitectureKind == BrainArchitectureKind.RtNeatGraph
            ? FromRtNeat(RtNeatOrThrow)
            : FromNeural(ArchitectureKind, NeuralOrThrow);
    }

    public BrainEvaluationResult Evaluate(
        in BrainInputFrame inputFrame,
        in LegacyNeuralMemoryInputFrame legacyMemoryInputs,
        Span<float> denseInputs,
        Span<float> denseOutputs)
    {
        if (ArchitectureKind == BrainArchitectureKind.RtNeatGraph)
        {
            return new BrainEvaluationResult(
                RtNeatOrThrow.Evaluate(inputFrame, legacyMemoryInputs),
                default);
        }

        LegacyNeuralBrainAdapter.FillInputs(
            inputFrame,
            legacyMemoryInputs,
            denseInputs);
        denseOutputs.Clear();
        NeuralOrThrow.Evaluate(denseInputs, denseOutputs);
        return new BrainEvaluationResult(
            LegacyNeuralBrainAdapter.ReadStandardOutputs(denseOutputs),
            LegacyNeuralBrainAdapter.ReadMemoryOutputs(denseOutputs));
    }

    public void Evaluate(Span<float> inputs, Span<float> outputs)
    {
        NeuralOrThrow.Evaluate(inputs, outputs);
    }

    public BrainEvaluationResult EvaluateWithDenseInputs(
        in BrainInputFrame inputFrame,
        in LegacyNeuralMemoryInputFrame legacyMemoryInputs,
        ReadOnlySpan<float> baselineDenseInputs,
        ReadOnlySpan<float> modifiedDenseInputs)
    {
        if (ArchitectureKind == BrainArchitectureKind.RtNeatGraph)
        {
            return new BrainEvaluationResult(
                RtNeatOrThrow.EvaluateWithDenseInputs(
                    inputFrame,
                    legacyMemoryInputs,
                    baselineDenseInputs,
                    modifiedDenseInputs),
                default);
        }

        var denseOutputs = new float[NeuralBrainSchema.OutputCount];
        NeuralOrThrow.Evaluate(modifiedDenseInputs, denseOutputs);
        return new BrainEvaluationResult(
            LegacyNeuralBrainAdapter.ReadStandardOutputs(denseOutputs),
            LegacyNeuralBrainAdapter.ReadMemoryOutputs(denseOutputs));
    }

    public float GetWeight(int index)
    {
        var weights = Weights;
        if ((uint)index >= (uint)weights.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return weights[index];
    }

    public float GetWeight(int outputIndex, int inputIndex)
    {
        return ArchitectureKind == BrainArchitectureKind.RtNeatGraph
            ? 0f
            : NeuralOrThrow.GetWeight(outputIndex, inputIndex);
    }

    public float GetHiddenInputWeight(int hiddenIndex, int inputIndex)
    {
        return ArchitectureKind == BrainArchitectureKind.RtNeatGraph
            ? 0f
            : NeuralOrThrow.GetHiddenInputWeight(hiddenIndex, inputIndex);
    }

    public float SumAbsoluteHiddenInputWeights()
    {
        if (ArchitectureKind != BrainArchitectureKind.RtNeatGraph)
        {
            return NeuralOrThrow.SumAbsoluteHiddenInputWeights();
        }

        var hiddenIds = GetRtNeatHiddenIds();
        return RtNeatOrThrow.Connections
            .Where(connection => hiddenIds.Contains(connection.TargetNodeId))
            .Sum(connection => Math.Abs(connection.Weight));
    }

    public float SumAbsoluteHiddenOutputWeights()
    {
        if (ArchitectureKind != BrainArchitectureKind.RtNeatGraph)
        {
            return NeuralOrThrow.SumAbsoluteHiddenOutputWeights();
        }

        var hiddenIds = GetRtNeatHiddenIds();
        return RtNeatOrThrow.Connections
            .Where(connection => hiddenIds.Contains(connection.SourceNodeId))
            .Sum(connection => Math.Abs(connection.Weight));
    }

    public int CountActiveHiddenOutputWeights(float threshold)
    {
        if (ArchitectureKind != BrainArchitectureKind.RtNeatGraph)
        {
            return NeuralOrThrow.CountActiveHiddenOutputWeights(threshold);
        }

        var hiddenIds = GetRtNeatHiddenIds();
        var outputIds = GetRtNeatOutputIds();
        return RtNeatOrThrow.Connections.Count(connection =>
            connection.Enabled
            && hiddenIds.Contains(connection.SourceNodeId)
            && outputIds.Contains(connection.TargetNodeId)
            && Math.Abs(connection.Weight) >= threshold);
    }

    public static implicit operator NeuralBrainGenome(BrainGenome brain)
    {
        ArgumentNullException.ThrowIfNull(brain);
        return brain.NeuralOrThrow;
    }

    private HashSet<int> GetRtNeatHiddenIds()
    {
        return RtNeatOrThrow.Nodes
            .Where(node => node.Kind == RtNeatNodeKind.Hidden)
            .Select(node => node.Id)
            .ToHashSet();
    }

    private HashSet<int> GetRtNeatOutputIds()
    {
        return RtNeatOrThrow.Nodes
            .Where(node => node.Kind == RtNeatNodeKind.Output)
            .Select(node => node.Id)
            .ToHashSet();
    }

    private int CountRtNeatConnectionsToHidden()
    {
        var hiddenIds = GetRtNeatHiddenIds();
        return RtNeatOrThrow.Connections.Count(connection => hiddenIds.Contains(connection.TargetNodeId));
    }

    private int CountRtNeatConnectionsFromHidden()
    {
        var hiddenIds = GetRtNeatHiddenIds();
        return RtNeatOrThrow.Connections.Count(connection => hiddenIds.Contains(connection.SourceNodeId));
    }
}

public readonly record struct BrainEvaluationResult(
    BrainOutputFrame Actions,
    LegacyNeuralMemoryOutputFrame Memory);

public sealed record BrainSnapshot
{
    public BrainArchitectureKind ArchitectureKind { get; init; } = BrainArchitectureKind.HybridNeural;

    public float[] Weights { get; init; } = [];

    public RtNeatBrainGenome? RtNeat { get; init; }

    public static BrainSnapshot Capture(BrainGenome brain)
    {
        ArgumentNullException.ThrowIfNull(brain);
        return brain.ArchitectureKind == BrainArchitectureKind.RtNeatGraph
            ? new BrainSnapshot
            {
                ArchitectureKind = BrainArchitectureKind.RtNeatGraph,
                RtNeat = brain.RtNeat,
                Weights = brain.Weights
            }
            : new BrainSnapshot
            {
                ArchitectureKind = brain.ArchitectureKind,
                Weights = brain.Weights.ToArray()
            };
    }

    public BrainGenome CreateBrain()
    {
        _ = BrainFactory.Describe(ArchitectureKind);
        return ArchitectureKind == BrainArchitectureKind.RtNeatGraph
            ? BrainGenome.FromRtNeat(RtNeat ?? throw new InvalidOperationException("rtNEAT brain snapshot is missing graph payload."))
            : BrainGenome.FromNeural(ArchitectureKind, new NeuralBrainGenome(Weights));
    }
}
