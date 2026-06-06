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

    private static readonly int[] SmallerCreatureSectorInputs =
    [
        NeuralBrainSchema.VisibleCreatureDensityInput,
        NeuralBrainSchema.CreatureProximityInput,
        NeuralBrainSchema.CreatureRelativeBodySizeInput
    ];

    private static readonly int[] SimilarCreatureSectorInputs =
    [
        NeuralBrainSchema.CreatureVisualTraitSimilarityInput,
        NeuralBrainSchema.CreatureVisualLineageSimilarityInput,
        NeuralBrainSchema.CreatureVisualIdentitySimilarityInput
    ];

    private static readonly int[] LargerCreatureSectorInputs =
    [
        NeuralBrainSchema.CreatureProximityInput,
        NeuralBrainSchema.CreatureRelativeBodySizeInput
    ];

    private static readonly int[] CreatureApproachSectorInputs =
    [
        NeuralBrainSchema.CreatureApproachRateInput
    ];

    private static readonly int[] CreatureFacingSectorInputs =
    [
        NeuralBrainSchema.CreatureFacingAlignmentInput
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

            if (state.TryGetBrain(creature.BrainId, out var brain) && brain is not null)
            {
                accumulator.Add(brain);
            }
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

            if (!state.TryGetBrain(creature.BrainId, out var brain) || brain is null)
            {
                continue;
            }

            var founderId = FindFounderId(creature, recordsById);
            if (!groups.TryGetValue(founderId, out var group))
            {
                group = new LineageBrainInputAccumulator { FounderId = founderId };
            }

            group.Diagnostics.Add(brain);
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

    private static float MeanAbsoluteDirectInputWeights(BrainGenome brain, int input)
    {
        var sum = 0f;
        for (var output = 0; output < NeuralBrainSchema.OutputCount; output++)
        {
            sum += Math.Abs(brain.GetWeight(output, input));
        }

        return sum / NeuralBrainSchema.OutputCount;
    }

    private static float MeanAbsoluteDirectInputWeights(BrainGenome brain, IReadOnlyList<int> inputs)
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

    private static float MeanAbsoluteHiddenInputWeights(BrainGenome brain, int input)
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

    private static float MeanAbsoluteHiddenInputWeights(BrainGenome brain, IReadOnlyList<int> inputs)
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

    private static float MeanDirectOutputInputWeights(BrainGenome brain, int output, IReadOnlyList<int> inputs)
    {
        var sum = 0f;
        for (var i = 0; i < inputs.Count; i++)
        {
            sum += brain.GetWeight(output, inputs[i]);
        }

        return sum / inputs.Count;
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
        private float _directCreatureApproachSectorMagnitude;
        private float _directCreatureFacingSectorMagnitude;
        private float _hiddenFreshnessMagnitude;
        private float _hiddenRotScentMagnitude;
        private float _hiddenSmallerCreatureSectorMagnitude;
        private float _hiddenSimilarCreatureSectorMagnitude;
        private float _hiddenLargerCreatureSectorMagnitude;
        private float _hiddenCreatureApproachSectorMagnitude;
        private float _hiddenCreatureFacingSectorMagnitude;
        private float _moveFreshnessWeight;
        private float _eatFreshnessWeight;
        private float _moveRotScentDensityWeight;
        private float _turnRotScentDensityWeight;
        private float _moveRotScentForwardWeight;
        private float _turnRotScentRightWeight;
        private float _attackSmallerCreatureSectorWeight;
        private float _attackLargerCreatureSectorWeight;
        private float _attackCreatureApproachSectorWeight;
        private float _attackCreatureFacingSectorWeight;

        public void Add(BrainGenome brain)
        {
            Count++;
            _directFreshnessMagnitude += MeanAbsoluteDirectInputWeights(brain, NeuralBrainSchema.VisibleMeatFreshnessInput);
            _directRotScentMagnitude += MeanAbsoluteDirectInputWeights(brain, RotScentInputs);
            _directSmallerCreatureSectorMagnitude += MeanAbsoluteDirectInputWeights(brain, SmallerCreatureSectorInputs);
            _directSimilarCreatureSectorMagnitude += MeanAbsoluteDirectInputWeights(brain, SimilarCreatureSectorInputs);
            _directLargerCreatureSectorMagnitude += MeanAbsoluteDirectInputWeights(brain, LargerCreatureSectorInputs);
            _directCreatureApproachSectorMagnitude += MeanAbsoluteDirectInputWeights(brain, CreatureApproachSectorInputs);
            _directCreatureFacingSectorMagnitude += MeanAbsoluteDirectInputWeights(brain, CreatureFacingSectorInputs);
            _hiddenFreshnessMagnitude += MeanAbsoluteHiddenInputWeights(brain, NeuralBrainSchema.VisibleMeatFreshnessInput);
            _hiddenRotScentMagnitude += MeanAbsoluteHiddenInputWeights(brain, RotScentInputs);
            _hiddenSmallerCreatureSectorMagnitude += MeanAbsoluteHiddenInputWeights(brain, SmallerCreatureSectorInputs);
            _hiddenSimilarCreatureSectorMagnitude += MeanAbsoluteHiddenInputWeights(brain, SimilarCreatureSectorInputs);
            _hiddenLargerCreatureSectorMagnitude += MeanAbsoluteHiddenInputWeights(brain, LargerCreatureSectorInputs);
            _hiddenCreatureApproachSectorMagnitude += MeanAbsoluteHiddenInputWeights(brain, CreatureApproachSectorInputs);
            _hiddenCreatureFacingSectorMagnitude += MeanAbsoluteHiddenInputWeights(brain, CreatureFacingSectorInputs);
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
            _attackCreatureApproachSectorWeight += MeanDirectOutputInputWeights(
                brain,
                NeuralBrainSchema.AttackOutput,
                CreatureApproachSectorInputs);
            _attackCreatureFacingSectorWeight += MeanDirectOutputInputWeights(
                brain,
                NeuralBrainSchema.AttackOutput,
                CreatureFacingSectorInputs);
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
                _directCreatureApproachSectorMagnitude / Count,
                _directCreatureFacingSectorMagnitude / Count,
                _hiddenFreshnessMagnitude / Count,
                _hiddenRotScentMagnitude / Count,
                _hiddenSmallerCreatureSectorMagnitude / Count,
                _hiddenSimilarCreatureSectorMagnitude / Count,
                _hiddenLargerCreatureSectorMagnitude / Count,
                _hiddenCreatureApproachSectorMagnitude / Count,
                _hiddenCreatureFacingSectorMagnitude / Count,
                _moveFreshnessWeight / Count,
                _eatFreshnessWeight / Count,
                _moveRotScentDensityWeight / Count,
                _turnRotScentDensityWeight / Count,
                _moveRotScentForwardWeight / Count,
                _turnRotScentRightWeight / Count,
                _attackSmallerCreatureSectorWeight / Count,
                _attackLargerCreatureSectorWeight / Count,
                _attackCreatureApproachSectorWeight / Count,
                _attackCreatureFacingSectorWeight / Count);
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
    float DirectCreatureApproachSectorWeightMagnitude,
    float DirectCreatureFacingSectorWeightMagnitude,
    float HiddenFreshnessWeightMagnitude,
    float HiddenRotScentWeightMagnitude,
    float HiddenSmallerCreatureSectorWeightMagnitude,
    float HiddenSimilarCreatureSectorWeightMagnitude,
    float HiddenLargerCreatureSectorWeightMagnitude,
    float HiddenCreatureApproachSectorWeightMagnitude,
    float HiddenCreatureFacingSectorWeightMagnitude,
    float MoveFreshnessWeight,
    float EatFreshnessWeight,
    float MoveRotScentDensityWeight,
    float TurnRotScentDensityWeight,
    float MoveRotScentForwardWeight,
    float TurnRotScentRightWeight,
    float AttackSmallerCreatureSectorWeight,
    float AttackLargerCreatureSectorWeight,
    float AttackCreatureApproachSectorWeight,
    float AttackCreatureFacingSectorWeight);

public readonly record struct LineageBrainInputDiagnosticSummary(
    EntityId FounderId,
    int LivingCreatures,
    float LivingShare,
    BrainInputDiagnosticSummary Diagnostics);
