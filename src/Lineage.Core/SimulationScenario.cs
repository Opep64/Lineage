namespace Lineage.Core;

/// <summary>
/// Reproducible setup parameters for a simulation run.
/// </summary>
///
/// <remarks>
/// Scenarios keep world construction out of viewers and CLI tools. The first version
/// is deliberately compact: one seeded forager lineage, calorie resources, and the
/// selectable core pipeline.
/// </remarks>
public sealed record SimulationScenario
{
    public const ulong DefaultSeed = 20260519UL;
    public const float ResourceDensityAreaUnits = 1_000_000f;

    public string Name { get; init; } = "Balanced Foraging";

    public ulong Seed { get; init; } = DefaultSeed;

    public SimulationPipelineKind PipelineKind { get; init; } = SimulationPipelineKind.Neural;

    public bool RandomizeInitialBrainWeights { get; init; }

    public bool EnableBiomes { get; init; } = true;

    public float WorldWidth { get; init; } = 2_000f;

    public float WorldHeight { get; init; } = 2_000f;

    public float BiomeCellSize { get; init; } = 500f;

    public float ResourceVoidBorderWidth { get; init; } = 80f;

    public float FixedDeltaSeconds { get; init; } = 1f / 30f;

    public float SpatialCellSize { get; init; } = 48f;

    public int StatsSnapshotIntervalTicks { get; init; } = 10;

    public int InitialCreatureCount { get; init; } = 80;

    public float InitialResourcesPerMillionArea { get; init; } = 165f;

    public float InitialCreatureEnergyMin { get; init; } = 25f;

    public float InitialCreatureEnergyMax { get; init; } = 55f;

    public float ResourceRadiusMin { get; init; } = 3f;

    public float ResourceRadiusMax { get; init; } = 8f;

    public float ResourceCaloriesMin { get; init; } = 45f;

    public float ResourceCaloriesMax { get; init; } = 100f;

    public float ResourceMaxCalories { get; init; } = 100f;

    public float ResourceRegrowthMin { get; init; } = 0.35f;

    public float ResourceRegrowthMax { get; init; } = 1.8f;

    public bool RelocateDepletedResources { get; init; } = true;

    public float BasalEnergyPerSecond { get; init; } = 0.25f;

    public float BodyRadiusEnergyCostPerSecond { get; init; } = 0.04f;

    public float MaxSpeedEnergyCostPerSecond { get; init; } = 0.006f;

    public float TurnRateEnergyCostPerSecond { get; init; } = 0.03f;

    public float SenseRadiusEnergyCostPerSecond { get; init; } = 0.0008f;

    public float VisionAngleRadians { get; init; } = MathF.PI * 2f / 3f;

    public float VisionAngleEnergyCostPerSecond { get; init; } = 0.02f;

    public float EatRateEnergyCostPerSecond { get; init; } = 0.006f;

    public float EggEnergyCostPerSecond { get; init; } = 0f;

    public float EggEnvironmentalDamagePerSecond { get; init; } = 0.08f;

    public float MovementEnergyPerSecond { get; init; } = 0.35f;

    public float EatCaloriesPerSecond { get; init; } = 18f;

    public float ReproductionEnergyThreshold { get; init; } = 84f;

    public float OffspringEnergyInvestment { get; init; } = 28f;

    public float EggProductionEnergyPerSecond { get; init; } = 3f;

    public float EggIncubationSeconds { get; init; } = 18f;

    public float MaturityAgeSeconds { get; init; } = 60f;

    public float ReproductionCooldownSeconds { get; init; } = 7f;

    public float DietaryAdaptation { get; init; } = 0.1f;

    public float DeathMeatCaloriesPerBodyRadius { get; init; } = 4f;

    public float DeathMeatEnergyFraction { get; init; } = 0.35f;

    public float MeatDecayCaloriesPerSecond { get; init; } = 0.03f;

    public float BiteDamagePerSecond { get; init; } = 0.25f;

    public float BiteEnergyCostPerSecond { get; init; } = 0.12f;

    public float BiteRangePadding { get; init; } = 1f;

    public float MutationStrength { get; init; } = 0.06f;

    public float TraitMutationRate { get; init; } = 0.2f;

    public float BrainMutationRate { get; init; } = 0.08f;

    public SimulationScenario Validated()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Scenario name cannot be empty.");
        }

        EnsurePositive(WorldWidth, nameof(WorldWidth));
        EnsurePositive(WorldHeight, nameof(WorldHeight));
        EnsurePositive(BiomeCellSize, nameof(BiomeCellSize));
        EnsureNonNegative(ResourceVoidBorderWidth, nameof(ResourceVoidBorderWidth));
        EnsurePositive(FixedDeltaSeconds, nameof(FixedDeltaSeconds));
        EnsurePositive(SpatialCellSize, nameof(SpatialCellSize));
        EnsurePositive(StatsSnapshotIntervalTicks, nameof(StatsSnapshotIntervalTicks));
        EnsureNonNegative(InitialCreatureCount, nameof(InitialCreatureCount));
        EnsureNonNegative(InitialResourcesPerMillionArea, nameof(InitialResourcesPerMillionArea));
        EnsureRange(InitialCreatureEnergyMin, InitialCreatureEnergyMax, nameof(InitialCreatureEnergyMin), nameof(InitialCreatureEnergyMax));
        EnsureRange(ResourceRadiusMin, ResourceRadiusMax, nameof(ResourceRadiusMin), nameof(ResourceRadiusMax));
        EnsureRange(ResourceCaloriesMin, ResourceCaloriesMax, nameof(ResourceCaloriesMin), nameof(ResourceCaloriesMax));
        EnsurePositive(ResourceMaxCalories, nameof(ResourceMaxCalories));
        EnsureRange(ResourceRegrowthMin, ResourceRegrowthMax, nameof(ResourceRegrowthMin), nameof(ResourceRegrowthMax));
        EnsureNonNegative(BasalEnergyPerSecond, nameof(BasalEnergyPerSecond));
        EnsureNonNegative(BodyRadiusEnergyCostPerSecond, nameof(BodyRadiusEnergyCostPerSecond));
        EnsureNonNegative(MaxSpeedEnergyCostPerSecond, nameof(MaxSpeedEnergyCostPerSecond));
        EnsureNonNegative(TurnRateEnergyCostPerSecond, nameof(TurnRateEnergyCostPerSecond));
        EnsureNonNegative(SenseRadiusEnergyCostPerSecond, nameof(SenseRadiusEnergyCostPerSecond));
        EnsureRange(VisionAngleRadians, MathF.PI / 12f, MathF.Tau, nameof(VisionAngleRadians));
        EnsureNonNegative(VisionAngleEnergyCostPerSecond, nameof(VisionAngleEnergyCostPerSecond));
        EnsureNonNegative(EatRateEnergyCostPerSecond, nameof(EatRateEnergyCostPerSecond));
        EnsureNonNegative(EggEnergyCostPerSecond, nameof(EggEnergyCostPerSecond));
        EnsureNonNegative(EggEnvironmentalDamagePerSecond, nameof(EggEnvironmentalDamagePerSecond));
        EnsureNonNegative(MovementEnergyPerSecond, nameof(MovementEnergyPerSecond));
        EnsurePositive(EatCaloriesPerSecond, nameof(EatCaloriesPerSecond));
        EnsurePositive(ReproductionEnergyThreshold, nameof(ReproductionEnergyThreshold));
        EnsurePositive(OffspringEnergyInvestment, nameof(OffspringEnergyInvestment));
        EnsurePositive(EggProductionEnergyPerSecond, nameof(EggProductionEnergyPerSecond));
        EnsureNonNegative(EggIncubationSeconds, nameof(EggIncubationSeconds));
        EnsureNonNegative(MaturityAgeSeconds, nameof(MaturityAgeSeconds));
        EnsureNonNegative(ReproductionCooldownSeconds, nameof(ReproductionCooldownSeconds));
        EnsureProbability(DietaryAdaptation, nameof(DietaryAdaptation));
        EnsureNonNegative(DeathMeatCaloriesPerBodyRadius, nameof(DeathMeatCaloriesPerBodyRadius));
        EnsureProbability(DeathMeatEnergyFraction, nameof(DeathMeatEnergyFraction));
        EnsureNonNegative(MeatDecayCaloriesPerSecond, nameof(MeatDecayCaloriesPerSecond));
        EnsureNonNegative(BiteDamagePerSecond, nameof(BiteDamagePerSecond));
        EnsureNonNegative(BiteEnergyCostPerSecond, nameof(BiteEnergyCostPerSecond));
        EnsureNonNegative(BiteRangePadding, nameof(BiteRangePadding));
        EnsureNonNegative(MutationStrength, nameof(MutationStrength));
        EnsureProbability(TraitMutationRate, nameof(TraitMutationRate));
        EnsureProbability(BrainMutationRate, nameof(BrainMutationRate));

        if (ResourceCaloriesMax > ResourceMaxCalories)
        {
            throw new InvalidOperationException("Resource calories max cannot exceed resource max calories.");
        }

        if (ResourceVoidBorderWidth * 2f >= Math.Min(WorldWidth, WorldHeight))
        {
            throw new InvalidOperationException("Resource void border width must leave a positive resource-spawn area.");
        }

        if (ReproductionEnergyThreshold < OffspringEnergyInvestment)
        {
            throw new InvalidOperationException("Reproduction threshold must be at least the offspring investment.");
        }

        return this;
    }

    public int CalculateInitialResourceCount()
    {
        return CalculateResourceCount(WorldWidth, WorldHeight, InitialResourcesPerMillionArea);
    }

    public static int CalculateResourceCount(float worldWidth, float worldHeight, float resourcesPerMillionArea)
    {
        EnsurePositive(worldWidth, nameof(worldWidth));
        EnsurePositive(worldHeight, nameof(worldHeight));
        EnsureNonNegative(resourcesPerMillionArea, nameof(resourcesPerMillionArea));

        var worldArea = worldWidth * worldHeight;
        return Math.Max(0, (int)MathF.Round(resourcesPerMillionArea * worldArea / ResourceDensityAreaUnits));
    }

    public static float CalculateResourcesPerMillionArea(int resourceCount, float worldWidth, float worldHeight)
    {
        EnsureNonNegative(resourceCount, nameof(resourceCount));
        EnsurePositive(worldWidth, nameof(worldWidth));
        EnsurePositive(worldHeight, nameof(worldHeight));

        return resourceCount / (worldWidth * worldHeight) * ResourceDensityAreaUnits;
    }

    private static void EnsurePositive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0f)
        {
            throw new InvalidOperationException($"{name} must be finite and positive.");
        }
    }

    private static void EnsurePositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException($"{name} must be positive.");
        }
    }

    private static void EnsureNonNegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f)
        {
            throw new InvalidOperationException($"{name} must be finite and non-negative.");
        }
    }

    private static void EnsureNonNegative(int value, string name)
    {
        if (value < 0)
        {
            throw new InvalidOperationException($"{name} must be non-negative.");
        }
    }

    private static void EnsureRange(float min, float max, string minName, string maxName)
    {
        EnsureNonNegative(min, minName);
        EnsurePositive(max, maxName);

        if (max < min)
        {
            throw new InvalidOperationException($"{maxName} must be greater than or equal to {minName}.");
        }
    }

    private static void EnsureRange(float value, float inclusiveMin, float inclusiveMax, string name)
    {
        if (!float.IsFinite(value) || value < inclusiveMin || value > inclusiveMax)
        {
            throw new InvalidOperationException($"{name} must be finite and between {inclusiveMin} and {inclusiveMax}.");
        }
    }

    private static void EnsureProbability(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f || value > 1f)
        {
            throw new InvalidOperationException($"{name} must be finite and between 0 and 1.");
        }
    }
}
