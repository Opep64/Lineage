namespace Lineage.Core;

public sealed record SimulationSpatialHeatmapSnapshot
{
    public float WorldWidth { get; init; }

    public float WorldHeight { get; init; }

    public int CellCountX { get; init; }

    public int CellCountY { get; init; }

    public float[] Births { get; init; } = [];

    public float[] Deaths { get; init; } = [];

    public float[] StarvationDeaths { get; init; } = [];

    public float[] InjuryDeaths { get; init; } = [];

    public float[] RottenMeatDeaths { get; init; } = [];

    public float[] OldAgeDeaths { get; init; } = [];

    public float[] UnknownDeaths { get; init; } = [];

    public float[] PlantCaloriesEaten { get; init; } = [];

    public float[] MeatCaloriesEaten { get; init; } = [];

    public float[] EggCaloriesEaten { get; init; } = [];

    public float[] AttackDamage { get; init; } = [];

    public float[] CreatureExposureSeconds { get; init; } = [];

    public float[] BiomeCreatureExposureSeconds { get; init; } = [];
}

public sealed class SimulationSpatialHeatmaps
{
    private const int MaximumGridAxisCells = 48;

    private float[] _births = [];
    private float[] _deaths = [];
    private float[] _starvationDeaths = [];
    private float[] _injuryDeaths = [];
    private float[] _rottenMeatDeaths = [];
    private float[] _oldAgeDeaths = [];
    private float[] _unknownDeaths = [];
    private float[] _plantCaloriesEaten = [];
    private float[] _meatCaloriesEaten = [];
    private float[] _eggCaloriesEaten = [];
    private float[] _attackDamage = [];
    private float[] _creatureExposureSeconds = [];
    private float[] _biomeCreatureExposureSeconds = [];

    public float WorldWidth { get; private set; }

    public float WorldHeight { get; private set; }

    public int CellCountX { get; private set; }

    public int CellCountY { get; private set; }

    public IReadOnlyList<float> Births => _births;

    public IReadOnlyList<float> Deaths => _deaths;

    public IReadOnlyList<float> StarvationDeaths => _starvationDeaths;

    public IReadOnlyList<float> InjuryDeaths => _injuryDeaths;

    public IReadOnlyList<float> RottenMeatDeaths => _rottenMeatDeaths;

    public IReadOnlyList<float> OldAgeDeaths => _oldAgeDeaths;

    public IReadOnlyList<float> UnknownDeaths => _unknownDeaths;

    public IReadOnlyList<float> PlantCaloriesEaten => _plantCaloriesEaten;

    public IReadOnlyList<float> MeatCaloriesEaten => _meatCaloriesEaten;

    public IReadOnlyList<float> EggCaloriesEaten => _eggCaloriesEaten;

    public IReadOnlyList<float> AttackDamage => _attackDamage;

    public IReadOnlyList<float> CreatureExposureSeconds => _creatureExposureSeconds;

    public IReadOnlyList<float> BiomeCreatureExposureSeconds => _biomeCreatureExposureSeconds;

    public bool HasExposure =>
        HasAny(_creatureExposureSeconds)
        || HasAny(_biomeCreatureExposureSeconds);

    public bool HasData =>
        HasAny(_births)
        || HasAny(_deaths)
        || HasAny(_plantCaloriesEaten)
        || HasAny(_meatCaloriesEaten)
        || HasAny(_eggCaloriesEaten)
        || HasAny(_attackDamage)
        || HasExposure;

    public void RecordBirth(WorldBounds bounds, SimVector2 position)
    {
        EnsureInitialized(bounds);
        Add(bounds, position, _births, 1f);
    }

    public void RecordDeath(WorldBounds bounds, SimVector2 position, CreatureDeathReason reason)
    {
        EnsureInitialized(bounds);
        Add(bounds, position, _deaths, 1f);
        Add(
            bounds,
            position,
            reason switch
            {
                CreatureDeathReason.Starvation => _starvationDeaths,
                CreatureDeathReason.Injury => _injuryDeaths,
                CreatureDeathReason.RottenMeat => _rottenMeatDeaths,
                CreatureDeathReason.OldAge => _oldAgeDeaths,
                _ => _unknownDeaths
            },
            1f);
    }

    public void RecordFoodEaten(WorldBounds bounds, SimVector2 position, ResourceKind kind, float calories)
    {
        EnsureInitialized(bounds);
        Add(bounds, position, kind == ResourceKind.Meat ? _meatCaloriesEaten : _plantCaloriesEaten, calories);
    }

    public void RecordEggEaten(WorldBounds bounds, SimVector2 position, float calories)
    {
        EnsureInitialized(bounds);
        Add(bounds, position, _eggCaloriesEaten, calories);
    }

    public void RecordAttackDamage(WorldBounds bounds, SimVector2 position, float damage)
    {
        EnsureInitialized(bounds);
        Add(bounds, position, _attackDamage, damage);
    }

    public void RecordCreatureExposure(WorldBounds bounds, SimVector2 position, BiomeKind biome, float seconds)
    {
        EnsureInitialized(bounds);
        Add(bounds, position, _creatureExposureSeconds, seconds);
        AddBiomeExposure(biome, seconds);
    }

    public SimulationSpatialHeatmapSnapshot ToSnapshot()
    {
        return new SimulationSpatialHeatmapSnapshot
        {
            WorldWidth = WorldWidth,
            WorldHeight = WorldHeight,
            CellCountX = CellCountX,
            CellCountY = CellCountY,
            Births = _births.ToArray(),
            Deaths = _deaths.ToArray(),
            StarvationDeaths = _starvationDeaths.ToArray(),
            InjuryDeaths = _injuryDeaths.ToArray(),
            RottenMeatDeaths = _rottenMeatDeaths.ToArray(),
            OldAgeDeaths = _oldAgeDeaths.ToArray(),
            UnknownDeaths = _unknownDeaths.ToArray(),
            PlantCaloriesEaten = _plantCaloriesEaten.ToArray(),
            MeatCaloriesEaten = _meatCaloriesEaten.ToArray(),
            EggCaloriesEaten = _eggCaloriesEaten.ToArray(),
            AttackDamage = _attackDamage.ToArray(),
            CreatureExposureSeconds = _creatureExposureSeconds.ToArray(),
            BiomeCreatureExposureSeconds = _biomeCreatureExposureSeconds.ToArray()
        };
    }

    public void Restore(SimulationSpatialHeatmapSnapshot? snapshot)
    {
        if (snapshot is null
            || snapshot.CellCountX <= 0
            || snapshot.CellCountY <= 0
            || !float.IsFinite(snapshot.WorldWidth)
            || !float.IsFinite(snapshot.WorldHeight)
            || snapshot.WorldWidth <= 0f
            || snapshot.WorldHeight <= 0f)
        {
            Clear();
            return;
        }

        var expectedLength = snapshot.CellCountX * snapshot.CellCountY;
        if (!HasLength(snapshot.Births, expectedLength)
            || !HasLength(snapshot.Deaths, expectedLength)
            || !HasLength(snapshot.StarvationDeaths, expectedLength)
            || !HasLength(snapshot.InjuryDeaths, expectedLength)
            || !HasLength(snapshot.RottenMeatDeaths, expectedLength)
            || !HasLength(snapshot.UnknownDeaths, expectedLength)
            || !HasLength(snapshot.PlantCaloriesEaten, expectedLength)
            || !HasLength(snapshot.MeatCaloriesEaten, expectedLength)
            || !HasLength(snapshot.EggCaloriesEaten, expectedLength)
            || !HasLength(snapshot.AttackDamage, expectedLength))
        {
            Clear();
            return;
        }

        WorldWidth = snapshot.WorldWidth;
        WorldHeight = snapshot.WorldHeight;
        CellCountX = snapshot.CellCountX;
        CellCountY = snapshot.CellCountY;
        _births = NormalizeValues(snapshot.Births);
        _deaths = NormalizeValues(snapshot.Deaths);
        _starvationDeaths = NormalizeValues(snapshot.StarvationDeaths);
        _injuryDeaths = NormalizeValues(snapshot.InjuryDeaths);
        _rottenMeatDeaths = NormalizeValues(snapshot.RottenMeatDeaths);
        _oldAgeDeaths = NormalizeValues(snapshot.OldAgeDeaths, expectedLength);
        _unknownDeaths = NormalizeValues(snapshot.UnknownDeaths);
        _plantCaloriesEaten = NormalizeValues(snapshot.PlantCaloriesEaten);
        _meatCaloriesEaten = NormalizeValues(snapshot.MeatCaloriesEaten);
        _eggCaloriesEaten = NormalizeValues(snapshot.EggCaloriesEaten);
        _attackDamage = NormalizeValues(snapshot.AttackDamage);
        _creatureExposureSeconds = NormalizeValues(snapshot.CreatureExposureSeconds, expectedLength);
        _biomeCreatureExposureSeconds = NormalizeValues(snapshot.BiomeCreatureExposureSeconds, BiomeKinds.All.Count);
    }

    private void Add(WorldBounds bounds, SimVector2 position, float[] values, float amount)
    {
        if (!float.IsFinite(amount) || amount <= 0f || !position.IsFinite)
        {
            return;
        }
        if (values.Length == 0 || !BoundsMatch(bounds))
        {
            return;
        }

        values[CellIndex(position)] += amount;
    }

    private void AddBiomeExposure(BiomeKind biome, float seconds)
    {
        if (!float.IsFinite(seconds) || seconds <= 0f || _biomeCreatureExposureSeconds.Length == 0)
        {
            return;
        }

        _biomeCreatureExposureSeconds[BiomeIndex(biome)] += seconds;
    }

    private void EnsureInitialized(WorldBounds bounds)
    {
        if (CellCountX > 0 && CellCountY > 0)
        {
            return;
        }

        var width = NormalizeDimension(bounds.Width);
        var height = NormalizeDimension(bounds.Height);
        WorldWidth = width;
        WorldHeight = height;
        if (width >= height)
        {
            CellCountX = MaximumGridAxisCells;
            CellCountY = Math.Max(1, (int)MathF.Round(MaximumGridAxisCells * height / width));
        }
        else
        {
            CellCountY = MaximumGridAxisCells;
            CellCountX = Math.Max(1, (int)MathF.Round(MaximumGridAxisCells * width / height));
        }

        var length = CellCountX * CellCountY;
        _births = new float[length];
        _deaths = new float[length];
        _starvationDeaths = new float[length];
        _injuryDeaths = new float[length];
        _rottenMeatDeaths = new float[length];
        _oldAgeDeaths = new float[length];
        _unknownDeaths = new float[length];
        _plantCaloriesEaten = new float[length];
        _meatCaloriesEaten = new float[length];
        _eggCaloriesEaten = new float[length];
        _attackDamage = new float[length];
        _creatureExposureSeconds = new float[length];
        _biomeCreatureExposureSeconds = new float[BiomeKinds.All.Count];
    }

    private int CellIndex(SimVector2 position)
    {
        var x = Math.Clamp((int)MathF.Floor(position.X / WorldWidth * CellCountX), 0, CellCountX - 1);
        var y = Math.Clamp((int)MathF.Floor(position.Y / WorldHeight * CellCountY), 0, CellCountY - 1);
        return y * CellCountX + x;
    }

    private bool BoundsMatch(WorldBounds bounds)
    {
        return Math.Abs(NormalizeDimension(bounds.Width) - WorldWidth) <= 0.001f
            && Math.Abs(NormalizeDimension(bounds.Height) - WorldHeight) <= 0.001f;
    }

    private void Clear()
    {
        WorldWidth = 0f;
        WorldHeight = 0f;
        CellCountX = 0;
        CellCountY = 0;
        _births = [];
        _deaths = [];
        _starvationDeaths = [];
        _injuryDeaths = [];
        _rottenMeatDeaths = [];
        _oldAgeDeaths = [];
        _unknownDeaths = [];
        _plantCaloriesEaten = [];
        _meatCaloriesEaten = [];
        _eggCaloriesEaten = [];
        _attackDamage = [];
        _creatureExposureSeconds = [];
        _biomeCreatureExposureSeconds = [];
    }

    private static float NormalizeDimension(float value)
    {
        return float.IsFinite(value) && value > 0f ? value : 1f;
    }

    private static bool HasLength(float[]? values, int length)
    {
        return values is not null && values.Length == length;
    }

    private static bool HasAny(float[] values)
    {
        return values.Any(static value => value > 0f);
    }

    private static int BiomeIndex(BiomeKind biome)
    {
        var canonical = BiomeKinds.Canonicalize(biome);
        for (var i = 0; i < BiomeKinds.All.Count; i++)
        {
            if (BiomeKinds.All[i] == canonical)
            {
                return i;
            }
        }

        return 0;
    }

    private static float[] NormalizeValues(float[] values)
    {
        return values
            .Select(static value => float.IsFinite(value) && value > 0f ? value : 0f)
            .ToArray();
    }

    private static float[] NormalizeValues(float[]? values, int expectedLength)
    {
        var normalized = new float[expectedLength];
        if (values is null)
        {
            return normalized;
        }

        var length = Math.Min(values.Length, expectedLength);
        for (var i = 0; i < length; i++)
        {
            var value = values[i];
            normalized[i] = float.IsFinite(value) && value > 0f ? value : 0f;
        }

        return normalized;
    }
}
