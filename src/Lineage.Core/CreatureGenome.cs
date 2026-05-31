namespace Lineage.Core;

/// <summary>
/// Heritable creature traits used by the first life-loop systems.
/// </summary>
///
/// <remarks>
/// This is intentionally small. Later phases can split body, senses, metabolism,
/// reproduction, and brain genes once the core ecology is stable enough to justify it.
/// </remarks>
public readonly record struct CreatureGenome
{
    public static CreatureGenome Baseline => new()
    {
        BodyRadius = 3f,
        MaxSpeed = 24f,
        MaxTurnRadiansPerSecond = 3f,
        SenseRadius = 90f,
        VisionAngleRadians = MathF.PI * 2f / 3f,
        BasalEnergyPerSecond = 0.25f,
        MovementEnergyPerSecond = 0.35f,
        EatCaloriesPerSecond = 18f,
        ReproductionEnergyThreshold = 70f,
        OffspringEnergyInvestment = 28f,
        EggProductionEnergyPerSecond = 3f,
        EggIncubationSeconds = 18f,
        MaturityAgeSeconds = 45f,
        ReproductionCooldownSeconds = 8f,
        DietaryAdaptation = 0.1f,
        CarrionAdaptation = 0f,
        TenderPlantAdaptation = 0f,
        RichPlantAdaptation = 0f,
        ToughPlantAdaptation = 0f,
        GutCapacityCalories = 55f,
        DigestionCaloriesPerSecond = 5f,
        FatStorageCapacityCalories = 42f,
        FatStorageEfficiency = 0.85f,
        BiteStrength = 0.55f,
        DamageResistance = 1f,
        MutationStrength = 0.05f,
        TraitMutationRate = 0.2f,
        BrainMutationRate = 0.08f
    };

    private const int MutatingTraitCount = 25;

    public float BodyRadius { get; init; }

    public float MaxSpeed { get; init; }

    public float MaxTurnRadiansPerSecond { get; init; }

    public float SenseRadius { get; init; }

    public float VisionAngleRadians { get; init; }

    public float BasalEnergyPerSecond { get; init; }

    public float MovementEnergyPerSecond { get; init; }

    public float EatCaloriesPerSecond { get; init; }

    public float ReproductionEnergyThreshold { get; init; }

    public float OffspringEnergyInvestment { get; init; }

    public float EggProductionEnergyPerSecond { get; init; }

    public float EggIncubationSeconds { get; init; }

    public float MaturityAgeSeconds { get; init; }

    public float ReproductionCooldownSeconds { get; init; }

    public float DietaryAdaptation { get; init; }

    public float CarrionAdaptation { get; init; }

    public float TenderPlantAdaptation { get; init; }

    public float RichPlantAdaptation { get; init; }

    public float ToughPlantAdaptation { get; init; }

    public float GutCapacityCalories { get; init; }

    public float DigestionCaloriesPerSecond { get; init; }

    public float FatStorageCapacityCalories { get; init; }

    public float FatStorageEfficiency { get; init; }

    public float BiteStrength { get; init; }

    public float DamageResistance { get; init; }

    /// <summary>
    /// Legacy snapshot/catalog field. Active mutation pressure is supplied by the world.
    /// </summary>
    public float MutationStrength { get; init; }

    /// <summary>
    /// Legacy snapshot/catalog field. Active mutation pressure is supplied by the world.
    /// </summary>
    public float TraitMutationRate { get; init; }

    /// <summary>
    /// Legacy snapshot/catalog field. Active mutation pressure is supplied by the world.
    /// </summary>
    public float BrainMutationRate { get; init; }

    public CreatureGenome Mutated(DeterministicRandom random)
    {
        return Mutated(random, MutationProfile.FromLegacyGenome(this));
    }

    public CreatureGenome Mutated(DeterministicRandom random, MutationProfile mutationProfile)
    {
        mutationProfile = mutationProfile.Validated();
        var strength = Math.Clamp(mutationProfile.MutationStrength, 0f, 0.5f);
        var traitMutationRate = Math.Clamp(mutationProfile.TraitMutationRate, 0f, 1f);
        var mutations = CreateMutationMask(random, MutatingTraitCount, traitMutationRate, strength);

        var mutated = this with
        {
            BodyRadius = MutateTraitIfSelected(random, mutations[0], BodyRadius, strength, 1f, 12f),
            MaxSpeed = MutateTraitIfSelected(random, mutations[1], MaxSpeed, strength, 2f, 80f),
            MaxTurnRadiansPerSecond = MutateTraitIfSelected(random, mutations[2], MaxTurnRadiansPerSecond, strength, 0.1f, 12f),
            SenseRadius = MutateTraitIfSelected(random, mutations[3], SenseRadius, strength, 5f, 300f),
            VisionAngleRadians = MutateTraitIfSelected(random, mutations[4], VisionAngleRadians, strength, MathF.PI / 12f, MathF.Tau),
            BasalEnergyPerSecond = MutateTraitIfSelected(random, mutations[5], BasalEnergyPerSecond, strength, 0.01f, 5f),
            MovementEnergyPerSecond = MutateTraitIfSelected(random, mutations[6], MovementEnergyPerSecond, strength, 0.01f, 5f),
            EatCaloriesPerSecond = MutateTraitIfSelected(random, mutations[7], EatCaloriesPerSecond, strength, 1f, 100f),
            OffspringEnergyInvestment = MutateTraitIfSelected(random, mutations[8], OffspringEnergyInvestment, strength, 5f, 200f),
            EggProductionEnergyPerSecond = MutateTraitIfSelected(random, mutations[9], EggProductionEnergyPerSecond, strength, 0.25f, 30f),
            EggIncubationSeconds = MutateTraitIfSelected(random, mutations[10], EggIncubationSeconds, strength, 1f, 300f),
            MaturityAgeSeconds = MutateTraitIfSelected(random, mutations[11], MaturityAgeSeconds, strength, 10f, 600f),
            ReproductionCooldownSeconds = MutateTraitIfSelected(random, mutations[12], ReproductionCooldownSeconds, strength, 1f, 60f),
            DietaryAdaptation = MutateUnitIntervalTraitIfSelected(random, mutations[13], DietaryAdaptation, strength * 0.5f),
            CarrionAdaptation = MutateUnitIntervalTraitIfSelected(random, mutations[14], CarrionAdaptation, strength),
            TenderPlantAdaptation = MutateUnitIntervalTraitIfSelected(random, mutations[15], TenderPlantAdaptation, strength),
            RichPlantAdaptation = MutateUnitIntervalTraitIfSelected(random, mutations[16], RichPlantAdaptation, strength),
            ToughPlantAdaptation = MutateUnitIntervalTraitIfSelected(random, mutations[17], ToughPlantAdaptation, strength),
            GutCapacityCalories = MutateTraitIfSelected(random, mutations[18], GutCapacityCalories, strength, 5f, 250f),
            DigestionCaloriesPerSecond = MutateTraitIfSelected(random, mutations[19], DigestionCaloriesPerSecond, strength, 1f, 60f),
            BiteStrength = MutateTraitIfSelected(random, mutations[20], BiteStrength, strength, 0.05f, 4f),
            DamageResistance = MutateTraitIfSelected(random, mutations[21], DamageResistance, strength, 0.25f, 4f),
            FatStorageCapacityCalories = MutateTraitIfSelected(random, mutations[23], FatStorageCapacityCalories, strength, 0f, 250f),
            FatStorageEfficiency = MutateTraitIfSelected(random, mutations[24], FatStorageEfficiency, strength, 0.35f, 0.98f),
            MutationStrength = mutationProfile.MutationStrength,
            TraitMutationRate = mutationProfile.TraitMutationRate,
            BrainMutationRate = mutationProfile.BrainMutationRate
        };

        var minimumThreshold = mutated.OffspringEnergyInvestment + 1f;
        mutated = mutated with
        {
            ReproductionEnergyThreshold = MutateTraitIfSelected(
                random,
                mutations[22],
                Math.Max(ReproductionEnergyThreshold, minimumThreshold),
                strength,
                minimumThreshold,
                500f)
        };

        return mutated.Validated();
    }

    public CreatureGenome Validated()
    {
        var normalized = this;
        if (normalized.FatStorageCapacityCalories <= 0f || !float.IsFinite(normalized.FatStorageCapacityCalories))
        {
            normalized = normalized with { FatStorageCapacityCalories = Baseline.FatStorageCapacityCalories };
        }

        if (normalized.FatStorageEfficiency <= 0f || !float.IsFinite(normalized.FatStorageEfficiency))
        {
            normalized = normalized with { FatStorageEfficiency = Baseline.FatStorageEfficiency };
        }

        EnsurePositive(normalized.BodyRadius, nameof(BodyRadius));
        EnsurePositive(normalized.MaxSpeed, nameof(MaxSpeed));
        EnsurePositive(normalized.MaxTurnRadiansPerSecond, nameof(MaxTurnRadiansPerSecond));
        EnsurePositive(normalized.SenseRadius, nameof(SenseRadius));
        EnsureRange(normalized.VisionAngleRadians, MathF.PI / 12f, MathF.Tau, nameof(VisionAngleRadians));
        EnsureNonNegative(normalized.BasalEnergyPerSecond, nameof(BasalEnergyPerSecond));
        EnsureNonNegative(normalized.MovementEnergyPerSecond, nameof(MovementEnergyPerSecond));
        EnsurePositive(normalized.EatCaloriesPerSecond, nameof(EatCaloriesPerSecond));
        EnsurePositive(normalized.OffspringEnergyInvestment, nameof(OffspringEnergyInvestment));
        EnsurePositive(normalized.EggProductionEnergyPerSecond, nameof(EggProductionEnergyPerSecond));
        EnsureNonNegative(normalized.EggIncubationSeconds, nameof(EggIncubationSeconds));
        EnsurePositive(normalized.ReproductionEnergyThreshold, nameof(ReproductionEnergyThreshold));
        EnsureNonNegative(normalized.MaturityAgeSeconds, nameof(MaturityAgeSeconds));
        EnsureNonNegative(normalized.ReproductionCooldownSeconds, nameof(ReproductionCooldownSeconds));
        EnsureProbability(normalized.DietaryAdaptation, nameof(DietaryAdaptation));
        EnsureProbability(normalized.CarrionAdaptation, nameof(CarrionAdaptation));
        EnsureProbability(normalized.TenderPlantAdaptation, nameof(TenderPlantAdaptation));
        EnsureProbability(normalized.RichPlantAdaptation, nameof(RichPlantAdaptation));
        EnsureProbability(normalized.ToughPlantAdaptation, nameof(ToughPlantAdaptation));
        EnsurePositive(normalized.GutCapacityCalories, nameof(GutCapacityCalories));
        EnsurePositive(normalized.DigestionCaloriesPerSecond, nameof(DigestionCaloriesPerSecond));
        EnsureNonNegative(normalized.FatStorageCapacityCalories, nameof(FatStorageCapacityCalories));
        EnsureRange(normalized.FatStorageEfficiency, 0.05f, 1f, nameof(FatStorageEfficiency));
        EnsurePositive(normalized.BiteStrength, nameof(BiteStrength));
        EnsurePositive(normalized.DamageResistance, nameof(DamageResistance));
        EnsureNonNegative(normalized.MutationStrength, nameof(MutationStrength));
        EnsureProbability(normalized.TraitMutationRate, nameof(TraitMutationRate));
        EnsureProbability(normalized.BrainMutationRate, nameof(BrainMutationRate));

        if (normalized.ReproductionEnergyThreshold < normalized.OffspringEnergyInvestment)
        {
            throw new InvalidOperationException("Reproduction threshold must be at least the offspring investment.");
        }

        return normalized;
    }

    private static bool[] CreateMutationMask(
        DeterministicRandom random,
        int traitCount,
        float mutationRate,
        float mutationStrength)
    {
        var mutations = new bool[traitCount];
        if (mutationRate <= 0f || mutationStrength <= 0f)
        {
            return mutations;
        }

        var mutationCount = 0;
        for (var i = 0; i < mutations.Length; i++)
        {
            mutations[i] = random.NextSingle() < mutationRate;
            if (mutations[i])
            {
                mutationCount++;
            }
        }

        if (mutationCount == 0)
        {
            mutations[random.NextInt32(mutations.Length)] = true;
        }

        return mutations;
    }

    private static float MutateTraitIfSelected(
        DeterministicRandom random,
        bool shouldMutate,
        float value,
        float strength,
        float inclusiveMin,
        float inclusiveMax)
    {
        return shouldMutate
            ? MutateTrait(random, value, strength, inclusiveMin, inclusiveMax)
            : Math.Clamp(value, inclusiveMin, inclusiveMax);
    }

    private static float MutateUnitIntervalTraitIfSelected(
        DeterministicRandom random,
        bool shouldMutate,
        float value,
        float strength)
    {
        return shouldMutate
            ? Math.Clamp(value + random.NextSingle(-strength, strength), 0f, 1f)
            : Math.Clamp(value, 0f, 1f);
    }

    private static float MutateTrait(
        DeterministicRandom random,
        float value,
        float strength,
        float inclusiveMin,
        float inclusiveMax)
    {
        var signed = random.NextSingle(-1f, 1f);
        var mutated = value * (1f + signed * strength);
        return Math.Clamp(mutated, inclusiveMin, inclusiveMax);
    }

    private static void EnsurePositive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0f)
        {
            throw new InvalidOperationException($"{name} must be finite and positive.");
        }
    }

    private static void EnsureNonNegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f)
        {
            throw new InvalidOperationException($"{name} must be finite and non-negative.");
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
