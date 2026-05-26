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
        Obstacles = ObstacleMap.CreateEmpty(bounds, MathF.Max(bounds.Width, bounds.Height));
        LocalFertility = LocalFertilityMap.CreateDisabled(bounds);
    }

    public long Tick { get; private set; }

    public double ElapsedSeconds { get; private set; }

    public WorldBounds Bounds { get; }

    public DeterministicRandom Random { get; }

    public BiomeMap Biomes { get; internal set; }

    public ObstacleMap Obstacles { get; internal set; }

    public LocalFertilityMap LocalFertility { get; internal set; }

    public SimulationStats Stats { get; } = new();

    public IReadOnlyList<CreatureLineageRecord> LineageRecords => _lineageRecords;

    public List<CreatureState> Creatures { get; } = [];

    public List<EggState> Eggs { get; } = [];

    public List<ResourcePatchState> Resources { get; } = [];

    public List<CreatureGenome> Genomes { get; } = [];

    public List<NeuralBrainGenome> Brains { get; } = [];

    public List<BrainArchitectureKind> BrainArchitectureKinds { get; } = [];

    internal long ResourceIndexVersion { get; private set; }

    internal long EggIndexVersion { get; private set; }

    internal SimulationProfile? Profile { get; set; }

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

    public bool TryGetGenome(int genomeId, out CreatureGenome genome)
    {
        if ((uint)genomeId < (uint)Genomes.Count)
        {
            genome = Genomes[genomeId];
            return true;
        }

        genome = default;
        return false;
    }

    public int AddBrain(
        NeuralBrainGenome brain,
        BrainArchitectureKind architectureKind = BrainArchitectureKind.HybridNeural)
    {
        ArgumentNullException.ThrowIfNull(brain);
        _ = BrainFactory.Describe(architectureKind);
        Brains.Add(brain);
        BrainArchitectureKinds.Add(architectureKind);
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

    public bool TryGetBrain(int brainId, out NeuralBrainGenome? brain)
    {
        if ((uint)brainId < (uint)Brains.Count)
        {
            brain = Brains[brainId];
            return true;
        }

        brain = null;
        return false;
    }

    public BrainArchitectureKind GetBrainArchitectureKind(int brainId)
    {
        if ((uint)brainId >= (uint)Brains.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(brainId), "Creature brain ID does not exist in this world.");
        }

        return (uint)brainId < (uint)BrainArchitectureKinds.Count
            ? BrainArchitectureKinds[brainId]
            : BrainArchitectureKind.HybridNeural;
    }

    public ExtinctPayloadPruneResult PruneExtinctPayloads()
    {
        var oldGenomeCount = Genomes.Count;
        var oldBrainCount = Brains.Count;
        var genomeMap = BuildRetainedPayloadMap(oldGenomeCount, EnumerateActiveGenomeIds(), "genome");
        var brainMap = BuildRetainedPayloadMap(oldBrainCount, EnumerateActiveBrainIds(), "brain");

        CompactGenomes(genomeMap);
        CompactBrains(brainMap);
        RemapActivePayloadReferences(genomeMap, brainMap);
        RemapLineagePayloadReferences(genomeMap, brainMap);

        return new ExtinctPayloadPruneResult(
            oldGenomeCount,
            Genomes.Count,
            oldBrainCount,
            Brains.Count,
            oldGenomeCount - Genomes.Count,
            oldBrainCount - Brains.Count);
    }

    public void SetBiomes(BiomeMap biomes)
    {
        if (biomes.Bounds.Width != Bounds.Width || biomes.Bounds.Height != Bounds.Height)
        {
            throw new ArgumentException("Biome map bounds must match the world bounds.", nameof(biomes));
        }

        Biomes = biomes;
        MarkResourcesDirty();
    }

    public void SetObstacles(ObstacleMap obstacles)
    {
        if (obstacles.Bounds.Width != Bounds.Width || obstacles.Bounds.Height != Bounds.Height)
        {
            throw new ArgumentException("Obstacle map bounds must match the world bounds.", nameof(obstacles));
        }

        Obstacles = obstacles;
    }

    public void SetLocalFertility(LocalFertilityMap localFertility)
    {
        if (localFertility.Bounds.Width != Bounds.Width || localFertility.Bounds.Height != Bounds.Height)
        {
            throw new ArgumentException("Local fertility map bounds must match the world bounds.", nameof(localFertility));
        }

        LocalFertility = localFertility;
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
        var clampedPosition = Bounds.Clamp(position);
        Creatures.Add(new CreatureState
        {
            Id = id,
            ParentId = parentId,
            Position = clampedPosition,
            MaxXReached = clampedPosition.X,
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
            BirthEnergy = energy,
            MaxXReached = clampedPosition.X
        });
        Stats.RecordEastwardProgress(clampedPosition.X);

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
        MarkEggsDirty();
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

    internal void MarkCreatureDead(EntityId id, CreatureDeathReason reason, BiomeKind deathBiome, float maxXReached)
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
        record.MaxXReached = Math.Max(record.MaxXReached, maxXReached);
        _lineageRecords[index] = record;
        var lifespanSeconds = MathF.Max(0f, (float)(record.DeathElapsedSeconds.Value - record.BirthElapsedSeconds));
        Stats.RecordCreatureDeath(reason, lifespanSeconds, deathBiome);
    }

    internal void RecordCreatureProgress(EntityId id, float maxXReached)
    {
        if (!float.IsFinite(maxXReached))
        {
            return;
        }

        Stats.RecordEastwardProgress(maxXReached);
        if (!_lineageRecordByEntityId.TryGetValue(id, out var index))
        {
            return;
        }

        var record = _lineageRecords[index];
        if (maxXReached <= record.MaxXReached)
        {
            return;
        }

        record.MaxXReached = maxXReached;
        _lineageRecords[index] = record;
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

        if (!float.IsFinite(patch.RespawnSecondsRemaining) || patch.RespawnSecondsRemaining < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(patch), "Resource respawn timer must be finite and non-negative.");
        }

        if (!float.IsFinite(patch.RespawnSecondsTotal) || patch.RespawnSecondsTotal < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(patch), "Resource respawn total timer must be finite and non-negative.");
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
        if (patch.Kind == ResourceKind.Plant)
        {
            patch.HabitatBiomeKind ??= Biomes.GetKindAt(patch.Position);
        }
        else
        {
            patch.HabitatBiomeKind = null;
        }

        if (patch.Kind == ResourceKind.Plant
            && patch.RespawnSecondsRemaining > 0f
            && patch.RespawnSecondsTotal <= 0f)
        {
            patch.RespawnSecondsTotal = patch.RespawnSecondsRemaining;
        }

        if (patch.Kind != ResourceKind.Meat)
        {
            patch.MeatAgeSeconds = 0f;
        }
        else
        {
            patch.RespawnSecondsRemaining = 0f;
            patch.RespawnSecondsTotal = 0f;
        }

        if (patch.Kind == ResourceKind.Plant && patch.Calories > 0f)
        {
            patch.RespawnSecondsRemaining = 0f;
            patch.RespawnSecondsTotal = 0f;
        }

        Resources.Add(patch);
        MarkResourcesDirty();
        return id;
    }

    internal void MarkResourcesDirty()
    {
        ResourceIndexVersion++;
    }

    internal void MarkEggsDirty()
    {
        EggIndexVersion++;
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

    private IEnumerable<int> EnumerateActiveGenomeIds()
    {
        foreach (var creature in Creatures)
        {
            yield return creature.GenomeId;
        }

        foreach (var egg in Eggs)
        {
            yield return egg.GenomeId;
        }
    }

    private IEnumerable<int> EnumerateActiveBrainIds()
    {
        foreach (var creature in Creatures)
        {
            if (creature.BrainId >= 0)
            {
                yield return creature.BrainId;
            }
        }

        foreach (var egg in Eggs)
        {
            if (egg.BrainId >= 0)
            {
                yield return egg.BrainId;
            }
        }
    }

    private static int[] BuildRetainedPayloadMap(int payloadCount, IEnumerable<int> referencedIds, string payloadName)
    {
        var retain = new bool[payloadCount];
        foreach (var id in referencedIds)
        {
            if ((uint)id >= (uint)payloadCount)
            {
                throw new InvalidOperationException($"Active entity references missing {payloadName} payload {id}.");
            }

            retain[id] = true;
        }

        var map = new int[payloadCount];
        Array.Fill(map, -1);
        var nextId = 0;
        for (var oldId = 0; oldId < retain.Length; oldId++)
        {
            if (retain[oldId])
            {
                map[oldId] = nextId++;
            }
        }

        return map;
    }

    private void CompactGenomes(IReadOnlyList<int> genomeMap)
    {
        if (Genomes.Count == 0)
        {
            return;
        }

        var oldGenomes = Genomes.ToArray();
        Genomes.Clear();
        for (var oldId = 0; oldId < oldGenomes.Length; oldId++)
        {
            if (genomeMap[oldId] >= 0)
            {
                Genomes.Add(oldGenomes[oldId]);
            }
        }
    }

    private void CompactBrains(IReadOnlyList<int> brainMap)
    {
        if (Brains.Count == 0)
        {
            BrainArchitectureKinds.Clear();
            return;
        }

        var oldBrains = Brains.ToArray();
        var oldKinds = BrainArchitectureKinds.ToArray();
        Brains.Clear();
        BrainArchitectureKinds.Clear();
        for (var oldId = 0; oldId < oldBrains.Length; oldId++)
        {
            if (brainMap[oldId] < 0)
            {
                continue;
            }

            Brains.Add(oldBrains[oldId]);
            BrainArchitectureKinds.Add((uint)oldId < (uint)oldKinds.Length
                ? oldKinds[oldId]
                : BrainArchitectureKind.HybridNeural);
        }
    }

    private void RemapActivePayloadReferences(IReadOnlyList<int> genomeMap, IReadOnlyList<int> brainMap)
    {
        for (var i = 0; i < Creatures.Count; i++)
        {
            var creature = Creatures[i];
            creature.GenomeId = RemapRequiredPayloadId(genomeMap, creature.GenomeId, "creature genome");
            if (creature.BrainId >= 0)
            {
                creature.BrainId = RemapRequiredPayloadId(brainMap, creature.BrainId, "creature brain");
            }

            Creatures[i] = creature;
        }

        for (var i = 0; i < Eggs.Count; i++)
        {
            var egg = Eggs[i];
            egg.GenomeId = RemapRequiredPayloadId(genomeMap, egg.GenomeId, "egg genome");
            if (egg.BrainId >= 0)
            {
                egg.BrainId = RemapRequiredPayloadId(brainMap, egg.BrainId, "egg brain");
            }

            Eggs[i] = egg;
        }
    }

    private void RemapLineagePayloadReferences(IReadOnlyList<int> genomeMap, IReadOnlyList<int> brainMap)
    {
        for (var i = 0; i < _lineageRecords.Count; i++)
        {
            var record = _lineageRecords[i];
            record.GenomeId = RemapOptionalPayloadId(genomeMap, record.GenomeId);
            record.BrainId = RemapOptionalPayloadId(brainMap, record.BrainId);
            _lineageRecords[i] = record;
        }
    }

    private static int RemapRequiredPayloadId(IReadOnlyList<int> map, int oldId, string description)
    {
        if ((uint)oldId >= (uint)map.Count || map[oldId] < 0)
        {
            throw new InvalidOperationException($"Active {description} payload {oldId} was not retained during pruning.");
        }

        return map[oldId];
    }

    private static int RemapOptionalPayloadId(IReadOnlyList<int> map, int oldId)
    {
        if ((uint)oldId >= (uint)map.Count)
        {
            return -1;
        }

        return map[oldId];
    }
}

public readonly record struct ExtinctPayloadPruneResult(
    int PreviousGenomeCount,
    int CurrentGenomeCount,
    int PreviousBrainCount,
    int CurrentBrainCount,
    int PrunedGenomeCount,
    int PrunedBrainCount);
