namespace Lineage.Core;

/// <summary>
/// Evaluates one creature brain against its current inputs, then against edited raw inputs.
/// </summary>
public sealed class BrainProbeService
{
    private const float MeaningfulOutputDelta = 0.01f;
    private const float ReproduceThreshold = 0.25f;
    private const float AttackThreshold = NeuralControllerSystem.DefaultAttackThreshold;
    private const float GrabThreshold = NeuralControllerSystem.DefaultGrabThreshold;
    private const float SoundEmissionThreshold = 0.05f;

    private static readonly IReadOnlyDictionary<string, BrainInputDefinition> InputByKey =
        BrainIoRegistry.Inputs.ToDictionary(input => input.Key, StringComparer.Ordinal);

    private delegate bool BrainProbeInputOverrideProvider(
        BrainInputDefinition input,
        float[] baselineInputs,
        out float overrideValue);

    public BrainProbeEvaluation Evaluate(
        WorldState state,
        EntityId creatureId,
        IReadOnlyDictionary<string, float>? inputOverrides = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!TryFindCreature(state, creatureId, out var creature))
        {
            throw new ArgumentException($"Creature {creatureId.Value} was not found.", nameof(creatureId));
        }

        var genome = state.GetGenome(creature.GenomeId);
        var brain = state.GetBrain(creature.BrainId);
        var overrides = NormalizeOverrides(inputOverrides);
        var provider = CreateFixedOverrideProvider(overrides);

        return EvaluateCreature(creature, genome, brain, overrides.Count, provider);
    }

    public BrainProbePopulationEvaluation EvaluatePopulation(
        WorldState state,
        IReadOnlyDictionary<string, float>? inputOverrides = null,
        int maxCreatures = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (maxCreatures <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCreatures), "Maximum creature count must be positive.");
        }

        var overrides = NormalizeOverrides(inputOverrides);
        return EvaluatePopulationCore(
            state,
            overrides.Count,
            CreateFixedOverrideProvider(overrides),
            maxCreatures);
    }

    public BrainProbePopulationEvaluation EvaluatePopulationPreset(
        WorldState state,
        BrainProbePresetKind presetKind,
        int maxCreatures = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (maxCreatures <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCreatures), "Maximum creature count must be positive.");
        }

        var provider = CreatePresetOverrideProvider(presetKind);
        return EvaluatePopulationCore(
            state,
            CountPresetOverrides(provider),
            provider,
            maxCreatures);
    }

    private BrainProbePopulationEvaluation EvaluatePopulationCore(
        WorldState state,
        int declaredOverrideCount,
        BrainProbeInputOverrideProvider overrideProvider,
        int maxCreatures)
    {
        var outputAccumulators = BrainIoRegistry.Outputs
            .Select(static output => new BrainProbePopulationOutputAccumulator(output))
            .ToArray();
        var totalCreatureCount = state.Creatures.Count;
        var creatureLimit = Math.Min(totalCreatureCount, maxCreatures);
        var changedCreatureCount = 0;
        var gateFlipCreatureCount = 0;
        var evaluatedCreatureCount = 0;
        var skippedCreatureCount = totalCreatureCount - creatureLimit;
        var maxAbsoluteOutputDelta = 0f;
        var unsupportedOverrideCount = 0;
        var architectureKinds = new HashSet<BrainArchitectureKind>();

        for (var i = 0; i < creatureLimit; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            var brain = state.GetBrain(creature.BrainId);
            architectureKinds.Add(brain.ArchitectureKind);

            if (declaredOverrideCount > 0 && brain.ArchitectureKind == BrainArchitectureKind.RtNeatGraph)
            {
                unsupportedOverrideCount++;
                skippedCreatureCount++;
                continue;
            }

            var evaluation = EvaluateCreature(
                creature,
                genome,
                brain,
                declaredOverrideCount,
                overrideProvider);
            var creatureChanged = false;
            var creatureGateFlipped = false;
            for (var outputIndex = 0; outputIndex < evaluation.Outputs.Count; outputIndex++)
            {
                var output = evaluation.Outputs[outputIndex];
                outputAccumulators[outputIndex].Add(output);
                creatureChanged |= output.Changed;
                creatureGateFlipped |= output.BaselineActive.HasValue
                    && output.ModifiedActive.HasValue
                    && output.BaselineActive.Value != output.ModifiedActive.Value;
                maxAbsoluteOutputDelta = Math.Max(maxAbsoluteOutputDelta, Math.Abs(output.Delta));
            }

            if (creatureChanged)
            {
                changedCreatureCount++;
            }

            if (creatureGateFlipped)
            {
                gateFlipCreatureCount++;
            }

            evaluatedCreatureCount++;
        }

        var architectureKind = architectureKinds.Count switch
        {
            0 => "None",
            1 => architectureKinds.Single().ToString(),
            _ => "Mixed"
        };

        return new BrainProbePopulationEvaluation(
            architectureKind,
            declaredOverrideCount == 0 || unsupportedOverrideCount == 0,
            declaredOverrideCount,
            totalCreatureCount,
            evaluatedCreatureCount,
            skippedCreatureCount,
            unsupportedOverrideCount,
            changedCreatureCount,
            evaluatedCreatureCount > 0 ? changedCreatureCount / (float)evaluatedCreatureCount : 0f,
            gateFlipCreatureCount,
            evaluatedCreatureCount > 0 ? gateFlipCreatureCount / (float)evaluatedCreatureCount : 0f,
            maxAbsoluteOutputDelta,
            outputAccumulators.Select(static accumulator => accumulator.ToValue()).ToArray());
    }

    private static BrainProbeEvaluation EvaluateCreature(
        CreatureState creature,
        CreatureGenome genome,
        BrainGenome brain,
        int declaredOverrideCount,
        BrainProbeInputOverrideProvider overrideProvider)
    {
        var inputFrame = BrainInputFrame.FromSenses(creature.Senses, genome);
        var memoryInputs = LegacyNeuralMemoryInputFrame.FromSenses(creature.Senses);
        var baselineInputs = new float[NeuralBrainSchema.InputCount];
        LegacyNeuralBrainAdapter.FillInputs(inputFrame, memoryInputs, baselineInputs);

        var modifiedInputs = baselineInputs.ToArray();
        var modifiedInputCount = 0;
        foreach (var input in BrainIoRegistry.Inputs)
        {
            if (!overrideProvider(input, baselineInputs, out var value))
            {
                continue;
            }

            modifiedInputs[input.FlatIndex] = Math.Clamp(value, input.MinimumValue, input.MaximumValue);
            modifiedInputCount++;
        }

        var baseline = EvaluateBrain(brain, inputFrame, memoryInputs, baselineInputs, declaredOverrideCount > 0);
        var modified = modifiedInputCount == 0
            ? baseline
            : EvaluateDenseBrain(brain, modifiedInputs);

        var inputs = BrainIoRegistry.Inputs
            .Select(input => new BrainProbeInputValue(
                input.Key,
                input.Name,
                input.FlatIndex,
                input.Group.ToString(),
                input.MinimumValue,
                input.MaximumValue,
                input.NeutralValue,
                baselineInputs[input.FlatIndex],
                modifiedInputs[input.FlatIndex],
                overrideProvider(input, baselineInputs, out _),
                input.Meaning))
            .ToArray();

        var outputs = BrainIoRegistry.Outputs
            .Select(output => CreateOutputValue(output, baseline, modified))
            .ToArray();

        var changedOutputCount = outputs.Count(output => output.Changed);
        var gateFlipCount = outputs.Count(output =>
            output.BaselineActive.HasValue
            && output.ModifiedActive.HasValue
            && output.BaselineActive.Value != output.ModifiedActive.Value);
        var maxAbsoluteOutputDelta = outputs.Length == 0
            ? 0f
            : outputs.Max(output => Math.Abs(output.Delta));

        return new BrainProbeEvaluation(
            new BrainProbeCreature(
                creature.Id.Value,
                creature.Generation,
                creature.AgeSeconds,
                creature.GenomeId,
                creature.BrainId,
                brain.ArchitectureKind.ToString(),
                creature.Senses.EnergyRatio,
                creature.Senses.HealthRatio,
                creature.Senses.Hunger,
                creature.Senses.SoundDetected,
                creature.Senses.SoundDensity,
                creature.Actions.SoundAmplitude),
            brain.ArchitectureKind.ToString(),
            brain.ArchitectureKind != BrainArchitectureKind.RtNeatGraph,
            modifiedInputCount,
            changedOutputCount,
            gateFlipCount,
            maxAbsoluteOutputDelta,
            inputs,
            outputs);
    }

    private static BrainEvaluationResult EvaluateBrain(
        BrainGenome brain,
        in BrainInputFrame inputFrame,
        in LegacyNeuralMemoryInputFrame memoryInputs,
        float[] denseInputs,
        bool hasOverrides)
    {
        if (brain.ArchitectureKind == BrainArchitectureKind.RtNeatGraph)
        {
            if (hasOverrides)
            {
                throw new NotSupportedException("Raw input overrides are currently supported for dense neural brains. rtNEAT support needs semantic frame editing.");
            }

            var scratchInputs = new float[NeuralBrainSchema.InputCount];
            var scratchOutputs = new float[NeuralBrainSchema.OutputCount];
            return brain.Evaluate(inputFrame, memoryInputs, scratchInputs, scratchOutputs);
        }

        return EvaluateDenseBrain(brain, denseInputs);
    }

    private static BrainEvaluationResult EvaluateDenseBrain(BrainGenome brain, float[] denseInputs)
    {
        if (brain.ArchitectureKind == BrainArchitectureKind.RtNeatGraph)
        {
            throw new NotSupportedException("Raw dense input evaluation is not available for rtNEAT graph brains.");
        }

        var outputs = new float[NeuralBrainSchema.OutputCount];
        brain.Evaluate(denseInputs, outputs);
        return new BrainEvaluationResult(
            LegacyNeuralBrainAdapter.ReadStandardOutputs(outputs),
            LegacyNeuralBrainAdapter.ReadMemoryOutputs(outputs));
    }

    private static Dictionary<string, float> NormalizeOverrides(IReadOnlyDictionary<string, float>? inputOverrides)
    {
        var overrides = new Dictionary<string, float>(StringComparer.Ordinal);
        if (inputOverrides is null)
        {
            return overrides;
        }

        foreach (var (key, value) in inputOverrides)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!InputByKey.TryGetValue(key, out var definition))
            {
                throw new ArgumentException($"Unknown brain input key '{key}'.");
            }

            if (!float.IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(nameof(inputOverrides), $"Override for '{key}' must be finite.");
            }

            overrides[definition.Key] = Math.Clamp(value, definition.MinimumValue, definition.MaximumValue);
        }

        return overrides;
    }

    private static BrainProbeInputOverrideProvider CreateFixedOverrideProvider(
        IReadOnlyDictionary<string, float> overrides)
    {
        return (BrainInputDefinition input, float[] _, out float overrideValue) =>
        {
            if (overrides.TryGetValue(input.Key, out overrideValue))
            {
                return true;
            }

            overrideValue = 0f;
            return false;
        };
    }

    private static BrainProbeInputOverrideProvider CreatePresetOverrideProvider(BrainProbePresetKind presetKind)
    {
        return presetKind switch
        {
            BrainProbePresetKind.MuteSound => NeutralizeGroup(BrainIoSignalGroup.Sound),
            BrainProbePresetKind.NoFood => Neutralize(IsFoodInput),
            BrainProbePresetKind.OnlyPlants => OnlyPlants,
            BrainProbePresetKind.OnlyMeatEggs => OnlyMeatEggs,
            BrainProbePresetKind.NoContact => NeutralizeGroup(BrainIoSignalGroup.Contact),
            BrainProbePresetKind.Hungry => FixedPreset(new Dictionary<string, float>(StringComparer.Ordinal)
            {
                ["internal.hunger"] = 1f,
                ["internal.energy_ratio"] = 0.2f,
                ["internal.energy_surplus"] = 0f,
                ["internal.fat_ratio"] = 0f,
                ["internal.mass_burden"] = 0f
            }),
            BrainProbePresetKind.Full => FixedPreset(new Dictionary<string, float>(StringComparer.Ordinal)
            {
                ["internal.hunger"] = 0f,
                ["internal.energy_ratio"] = 1f,
                ["internal.energy_surplus"] = 1f,
                ["internal.fat_ratio"] = 1f,
                ["internal.mass_burden"] = 1f
            }),
            BrainProbePresetKind.ReadyToReproduce => FixedPreset(new Dictionary<string, float>(StringComparer.Ordinal)
            {
                ["internal.reproduction_readiness"] = 1f,
                ["internal.egg_reserve_ratio"] = 1f,
                ["internal.energy_surplus"] = 1f,
                ["internal.health_ratio"] = 1f
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(presetKind), presetKind, "Unknown Brain Lab preset.")
        };
    }

    private static BrainProbeInputOverrideProvider NeutralizeGroup(BrainIoSignalGroup group)
    {
        return Neutralize(input => input.Group == group);
    }

    private static BrainProbeInputOverrideProvider Neutralize(Func<BrainInputDefinition, bool> predicate)
    {
        return (BrainInputDefinition input, float[] _, out float overrideValue) =>
        {
            if (predicate(input))
            {
                overrideValue = input.NeutralValue;
                return true;
            }

            overrideValue = 0f;
            return false;
        };
    }

    private static BrainProbeInputOverrideProvider FixedPreset(IReadOnlyDictionary<string, float> values)
    {
        return (BrainInputDefinition input, float[] _, out float overrideValue) =>
        {
            if (values.TryGetValue(input.Key, out overrideValue))
            {
                return true;
            }

            overrideValue = 0f;
            return false;
        };
    }

    private static bool OnlyPlants(BrainInputDefinition input, float[] baselineInputs, out float overrideValue)
    {
        if (IsMeatOrEggInput(input))
        {
            overrideValue = input.NeutralValue;
            return true;
        }

        if (string.Equals(input.Key, "vision.food_density", StringComparison.Ordinal))
        {
            overrideValue = BaselineValue("vision.plant_density", baselineInputs);
            return true;
        }

        if (string.Equals(input.Key, "contact.food", StringComparison.Ordinal))
        {
            overrideValue = BaselineValue("contact.plant_food", baselineInputs);
            return true;
        }

        overrideValue = 0f;
        return false;
    }

    private static bool OnlyMeatEggs(BrainInputDefinition input, float[] baselineInputs, out float overrideValue)
    {
        if (IsPlantInput(input))
        {
            overrideValue = input.NeutralValue;
            return true;
        }

        if (string.Equals(input.Key, "vision.food_density", StringComparison.Ordinal))
        {
            overrideValue = BaselineValue("vision.meat_density", baselineInputs);
            return true;
        }

        if (string.Equals(input.Key, "contact.food", StringComparison.Ordinal))
        {
            overrideValue = Math.Max(
                BaselineValue("contact.meat_food", baselineInputs),
                BaselineValue("contact.egg_food", baselineInputs));
            return true;
        }

        overrideValue = 0f;
        return false;
    }

    private static int CountPresetOverrides(BrainProbeInputOverrideProvider provider)
    {
        var baselineInputs = new float[NeuralBrainSchema.InputCount];
        return BrainIoRegistry.Inputs.Count(input => provider(input, baselineInputs, out _));
    }

    private static float BaselineValue(string key, float[] baselineInputs)
    {
        return InputByKey.TryGetValue(key, out var definition)
            ? baselineInputs[definition.FlatIndex]
            : 0f;
    }

    private static bool IsFoodInput(BrainInputDefinition input)
    {
        return string.Equals(input.Key, "vision.food_density", StringComparison.Ordinal)
            || IsPlantInput(input)
            || IsMeatOrEggInput(input)
            || input.Key.StartsWith("contact.food", StringComparison.Ordinal)
            || input.Key.StartsWith("contact.plant_", StringComparison.Ordinal)
            || input.Key.StartsWith("contact.meat_", StringComparison.Ordinal)
            || input.Key.StartsWith("contact.egg_", StringComparison.Ordinal)
            || input.Key.StartsWith("scent.meat", StringComparison.Ordinal)
            || input.Key.StartsWith("scent.rotten_meat", StringComparison.Ordinal);
    }

    private static bool IsPlantInput(BrainInputDefinition input)
    {
        return input.Key.StartsWith("vision.plant", StringComparison.Ordinal)
            || input.Key.Contains(".plant_", StringComparison.Ordinal)
            || input.Key.StartsWith("contact.plant", StringComparison.Ordinal);
    }

    private static bool IsMeatOrEggInput(BrainInputDefinition input)
    {
        return input.Key.StartsWith("vision.meat", StringComparison.Ordinal)
            || input.Key.Contains(".meat_", StringComparison.Ordinal)
            || input.Key.Contains(".egg_", StringComparison.Ordinal)
            || input.Key.StartsWith("contact.meat", StringComparison.Ordinal)
            || input.Key.StartsWith("contact.egg", StringComparison.Ordinal)
            || input.Key.StartsWith("scent.meat", StringComparison.Ordinal)
            || input.Key.StartsWith("scent.rotten_meat", StringComparison.Ordinal);
    }

    private static BrainProbeOutputValue CreateOutputValue(
        BrainOutputDefinition definition,
        BrainEvaluationResult baseline,
        BrainEvaluationResult modified)
    {
        var baselineValue = ReadOutput(definition.Key, baseline);
        var modifiedValue = ReadOutput(definition.Key, modified);
        var delta = modifiedValue - baselineValue;
        var threshold = ActivationThreshold(definition.Key);
        return new BrainProbeOutputValue(
            definition.Key,
            definition.Name,
            definition.FlatIndex,
            definition.Group.ToString(),
            definition.MinimumValue,
            definition.MaximumValue,
            definition.NeutralValue,
            baselineValue,
            modifiedValue,
            delta,
            Math.Abs(delta) >= MeaningfulOutputDelta,
            threshold,
            threshold.HasValue ? baselineValue > threshold.Value : null,
            threshold.HasValue ? modifiedValue > threshold.Value : null,
            definition.Meaning);
    }

    private static float ReadOutput(string key, BrainEvaluationResult evaluation)
    {
        return key switch
        {
            "action.move_forward" => evaluation.Actions.MoveForward,
            "action.turn" => evaluation.Actions.Turn,
            "action.eat" => evaluation.Actions.Eat,
            "action.reproduce" => evaluation.Actions.Reproduce,
            "action.attack" => evaluation.Actions.Attack,
            "action.grab" => evaluation.Actions.Grab,
            "action.sound_amplitude" => evaluation.Actions.SoundAmplitude,
            "action.sound_tone" => evaluation.Actions.SoundTone,
            "dense_memory.write_forward" => evaluation.Memory.DirectionForward,
            "dense_memory.write_right" => evaluation.Memory.DirectionRight,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown brain output key.")
        };
    }

    private static float? ActivationThreshold(string key)
    {
        return key switch
        {
            "action.eat" => 0f,
            "action.reproduce" => ReproduceThreshold,
            "action.attack" => AttackThreshold,
            "action.grab" => GrabThreshold,
            "action.sound_amplitude" => SoundEmissionThreshold,
            _ => null
        };
    }

    private static bool TryFindCreature(WorldState state, EntityId creatureId, out CreatureState creature)
    {
        foreach (var candidate in state.Creatures)
        {
            if (candidate.Id == creatureId)
            {
                creature = candidate;
                return true;
            }
        }

        creature = default;
        return false;
    }
}

public sealed record BrainProbeEvaluation(
    BrainProbeCreature Creature,
    string BrainArchitectureKind,
    bool SupportsRawInputOverrides,
    int OverrideCount,
    int ChangedOutputCount,
    int GateFlipCount,
    float MaxAbsoluteOutputDelta,
    IReadOnlyList<BrainProbeInputValue> Inputs,
    IReadOnlyList<BrainProbeOutputValue> Outputs);

public sealed record BrainProbePopulationEvaluation(
    string BrainArchitectureKind,
    bool SupportsRawInputOverrides,
    int OverrideCount,
    int TotalCreatureCount,
    int EvaluatedCreatureCount,
    int SkippedCreatureCount,
    int UnsupportedOverrideCreatureCount,
    int ChangedCreatureCount,
    float ChangedCreatureShare,
    int GateFlipCreatureCount,
    float GateFlipCreatureShare,
    float MaxAbsoluteOutputDelta,
    IReadOnlyList<BrainProbePopulationOutputValue> Outputs);

public sealed record BrainProbeCreature(
    int Id,
    int Generation,
    float AgeSeconds,
    int GenomeId,
    int BrainId,
    string BrainArchitectureKind,
    float EnergyRatio,
    float HealthRatio,
    float Hunger,
    bool SoundDetected,
    float SoundDensity,
    float SoundAmplitude);

public sealed record BrainProbeInputValue(
    string Key,
    string Name,
    int FlatIndex,
    string Group,
    float MinimumValue,
    float MaximumValue,
    float NeutralValue,
    float BaselineValue,
    float ModifiedValue,
    bool Overridden,
    string Meaning);

public sealed record BrainProbeOutputValue(
    string Key,
    string Name,
    int FlatIndex,
    string Group,
    float MinimumValue,
    float MaximumValue,
    float NeutralValue,
    float BaselineValue,
    float ModifiedValue,
    float Delta,
    bool Changed,
    float? ActivationThreshold,
    bool? BaselineActive,
    bool? ModifiedActive,
    string Meaning);

public sealed record BrainProbePopulationOutputValue(
    string Key,
    string Name,
    int FlatIndex,
    string Group,
    float BaselineMean,
    float ModifiedMean,
    float MeanDelta,
    float MeanAbsoluteDelta,
    float MaxAbsoluteDelta,
    int ChangedCreatureCount,
    float ChangedCreatureShare,
    int GateFlipCount,
    float GateFlipShare,
    int PositiveDeltaCount,
    int NegativeDeltaCount,
    string Meaning);

public enum BrainProbePresetKind
{
    MuteSound,
    NoFood,
    OnlyPlants,
    OnlyMeatEggs,
    NoContact,
    Hungry,
    Full,
    ReadyToReproduce
}

internal sealed class BrainProbePopulationOutputAccumulator(BrainOutputDefinition definition)
{
    private float _baselineTotal;
    private float _modifiedTotal;
    private float _deltaTotal;
    private float _absoluteDeltaTotal;
    private float _maxAbsoluteDelta;
    private int _sampleCount;
    private int _changedCreatureCount;
    private int _gateFlipCount;
    private int _positiveDeltaCount;
    private int _negativeDeltaCount;

    public void Add(BrainProbeOutputValue output)
    {
        var delta = output.Delta;
        var absoluteDelta = Math.Abs(delta);
        _sampleCount++;
        _baselineTotal += output.BaselineValue;
        _modifiedTotal += output.ModifiedValue;
        _deltaTotal += delta;
        _absoluteDeltaTotal += absoluteDelta;
        _maxAbsoluteDelta = Math.Max(_maxAbsoluteDelta, absoluteDelta);
        if (output.Changed)
        {
            _changedCreatureCount++;
        }

        if (output.BaselineActive.HasValue
            && output.ModifiedActive.HasValue
            && output.BaselineActive.Value != output.ModifiedActive.Value)
        {
            _gateFlipCount++;
        }

        if (delta > 0f)
        {
            _positiveDeltaCount++;
        }
        else if (delta < 0f)
        {
            _negativeDeltaCount++;
        }
    }

    public BrainProbePopulationOutputValue ToValue()
    {
        var divisor = Math.Max(1, _sampleCount);
        return new BrainProbePopulationOutputValue(
            definition.Key,
            definition.Name,
            definition.FlatIndex,
            definition.Group.ToString(),
            _baselineTotal / divisor,
            _modifiedTotal / divisor,
            _deltaTotal / divisor,
            _absoluteDeltaTotal / divisor,
            _maxAbsoluteDelta,
            _changedCreatureCount,
            _changedCreatureCount / (float)divisor,
            _gateFlipCount,
            _gateFlipCount / (float)divisor,
            _positiveDeltaCount,
            _negativeDeltaCount,
            definition.Meaning);
    }
}
