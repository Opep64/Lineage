namespace Lineage.Core;

/// <summary>
/// Evaluates each creature's neural brain and writes movement/action intent.
/// </summary>
public sealed class NeuralControllerSystem(
    float eatThreshold = 0f,
    float reproduceThreshold = 0.25f,
    float attackThreshold = 0.25f) : ISimulationSystem
{
    public void Update(WorldState state, float deltaSeconds)
    {
        Span<float> inputs = stackalloc float[NeuralBrainSchema.InputCount];
        Span<float> outputs = stackalloc float[NeuralBrainSchema.OutputCount];

        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);

            if (creature.BrainId < 0)
            {
                creature.Actions = default;
                creature.DesiredVelocity = SimVector2.Zero;
                state.Creatures[i] = creature;
                continue;
            }

            var brain = state.GetBrain(creature.BrainId);
            FillInputs(creature.Senses, genome, inputs);
            outputs.Clear();
            brain.Evaluate(inputs, outputs);

            var moveForward = Math.Clamp(outputs[NeuralBrainSchema.MoveForwardOutput], 0f, 1f);
            var turn = Math.Clamp(outputs[NeuralBrainSchema.TurnOutput], -1f, 1f);
            var effectiveMaxSpeed = CreatureGrowth.EffectiveMaxSpeed(creature, genome);
            var effectiveTurnRate = CreatureGrowth.EffectiveMaxTurnRadiansPerSecond(creature, genome);

            creature.HeadingRadians += turn * effectiveTurnRate * deltaSeconds;
            creature.DesiredVelocity = SimVector2.FromAngle(creature.HeadingRadians)
                * effectiveMaxSpeed
                * moveForward;
            creature.Actions = new CreatureActionState
            {
                MoveForward = moveForward,
                Turn = turn,
                WantsEat = outputs[NeuralBrainSchema.EatOutput] > eatThreshold,
                WantsReproduce = outputs[NeuralBrainSchema.ReproduceOutput] > reproduceThreshold,
                WantsAttack = outputs[NeuralBrainSchema.AttackOutput] > attackThreshold
            };

            state.Creatures[i] = creature;
        }
    }

    private static void FillInputs(CreatureSenseState senses, CreatureGenome genome, Span<float> inputs)
    {
        inputs[NeuralBrainSchema.BiasInput] = 1f;
        inputs[NeuralBrainSchema.EnergyRatioInput] = senses.EnergyRatio;
        inputs[NeuralBrainSchema.HungerInput] = senses.Hunger;
        inputs[NeuralBrainSchema.FoodProximityInput] = senses.FoodProximity;
        inputs[NeuralBrainSchema.FoodForwardInput] = senses.FoodDirectionForward;
        inputs[NeuralBrainSchema.FoodRightInput] = senses.FoodDirectionRight;
        inputs[NeuralBrainSchema.VisibleFoodDensityInput] = senses.VisibleFoodDensity;
        inputs[NeuralBrainSchema.VisiblePlantDensityInput] = senses.VisiblePlantDensity;
        inputs[NeuralBrainSchema.PlantProximityInput] = senses.PlantProximity;
        inputs[NeuralBrainSchema.PlantForwardInput] = senses.PlantDirectionForward;
        inputs[NeuralBrainSchema.PlantRightInput] = senses.PlantDirectionRight;
        inputs[NeuralBrainSchema.VisibleMeatDensityInput] = senses.VisibleMeatDensity;
        inputs[NeuralBrainSchema.MeatProximityInput] = senses.MeatProximity;
        inputs[NeuralBrainSchema.MeatForwardInput] = senses.MeatDirectionForward;
        inputs[NeuralBrainSchema.MeatRightInput] = senses.MeatDirectionRight;
        inputs[NeuralBrainSchema.DietaryMeatBiasInput] = genome.DietaryAdaptation;
        inputs[NeuralBrainSchema.EggReserveRatioInput] = senses.EggReserveRatio;
        inputs[NeuralBrainSchema.ReproductionReadinessInput] = senses.ReproductionReadiness;
        inputs[NeuralBrainSchema.VisibleCreatureDensityInput] = senses.VisibleCreatureDensity;
        inputs[NeuralBrainSchema.CreatureProximityInput] = senses.CreatureProximity;
        inputs[NeuralBrainSchema.CreatureForwardInput] = senses.CreatureDirectionForward;
        inputs[NeuralBrainSchema.CreatureRightInput] = senses.CreatureDirectionRight;
        inputs[NeuralBrainSchema.MeatScentDensityInput] = senses.MeatScentDensity;
        inputs[NeuralBrainSchema.MeatScentForwardInput] = senses.MeatScentDirectionForward;
        inputs[NeuralBrainSchema.MeatScentRightInput] = senses.MeatScentDirectionRight;
        inputs[NeuralBrainSchema.CreatureRelativeBodySizeInput] = senses.CreatureRelativeBodySize;
        inputs[NeuralBrainSchema.CreatureRelativeSpeedInput] = senses.CreatureRelativeSpeed;
        inputs[NeuralBrainSchema.CreatureApproachRateInput] = senses.CreatureApproachRate;
        inputs[NeuralBrainSchema.CreatureFacingAlignmentInput] = senses.CreatureFacingAlignment;
        inputs[NeuralBrainSchema.CurrentTerrainDragInput] = senses.CurrentTerrainDrag;
        inputs[NeuralBrainSchema.ForwardTerrainDragInput] = senses.ForwardTerrainDrag;
        inputs[NeuralBrainSchema.LeftTerrainDragInput] = senses.LeftTerrainDrag;
        inputs[NeuralBrainSchema.RightTerrainDragInput] = senses.RightTerrainDrag;
        inputs[NeuralBrainSchema.EnergySurplusInput] = senses.EnergySurplusRatio;
        inputs[NeuralBrainSchema.RecentFoodSuccessInput] = senses.RecentFoodSuccess;
    }
}
