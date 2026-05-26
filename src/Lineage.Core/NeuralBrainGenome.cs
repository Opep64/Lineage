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
    private const float SparseDirectWeightThreshold = 0.75f;
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
    private const int LegacyInputCountWithoutObstacleSensing = 42;
    private const int LegacyInputCountWithoutSectorVision = 46;
    private const int LegacyVisionSectorChannelCount = 8;
    private const int LegacyInputCountWithoutFoodContact = 118;
    private const int LegacyInputCountWithoutFoodContactKinds = 119;
    private const int LegacyInputCountWithoutHealthRatio = 122;
    private const int LegacyInputCountWithoutCreatureSectorSize = 123;
    private const int LegacyCreatureSectorSizeChannelCount = 14;
    private const int LegacyCreatureSectorMotionFoodContactInput =
        NeuralBrainSchema.VisionSectorInputStart + VisionSectorSet.SectorCount * LegacyCreatureSectorSizeChannelCount;
    private const int LegacyInputCountWithoutCreatureSectorMotion = LegacyCreatureSectorMotionFoodContactInput + 5;
    private const int LegacyInputCountWithoutCreatureContact = 195;
    private const int LegacyInputCountWithoutPlantQuality = 196;
    private const int LegacyInputCountWithoutFoodEnergyYield = 202;
    private const int LegacyVisionSectorChannelCountWithoutPlantQuality = 16;
    private const int LegacySectorPlantQualityFoodContactInput =
        NeuralBrainSchema.VisionSectorInputStart + VisionSectorSet.SectorCount * LegacyVisionSectorChannelCountWithoutPlantQuality;
    private const int LegacyInputCountWithoutSectorPlantQuality = LegacySectorPlantQualityFoodContactInput + 13;
    private const int LegacyInputCountWithoutPlantPreferenceBridge = 227;
    private const int LegacyInputCountWithoutCreatureSimilarityScent = 231;
    private const int LegacyInputCountWithoutPlantPayoffTrace = 224;
    private const int LegacyInputCountWithoutTypedPlantEnergyYield = 221;
    private const int LegacyOutputCountWithoutAttack = 4;
    private const int LegacyOutputCountWithoutMemory = 5;

    public NeuralBrainGenome(IEnumerable<float> weights)
    {
        var normalized = NormalizeWeights(weights.ToArray());
        Weights = normalized.Weights;
        HiddenNodeCount = normalized.HiddenNodeCount;
        HasActiveHiddenOutputs = HasNonZeroHiddenOutputWeights(Weights, HiddenNodeCount);
        SparseDirectWeightIndices = CreateSparseDirectWeightIndex(Weights);
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
        HasActiveHiddenOutputs = HasNonZeroHiddenOutputWeights(Weights, HiddenNodeCount);
        SparseDirectWeightIndices = CreateSparseDirectWeightIndex(Weights);
    }

    public float[] Weights { get; }

    public int HiddenNodeCount { get; }

    private bool HasActiveHiddenOutputs { get; }

    private int[]? SparseDirectWeightIndices { get; }

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

    public static NeuralBrainGenome CreateHiddenLayerRandom(
        DeterministicRandom random,
        float scale = 1f,
        int hiddenNodeCount = NeuralBrainSchema.DefaultHiddenLayerNodeCount)
    {
        if (!float.IsFinite(scale) || scale < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Random brain scale must be finite and non-negative.");
        }

        ValidateHiddenLayerNodeCount(hiddenNodeCount);

        var weights = new float[GetExpectedWeightCount(hiddenNodeCount)];
        for (var i = DirectWeightCount; i < weights.Length; i++)
        {
            weights[i] = random.NextSingle(-scale, scale);
        }

        return new NeuralBrainGenome(weights, hiddenNodeCount, trusted: true);
    }

    public static NeuralBrainGenome CreateHiddenLayerFromDirect(
        NeuralBrainGenome directBrain,
        int hiddenNodeCount = NeuralBrainSchema.DefaultHiddenLayerNodeCount)
    {
        ArgumentNullException.ThrowIfNull(directBrain);
        ValidateHiddenLayerNodeCount(hiddenNodeCount);

        var weights = new float[GetExpectedWeightCount(hiddenNodeCount)];
        for (var output = 0; output < NeuralBrainSchema.OutputCount; output++)
        {
            var hidden = output;
            for (var input = 0; input < NeuralBrainSchema.InputCount; input++)
            {
                SetHiddenInput(weights, hiddenNodeCount, hidden, input, directBrain.GetWeight(output, input));
            }

            SetHiddenOutput(weights, hiddenNodeCount, output, hidden, 1.75f);
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
    /// Probe controller that steers from visual sector channels instead of the legacy nearest-food direction.
    /// </summary>
    public static NeuralBrainGenome CreateSectorForager(int hiddenNodeCount = 0)
    {
        return new NeuralBrainGenome(CreateSectorForagerWeights(hiddenNodeCount), hiddenNodeCount, trusted: true);
    }

    /// <summary>
    /// Plant-first probe controller that will sample meat it reaches without actively scent-chasing carrion.
    /// </summary>
    public static NeuralBrainGenome CreateOpportunisticForager(int hiddenNodeCount = 0)
    {
        var weights = CreateSectorForagerWeights(hiddenNodeCount);

        Set(weights, NeuralBrainSchema.EatOutput, NeuralBrainSchema.MeatFoodContactInput, 3.5f);

        return new NeuralBrainGenome(weights, hiddenNodeCount, trusted: true);
    }

    /// <summary>
    /// Seed controller that still grazes, but actively follows visible meat and meat scent without attacking.
    /// </summary>
    public static NeuralBrainGenome CreateScavengerForager(int hiddenNodeCount = 0)
    {
        var weights = CreateSectorForagerWeights(hiddenNodeCount);

        SeedScavengerForagerWeights(weights);

        return new NeuralBrainGenome(weights, hiddenNodeCount, trusted: true);
    }

    /// <summary>
    /// Scavenger probe brain that follows meat cues, but suppresses movement into strong rot scent.
    /// </summary>
    public static NeuralBrainGenome CreateFreshnessAwareScavenger(int hiddenNodeCount = 0)
    {
        var weights = CreateSectorForagerWeights(hiddenNodeCount);
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
        var weights = CreateSectorForagerWeights(hiddenNodeCount);

        SeedSectorCreatureHuntingWeights(weights);

        Set(weights, NeuralBrainSchema.EatOutput, NeuralBrainSchema.MeatFoodContactInput, 5.0f);
        Set(weights, NeuralBrainSchema.EatOutput, NeuralBrainSchema.MeatProximityInput, 4.0f);

        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.HungerInput, 0.45f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.FoodForwardInput, 1.25f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.CreatureForwardInput, 1.2f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.CreatureProximityInput, -0.6f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.CreatureContactInput, -0.7f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.MeatScentForwardInput, 0.8f);

        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.FoodRightInput, 2.4f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.CreatureRightInput, 2.8f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.MeatScentRightInput, 1.4f);

        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.BiasInput, -4f);
        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.EnergyRatioInput, -1.5f);
        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.HungerInput, 2f);
        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.CreatureProximityInput, 4f);
        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.CreatureContactInput, 4.5f);
        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.CreatureRelativeBodySizeInput, -0.8f);
        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.CreatureFacingAlignmentInput, -0.3f);
        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.DietaryMeatBiasInput, 1.5f);

        return new NeuralBrainGenome(weights, hiddenNodeCount, trusted: true);
    }

    private static float[] CreateSectorForagerWeights(int hiddenNodeCount)
    {
        var weights = new float[GetExpectedWeightCount(hiddenNodeCount)];

        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.BiasInput, 0.35f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.HungerInput, 0.55f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RecentFoodSuccessInput, -0.45f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.FoodContactInput, -3.0f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.ForwardObstacleInput, -1.1f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.MovementBlockedInput, -1.5f);

        Set(weights, NeuralBrainSchema.EatOutput, NeuralBrainSchema.BiasInput, -2.5f);
        Set(weights, NeuralBrainSchema.EatOutput, NeuralBrainSchema.PlantProximityInput, 4.0f);
        Set(weights, NeuralBrainSchema.EatOutput, NeuralBrainSchema.PlantFoodContactInput, 5.0f);
        Set(weights, NeuralBrainSchema.EatOutput, NeuralBrainSchema.MeatFoodContactInput, 1.0f);

        for (var sectorIndex = 0; sectorIndex < VisionSectorSet.SectorCount; sectorIndex++)
        {
            var side = (sectorIndex - VisionSectorSet.CenterSectorIndex) / (float)VisionSectorSet.CenterSectorIndex;
            var centerBias = 1f - Math.Abs(side);
            var plantDensityInput = NeuralBrainSchema.VisionSectorPlantDensityInput(sectorIndex);
            var plantProximityInput = NeuralBrainSchema.VisionSectorPlantProximityInput(sectorIndex);
            var meatDensityInput = NeuralBrainSchema.VisionSectorMeatDensityInput(sectorIndex);
            var meatProximityInput = NeuralBrainSchema.VisionSectorMeatProximityInput(sectorIndex);

            Set(weights, NeuralBrainSchema.TurnOutput, plantDensityInput, side * 2.0f);
            Set(weights, NeuralBrainSchema.TurnOutput, plantProximityInput, side * 3.2f);
            Set(weights, NeuralBrainSchema.TurnOutput, meatDensityInput, side * 0.8f);
            Set(weights, NeuralBrainSchema.TurnOutput, meatProximityInput, side * 1.2f);

            Set(weights, NeuralBrainSchema.MoveForwardOutput, plantDensityInput, centerBias * 0.35f);
            Set(weights, NeuralBrainSchema.MoveForwardOutput, plantProximityInput, centerBias * 1.1f);
            Set(weights, NeuralBrainSchema.MoveForwardOutput, meatDensityInput, centerBias * 0.1f);
            Set(weights, NeuralBrainSchema.MoveForwardOutput, meatProximityInput, centerBias * 0.35f);
        }

        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.BiasInput, -2f);
        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.ReproductionReadinessInput, 2.75f);
        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.EnergySurplusInput, 0.75f);
        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.RecentFoodSuccessInput, 0.35f);
        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.VisibleCreatureDensityInput, -1.2f);

        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.BiasInput, -4f);

        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.ForwardObstacleInput, 0.8f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.LeftObstacleInput, 1.1f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.RightObstacleInput, -1.1f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.MovementBlockedInput, 0.7f);

        return weights;
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
        Set(weights, NeuralBrainSchema.EatOutput, NeuralBrainSchema.PlantFoodContactInput, 5.0f);
        Set(weights, NeuralBrainSchema.EatOutput, NeuralBrainSchema.MeatFoodContactInput, 1.0f);

        SeedSectorPlantForagingWeights(weights);

        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.BiasInput, -2f);
        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.ReproductionReadinessInput, 2.75f);
        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.EnergySurplusInput, 0.75f);
        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.RecentFoodSuccessInput, 0.35f);
        Set(weights, NeuralBrainSchema.ReproduceOutput, NeuralBrainSchema.VisibleCreatureDensityInput, -1.2f);

        Set(weights, NeuralBrainSchema.AttackOutput, NeuralBrainSchema.BiasInput, -4f);

        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.ForwardObstacleInput, -1.1f);
        Set(weights, NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.MovementBlockedInput, -1.5f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.ForwardObstacleInput, 0.8f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.LeftObstacleInput, 1.1f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.RightObstacleInput, -1.1f);
        Set(weights, NeuralBrainSchema.TurnOutput, NeuralBrainSchema.MovementBlockedInput, 0.7f);

        SeedHiddenConceptInputs(weights, hiddenNodeCount);

        return weights;
    }

    private static void SeedScavengerForagerWeights(float[] weights)
    {
        SeedSectorMeatForagingWeights(weights);

        Set(weights, NeuralBrainSchema.EatOutput, NeuralBrainSchema.MeatFoodContactInput, 5.0f);
        Set(weights, NeuralBrainSchema.EatOutput, NeuralBrainSchema.MeatProximityInput, 4.0f);

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

    private static void SeedSectorPlantForagingWeights(float[] weights)
    {
        for (var sectorIndex = 0; sectorIndex < VisionSectorSet.SectorCount; sectorIndex++)
        {
            var side = (sectorIndex - VisionSectorSet.CenterSectorIndex) / (float)VisionSectorSet.CenterSectorIndex;
            var centerBias = 1f - Math.Abs(side);
            var plantDensityInput = NeuralBrainSchema.VisionSectorPlantDensityInput(sectorIndex);
            var plantProximityInput = NeuralBrainSchema.VisionSectorPlantProximityInput(sectorIndex);

            Set(weights, NeuralBrainSchema.TurnOutput, plantDensityInput, side * 1.4f);
            Set(weights, NeuralBrainSchema.TurnOutput, plantProximityInput, side * 2.4f);
            Set(weights, NeuralBrainSchema.MoveForwardOutput, plantDensityInput, centerBias * 0.2f);
            Set(weights, NeuralBrainSchema.MoveForwardOutput, plantProximityInput, centerBias * 0.75f);
        }
    }

    private static void SeedSectorMeatForagingWeights(float[] weights)
    {
        for (var sectorIndex = 0; sectorIndex < VisionSectorSet.SectorCount; sectorIndex++)
        {
            var side = (sectorIndex - VisionSectorSet.CenterSectorIndex) / (float)VisionSectorSet.CenterSectorIndex;
            var centerBias = 1f - Math.Abs(side);
            var meatDensityInput = NeuralBrainSchema.VisionSectorMeatDensityInput(sectorIndex);
            var meatProximityInput = NeuralBrainSchema.VisionSectorMeatProximityInput(sectorIndex);

            Set(weights, NeuralBrainSchema.TurnOutput, meatDensityInput, side * 1.2f);
            Set(weights, NeuralBrainSchema.TurnOutput, meatProximityInput, side * 2.1f);
            Set(weights, NeuralBrainSchema.MoveForwardOutput, meatDensityInput, 0.15f);
            Set(weights, NeuralBrainSchema.MoveForwardOutput, meatProximityInput, 0.25f + centerBias * 0.45f);
        }
    }

    private static void SeedSectorCreatureHuntingWeights(float[] weights)
    {
        for (var sectorIndex = 0; sectorIndex < VisionSectorSet.SectorCount; sectorIndex++)
        {
            var side = (sectorIndex - VisionSectorSet.CenterSectorIndex) / (float)VisionSectorSet.CenterSectorIndex;
            var centerBias = 1f - Math.Abs(side);
            var smallerDensityInput = NeuralBrainSchema.VisionSectorSmallerCreatureDensityInput(sectorIndex);
            var smallerProximityInput = NeuralBrainSchema.VisionSectorSmallerCreatureProximityInput(sectorIndex);
            var similarDensityInput = NeuralBrainSchema.VisionSectorSimilarCreatureDensityInput(sectorIndex);
            var similarProximityInput = NeuralBrainSchema.VisionSectorSimilarCreatureProximityInput(sectorIndex);
            var largerDensityInput = NeuralBrainSchema.VisionSectorLargerCreatureDensityInput(sectorIndex);
            var largerProximityInput = NeuralBrainSchema.VisionSectorLargerCreatureProximityInput(sectorIndex);
            var approachInput = NeuralBrainSchema.VisionSectorCreatureApproachRateInput(sectorIndex);
            var facingInput = NeuralBrainSchema.VisionSectorCreatureFacingAlignmentInput(sectorIndex);

            Set(weights, NeuralBrainSchema.TurnOutput, smallerDensityInput, side * 1.4f);
            Set(weights, NeuralBrainSchema.TurnOutput, smallerProximityInput, side * 2.8f);
            Set(weights, NeuralBrainSchema.TurnOutput, similarDensityInput, side * 0.8f);
            Set(weights, NeuralBrainSchema.TurnOutput, similarProximityInput, side * 1.4f);
            Set(weights, NeuralBrainSchema.TurnOutput, largerDensityInput, side * 0.4f);
            Set(weights, NeuralBrainSchema.TurnOutput, largerProximityInput, side * 0.7f);

            Set(weights, NeuralBrainSchema.MoveForwardOutput, smallerDensityInput, centerBias * 0.2f);
            Set(weights, NeuralBrainSchema.MoveForwardOutput, smallerProximityInput, centerBias * 0.9f);
            Set(weights, NeuralBrainSchema.MoveForwardOutput, similarProximityInput, centerBias * 0.35f);
            Set(weights, NeuralBrainSchema.MoveForwardOutput, largerProximityInput, -centerBias * 0.45f);

            var smallerAttackBias = 0.65f + centerBias * 0.35f;
            Set(weights, NeuralBrainSchema.AttackOutput, smallerDensityInput, smallerAttackBias * 1.6f);
            Set(weights, NeuralBrainSchema.AttackOutput, smallerProximityInput, smallerAttackBias * 4.2f);
            Set(weights, NeuralBrainSchema.AttackOutput, similarProximityInput, centerBias * 1.2f);
            Set(weights, NeuralBrainSchema.AttackOutput, largerProximityInput, -centerBias * 2.2f);
            Set(weights, NeuralBrainSchema.AttackOutput, approachInput, -centerBias * 0.8f);
            Set(weights, NeuralBrainSchema.AttackOutput, facingInput, -centerBias * 0.45f);
        }
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
            SetHiddenInput(weights, hiddenNodeCount, SeedCreatureCueHiddenNode, NeuralBrainSchema.CreatureContactInput, 1.0f);
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

        outputs.Clear();

        if (SparseDirectWeightIndices is { } sparseDirectWeightIndices)
        {
            foreach (var weightIndex in sparseDirectWeightIndices)
            {
                var output = weightIndex / NeuralBrainSchema.InputCount;
                var input = weightIndex - output * NeuralBrainSchema.InputCount;
                outputs[output] += Weights[weightIndex] * inputs[input];
            }
        }
        else
        {
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
        }

        if (HasActiveHiddenOutputs)
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

    private static bool HasNonZeroHiddenOutputWeights(float[] weights, int hiddenNodeCount)
    {
        if (hiddenNodeCount <= 0)
        {
            return false;
        }

        var offset = DirectWeightCount + hiddenNodeCount * NeuralBrainSchema.InputCount;
        var count = hiddenNodeCount * NeuralBrainSchema.OutputCount;
        for (var i = 0; i < count; i++)
        {
            if (weights[offset + i] != 0f)
            {
                return true;
            }
        }

        return false;
    }

    private static int[]? CreateSparseDirectWeightIndex(float[] weights)
    {
        var activeCount = 0;
        for (var i = 0; i < DirectWeightCount; i++)
        {
            if (weights[i] != 0f)
            {
                activeCount++;
            }
        }

        if (activeCount > DirectWeightCount * SparseDirectWeightThreshold)
        {
            return null;
        }

        var activeIndices = new int[activeCount];
        var activeIndex = 0;
        for (var i = 0; i < DirectWeightCount; i++)
        {
            if (weights[i] != 0f)
            {
                activeIndices[activeIndex++] = i;
            }
        }

        return activeIndices;
    }

    public NeuralBrainGenome Mutated(DeterministicRandom random, float mutationStrength)
    {
        return Mutated(random, mutationStrength, mutationRate: 1f);
    }

    public NeuralBrainGenome Mutated(DeterministicRandom random, float mutationStrength, float mutationRate)
    {
        var strength = ValidateMutationParameters(mutationStrength, mutationRate);
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

    public NeuralBrainGenome MutatedHiddenLayer(
        DeterministicRandom random,
        float mutationStrength,
        float mutationRate)
    {
        ArgumentNullException.ThrowIfNull(random);
        ValidateHiddenLayerNodeCount(HiddenNodeCount);
        var strength = ValidateMutationParameters(mutationStrength, mutationRate);
        var weights = new float[Weights.Length];
        Array.Copy(Weights, weights, Weights.Length);
        Array.Clear(weights, 0, DirectWeightCount);

        var mutatedAny = false;
        for (var i = DirectWeightCount; i < weights.Length; i++)
        {
            var shouldMutate = strength > 0f && mutationRate > 0f && random.NextSingle() < mutationRate;
            mutatedAny |= shouldMutate;
            if (shouldMutate)
            {
                weights[i] = Math.Clamp(weights[i] + random.NextSingle(-strength, strength), -WeightLimit, WeightLimit);
            }
        }

        if (!mutatedAny && strength > 0f && mutationRate > 0f && weights.Length > DirectWeightCount)
        {
            var index = DirectWeightCount + random.NextInt32(weights.Length - DirectWeightCount);
            weights[index] = Math.Clamp(weights[index] + random.NextSingle(-strength, strength), -WeightLimit, WeightLimit);
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

    private static void SetHiddenOutput(
        float[] weights,
        int hiddenNodeCount,
        int outputIndex,
        int hiddenIndex,
        float value)
    {
        weights[GetHiddenOutputWeightIndex(hiddenNodeCount, outputIndex, hiddenIndex)] = value;
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

    private static void ValidateHiddenLayerNodeCount(int hiddenNodeCount)
    {
        if (hiddenNodeCount < NeuralBrainSchema.OutputCount || hiddenNodeCount > NeuralBrainSchema.MaxHiddenNodeCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hiddenNodeCount),
                $"Hidden-layer neural brains require between {NeuralBrainSchema.OutputCount} and {NeuralBrainSchema.MaxHiddenNodeCount} hidden nodes.");
        }
    }

    private static float ValidateMutationParameters(float mutationStrength, float mutationRate)
    {
        if (!float.IsFinite(mutationStrength) || mutationStrength < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(mutationStrength), "Mutation strength must be finite and non-negative.");
        }

        if (!float.IsFinite(mutationRate) || mutationRate < 0f || mutationRate > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(mutationRate), "Mutation rate must be finite and between 0 and 1.");
        }

        return Math.Clamp(mutationStrength, 0f, 1f);
    }

    private static (float[] Weights, int HiddenNodeCount) NormalizeWeights(float[] weights)
    {
        if (TryInferCurrentWeightLayout(weights.Length, out var hiddenNodeCount))
        {
            return (weights, hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutCreatureSimilarityScent,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutCreatureSimilarityScent,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutPlantPreferenceBridge,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutPlantPreferenceBridge,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutPlantPayoffTrace,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutPlantPayoffTrace,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutTypedPlantEnergyYield,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutTypedPlantEnergyYield,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutSectorPlantQuality,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutSectorPlantQuality,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutFoodEnergyYield,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutFoodEnergyYield,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutPlantQuality,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutPlantQuality,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutCreatureContact,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutCreatureContact,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutCreatureSectorMotion,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutCreatureSectorMotion,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutCreatureSectorSize,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutCreatureSectorSize,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutHealthRatio,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutHealthRatio,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutFoodContactKinds,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutFoodContactKinds,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutFoodContact,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutFoodContact,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutSectorVision,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutSectorVision,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
        }

        if (TryInferLegacyWeightLayout(
            weights.Length,
            LegacyInputCountWithoutObstacleSensing,
            NeuralBrainSchema.OutputCount,
            out hiddenNodeCount))
        {
            return (NormalizeLegacyWeights(
                weights,
                LegacyInputCountWithoutObstacleSensing,
                NeuralBrainSchema.OutputCount,
                oldEggReserveInput: NeuralBrainSchema.EggReserveRatioInput,
                oldReproductionReadinessInput: NeuralBrainSchema.ReproductionReadinessInput,
                hiddenNodeCount), hiddenNodeCount);
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
                var targetInput = MapLegacyInput(
                    input,
                    legacyInputCount,
                    oldEggReserveInput,
                    oldReproductionReadinessInput);

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
                    var targetInput = MapLegacyInput(
                        input,
                        legacyInputCount,
                        oldEggReserveInput,
                        oldReproductionReadinessInput);

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

    private static int MapLegacyInput(
        int input,
        int legacyInputCount,
        int oldEggReserveInput,
        int oldReproductionReadinessInput)
    {
        if (input == oldEggReserveInput)
        {
            return NeuralBrainSchema.EggReserveRatioInput;
        }

        if (input == oldReproductionReadinessInput)
        {
            return NeuralBrainSchema.ReproductionReadinessInput;
        }

        if (legacyInputCount == LegacyInputCountWithoutCreatureSimilarityScent
            || legacyInputCount == LegacyInputCountWithoutPlantPreferenceBridge
            || legacyInputCount == LegacyInputCountWithoutPlantPayoffTrace
            || legacyInputCount == LegacyInputCountWithoutTypedPlantEnergyYield)
        {
            return input;
        }

        if (legacyInputCount >= LegacyInputCountWithoutCreatureContact)
        {
            if (input >= NeuralBrainSchema.VisionSectorInputStart
                && input < LegacySectorPlantQualityFoodContactInput)
            {
                var sectorOffset = input - NeuralBrainSchema.VisionSectorInputStart;
                var sectorIndex = sectorOffset / LegacyVisionSectorChannelCountWithoutPlantQuality;
                var channelOffset = sectorOffset % LegacyVisionSectorChannelCountWithoutPlantQuality;
                return NeuralBrainSchema.GetVisionSectorInput(sectorIndex, MapLegacySectorChannelOffset(channelOffset));
            }

            if (input >= LegacySectorPlantQualityFoodContactInput)
            {
                var trailingOffset = input - LegacySectorPlantQualityFoodContactInput;
                if (legacyInputCount == LegacyInputCountWithoutCreatureContact && trailingOffset == 4)
                {
                    return NeuralBrainSchema.HealthRatioInput;
                }

                return NeuralBrainSchema.FoodContactInput + trailingOffset;
            }
        }

        if (legacyInputCount >= LegacyInputCountWithoutCreatureSectorMotion)
        {
            if (input >= NeuralBrainSchema.VisionSectorInputStart
                && input < LegacyCreatureSectorMotionFoodContactInput)
            {
                var sectorOffset = input - NeuralBrainSchema.VisionSectorInputStart;
                var sectorIndex = sectorOffset / LegacyCreatureSectorSizeChannelCount;
                var channelOffset = sectorOffset % LegacyCreatureSectorSizeChannelCount;
                return NeuralBrainSchema.GetVisionSectorInput(sectorIndex, MapLegacySectorChannelOffset(channelOffset));
            }

            if (input == LegacyCreatureSectorMotionFoodContactInput)
            {
                return NeuralBrainSchema.FoodContactInput;
            }

            if (input == LegacyCreatureSectorMotionFoodContactInput + 1)
            {
                return NeuralBrainSchema.PlantFoodContactInput;
            }

            if (input == LegacyCreatureSectorMotionFoodContactInput + 2)
            {
                return NeuralBrainSchema.MeatFoodContactInput;
            }

            if (input == LegacyCreatureSectorMotionFoodContactInput + 3)
            {
                return NeuralBrainSchema.EggFoodContactInput;
            }

            if (input == LegacyCreatureSectorMotionFoodContactInput + 4)
            {
                return NeuralBrainSchema.HealthRatioInput;
            }
        }

        if (legacyInputCount >= LegacyInputCountWithoutFoodContact
            && input >= NeuralBrainSchema.VisionSectorInputStart
            && input < LegacyInputCountWithoutFoodContact)
        {
            var sectorOffset = input - NeuralBrainSchema.VisionSectorInputStart;
            var sectorIndex = sectorOffset / LegacyVisionSectorChannelCount;
            var channelOffset = sectorOffset % LegacyVisionSectorChannelCount;
            return NeuralBrainSchema.GetVisionSectorInput(sectorIndex, MapLegacySectorChannelOffset(channelOffset));
        }

        if (legacyInputCount > LegacyInputCountWithoutFoodContact
            && input == LegacyInputCountWithoutFoodContact)
        {
            return NeuralBrainSchema.FoodContactInput;
        }

        if (legacyInputCount > LegacyInputCountWithoutFoodContactKinds
            && input == LegacyInputCountWithoutFoodContactKinds)
        {
            return NeuralBrainSchema.PlantFoodContactInput;
        }

        if (legacyInputCount > LegacyInputCountWithoutFoodContactKinds + 1
            && input == LegacyInputCountWithoutFoodContactKinds + 1)
        {
            return NeuralBrainSchema.MeatFoodContactInput;
        }

        if (legacyInputCount > LegacyInputCountWithoutFoodContactKinds + 2
            && input == LegacyInputCountWithoutFoodContactKinds + 2)
        {
            return NeuralBrainSchema.EggFoodContactInput;
        }

        if (legacyInputCount > LegacyInputCountWithoutHealthRatio
            && input == LegacyInputCountWithoutHealthRatio)
        {
            return NeuralBrainSchema.HealthRatioInput;
        }

        return input;
    }

    private static int MapLegacySectorChannelOffset(int channelOffset)
    {
        return channelOffset <= NeuralBrainSchema.VisionSectorPlantProximityOffset
            ? channelOffset
            : channelOffset + 2;
    }
}
