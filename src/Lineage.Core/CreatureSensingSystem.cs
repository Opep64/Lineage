using System.Diagnostics;

namespace Lineage.Core;

/// <summary>
/// Converts nearby world state and internal state into explicit creature senses.
/// </summary>
public sealed class CreatureSensingSystem : ISimulationSystem
{
    private const float DensitySaturationFoodCount = 8f;
    private const float DirectionEpsilonSquared = 0.00000001f;
    private const float MinimumScentStrength = 0.001f;
    private const float MinimumTerrainProbeDistance = 24f;
    private const float MaximumTerrainProbeDistance = 160f;
    private const float MinimumHabitatProbeDistance = 16f;
    private const float MaximumHabitatProbeDistance = 80f;
    private const int ObstacleProbeSteps = 4;
    private const float MinimumExpectedFoodTransfer = 0.001f;
    private const float MinimumExpectedPlantDigestiveYield = 0.001f;
    private const float MinimumPlantQualityClarity = 0.04f;
    public const float CreatureSimilarityScentRangeMultiplier = 1.5f;
    private const float CreatureSimilarityScentDensitySaturation = 1f;
    public const int DefaultWorldSenseIntervalTicks = 10;
    public const float DefaultSoundRangeMultiplier = 2.5f;
    public const float DefaultSoundDensitySaturation = 1f;
    public const float DefaultCloseSenseRefreshProximity = 0.85f;
    public const int DefaultCloseSenseRefreshMinimumTicks = 1;
    public const bool DefaultEnableSectorVision = false;
    public const float DefaultPlantPayoffTraceHalfLifeSeconds = 45f;
    public const int DefaultSensingThreadCount = 4;
    private static readonly float MaximumHabitatDensityMultiplier =
        BiomeKinds.All.Max(BiomeMap.GetResourceDensityMultiplier);
    private static readonly float MaximumHabitatRegrowthMultiplier =
        BiomeKinds.All.Max(BiomeMap.GetResourceRegrowthMultiplier);

    private readonly UniformSpatialIndex _spatialIndex;
    private readonly BiomePressureProfile _biomeSpeedProfile;
    private readonly BiomePressureProfile _biomeVisionRangeProfile;
    private readonly bool _hasUniformBiomeSpeedProfile;
    private readonly float _uniformBiomeDrag;
    private readonly float _meatScentRangeMultiplier;
    private readonly float _meatScentCaloriesForFullStrength;
    private readonly float _meatScentDensitySaturation;
    private readonly float _soundRangeMultiplier;
    private readonly float _soundDensitySaturation;
    private readonly int _worldSenseIntervalTicks;
    private readonly float _closeSenseRefreshProximity;
    private readonly int _closeSenseRefreshMinimumTicks;
    private readonly bool _enableSectorVision;
    private readonly float _plantPayoffTraceHalfLifeSeconds;
    private readonly ParallelOptions _parallelOptions;

    private readonly CreatureSensingScratch _sequentialScratch = new();
    private readonly ThreadLocal<CreatureSensingScratch> _parallelScratch = new(() => new CreatureSensingScratch());
    private CreatureState[] _parallelCreatureBuffer = [];
    private float[] _cachedBodyRadii = [];
    private float[] _cachedMaxSpeeds = [];
    private int[] _cachedTraitStamps = [];
    private int _traitCacheStamp;

    public CreatureSensingSystem(
        UniformSpatialIndex spatialIndex,
        float meatScentRangeMultiplier = 2f,
        float meatScentCaloriesForFullStrength = 60f,
        float meatScentDensitySaturation = 1f,
        BiomePressureProfile? biomeSpeedProfile = null,
        BiomePressureProfile? biomeVisionRangeProfile = null,
        int worldSenseIntervalTicks = DefaultWorldSenseIntervalTicks,
        float closeSenseRefreshProximity = DefaultCloseSenseRefreshProximity,
        int closeSenseRefreshMinimumTicks = DefaultCloseSenseRefreshMinimumTicks,
        bool enableSectorVision = DefaultEnableSectorVision,
        float plantPayoffTraceHalfLifeSeconds = DefaultPlantPayoffTraceHalfLifeSeconds,
        int sensingThreadCount = DefaultSensingThreadCount,
        float soundRangeMultiplier = DefaultSoundRangeMultiplier,
        float soundDensitySaturation = DefaultSoundDensitySaturation)
    {
        if (meatScentRangeMultiplier < 1f || !float.IsFinite(meatScentRangeMultiplier))
        {
            throw new ArgumentOutOfRangeException(nameof(meatScentRangeMultiplier), "Meat scent range multiplier must be finite and at least 1.");
        }

        if (meatScentCaloriesForFullStrength <= 0f || !float.IsFinite(meatScentCaloriesForFullStrength))
        {
            throw new ArgumentOutOfRangeException(nameof(meatScentCaloriesForFullStrength), "Meat scent calorie scale must be finite and positive.");
        }

        if (meatScentDensitySaturation <= 0f || !float.IsFinite(meatScentDensitySaturation))
        {
            throw new ArgumentOutOfRangeException(nameof(meatScentDensitySaturation), "Meat scent density saturation must be finite and positive.");
        }

        if (soundRangeMultiplier < 1f || !float.IsFinite(soundRangeMultiplier))
        {
            throw new ArgumentOutOfRangeException(nameof(soundRangeMultiplier), "Sound range multiplier must be finite and at least 1.");
        }

        if (soundDensitySaturation <= 0f || !float.IsFinite(soundDensitySaturation))
        {
            throw new ArgumentOutOfRangeException(nameof(soundDensitySaturation), "Sound density saturation must be finite and positive.");
        }

        if (worldSenseIntervalTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(worldSenseIntervalTicks), "World sense interval must be positive.");
        }

        if (!float.IsFinite(closeSenseRefreshProximity) || closeSenseRefreshProximity < 0f || closeSenseRefreshProximity > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(closeSenseRefreshProximity), "Close sense refresh proximity must be in [0, 1].");
        }

        if (closeSenseRefreshMinimumTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(closeSenseRefreshMinimumTicks), "Close sense refresh minimum ticks must be positive.");
        }

        if (plantPayoffTraceHalfLifeSeconds <= 0f || !float.IsFinite(plantPayoffTraceHalfLifeSeconds))
        {
            throw new ArgumentOutOfRangeException(nameof(plantPayoffTraceHalfLifeSeconds), "Plant payoff trace half-life must be finite and positive.");
        }

        if (sensingThreadCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sensingThreadCount), "Sensing thread count must be positive.");
        }

        _spatialIndex = spatialIndex;
        _biomeSpeedProfile = BiomePressureProfile.Validate(
            biomeSpeedProfile ?? BiomePressureProfile.Neutral,
            nameof(biomeSpeedProfile));
        _biomeVisionRangeProfile = BiomePressureProfile.Validate(
            biomeVisionRangeProfile ?? BiomePressureProfile.Neutral,
            nameof(biomeVisionRangeProfile));
        _hasUniformBiomeSpeedProfile = HasUniformMultipliers(_biomeSpeedProfile);
        _uniformBiomeDrag = SpeedMultiplierToDrag(_biomeSpeedProfile.Barren);
        _meatScentRangeMultiplier = meatScentRangeMultiplier;
        _meatScentCaloriesForFullStrength = meatScentCaloriesForFullStrength;
        _meatScentDensitySaturation = meatScentDensitySaturation;
        _soundRangeMultiplier = soundRangeMultiplier;
        _soundDensitySaturation = soundDensitySaturation;
        _worldSenseIntervalTicks = worldSenseIntervalTicks;
        _closeSenseRefreshProximity = closeSenseRefreshProximity;
        _closeSenseRefreshMinimumTicks = closeSenseRefreshMinimumTicks;
        _enableSectorVision = enableSectorVision;
        _plantPayoffTraceHalfLifeSeconds = plantPayoffTraceHalfLifeSeconds;
        _parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = sensingThreadCount };
    }

    public void Update(WorldState state, float deltaSeconds)
    {
        var sensingProfile = state.Profile?.Sensing;
        var traitCacheStartedAt = sensingProfile is not null
            ? Stopwatch.GetTimestamp()
            : 0L;
        var creatureCount = state.Creatures.Count;
        var useParallel = _parallelOptions.MaxDegreeOfParallelism > 1 && creatureCount > 1;
        BeginTraitCache(state, precomputeTraits: useParallel);
        if (!useParallel)
        {
            sensingProfile?.RecordTraitCache(
                creatureCount,
                Stopwatch.GetTimestamp() - traitCacheStartedAt);
            sensingProfile?.BeginUpdate(creatureCount);

            for (var i = 0; i < creatureCount; i++)
            {
                state.Creatures[i] = SenseCreature(state, i, deltaSeconds, _sequentialScratch, sensingProfile);
            }

            return;
        }

        EnsureParallelCreatureBufferCapacity(creatureCount);
        Parallel.For(
            0,
            creatureCount,
            _parallelOptions,
            creatureIndex =>
            {
                var scratch = _parallelScratch.Value ?? throw new InvalidOperationException("Parallel sensing scratch was not initialized.");
                _parallelCreatureBuffer[creatureIndex] = SenseCreature(state, creatureIndex, deltaSeconds, scratch, sensingProfile: null);
            });

        for (var i = 0; i < creatureCount; i++)
        {
            state.Creatures[i] = _parallelCreatureBuffer[i];
        }
    }

    private CreatureState SenseCreature(
        WorldState state,
        int creatureIndex,
        float deltaSeconds,
        CreatureSensingScratch scratch,
        SimulationSensingProfile? sensingProfile)
    {
        var creatureSetupStartedAt = sensingProfile is not null
            ? Stopwatch.GetTimestamp()
            : 0L;
        var creature = state.Creatures[creatureIndex];
        var genome = state.GetGenome(creature.GenomeId);
        var effectiveSenseRadius = CreatureGrowth.EffectiveSenseRadius(creature, genome);
        var effectiveVisionRadius = MathF.Max(
            0.001f,
            effectiveSenseRadius * _biomeVisionRangeProfile.For(state.Biomes.GetKindAt(creature.Position)));
        var effectiveVisionAngle = CreatureGrowth.EffectiveVisionAngleRadians(creature, genome);
        var forward = SimVector2.FromAngle(creature.HeadingRadians);
        var right = new SimVector2(-forward.Y, forward.X);
        var hasLimitedVision = effectiveVisionAngle < MathF.Tau;
        var visionCosThreshold = hasLimitedVision
            ? MathF.Cos(effectiveVisionAngle * 0.5f)
            : -1f;
        var freshMeatFoodEfficiency = CreatureDigestion.FreshMeatEnergyEfficiency(genome);
        var meatScentRadius = effectiveSenseRadius * _meatScentRangeMultiplier;
        var creatureSimilarityScentRadius = effectiveSenseRadius * CreatureSimilarityScentRangeMultiplier;
        var soundRadius = effectiveSenseRadius * _soundRangeMultiplier;
        sensingProfile?.RecordCreatureSetup(Stopwatch.GetTimestamp() - creatureSetupStartedAt);

        var worldSenseRefreshReason = GetWorldSenseRefreshReason(state, creature);
        sensingProfile?.RecordWorldSenseRefresh(worldSenseRefreshReason);
        var shouldRefreshWorldSense = worldSenseRefreshReason != WorldSenseRefreshReason.Skipped;
        var senses = shouldRefreshWorldSense
            ? new CreatureSenseState()
            : creature.Senses;
        SetWorldSenseFreshness(ref senses, state.Tick, shouldRefreshWorldSense);
        var internalStateStartedAt = sensingProfile is not null
            ? Stopwatch.GetTimestamp()
            : 0L;
        ApplyInternalSense(ref senses, ref creature, genome, deltaSeconds, _plantPayoffTraceHalfLifeSeconds);
        sensingProfile?.RecordInternalState(Stopwatch.GetTimestamp() - internalStateStartedAt);

        senses.MovementBlocked = creature.LastMovementBlocked ? 1f : 0f;
        senses.FoodContact = creature.IsTouchingFood ? 1f : 0f;
        senses.PlantFoodContact = creature.IsTouchingFood
            && creature.FoodContactKind == FoodContactKind.Resource
            && creature.FoodContactResourceKind == ResourceKind.Plant
                ? 1f
                : 0f;
        senses.PlantFoodContactEnergyQuality = senses.PlantFoodContact > 0f
            ? PlantResourceTraits.EnergyQualitySense(creature.FoodContactPlantKind)
            : 0f;
        senses.PlantFoodContactBiteEase = senses.PlantFoodContact > 0f
            ? PlantResourceTraits.BiteEaseSense(creature.FoodContactPlantKind)
            : 0f;
        senses.MeatFoodContact = creature.IsTouchingFood
            && ((creature.FoodContactKind == FoodContactKind.Resource
                    && creature.FoodContactResourceKind == ResourceKind.Meat)
                || creature.FoodContactKind == FoodContactKind.SmallPrey)
                ? 1f
                : 0f;
        senses.EggFoodContact = creature.IsTouchingFood && creature.FoodContactKind == FoodContactKind.Egg
            ? 1f
            : 0f;
        senses.CreatureContact = creature.IsTouchingCreature ? 1f : 0f;
        if (!creature.IsTouchingCreature)
        {
            senses.CreatureContactSimilarity = 0f;
        }

        senses.GrabPressure = Math.Clamp(creature.GrabPressure, 0f, 1f);
        senses.CanGrabCreature = creature.IsTouchingCreature ? 1f : 0f;
        senses.IsHoldingCreature = creature.HeldCreatureId != default ? 1f : 0f;
        if (senses.GrabPressure > 0f && creature.GrabDirection.IsFinite)
        {
            var grabDirection = creature.GrabDirection.ClampedLength(1f);
            senses.GrabDirectionForward = SimVector2.Dot(grabDirection, forward);
            senses.GrabDirectionRight = SimVector2.Dot(grabDirection, right);
        }
        else
        {
            senses.GrabDirectionForward = 0f;
            senses.GrabDirectionRight = 0f;
        }

        var memorySenseStartedAt = sensingProfile is not null
            ? Stopwatch.GetTimestamp()
            : 0L;
        ApplyMemorySense(ref senses, creature, forward, right);
        sensingProfile?.RecordMemorySense(Stopwatch.GetTimestamp() - memorySenseStartedAt);

        if (!shouldRefreshWorldSense)
        {
            var skippedFinalizationStartedAt = sensingProfile is not null
                ? Stopwatch.GetTimestamp()
                : 0L;
            ApplyPlantPreferenceBridge(ref senses);
            creature.Senses = senses;
            sensingProfile?.RecordSenseFinalization(Stopwatch.GetTimestamp() - skippedFinalizationStartedAt);
            return creature;
        }

        var resourceQueryStartedAt = sensingProfile is not null
            ? Stopwatch.GetTimestamp()
            : 0L;
        _spatialIndex.AddPlantAndMeatResourceCandidatesWithCalories(
            state,
            creature.Position,
            effectiveVisionRadius,
            meatScentRadius,
            0f,
            scratch.PlantResourceCandidates,
            scratch.SeenPlantResourceCandidates,
            scratch.MeatResourceCandidates,
            scratch.SeenMeatResourceCandidates);
        sensingProfile?.RecordSplitResourceQuery(
            scratch.PlantResourceCandidates.Count,
            scratch.MeatResourceCandidates.Count,
            Stopwatch.GetTimestamp() - resourceQueryStartedAt);

        var eggQueryStartedAt = sensingProfile is not null
            ? Stopwatch.GetTimestamp()
            : 0L;
        _spatialIndex.AddEggCandidatesWithEnergy(
            state,
            creature.Position,
            effectiveVisionRadius,
            minimumEnergy: 0f,
            scratch.EggCandidates,
            scratch.SeenEggCandidates);
        sensingProfile?.RecordEggQuery(
            scratch.EggCandidates.Count,
            Stopwatch.GetTimestamp() - eggQueryStartedAt);

        _spatialIndex.AddSmallPreyCandidatesWithCalories(
            state,
            creature.Position,
            MathF.Max(effectiveVisionRadius, meatScentRadius),
            minimumCalories: 0f,
            scratch.SmallPreyCandidates,
            scratch.SeenSmallPreyCandidates);

        var visionSectors = default(VisionSectorSet);
        var creatureVisibilityStartedAt = sensingProfile is not null
            ? Stopwatch.GetTimestamp()
            : 0L;
        var creatureVisibility = _spatialIndex.FindNearestVisibleCreature(
            state,
            creatureIndex,
            creature.Id,
            creature.Position,
            effectiveVisionRadius + 12f,
            effectiveVisionRadius,
            forward,
            hasLimitedVision,
            visionCosThreshold,
            effectiveVisionAngle,
            _cachedBodyRadii,
            _cachedMaxSpeeds,
            _enableSectorVision,
            ref visionSectors);
        sensingProfile?.RecordCreatureQuery(
            creatureVisibility,
            Stopwatch.GetTimestamp() - creatureVisibilityStartedAt);
        sensingProfile?.RecordCreatureScan(
            creatureVisibility.VisibleCount,
            0L);
        var creatureTraits = GetCreatureTraits(state, creatureIndex);
        var creatureAmbientSense = CalculateCreatureAmbientSense(
            state,
            creatureIndex,
            creature,
            creatureTraits,
            forward,
            creatureSimilarityScentRadius,
            soundRadius,
            scratch.CreatureCandidates);

        var terrainSenseStartedAt = sensingProfile is not null
            ? Stopwatch.GetTimestamp()
            : 0L;
        ApplyTerrainDragSense(ref senses, state, creature, genome, forward, right, effectiveSenseRadius);
        sensingProfile?.RecordTerrainSense(Stopwatch.GetTimestamp() - terrainSenseStartedAt);

        var obstacleSenseStartedAt = sensingProfile is not null
            ? Stopwatch.GetTimestamp()
            : 0L;
        ApplyObstacleSense(ref senses, state, creature, genome, forward, right, effectiveSenseRadius);
        sensingProfile?.RecordObstacleSense(Stopwatch.GetTimestamp() - obstacleSenseStartedAt);

        var visibleFoodCount = 0;
        var visiblePlantCount = 0;
        var visibleMeatCount = 0;
        var visibleCreatureCount = creatureVisibility.VisibleCount;
        var totalMeatScentStrength = 0f;
        var meatScentVector = SimVector2.Zero;
        var totalRottenMeatScentStrength = 0f;
        var rottenMeatScentVector = SimVector2.Zero;
        var bestVisibleFoodKind = FoodContactKind.None;
        var bestVisibleFoodIndex = -1;
        var bestVisibleFoodScore = float.NegativeInfinity;
        var bestVisibleFoodDistanceSquared = float.PositiveInfinity;
        var nearestVisiblePlantIndex = -1;
        var nearestVisiblePlantDistanceSquared = float.PositiveInfinity;
        var nearestVisibleMeatKind = FoodContactKind.None;
        var nearestVisibleMeatIndex = -1;
        var nearestVisibleMeatDistanceSquared = float.PositiveInfinity;
        var nearestVisibleMeatFreshness = 0f;
        var nearestVisibleCreatureIndex = creatureVisibility.NearestIndex;
        var visiblePlantQualityWeight = 0f;
        var visiblePlantEnergyQualityTotal = 0f;
        var visiblePlantBiteEaseTotal = 0f;

        var resourceScanStartedAt = sensingProfile is not null
            ? Stopwatch.GetTimestamp()
            : 0L;
        var plantCandidates = 0;
        var meatResourceCandidates = 0;
        var visiblePlantCandidates = 0;
        var visibleMeatResourceCandidates = 0;

        foreach (var resourceIndex in scratch.PlantResourceCandidates)
        {
            var resource = state.Resources[resourceIndex];
            plantCandidates++;
            var toResource = resource.Position - creature.Position;
            var distanceSquared = toResource.LengthSquared;

            if (!IsWithinEdgeRange(distanceSquared, resource.Radius, effectiveVisionRadius)
                || !IsInsideVisionCone(toResource, distanceSquared, forward, hasLimitedVision, visionCosThreshold))
            {
                continue;
            }

            var centerDistance = MathF.Sqrt(distanceSquared);
            var edgeDistance = Math.Max(0f, centerDistance - resource.Radius);

            visibleFoodCount++;
            visiblePlantCount++;
            visiblePlantCandidates++;

            if (distanceSquared < nearestVisiblePlantDistanceSquared)
            {
                nearestVisiblePlantDistanceSquared = distanceSquared;
                nearestVisiblePlantIndex = resourceIndex;
            }

            var proximity = 1f - Math.Clamp(edgeDistance / effectiveVisionRadius, 0f, 1f);
            var qualityClarity = PlantQualityClarity(proximity);
            var qualityWeight = qualityClarity >= MinimumPlantQualityClarity
                ? qualityClarity * Math.Clamp(resource.Calories / Math.Max(1f, resource.MaxCalories), 0f, 1f)
                : 0f;
            var energyQuality = PlantResourceTraits.EnergyQualitySense(resource.PlantKind);
            var biteEase = PlantResourceTraits.BiteEaseSense(resource.PlantKind);
            if (qualityWeight > 0f)
            {
                visiblePlantQualityWeight += qualityWeight;
                visiblePlantEnergyQualityTotal += energyQuality * qualityWeight;
                visiblePlantBiteEaseTotal += biteEase * qualityWeight;
            }

            if (_enableSectorVision
                && VisionSectorSet.TryGetSectorIndex(
                    toResource,
                    forward,
                    right,
                    hasLimitedVision,
                    effectiveVisionAngle,
                    out var sectorIndex))
            {
                visionSectors.AddPlant(sectorIndex, proximity, energyQuality, biteEase, qualityWeight);
            }

            var foodScore = proximity * CreatureDigestion.PlantTypeEnergyEfficiency(genome, resource.PlantKind);
            if (foodScore > bestVisibleFoodScore
                || (Math.Abs(foodScore - bestVisibleFoodScore) <= 0.0001f
                    && distanceSquared < bestVisibleFoodDistanceSquared))
            {
                bestVisibleFoodScore = foodScore;
                bestVisibleFoodDistanceSquared = distanceSquared;
                bestVisibleFoodKind = FoodContactKind.Resource;
                bestVisibleFoodIndex = resourceIndex;
            }
        }

        foreach (var resourceIndex in scratch.MeatResourceCandidates)
        {
            var resource = state.Resources[resourceIndex];
            meatResourceCandidates++;
            var toResource = resource.Position - creature.Position;
            var distanceSquared = toResource.LengthSquared;
            var centerDistance = MathF.Sqrt(distanceSquared);
            var edgeDistance = Math.Max(0f, centerDistance - resource.Radius);
            var distanceFactor = 1f - Math.Clamp(edgeDistance / meatScentRadius, 0f, 1f);
            if (distanceFactor > 0f)
            {
                var calorieFactor = Math.Clamp(resource.Calories / _meatScentCaloriesForFullStrength, 0f, 1f);
                var scentStrength = calorieFactor * distanceFactor * distanceFactor;
                if (scentStrength > MinimumScentStrength)
                {
                    var scentDirection = centerDistance > 0.0001f
                        ? toResource / centerDistance
                        : forward;
                    totalMeatScentStrength += scentStrength;
                    meatScentVector += scentDirection * scentStrength;

                    var staleStrength = scentStrength * MeatQuality.Staleness(resource);
                    if (staleStrength > MinimumScentStrength)
                    {
                        totalRottenMeatScentStrength += staleStrength;
                        rottenMeatScentVector += scentDirection * staleStrength;
                    }
                }
            }

            if (edgeDistance > effectiveVisionRadius
                || !IsInsideVisionCone(toResource, distanceSquared, forward, hasLimitedVision, visionCosThreshold))
            {
                continue;
            }

            visibleFoodCount++;
            visibleMeatCount++;
            visibleMeatResourceCandidates++;

            if (distanceSquared < nearestVisibleMeatDistanceSquared)
            {
                nearestVisibleMeatDistanceSquared = distanceSquared;
                nearestVisibleMeatKind = FoodContactKind.Resource;
                nearestVisibleMeatIndex = resourceIndex;
                nearestVisibleMeatFreshness = MeatQuality.Freshness(resource);
            }

            var proximity = 1f - Math.Clamp(edgeDistance / effectiveVisionRadius, 0f, 1f);
            if (_enableSectorVision
                && VisionSectorSet.TryGetSectorIndex(
                    toResource,
                    forward,
                    right,
                    hasLimitedVision,
                    effectiveVisionAngle,
                    out var sectorIndex))
            {
                visionSectors.AddMeat(sectorIndex, proximity);
            }

            var foodScore = proximity * CreatureDigestion.MeatEnergyEfficiency(genome, MeatQuality.Freshness(resource));
            if (foodScore > bestVisibleFoodScore
                || (Math.Abs(foodScore - bestVisibleFoodScore) <= 0.0001f
                    && distanceSquared < bestVisibleFoodDistanceSquared))
            {
                bestVisibleFoodScore = foodScore;
                bestVisibleFoodDistanceSquared = distanceSquared;
                bestVisibleFoodKind = FoodContactKind.Resource;
                bestVisibleFoodIndex = resourceIndex;
            }
        }

        sensingProfile?.RecordResourceScan(
            plantCandidates,
            meatResourceCandidates,
            visiblePlantCandidates,
            visibleMeatResourceCandidates,
            Stopwatch.GetTimestamp() - resourceScanStartedAt);

        var eggScanStartedAt = sensingProfile is not null
            ? Stopwatch.GetTimestamp()
            : 0L;
        var visibleEggCandidates = 0;

        foreach (var eggIndex in scratch.EggCandidates)
        {
            var egg = state.Eggs[eggIndex];
            var eggRadius = EggPredation.ContactRadius(egg);
            var toEgg = egg.Position - creature.Position;
            var distanceSquared = toEgg.LengthSquared;

            if (!IsWithinEdgeRange(distanceSquared, eggRadius, effectiveVisionRadius)
                || !IsInsideVisionCone(toEgg, distanceSquared, forward, hasLimitedVision, visionCosThreshold))
            {
                continue;
            }

            var centerDistance = MathF.Sqrt(distanceSquared);
            var edgeDistance = Math.Max(0f, centerDistance - eggRadius);

            visibleFoodCount++;
            visibleMeatCount++;
            visibleEggCandidates++;

            if (distanceSquared < nearestVisibleMeatDistanceSquared)
            {
                nearestVisibleMeatDistanceSquared = distanceSquared;
                nearestVisibleMeatKind = FoodContactKind.Egg;
                nearestVisibleMeatIndex = eggIndex;
                nearestVisibleMeatFreshness = 1f;
            }

            var proximity = 1f - Math.Clamp(edgeDistance / effectiveVisionRadius, 0f, 1f);
            if (_enableSectorVision
                && VisionSectorSet.TryGetSectorIndex(
                    toEgg,
                    forward,
                    right,
                    hasLimitedVision,
                    effectiveVisionAngle,
                    out var sectorIndex))
            {
                visionSectors.AddEgg(sectorIndex, proximity);
            }

            var foodScore = proximity * freshMeatFoodEfficiency;
            if (foodScore > bestVisibleFoodScore
                || (Math.Abs(foodScore - bestVisibleFoodScore) <= 0.0001f
                    && distanceSquared < bestVisibleFoodDistanceSquared))
            {
                bestVisibleFoodScore = foodScore;
                bestVisibleFoodDistanceSquared = distanceSquared;
                bestVisibleFoodKind = FoodContactKind.Egg;
                bestVisibleFoodIndex = eggIndex;
            }
        }

        sensingProfile?.RecordEggScan(
            visibleEggCandidates,
            Stopwatch.GetTimestamp() - eggScanStartedAt);

        foreach (var preyIndex in scratch.SmallPreyCandidates)
        {
            var prey = state.SmallPrey[preyIndex];
            var toPrey = prey.Position - creature.Position;
            var distanceSquared = toPrey.LengthSquared;
            var centerDistance = MathF.Sqrt(distanceSquared);
            var edgeDistance = Math.Max(0f, centerDistance - prey.Radius);
            var scentDistanceFactor = 1f - Math.Clamp(edgeDistance / meatScentRadius, 0f, 1f);
            if (scentDistanceFactor > 0f)
            {
                var calorieFactor = Math.Clamp(prey.Calories / _meatScentCaloriesForFullStrength, 0f, 1f);
                var scentStrength = calorieFactor * scentDistanceFactor * scentDistanceFactor;
                if (scentStrength > MinimumScentStrength)
                {
                    var scentDirection = centerDistance > 0.0001f
                        ? toPrey / centerDistance
                        : forward;
                    totalMeatScentStrength += scentStrength;
                    meatScentVector += scentDirection * scentStrength;
                }
            }

            if (!IsWithinEdgeRange(distanceSquared, prey.Radius, effectiveVisionRadius)
                || !IsInsideVisionCone(toPrey, distanceSquared, forward, hasLimitedVision, visionCosThreshold))
            {
                continue;
            }

            visibleFoodCount++;
            visibleMeatCount++;

            if (distanceSquared < nearestVisibleMeatDistanceSquared)
            {
                nearestVisibleMeatDistanceSquared = distanceSquared;
                nearestVisibleMeatKind = FoodContactKind.SmallPrey;
                nearestVisibleMeatIndex = preyIndex;
                nearestVisibleMeatFreshness = 1f;
            }

            var proximity = 1f - Math.Clamp(edgeDistance / effectiveVisionRadius, 0f, 1f);
            if (_enableSectorVision
                && VisionSectorSet.TryGetSectorIndex(
                    toPrey,
                    forward,
                    right,
                    hasLimitedVision,
                    effectiveVisionAngle,
                    out var sectorIndex))
            {
                visionSectors.AddMeat(sectorIndex, proximity);
            }

            var foodScore = proximity * freshMeatFoodEfficiency;
            if (foodScore > bestVisibleFoodScore
                || (Math.Abs(foodScore - bestVisibleFoodScore) <= 0.0001f
                    && distanceSquared < bestVisibleFoodDistanceSquared))
            {
                bestVisibleFoodScore = foodScore;
                bestVisibleFoodDistanceSquared = distanceSquared;
                bestVisibleFoodKind = FoodContactKind.SmallPrey;
                bestVisibleFoodIndex = preyIndex;
            }
        }

        var senseFinalizationStartedAt = sensingProfile is not null
            ? Stopwatch.GetTimestamp()
            : 0L;
        senses.VisibleFoodDensity = Math.Clamp(visibleFoodCount / DensitySaturationFoodCount, 0f, 1f);
        senses.VisiblePlantDensity = Math.Clamp(visiblePlantCount / DensitySaturationFoodCount, 0f, 1f);
        senses.VisiblePlantEnergyQuality = visiblePlantQualityWeight > 0f
            ? visiblePlantEnergyQualityTotal / visiblePlantQualityWeight
            : 0f;
        senses.VisiblePlantBiteEase = visiblePlantQualityWeight > 0f
            ? visiblePlantBiteEaseTotal / visiblePlantQualityWeight
            : 0f;
        senses.VisibleMeatDensity = Math.Clamp(visibleMeatCount / DensitySaturationFoodCount, 0f, 1f);
        senses.VisibleCreatureDensity = Math.Clamp(visibleCreatureCount / DensitySaturationFoodCount, 0f, 1f);
        senses.VisiblePreyDensity = senses.VisibleCreatureDensity;
        senses.VisionSectors = visionSectors;
        ApplyMeatScentSense(ref senses, meatScentVector, totalMeatScentStrength, forward, right);
        ApplyRottenMeatScentSense(ref senses, rottenMeatScentVector, totalRottenMeatScentStrength, forward, right);
        ApplyCreatureSimilarityScentSense(
            ref senses,
            creatureAmbientSense.ScentVector,
            creatureAmbientSense.TotalScentStrength,
            forward,
            right);
        ApplySoundSense(
            ref senses,
            creatureAmbientSense.SoundVector,
            creatureAmbientSense.TotalSoundStrength,
            creatureAmbientSense.SoundToneWeightedTotal,
            creatureAmbientSense.SoundToneSquaredWeightedTotal,
            forward,
            right);
        senses.CreatureContactSimilarity = creatureAmbientSense.ContactSimilarity;

        if (bestVisibleFoodKind == FoodContactKind.Resource && bestVisibleFoodIndex >= 0)
        {
            ApplyGenericFoodSense(
                ref senses,
                state.Resources[bestVisibleFoodIndex],
                creature,
                forward,
                right,
                effectiveVisionRadius);
        }
        else if (bestVisibleFoodKind == FoodContactKind.Egg && bestVisibleFoodIndex >= 0)
        {
            ApplyGenericEggSense(
                ref senses,
                state.Eggs[bestVisibleFoodIndex],
                creature,
                forward,
                right,
                effectiveVisionRadius);
        }
        else if (bestVisibleFoodKind == FoodContactKind.SmallPrey && bestVisibleFoodIndex >= 0)
        {
            ApplyGenericSmallPreySense(
                ref senses,
                state.SmallPrey[bestVisibleFoodIndex],
                creature,
                forward,
                right,
                effectiveVisionRadius);
        }
        if (nearestVisiblePlantIndex >= 0)
        {
            ApplyPlantSense(
                ref senses,
                state.Resources[nearestVisiblePlantIndex],
                creature,
                forward,
                right,
                effectiveVisionRadius);
        }

        if (nearestVisibleMeatKind == FoodContactKind.Resource && nearestVisibleMeatIndex >= 0)
        {
            senses.VisibleMeatFreshness = nearestVisibleMeatFreshness;
            ApplyMeatSense(
                ref senses,
                state.Resources[nearestVisibleMeatIndex],
                creature,
                forward,
                right,
                effectiveVisionRadius);
        }
        else if (nearestVisibleMeatKind == FoodContactKind.Egg && nearestVisibleMeatIndex >= 0)
        {
            senses.VisibleMeatFreshness = nearestVisibleMeatFreshness;
            ApplyMeatEggSense(
                ref senses,
                state.Eggs[nearestVisibleMeatIndex],
                creature,
                forward,
                right,
                effectiveVisionRadius);
        }
        else if (nearestVisibleMeatKind == FoodContactKind.SmallPrey && nearestVisibleMeatIndex >= 0)
        {
            senses.VisibleMeatFreshness = nearestVisibleMeatFreshness;
            ApplyMeatSmallPreySense(
                ref senses,
                state.SmallPrey[nearestVisibleMeatIndex],
                creature,
                forward,
                right,
                effectiveVisionRadius);
        }
        if (nearestVisibleCreatureIndex >= 0)
        {
            ApplyCreatureSense(
                ref senses,
                state.Creatures[nearestVisibleCreatureIndex],
                GetCreatureTraits(state, nearestVisibleCreatureIndex),
                creatureTraits,
                creature,
                forward,
                right,
                effectiveVisionRadius);
        }

        ApplyPlantPreferenceBridge(ref senses);
        creature.Senses = senses;
        sensingProfile?.RecordSenseFinalization(Stopwatch.GetTimestamp() - senseFinalizationStartedAt);
        return creature;
    }

    private WorldSenseRefreshReason GetWorldSenseRefreshReason(WorldState state, CreatureState creature)
    {
        if (_worldSenseIntervalTicks <= 1 || state.Tick == 0 || creature.Senses.WorldSenseTick < 0)
        {
            return WorldSenseRefreshReason.Forced;
        }

        if (HasImmediateCloseWorldSenseRefreshCue(creature))
        {
            return WorldSenseRefreshReason.Close;
        }

        if (NeedsProximityCloseWorldSenseRefresh(creature)
            && WorldSenseAgeTicks(state, creature) >= _closeSenseRefreshMinimumTicks)
        {
            return WorldSenseRefreshReason.Close;
        }

        return (state.Tick + creature.Id.Value) % _worldSenseIntervalTicks == 0
            ? WorldSenseRefreshReason.Scheduled
            : WorldSenseRefreshReason.Skipped;
    }

    private static bool HasImmediateCloseWorldSenseRefreshCue(CreatureState creature)
    {
        return creature.IsTouchingFood
            || creature.IsTouchingCreature
            || creature.LastMovementBlocked;
    }

    private bool NeedsProximityCloseWorldSenseRefresh(CreatureState creature)
    {
        var senses = creature.Senses;
        return senses.FoodProximity >= _closeSenseRefreshProximity
            || senses.PlantProximity >= _closeSenseRefreshProximity
            || senses.MeatProximity >= _closeSenseRefreshProximity
            || senses.CreatureProximity >= _closeSenseRefreshProximity
            || senses.PreyProximity >= _closeSenseRefreshProximity
            || senses.ForwardObstacle >= _closeSenseRefreshProximity
            || senses.LeftObstacle >= _closeSenseRefreshProximity
            || senses.RightObstacle >= _closeSenseRefreshProximity;
    }

    private static int WorldSenseAgeTicks(WorldState state, CreatureState creature)
    {
        return creature.Senses.WorldSenseTick >= 0 && state.Tick >= creature.Senses.WorldSenseTick
            ? (int)Math.Min(int.MaxValue, state.Tick - creature.Senses.WorldSenseTick)
            : int.MaxValue;
    }

    private static void SetWorldSenseFreshness(ref CreatureSenseState senses, long tick, bool refreshed)
    {
        senses.WorldSenseRefreshed = refreshed;
        if (refreshed)
        {
            senses.WorldSenseTick = tick;
            senses.WorldSenseAgeTicks = 0;
            return;
        }

        senses.WorldSenseAgeTicks = senses.WorldSenseTick >= 0 && tick >= senses.WorldSenseTick
            ? (int)Math.Min(int.MaxValue, tick - senses.WorldSenseTick)
            : int.MaxValue;
    }

    private static void ApplyInternalSense(
        ref CreatureSenseState senses,
        ref CreatureState creature,
        CreatureGenome genome,
        float deltaSeconds,
        float plantPayoffTraceHalfLifeSeconds)
    {
        var energyRatio = Math.Clamp(creature.Energy / genome.ReproductionEnergyThreshold, 0f, 1f);
        var maxHealth = OffspringDevelopment.JuvenileGrowthScale(creature.BirthInvestmentRatio);
        var healthRatio = maxHealth > 0f
            ? Math.Clamp(creature.Health / maxHealth, 0f, 1f)
            : 0f;
        var eggReserveRatio = Math.Clamp(creature.ReproductiveEnergy / genome.OffspringEnergyInvestment, 0f, 1f);
        var energySurplusRatio = Math.Clamp(
            (creature.Energy - genome.ReproductionEnergyThreshold) / Math.Max(1f, genome.OffspringEnergyInvestment),
            0f,
            1f);
        var expectedFoodTransfer = Math.Max(
            MinimumExpectedFoodTransfer,
            CreatureGrowth.EffectiveEatCaloriesPerSecond(creature, genome) * Math.Max(0f, deltaSeconds));
        var recentFoodSuccess = Math.Clamp(
            (creature.LastCaloriesEaten + creature.LastCaloriesDigested) / expectedFoodTransfer,
            0f,
            1f);
        var expectedPlantDigestiveYield = Math.Max(
            MinimumExpectedPlantDigestiveYield,
            CreatureGrowth.EffectiveDigestionCaloriesPerSecond(creature, genome) * Math.Max(0f, deltaSeconds));
        var recentPlantRawYield = Math.Clamp(
            creature.LastPlantCaloriesEaten / expectedFoodTransfer,
            0f,
            1f);
        var recentPlantEnergyYield = Math.Clamp(
            creature.LastPlantDigestedEnergy / expectedPlantDigestiveYield,
            0f,
            1f);
        var recentTenderPlantEnergyYield = Math.Clamp(
            creature.LastTenderPlantDigestedEnergy / expectedPlantDigestiveYield,
            0f,
            1f);
        var recentRichPlantEnergyYield = Math.Clamp(
            creature.LastRichPlantDigestedEnergy / expectedPlantDigestiveYield,
            0f,
            1f);
        var recentToughPlantEnergyYield = Math.Clamp(
            creature.LastToughPlantDigestedEnergy / expectedPlantDigestiveYield,
            0f,
            1f);
        var recentFoodEnergyYield = Math.Clamp(
            creature.LastCaloriesDigested / expectedPlantDigestiveYield,
            0f,
            1f);
        creature.TenderPlantPayoffTrace = UpdatePlantPayoffTrace(
            creature.TenderPlantPayoffTrace,
            recentTenderPlantEnergyYield,
            deltaSeconds,
            plantPayoffTraceHalfLifeSeconds);
        creature.RichPlantPayoffTrace = UpdatePlantPayoffTrace(
            creature.RichPlantPayoffTrace,
            recentRichPlantEnergyYield,
            deltaSeconds,
            plantPayoffTraceHalfLifeSeconds);
        creature.ToughPlantPayoffTrace = UpdatePlantPayoffTrace(
            creature.ToughPlantPayoffTrace,
            recentToughPlantEnergyYield,
            deltaSeconds,
            plantPayoffTraceHalfLifeSeconds);
        var isReadyToLay =
            eggReserveRatio >= 1f
            && creature.AgeSeconds >= genome.MaturityAgeSeconds
            && creature.ReproductionCooldownSeconds <= 0f;

        senses.EnergyRatio = energyRatio;
        senses.HealthRatio = healthRatio;
        senses.Hunger = 1f - energyRatio;
        senses.EggReserveRatio = eggReserveRatio;
        senses.EnergySurplusRatio = energySurplusRatio;
        senses.FatRatio = CreatureGrowth.FatStorageRatio(creature, genome);
        senses.MassBurdenRatio = CreatureGrowth.FatMassBurdenRatio(creature, genome);
        senses.RecentFoodSuccess = recentFoodSuccess;
        senses.RecentPlantRawYield = recentPlantRawYield;
        senses.RecentPlantEnergyYield = recentPlantEnergyYield;
        senses.RecentTenderPlantEnergyYield = recentTenderPlantEnergyYield;
        senses.RecentRichPlantEnergyYield = recentRichPlantEnergyYield;
        senses.RecentToughPlantEnergyYield = recentToughPlantEnergyYield;
        senses.TenderPlantPayoffTrace = creature.TenderPlantPayoffTrace;
        senses.RichPlantPayoffTrace = creature.RichPlantPayoffTrace;
        senses.ToughPlantPayoffTrace = creature.ToughPlantPayoffTrace;
        senses.RecentFoodEnergyYield = recentFoodEnergyYield;
        senses.ReproductionReadiness = isReadyToLay ? 1f : 0f;
    }

    private static float UpdatePlantPayoffTrace(
        float currentTrace,
        float immediateYield,
        float deltaSeconds,
        float halfLifeSeconds)
    {
        var safeDeltaSeconds = Math.Max(0f, deltaSeconds);
        var decay = MathF.Pow(0.5f, safeDeltaSeconds / halfLifeSeconds);
        return Math.Clamp(Math.Max(currentTrace * decay, immediateYield), 0f, 1f);
    }

    private static void ApplyPlantPreferenceBridge(ref CreatureSenseState senses)
    {
        senses.PlantFoodContactPreference = senses.PlantFoodContact > 0f
            ? PlantResourceTraits.PayoffPreferenceCue(
                senses.PlantFoodContactEnergyQuality,
                senses.PlantFoodContactBiteEase,
                senses.TenderPlantPayoffTrace,
                senses.RichPlantPayoffTrace,
                senses.ToughPlantPayoffTrace)
            : 0f;

        var preferenceDensity = 0f;
        var preferenceForward = 0f;
        var preferenceRight = 0f;
        if (senses.VisionSectors.HasAnySignal)
        {
            for (var sectorIndex = 0; sectorIndex < VisionSectorSet.SectorCount; sectorIndex++)
            {
                var sector = senses.VisionSectors.Get(sectorIndex);
                if (sector.PlantDensity <= 0f
                    || (sector.PlantEnergyQuality <= 0f && sector.PlantBiteEase <= 0f))
                {
                    continue;
                }

                var preference = PlantResourceTraits.PayoffPreferenceCue(
                        sector.PlantEnergyQuality,
                        sector.PlantBiteEase,
                        senses.TenderPlantPayoffTrace,
                        senses.RichPlantPayoffTrace,
                        senses.ToughPlantPayoffTrace)
                    * sector.PlantProximity;
                if (preference <= 0f)
                {
                    continue;
                }

                var right = (sectorIndex - VisionSectorSet.CenterSectorIndex)
                    / (float)VisionSectorSet.CenterSectorIndex;
                var forward = 1f - Math.Abs(right);
                preferenceDensity += preference;
                preferenceForward += preference * forward;
                preferenceRight += preference * right;
            }
        }

        if (preferenceDensity <= 0f
            && senses.VisiblePlantDensity > 0f
            && (senses.VisiblePlantEnergyQuality > 0f || senses.VisiblePlantBiteEase > 0f))
        {
            var preference = PlantResourceTraits.PayoffPreferenceCue(
                    senses.VisiblePlantEnergyQuality,
                    senses.VisiblePlantBiteEase,
                    senses.TenderPlantPayoffTrace,
                    senses.RichPlantPayoffTrace,
                    senses.ToughPlantPayoffTrace)
                * senses.PlantProximity;
            preferenceDensity = preference;
            preferenceForward = preference * senses.PlantDirectionForward;
            preferenceRight = preference * senses.PlantDirectionRight;
        }

        senses.PlantPreferenceDensity = Math.Clamp(preferenceDensity, 0f, 1f);
        senses.PlantPreferenceDirectionForward = Math.Clamp(preferenceForward, -1f, 1f);
        senses.PlantPreferenceDirectionRight = Math.Clamp(preferenceRight, -1f, 1f);
    }

    private void ApplyTerrainDragSense(
        ref CreatureSenseState senses,
        WorldState state,
        CreatureState creature,
        CreatureGenome genome,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var terrainProbeDistance = Math.Clamp(
            effectiveSenseRadius * 0.5f,
            MinimumTerrainProbeDistance,
            MaximumTerrainProbeDistance);
        var terrainForwardPosition = state.Bounds.Clamp(creature.Position + forward * terrainProbeDistance);
        var terrainLeftPosition = state.Bounds.Clamp(creature.Position - right * terrainProbeDistance);
        var terrainRightPosition = state.Bounds.Clamp(creature.Position + right * terrainProbeDistance);

        var bodyRadius = CreatureGrowth.EffectiveBodyRadius(creature, genome);
        var habitatProbeDistance = Math.Clamp(
            MathF.Min(effectiveSenseRadius * 0.25f, bodyRadius * 8f),
            MinimumHabitatProbeDistance,
            MaximumHabitatProbeDistance);
        var habitatForwardPosition = state.Bounds.Clamp(creature.Position + forward * habitatProbeDistance);
        var habitatLeftPosition = state.Bounds.Clamp(creature.Position - right * habitatProbeDistance);
        var habitatRightPosition = state.Bounds.Clamp(creature.Position + right * habitatProbeDistance);

        senses.CurrentHabitatQuality = HabitatQualityAt(state, creature.Position);
        senses.ForwardHabitatQuality = HabitatQualityAt(state, habitatForwardPosition);
        senses.LeftHabitatQuality = HabitatQualityAt(state, habitatLeftPosition);
        senses.RightHabitatQuality = HabitatQualityAt(state, habitatRightPosition);

        if (_hasUniformBiomeSpeedProfile)
        {
            if (_uniformBiomeDrag != 0f)
            {
                senses.CurrentTerrainDrag = _uniformBiomeDrag;
                senses.ForwardTerrainDrag = _uniformBiomeDrag;
                senses.LeftTerrainDrag = _uniformBiomeDrag;
                senses.RightTerrainDrag = _uniformBiomeDrag;
            }

            return;
        }

        senses.CurrentTerrainDrag = SpeedMultiplierToDrag(TerrainSpeedMultiplierAt(state, creature.Position));
        senses.ForwardTerrainDrag = SpeedMultiplierToDrag(TerrainSpeedMultiplierAt(state, terrainForwardPosition));
        senses.LeftTerrainDrag = SpeedMultiplierToDrag(TerrainSpeedMultiplierAt(state, terrainLeftPosition));
        senses.RightTerrainDrag = SpeedMultiplierToDrag(TerrainSpeedMultiplierAt(state, terrainRightPosition));
    }

    private float TerrainSpeedMultiplierAt(WorldState state, SimVector2 position)
    {
        return _biomeSpeedProfile.For(state.Biomes.GetKindAt(position));
    }

    private static float HabitatQualityAt(WorldState state, SimVector2 position)
    {
        var densityQuality = MaximumHabitatDensityMultiplier > 0f
            ? state.Biomes.GetResourceDensityMultiplierAt(position) / MaximumHabitatDensityMultiplier
            : 0f;
        var regrowthQuality = MaximumHabitatRegrowthMultiplier > 0f
            ? state.Biomes.GetResourceRegrowthMultiplierAt(position) / MaximumHabitatRegrowthMultiplier
            : 0f;
        var longTermQuality = densityQuality * 0.65f + regrowthQuality * 0.35f;
        return Math.Clamp(longTermQuality * state.LocalFertility.GetMultiplierAt(position), 0f, 1f);
    }

    private static void ApplyObstacleSense(
        ref CreatureSenseState senses,
        WorldState state,
        CreatureState creature,
        CreatureGenome genome,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        senses.MovementBlocked = creature.LastMovementBlocked ? 1f : 0f;

        if (!state.Obstacles.HasObstacles)
        {
            return;
        }

        var bodyRadius = CreatureGrowth.EffectiveBodyRadius(creature, genome);
        var probeDistance = Math.Clamp(
            MathF.Max(effectiveSenseRadius * 0.35f, bodyRadius * 4f),
            MinimumTerrainProbeDistance,
            MaximumTerrainProbeDistance);

        senses.ForwardObstacle = SampleObstacleProximity(
            state.Obstacles,
            creature.Position,
            forward,
            bodyRadius,
            probeDistance);
        senses.LeftObstacle = SampleObstacleProximity(
            state.Obstacles,
            creature.Position,
            right * -1f,
            bodyRadius,
            probeDistance);
        senses.RightObstacle = SampleObstacleProximity(
            state.Obstacles,
            creature.Position,
            right,
            bodyRadius,
            probeDistance);
    }

    private static float SampleObstacleProximity(
        ObstacleMap obstacles,
        SimVector2 origin,
        SimVector2 direction,
        float bodyRadius,
        float probeDistance)
    {
        if (direction.LengthSquared <= DirectionEpsilonSquared)
        {
            return 0f;
        }

        for (var step = 1; step <= ObstacleProbeSteps; step++)
        {
            var distance = probeDistance * step / ObstacleProbeSteps;
            if (obstacles.IsBlockedForCircle(origin + direction * distance, bodyRadius))
            {
                return 1f - Math.Clamp((distance - probeDistance / ObstacleProbeSteps) / probeDistance, 0f, 1f);
            }
        }

        return 0f;
    }

    private static void ApplyMemorySense(
        ref CreatureSenseState senses,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right)
    {
        senses.MemoryStrength = 0f;
        senses.MemoryDirectionForward = 0f;
        senses.MemoryDirectionRight = 0f;

        var memory = creature.MemoryVector.ClampedLength(1f);
        var memoryStrength = Math.Clamp(memory.Length, 0f, 1f);
        if (memoryStrength <= 0.000001f)
        {
            return;
        }

        senses.MemoryStrength = memoryStrength;
        senses.MemoryDirectionForward = Math.Clamp(SimVector2.Dot(memory, forward), -1f, 1f);
        senses.MemoryDirectionRight = Math.Clamp(SimVector2.Dot(memory, right), -1f, 1f);
    }

    private static float SpeedMultiplierToDrag(float speedMultiplier)
    {
        return Math.Clamp(1f - speedMultiplier, -1f, 1f);
    }

    private static bool HasUniformMultipliers(BiomePressureProfile profile)
    {
        const float epsilon = 0.000001f;
        return Math.Abs(profile.Barren - profile.Sparse) <= epsilon
            && Math.Abs(profile.Barren - profile.Grassland) <= epsilon
            && Math.Abs(profile.Barren - profile.Rich) <= epsilon
            && Math.Abs(profile.Barren - profile.Forest) <= epsilon
            && Math.Abs(profile.Barren - profile.Wetland) <= epsilon
            && Math.Abs(profile.Barren - profile.Tundra) <= epsilon
            && Math.Abs(profile.Barren - profile.Highland) <= epsilon;
    }

    private void ApplyMeatScentSense(
        ref CreatureSenseState senses,
        SimVector2 scentVector,
        float totalScentStrength,
        SimVector2 forward,
        SimVector2 right)
    {
        if (totalScentStrength <= MinimumScentStrength || scentVector.LengthSquared <= 0.000001f)
        {
            return;
        }

        var density = Math.Clamp(totalScentStrength / _meatScentDensitySaturation, 0f, 1f);
        var direction = scentVector.Normalized();
        var directionalConfidence = Math.Clamp(scentVector.Length / totalScentStrength, 0f, 1f) * density;

        senses.MeatScentDetected = true;
        senses.MeatScentDensity = density;
        senses.MeatScentDirectionForward = Math.Clamp(SimVector2.Dot(direction, forward), -1f, 1f) * directionalConfidence;
        senses.MeatScentDirectionRight = Math.Clamp(SimVector2.Dot(direction, right), -1f, 1f) * directionalConfidence;
    }

    private void ApplyRottenMeatScentSense(
        ref CreatureSenseState senses,
        SimVector2 scentVector,
        float totalScentStrength,
        SimVector2 forward,
        SimVector2 right)
    {
        if (totalScentStrength <= MinimumScentStrength || scentVector.LengthSquared <= 0.000001f)
        {
            return;
        }

        var density = Math.Clamp(totalScentStrength / _meatScentDensitySaturation, 0f, 1f);
        var direction = scentVector.Normalized();
        var directionalConfidence = Math.Clamp(scentVector.Length / totalScentStrength, 0f, 1f) * density;

        senses.RottenMeatScentDetected = true;
        senses.RottenMeatScentDensity = density;
        senses.RottenMeatScentDirectionForward = Math.Clamp(SimVector2.Dot(direction, forward), -1f, 1f) * directionalConfidence;
        senses.RottenMeatScentDirectionRight = Math.Clamp(SimVector2.Dot(direction, right), -1f, 1f) * directionalConfidence;
    }

    private void ApplyCreatureSimilarityScentSense(
        ref CreatureSenseState senses,
        SimVector2 scentVector,
        float totalScentStrength,
        SimVector2 forward,
        SimVector2 right)
    {
        if (totalScentStrength <= MinimumScentStrength)
        {
            return;
        }

        var density = Math.Clamp(totalScentStrength / CreatureSimilarityScentDensitySaturation, 0f, 1f);
        senses.CreatureSimilarityScentDetected = true;
        senses.CreatureSimilarityScentDensity = density;

        if (scentVector.LengthSquared <= 0.000001f)
        {
            senses.CreatureSimilarityScentDirectionForward = 0f;
            senses.CreatureSimilarityScentDirectionRight = 0f;
            return;
        }

        var direction = scentVector.Normalized();
        var directionalConfidence = Math.Clamp(scentVector.Length / totalScentStrength, 0f, 1f) * density;
        senses.CreatureSimilarityScentDirectionForward =
            Math.Clamp(SimVector2.Dot(direction, forward), -1f, 1f) * directionalConfidence;
        senses.CreatureSimilarityScentDirectionRight =
            Math.Clamp(SimVector2.Dot(direction, right), -1f, 1f) * directionalConfidence;
    }

    private void ApplySoundSense(
        ref CreatureSenseState senses,
        SimVector2 soundVector,
        float totalSoundStrength,
        float soundToneWeightedTotal,
        float soundToneSquaredWeightedTotal,
        SimVector2 forward,
        SimVector2 right)
    {
        if (totalSoundStrength <= MinimumScentStrength)
        {
            senses.SoundDetected = false;
            senses.SoundDensity = 0f;
            senses.SoundDirectionForward = 0f;
            senses.SoundDirectionRight = 0f;
            senses.SoundTone = 0f;
            senses.SoundToneClarity = 0f;
            return;
        }

        var density = Math.Clamp(totalSoundStrength / _soundDensitySaturation, 0f, 1f);
        var tone = Math.Clamp(soundToneWeightedTotal / totalSoundStrength, -1f, 1f);
        var toneMeanSquare = soundToneSquaredWeightedTotal / totalSoundStrength;
        var toneVariance = Math.Max(0f, toneMeanSquare - tone * tone);
        var toneClarity = Math.Clamp(1f - toneVariance, 0f, 1f) * density;

        senses.SoundDetected = true;
        senses.SoundDensity = density;
        senses.SoundTone = tone;
        senses.SoundToneClarity = toneClarity;
        if (soundVector.LengthSquared <= 0.000001f)
        {
            senses.SoundDirectionForward = 0f;
            senses.SoundDirectionRight = 0f;
            return;
        }

        var direction = soundVector.Normalized();
        var directionalConfidence = Math.Clamp(soundVector.Length / totalSoundStrength, 0f, 1f) * density;
        senses.SoundDirectionForward = Math.Clamp(SimVector2.Dot(direction, forward), -1f, 1f) * directionalConfidence;
        senses.SoundDirectionRight = Math.Clamp(SimVector2.Dot(direction, right), -1f, 1f) * directionalConfidence;
    }

    private CreatureAmbientSense CalculateCreatureAmbientSense(
        WorldState state,
        int creatureIndex,
        CreatureState creature,
        CreatureSensingTraits creatureTraits,
        SimVector2 forward,
        float scentRadius,
        float soundRadius,
        List<int> creatureCandidates)
    {
        _spatialIndex.AddCreatureCandidates(
            state,
            creature.Position,
            MathF.Max(scentRadius, soundRadius) + 12f,
            creatureCandidates);

        var totalScentStrength = 0f;
        var scentVector = SimVector2.Zero;
        var totalSoundStrength = 0f;
        var soundVector = SimVector2.Zero;
        var soundToneWeightedTotal = 0f;
        var soundToneSquaredWeightedTotal = 0f;
        var contactSimilarity = 0f;
        var hasContact = creature.IsTouchingCreature && creature.CreatureContactId != default;

        foreach (var otherCreatureIndex in creatureCandidates)
        {
            if (otherCreatureIndex == creatureIndex)
            {
                continue;
            }

            var otherCreature = state.Creatures[otherCreatureIndex];
            if (otherCreature.Id == creature.Id
                || otherCreature.Health <= 0f
                || otherCreature.Energy <= 0f)
            {
                continue;
            }

            var otherTraits = GetCreatureTraits(state, otherCreatureIndex);
            var similarity = CreatureSimilarity.GeneticSimilarity(creatureTraits.Genome, otherTraits.Genome);
            if (hasContact && otherCreature.Id == creature.CreatureContactId)
            {
                contactSimilarity = similarity;
            }

            var toOther = otherCreature.Position - creature.Position;
            var centerDistance = toOther.Length;
            var edgeDistance = Math.Max(0f, centerDistance - otherTraits.BodyRadius);
            if (edgeDistance <= scentRadius)
            {
                var similarityWeight = CreatureSimilarity.ScentWeight(similarity);
                if (similarityWeight > 0f)
                {
                    var distanceFactor = 1f - Math.Clamp(edgeDistance / scentRadius, 0f, 1f);
                    var scentStrength = similarityWeight * distanceFactor * distanceFactor;
                    if (scentStrength > MinimumScentStrength)
                    {
                        var scentDirection = centerDistance > 0.0001f
                            ? toOther / centerDistance
                            : forward;
                        totalScentStrength += scentStrength;
                        scentVector += scentDirection * scentStrength;
                    }
                }
            }

            var soundAmplitude = Math.Clamp(otherCreature.Actions.SoundAmplitude, 0f, 1f);
            if (soundAmplitude <= MinimumScentStrength || edgeDistance > soundRadius)
            {
                continue;
            }

            var soundDistanceFactor = 1f - Math.Clamp(edgeDistance / soundRadius, 0f, 1f);
            var soundStrength = soundAmplitude * soundDistanceFactor * soundDistanceFactor;
            if (soundStrength <= MinimumScentStrength)
            {
                continue;
            }

            var soundDirection = centerDistance > 0.0001f
                ? toOther / centerDistance
                : forward;
            var soundTone = Math.Clamp(otherCreature.Actions.SoundTone, -1f, 1f);
            totalSoundStrength += soundStrength;
            soundVector += soundDirection * soundStrength;
            soundToneWeightedTotal += soundTone * soundStrength;
            soundToneSquaredWeightedTotal += soundTone * soundTone * soundStrength;
        }

        return new CreatureAmbientSense(
            totalScentStrength,
            scentVector,
            contactSimilarity,
            totalSoundStrength,
            soundVector,
            soundToneWeightedTotal,
            soundToneSquaredWeightedTotal);
    }

    private static void ApplyGenericFoodSense(
        ref CreatureSenseState senses,
        ResourcePatchState resource,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var sense = CalculateResourceSense(resource, creature, forward, right, effectiveSenseRadius);
        senses.FoodDetected = true;
        senses.FoodProximity = sense.Proximity;
        senses.FoodDirectionForward = sense.DirectionForward;
        senses.FoodDirectionRight = sense.DirectionRight;
    }

    private static void ApplyGenericEggSense(
        ref CreatureSenseState senses,
        EggState egg,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var sense = CalculateFoodSense(egg.Position, EggPredation.ContactRadius(egg), creature, forward, right, effectiveSenseRadius);
        senses.FoodDetected = true;
        senses.FoodProximity = sense.Proximity;
        senses.FoodDirectionForward = sense.DirectionForward;
        senses.FoodDirectionRight = sense.DirectionRight;
    }

    private static void ApplyGenericSmallPreySense(
        ref CreatureSenseState senses,
        SmallPreyState prey,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var sense = CalculateFoodSense(prey.Position, prey.Radius, creature, forward, right, effectiveSenseRadius);
        senses.FoodDetected = true;
        senses.FoodProximity = sense.Proximity;
        senses.FoodDirectionForward = sense.DirectionForward;
        senses.FoodDirectionRight = sense.DirectionRight;
    }

    private static void ApplyPlantSense(
        ref CreatureSenseState senses,
        ResourcePatchState resource,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var sense = CalculateResourceSense(resource, creature, forward, right, effectiveSenseRadius);
        senses.PlantDetected = true;
        senses.PlantProximity = sense.Proximity;
        senses.PlantDirectionForward = sense.DirectionForward;
        senses.PlantDirectionRight = sense.DirectionRight;
    }

    private static void ApplyMeatSense(
        ref CreatureSenseState senses,
        ResourcePatchState resource,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var sense = CalculateResourceSense(resource, creature, forward, right, effectiveSenseRadius);
        senses.MeatDetected = true;
        senses.MeatProximity = sense.Proximity;
        senses.MeatDirectionForward = sense.DirectionForward;
        senses.MeatDirectionRight = sense.DirectionRight;
    }

    private static void ApplyMeatEggSense(
        ref CreatureSenseState senses,
        EggState egg,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var sense = CalculateFoodSense(egg.Position, EggPredation.ContactRadius(egg), creature, forward, right, effectiveSenseRadius);
        senses.MeatDetected = true;
        senses.MeatProximity = sense.Proximity;
        senses.MeatDirectionForward = sense.DirectionForward;
        senses.MeatDirectionRight = sense.DirectionRight;
    }

    private static void ApplyMeatSmallPreySense(
        ref CreatureSenseState senses,
        SmallPreyState prey,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var sense = CalculateFoodSense(prey.Position, prey.Radius, creature, forward, right, effectiveSenseRadius);
        senses.MeatDetected = true;
        senses.MeatProximity = sense.Proximity;
        senses.MeatDirectionForward = sense.DirectionForward;
        senses.MeatDirectionRight = sense.DirectionRight;
    }

    private static void ApplyCreatureSense(
        ref CreatureSenseState senses,
        CreatureState visibleCreature,
        CreatureSensingTraits visibleTraits,
        CreatureSensingTraits creatureTraits,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var sense = CalculateCreatureSense(
            visibleCreature,
            visibleTraits,
            creature,
            creatureTraits,
            forward,
            right,
            effectiveSenseRadius);
        senses.CreatureDetected = true;
        senses.CreatureProximity = sense.Proximity;
        senses.CreatureDirectionForward = sense.DirectionForward;
        senses.CreatureDirectionRight = sense.DirectionRight;
        senses.CreatureRelativeBodySize = sense.RelativeBodySize;
        senses.CreatureRelativeSpeed = sense.RelativeSpeed;
        senses.CreatureApproachRate = sense.ApproachRate;
        senses.CreatureFacingAlignment = sense.FacingAlignment;
        senses.PreyDetected = true;
        senses.PreyProximity = sense.Proximity;
        senses.PreyDirectionForward = sense.DirectionForward;
        senses.PreyDirectionRight = sense.DirectionRight;
    }

    private static ResourceSense CalculateResourceSense(
        ResourcePatchState resource,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        return CalculateFoodSense(resource.Position, resource.Radius, creature, forward, right, effectiveSenseRadius);
    }

    private static CreatureVisualSense CalculateCreatureSense(
        CreatureState visibleCreature,
        CreatureSensingTraits visibleTraits,
        CreatureState creature,
        CreatureSensingTraits creatureTraits,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var contactSense = CalculateFoodSense(
            visibleCreature.Position,
            visibleTraits.BodyRadius,
            creature,
            forward,
            right,
            effectiveSenseRadius);
        var selfRadius = creatureTraits.BodyRadius;
        var visibleRadius = visibleTraits.BodyRadius;
        var radiusScale = MathF.Max(0.001f, MathF.Max(selfRadius, visibleRadius));
        var relativeBodySize = Math.Clamp((visibleRadius - selfRadius) / radiusScale, -1f, 1f);

        var selfMaxSpeed = MathF.Max(1f, creatureTraits.MaxSpeed);
        var visibleMaxSpeed = MathF.Max(1f, visibleTraits.MaxSpeed);
        var relativeSpeed = Math.Clamp(
            (visibleCreature.Velocity.Length - creature.Velocity.Length) / selfMaxSpeed,
            -1f,
            1f);

        var toVisible = visibleCreature.Position - creature.Position;
        var centerDistance = toVisible.Length;
        var directionToVisible = centerDistance > 0.0001f
            ? toVisible / centerDistance
            : forward;
        var relativeVelocity = visibleCreature.Velocity - creature.Velocity;
        var approachScale = MathF.Max(1f, MathF.Max(selfMaxSpeed, visibleMaxSpeed));
        var approachRate = Math.Clamp(-SimVector2.Dot(relativeVelocity, directionToVisible) / approachScale, -1f, 1f);
        var visibleForward = SimVector2.FromAngle(visibleCreature.HeadingRadians);
        var facingAlignment = Math.Clamp(SimVector2.Dot(visibleForward, directionToVisible * -1f), -1f, 1f);

        return new CreatureVisualSense(
            contactSense.Proximity,
            contactSense.DirectionForward,
            contactSense.DirectionRight,
            relativeBodySize,
            relativeSpeed,
            approachRate,
            facingAlignment);
    }

    private static ResourceSense CalculateFoodSense(
        SimVector2 position,
        float radius,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var toResource = position - creature.Position;
        var centerDistance = toResource.Length;
        var edgeDistance = Math.Max(0f, centerDistance - radius);
        var direction = centerDistance > 0.0001f
            ? toResource / centerDistance
            : forward;

        return new ResourceSense(
            1f - Math.Clamp(edgeDistance / effectiveSenseRadius, 0f, 1f),
            Math.Clamp(SimVector2.Dot(direction, forward), -1f, 1f),
            Math.Clamp(SimVector2.Dot(direction, right), -1f, 1f));
    }

    private static bool IsInsideVisionCone(
        SimVector2 direction,
        SimVector2 forward,
        bool hasLimitedVision,
        float visionCosThreshold)
    {
        if (!hasLimitedVision)
        {
            return true;
        }

        return SimVector2.Dot(direction, forward) >= visionCosThreshold;
    }

    private static bool IsInsideVisionCone(
        SimVector2 toTarget,
        float distanceSquared,
        SimVector2 forward,
        bool hasLimitedVision,
        float visionCosThreshold)
    {
        if (!hasLimitedVision || distanceSquared <= DirectionEpsilonSquared)
        {
            return true;
        }

        var forwardDot = SimVector2.Dot(toTarget, forward);
        var thresholdSquaredDistance = visionCosThreshold * visionCosThreshold * distanceSquared;
        if (visionCosThreshold >= 0f)
        {
            return forwardDot >= 0f && forwardDot * forwardDot >= thresholdSquaredDistance;
        }

        return forwardDot >= 0f || forwardDot * forwardDot <= thresholdSquaredDistance;
    }

    private static bool IsWithinEdgeRange(float distanceSquared, float targetRadius, float senseRadius)
    {
        var maxCenterDistance = targetRadius + senseRadius;
        return distanceSquared <= maxCenterDistance * maxCenterDistance;
    }

    private static float PlantQualityClarity(float proximity)
    {
        var clamped = Math.Clamp(proximity, 0f, 1f);
        return clamped * clamped;
    }

    private void BeginTraitCache(WorldState state, bool precomputeTraits)
    {
        var creatureCount = state.Creatures.Count;
        if (_cachedTraitStamps.Length < creatureCount)
        {
            Array.Resize(ref _cachedBodyRadii, creatureCount);
            Array.Resize(ref _cachedMaxSpeeds, creatureCount);
            Array.Resize(ref _cachedTraitStamps, creatureCount);
        }

        if (_traitCacheStamp == int.MaxValue)
        {
            Array.Clear(_cachedTraitStamps);
            _traitCacheStamp = 0;
        }

        _traitCacheStamp++;

        for (var i = 0; i < creatureCount; i++)
        {
            if (precomputeTraits)
            {
                var creature = state.Creatures[i];
                var genome = state.GetGenome(creature.GenomeId);
                _cachedBodyRadii[i] = CreatureGrowth.EffectiveBodyRadius(creature, genome);
                _cachedMaxSpeeds[i] = CreatureGrowth.EffectiveMaxSpeed(creature, genome);
            }
            else
            {
                _cachedBodyRadii[i] = -1f;
                _cachedMaxSpeeds[i] = -1f;
            }

            _cachedTraitStamps[i] = _traitCacheStamp;
        }
    }

    private void EnsureParallelCreatureBufferCapacity(int creatureCount)
    {
        if (_parallelCreatureBuffer.Length >= creatureCount)
        {
            return;
        }

        _parallelCreatureBuffer = new CreatureState[Math.Max(creatureCount, _parallelCreatureBuffer.Length * 2)];
    }

    private CreatureSensingTraits GetCreatureTraits(WorldState state, int creatureIndex)
    {
        var creature = state.Creatures[creatureIndex];
        var genome = state.GetGenome(creature.GenomeId);
        if (_cachedTraitStamps[creatureIndex] != _traitCacheStamp
            || _cachedBodyRadii[creatureIndex] < 0f
            || _cachedMaxSpeeds[creatureIndex] < 0f)
        {
            if (_cachedBodyRadii[creatureIndex] < 0f
                || _cachedTraitStamps[creatureIndex] != _traitCacheStamp)
            {
                _cachedBodyRadii[creatureIndex] = CreatureGrowth.EffectiveBodyRadius(creature, genome);
            }

            if (_cachedMaxSpeeds[creatureIndex] < 0f
                || _cachedTraitStamps[creatureIndex] != _traitCacheStamp)
            {
                _cachedMaxSpeeds[creatureIndex] = CreatureGrowth.EffectiveMaxSpeed(creature, genome);
            }

            _cachedTraitStamps[creatureIndex] = _traitCacheStamp;
        }

        return new CreatureSensingTraits(
            _cachedBodyRadii[creatureIndex],
            _cachedMaxSpeeds[creatureIndex],
            genome);
    }

    private readonly record struct ResourceSense(
        float Proximity,
        float DirectionForward,
        float DirectionRight);

    private readonly record struct CreatureSensingTraits(
        float BodyRadius,
        float MaxSpeed,
        CreatureGenome Genome);

    private readonly record struct CreatureAmbientSense(
        float TotalScentStrength,
        SimVector2 ScentVector,
        float ContactSimilarity,
        float TotalSoundStrength,
        SimVector2 SoundVector,
        float SoundToneWeightedTotal,
        float SoundToneSquaredWeightedTotal);

    private readonly record struct CreatureVisualSense(
        float Proximity,
        float DirectionForward,
        float DirectionRight,
        float RelativeBodySize,
        float RelativeSpeed,
        float ApproachRate,
        float FacingAlignment);

    private sealed class CreatureSensingScratch
    {
        public List<int> PlantResourceCandidates { get; } = [];

        public IndexStampSet SeenPlantResourceCandidates { get; } = new();

        public List<int> MeatResourceCandidates { get; } = [];

        public IndexStampSet SeenMeatResourceCandidates { get; } = new();

        public List<int> EggCandidates { get; } = [];

        public IndexStampSet SeenEggCandidates { get; } = new();

        public List<int> SmallPreyCandidates { get; } = [];

        public IndexStampSet SeenSmallPreyCandidates { get; } = new();

        public List<int> CreatureCandidates { get; } = [];
    }
}
