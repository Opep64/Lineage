namespace Lineage.Core;

/// <summary>
/// Portable controller profile that can be paired with any compatible creature body.
/// </summary>
public sealed record BrainProfile
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public string Name { get; init; } = "Unnamed brain";

    public string Notes { get; init; } = string.Empty;

    public BrainProfileSource Source { get; init; } = new();

    public BrainArchitectureKind BrainArchitectureKind { get; init; } = BrainArchitectureKind.HybridNeural;

    public int InputSchemaVersion { get; init; } = NeuralBrainSchema.InputSchemaVersion;

    public int OutputSchemaVersion { get; init; } = NeuralBrainSchema.OutputSchemaVersion;

    public int InputCount { get; init; } = NeuralBrainSchema.InputCount;

    public int OutputCount { get; init; } = NeuralBrainSchema.OutputCount;

    public int HiddenNodeCount { get; init; }

    public float[] Weights { get; init; } = [];

    public NeuralBrainGenome CreateBrain()
    {
        _ = BrainFactory.Describe(BrainArchitectureKind);
        return new NeuralBrainGenome(Weights);
    }

    public BrainProfile Validated()
    {
        if (Version != CurrentVersion)
        {
            throw new InvalidOperationException($"Unsupported brain profile version {Version}.");
        }

        var name = string.IsNullOrWhiteSpace(Name)
            ? "Unnamed brain"
            : Name.Trim();
        _ = BrainFactory.Describe(BrainArchitectureKind);
        if (InputSchemaVersion > NeuralBrainSchema.InputSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Brain profile input schema {InputSchemaVersion} is newer than supported schema {NeuralBrainSchema.InputSchemaVersion}.");
        }

        if (OutputSchemaVersion > NeuralBrainSchema.OutputSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Brain profile output schema {OutputSchemaVersion} is newer than supported schema {NeuralBrainSchema.OutputSchemaVersion}.");
        }

        if (Weights.Length == 0)
        {
            throw new InvalidOperationException("Brain profile must include neural brain weights.");
        }

        // NeuralBrainGenome normalizes older dense layouts into the current input/output schema,
        // leaving newly added senses or outputs neutral when possible.
        var brain = new NeuralBrainGenome(Weights);
        return this with
        {
            Name = name,
            Notes = Notes.Trim(),
            InputSchemaVersion = NeuralBrainSchema.InputSchemaVersion,
            OutputSchemaVersion = NeuralBrainSchema.OutputSchemaVersion,
            InputCount = NeuralBrainSchema.InputCount,
            OutputCount = NeuralBrainSchema.OutputCount,
            HiddenNodeCount = brain.HiddenNodeCount,
            Weights = brain.Weights.ToArray()
        };
    }
}

public sealed record BrainProfileSource
{
    public string ScenarioName { get; init; } = string.Empty;

    public ulong Seed { get; init; }

    public long Tick { get; init; }

    public double ElapsedSeconds { get; init; }

    public InitialBrainKind? InitialBrainKind { get; init; }

    public int CreatureId { get; init; }

    public int FounderId { get; init; }

    public int ParentId { get; init; }

    public int Generation { get; init; }

    public int BrainId { get; init; }

    public DateTimeOffset ExportedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
