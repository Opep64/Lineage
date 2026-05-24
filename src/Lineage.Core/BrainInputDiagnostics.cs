namespace Lineage.Core;

/// <summary>
/// Summarizes whether living neural brains are wiring recently added sensory inputs.
/// </summary>
///
/// <remarks>
/// Behavior assays show what a brain does in standardized situations. These diagnostics
/// complement that by showing whether mutation is even attaching meaningful weights to
/// selected food, scent, and creature-vision inputs before those weights become
/// behaviorally obvious.
/// </remarks>
public static class BrainInputDiagnostics
{
    private static readonly int[] RotScentInputs =
    [
        NeuralBrainSchema.RottenMeatScentDensityInput,
        NeuralBrainSchema.RottenMeatScentForwardInput,
        NeuralBrainSchema.RottenMeatScentRightInput
    ];

    private static readonly int[] SmallerCreatureSectorInputs = CreateVisionSectorInputs(
        NeuralBrainSchema.VisionSectorSmallerCreatureDensityOffset,
        NeuralBrainSchema.VisionSectorSmallerCreatureProximityOffset);

    private static readonly int[] SimilarCreatureSectorInputs = CreateVisionSectorInputs(
        NeuralBrainSchema.VisionSectorSimilarCreatureDensityOffset,
        NeuralBrainSchema.VisionSectorSimilarCreatureProximityOffset);

    private static readonly int[] LargerCreatureSectorInputs = CreateVisionSectorInputs(
        NeuralBrainSchema.VisionSectorLargerCreatureDensityOffset,
        NeuralBrainSchema.VisionSectorLargerCreatureProximityOffset);

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

    private static float MeanDirectOutputInputWeights(NeuralBrainGenome brain, int output, IReadOnlyList<int> inputs)
    {
        var sum = 0f;
        for (var i = 0; i < inputs.Count; i++)
        {
            sum += brain.GetWeight(output, inputs[i]);
        }

        return sum / inputs.Count;
    }

    private static int[] CreateVisionSectorInputs(params int[] channelOffsets)
    {
        var inputs = new int[VisionSectorSet.SectorCount * channelOffsets.Length];
        var writeIndex = 0;
        for (var sectorIndex = 0; sectorIndex < VisionSectorSet.SectorCount; sectorIndex++)
        {
            for (var offsetIndex = 0; offsetIndex < channelOffsets.Length; offsetIndex++)
            {
                inputs[writeIndex++] = NeuralBrainSchema.GetVisionSectorInput(sectorIndex, channelOffsets[offsetIndex]);
            }
        }

        return inputs;
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
        private float _directSmallerCreatureSectorMagnitude;
        private float _directSimilarCreatureSectorMagnitude;
        private float _directLargerCreatureSectorMagnitude;
        private float _hiddenFreshnessMagnitude;
        private float _hiddenRotScentMagnitude;
        private float _hiddenSmallerCreatureSectorMagnitude;
        private float _hiddenSimilarCreatureSectorMagnitude;
        private float _hiddenLargerCreatureSectorMagnitude;
        private float _moveFreshnessWeight;
        private float _eatFreshnessWeight;
        private float _moveRotScentDensityWeight;
        private float _turnRotScentDensityWeight;
        private float _moveRotScentForwardWeight;
        private float _turnRotScentRightWeight;
        private float _attackSmallerCreatureSectorWeight;
        private float _attackLargerCreatureSectorWeight;

        public void Add(NeuralBrainGenome brain)
        {
            Count++;
            _directFreshnessMagnitude += MeanAbsoluteDirectInputWeights(brain, NeuralBrainSchema.VisibleMeatFreshnessInput);
            _directRotScentMagnitude += MeanAbsoluteDirectInputWeights(brain, RotScentInputs);
            _directSmallerCreatureSectorMagnitude += MeanAbsoluteDirectInputWeights(brain, SmallerCreatureSectorInputs);
            _directSimilarCreatureSectorMagnitude += MeanAbsoluteDirectInputWeights(brain, SimilarCreatureSectorInputs);
            _directLargerCreatureSectorMagnitude += MeanAbsoluteDirectInputWeights(brain, LargerCreatureSectorInputs);
            _hiddenFreshnessMagnitude += MeanAbsoluteHiddenInputWeights(brain, NeuralBrainSchema.VisibleMeatFreshnessInput);
            _hiddenRotScentMagnitude += MeanAbsoluteHiddenInputWeights(brain, RotScentInputs);
            _hiddenSmallerCreatureSectorMagnitude += MeanAbsoluteHiddenInputWeights(brain, SmallerCreatureSectorInputs);
            _hiddenSimilarCreatureSectorMagnitude += MeanAbsoluteHiddenInputWeights(brain, SimilarCreatureSectorInputs);
            _hiddenLargerCreatureSectorMagnitude += MeanAbsoluteHiddenInputWeights(brain, LargerCreatureSectorInputs);
            _moveFreshnessWeight += brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.VisibleMeatFreshnessInput);
            _eatFreshnessWeight += brain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.VisibleMeatFreshnessInput);
            _moveRotScentDensityWeight += brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RottenMeatScentDensityInput);
            _turnRotScentDensityWeight += brain.GetWeight(NeuralBrainSchema.TurnOutput, NeuralBrainSchema.RottenMeatScentDensityInput);
            _moveRotScentForwardWeight += brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RottenMeatScentForwardInput);
            _turnRotScentRightWeight += brain.GetWeight(NeuralBrainSchema.TurnOutput, NeuralBrainSchema.RottenMeatScentRightInput);
            _attackSmallerCreatureSectorWeight += MeanDirectOutputInputWeights(
                brain,
                NeuralBrainSchema.AttackOutput,
                SmallerCreatureSectorInputs);
            _attackLargerCreatureSectorWeight += MeanDirectOutputInputWeights(
                brain,
                NeuralBrainSchema.AttackOutput,
                LargerCreatureSectorInputs);
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
                _directSmallerCreatureSectorMagnitude / Count,
                _directSimilarCreatureSectorMagnitude / Count,
                _directLargerCreatureSectorMagnitude / Count,
                _hiddenFreshnessMagnitude / Count,
                _hiddenRotScentMagnitude / Count,
                _hiddenSmallerCreatureSectorMagnitude / Count,
                _hiddenSimilarCreatureSectorMagnitude / Count,
                _hiddenLargerCreatureSectorMagnitude / Count,
                _moveFreshnessWeight / Count,
                _eatFreshnessWeight / Count,
                _moveRotScentDensityWeight / Count,
                _turnRotScentDensityWeight / Count,
                _moveRotScentForwardWeight / Count,
                _turnRotScentRightWeight / Count,
                _attackSmallerCreatureSectorWeight / Count,
                _attackLargerCreatureSectorWeight / Count);
        }
    }
}

public readonly record struct BrainInputDiagnosticSummary(
    int EvaluatedCreatureCount,
    float DirectFreshnessWeightMagnitude,
    float DirectRotScentWeightMagnitude,
    float DirectSmallerCreatureSectorWeightMagnitude,
    float DirectSimilarCreatureSectorWeightMagnitude,
    float DirectLargerCreatureSectorWeightMagnitude,
    float HiddenFreshnessWeightMagnitude,
    float HiddenRotScentWeightMagnitude,
    float HiddenSmallerCreatureSectorWeightMagnitude,
    float HiddenSimilarCreatureSectorWeightMagnitude,
    float HiddenLargerCreatureSectorWeightMagnitude,
    float MoveFreshnessWeight,
    float EatFreshnessWeight,
    float MoveRotScentDensityWeight,
    float TurnRotScentDensityWeight,
    float MoveRotScentForwardWeight,
    float TurnRotScentRightWeight,
    float AttackSmallerCreatureSectorWeight,
    float AttackLargerCreatureSectorWeight);

public readonly record struct LineageBrainInputDiagnosticSummary(
    EntityId FounderId,
    int LivingCreatures,
    float LivingShare,
    BrainInputDiagnosticSummary Diagnostics);
