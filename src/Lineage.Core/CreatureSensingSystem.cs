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
    private const int ObstacleProbeSteps = 4;
    private const float MinimumExpectedFoodTransfer = 0.001f;

    private readonly UniformSpatialIndex _spatialIndex;
    private readonly BiomePressureProfile _biomeSpeedProfile;
    private readonly float _meatScentRangeMultiplier;
    private readonly float _meatScentCaloriesForFullStrength;
    private readonly float _meatScentDensitySaturation;

    private readonly List<int> _plantResourceCandidates = [];
    private readonly IndexStampSet _seenPlantResourceCandidates = new();
    private readonly List<int> _meatResourceCandidates = [];
    private readonly IndexStampSet _seenMeatResourceCandidates = new();
    private readonly List<int> _eggCandidates = [];
    private readonly IndexStampSet _seenEggCandidates = new();
    private float[] _cachedBodyRadii = [];
    private float[] _cachedMaxSpeeds = [];
    private int[] _cachedTraitStamps = [];
    private int _traitCacheStamp;

    public CreatureSensingSystem(
        UniformSpatialIndex spatialIndex,
        float meatScentRangeMultiplier = 2f,
        float meatScentCaloriesForFullStrength = 60f,
        float meatScentDensitySaturation = 1f,
        BiomePressureProfile? biomeSpeedProfile = null)
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

        _spatialIndex = spatialIndex;
        _biomeSpeedProfile = biomeSpeedProfile ?? BiomePressureProfile.Neutral;
        _meatScentRangeMultiplier = meatScentRangeMultiplier;
        _meatScentCaloriesForFullStrength = meatScentCaloriesForFullStrength;
        _meatScentDensitySaturation = meatScentDensitySaturation;
    }

    public void Update(WorldState state, float deltaSeconds)
    {
        var sensingProfile = state.Profile?.Sensing;
        var traitCacheStartedAt = sensingProfile is not null
            ? Stopwatch.GetTimestamp()
            : 0L;
        BeginTraitCache(state);
        sensingProfile?.RecordTraitCache(
            state.Creatures.Count,
            Stopwatch.GetTimestamp() - traitCacheStartedAt);
        sensingProfile?.BeginUpdate(state.Creatures.Count);

        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            var effectiveSenseRadius = CreatureGrowth.EffectiveSenseRadius(creature, genome);
            var effectiveVisionAngle = CreatureGrowth.EffectiveVisionAngleRadians(creature, genome);
            var forward = SimVector2.FromAngle(creature.HeadingRadians);
            var right = new SimVector2(-forward.Y, forward.X);
            var hasLimitedVision = effectiveVisionAngle < MathF.Tau;
            var visionCosThreshold = hasLimitedVision
                ? MathF.Cos(effectiveVisionAngle * 0.5f)
                : -1f;
            var plantFoodEfficiency = CreatureDigestion.PlantEfficiency(genome);
            var freshMeatFoodEfficiency = CreatureDigestion.FreshMeatEnergyEfficiency(genome);
            var meatScentRadius = effectiveSenseRadius * _meatScentRangeMultiplier;

            var resourceQueryStartedAt = sensingProfile is not null
                ? Stopwatch.GetTimestamp()
                : 0L;
            _spatialIndex.AddPlantAndMeatResourceCandidatesWithCalories(
                state,
                creature.Position,
                effectiveSenseRadius,
                meatScentRadius,
                0f,
                _plantResourceCandidates,
                _seenPlantResourceCandidates,
                _meatResourceCandidates,
                _seenMeatResourceCandidates);
            sensingProfile?.RecordSplitResourceQuery(
                _plantResourceCandidates.Count,
                _meatResourceCandidates.Count,
                Stopwatch.GetTimestamp() - resourceQueryStartedAt);

            var eggQueryStartedAt = sensingProfile is not null
                ? Stopwatch.GetTimestamp()
                : 0L;
            _spatialIndex.AddEggCandidatesWithEnergy(
                state,
                creature.Position,
                effectiveSenseRadius,
                minimumEnergy: 0f,
                _eggCandidates,
                _seenEggCandidates);
            sensingProfile?.RecordEggQuery(
                _eggCandidates.Count,
                Stopwatch.GetTimestamp() - eggQueryStartedAt);

            var creatureVisibilityStartedAt = sensingProfile is not null
                ? Stopwatch.GetTimestamp()
                : 0L;
            var creatureVisibility = _spatialIndex.FindNearestVisibleCreature(
                state,
                i,
                creature.Id,
                creature.Position,
                effectiveSenseRadius + 12f,
                effectiveSenseRadius,
                forward,
                hasLimitedVision,
                visionCosThreshold,
                _cachedBodyRadii);
            sensingProfile?.RecordCreatureQuery(
                creatureVisibility,
                Stopwatch.GetTimestamp() - creatureVisibilityStartedAt);
            sensingProfile?.RecordCreatureScan(
                creatureVisibility.VisibleCount,
                0L);

            var energyRatio = Math.Clamp(creature.Energy / genome.ReproductionEnergyThreshold, 0f, 1f);
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
            var isReadyToLay =
                eggReserveRatio >= 1f
                && creature.AgeSeconds >= genome.MaturityAgeSeconds
                && creature.ReproductionCooldownSeconds <= 0f;
            var senses = new CreatureSenseState
            {
                EnergyRatio = energyRatio,
                Hunger = 1f - energyRatio,
                EggReserveRatio = eggReserveRatio,
                EnergySurplusRatio = energySurplusRatio,
                RecentFoodSuccess = recentFoodSuccess,
                ReproductionReadiness = isReadyToLay ? 1f : 0f
            };
            ApplyTerrainDragSense(ref senses, state, creature, forward, right, effectiveSenseRadius);
            var obstacleSenseStartedAt = sensingProfile is not null
                ? Stopwatch.GetTimestamp()
                : 0L;
            ApplyObstacleSense(ref senses, state, creature, genome, forward, right, effectiveSenseRadius);
            sensingProfile?.RecordObstacleSense(Stopwatch.GetTimestamp() - obstacleSenseStartedAt);
            ApplyMemorySense(ref senses, creature, forward, right);

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

            var resourceScanStartedAt = sensingProfile is not null
                ? Stopwatch.GetTimestamp()
                : 0L;
            var plantCandidates = 0;
            var meatResourceCandidates = 0;
            var visiblePlantCandidates = 0;
            var visibleMeatResourceCandidates = 0;

            foreach (var resourceIndex in _plantResourceCandidates)
            {
                var resource = state.Resources[resourceIndex];
                plantCandidates++;
                var toResource = resource.Position - creature.Position;
                var distanceSquared = toResource.LengthSquared;

                if (!IsWithinEdgeRange(distanceSquared, resource.Radius, effectiveSenseRadius)
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

                var proximity = 1f - Math.Clamp(edgeDistance / effectiveSenseRadius, 0f, 1f);
                var foodScore = proximity * plantFoodEfficiency;
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

            foreach (var resourceIndex in _meatResourceCandidates)
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

                if (edgeDistance > effectiveSenseRadius
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

                var proximity = 1f - Math.Clamp(edgeDistance / effectiveSenseRadius, 0f, 1f);
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

            foreach (var eggIndex in _eggCandidates)
            {
                var egg = state.Eggs[eggIndex];
                var eggRadius = EggPredation.ContactRadius(egg);
                var toEgg = egg.Position - creature.Position;
                var distanceSquared = toEgg.LengthSquared;

                if (!IsWithinEdgeRange(distanceSquared, eggRadius, effectiveSenseRadius)
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

                var proximity = 1f - Math.Clamp(edgeDistance / effectiveSenseRadius, 0f, 1f);
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

            senses.VisibleFoodDensity = Math.Clamp(visibleFoodCount / DensitySaturationFoodCount, 0f, 1f);
            senses.VisiblePlantDensity = Math.Clamp(visiblePlantCount / DensitySaturationFoodCount, 0f, 1f);
            senses.VisibleMeatDensity = Math.Clamp(visibleMeatCount / DensitySaturationFoodCount, 0f, 1f);
            senses.VisibleCreatureDensity = Math.Clamp(visibleCreatureCount / DensitySaturationFoodCount, 0f, 1f);
            senses.VisiblePreyDensity = senses.VisibleCreatureDensity;
            ApplyMeatScentSense(ref senses, meatScentVector, totalMeatScentStrength, forward, right);
            ApplyRottenMeatScentSense(ref senses, rottenMeatScentVector, totalRottenMeatScentStrength, forward, right);

            if (bestVisibleFoodKind == FoodContactKind.Resource && bestVisibleFoodIndex >= 0)
            {
                ApplyGenericFoodSense(
                    ref senses,
                    state.Resources[bestVisibleFoodIndex],
                    creature,
                    forward,
                    right,
                    effectiveSenseRadius);
            }
            else if (bestVisibleFoodKind == FoodContactKind.Egg && bestVisibleFoodIndex >= 0)
            {
                ApplyGenericEggSense(
                    ref senses,
                    state.Eggs[bestVisibleFoodIndex],
                    creature,
                    forward,
                    right,
                    effectiveSenseRadius);
            }
            if (nearestVisiblePlantIndex >= 0)
            {
                ApplyPlantSense(
                    ref senses,
                    state.Resources[nearestVisiblePlantIndex],
                    creature,
                    forward,
                    right,
                    effectiveSenseRadius);
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
                    effectiveSenseRadius);
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
                    effectiveSenseRadius);
            }
            if (nearestVisibleCreatureIndex >= 0)
            {
                ApplyCreatureSense(
                    ref senses,
                    state.Creatures[nearestVisibleCreatureIndex],
                    GetCreatureTraits(state, nearestVisibleCreatureIndex),
                    GetCreatureTraits(state, i),
                    creature,
                    forward,
                    right,
                    effectiveSenseRadius);
            }

            creature.Senses = senses;
            state.Creatures[i] = creature;
        }
    }

    private void ApplyTerrainDragSense(
        ref CreatureSenseState senses,
        WorldState state,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var currentSpeedMultiplier = _biomeSpeedProfile.For(state.Biomes.GetKindAt(creature.Position));
        var probeDistance = Math.Clamp(
            effectiveSenseRadius * 0.5f,
            MinimumTerrainProbeDistance,
            MaximumTerrainProbeDistance);
        var forwardPosition = state.Bounds.Clamp(creature.Position + forward * probeDistance);
        var leftPosition = state.Bounds.Clamp(creature.Position - right * probeDistance);
        var rightPosition = state.Bounds.Clamp(creature.Position + right * probeDistance);
        var forwardSpeedMultiplier = _biomeSpeedProfile.For(state.Biomes.GetKindAt(forwardPosition));
        var leftSpeedMultiplier = _biomeSpeedProfile.For(state.Biomes.GetKindAt(leftPosition));
        var rightSpeedMultiplier = _biomeSpeedProfile.For(state.Biomes.GetKindAt(rightPosition));

        senses.CurrentTerrainDrag = SpeedMultiplierToDrag(currentSpeedMultiplier);
        senses.ForwardTerrainDrag = SpeedMultiplierToDrag(forwardSpeedMultiplier);
        senses.LeftTerrainDrag = SpeedMultiplierToDrag(leftSpeedMultiplier);
        senses.RightTerrainDrag = SpeedMultiplierToDrag(rightSpeedMultiplier);
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
        var normalized = direction.Normalized();
        if (normalized.LengthSquared <= DirectionEpsilonSquared)
        {
            return 0f;
        }

        for (var step = 1; step <= ObstacleProbeSteps; step++)
        {
            var distance = probeDistance * step / ObstacleProbeSteps;
            if (obstacles.IsBlockedForCircle(origin + normalized * distance, bodyRadius))
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

    private void BeginTraitCache(WorldState state)
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
            _cachedBodyRadii[i] = -1f;
            _cachedMaxSpeeds[i] = -1f;
            _cachedTraitStamps[i] = _traitCacheStamp;
        }
    }

    private CreatureSensingTraits GetCreatureTraits(WorldState state, int creatureIndex)
    {
        if (_cachedTraitStamps[creatureIndex] != _traitCacheStamp
            || _cachedBodyRadii[creatureIndex] < 0f
            || _cachedMaxSpeeds[creatureIndex] < 0f)
        {
            var creature = state.Creatures[creatureIndex];
            var genome = state.GetGenome(creature.GenomeId);
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
            _cachedMaxSpeeds[creatureIndex]);
    }

    private readonly record struct ResourceSense(
        float Proximity,
        float DirectionForward,
        float DirectionRight);

    private readonly record struct CreatureSensingTraits(
        float BodyRadius,
        float MaxSpeed);

    private readonly record struct CreatureVisualSense(
        float Proximity,
        float DirectionForward,
        float DirectionRight,
        float RelativeBodySize,
        float RelativeSpeed,
        float ApproachRate,
        float FacingAlignment);
}
