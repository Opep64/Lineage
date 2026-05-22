namespace Lineage.Core;

/// <summary>
/// Calculates the global seasonal multiplier used by plant lifecycle systems.
/// </summary>
public static class SeasonalFertility
{
    public const float NeutralMultiplier = 1f;
    public const float MaxEffectiveAmplitude = 0.95f;

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

    public static BiomeSeasonalFertilityMultipliers CalculateBiomeMultipliers(
        bool enabled,
        double elapsedSeconds,
        float seasonLengthSeconds,
        float fertilityAmplitude,
        float phaseOffsetSeconds,
        BiomePressureProfile biomeAmplitudeProfile)
    {
        var phase = Calculate(
            enabled,
            elapsedSeconds,
            seasonLengthSeconds,
            fertilityAmplitude,
            phaseOffsetSeconds).Phase;

        return new BiomeSeasonalFertilityMultipliers(
            phase,
            CalculateBiomeMultiplier(enabled, elapsedSeconds, seasonLengthSeconds, fertilityAmplitude, phaseOffsetSeconds, biomeAmplitudeProfile.Barren),
            CalculateBiomeMultiplier(enabled, elapsedSeconds, seasonLengthSeconds, fertilityAmplitude, phaseOffsetSeconds, biomeAmplitudeProfile.Sparse),
            CalculateBiomeMultiplier(enabled, elapsedSeconds, seasonLengthSeconds, fertilityAmplitude, phaseOffsetSeconds, biomeAmplitudeProfile.Grassland),
            CalculateBiomeMultiplier(enabled, elapsedSeconds, seasonLengthSeconds, fertilityAmplitude, phaseOffsetSeconds, biomeAmplitudeProfile.Rich));
    }

    private static float CalculateBiomeMultiplier(
        bool enabled,
        double elapsedSeconds,
        float seasonLengthSeconds,
        float fertilityAmplitude,
        float phaseOffsetSeconds,
        float biomeAmplitudeMultiplier)
    {
        var effectiveAmplitude = Math.Clamp(
            fertilityAmplitude * biomeAmplitudeMultiplier,
            0f,
            MaxEffectiveAmplitude);
        return Calculate(
            enabled,
            elapsedSeconds,
            seasonLengthSeconds,
            effectiveAmplitude,
            phaseOffsetSeconds).FertilityMultiplier;
    }
}

public readonly record struct SeasonalFertilityState(
    float Phase,
    float FertilityMultiplier);

public readonly record struct BiomeSeasonalFertilityMultipliers(
    float Phase,
    float Barren,
    float Sparse,
    float Grassland,
    float Rich)
{
    public float For(BiomeKind kind)
    {
        return kind switch
        {
            BiomeKind.Barren => Barren,
            BiomeKind.Sparse => Sparse,
            BiomeKind.Rich => Rich,
            _ => Grassland
        };
    }
}
