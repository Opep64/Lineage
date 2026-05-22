namespace Lineage.Core;

/// <summary>
/// Direct weighted-input neural controller used by the first evolvable brain phase.
/// </summary>
///
/// <remarks>
/// The model is intentionally modest: every output is a tanh activation over the
/// fixed input vector. This is enough for selection to act on movement/eating/
/// reproduction choices while keeping performance predictable and tests easy.
/// </remarks>
public sealed class NeuralBrainGenome
{
    private const float WeightLimit = 8f;
    private const int LegacyInputCountWithoutEggReserve = 8;
    private const int LegacyInputCountWithoutDietSenses = 9;
    private const int LegacyInputCountWithoutPreySenses = 18;
    private const int LegacyInputCountWithoutMeatScent = 22;
    private const int LegacyInputCountWithoutCreatureRelations = 25;
    private const int LegacyInputCountWithoutTerrainDrag = 29;
    private const int LegacyInputCountWithoutLateralTerrainDrag = 31;
    private const int LegacyInputCountWithoutReproductiveContext = 33;
    private const int LegacyOutputCountWithoutAttack = 4;

    public NeuralBrainGenome(IEnumerable<float> weights)
    {
        Weights = NormalizeWeights(weights.ToArray());
        ValidateWeights(Weights);
    }

    private NeuralBrainGenome(float[] weights, bool trusted)
    {
        if (!trusted)
        {
            ValidateWeights(weights);
        }

        Weights = weights;
    }

    public float[] Weights { get; }

    public static NeuralBrainGenome CreateZero()
    {
        return new NeuralBrainGenome(new float[NeuralBrainSchema.InputCount * NeuralBrainSchema.OutputCount], trusted: true);
    }

    public static NeuralBrainGenome CreateRandom(DeterministicRandom random, float scale = 1f)
    {
        if (!float.IsFinite(scale) || scale < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Random brain scale must be finite and non-negative.");
        }

        var weights = new float[NeuralBrainSchema.InputCount * NeuralBrainSchema.OutputCount];
        for (var i = 0; i < weights.Length; i++)
        {
            weights[i] = random.NextSingle(-scale, scale);
        }

        return new NeuralBrainGenome(weights, trusted: true);
    }

    /// <summary>
    /// Seed controller that can seek and eat visible resources without hard-coded actions.
    /// </summary>
    public static NeuralBrainGenome CreateSeedForager()
    {
        return new NeuralBrainGenome(CreateSeedForagerWeights(), trusted: true);
    }

    /// <summary>
    /// Seed controller that still forages, but also steers toward close visible creatures while hungry.
    /// </summary>
    public static NeuralBrainGenome CreateForagerPredator()
    {
        var weights = CreateSeedForagerWeights();

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

        return new NeuralBrainGenome(weights, trusted: true);
    }

    private static float[] CreateSeedForagerWeights()
    {
        var weights = new float[NeuralBrainSchema.InputCount * NeuralBrainSchema.OutputCount];

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

        return weights;
    }

    public float GetWeight(int outputIndex, int inputIndex)
    {
        return Weights[GetWeightIndex(outputIndex, inputIndex)];
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

            outputs[output] = MathF.Tanh(sum);
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

        return new NeuralBrainGenome(weights, trusted: true);
    }

    private static void Set(float[] weights, int outputIndex, int inputIndex, float value)
    {
        weights[GetWeightIndex(outputIndex, inputIndex)] = value;
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

    private static void ValidateWeights(float[] weights)
    {
        if (weights.Length != NeuralBrainSchema.InputCount * NeuralBrainSchema.OutputCount)
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

    private static float[] NormalizeWeights(float[] weights)
    {
        if (weights.Length == NeuralBrainSchema.InputCount * NeuralBrainSchema.OutputCount)
        {
            return weights;
        }

        if (weights.Length == LegacyInputCountWithoutReproductiveContext * NeuralBrainSchema.OutputCount)
        {
            return NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutReproductiveContext,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput);
        }

        if (weights.Length == LegacyInputCountWithoutLateralTerrainDrag * NeuralBrainSchema.OutputCount)
        {
            return NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutLateralTerrainDrag,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput);
        }

        if (weights.Length == LegacyInputCountWithoutTerrainDrag * NeuralBrainSchema.OutputCount)
        {
            return NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutTerrainDrag,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput);
        }

        if (weights.Length == LegacyInputCountWithoutCreatureRelations * NeuralBrainSchema.OutputCount)
        {
            return NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutCreatureRelations,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput);
        }

        if (weights.Length == LegacyInputCountWithoutMeatScent * NeuralBrainSchema.OutputCount)
        {
            return NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutMeatScent,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput);
        }

        if (weights.Length == LegacyInputCountWithoutPreySenses * LegacyOutputCountWithoutAttack)
        {
            return NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutPreySenses,
                LegacyOutputCountWithoutAttack,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput);
        }

        if (weights.Length == LegacyInputCountWithoutDietSenses * LegacyOutputCountWithoutAttack)
        {
            return NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutDietSenses,
                LegacyOutputCountWithoutAttack,
                oldEggReserveInput: 7,
                oldReproductionReadinessInput: 8);
        }

        if (weights.Length != LegacyInputCountWithoutEggReserve * LegacyOutputCountWithoutAttack)
        {
            return weights;
        }

        return NormalizeLegacyWeights(
            weights,
            LegacyInputCountWithoutEggReserve,
            LegacyOutputCountWithoutAttack,
            oldEggReserveInput: -1,
            oldReproductionReadinessInput: 7);
    }

    private static float[] NormalizeLegacyWeights(
        float[] weights,
        int legacyInputCount,
        int legacyOutputCount,
        int oldEggReserveInput,
        int oldReproductionReadinessInput)
    {
        var migrated = new float[NeuralBrainSchema.InputCount * NeuralBrainSchema.OutputCount];
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

        return migrated;
    }
}
