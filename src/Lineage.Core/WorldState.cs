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
        Temperature = TemperatureMap.CreateNeutral(bounds);
    }

    public long Tick { get; private set; }

    public double ElapsedSeconds { get; private set; }

    public WorldBounds Bounds { get; }

    public DeterministicRandom Random { get; }

    public BiomeMap Biomes { get; internal set; }

    public ObstacleMap Obstacles { get; internal set; }

    public LocalFertilityMap LocalFertility { get; internal set; }

    public TemperatureMap Temperature { get; internal set; }

    public SimulationStats Stats { get; } = new();

    public IReadOnlyList<CreatureLineageRecord> LineageRecords => _lineageRecords;

    public List<CreatureState> Creatures { get; } = [];

    public List<EggState> Eggs { get; } = [];

    public List<SmallPreyState> SmallPrey { get; } = [];

    public List<ResourcePatchState> Resources { get; } = [];

    public List<CreatureGenome> Genomes { get; } = [];

    public List<BrainGenome> Brains { get; } = [];

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
        return AddBrain(BrainGenome.FromNeural(architectureKind, brain));
    }

    public int AddBrain(BrainGenome brain)
    {
        ArgumentNullException.ThrowIfNull(brain);
        var validated = brain.Validated();
        Brains.Add(validated);
        BrainArchitectureKinds.Add(validated.ArchitectureKind);
        return Brains.Count - 1;
    }

    public BrainGenome GetBrain(int brainId)
    {
        if ((uint)brainId >= (uint)Brains.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(brainId), "Creature brain ID does not exist in this world.");
        }

        return Brains[brainId];
    }

    public bool TryGetBrain(int brainId, out BrainGenome? brain)
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
            : Brains[brainId].ArchitectureKind;
    }

    public ExtinctPayloadPruneResult PruneExtinctPayloads()
    {
        var oldGenomeCount = Genomes.Count;
        var oldBrainCount = Brains.Count;
        var survivorAncestorRecords = EnumerateSurvivorAncestryRecords().ToArray();
        var genomeMap = BuildRetainedPayloadMap(
            oldGenomeCount,
            EnumerateActiveGenomeIds().Concat(EnumerateLineageGenomeIds(survivorAncestorRecords)),
            "genome");
        var brainMap = BuildRetainedPayloadMap(
            oldBrainCount,
            EnumerateActiveBrainIds().Concat(EnumerateLineageBrainIds(survivorAncestorRecords)),
            "brain");

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

    public void SetTemperature(TemperatureMap temperature)
    {
        if (temperature.Bounds.Width != Bounds.Width || temperature.Bounds.Height != Bounds.Height)
        {
            throw new ArgumentException("Temperature map bounds must match the world bounds.", nameof(temperature));
        }

        Temperature = temperature;
    }

    public EntityId SpawnCreature(
        int genomeId,
        SimVector2 position,
        float energy,
        float health = 1f,
        int generation = 0,
        EntityId parentId = default,
        int brainId = -1,
        float birthInvestmentRatio = 1f,
        MutationProfile? birthMutationProfile = null)
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

        var mutationProfile = birthMutationProfile?.Validated() ?? default;
        var id = CreateEntityId();
        var clampedPosition = Bounds.Clamp(position);
        Creatures.Add(new CreatureState
        {
            Id = id,
            ParentId = parentId,
            Position = clampedPosition,
            PreviousPosition = clampedPosition,
            MaxXReached = clampedPosition.X,
            HeadingRadians = Random.NextSingle(0f, MathF.Tau),
            Senses = new CreatureSenseState { WorldSenseTick = -1 },
            LastNeuralDecisionTick = -1,
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
            BirthPosition = clampedPosition,
            BirthTemperature = Temperature.GetTemperatureAt(clampedPosition),
            Generation = generation,
            GenomeId = genomeId,
            BrainId = brainId,
            BirthEnergy = energy,
            BirthMutationStrength = mutationProfile.MutationStrength,
            BirthTraitMutationRate = mutationProfile.TraitMutationRate,
            BirthBrainMutationRate = mutationProfile.BrainMutationRate,
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
        int generation,
        MutationProfile? birthMutationProfile = null)
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

        var mutationProfile = birthMutationProfile?.Validated() ?? default;
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
            BrainId = brainId,
            BirthMutationStrength = mutationProfile.MutationStrength,
            BirthTraitMutationRate = mutationProfile.TraitMutationRate,
            BirthBrainMutationRate = mutationProfile.BrainMutationRate
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

    internal void MarkCreatureDead(
        EntityId id,
        CreatureDeathReason reason,
        BiomeKind deathBiome,
        SimVector2 deathPosition,
        float maxXReached,
        EntityId attackerId = default)
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
        record.DeathPosition = Bounds.Clamp(deathPosition);
        record.DeathTemperature = Temperature.GetTemperatureAt(record.DeathPosition.Value);
        record.DeathReason = reason;
        record.DeathAttackerId = attackerId;
        record.MaxXReached = Math.Max(record.MaxXReached, maxXReached);
        _lineageRecords[index] = record;
        var lifespanSeconds = MathF.Max(0f, (float)(record.DeathElapsedSeconds.Value - record.BirthElapsedSeconds));
        Stats.RecordCreatureDeath(reason, lifespanSeconds, deathBiome, Bounds, record.DeathPosition.Value);
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

    internal void RecordCreatureTelemetry(CreatureState creature, float deltaSeconds)
    {
        if (!_lineageRecordByEntityId.TryGetValue(creature.Id, out var index))
        {
            return;
        }

        var record = _lineageRecords[index];
        if (!record.IsAlive)
        {
            return;
        }

        var seconds = float.IsFinite(deltaSeconds) && deltaSeconds > 0f
            ? deltaSeconds
            : 0f;
        record.TelemetryLivingSeconds = AddTelemetry(record.TelemetryLivingSeconds, seconds);
        if (seconds > 0f && TryGetGenome(creature.GenomeId, out var genome))
        {
            var temperature = Temperature.GetTemperatureAt(creature.Position);
            var thermalMismatch = CreatureThermal.ThermalMismatch(temperature, genome);
            var thermalOptimum = CreatureThermal.NormalizeOptimum(genome.ThermalOptimum);
            record.TelemetryTemperatureExposure = AddTelemetry(record.TelemetryTemperatureExposure, temperature * seconds);
            record.TelemetryThermalMismatchExposure = AddTelemetry(record.TelemetryThermalMismatchExposure, thermalMismatch * seconds);
            switch (CreatureThermal.ClassifyTemperatureBand(temperature))
            {
                case TemperatureBand.Cold:
                    record.TelemetryColdTemperatureSeconds = AddTelemetry(record.TelemetryColdTemperatureSeconds, seconds);
                    break;
                case TemperatureBand.Hot:
                    record.TelemetryHotTemperatureSeconds = AddTelemetry(record.TelemetryHotTemperatureSeconds, seconds);
                    break;
                default:
                    record.TelemetryTemperateTemperatureSeconds = AddTelemetry(record.TelemetryTemperateTemperatureSeconds, seconds);
                    break;
            }

            if (thermalMismatch < CreatureThermal.ThermalStressMismatchThreshold)
            {
                record.TelemetryComfortableThermalSeconds = AddTelemetry(record.TelemetryComfortableThermalSeconds, seconds);
            }
            else if (temperature < thermalOptimum)
            {
                record.TelemetryColdThermalStressSeconds = AddTelemetry(record.TelemetryColdThermalStressSeconds, seconds);
            }
            else
            {
                record.TelemetryHotThermalStressSeconds = AddTelemetry(record.TelemetryHotThermalStressSeconds, seconds);
            }
        }

        record.TelemetryEatingSeconds = AddTelemetrySeconds(
            record.TelemetryEatingSeconds,
            seconds,
            creature.LastCaloriesEaten > 0f);
        record.TelemetryMeatEatingSeconds = AddTelemetrySeconds(
            record.TelemetryMeatEatingSeconds,
            seconds,
            creature.LastCarcassCaloriesEaten + creature.LastEggCaloriesEaten + creature.LastLivePreyCaloriesEaten > 0f);
        record.TelemetryFoodContactSeconds = AddTelemetrySeconds(
            record.TelemetryFoodContactSeconds,
            seconds,
            creature.IsTouchingFood);
        record.TelemetryCreatureContactSeconds = AddTelemetrySeconds(
            record.TelemetryCreatureContactSeconds,
            seconds,
            creature.IsTouchingCreature);
        record.TelemetrySimilarCreatureContactSeconds = AddTelemetrySeconds(
            record.TelemetrySimilarCreatureContactSeconds,
            seconds,
            creature.IsTouchingCreature
                && creature.Senses.CreatureContactSimilarity >= CreatureSimilarity.SimilarContactThreshold);
        record.TelemetryLineageCreatureContactSeconds = AddTelemetrySeconds(
            record.TelemetryLineageCreatureContactSeconds,
            seconds,
            creature.IsTouchingCreature
                && creature.Senses.CreatureContactLineageSimilarity >= LineageFamiliarity.SameLineageThreshold);
        record.TelemetryEggLineageContactSeconds = AddTelemetrySeconds(
            record.TelemetryEggLineageContactSeconds,
            seconds,
            creature.Senses.EggFoodContact > 0f
                && creature.Senses.EggContactLineageSimilarity >= LineageFamiliarity.SameLineageThreshold);
        record.TelemetryAttackIntentSeconds = AddTelemetrySeconds(
            record.TelemetryAttackIntentSeconds,
            seconds,
            creature.Actions.WantsAttack);
        record.TelemetryAttackIntentTouchingSeconds = AddTelemetrySeconds(
            record.TelemetryAttackIntentTouchingSeconds,
            seconds,
            creature.Actions.WantsAttack && creature.IsTouchingCreature);
        record.TelemetryAttackIntentLineageTouchingSeconds = AddTelemetrySeconds(
            record.TelemetryAttackIntentLineageTouchingSeconds,
            seconds,
            creature.Actions.WantsAttack
                && creature.IsTouchingCreature
                && creature.Senses.CreatureContactLineageSimilarity >= LineageFamiliarity.SameLineageThreshold);
        record.TelemetryAttackIntentUnrelatedTouchingSeconds = AddTelemetrySeconds(
            record.TelemetryAttackIntentUnrelatedTouchingSeconds,
            seconds,
            creature.Actions.WantsAttack
                && creature.IsTouchingCreature
                && creature.Senses.CreatureContactLineageSimilarity < LineageFamiliarity.SameLineageThreshold);
        record.TelemetryAttackDamageDealingSeconds = AddTelemetrySeconds(
            record.TelemetryAttackDamageDealingSeconds,
            seconds,
            creature.LastAttackDamageDealt > 0f);
        record.TelemetryMeatDetectedSeconds = AddTelemetrySeconds(
            record.TelemetryMeatDetectedSeconds,
            seconds,
            creature.Senses.MeatDetected);
        record.TelemetryFreshMeatDetectedSeconds = AddTelemetrySeconds(
            record.TelemetryFreshMeatDetectedSeconds,
            seconds,
            creature.Senses.MeatDetected && MeatQuality.IsFresh(creature.Senses.VisibleMeatFreshness));
        record.TelemetryStaleMeatDetectedSeconds = AddTelemetrySeconds(
            record.TelemetryStaleMeatDetectedSeconds,
            seconds,
            creature.Senses.MeatDetected && !MeatQuality.IsFresh(creature.Senses.VisibleMeatFreshness));
        record.TelemetryRottenMeatScentDetectedSeconds = AddTelemetrySeconds(
            record.TelemetryRottenMeatScentDetectedSeconds,
            seconds,
            creature.Senses.RottenMeatScentDetected);

        record.TelemetryCaloriesEaten = AddTelemetry(record.TelemetryCaloriesEaten, creature.LastCaloriesEaten);
        record.TelemetryPlantCaloriesEaten = AddTelemetry(record.TelemetryPlantCaloriesEaten, creature.LastPlantCaloriesEaten);
        record.TelemetryCarcassCaloriesEaten = AddTelemetry(record.TelemetryCarcassCaloriesEaten, creature.LastCarcassCaloriesEaten);
        record.TelemetryEggCaloriesEaten = AddTelemetry(record.TelemetryEggCaloriesEaten, creature.LastEggCaloriesEaten);
        record.TelemetryFreshKillCaloriesEaten = AddTelemetry(record.TelemetryFreshKillCaloriesEaten, creature.LastLivePreyCaloriesEaten);
        record.TelemetrySmallPreyCaloriesEaten = AddTelemetry(record.TelemetrySmallPreyCaloriesEaten, creature.LastSmallPreyCaloriesEaten);
        record.TelemetryFreshMeatCaloriesEaten = AddTelemetry(record.TelemetryFreshMeatCaloriesEaten, creature.LastFreshMeatCaloriesEaten);
        record.TelemetryStaleMeatCaloriesEaten = AddTelemetry(record.TelemetryStaleMeatCaloriesEaten, creature.LastStaleMeatCaloriesEaten);
        record.TelemetryRottenMeatDamage = AddTelemetry(record.TelemetryRottenMeatDamage, creature.LastRottenMeatDamage);
        record.TelemetryAttackDamageDealt = AddTelemetry(record.TelemetryAttackDamageDealt, creature.LastAttackDamageDealt);
        record.TelemetryAttackDamageTaken = AddTelemetry(record.TelemetryAttackDamageTaken, creature.LastAttackDamageTaken);

        _lineageRecords[index] = record;
    }

    public EntityId SpawnSmallPrey(SmallPreyState prey)
    {
        if (!prey.Position.IsFinite)
        {
            throw new ArgumentOutOfRangeException(nameof(prey), "Small prey position must be finite.");
        }

        if (!prey.Velocity.IsFinite)
        {
            throw new ArgumentOutOfRangeException(nameof(prey), "Small prey velocity must be finite.");
        }

        if (!float.IsFinite(prey.HeadingRadians))
        {
            throw new ArgumentOutOfRangeException(nameof(prey), "Small prey heading must be finite.");
        }

        if (!float.IsFinite(prey.Radius) || prey.Radius <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(prey), "Small prey radius must be finite and positive.");
        }

        if (!float.IsFinite(prey.Calories) || prey.Calories <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(prey), "Small prey calories must be finite and positive.");
        }

        if (!float.IsFinite(prey.MaxCalories) || prey.MaxCalories <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(prey), "Small prey max calories must be finite and positive.");
        }

        if (!float.IsFinite(prey.Health) || prey.Health <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(prey), "Small prey health must be finite and positive.");
        }

        if (!float.IsFinite(prey.MaxHealth) || prey.MaxHealth <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(prey), "Small prey max health must be finite and positive.");
        }

        if (!float.IsFinite(prey.AgeSeconds) || prey.AgeSeconds < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(prey), "Small prey age must be finite and non-negative.");
        }

        if (!float.IsFinite(prey.WanderSecondsRemaining) || prey.WanderSecondsRemaining < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(prey), "Small prey wander timer must be finite and non-negative.");
        }

        if (!float.IsFinite(prey.GrabPressure) || prey.GrabPressure < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(prey), "Small prey grab pressure must be finite and non-negative.");
        }

        var id = CreateEntityId();
        prey.Id = id;
        prey.Position = Bounds.Clamp(prey.Position);
        prey.Calories = Math.Min(prey.Calories, prey.MaxCalories);
        prey.Health = Math.Min(prey.Health, prey.MaxHealth);
        prey.GrabPressure = Math.Clamp(prey.GrabPressure, 0f, 1f);
        SmallPrey.Add(prey);
        Stats.RecordSmallPreySpawned();
        return id;
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
        Stats.RecordCreatureBirth(record, Bounds);
    }

    private static float AddTelemetry(float current, float value)
    {
        return float.IsFinite(value) && value > 0f
            ? current + value
            : current;
    }

    private static float AddTelemetrySeconds(float current, float seconds, bool condition)
    {
        return condition
            ? AddTelemetry(current, seconds)
            : current;
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

    private IEnumerable<CreatureLineageRecord> EnumerateSurvivorAncestryRecords()
    {
        var descendantIds = Creatures
            .Select(creature => creature.Id)
            .Concat(Eggs.Select(egg => egg.ParentId))
            .Where(id => id != default);
        var ancestorIds = SurvivorLineageAnalyzer.CollectAncestorIds(_lineageRecords, descendantIds);
        foreach (var id in ancestorIds)
        {
            if (TryGetLineageRecord(id, out var record))
            {
                yield return record;
            }
        }
    }

    private static IEnumerable<int> EnumerateLineageGenomeIds(IEnumerable<CreatureLineageRecord> records)
    {
        foreach (var record in records)
        {
            if (record.GenomeId >= 0)
            {
                yield return record.GenomeId;
            }
        }
    }

    private static IEnumerable<int> EnumerateLineageBrainIds(IEnumerable<CreatureLineageRecord> records)
    {
        foreach (var record in records)
        {
            if (record.BrainId >= 0)
            {
                yield return record.BrainId;
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
                : oldBrains[oldId].ArchitectureKind);
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
