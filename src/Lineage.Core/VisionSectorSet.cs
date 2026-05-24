namespace Lineage.Core;

/// <summary>
/// Fixed-width visual field sampled across a creature's current vision arc.
/// </summary>
///
/// <remarks>
/// The samples are ordered from left edge to right edge relative to the creature's
/// heading. The center sample is index 4 for the initial 9-sector model.
/// </remarks>
public struct VisionSectorSet
{
    public const int SectorCount = 9;
    public const int CenterSectorIndex = SectorCount / 2;

    public VisionSectorSample Sector0;
    public VisionSectorSample Sector1;
    public VisionSectorSample Sector2;
    public VisionSectorSample Sector3;
    public VisionSectorSample Sector4;
    public VisionSectorSample Sector5;
    public VisionSectorSample Sector6;
    public VisionSectorSample Sector7;
    public VisionSectorSample Sector8;

    public bool HasAnySignal { get; private set; }

    public readonly VisionSectorSample Get(int index)
    {
        return index switch
        {
            0 => Sector0,
            1 => Sector1,
            2 => Sector2,
            3 => Sector3,
            4 => Sector4,
            5 => Sector5,
            6 => Sector6,
            7 => Sector7,
            8 => Sector8,
            _ => throw new ArgumentOutOfRangeException(nameof(index), "Vision sector index is out of range.")
        };
    }

    public void AddPlant(int sectorIndex, float proximity)
    {
        var sector = Get(sectorIndex);
        sector.AddPlant(proximity);
        Set(sectorIndex, sector);
        HasAnySignal = true;
    }

    public void AddMeat(int sectorIndex, float proximity)
    {
        var sector = Get(sectorIndex);
        sector.AddMeat(proximity);
        Set(sectorIndex, sector);
        HasAnySignal = true;
    }

    public void AddEgg(int sectorIndex, float proximity)
    {
        var sector = Get(sectorIndex);
        sector.AddEgg(proximity);
        Set(sectorIndex, sector);
        HasAnySignal = true;
    }

    public void AddCreature(int sectorIndex, float proximity)
    {
        var sector = Get(sectorIndex);
        sector.AddCreature(proximity);
        Set(sectorIndex, sector);
        HasAnySignal = true;
    }

    public static bool TryGetSectorIndex(
        SimVector2 toTarget,
        SimVector2 forward,
        SimVector2 right,
        bool hasLimitedVision,
        float visionAngleRadians,
        out int sectorIndex)
    {
        return TryGetSectorIndex(
            toTarget.X,
            toTarget.Y,
            forward.X,
            forward.Y,
            right.X,
            right.Y,
            hasLimitedVision,
            visionAngleRadians,
            out sectorIndex);
    }

    internal static bool TryGetSectorIndex(
        float toTargetX,
        float toTargetY,
        float forwardX,
        float forwardY,
        float rightX,
        float rightY,
        bool hasLimitedVision,
        float visionAngleRadians,
        out int sectorIndex)
    {
        var forwardDistance = toTargetX * forwardX + toTargetY * forwardY;
        var rightDistance = toTargetX * rightX + toTargetY * rightY;
        if (Math.Abs(forwardDistance) <= 0.000001f
            && Math.Abs(rightDistance) <= 0.000001f)
        {
            sectorIndex = CenterSectorIndex;
            return true;
        }

        var angle = MathF.Atan2(rightDistance, forwardDistance);
        var halfAngle = hasLimitedVision
            ? Math.Clamp(visionAngleRadians * 0.5f, 0.0001f, MathF.PI)
            : MathF.PI;
        if (angle < -halfAngle || angle > halfAngle)
        {
            sectorIndex = -1;
            return false;
        }

        var normalized = (angle + halfAngle) / (halfAngle * 2f);
        sectorIndex = Math.Clamp((int)MathF.Floor(normalized * SectorCount), 0, SectorCount - 1);
        return true;
    }

    private void Set(int index, VisionSectorSample sample)
    {
        switch (index)
        {
            case 0:
                Sector0 = sample;
                break;
            case 1:
                Sector1 = sample;
                break;
            case 2:
                Sector2 = sample;
                break;
            case 3:
                Sector3 = sample;
                break;
            case 4:
                Sector4 = sample;
                break;
            case 5:
                Sector5 = sample;
                break;
            case 6:
                Sector6 = sample;
                break;
            case 7:
                Sector7 = sample;
                break;
            case 8:
                Sector8 = sample;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(index), "Vision sector index is out of range.");
        }
    }
}

/// <summary>
/// Aggregated visual signal for one angular sector.
/// </summary>
public struct VisionSectorSample
{
    private const float DensityIncrement = 1f / 8f;

    public float PlantDensity { get; private set; }

    public float PlantProximity { get; private set; }

    public float MeatDensity { get; private set; }

    public float MeatProximity { get; private set; }

    public float EggDensity { get; private set; }

    public float EggProximity { get; private set; }

    public float CreatureDensity { get; private set; }

    public float CreatureProximity { get; private set; }

    public void AddPlant(float proximity)
    {
        PlantDensity = AddDensity(PlantDensity);
        PlantProximity = Math.Max(PlantProximity, ClampUnit(proximity));
    }

    public void AddMeat(float proximity)
    {
        MeatDensity = AddDensity(MeatDensity);
        MeatProximity = Math.Max(MeatProximity, ClampUnit(proximity));
    }

    public void AddEgg(float proximity)
    {
        EggDensity = AddDensity(EggDensity);
        EggProximity = Math.Max(EggProximity, ClampUnit(proximity));
    }

    public void AddCreature(float proximity)
    {
        CreatureDensity = AddDensity(CreatureDensity);
        CreatureProximity = Math.Max(CreatureProximity, ClampUnit(proximity));
    }

    private static float AddDensity(float density)
    {
        return Math.Clamp(density + DensityIncrement, 0f, 1f);
    }

    private static float ClampUnit(float value)
    {
        return Math.Clamp(value, 0f, 1f);
    }
}
