namespace Lineage.Core;

/// <summary>
/// Temporary hand-written controller used to prove the ecology loop before neural brains.
/// </summary>
///
/// <remarks>
/// Creatures move toward the nearest sensed resource with calories. If nothing is in
/// range, they wander using deterministic heading jitter from the world's RNG.
/// </remarks>
public sealed class SimpleForagingSystem(
    UniformSpatialIndex spatialIndex,
    float wanderSpeedFraction = 0.25f,
    float wanderTurnRadiansPerSecond = 1.25f) : ISimulationSystem
{
    private readonly List<int> _resourceCandidates = [];
    private readonly IndexStampSet _seenResourceCandidates = new();
    private readonly List<int> _eggCandidates = [];
    private readonly IndexStampSet _seenEggCandidates = new();

    public void Update(WorldState state, float deltaSeconds)
    {
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            var effectiveMaxSpeed = CreatureGrowth.EffectiveMaxSpeed(creature, genome);
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
            var target = FindBestVisibleFoodTarget(state, creature, genome, effectiveSenseRadius, effectiveVisionAngle);

            if (target.Kind != FoodContactKind.None)
            {
                var toTarget = target.Position - creature.Position;

                if (toTarget.LengthSquared > 0.0001f)
                {
                    var direction = toTarget.Normalized();
                    creature.HeadingRadians = MathF.Atan2(direction.Y, direction.X);
                    creature.DesiredVelocity = direction * effectiveMaxSpeed;
                }
                else
                {
                    creature.DesiredVelocity = SimVector2.Zero;
                }
            }
            else
            {
                var turn = state.Random.NextSingle(
                    -wanderTurnRadiansPerSecond,
                    wanderTurnRadiansPerSecond) * deltaSeconds;
                creature.HeadingRadians += turn;
                creature.DesiredVelocity = SimVector2.FromAngle(creature.HeadingRadians)
                    * effectiveMaxSpeed
                    * wanderSpeedFraction;
            }

            creature.Actions = new CreatureActionState
            {
                MoveForward = effectiveMaxSpeed > 0f ? creature.DesiredVelocity.Length / effectiveMaxSpeed : 0f,
                WantsEat = true,
                WantsReproduce = true
            };
            state.Creatures[i] = creature;
        }
    }

    private FoodTarget FindBestVisibleFoodTarget(
        WorldState state,
        CreatureState creature,
        CreatureGenome genome,
        float effectiveSenseRadius,
        float visionAngleRadians)
    {
        var forward = SimVector2.FromAngle(creature.HeadingRadians);
        var bestTarget = FoodTarget.None;
        var bestScore = float.NegativeInfinity;
        var bestDistanceSquared = float.PositiveInfinity;

        foreach (var resourceIndex in _resourceCandidates)
        {
            var resource = state.Resources[resourceIndex];
            var toResource = resource.Position - creature.Position;
            var centerDistance = toResource.Length;
            var direction = centerDistance > 0.0001f
                ? toResource / centerDistance
                : forward;

            if (!IsInsideVisionCone(direction, forward, visionAngleRadians))
            {
                continue;
            }

            var edgeDistance = Math.Max(0f, centerDistance - resource.Radius);
            var proximity = 1f - Math.Clamp(edgeDistance / effectiveSenseRadius, 0f, 1f);
            var efficiency = resource.Kind == ResourceKind.Meat
                ? CreatureDigestion.MeatEnergyEfficiency(genome, MeatQuality.Freshness(resource))
                : CreatureDigestion.PlantEfficiency(genome);
            var score = proximity * efficiency;
            var distanceSquared = centerDistance * centerDistance;
            if (score > bestScore
                || (Math.Abs(score - bestScore) <= 0.0001f && distanceSquared < bestDistanceSquared))
            {
                bestScore = score;
                bestDistanceSquared = distanceSquared;
                bestTarget = new FoodTarget(FoodContactKind.Resource, resourceIndex, resource.Position);
            }
        }

        foreach (var eggIndex in _eggCandidates)
        {
            var egg = state.Eggs[eggIndex];
            var toEgg = egg.Position - creature.Position;
            var centerDistance = toEgg.Length;
            var direction = centerDistance > 0.0001f
                ? toEgg / centerDistance
                : forward;

            if (!IsInsideVisionCone(direction, forward, visionAngleRadians))
            {
                continue;
            }

            var edgeDistance = Math.Max(0f, centerDistance - EggPredation.ContactRadius(egg));
            var proximity = 1f - Math.Clamp(edgeDistance / effectiveSenseRadius, 0f, 1f);
            var score = proximity * CreatureDigestion.FreshMeatEnergyEfficiency(genome);
            var distanceSquared = centerDistance * centerDistance;
            if (score > bestScore
                || (Math.Abs(score - bestScore) <= 0.0001f && distanceSquared < bestDistanceSquared))
            {
                bestScore = score;
                bestDistanceSquared = distanceSquared;
                bestTarget = new FoodTarget(FoodContactKind.Egg, eggIndex, egg.Position);
            }
        }

        return bestTarget;
    }

    private static bool IsInsideVisionCone(SimVector2 direction, SimVector2 forward, float visionAngleRadians)
    {
        return visionAngleRadians >= MathF.Tau
            || SimVector2.Dot(direction, forward) >= MathF.Cos(visionAngleRadians * 0.5f);
    }

    private readonly record struct FoodTarget(FoodContactKind Kind, int Index, SimVector2 Position)
    {
        public static FoodTarget None { get; } = new(FoodContactKind.None, -1, SimVector2.Zero);
    }
}
