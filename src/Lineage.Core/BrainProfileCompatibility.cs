namespace Lineage.Core;

public sealed record BrainProfileCompatibility(
    bool IsCompatible,
    bool RequiresNormalization,
    string Status,
    IReadOnlyList<string> Warnings)
{
    public static BrainProfileCompatibility Compatible { get; } = new(
        IsCompatible: true,
        RequiresNormalization: false,
        Status: "Compatible with the current sense/action schema.",
        Warnings: Array.Empty<string>());

    public static BrainProfileCompatibility Assess(BrainProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var warnings = new List<string>();
        if (profile.Version != BrainProfile.CurrentVersion)
        {
            return Incompatible($"Unsupported brain profile version {profile.Version}.");
        }

        try
        {
            _ = BrainFactory.Describe(profile.BrainArchitectureKind);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            return Incompatible(ex.Message);
        }

        if (profile.InputSchemaVersion > NeuralBrainSchema.InputSchemaVersion)
        {
            return Incompatible(
                $"Brain profile input schema {profile.InputSchemaVersion} is newer than supported schema {NeuralBrainSchema.InputSchemaVersion}.");
        }

        if (profile.OutputSchemaVersion > NeuralBrainSchema.OutputSchemaVersion)
        {
            return Incompatible(
                $"Brain profile output schema {profile.OutputSchemaVersion} is newer than supported schema {NeuralBrainSchema.OutputSchemaVersion}.");
        }

        if (profile.BrainArchitectureKind == BrainArchitectureKind.RtNeatGraph)
        {
            if (profile.RtNeatBrain is null)
            {
                return Incompatible("rtNEAT brain profile must include graph payload.");
            }
        }
        else if (profile.Weights.Length == 0)
        {
            return Incompatible("Brain profile must include neural brain weights.");
        }

        try
        {
            var validated = profile.Validated();
            if (profile.InputSchemaVersion < NeuralBrainSchema.InputSchemaVersion
                || profile.OutputSchemaVersion < NeuralBrainSchema.OutputSchemaVersion
                || profile.InputCount != NeuralBrainSchema.InputCount
                || profile.OutputCount != NeuralBrainSchema.OutputCount
                || profile.WeightCount != validated.WeightCount)
            {
                warnings.Add(
                    $"Profile will be normalized from input v{profile.InputSchemaVersion}/{profile.InputCount} and output v{profile.OutputSchemaVersion}/{profile.OutputCount} to input v{NeuralBrainSchema.InputSchemaVersion}/{NeuralBrainSchema.InputCount} and output v{NeuralBrainSchema.OutputSchemaVersion}/{NeuralBrainSchema.OutputCount}.");
            }
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            return Incompatible(ex.Message);
        }

        return warnings.Count == 0
            ? Compatible
            : new BrainProfileCompatibility(
                IsCompatible: true,
                RequiresNormalization: true,
                Status: "Compatible after schema normalization.",
                Warnings: warnings);
    }

    public static BrainProfileCompatibility Incompatible(string status)
    {
        var message = string.IsNullOrWhiteSpace(status)
            ? "Brain profile is not compatible with the current runtime."
            : status.Trim();
        return new BrainProfileCompatibility(
            IsCompatible: false,
            RequiresNormalization: false,
            Status: message,
            Warnings: new[] { message });
    }
}
