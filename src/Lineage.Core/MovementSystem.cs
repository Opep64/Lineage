namespace Lineage.Core;

/// <summary>
/// Resolves desired creature movement and charges movement energy.
/// </summary>
public sealed class MovementSystem(BiomePressureProfile? biomeMovementCostProfile = null) : ISimulationSystem
{
    private readonly BiomePressureProfile _biomeMovementCostProfile =
        BiomePressureProfile.Validate(biomeMovementCostProfile ?? BiomePressureProfile.Neutral, nameof(biomeMovementCostProfile));

    public void Update(WorldState state, float deltaSeconds)
    {
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            var effectiveMaxSpeed = CreatureGrowth.EffectiveMaxSpeed(creature, genome);
            var desiredVelocity = creature.DesiredVelocity.ClampedLength(effectiveMaxSpeed);
            var previousPosition = creature.Position;
            var nextPosition = state.Bounds.Clamp(previousPosition + desiredVelocity * deltaSeconds);
            var distanceTraveled = SimVector2.Distance(previousPosition, nextPosition);

            creature.Position = nextPosition;
            creature.Velocity = (nextPosition - previousPosition) / deltaSeconds;
            creature.LastDistanceTraveled = distanceTraveled;
            creature.DistanceSinceLastMeal += distanceTraveled;

            var effort = effectiveMaxSpeed > 0f
                ? desiredVelocity.Length / effectiveMaxSpeed
                : 0f;
            var biomeMovementCostMultiplier = _biomeMovementCostProfile.For(state.Biomes.GetKindAt(creature.Position));
            creature.Energy -= genome.MovementEnergyPerSecond
                * biomeMovementCostMultiplier
                * CreatureGrowth.GrowthFactor(creature, genome)
                * effort
                * deltaSeconds;

            state.Creatures[i] = creature;
        }
    }
}
