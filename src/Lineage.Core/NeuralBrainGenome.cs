namespace Lineage.Core;

/// <summary>
/// Weighted neural controller used by the first evolvable brain phases.
/// </summary>
///
/// <remarks>
/// The model keeps direct input-to-output weights for stable starter behavior, then
/// optionally adds a small hidden layer for evolvable internal feature detectors.
/// The flat weight layout is direct weights first, hidden input weights second, and
/// hidden output weights last so older direct-only brains remain loadable.
/// </remarks>
public sealed class NeuralBrainGenome
{
    private const float WeightLimit = 8f;
    private const int SeedFoodOpportunityHiddenNode = 0;
    private const int SeedReproductionOpportunityHiddenNode = 1;
    private const int SeedCreatureCueHiddenNode = 2;
    private const int SeedTerrainDragHiddenNode = 3;
    private const int LegacyInputCountWithoutEggReserve = 8;
    private const int LegacyInputCountWithoutDietSenses = 9;
    private const int LegacyInputCountWithoutPreySenses = 18;
    private const int LegacyInputCountWithoutMeatScent = 22;
    private const int LegacyInputCountWithoutCreatureRelations = 25;
    private const int LegacyInputCountWithoutTerrainDrag = 29;
    private const int LegacyInputCountWithoutLateralTerrainDrag = 31;
    private const int LegacyInputCountWithoutReproductiveContext = 33;
    private const int LegacyInputCountWithoutMemory = 35;
    private const int LegacyInputCountWithoutRottenMeatSensing = 38;
    private const int LegacyOutputCountWithoutAttack = 4;
    private const int LegacyOutputCountWithoutMemory = 5;

    public NeuralBrainGenome(IEnumerable<float> weights)
    {
        var normalized = NormalizeWeights(weights.ToArray());
        Weights = normalized.Weights;
        HiddenNodeCount = normalized.HiddenNodeCount;
        ValidateWeights(Weights, HiddenNodeCount);
    }

    private NeuralBrainGenome(float[] weights, int hiddenNodeCount, bool trusted)
    {
        ValidateHiddenNodeCount(hiddenNodeCount);
        if (!trusted)
        {
            ValidateWeights(weights, hiddenNodeCount);
        }

        Weights = weights;
        HiddenNodeCount = hiddenNodeCount;
    }

    public float[] Weights { get; }

    public int HiddenNodeCount { get; }

    public static int DirectWeightCount => NeuralBrainSchema.InputCount * NeuralBrainSchema.OutputCount;

    public static int GetExpectedWeightCount(int hiddenNodeCount)
    {
        ValidateHiddenNodeCount(hiddenNodeCount);
        return DirectWeightCount
            + hiddenNodeCount * NeuralBrainSchema.InputCount
            + hiddenNodeCount * NeuralBrainSchema.OutputCount;
    }

    public static NeuralBrainGenome CreateZero(int hiddenNodeCount = 0)
    {
        return new NeuralBrainGenome(new float[GetExpectedWeightCount(hiddenNodeCount)], hiddenNodeCount, trusted: true);
    }

    public static NeuralBrainGenome CreateRandom(
        DeterministicRandom random,
        float scale = 1f,
        int hiddenNodeCount = 0)
    {
        if (!float.IsFinite(scale) || scale < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Random brain scale must be finite and non-negative.");
        }

        var weights = new float[GetExpectedWeightCount(hiddenNodeCount)];
        for (var i = 0; i < weights.Length; i++)
        {
            weights[i] = random.NextSingle(-scale, scale);
        }

        return new NeuralBrainGenome(weights, hiddenNodeCount, trusted: true);
    }

    /// <summary>
    /// Seed controller that can seek and eat visible resources without hard-coded actions.
    /// </summary>
    public static NeuralBrainGenome CreateSeedForager(int hiddenNodeCount = 0)
    {
        return new NeuralBrainGenome(CreateSeedForagerWeights(hiddenNodeCount), hiddenNodeCount, trusted: true);
    }

    /// <summary>
    /// Seed controller biased to keep moving when no food is visible, then forage once food enters vision.
    /// </summary>
    public static NeuralBrainGenome CreateExplorerForager(int hiddenNodeCount = 0)
    {
        var weights = CreateSeedForagerWeights(hiddenNodeCount);

        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.BiasInput, 0.9f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.EnergyRatioInput, 0.15f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.HungerInput, 1.15f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.FoodProximityInput, -2.2f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.FoodForwardInput, 0.85f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RecentFoodSuccessInput, -0.9f);

        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.FoodRightInput, 2.4f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.LeftTerrainDragInput, 0.25f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.RightTerrainDragInput, -0.25f);

        return new NeuralBrainGenome(weights, hiddenNodeCount, trusted: true);
    }

    /// <summary>
    /// Seed controller that still grazes, but actively follows visible meat and meat scent without attacking.
    /// </summary>
    public static NeuralBrainGenome CreateScavengerForager(int hiddenNodeCount = 0)
    {
        var weights = CreateSeedForagerWeights(hiddenNodeCount);

        SeedScavengerForagerWeights(weights);

        return new NeuralBrainGenome(weights, hiddenNodeCount, trusted: true);
    }

    /// <summary>
    /// Scavenger probe brain that follows meat cues, but suppresses movement into strong rot scent.
    /// </summary>
    public static NeuralBrainGenome CreateFreshnessAwareScavenger(int hiddenNodeCount = 0)
    {
        var weights = CreateSeedForagerWeights(hiddenNodeCount);
        SeedScavengerForagerWeights(weights);

        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RottenMeatScentDensityInput, -0.7f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RottenMeatScentForwardInput, -2.7f);

        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.RottenMeatScentDensityInput, -1.2f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.RottenMeatScentRightInput, -6.5f);

        return new NeuralBrainGenome(weights, hiddenNodeCount, trusted: true);
    }

    /// <summary>
    /// Seed controller that still forages, but also steers toward close visible creatures while hungry.
    /// </summary>
    public static NeuralBrainGenome CreateForagerPredator(int hiddenNodeCount = 0)
    {
        var weights = CreateSeedForagerWeights(hiddenNodeCount);

        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.HungerInput, 0.45f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.FoodForwardInput, 1.25f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.CreatureForwardInput, 1.2f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.CreatureProximityInput, -0.6f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.MeatScentForwardInput, 0.8f);

        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.FoodRightInput, 2.4f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.CreatureRightInput, 2.8f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.MeatScentRightInput, 1.4f);

        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.BiasInput, -4f);
        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.EnergyRatioInput, -1.5f);
        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.HungerInput, 2f);
        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.CreatureProximityInput, 4f);
        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.CreatureRelativeBodySizeInput, -0.8f);
        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.CreatureFacingAlignmentInput, -0.3f);
        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.DietaryMeatBiasInput, 1.5f);

        return new NeuralBrainGenome(weights, hiddenNodeCount, trusted: true);
    }

    private static float[] CreateSeedForagerWeights(int hiddenNodeCount)
    {
        var weights = new float[GetExpectedWeightCount(hiddenNodeCount)];

        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.HungerInput, 0.35f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.FoodProximityInput, -1.4f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.FoodForwardInput, 1.6f);

        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.FoodRightInput, 3.5f);

        Set(weights, NeuralBrainSchema.EatOutput, NeuralBrainSchema.BiasInput, -2.5f);
        Set(weights, NeuralBrainSchema.EatOutput, NeuralBrainSchema.FoodProximityInput, 4f);

        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.BiasInput, -2f);
        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.ReproductionReadinessInput, 2.75f);
        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.EnergySurplusInput, 0.75f);
        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.RecentFoodSuccessInput, 0.35f);
        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.VisibleCreatureDensityInput, -1.2f);

        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.BiasInput, -4f);

        SeedHiddenConceptInputs(weights, hiddenNodeCount);

        return weights;
    }

    private static void SeedScavengerForagerWeights(float[] weights)
    {
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.HungerInput, 0.45f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.FoodProximityInput, -1.3f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.FoodForwardInput, 1.25f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.MeatForwardInput, 0.9f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.MeatScentDensityInput, 0.2f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.MeatScentForwardInput, 1.15f);

        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.FoodRightInput, 2.4f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.MeatRightInput, 1.8f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.MeatScentRightInput, 2.5f);

        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.BiasInput, -4f);
    }

    private static void SeedHiddenConceptInputs(float[] weights, int hiddenNodeCount)
    {
        if (hiddenNodeCount > SeedFoodOpportunityHiddenNode)
        {
            SetHiddenInput(weights, hiddenNodeCount, SeedFoodOpportunityHiddenNode, NeuralBrainSchema.BiasInput, -0.3f);
            SetHiddenInput(weights, hiddenNodeCount, SeedFoodOpportunityHiddenNode, NeuralBrainSchema.HungerInput, 0.9f);
            SetHiddenInput(weights, hiddenNodeCount, SeedFoodOpportunityHiddenNode, NeuralBrainSchema.FoodForwardInput, 1.2f);
            SetHiddenInput(weights, hiddenNodeCount, SeedFoodOpportunityHiddenNode, NeuralBrainSchema.FoodProximityInput, 0.8f);
            SetHiddenInput(weights, hiddenNodeCount, SeedFoodOpportunityHiddenNode, NeuralBrainSchema.VisibleFoodDensityInput, 0.5f);
        }

        if (hiddenNodeCount > SeedReproductionOpportunityHiddenNode)
        {
            SetHiddenInput(weights, hiddenNodeCount, SeedReproductionOpportunityHiddenNode, NeuralBrainSchema.BiasInput, -0.8f);
            SetHiddenInput(weights, hiddenNodeCount, SeedReproductionOpportunityHiddenNode, NeuralBrainSchema.ReproductionReadinessInput, 1.5f);
            SetHiddenInput(weights, hiddenNodeCount, SeedReproductionOpportunityHiddenNode, NeuralBrainSchema.EnergySurplusInput, 0.9f);
            SetHiddenInput(weights, hiddenNodeCount, SeedReproductionOpportunityHiddenNode, NeuralBrainSchema.RecentFoodSuccessInput, 0.5f);
            SetHiddenInput(weights, hiddenNodeCount, SeedReproductionOpportunityHiddenNode, NeuralBrainSchema.VisibleCreatureDensityInput, -0.6f);
        }

        if (hiddenNodeCount > SeedCreatureCueHiddenNode)
        {
            SetHiddenInput(weights, hiddenNodeCount, SeedCreatureCueHiddenNode, NeuralBrainSchema.BiasInput, -0.2f);
            SetHiddenInput(weights, hiddenNodeCount, SeedCreatureCueHiddenNode, NeuralBrainSchema.CreatureForwardInput, 1.0f);
            SetHiddenInput(weights, hiddenNodeCount, SeedCreatureCueHiddenNode, NeuralBrainSchema.CreatureProximityInput, 1.1f);
            SetHiddenInput(weights, hiddenNodeCount, SeedCreatureCueHiddenNode, NeuralBrainSchema.CreatureRelativeBodySizeInput, -0.4f);
            SetHiddenInput(weights, hiddenNodeCount, SeedCreatureCueHiddenNode, NeuralBrainSchema.CreatureApproachRateInput, 0.4f);
        }

        if (hiddenNodeCount > SeedTerrainDragHiddenNode)
        {
            SetHiddenInput(weights, hiddenNodeCount, SeedTerrainDragHiddenNode, NeuralBrainSchema.CurrentTerrainDragInput, 0.8f);
            SetHiddenInput(weights, hiddenNodeCount, SeedTerrainDragHiddenNode, NeuralBrainSchema.ForwardTerrainDragInput, 1.1f);
            SetHiddenInput(weights, hiddenNodeCount, SeedTerrainDragHiddenNode, NeuralBrainSchema.LeftTerrainDragInput, 0.6f);
            SetHiddenInput(weights, hiddenNodeCount, SeedTerrainDragHiddenNode, NeuralBrainSchema.RightTerrainDragInput, 0.6f);
        }
    }

    public float GetWeight(int outputIndex, int inputIndex)
    {
        return Weights[GetWeightIndex(outputIndex, inputIndex)];
    }

    public float GetHiddenInputWeight(int hiddenIndex, int inputIndex)
    {
        return Weights[GetHiddenInputWeightIndex(HiddenNodeCount, hiddenIndex, inputIndex)];
    }

    public float GetHiddenOutputWeight(int outputIndex, int hiddenIndex)
    {
        return Weights[GetHiddenOutputWeightIndex(HiddenNodeCount, outputIndex, hiddenIndex)];
    }

    public int HiddenInputWeightCount => HiddenNodeCount * NeuralBrainSchema.InputCount;

    public int HiddenOutputWeightCount => HiddenNodeCount * NeuralBrainSchema.OutputCount;

    public float SumAbsoluteHiddenInputWeights()
    {
        var sum = 0f;
        var hiddenInputWeightCount = HiddenInputWeightCount;
        var offset = DirectWeightCount;

        for (var i = 0; i < hiddenInputWeightCount; i++)
        {
            sum += Math.Abs(Weights[offset + i]);
        }

        return sum;
    }

    public float SumAbsoluteHiddenOutputWeights()
    {
        var sum = 0f;
        var hiddenOutputWeightCount = HiddenOutputWeightCount;
        var offset = DirectWeightCount + HiddenInputWeightCount;

        for (var i = 0; i < hiddenOutputWeightCount; i++)
        {
            sum += Math.Abs(Weights[offset + i]);
        }

        return sum;
    }

    public int CountActiveHiddenOutputWeights(float threshold)
    {
        if (!float.IsFinite(threshold) || threshold < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "Hidden output threshold must be finite and non-negative.");
        }

        var count = 0;
        var hiddenOutputWeightCount = HiddenOutputWeightCount;
        var offset = DirectWeightCount + HiddenInputWeightCount;

        for (var i = 0; i < hiddenOutputWeightCount; i++)
        {
            if (Math.Abs(Weights[offset + i]) >= threshold)
            {
                count++;
            }
        }

        return count;
    }

    public void Evaluate(ReadOnlySpan<float> inputs, Span<float> outputs)
    {
        if (inputs.Length != NeuralBrainSchema.InputCount)
        {
            throw new ArgumentException("Unexpected neural input count.", nameof(inputs));
        }

        if (outputs.Length != NeuralBrainSchema.OutputCount)
        {
            throw new ArgumentException("Unexpected neural output count.", nameof(outputs));
        }

        for (var output = 0; output < NeuralBrainSchema.OutputCount; output++)
        {
            var sum = 0f;
            var offset = output * NeuralBrainSchema.InputCount;

            for (var input = 0; input < NeuralBrainSchema.InputCount; input++)
            {
                sum += Weights[offset + input] * inputs[input];
            }

            outputs[output] = sum;
        }

        if (HiddenNodeCount > 0)
        {
            Span<float> hiddenValues = HiddenNodeCount <= NeuralBrainSchema.MaxHiddenNodeCount
                ? stackalloc float[HiddenNodeCount]
                : new float[HiddenNodeCount];

            for (var hidden = 0; hidden < HiddenNodeCount; hidden++)
            {
                var sum = 0f;
                var offset = DirectWeightCount + hidden * NeuralBrainSchema.InputCount;

                for (var input = 0; input < NeuralBrainSchema.InputCount; input++)
                {
                    sum += Weights[offset + input] * inputs[input];
                }

                hiddenValues[hidden] = MathF.Tanh(sum);
            }

            var hiddenOutputOffset = DirectWeightCount + HiddenNodeCount * NeuralBrainSchema.InputCount;
            for (var output = 0; output < NeuralBrainSchema.OutputCount; output++)
            {
                var sum = outputs[output];
                var offset = hiddenOutputOffset + output * HiddenNodeCount;

                for (var hidden = 0; hidden < HiddenNodeCount; hidden++)
                {
                    sum += Weights[offset + hidden] * hiddenValues[hidden];
                }

                outputs[output] = sum;
            }
        }

        for (var output = 0; output < NeuralBrainSchema.OutputCount; output++)
        {
            outputs[output] = MathF.Tanh(outputs[output]);
        }
    }

    public NeuralBrainGenome Mutated(DeterministicRandom random, float mutationStrength)
    {
        return Mutated(random, mutationStrength, mutationRate: 1f);
    }

    public NeuralBrainGenome Mutated(DeterministicRandom random, float mutationStrength, float mutationRate)
    {
        if (!float.IsFinite(mutationStrength) || mutationStrength < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(mutationStrength), "Mutation strength must be finite and non-negative.");
        }

        if (!float.IsFinite(mutationRate) || mutationRate < 0f || mutationRate > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(mutationRate), "Mutation rate must be finite and between 0 and 1.");
        }

        var strength = Math.Clamp(mutationStrength, 0f, 1f);
        var weights = new float[Weights.Length];
        var mutatedAny = false;

        for (var i = 0; i < weights.Length; i++)
        {
            var shouldMutate = strength > 0f && mutationRate > 0f && random.NextSingle() < mutationRate;
            mutatedAny |= shouldMutate;
            weights[i] = shouldMutate
                ? Math.Clamp(Weights[i] + random.NextSingle(-strength, strength), -WeightLimit, WeightLimit)
                : Weights[i];
        }

        if (!mutatedAny && strength > 0f && mutationRate > 0f && weights.Length > 0)
        {
            var index = random.NextInt32(weights.Length);
            weights[index] = Math.Clamp(Weights[index] + random.NextSingle(-strength, strength), -WeightLimit, WeightLimit);
        }

        return new NeuralBrainGenome(weights, HiddenNodeCount, trusted: true);
    }

    private static void Set(float[] weights, int outputIndex, int inputIndex, float value)
    {
        weights[GetWeightIndex(outputIndex, inputIndex)] = value;
    }

    private static void SetHiddenInput(
        float[] weights,
        int hiddenNodeCount,
        int hiddenIndex,
        int inputIndex,
        float value)
    {
        weights[GetHiddenInputWeightIndex(hiddenNodeCount, hiddenIndex, inputIndex)] = value;
    }

    private static int GetWeightIndex(int outputIndex, int inputIndex)
    {
        if ((uint)outputIndex >= NeuralBrainSchema.OutputCount)
        {
            throw new ArgumentOutOfRangeException(nameof(outputIndex));
        }

        if ((uint)inputIndex >= NeuralBrainSchema.InputCount)
        {
            throw new ArgumentOutOfRangeException(nameof(inputIndex));
        }

        return outputIndex * NeuralBrainSchema.InputCount + inputIndex;
    }

    private static int GetHiddenInputWeightIndex(int hiddenNodeCount, int hiddenIndex, int inputIndex)
    {
        ValidateHiddenNodeCount(hiddenNodeCount);
        if ((uint)hiddenIndex >= (uint)hiddenNodeCount)
        {
            throw new ArgumentOutOfRangeException(nameof(hiddenIndex));
        }

        if ((uint)inputIndex >= NeuralBrainSchema.InputCount)
        {
            throw new ArgumentOutOfRangeException(nameof(inputIndex));
        }

        return DirectWeightCount + hiddenIndex * NeuralBrainSchema.InputCount + inputIndex;
    }

    private static int GetHiddenOutputWeightIndex(int hiddenNodeCount, int outputIndex, int hiddenIndex)
    {
        ValidateHiddenNodeCount(hiddenNodeCount);
        if ((uint)outputIndex >= NeuralBrainSchema.OutputCount)
        {
            throw new ArgumentOutOfRangeException(nameof(outputIndex));
        }

        if ((uint)hiddenIndex >= (uint)hiddenNodeCount)
        {
            throw new ArgumentOutOfRangeException(nameof(hiddenIndex));
        }

        return DirectWeightCount
            + hiddenNodeCount * NeuralBrainSchema.InputCount
            + outputIndex * hiddenNodeCount
            + hiddenIndex;
    }

    private static void ValidateWeights(float[] weights, int hiddenNodeCount)
    {
        if (weights.Length != GetExpectedWeightCount(hiddenNodeCount))
        {
            throw new ArgumentException("Unexpected neural brain weight count.", nameof(weights));
        }

        for (var i = 0; i < weights.Length; i++)
        {
            if (!float.IsFinite(weights[i]))
            {
                throw new ArgumentException($"Brain weight {i} must be finite.", nameof(weights));
            }
        }
    }

    private static void ValidateHiddenNodeCount(int hiddenNodeCount)
    {
        if (hiddenNodeCount < 0 || hiddenNodeCount > NeuralBrainSchema.MaxHiddenNodeCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hiddenNodeCount),
                $"Hidden node count must be between 0 and {NeuralBrainSchema.MaxHiddenNodeCount}.");
        }
    }

    private static (float[] Weights, int HiddenNodeCount) NormalizeWeights(float[] weights)
    {
        if (TryInferCurrentWeightLayout(weights.Length, out var hiddenNodeCount))
        {
            return (weights, hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutRottenMeatSensing,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutRottenMeatSensing,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutMemory,
            LegacyOutputCountWithoutMemory,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutMemory,
                LegacyOutputCountWithoutMemory,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (weights.Length == LegacyInputCountWithoutReproductiveContext * LegacyOutputCountWithoutMemory)
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutReproductiveContext,
                LegacyOutputCountWithoutMemory,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput), 0);
        }

        if (weights.Length == LegacyInputCountWithoutLateralTerrainDrag * LegacyOutputCountWithoutMemory)
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutLateralTerrainDrag,
                LegacyOutputCountWithoutMemory,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput), 0);
        }

        if (weights.Length == LegacyInputCountWithoutTerrainDrag * LegacyOutputCountWithoutMemory)
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutTerrainDrag,
                LegacyOutputCountWithoutMemory,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput), 0);
        }

        if (weights.Length == LegacyInputCountWithoutCreatureRelations * LegacyOutputCountWithoutMemory)
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutCreatureRelations,
                LegacyOutputCountWithoutMemory,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput), 0);
        }

        if (weights.Length == LegacyInputCountWithoutMeatScent * LegacyOutputCountWithoutMemory)
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutMeatScent,
                LegacyOutputCountWithoutMemory,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput), 0);
        }

        if (weights.Length == LegacyInputCountWithoutPreySenses * LegacyOutputCountWithoutAttack)
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutPreySenses,
                LegacyOutputCountWithoutAttack,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput), 0);
        }

        if (weights.Length == LegacyInputCountWithoutDietSenses * LegacyOutputCountWithoutAttack)
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutDietSenses,
                LegacyOutputCountWithoutAttack,
                oldEggReserveInput: 7,
                oldReproductionReadinessInput: 8), 0);
        }

        if (weights.Length != LegacyInputCountWithoutEggReserve * LegacyOutputCountWithoutAttack)
        {
            return (weights, 0);
        }

        return (NormalizeLegacyWeights(
            weights,
            LegacyInputCountWithoutEggReserve,
            LegacyOutputCountWithoutAttack,
            oldEggReserveInput: -1,
            oldReproductionReadinessInput: 7), 0);
    }

    private static bool TryInferCurrentWeightLayout(int weightCount, out int hiddenNodeCount)
    {
        hiddenNodeCount = 0;
        if (weightCount < DirectWeightCount)
        {
            return false;
        }

        var hiddenWeightCount = weightCount - DirectWeightCount;
        var weightsPerHiddenNode = NeuralBrainSchema.InputCount + NeuralBrainSchema.OutputCount;
        if (hiddenWeightCount % weightsPerHiddenNode != 0)
        {
            return false;
        }

        hiddenNodeCount = hiddenWeightCount / weightsPerHiddenNode;
        ValidateHiddenNodeCount(hiddenNodeCount);
        return true;
    }

    private static bool TryInferLegacyWeightLayout(
        int weightCount,
        int legacyInputCount,
        int legacyOutputCount,
        out int hiddenNodeCount)
    {
        hiddenNodeCount = 0;
        var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
        if (weightCount < legacyDirectWeightCount)
        {
            return false;
        }

        var hiddenWeightCount = weightCount - legacyDirectWeightCount;
        var weightsPerHiddenNode = legacyInputCount + legacyOutputCount;
        if (hiddenWeightCount % weightsPerHiddenNode != 0)
        {
            return false;
        }

        hiddenNodeCount = hiddenWeightCount / weightsPerHiddenNode;
        ValidateHiddenNodeCount(hiddenNodeCount);
        return true;
    }

    private static float[] NormalizeLegacyWeights(
        float[] weights,
        int legacyInputCount,
        int legacyOutputCount,
        int oldEggReserveInput,
        int oldReproductionReadinessInput,
        int hiddenNodeCount = 0)
    {
        var migrated = new float[GetExpectedWeightCount(hiddenNodeCount)];
        for (var output = 0; output < legacyOutputCount; output++)
        {
            var legacyOffset = output * legacyInputCount;
            var newOffset = output * NeuralBrainSchema.InputCount;

            for (var input = 0; input < legacyInputCount; input++)
            {
                var targetInput = input;
                if (input == oldEggReserveInput)
                {
                    targetInput = NeuralBrainSchema.EggReserveRatioInput;
                }
                else if (input == oldReproductionReadinessInput)
                {
                    targetInput = NeuralBrainSchema.ReproductionReadinessInput;
                }

                migrated[newOffset + targetInput] = weights[legacyOffset + input];
            }
        }

        if (hiddenNodeCount > 0)
        {
            var legacyDirectWeightCount = legacyInputCount * legacyOutputCount;
            var legacyHiddenInputOffset = legacyDirectWeightCount;
            var newHiddenInputOffset = DirectWeightCount;

            for (var hidden = 0; hidden < hiddenNodeCount; hidden++)
            {
                var legacyOffset = legacyHiddenInputOffset + hidden * legacyInputCount;
                var newOffset = newHiddenInputOffset + hidden * NeuralBrainSchema.InputCount;

                for (var input = 0; input < legacyInputCount; input++)
                {
                    var targetInput = input;
                    if (input == oldEggReserveInput)
                    {
                        targetInput = NeuralBrainSchema.EggReserveRatioInput;
                    }
                    else if (input == oldReproductionReadinessInput)
                    {
                        targetInput = NeuralBrainSchema.ReproductionReadinessInput;
                    }

                    migrated[newOffset + targetInput] = weights[legacyOffset + input];
                }
            }

            var legacyHiddenOutputOffset = legacyHiddenInputOffset + hiddenNodeCount * legacyInputCount;
            var newHiddenOutputOffset = DirectWeightCount + hiddenNodeCount * NeuralBrainSchema.InputCount;
            for (var output = 0; output < legacyOutputCount; output++)
            {
                var legacyOffset = legacyHiddenOutputOffset + output * hiddenNodeCount;
                var newOffset = newHiddenOutputOffset + output * hiddenNodeCount;
                for (var hidden = 0; hidden < hiddenNodeCount; hidden++)
                {
                    migrated[newOffset + hidden] = weights[legacyOffset + hidden];
                }
            }
        }

        return migrated;
    }
}
