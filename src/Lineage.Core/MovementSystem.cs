namespace Lineage.Core;

/// <summary>
/// Resolves desired creature movement and charges movement energy.
/// </summary>
public sealed class MovementSystem(
    BiomePressureProfile? biomeMovementCostProfile = null,
    BiomePressureProfile? biomeSpeedProfile = null,
    float movementSpeedCostExponent = 1f) : ISimulationSystem
{
    private readonly BiomePressureProfile _biomeMovementCostProfile =
        BiomePressureProfile.Validate(biomeMovementCostProfile ?? BiomePressureProfile.Neutral, nameof(biomeMovementCostProfile));
    private readonly BiomePressureProfile _biomeSpeedProfile =
        BiomePressureProfile.Validate(biomeSpeedProfile ?? BiomePressureProfile.Neutral, nameof(biomeSpeedProfile));
    private readonly float _movementSpeedCostExponent = ValidateExponent(
        movementSpeedCostExponent,
        nameof(movementSpeedCostExponent));

    public void Update(WorldState state, float deltaSeconds)
    {
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            var effectiveMaxSpeed = CreatureGrowth.EffectiveMaxSpeed(creature, genome);
            var previousPosition = creature.Position;
            var biome = state.Biomes.GetKindAt(previousPosition);
            var desiredVelocity = creature.DesiredVelocity.ClampedLength(effectiveMaxSpeed);
            var biomeSpeedMultiplier = _biomeSpeedProfile.For(biome);
            var terrainAdjustedVelocity = desiredVelocity * biomeSpeedMultiplier;
            var nextPosition = state.Bounds.Clamp(previousPosition + terrainAdjustedVelocity * deltaSeconds);
            var distanceTraveled = SimVector2.Distance(previousPosition, nextPosition);

            creature.Position = nextPosition;
            creature.MaxXReached = Math.Max(creature.MaxXReached, nextPosition.X);
            creature.Velocity = (nextPosition - previousPosition) / deltaSeconds;
            creature.LastDistanceTraveled = distanceTraveled;
            creature.DistanceSinceLastMeal += distanceTraveled;

            var speedCostMultiplier = CalculateSpeedCostMultiplier(creature.Velocity.Length, _movementSpeedCostExponent);
            var biomeMovementCostMultiplier = _biomeMovementCostProfile.For(state.Biomes.GetKindAt(creature.Position));
            creature.Energy -= genome.MovementEnergyPerSecond
                * biomeMovementCostMultiplier
                * CreatureGrowth.GrowthFactor(creature, genome)
                * speedCostMultiplier
                * deltaSeconds;

            state.Creatures[i] = creature;
            state.RecordCreatureProgress(creature.Id, creature.MaxXReached);
        }
    }

    public static float CalculateSpeedCostMultiplier(float speed, float movementSpeedCostExponent)
    {
        if (speed <= 0f)
        {
            return 0f;
        }

        var referenceSpeed = MathF.Max(0.000001f, CreatureGenome.Baseline.MaxSpeed);
        var speedRatio = speed / referenceSpeed;
        return MathF.Pow(speedRatio, movementSpeedCostExponent);
    }

    private static float ValidateExponent(float value, string name)
    {
        return float.IsFinite(value) && value > 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Movement speed cost exponent must be finite and positive.");
    }
}
