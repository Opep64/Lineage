namespace Lineage.Core;

/// <summary>
/// Converts nearby world state and internal state into explicit creature senses.
/// </summary>
public sealed class CreatureSensingSystem(UniformSpatialIndex spatialIndex) : ISimulationSystem
{
    private const float DensitySaturationFoodCount = 8f;

    private readonly List<int> _resourceCandidates = [];
    private readonly HashSet<int> _seenResourceCandidates = [];
    private readonly List<int> _eggCandidates = [];
    private readonly HashSet<int> _seenEggCandidates = [];
    private readonly List<int> _creatureCandidates = [];
    private readonly HashSet<int> _seenCreatureCandidates = [];

    public void Update(WorldState state, float deltaSeconds)
    {
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            var effectiveSenseRadius = CreatureGrowth.EffectiveSenseRadius(creature, genome);
            var effectiveVisionAngle = CreatureGrowth.EffectiveVisionAngleRadians(creature, genome);
            spatialIndex.AddResourceCandidatesWithCalories(
                state,
                creature.Position,
                effectiveSenseRadius,
                minimumCalories: 0f,
                _resourceCandidates,
                _seenResourceCandidates);
            spatialIndex.AddEggCandidatesWithEnergy(
                state,
                creature.Position,
                effectiveSenseRadius,
                minimumEnergy: 0f,
                _eggCandidates,
                _seenEggCandidates);
            spatialIndex.AddCreatureCandidates(
                state,
                creature.Position,
                effectiveSenseRadius + 12f,
                _creatureCandidates,
                _seenCreatureCandidates);

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
            var visiblePreyCount = 0;
            var bestVisibleFoodKind = FoodContactKind.None;
            var bestVisibleFoodIndex = -1;
            var bestVisibleFoodScore = float.NegativeInfinity;
            var bestVisibleFoodDistanceSquared = float.PositiveInfinity;
            var nearestVisiblePlantIndex = -1;
            var nearestVisiblePlantDistanceSquared = float.PositiveInfinity;
            var nearestVisibleMeatKind = FoodContactKind.None;
            var nearestVisibleMeatIndex = -1;
            var nearestVisibleMeatDistanceSquared = float.PositiveInfinity;
            var nearestVisiblePreyIndex = -1;
            var nearestVisiblePreyDistanceSquared = float.PositiveInfinity;
            var forward = SimVector2.FromAngle(creature.HeadingRadians);
            var right = new SimVector2(-forward.Y, forward.X);

            foreach (var resourceIndex in _resourceCandidates)
            {
                var resource = state.Resources[resourceIndex];
                var toResource = resource.Position - creature.Position;
                var centerDistance = toResource.Length;
                var edgeDistance = Math.Max(0f, centerDistance - resource.Radius);
                var direction = centerDistance > 0.0001f
                    ? toResource / centerDistance
                    : SimVector2.FromAngle(creature.HeadingRadians);

                if (!IsInsideVisionCone(direction, forward, effectiveVisionAngle))
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
                var foodScore = proximity * CreatureDigestion.EfficiencyFor(genome, resource.Kind);
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

                if (!IsInsideVisionCone(direction, forward, effectiveVisionAngle))
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
                var foodScore = proximity * CreatureDigestion.MeatEfficiency(genome);
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

            foreach (var preyIndex in _creatureCandidates)
            {
                if (preyIndex == i)
                {
                    continue;
                }

                var prey = state.Creatures[preyIndex];
                if (prey.Id == creature.Id || prey.Health <= 0f || prey.Energy <= 0f)
                {
                    continue;
                }

                var preyGenome = state.GetGenome(prey.GenomeId);
                var preyRadius = CreatureGrowth.EffectiveBodyRadius(prey, preyGenome);
                var toPrey = prey.Position - creature.Position;
                var centerDistance = toPrey.Length;
                var edgeDistance = Math.Max(0f, centerDistance - preyRadius);
                if (edgeDistance > effectiveSenseRadius)
                {
                    continue;
                }

                var direction = centerDistance > 0.0001f
                    ? toPrey / centerDistance
                    : forward;

                if (!IsInsideVisionCone(direction, forward, effectiveVisionAngle))
                {
                    continue;
                }

                visibleFoodCount++;
                visibleMeatCount++;
                visiblePreyCount++;

                var distanceSquared = centerDistance * centerDistance;
                if (distanceSquared < nearestVisibleMeatDistanceSquared)
                {
                    nearestVisibleMeatDistanceSquared = distanceSquared;
                    nearestVisibleMeatKind = FoodContactKind.Creature;
                    nearestVisibleMeatIndex = preyIndex;
                }

                if (distanceSquared < nearestVisiblePreyDistanceSquared)
                {
                    nearestVisiblePreyDistanceSquared = distanceSquared;
                    nearestVisiblePreyIndex = preyIndex;
                }

                var proximity = 1f - Math.Clamp(edgeDistance / effectiveSenseRadius, 0f, 1f);
                var foodScore = proximity * CreatureDigestion.MeatEfficiency(genome);
                if (foodScore > bestVisibleFoodScore
                    || (Math.Abs(foodScore - bestVisibleFoodScore) <= 0.0001f
                        && distanceSquared < bestVisibleFoodDistanceSquared))
                {
                    bestVisibleFoodScore = foodScore;
                    bestVisibleFoodDistanceSquared = distanceSquared;
                    bestVisibleFoodKind = FoodContactKind.Creature;
                    bestVisibleFoodIndex = preyIndex;
                }
            }

            senses.VisibleFoodDensity = Math.Clamp(visibleFoodCount / DensitySaturationFoodCount, 0f, 1f);
            senses.VisiblePlantDensity = Math.Clamp(visiblePlantCount / DensitySaturationFoodCount, 0f, 1f);
            senses.VisibleMeatDensity = Math.Clamp(visibleMeatCount / DensitySaturationFoodCount, 0f, 1f);
            senses.VisiblePreyDensity = Math.Clamp(visiblePreyCount / DensitySaturationFoodCount, 0f, 1f);

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
            else if (bestVisibleFoodKind == FoodContactKind.Creature && bestVisibleFoodIndex >= 0)
            {
                ApplyGenericPreySense(
                    ref senses,
                    state.Creatures[bestVisibleFoodIndex],
                    state.GetGenome(state.Creatures[bestVisibleFoodIndex].GenomeId),
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
            else if (nearestVisibleMeatKind == FoodContactKind.Creature && nearestVisibleMeatIndex >= 0)
            {
                ApplyMeatPreySense(
                    ref senses,
                    state.Creatures[nearestVisibleMeatIndex],
                    state.GetGenome(state.Creatures[nearestVisibleMeatIndex].GenomeId),
                    creature,
                    forward,
                    right,
                    effectiveSenseRadius);
            }

            if (nearestVisiblePreyIndex >= 0)
            {
                ApplyPreySense(
                    ref senses,
                    state.Creatures[nearestVisiblePreyIndex],
                    state.GetGenome(state.Creatures[nearestVisiblePreyIndex].GenomeId),
                    creature,
                    forward,
                    right,
                    effectiveSenseRadius);
            }

            creature.Senses = senses;
            state.Creatures[i] = creature;
        }
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

    private static void ApplyGenericPreySense(
        ref CreatureSenseState senses,
        CreatureState prey,
        CreatureGenome preyGenome,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var sense = CalculateCreatureSense(prey, preyGenome, creature, forward, right, effectiveSenseRadius);
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

    private static void ApplyMeatPreySense(
        ref CreatureSenseState senses,
        CreatureState prey,
        CreatureGenome preyGenome,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var sense = CalculateCreatureSense(prey, preyGenome, creature, forward, right, effectiveSenseRadius);
        senses.MeatDetected = true;
        senses.MeatProximity = sense.Proximity;
        senses.MeatDirectionForward = sense.DirectionForward;
        senses.MeatDirectionRight = sense.DirectionRight;
    }

    private static void ApplyPreySense(
        ref CreatureSenseState senses,
        CreatureState prey,
        CreatureGenome preyGenome,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        var sense = CalculateCreatureSense(prey, preyGenome, creature, forward, right, effectiveSenseRadius);
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

    private static ResourceSense CalculateCreatureSense(
        CreatureState prey,
        CreatureGenome preyGenome,
        CreatureState creature,
        SimVector2 forward,
        SimVector2 right,
        float effectiveSenseRadius)
    {
        return CalculateFoodSense(
            prey.Position,
            CreatureGrowth.EffectiveBodyRadius(prey, preyGenome),
            creature,
            forward,
            right,
            effectiveSenseRadius);
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

    private static bool IsInsideVisionCone(SimVector2 direction, SimVector2 forward, float visionAngleRadians)
    {
        if (visionAngleRadians >= MathF.Tau)
        {
            return true;
        }

        var halfAngle = visionAngleRadians * 0.5f;
        return SimVector2.Dot(direction, forward) >= MathF.Cos(halfAngle);
    }

    private readonly record struct ResourceSense(
        float Proximity,
        float DirectionForward,
        float DirectionRight);
}
