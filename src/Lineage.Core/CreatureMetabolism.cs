namespace Lineage.Core;

public static class CreatureMetabolism
{
    public const float DefaultPace = 1f;
    public const float MinimumPace = 0.5f;
    public const float MaximumPace = 2f;
    public const float LowPaceThreshold = 0.85f;
    public const float HighPaceThreshold = 1.15f;
    private const float BodySizeLifeExpectancyExponent = 0.25f;
    private const float OldAgeDeathRampFraction = 0.25f;
    private const float MinimumOldAgeDeathRampSeconds = 1f;

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
        return Pow(NormalizePace(genome.MetabolicPace), 1.1f);
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
        return Pow(NormalizePace(genome.MetabolicPace), 0.1f);
    }

    public static float EffectiveMaturityAgeSeconds(CreatureGenome genome)
    {
        return genome.MaturityAgeSeconds <= 0f
            ? 0f
            : genome.MaturityAgeSeconds / DevelopmentRateMultiplier(genome);
    }

    public static float EffectiveMaxLifeExpectancySeconds(CreatureGenome genome)
    {
        var bodyScale = Pow(
            Math.Max(0.000001f, genome.BodyRadius / CreatureGenome.Baseline.BodyRadius),
            BodySizeLifeExpectancyExponent);
        return Math.Max(1f, genome.MaxLifeExpectancySeconds)
            * bodyScale
            / BiologicalAgeRateMultiplier(genome);
    }

    public static float OldAgeDeathProbability(CreatureState creature, CreatureGenome genome, float deltaSeconds)
    {
        if (deltaSeconds <= 0f)
        {
            return 0f;
        }

        var expectedLifeSeconds = EffectiveMaxLifeExpectancySeconds(genome);
        if (creature.AgeSeconds <= expectedLifeSeconds)
        {
            return 0f;
        }

        var rampSeconds = Math.Max(
            MinimumOldAgeDeathRampSeconds,
            expectedLifeSeconds * OldAgeDeathRampFraction);
        var overageSeconds = creature.AgeSeconds - expectedLifeSeconds;
        if (overageSeconds >= rampSeconds)
        {
            return 1f;
        }

        var progress = Math.Clamp(overageSeconds / rampSeconds, 0f, 1f);
        return Math.Clamp(deltaSeconds * progress * progress * 4f / rampSeconds, 0f, 1f);
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
