namespace Lineage.Core;

/// <summary>
/// Portable description of a species representative that can be exported and injected into another world.
/// </summary>
///
/// <remarks>
/// The first profile format stores one exact representative genome and neural brain. Later versions can add
/// lineage-level summaries or averaged species centroids without weakening this reproducible baseline.
/// </remarks>
public sealed record SpeciesProfile
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public string Name { get; init; } = "Unnamed species";

    public string Notes { get; init; } = string.Empty;

    public SpeciesProfileSource Source { get; init; } = new();

    public string? DefaultBrainPath { get; init; }

    public CreatureGenome Genome { get; init; } = CreatureGenome.Baseline;

    public BrainArchitectureKind BrainArchitectureKind { get; init; } = BrainArchitectureKind.HybridNeural;

    public int BrainHiddenNodeCount { get; init; }

    public float[] BrainWeights { get; init; } = [];

    public RtNeatBrainGenome? RtNeatBrain { get; init; }

    public int BrainWeightCount => BrainArchitectureKind == BrainArchitectureKind.RtNeatGraph
        ? RtNeatBrain?.WeightCount ?? 0
        : BrainWeights.Length;

    public BrainGenome CreateBrain()
    {
        _ = BrainFactory.Describe(BrainArchitectureKind);
        return BrainArchitectureKind == BrainArchitectureKind.RtNeatGraph
            ? BrainGenome.FromRtNeat(RtNeatBrain ?? throw new InvalidOperationException("rtNEAT species profile must include graph brain payload."))
            : BrainGenome.FromNeural(BrainArchitectureKind, new NeuralBrainGenome(BrainWeights));
    }

    public SpeciesProfile Validated()
    {
        if (Version != CurrentVersion)
        {
            throw new InvalidOperationException($"Unsupported species profile version {Version}.");
        }

        var name = string.IsNullOrWhiteSpace(Name)
            ? "Unnamed species"
            : Name.Trim();
        var genome = Genome.Validated();
        _ = BrainFactory.Describe(BrainArchitectureKind);
        if (BrainArchitectureKind == BrainArchitectureKind.RtNeatGraph)
        {
            if (RtNeatBrain is null)
            {
                throw new InvalidOperationException("rtNEAT species profile must include graph brain payload.");
            }

            var graphBrain = RtNeatBrain.Validated();
            return this with
            {
                Name = name,
                Notes = Notes.Trim(),
                DefaultBrainPath = string.IsNullOrWhiteSpace(DefaultBrainPath)
                    ? null
                    : DefaultBrainPath.Trim(),
                Genome = genome,
                BrainArchitectureKind = BrainArchitectureKind,
                BrainHiddenNodeCount = graphBrain.HiddenNodeCount,
                BrainWeights = graphBrain.FlattenWeights(),
                RtNeatBrain = graphBrain
            };
        }

        if (BrainWeights.Length == 0)
        {
            throw new InvalidOperationException("Species profile must include neural brain weights.");
        }

        var brain = new NeuralBrainGenome(BrainWeights);
        _ = BrainGenome.FromNeural(BrainArchitectureKind, brain);
        return this with
        {
            Name = name,
            Notes = Notes.Trim(),
            DefaultBrainPath = string.IsNullOrWhiteSpace(DefaultBrainPath)
                ? null
                : DefaultBrainPath.Trim(),
            Genome = genome,
            BrainArchitectureKind = BrainArchitectureKind,
            BrainHiddenNodeCount = brain.HiddenNodeCount,
            BrainWeights = brain.Weights.ToArray()
        };
    }
}

public sealed record SpeciesProfileSource
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

    public int GenomeId { get; init; }

    public int BrainId { get; init; }

    public DateTimeOffset ExportedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
