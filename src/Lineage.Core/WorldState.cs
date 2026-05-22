namespace Lineage.Core;

/// <summary>
/// Mutable state for a single simulation world.
/// </summary>
///
/// <remarks>
/// The public collections are intentionally simple in Phase 1. Systems should mutate
/// entries by index, which lets us replace the backing storage later without changing
/// the high-level update model.
/// </remarks>
public sealed class WorldState
{
    private int _nextEntityId = 1;
    private readonly List<CreatureLineageRecord> _lineageRecords = [];
    private readonly Dictionary<EntityId, int> _lineageRecordByEntityId = [];

    internal WorldState(WorldBounds bounds, ulong seed)
    {
        Bounds = bounds;
        Random = new DeterministicRandom(seed);
        Biomes = BiomeMap.CreateUniform(bounds, MathF.Max(bounds.Width, bounds.Height), BiomeKind.Grassland);
    }

    public long Tick { get; private set; }

    public double ElapsedSeconds { get; private set; }

    public WorldBounds Bounds { get; }

    public DeterministicRandom Random { get; }

    public BiomeMap Biomes { get; internal set; }

    public SimulationStats Stats { get; } = new();

    public IReadOnlyList<CreatureLineageRecord> LineageRecords => _lineageRecords;

    public List<CreatureState> Creatures { get; } = [];

    public List<EggState> Eggs { get; } = [];

    public List<ResourcePatchState> Resources { get; } = [];

    public List<CreatureGenome> Genomes { get; } = [];

    public List<NeuralBrainGenome> Brains { get; } = [];

    public EntityId CreateEntityId()
    {
        return new EntityId(_nextEntityId++);
    }

    internal int NextEntityId => _nextEntityId;

    public int AddGenome(CreatureGenome genome)
    {
        Genomes.Add(genome.Validated());
        return Genomes.Count - 1;
    }

    public CreatureGenome GetGenome(int genomeId)
    {
        if ((uint)genomeId >= (uint)Genomes.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(genomeId), "Creature genome ID does not exist in this world.");
        }

        return Genomes[genomeId];
    }

    public int AddBrain(NeuralBrainGenome brain)
    {
        Brains.Add(brain);
        return Brains.Count - 1;
    }

    public NeuralBrainGenome GetBrain(int brainId)
    {
        if ((uint)brainId >= (uint)Brains.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(brainId), "Creature brain ID does not exist in this world.");
        }

        return Brains[brainId];
    }

    public EntityId SpawnCreature(
        int genomeId,
        SimVector2 position,
        float energy,
        float health = 1f,
        int generation = 0,
        EntityId parentId = default,
        int brainId = -1,
        float birthInvestmentRatio = 1f)
    {
        _ = GetGenome(genomeId);
        if (brainId >= 0)
        {
            _ = GetBrain(brainId);
        }

        if (!position.IsFinite)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Creature position must be finite.");
        }

        if (!float.IsFinite(energy) || energy <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(energy), "Creature energy must be finite and positive.");
        }

        if (!float.IsFinite(health) || health <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(health), "Creature health must be finite and positive.");
        }

        var id = CreateEntityId();
        Creatures.Add(new CreatureState
        {
            Id = id,
            ParentId = parentId,
            Position = Bounds.Clamp(position),
            HeadingRadians = Random.NextSingle(0f, MathF.Tau),
            Energy = energy,
            Health = health,
            BirthInvestmentRatio = OffspringDevelopment.NormalizeInvestmentRatio(birthInvestmentRatio),
            GenomeId = genomeId,
            BrainId = brainId,
            Generation = generation
        });

        RecordCreatureBirth(new CreatureLineageRecord
        {
            Id = id,
            ParentId = parentId,
            BirthTick = Tick,
            BirthElapsedSeconds = ElapsedSeconds,
            Generation = generation,
            GenomeId = genomeId,
            BrainId = brainId,
            BirthEnergy = energy
        });

        return id;
    }

    public EntityId SpawnEgg(
        int genomeId,
        int brainId,
        EntityId parentId,
        SimVector2 position,
        float energy,
        float incubationSeconds,
        int generation)
    {
        _ = GetGenome(genomeId);
        if (brainId >= 0)
        {
            _ = GetBrain(brainId);
        }

        if (parentId == default)
        {
            throw new ArgumentException("Egg parent ID cannot be default.", nameof(parentId));
        }

        if (!position.IsFinite)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Egg position must be finite.");
        }

        if (!float.IsFinite(energy) || energy <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(energy), "Egg energy must be finite and positive.");
        }

        if (!float.IsFinite(incubationSeconds) || incubationSeconds < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(incubationSeconds), "Egg incubation seconds must be finite and non-negative.");
        }

        var investmentRatio = OffspringDevelopment.InvestmentRatio(energy);
        var maxHealth = OffspringDevelopment.EggMaxHealth(investmentRatio);
        var id = CreateEntityId();
        Eggs.Add(new EggState
        {
            Id = id,
            ParentId = parentId,
            Position = Bounds.Clamp(position),
            Energy = energy,
            Health = maxHealth,
            MaxHealth = maxHealth,
            InvestmentRatio = investmentRatio,
            IncubationSeconds = incubationSeconds,
            Generation = generation,
            GenomeId = genomeId,
            BrainId = brainId
        });
        Stats.RecordEggLaid();
        return id;
    }

    public bool TryGetLineageRecord(EntityId id, out CreatureLineageRecord record)
    {
        if (_lineageRecordByEntityId.TryGetValue(id, out var index))
        {
            record = _lineageRecords[index];
            return true;
        }

        record = default;
        return false;
    }

    internal void MarkCreatureDead(EntityId id, CreatureDeathReason reason)
    {
        if (!_lineageRecordByEntityId.TryGetValue(id, out var index))
        {
            return;
        }

        var record = _lineageRecords[index];
        if (!record.IsAlive)
        {
            return;
        }

        record.DeathTick = Tick;
        record.DeathElapsedSeconds = ElapsedSeconds;
        record.DeathReason = reason;
        _lineageRecords[index] = record;
        Stats.RecordCreatureDeath(reason);
    }

    public EntityId SpawnResourcePatch(ResourcePatchState patch)
    {
        if (!patch.Position.IsFinite)
        {
            throw new ArgumentOutOfRangeException(nameof(patch), "Resource position must be finite.");
        }

        if (!float.IsFinite(patch.Radius) || patch.Radius <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(patch), "Resource radius must be finite and positive.");
        }

        if (!float.IsFinite(patch.MaxCalories) || patch.MaxCalories <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(patch), "Resource max calories must be finite and positive.");
        }

        if (!float.IsFinite(patch.Calories) || patch.Calories < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(patch), "Resource calories must be finite and non-negative.");
        }

        if (!float.IsFinite(patch.RegrowthCaloriesPerSecond) || patch.RegrowthCaloriesPerSecond < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(patch), "Resource regrowth must be finite and non-negative.");
        }

        if (!float.IsFinite(patch.DecayCaloriesPerSecond) || patch.DecayCaloriesPerSecond < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(patch), "Resource decay must be finite and non-negative.");
        }

        if (!float.IsFinite(patch.FreshKillSecondsRemaining) || patch.FreshKillSecondsRemaining < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(patch), "Resource fresh-kill timer must be finite and non-negative.");
        }

        if (!float.IsFinite(patch.MeatAgeSeconds) || patch.MeatAgeSeconds < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(patch), "Resource meat age must be finite and non-negative.");
        }

        var id = CreateEntityId();
        patch.Id = id;
        patch.Position = Bounds.Clamp(patch.Position);
        patch.Calories = Math.Min(patch.Calories, patch.MaxCalories);
        if (patch.Kind != ResourceKind.Meat)
        {
            patch.MeatAgeSeconds = 0f;
        }

        Resources.Add(patch);
        return id;
    }

    internal void AdvanceClock(float deltaSeconds)
    {
        Tick++;
        ElapsedSeconds += deltaSeconds;
    }

    internal void RestoreClock(long tick, double elapsedSeconds)
    {
        if (tick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tick), "Tick cannot be negative.");
        }

        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "Elapsed seconds must be finite and non-negative.");
        }

        Tick = tick;
        ElapsedSeconds = elapsedSeconds;
    }

    internal void RestoreNextEntityId(int nextEntityId)
    {
        if (nextEntityId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nextEntityId), "Next entity ID must be positive.");
        }

        _nextEntityId = nextEntityId;
    }

    internal void RestoreLineageRecords(IEnumerable<CreatureLineageRecord> records)
    {
        _lineageRecords.Clear();
        _lineageRecordByEntityId.Clear();

        foreach (var record in records)
        {
            if (record.Id == default)
            {
                throw new InvalidOperationException("Lineage record ID cannot be default.");
            }

            _lineageRecordByEntityId.Add(record.Id, _lineageRecords.Count);
            _lineageRecords.Add(record);
        }
    }

    private void RecordCreatureBirth(CreatureLineageRecord record)
    {
        _lineageRecordByEntityId.Add(record.Id, _lineageRecords.Count);
        _lineageRecords.Add(record);
        Stats.RecordCreatureBirth(record);
    }
}
