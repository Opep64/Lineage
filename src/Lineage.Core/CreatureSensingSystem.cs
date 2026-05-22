namespace Lineage.Core;

/// <summary>
/// Converts nearby world state and internal state into explicit creature senses.
/// </summary>
public sealed class CreatureSensingSystem : ISimulationSystem
{
    private const float DensitySaturationFoodCount = 8f;
    private const float MinimumScentStrength = 0.001f;

    private readonly UniformSpatialIndex _spatialIndex;
    private readonly float _meatScentRangeMultiplier;
    private readonly float _meatScentCaloriesForFullStrength;
    private readonly float _meatScentDensitySaturation;

    private readonly List<int> _scentResourceCandidates = [];
    private readonly HashSet<int> _seenScentResourceCandidates = [];
    private readonly List<int> _eggCandidates = [];
    private readonly HashSet<int> _seenEggCandidates = [];
    private readonly List<int> _creatureCandidates = [];
    private float[] _cachedBodyRadii = [];
    private float[] _cachedMaxSpeeds = [];
    private int[] _cachedTraitStamps = [];
    private int _traitCacheStamp;

    public CreatureSensingSystem(
        UniformSpatialIndex spatialIndex,
        float meatScentRangeMultiplier = 2f,
        float meatScentCaloriesForFullStrength = 60f,
        float meatScentDensitySaturation = 1f)
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
        _meatScentRangeMultiplier = meatScentRangeMultiplier;
        _meatScentCaloriesForFullStrength = meatScentCaloriesForFullStrength;
        _meatScentDensitySaturation = meatScentDensitySaturation;
    }

    public void Update(WorldState state, float deltaSeconds)
    {
        BeginTraitCache(state.Creatures.Count);

        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            var effectiveSenseRadius = CreatureGrowth.EffectiveSenseRadius(creature, genome);
            var effectiveVisionAngle = CreatureGrowth.EffectiveVisionAngleRadians(creature, genome);
            var hasLimitedVision = effectiveVisionAngle < MathF.Tau;
            var visionCosThreshold = hasLimitedVision
                ? MathF.Cos(effectiveVisionAngle * 0.5f)
                : -1f;
            var plantFoodEfficiency = CreatureDigestion.PlantEfficiency(genome);
            var freshMeatFoodEfficiency = CreatureDigestion.FreshMeatEnergyEfficiency(genome);
            var meatScentRadius = effectiveSenseRadius * _meatScentRangeMultiplier;
            _spatialIndex.AddResourceCandidatesWithCalories(
                state,
                creature.Position,
                meatScentRadius,
                minimumCalories: 0f,
                _scentResourceCandidates,
                _seenScentResourceCandidates);
            _spatialIndex.AddEggCandidatesWithEnergy(
                state,
                creature.Position,
                effectiveSenseRadius,
                minimumEnergy: 0f,
                _eggCandidates,
                _seenEggCandidates);
            _spatialIndex.AddCreatureCandidates(
                state,
                creature.Position,
                effectiveSenseRadius + 12f,
                _creatureCandidates);

            var energyRatio = Math.Clamp(creature.Energy / genome.ReproductionEnergyThreshold, 0f, 1f);
            var eggReserveRatio = Math.Clamp(creature.ReproductiveEnergy / genome.OffspringEnergyInvestment, 0f, 1f);
            var isReadyToLay =
                eggReserveRatio >= 1f
                && creature.AgeSeconds >= genome.MaturityAgeSeconds
                && creature.ReproductionCooldownSeconds <= 0f;
            var senses = new CreatureSenseState
            {
                EnergyRatio = energyRatio,
                Hunger = 1f - energyRatio,
                EggReserveRatio = eggReserveRatio,
                ReproductionReadiness = isReadyToLay ? 1f : 0f
            };

            var visibleFoodCount = 0;
            var visiblePlantCount = 0;
            var visibleMeatCount = 0;
            var visibleCreatureCount = 0;
            var totalMeatScentStrength = 0f;
            var meatScentVector = SimVector2.Zero;
            var bestVisibleFoodKind = FoodContactKind.None;
            var bestVisibleFoodIndex = -1;
            var bestVisibleFoodScore = float.NegativeInfinity;
            var bestVisibleFoodDistanceSquared = float.PositiveInfinity;
            var nearestVisiblePlantIndex = -1;
            var nearestVisiblePlantDistanceSquared = float.PositiveInfinity;
            var nearestVisibleMeatKind = FoodContactKind.None;
            var nearestVisibleMeatIndex = -1;
            var nearestVisibleMeatDistanceSquared = float.PositiveInfinity;
            var nearestVisibleCreatureIndex = -1;
            var nearestVisibleCreatureDistanceSquared = float.PositiveInfinity;
            var forward = SimVector2.FromAngle(creature.HeadingRadians);
            var right = new SimVector2(-forward.Y, forward.X);

            foreach (var resourceIndex in _scentResourceCandidates)
            {
                var resource = state.Resources[resourceIndex];
                var toResource = resource.Position - creature.Position;
                var centerDistance = toResource.Length;
                var edgeDistance = Math.Max(0f, centerDistance - resource.Radius);

                if (resource.Kind == ResourceKind.Meat)
                {
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
                        }
                    }
                }

                if (edgeDistance > effectiveSenseRadius)
                {
                    continue;
                }

                var direction = centerDistance > 0.0001f
                    ? toResource / centerDistance
                    : forward;

                if (!IsInsideVisionCone(direction, forward, hasLimitedVision, visionCosThreshold))
                {
                    continue;
                }

                visibleFoodCount++;

                if (resource.Kind == ResourceKind.Meat)
                {
                    visibleMeatCount++;
                }
                else
                {
                    visiblePlantCount++;
                }

                var distanceSquared = centerDistance * centerDistance;
                if (resource.Kind == ResourceKind.Meat && distanceSquared < nearestVisibleMeatDistanceSquared)
                {
                    nearestVisibleMeatDistanceSquared = distanceSquared;
                    nearestVisibleMeatKind = FoodContactKind.Resource;
                    nearestVisibleMeatIndex = resourceIndex;
                }
                else if (resource.Kind != ResourceKind.Meat && distanceSquared < nearestVisiblePlantDistanceSquared)
                {
                    nearestVisiblePlantDistanceSquared = distanceSquared;
                    nearestVisiblePlantIndex = resourceIndex;
                }

                var proximity = 1f - Math.Clamp(edgeDistance / effectiveSenseRadius, 0f, 1f);
                var resourceFoodEfficiency = resource.Kind == ResourceKind.Meat
                    ? CreatureDigestion.MeatEnergyEfficiency(genome, MeatQuality.Freshness(resource))
                    : plantFoodEfficiency;
                var foodScore = proximity * resourceFoodEfficiency;
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

            foreach (var eggIndex in _eggCandidates)
            {
                var egg = state.Eggs[eggIndex];
                var eggRadius = EggPredation.ContactRadius(egg);
                var toEgg = egg.Position - creature.Position;
                var centerDistance = toEgg.Length;
                var edgeDistance = Math.Max(0f, centerDistance - eggRadius);
                var direction = centerDistance > 0.0001f
                    ? toEgg / centerDistance
                    : forward;

                if (!IsInsideVisionCone(direction, forward, hasLimitedVision, visionCosThreshold))
                {
                    continue;
                }

                visibleFoodCount++;
                visibleMeatCount++;

                var distanceSquared = centerDistance * centerDistance;
                if (distanceSquared < nearestVisibleMeatDistanceSquared)
                {
                    nearestVisibleMeatDistanceSquared = distanceSquared;
                    nearestVisibleMeatKind = FoodContactKind.Egg;
                    nearestVisibleMeatIndex = eggIndex;
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

            foreach (var otherCreatureIndex in _creatureCandidates)
            {
                if (otherCreatureIndex == i)
                {
                    continue;
                }

                var otherCreature = state.Creatures[otherCreatureIndex];
                if (otherCreature.Id == creature.Id || otherCreature.Health <= 0f || otherCreature.Energy <= 0f)
                {
                    continue;
                }

                var otherTraits = GetCreatureTraits(state, otherCreatureIndex);
                var otherRadius = otherTraits.BodyRadius;
                var toOther = otherCreature.Position - creature.Position;
                var centerDistance = toOther.Length;
                var edgeDistance = Math.Max(0f, centerDistance - otherRadius);
                if (edgeDistance > effectiveSenseRadius)
                {
                    continue;
                }

                var direction = centerDistance > 0.0001f
                    ? toOther / centerDistance
                    : forward;

                if (!IsInsideVisionCone(direction, forward, hasLimitedVision, visionCosThreshold))
                {
                    continue;
                }

                visibleCreatureCount++;

                var distanceSquared = centerDistance * centerDistance;
                if (distanceSquared < nearestVisibleCreatureDistanceSquared)
                {
                    nearestVisibleCreatureDistanceSquared = distanceSquared;
                    nearestVisibleCreatureIndex = otherCreatureIndex;
                }
            }

            senses.VisibleFoodDensity = Math.Clamp(visibleFoodCount / DensitySaturationFoodCount, 0f, 1f);
            senses.VisiblePlantDensity = Math.Clamp(visiblePlantCount / DensitySaturationFoodCount, 0f, 1f);
            senses.VisibleMeatDensity = Math.Clamp(visibleMeatCount / DensitySaturationFoodCount, 0f, 1f);
            senses.VisibleCreatureDensity = Math.Clamp(visibleCreatureCount / DensitySaturationFoodCount, 0f, 1f);
            senses.VisiblePreyDensity = senses.VisibleCreatureDensity;
            ApplyMeatScentSense(ref senses, meatScentVector, totalMeatScentStrength, forward, right);

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

    private void BeginTraitCache(int creatureCount)
    {
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
    }

    private CreatureSensingTraits GetCreatureTraits(WorldState state, int creatureIndex)
    {
        if (_cachedTraitStamps[creatureIndex] != _traitCacheStamp)
        {
            var creature = state.Creatures[creatureIndex];
            var genome = state.GetGenome(creature.GenomeId);
            _cachedBodyRadii[creatureIndex] = CreatureGrowth.EffectiveBodyRadius(creature, genome);
            _cachedMaxSpeeds[creatureIndex] = CreatureGrowth.EffectiveMaxSpeed(creature, genome);
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
