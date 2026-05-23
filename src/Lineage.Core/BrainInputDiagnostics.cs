namespace Lineage.Core;

/// <summary>
/// Summarizes whether living neural brains are wiring recently added sensory inputs.
/// </summary>
///
/// <remarks>
/// Behavior assays show what a brain does in standardized situations. These diagnostics
/// complement that by showing whether mutation is even attaching meaningful weights to
/// freshness and rot-scent inputs before those weights become behaviorally obvious.
/// </remarks>
public static class BrainInputDiagnostics
{
    private static readonly int[] RotScentInputs =
    [
        NeuralBrainSchema.RottenMeatScentDensityInput,
        NeuralBrainSchema.RottenMeatScentForwardInput,
        NeuralBrainSchema.RottenMeatScentRightInput
    ];

    public static BrainInputDiagnosticSummary Analyze(WorldState state)
    {
        return Analyze(state, state.Creatures);
    }

    public static BrainInputDiagnosticSummary Analyze(WorldState state, IEnumerable<CreatureState> creatures)
    {
        var accumulator = new BrainInputAccumulator();
        foreach (var creature in creatures)
        {
            if (creature.BrainId < 0)
            {
                continue;
            }

            accumulator.Add(state.GetBrain(creature.BrainId));
        }

        return accumulator.ToSummary();
    }

    public static IReadOnlyList<LineageBrainInputDiagnosticSummary> AnalyzeTopFounderLineages(
        WorldState state,
        int maxLineages = 10)
    {
        if (maxLineages <= 0 || state.LineageRecords.Count == 0 || state.Creatures.Count == 0)
        {
            return Array.Empty<LineageBrainInputDiagnosticSummary>();
        }

        var recordsById = state.LineageRecords.ToDictionary(record => record.Id);
        var groups = new Dictionary<EntityId, LineageBrainInputAccumulator>();
        var totalEvaluated = 0;

        foreach (var creature in state.Creatures)
        {
            if (creature.BrainId < 0)
            {
                continue;
            }

            var founderId = FindFounderId(creature, recordsById);
            if (!groups.TryGetValue(founderId, out var group))
            {
                group = new LineageBrainInputAccumulator { FounderId = founderId };
            }

            group.Diagnostics.Add(state.GetBrain(creature.BrainId));
            groups[founderId] = group;
            totalEvaluated++;
        }

        if (totalEvaluated == 0)
        {
            return Array.Empty<LineageBrainInputDiagnosticSummary>();
        }

        return groups.Values
            .OrderByDescending(group => group.Diagnostics.Count)
            .ThenBy(group => group.FounderId.Value)
            .Take(maxLineages)
            .Select(group => new LineageBrainInputDiagnosticSummary(
                group.FounderId,
                group.Diagnostics.Count,
                group.Diagnostics.Count / (float)totalEvaluated,
                group.Diagnostics.ToSummary()))
            .ToArray();
    }

    private static EntityId FindFounderId(
        CreatureState creature,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> recordsById)
    {
        return recordsById.TryGetValue(creature.Id, out var record)
            ? FindFounderId(record, recordsById)
            : creature.ParentId == default
                ? creature.Id
                : creature.ParentId;
    }

    private static EntityId FindFounderId(
        CreatureLineageRecord record,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> recordsById)
    {
        var current = record;
        while (true)
        {
            if (current.IsFounder || !recordsById.TryGetValue(current.ParentId, out var parent))
            {
                return current.Id;
            }

            current = parent;
        }
    }

    private static float MeanAbsoluteDirectInputWeights(NeuralBrainGenome brain, int input)
    {
        var sum = 0f;
        for (var output = 0; output < NeuralBrainSchema.OutputCount; output++)
        {
            sum += Math.Abs(brain.GetWeight(output, input));
        }

        return sum / NeuralBrainSchema.OutputCount;
    }

    private static float MeanAbsoluteDirectInputWeights(NeuralBrainGenome brain, IReadOnlyList<int> inputs)
    {
        var sum = 0f;
        for (var output = 0; output < NeuralBrainSchema.OutputCount; output++)
        {
            for (var i = 0; i < inputs.Count; i++)
            {
                sum += Math.Abs(brain.GetWeight(output, inputs[i]));
            }
        }

        return sum / (NeuralBrainSchema.OutputCount * inputs.Count);
    }

    private static float MeanAbsoluteHiddenInputWeights(NeuralBrainGenome brain, int input)
    {
        if (brain.HiddenNodeCount == 0)
        {
            return 0f;
        }

        var sum = 0f;
        for (var hidden = 0; hidden < brain.HiddenNodeCount; hidden++)
        {
            sum += Math.Abs(brain.GetHiddenInputWeight(hidden, input));
        }

        return sum / brain.HiddenNodeCount;
    }

    private static float MeanAbsoluteHiddenInputWeights(NeuralBrainGenome brain, IReadOnlyList<int> inputs)
    {
        if (brain.HiddenNodeCount == 0)
        {
            return 0f;
        }

        var sum = 0f;
        for (var hidden = 0; hidden < brain.HiddenNodeCount; hidden++)
        {
            for (var i = 0; i < inputs.Count; i++)
            {
                sum += Math.Abs(brain.GetHiddenInputWeight(hidden, inputs[i]));
            }
        }

        return sum / (brain.HiddenNodeCount * inputs.Count);
    }

    private struct LineageBrainInputAccumulator
    {
        public EntityId FounderId;
        public BrainInputAccumulator Diagnostics;
    }

    private struct BrainInputAccumulator
    {
        public int Count;
        private float _directFreshnessMagnitude;
        private float _directRotScentMagnitude;
        private float _hiddenFreshnessMagnitude;
        private float _hiddenRotScentMagnitude;
        private float _moveFreshnessWeight;
        private float _eatFreshnessWeight;
        private float _moveRotScentDensityWeight;
        private float _turnRotScentDensityWeight;
        private float _moveRotScentForwardWeight;
        private float _turnRotScentRightWeight;

        public void Add(NeuralBrainGenome brain)
        {
            Count++;
            _directFreshnessMagnitude += MeanAbsoluteDirectInputWeights(brain, NeuralBrainSchema.VisibleMeatFreshnessInput);
            _directRotScentMagnitude += MeanAbsoluteDirectInputWeights(brain, RotScentInputs);
            _hiddenFreshnessMagnitude += MeanAbsoluteHiddenInputWeights(brain, NeuralBrainSchema.VisibleMeatFreshnessInput);
            _hiddenRotScentMagnitude += MeanAbsoluteHiddenInputWeights(brain, RotScentInputs);
            _moveFreshnessWeight += brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.VisibleMeatFreshnessInput);
            _eatFreshnessWeight += brain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.VisibleMeatFreshnessInput);
            _moveRotScentDensityWeight += brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RottenMeatScentDensityInput);
            _turnRotScentDensityWeight += brain.GetWeight(NeuralBrainSchema.TurnOutput, NeuralBrainSchema.RottenMeatScentDensityInput);
            _moveRotScentForwardWeight += brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RottenMeatScentForwardInput);
            _turnRotScentRightWeight += brain.GetWeight(NeuralBrainSchema.TurnOutput, NeuralBrainSchema.RottenMeatScentRightInput);
        }

        public BrainInputDiagnosticSummary ToSummary()
        {
            if (Count == 0)
            {
                return default;
            }

            return new BrainInputDiagnosticSummary(
                Count,
                _directFreshnessMagnitude / Count,
                _directRotScentMagnitude / Count,
                _hiddenFreshnessMagnitude / Count,
                _hiddenRotScentMagnitude / Count,
                _moveFreshnessWeight / Count,
                _eatFreshnessWeight / Count,
                _moveRotScentDensityWeight / Count,
                _turnRotScentDensityWeight / Count,
                _moveRotScentForwardWeight / Count,
                _turnRotScentRightWeight / Count);
        }
    }
}

public readonly record struct BrainInputDiagnosticSummary(
    int EvaluatedCreatureCount,
    float DirectFreshnessWeightMagnitude,
    float DirectRotScentWeightMagnitude,
    float HiddenFreshnessWeightMagnitude,
    float HiddenRotScentWeightMagnitude,
    float MoveFreshnessWeight,
    float EatFreshnessWeight,
    float MoveRotScentDensityWeight,
    float TurnRotScentDensityWeight,
    float MoveRotScentForwardWeight,
    float TurnRotScentRightWeight);

public readonly record struct LineageBrainInputDiagnosticSummary(
    EntityId FounderId,
    int LivingCreatures,
    float LivingShare,
    BrainInputDiagnosticSummary Diagnostics);
