namespace Lineage.Core;

public enum EcologicalEventKind
{
    RegionalFertilityPulse,
    RegionalFertilityCrash,
    HeatWave,
    ColdSnap
}

public sealed record EcologicalEventDefinition
{
    public string Name { get; init; } = string.Empty;

    public EcologicalEventKind Kind { get; init; } = EcologicalEventKind.RegionalFertilityPulse;

    public float StartSeconds { get; init; }

    public float DurationSeconds { get; init; } = 60f;

    public float RegionX { get; init; }

    public float RegionY { get; init; }

    public float RegionWidth { get; init; } = 1f;

    public float RegionHeight { get; init; } = 1f;

    public float Strength { get; init; } = 1f;

    [System.Text.Json.Serialization.JsonIgnore]
    public double EndSeconds => (double)StartSeconds + DurationSeconds;

    public bool IsActive(double elapsedSeconds)
    {
        return elapsedSeconds >= StartSeconds && elapsedSeconds < EndSeconds;
    }

    public bool Contains(WorldBounds bounds, SimVector2 position)
    {
        var clamped = bounds.Clamp(position);
        var left = RegionX * bounds.Width;
        var top = RegionY * bounds.Height;
        var right = (RegionX + RegionWidth) * bounds.Width;
        var bottom = (RegionY + RegionHeight) * bounds.Height;
        return clamped.X >= left
            && clamped.X <= right
            && clamped.Y >= top
            && clamped.Y <= bottom;
    }

    public bool AffectsFertility => Kind is EcologicalEventKind.RegionalFertilityPulse
        or EcologicalEventKind.RegionalFertilityCrash;

    public bool AffectsTemperature => Kind is EcologicalEventKind.HeatWave
        or EcologicalEventKind.ColdSnap;

    public float FertilityMultiplierAt(double elapsedSeconds, WorldBounds bounds, SimVector2 position)
    {
        return AffectsFertility && IsActive(elapsedSeconds) && Contains(bounds, position)
            ? Strength
            : 1f;
    }

    public float TemperatureDeltaAt(double elapsedSeconds, WorldBounds bounds, SimVector2 position)
    {
        if (!AffectsTemperature || !IsActive(elapsedSeconds) || !Contains(bounds, position))
        {
            return 0f;
        }

        return Kind == EcologicalEventKind.ColdSnap ? -Strength : Strength;
    }

    public EcologicalEventDefinition Validated()
    {
        if (!Enum.IsDefined(Kind))
        {
            throw new InvalidOperationException($"{nameof(Kind)} must be a defined ecological event kind.");
        }

        EnsureNonNegative(StartSeconds, nameof(StartSeconds));
        EnsurePositive(DurationSeconds, nameof(DurationSeconds));
        EnsureRange(RegionX, 0f, 1f, nameof(RegionX));
        EnsureRange(RegionY, 0f, 1f, nameof(RegionY));
        EnsureRange(RegionWidth, 0.0001f, 1f, nameof(RegionWidth));
        EnsureRange(RegionHeight, 0.0001f, 1f, nameof(RegionHeight));
        if (RegionX + RegionWidth > 1.000001f)
        {
            throw new InvalidOperationException("Ecological event region X plus width cannot exceed 1.");
        }

        if (RegionY + RegionHeight > 1.000001f)
        {
            throw new InvalidOperationException("Ecological event region Y plus height cannot exceed 1.");
        }

        switch (Kind)
        {
            case EcologicalEventKind.RegionalFertilityPulse:
                EnsureRange(Strength, 0f, 10f, nameof(Strength));
                break;
            case EcologicalEventKind.RegionalFertilityCrash:
            case EcologicalEventKind.HeatWave:
            case EcologicalEventKind.ColdSnap:
                EnsureRange(Strength, 0f, 1f, nameof(Strength));
                break;
            default:
                throw new InvalidOperationException($"{nameof(Kind)} must be a defined ecological event kind.");
        }

        return this with
        {
            Name = string.IsNullOrWhiteSpace(Name)
                ? Kind.ToString()
                : Name.Trim()
        };
    }

    private static void EnsurePositive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0f)
        {
            throw new InvalidOperationException($"{name} must be finite and positive.");
        }
    }

    private static void EnsureNonNegative(float value, string name)
    {
        if (!float.IsFinite(value) || value < 0f)
        {
            throw new InvalidOperationException($"{name} must be finite and non-negative.");
        }
    }

    private static void EnsureRange(float value, float inclusiveMin, float inclusiveMax, string name)
    {
        if (!float.IsFinite(value) || value < inclusiveMin || value > inclusiveMax)
        {
            throw new InvalidOperationException($"{name} must be finite and between {inclusiveMin} and {inclusiveMax}.");
        }
    }
}
