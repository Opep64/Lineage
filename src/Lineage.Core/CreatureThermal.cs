namespace Lineage.Core;

/// <summary>
/// Shared helpers for normalized creature climate traits and temperature mismatch cues.
/// </summary>
public static class CreatureThermal
{
    public const float DefaultOptimum = TemperatureMap.NeutralTemperature;
    public const float DefaultTolerance = 0.25f;
    public const float MinimumOptimum = 0.05f;
    public const float MaximumOptimum = 0.95f;
    public const float MinimumTolerance = 0.05f;
    public const float MaximumTolerance = 0.6f;
    public const float ColdTemperatureBandMaximum = 0.35f;
    public const float HotTemperatureBandMinimum = 0.65f;
    public const float ThermalStressMismatchThreshold = 0.5f;

    public static float ThermalMismatch(float temperature, CreatureGenome genome)
    {
        var optimum = NormalizeOptimum(genome.ThermalOptimum);
        var tolerance = NormalizeTolerance(genome.ThermalTolerance);
        return Math.Clamp(MathF.Abs(Math.Clamp(temperature, 0f, 1f) - optimum) / tolerance, 0f, 1f);
    }

    public static float NormalizeOptimum(float optimum)
    {
        return optimum <= 0f || !float.IsFinite(optimum)
            ? DefaultOptimum
            : Math.Clamp(optimum, MinimumOptimum, MaximumOptimum);
    }

    public static float NormalizeTolerance(float tolerance)
    {
        return tolerance <= 0f || !float.IsFinite(tolerance)
            ? DefaultTolerance
            : Math.Clamp(tolerance, MinimumTolerance, MaximumTolerance);
    }

    public static TemperatureBand ClassifyTemperatureBand(float temperature)
    {
        var normalized = Math.Clamp(temperature, 0f, 1f);
        if (normalized < ColdTemperatureBandMaximum)
        {
            return TemperatureBand.Cold;
        }

        return normalized >= HotTemperatureBandMinimum
            ? TemperatureBand.Hot
            : TemperatureBand.Temperate;
    }

    public static string FormatNicheLabel(
        float averageTemperature,
        float averageMismatch,
        float coldShare,
        float temperateShare,
        float hotShare)
    {
        if (averageMismatch >= 0.75f)
        {
            return "thermal-stressed";
        }

        if (coldShare >= 0.45f || averageTemperature < 0.4f)
        {
            return "cold-biased";
        }

        if (hotShare >= 0.35f || averageTemperature > 0.6f)
        {
            return "hot-biased";
        }

        if (coldShare >= 0.2f && hotShare >= 0.2f)
        {
            return "wide-ranging";
        }

        return temperateShare >= 0.55f
            ? "temperate"
            : "mixed-climate";
    }
}

public enum TemperatureBand
{
    Cold,
    Temperate,
    Hot
}
