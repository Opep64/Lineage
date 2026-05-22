namespace Lineage.Core;

/// <summary>
/// Calculates the global seasonal multiplier used by plant lifecycle systems.
/// </summary>
public static class SeasonalFertility
{
    public const float NeutralMultiplier = 1f;

    public static SeasonalFertilityState Calculate(
        bool enabled,
        double elapsedSeconds,
        float seasonLengthSeconds,
        float fertilityAmplitude,
        float phaseOffsetSeconds)
    {
        if (!enabled || fertilityAmplitude <= 0f || seasonLengthSeconds <= 0f)
        {
            return new SeasonalFertilityState(0f, NeutralMultiplier);
        }

        var wrappedSeconds = (elapsedSeconds + phaseOffsetSeconds) % seasonLengthSeconds;
        if (wrappedSeconds < 0.0)
        {
            wrappedSeconds += seasonLengthSeconds;
        }

        var phase = (float)(wrappedSeconds / seasonLengthSeconds);
        var wave = MathF.Sin(phase * MathF.Tau);
        var multiplier = MathF.Max(0f, NeutralMultiplier + fertilityAmplitude * wave);
        return new SeasonalFertilityState(phase, multiplier);
    }
}

public readonly record struct SeasonalFertilityState(
    float Phase,
    float FertilityMultiplier);
