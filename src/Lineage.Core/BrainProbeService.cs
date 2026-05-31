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

        var inputFrame = BrainInputFrame.FromSenses(creature.Senses, genome);
        var memoryInputs = LegacyNeuralMemoryInputFrame.FromSenses(creature.Senses);
        var baselineInputs = new float[NeuralBrainSchema.InputCount];
        LegacyNeuralBrainAdapter.FillInputs(inputFrame, memoryInputs, baselineInputs);

        var modifiedInputs = baselineInputs.ToArray();
        foreach (var (key, value) in overrides)
        {
            var definition = InputByKey[key];
            modifiedInputs[definition.FlatIndex] = Math.Clamp(value, definition.MinimumValue, definition.MaximumValue);
        }

        var baseline = EvaluateBrain(brain, inputFrame, memoryInputs, baselineInputs, overrides.Count > 0);
        var modified = overrides.Count == 0
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
                overrides.ContainsKey(input.Key),
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
            overrides.Count,
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
