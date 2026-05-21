namespace Lineage.Core;

/// <summary>
/// Resolves desired creature movement and charges movement energy.
/// </summary>
public sealed class MovementSystem : ISimulationSystem
{
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

            creature.Position = nextPosition;
            creature.Velocity = (nextPosition - previousPosition) / deltaSeconds;

            var effort = effectiveMaxSpeed > 0f
                ? desiredVelocity.Length / effectiveMaxSpeed
                : 0f;
            creature.Energy -= genome.MovementEnergyPerSecond * CreatureGrowth.GrowthFactor(creature, genome) * effort * deltaSeconds;

            state.Creatures[i] = creature;
        }
    }
}
