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

    public static SeasonalFertilityState CalculateAt(
        bool enabled,
        double elapsedSeconds,
        float seasonLengthSeconds,
        float fertilityAmplitude,
        float phaseOffsetSeconds,
        SeasonPhaseMode phaseMode,
        WorldBounds bounds,
        SimVector2 position)
    {
        var spatialOffsetSeconds = CalculateSpatialPhaseOffsetSeconds(phaseMode, bounds, position, seasonLengthSeconds);
        return Calculate(
            enabled,
            elapsedSeconds,
            seasonLengthSeconds,
            fertilityAmplitude,
            phaseOffsetSeconds + spatialOffsetSeconds);
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
            CalculateBiomeMultiplier(enabled, elapsedSeconds, seasonLengthSeconds, fertilityAmplitude, phaseOffsetSeconds, biomeAmplitudeProfile.Desert),
            CalculateBiomeMultiplier(enabled, elapsedSeconds, seasonLengthSeconds, fertilityAmplitude, phaseOffsetSeconds, biomeAmplitudeProfile.Scrubland),
            CalculateBiomeMultiplier(enabled, elapsedSeconds, seasonLengthSeconds, fertilityAmplitude, phaseOffsetSeconds, biomeAmplitudeProfile.Grassland),
            CalculateBiomeMultiplier(enabled, elapsedSeconds, seasonLengthSeconds, fertilityAmplitude, phaseOffsetSeconds, biomeAmplitudeProfile.Fertile),
            CalculateBiomeMultiplier(enabled, elapsedSeconds, seasonLengthSeconds, fertilityAmplitude, phaseOffsetSeconds, biomeAmplitudeProfile.Forest),
            CalculateBiomeMultiplier(enabled, elapsedSeconds, seasonLengthSeconds, fertilityAmplitude, phaseOffsetSeconds, biomeAmplitudeProfile.Wetland),
            CalculateBiomeMultiplier(enabled, elapsedSeconds, seasonLengthSeconds, fertilityAmplitude, phaseOffsetSeconds, biomeAmplitudeProfile.Tundra),
            CalculateBiomeMultiplier(enabled, elapsedSeconds, seasonLengthSeconds, fertilityAmplitude, phaseOffsetSeconds, biomeAmplitudeProfile.Highland));
    }

    public static float CalculateBiomeMultiplierAt(
        bool enabled,
        double elapsedSeconds,
        float seasonLengthSeconds,
        float fertilityAmplitude,
        float phaseOffsetSeconds,
        SeasonPhaseMode phaseMode,
        WorldBounds bounds,
        SimVector2 position,
        BiomeKind biome,
        BiomePressureProfile biomeAmplitudeProfile)
    {
        var effectiveAmplitude = Math.Clamp(
            fertilityAmplitude * biomeAmplitudeProfile.For(biome),
            0f,
            MaxEffectiveAmplitude);
        return CalculateAt(
            enabled,
            elapsedSeconds,
            seasonLengthSeconds,
            effectiveAmplitude,
            phaseOffsetSeconds,
            phaseMode,
            bounds,
            position).FertilityMultiplier;
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

    private static float CalculateSpatialPhaseOffsetSeconds(
        SeasonPhaseMode phaseMode,
        WorldBounds bounds,
        SimVector2 position,
        float seasonLengthSeconds)
    {
        if (seasonLengthSeconds <= 0f)
        {
            return 0f;
        }

        return phaseMode switch
        {
            SeasonPhaseMode.HorizontalOpposed when bounds.Width > 0f && position.X >= bounds.Width * 0.5f =>
                seasonLengthSeconds * 0.5f,
            SeasonPhaseMode.VerticalOpposed when bounds.Height > 0f && position.Y >= bounds.Height * 0.5f =>
                seasonLengthSeconds * 0.5f,
            _ => 0f
        };
    }
}

public readonly record struct SeasonalFertilityState(
    float Phase,
    float FertilityMultiplier);

public readonly record struct BiomeSeasonalFertilityMultipliers(
    float Phase,
    float Desert,
    float Scrubland,
    float Grassland,
    float Fertile,
    float Forest,
    float Wetland,
    float Tundra,
    float Highland)
{
    public float Barren => Desert;

    public float Sparse => Scrubland;

    public float Rich => Fertile;

    public float For(BiomeKind kind)
    {
        return BiomeKinds.Canonicalize(kind) switch
        {
            BiomeKind.Desert => Desert,
            BiomeKind.Scrubland => Scrubland,
            BiomeKind.Fertile => Fertile,
            BiomeKind.Forest => Forest,
            BiomeKind.Wetland => Wetland,
            BiomeKind.Tundra => Tundra,
            BiomeKind.Highland => Highland,
            _ => Grassland
        };
    }
}
