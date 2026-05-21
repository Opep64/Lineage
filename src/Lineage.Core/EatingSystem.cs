namespace Lineage.Core;

/// <summary>
/// Transfers calories from reachable resource patches into creature energy.
/// </summary>
public sealed class EatingSystem(
    UniformSpatialIndex spatialIndex,
    float reachPadding = 0f,
    bool requireEatIntent = false) : ISimulationSystem
{
    private readonly List<int> _resourceCandidates = [];
    private readonly HashSet<int> _seenResourceCandidates = [];
    private readonly List<int> _eggCandidates = [];
    private readonly HashSet<int> _seenEggCandidates = [];

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

            creature.IsTouchingFood = false;
            creature.FoodContactKind = FoodContactKind.None;
            creature.FoodContactResourceId = default;
            creature.FoodContactEdgeDistance = 0f;
            creature.FoodContactCalories = 0f;
            creature.LastCaloriesEaten = 0f;

            var target = FindBestFoodContact(state, creature, genome, contactRadius);

            if (target.Kind == FoodContactKind.None)
            {
                state.Creatures[i] = creature;
                continue;
            }

            creature.IsTouchingFood = true;
            creature.FoodContactKind = target.Kind;
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

            var efficiency = CreatureDigestion.EfficiencyFor(genome, resource.Kind);
            var distanceSquared = centerDistance * centerDistance;
            if (IsBetterFoodContact(efficiency, edgeDistance, distanceSquared, bestEfficiency, bestEdgeDistance, bestDistanceSquared))
            {
                best = new FoodContact(
                    FoodContactKind.Resource,
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

            var efficiency = CreatureDigestion.MeatEfficiency(genome);
            var distanceSquared = centerDistance * centerDistance;
            if (IsBetterFoodContact(efficiency, edgeDistance, distanceSquared, bestEfficiency, bestEdgeDistance, bestDistanceSquared))
            {
                best = new FoodContact(
                    FoodContactKind.Egg,
                    eggIndex,
                    egg.Id,
                    edgeDistance,
                    egg.Energy);
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
        var amount = Math.Min(resource.Calories, CreatureGrowth.EffectiveEatCaloriesPerSecond(creature, genome) * deltaSeconds);

        if (amount <= 0f)
        {
            return;
        }

        var digestedCalories = amount * CreatureDigestion.EfficiencyFor(genome, resource.Kind);
        resource.Calories -= amount;
        creature.Energy += digestedCalories;
        creature.LastCaloriesEaten = digestedCalories;
        creature.SecondsSinceLastMeal = 0f;

        state.Resources[resourceIndex] = resource;
    }

    private static void EatEgg(
        WorldState state,
        int eggIndex,
        ref CreatureState creature,
        CreatureGenome genome,
        float deltaSeconds)
    {
        var egg = state.Eggs[eggIndex];
        var amount = Math.Min(egg.Energy, CreatureGrowth.EffectiveEatCaloriesPerSecond(creature, genome) * deltaSeconds);

        if (amount <= 0f)
        {
            return;
        }

        var digestedCalories = amount * CreatureDigestion.MeatEfficiency(genome);
        egg.Energy -= amount;
        if (egg.Energy <= 0f)
        {
            egg.Energy = 0f;
            egg.PendingDeathReason = EggDeathReason.Predation;
        }

        creature.Energy += digestedCalories;
        creature.LastCaloriesEaten = digestedCalories;
        creature.SecondsSinceLastMeal = 0f;

        state.Eggs[eggIndex] = egg;
    }

    private readonly record struct FoodContact(
        FoodContactKind Kind,
        int Index,
        EntityId Id,
        float EdgeDistance,
        float Calories)
    {
        public static FoodContact None { get; } = new(FoodContactKind.None, -1, default, 0f, 0f);
    }
}
