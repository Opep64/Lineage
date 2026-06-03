namespace Lineage.Core;

/// <summary>
/// Transfers raw calories from reachable food into a creature's gut.
/// </summary>
public sealed class EatingSystem(
    UniformSpatialIndex spatialIndex,
    float reachPadding = 0f,
    bool requireEatIntent = false) : ISimulationSystem
{
    private readonly List<int> _resourceCandidates = [];
    private readonly IndexStampSet _seenResourceCandidates = new();
    private readonly List<int> _eggCandidates = [];
    private readonly IndexStampSet _seenEggCandidates = new();
    private readonly List<int> _smallPreyCandidates = [];
    private readonly IndexStampSet _seenSmallPreyCandidates = new();

    public void Update(WorldState state, float deltaSeconds)
    {
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            var effectiveBodyRadius = CreatureGrowth.EffectiveBodyRadius(creature, genome);
            var contactRadius = effectiveBodyRadius + reachPadding;

            spatialIndex.AddResourceCandidatesWithCalories(
                state,
                creature.Position,
                contactRadius,
                minimumCalories: 0f,
                _resourceCandidates,
                _seenResourceCandidates);
            spatialIndex.AddEggCandidatesWithEnergy(
                state,
                creature.Position,
                contactRadius,
                minimumEnergy: 0f,
                _eggCandidates,
                _seenEggCandidates);
            spatialIndex.AddSmallPreyCandidatesWithCalories(
                state,
                creature.Position,
                contactRadius,
                minimumCalories: 0f,
                _smallPreyCandidates,
                _seenSmallPreyCandidates);

            creature.IsTouchingFood = false;
            creature.FoodContactKind = FoodContactKind.None;
            creature.FoodContactResourceKind = default;
            creature.FoodContactPlantKind = default;
            creature.FoodContactResourceId = default;
            creature.FoodContactEdgeDistance = 0f;
            creature.FoodContactCalories = 0f;
            creature.LastCaloriesEaten = 0f;
            creature.LastPlantCaloriesEaten = 0f;
            creature.LastTenderPlantCaloriesEaten = 0f;
            creature.LastRichPlantCaloriesEaten = 0f;
            creature.LastToughPlantCaloriesEaten = 0f;
            creature.LastCarcassCaloriesEaten = 0f;
            creature.LastEggCaloriesEaten = 0f;
            creature.LastLivePreyCaloriesEaten = 0f;
            creature.LastSmallPreyCaloriesEaten = 0f;
            creature.LastFreshMeatCaloriesEaten = 0f;
            creature.LastStaleMeatCaloriesEaten = 0f;

            var target = FindBestFoodContact(state, creature, genome, contactRadius);

            if (target.Kind == FoodContactKind.None)
            {
                state.Creatures[i] = creature;
                continue;
            }

            creature.IsTouchingFood = true;
            creature.FoodContactKind = target.Kind;
            creature.FoodContactResourceKind = target.ResourceKind;
            creature.FoodContactPlantKind = target.PlantKind;
            creature.FoodContactResourceId = target.Id;
            creature.FoodContactEdgeDistance = target.EdgeDistance;
            creature.FoodContactCalories = target.Calories;

            if (requireEatIntent && !creature.Actions.WantsEat)
            {
                state.Creatures[i] = creature;
                continue;
            }

            if (target.Kind == FoodContactKind.Resource)
            {
                EatResource(state, target.Index, ref creature, genome, deltaSeconds);
            }
            else if (target.Kind == FoodContactKind.Egg)
            {
                EatEgg(state, target.Index, ref creature, genome, deltaSeconds);
            }
            else if (target.Kind == FoodContactKind.SmallPrey)
            {
                EatSmallPrey(state, target.Index, ref creature, genome, deltaSeconds);
            }

            state.Creatures[i] = creature;
        }
    }

    private FoodContact FindBestFoodContact(
        WorldState state,
        CreatureState creature,
        CreatureGenome genome,
        float contactRadius)
    {
        var best = FoodContact.None;
        var bestEfficiency = float.NegativeInfinity;
        var bestEdgeDistance = float.PositiveInfinity;
        var bestDistanceSquared = float.PositiveInfinity;

        foreach (var resourceIndex in _resourceCandidates)
        {
            var resource = state.Resources[resourceIndex];
            if (resource.Calories <= 0f)
            {
                continue;
            }

            var centerDistance = SimVector2.Distance(creature.Position, resource.Position);
            var edgeDistance = Math.Max(0f, centerDistance - resource.Radius);
            if (edgeDistance > contactRadius)
            {
                continue;
            }

            var efficiency = resource.Kind == ResourceKind.Meat
                ? CreatureDigestion.MeatEnergyEfficiency(genome, MeatQuality.Freshness(resource))
                : CreatureDigestion.PlantTypeEnergyEfficiency(genome, resource.PlantKind);

            var distanceSquared = centerDistance * centerDistance;
            if (IsBetterFoodContact(efficiency, edgeDistance, distanceSquared, bestEfficiency, bestEdgeDistance, bestDistanceSquared))
            {
                best = new FoodContact(
                    FoodContactKind.Resource,
                    resource.Kind,
                    resource.PlantKind,
                    resourceIndex,
                    resource.Id,
                    edgeDistance,
                    resource.Calories);
                bestEfficiency = efficiency;
                bestEdgeDistance = edgeDistance;
                bestDistanceSquared = distanceSquared;
            }
        }

        foreach (var eggIndex in _eggCandidates)
        {
            var egg = state.Eggs[eggIndex];
            if (egg.Energy <= 0f || egg.Health <= 0f)
            {
                continue;
            }

            var centerDistance = SimVector2.Distance(creature.Position, egg.Position);
            var edgeDistance = Math.Max(0f, centerDistance - EggPredation.ContactRadius(egg));
            if (edgeDistance > contactRadius)
            {
                continue;
            }

            var efficiency = CreatureDigestion.FreshMeatEnergyEfficiency(genome);
            var distanceSquared = centerDistance * centerDistance;
            if (IsBetterFoodContact(efficiency, edgeDistance, distanceSquared, bestEfficiency, bestEdgeDistance, bestDistanceSquared))
            {
                best = new FoodContact(
                    FoodContactKind.Egg,
                    default,
                    default,
                    eggIndex,
                    egg.Id,
                    edgeDistance,
                    egg.Energy);
                bestEfficiency = efficiency;
                bestEdgeDistance = edgeDistance;
                bestDistanceSquared = distanceSquared;
            }
        }

        foreach (var preyIndex in _smallPreyCandidates)
        {
            var prey = state.SmallPrey[preyIndex];
            if (prey.Calories <= 0f || prey.Health <= 0f)
            {
                continue;
            }

            var centerDistance = SimVector2.Distance(creature.Position, prey.Position);
            var edgeDistance = Math.Max(0f, centerDistance - prey.Radius);
            if (edgeDistance > contactRadius)
            {
                continue;
            }

            var efficiency = CreatureDigestion.FreshMeatEnergyEfficiency(genome);
            var distanceSquared = centerDistance * centerDistance;
            if (IsBetterFoodContact(efficiency, edgeDistance, distanceSquared, bestEfficiency, bestEdgeDistance, bestDistanceSquared))
            {
                best = new FoodContact(
                    FoodContactKind.SmallPrey,
                    ResourceKind.Meat,
                    default,
                    preyIndex,
                    prey.Id,
                    edgeDistance,
                    prey.Calories);
                bestEfficiency = efficiency;
                bestEdgeDistance = edgeDistance;
                bestDistanceSquared = distanceSquared;
            }
        }

        return best;
    }

    private static bool IsBetterFoodContact(
        float efficiency,
        float edgeDistance,
        float distanceSquared,
        float bestEfficiency,
        float bestEdgeDistance,
        float bestDistanceSquared)
    {
        return efficiency > bestEfficiency
            || (Math.Abs(efficiency - bestEfficiency) <= 0.0001f
                && (edgeDistance < bestEdgeDistance
                    || (Math.Abs(edgeDistance - bestEdgeDistance) <= 0.0001f
                        && distanceSquared < bestDistanceSquared)));
    }

    private static void EatResource(
        WorldState state,
        int resourceIndex,
        ref CreatureState creature,
        CreatureGenome genome,
        float deltaSeconds)
    {
        var resource = state.Resources[resourceIndex];
        var eatRateMultiplier = resource.Kind == ResourceKind.Plant
            ? PlantResourceTraits.EatingRateMultiplier(resource.PlantKind)
            : 1f;
        var amount = Math.Min(
            resource.Calories,
            Math.Min(
                CreatureGrowth.EffectiveEatCaloriesPerSecond(creature, genome) * eatRateMultiplier * deltaSeconds,
                AvailableGutCapacity(creature, genome)));

        if (amount <= 0f)
        {
            return;
        }

        resource.Calories -= amount;
        var meatFreshness = resource.Kind == ResourceKind.Meat
            ? MeatQuality.Freshness(resource)
            : 1f;
        state.Stats.RecordFoodEaten(state.Bounds, resource.Position, resource.Kind, amount);
        AddToGut(ref creature, resource.Kind, resource.PlantKind, amount, meatFreshness);
        creature.LastCaloriesEaten = amount;
        if (resource.Kind == ResourceKind.Meat)
        {
            if (IsCreditedFreshKill(resource, creature))
            {
                creature.LastLivePreyCaloriesEaten = amount;
            }
            else
            {
                creature.LastCarcassCaloriesEaten = amount;
            }

            if (MeatQuality.IsFresh(meatFreshness))
            {
                creature.LastFreshMeatCaloriesEaten = amount;
            }
            else
            {
                creature.LastStaleMeatCaloriesEaten = amount;
            }
        }
        else
        {
            creature.LastPlantCaloriesEaten = amount;
            switch (resource.PlantKind)
            {
                case PlantResourceKind.Tender:
                    creature.LastTenderPlantCaloriesEaten = amount;
                    break;
                case PlantResourceKind.Rich:
                    creature.LastRichPlantCaloriesEaten = amount;
                    break;
                case PlantResourceKind.Tough:
                    creature.LastToughPlantCaloriesEaten = amount;
                    break;
            }
        }

        creature.SecondsSinceLastMeal = 0f;
        creature.DistanceSinceLastMeal = 0f;

        state.Resources[resourceIndex] = resource;
    }

    private static bool IsCreditedFreshKill(ResourcePatchState resource, CreatureState creature)
    {
        return resource.FreshKillSecondsRemaining > 0f
            && resource.FreshKillAttackerId == creature.Id;
    }

    private static void EatEgg(
        WorldState state,
        int eggIndex,
        ref CreatureState creature,
        CreatureGenome genome,
        float deltaSeconds)
    {
        var egg = state.Eggs[eggIndex];
        var amount = Math.Min(
            egg.Energy,
            Math.Min(
                CreatureGrowth.EffectiveEatCaloriesPerSecond(creature, genome) * deltaSeconds,
                AvailableGutCapacity(creature, genome)));

        if (amount <= 0f)
        {
            return;
        }

        egg.Energy -= amount;
        state.Stats.RecordEggEaten(state.Bounds, egg.Position, amount);
        if (egg.Energy <= 0f)
        {
            egg.Energy = 0f;
            egg.PendingDeathReason = EggDeathReason.Predation;
        }

        creature.GutMeatCalories += amount;
        creature.GutMeatQualityCalories += amount;
        creature.LastCaloriesEaten = amount;
        creature.LastEggCaloriesEaten = amount;
        creature.SecondsSinceLastMeal = 0f;
        creature.DistanceSinceLastMeal = 0f;

        state.Eggs[eggIndex] = egg;
    }

    private static void EatSmallPrey(
        WorldState state,
        int preyIndex,
        ref CreatureState creature,
        CreatureGenome genome,
        float deltaSeconds)
    {
        var prey = state.SmallPrey[preyIndex];
        var amount = Math.Min(
            prey.Calories,
            Math.Min(
                CreatureGrowth.EffectiveEatCaloriesPerSecond(creature, genome) * deltaSeconds,
                AvailableGutCapacity(creature, genome)));

        if (amount <= 0f)
        {
            return;
        }

        prey.Calories -= amount;
        if (prey.Calories <= 0f)
        {
            prey.Calories = 0f;
            prey.Health = 0f;
            state.Stats.RecordSmallPreyEaten();
        }

        state.Stats.RecordFoodEaten(state.Bounds, prey.Position, ResourceKind.Meat, amount);
        creature.GutMeatCalories += amount;
        creature.GutMeatQualityCalories += amount;
        creature.LastCaloriesEaten = amount;
        creature.LastLivePreyCaloriesEaten = amount;
        creature.LastSmallPreyCaloriesEaten = amount;
        creature.LastFreshMeatCaloriesEaten = amount;
        creature.SecondsSinceLastMeal = 0f;
        creature.DistanceSinceLastMeal = 0f;

        state.SmallPrey[preyIndex] = prey;
    }

    private static float AvailableGutCapacity(CreatureState creature, CreatureGenome genome)
    {
        var capacity = CreatureGrowth.EffectiveGutCapacityCalories(creature, genome);
        return Math.Max(0f, capacity - creature.GutPlantCalories - creature.GutMeatCalories);
    }

    private static void AddToGut(
        ref CreatureState creature,
        ResourceKind kind,
        PlantResourceKind plantKind,
        float amount,
        float meatFreshness)
    {
        if (kind == ResourceKind.Meat)
        {
            creature.GutMeatCalories += amount;
            creature.GutMeatQualityCalories += amount * Math.Clamp(meatFreshness, MeatQuality.MinimumFreshness, 1f);
        }
        else
        {
            creature.GutPlantCalories += amount;
            switch (plantKind)
            {
                case PlantResourceKind.Tender:
                    creature.GutTenderPlantCalories += amount;
                    break;
                case PlantResourceKind.Rich:
                    creature.GutRichPlantCalories += amount;
                    break;
                case PlantResourceKind.Tough:
                    creature.GutToughPlantCalories += amount;
                    break;
            }
        }
    }

    private readonly record struct FoodContact(
        FoodContactKind Kind,
        ResourceKind ResourceKind,
        PlantResourceKind PlantKind,
        int Index,
        EntityId Id,
        float EdgeDistance,
        float Calories)
    {
        public static FoodContact None { get; } = new(FoodContactKind.None, default, default, -1, default, 0f, 0f);
    }
}
