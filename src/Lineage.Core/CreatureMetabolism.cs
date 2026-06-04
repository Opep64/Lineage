namespace Lineage.Core;

public static class CreatureMetabolism
{
    public const float DefaultPace = 1f;
    public const float MinimumPace = 0.5f;
    public const float MaximumPace = 2f;
    public const float LowPaceThreshold = 0.85f;
    public const float HighPaceThreshold = 1.15f;

    public static float NormalizePace(float pace)
    {
        return float.IsFinite(pace)
            ? Math.Clamp(pace, MinimumPace, MaximumPace)
            : DefaultPace;
    }

    public static MetabolicPaceBand PaceBand(CreatureGenome genome)
    {
        var pace = NormalizePace(genome.MetabolicPace);
        if (pace < LowPaceThreshold)
        {
            return MetabolicPaceBand.Low;
        }

        return pace > HighPaceThreshold
            ? MetabolicPaceBand.High
            : MetabolicPaceBand.Normal;
    }

    public static float BasalCostMultiplier(CreatureGenome genome)
    {
        return Pow(NormalizePace(genome.MetabolicPace), 1.25f);
    }

    public static float DigestionRateMultiplier(CreatureGenome genome)
    {
        return Pow(NormalizePace(genome.MetabolicPace), 0.65f);
    }

    public static float EggProductionRateMultiplier(CreatureGenome genome)
    {
        return Pow(NormalizePace(genome.MetabolicPace), 0.85f);
    }

    public static float CooldownRecoveryMultiplier(CreatureGenome genome)
    {
        return Pow(NormalizePace(genome.MetabolicPace), 0.65f);
    }

    public static float DevelopmentRateMultiplier(CreatureGenome genome)
    {
        return Pow(NormalizePace(genome.MetabolicPace), 0.85f);
    }

    public static float HealingRateMultiplier(CreatureGenome genome)
    {
        return Pow(NormalizePace(genome.MetabolicPace), 0.65f);
    }

    public static float BiologicalAgeRateMultiplier(CreatureGenome genome)
    {
        return Pow(NormalizePace(genome.MetabolicPace), 0.5f);
    }

    public static float LocomotionRateMultiplier(CreatureGenome genome)
    {
        return Pow(NormalizePace(genome.MetabolicPace), 0.3f);
    }

    public static float EffectiveMaturityAgeSeconds(CreatureGenome genome)
    {
        return genome.MaturityAgeSeconds <= 0f
            ? 0f
            : genome.MaturityAgeSeconds / DevelopmentRateMultiplier(genome);
    }

    private static float Pow(float value, float exponent)
    {
        return MathF.Pow(Math.Max(0.000001f, value), exponent);
    }
}

public enum MetabolicPaceBand
{
    Low,
    Normal,
    High
}
