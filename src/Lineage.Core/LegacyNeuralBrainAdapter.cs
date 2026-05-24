namespace Lineage.Core;

/// <summary>
/// Converts the architecture-neutral brain frame into the original flat neural schema.
/// </summary>
///
/// <remarks>
/// This adapter lets us evolve the simulation-facing input model without immediately
/// rewriting saved neural brains, behavior assays, or starter controllers.
/// </remarks>
public static class LegacyNeuralBrainAdapter
{
    public static void FillInputs(
        in BrainInputFrame frame,
        in LegacyNeuralMemoryInputFrame memory,
        Span<float> inputs,
        bool enableLegacyNearestFoodVisionInputs = true)
    {
        if (inputs.Length < NeuralBrainSchema.InputCount)
        {
            throw new ArgumentException("Input span is smaller than the neural brain schema requires.", nameof(inputs));
        }

        inputs.Clear();

        inputs[NeuralBrainSchema.BiasInput] = 1f;
        inputs[NeuralBrainSchema.EnergyRatioInput] = frame.Internal.EnergyRatio;
        inputs[NeuralBrainSchema.HealthRatioInput] = frame.Internal.HealthRatio;
        inputs[NeuralBrainSchema.HungerInput] = frame.Internal.Hunger;
        inputs[NeuralBrainSchema.VisibleFoodDensityInput] = frame.Vision.Food.Density;
        inputs[NeuralBrainSchema.VisiblePlantDensityInput] = frame.Vision.Plant.Density;
        inputs[NeuralBrainSchema.VisibleMeatDensityInput] = frame.Vision.Meat.Density;
        if (enableLegacyNearestFoodVisionInputs)
        {
            inputs[NeuralBrainSchema.FoodProximityInput] = frame.Vision.Food.Proximity;
            inputs[NeuralBrainSchema.FoodForwardInput] = frame.Vision.Food.DirectionForward;
            inputs[NeuralBrainSchema.FoodRightInput] = frame.Vision.Food.DirectionRight;
            inputs[NeuralBrainSchema.PlantProximityInput] = frame.Vision.Plant.Proximity;
            inputs[NeuralBrainSchema.PlantForwardInput] = frame.Vision.Plant.DirectionForward;
            inputs[NeuralBrainSchema.PlantRightInput] = frame.Vision.Plant.DirectionRight;
            inputs[NeuralBrainSchema.MeatProximityInput] = frame.Vision.Meat.Proximity;
            inputs[NeuralBrainSchema.MeatForwardInput] = frame.Vision.Meat.DirectionForward;
            inputs[NeuralBrainSchema.MeatRightInput] = frame.Vision.Meat.DirectionRight;
        }
        inputs[NeuralBrainSchema.DietaryMeatBiasInput] = frame.Internal.DietaryMeatBias;
        inputs[NeuralBrainSchema.EggReserveRatioInput] = frame.Internal.EggReserveRatio;
        inputs[NeuralBrainSchema.ReproductionReadinessInput] = frame.Internal.ReproductionReadiness;
        inputs[NeuralBrainSchema.VisibleCreatureDensityInput] = frame.Vision.Creature.Density;
        inputs[NeuralBrainSchema.CreatureProximityInput] = frame.Vision.Creature.Proximity;
        inputs[NeuralBrainSchema.CreatureForwardInput] = frame.Vision.Creature.DirectionForward;
        inputs[NeuralBrainSchema.CreatureRightInput] = frame.Vision.Creature.DirectionRight;
        inputs[NeuralBrainSchema.MeatScentDensityInput] = frame.Scent.Meat.Density;
        inputs[NeuralBrainSchema.MeatScentForwardInput] = frame.Scent.Meat.DirectionForward;
        inputs[NeuralBrainSchema.MeatScentRightInput] = frame.Scent.Meat.DirectionRight;
        inputs[NeuralBrainSchema.CreatureRelativeBodySizeInput] = frame.Vision.CreatureRelativeBodySize;
        inputs[NeuralBrainSchema.CreatureRelativeSpeedInput] = frame.Vision.CreatureRelativeSpeed;
        inputs[NeuralBrainSchema.CreatureApproachRateInput] = frame.Vision.CreatureApproachRate;
        inputs[NeuralBrainSchema.CreatureFacingAlignmentInput] = frame.Vision.CreatureFacingAlignment;
        inputs[NeuralBrainSchema.CurrentTerrainDragInput] = frame.Body.CurrentTerrainDrag;
        inputs[NeuralBrainSchema.ForwardTerrainDragInput] = frame.Body.ForwardTerrainDrag;
        inputs[NeuralBrainSchema.LeftTerrainDragInput] = frame.Body.LeftTerrainDrag;
        inputs[NeuralBrainSchema.RightTerrainDragInput] = frame.Body.RightTerrainDrag;
        inputs[NeuralBrainSchema.EnergySurplusInput] = frame.Internal.EnergySurplusRatio;
        inputs[NeuralBrainSchema.RecentFoodSuccessInput] = frame.Internal.RecentFoodSuccess;
        inputs[NeuralBrainSchema.MemoryForwardInput] = memory.DirectionForward;
        inputs[NeuralBrainSchema.MemoryRightInput] = memory.DirectionRight;
        inputs[NeuralBrainSchema.MemoryStrengthInput] = memory.Strength;
        inputs[NeuralBrainSchema.VisibleMeatFreshnessInput] = frame.Vision.MeatFreshness;
        inputs[NeuralBrainSchema.RottenMeatScentDensityInput] = frame.Scent.RottenMeat.Density;
        inputs[NeuralBrainSchema.RottenMeatScentForwardInput] = frame.Scent.RottenMeat.DirectionForward;
        inputs[NeuralBrainSchema.RottenMeatScentRightInput] = frame.Scent.RottenMeat.DirectionRight;
        inputs[NeuralBrainSchema.ForwardObstacleInput] = frame.Body.ForwardObstacle;
        inputs[NeuralBrainSchema.LeftObstacleInput] = frame.Body.LeftObstacle;
        inputs[NeuralBrainSchema.RightObstacleInput] = frame.Body.RightObstacle;
        inputs[NeuralBrainSchema.MovementBlockedInput] = frame.Body.MovementBlocked;
        inputs[NeuralBrainSchema.FoodContactInput] = frame.Body.FoodContact;
        inputs[NeuralBrainSchema.PlantFoodContactInput] = frame.Body.PlantFoodContact;
        inputs[NeuralBrainSchema.MeatFoodContactInput] = frame.Body.MeatFoodContact;
        inputs[NeuralBrainSchema.EggFoodContactInput] = frame.Body.EggFoodContact;

        if (!frame.Vision.Sectors.HasAnySignal)
        {
            return;
        }

        for (var sectorIndex = 0; sectorIndex < VisionSectorSet.SectorCount; sectorIndex++)
        {
            var sector = frame.Vision.Sectors.Get(sectorIndex);
            inputs[NeuralBrainSchema.VisionSectorPlantDensityInput(sectorIndex)] = sector.PlantDensity;
            inputs[NeuralBrainSchema.VisionSectorPlantProximityInput(sectorIndex)] = sector.PlantProximity;
            inputs[NeuralBrainSchema.VisionSectorMeatDensityInput(sectorIndex)] = sector.MeatDensity;
            inputs[NeuralBrainSchema.VisionSectorMeatProximityInput(sectorIndex)] = sector.MeatProximity;
            inputs[NeuralBrainSchema.VisionSectorEggDensityInput(sectorIndex)] = sector.EggDensity;
            inputs[NeuralBrainSchema.VisionSectorEggProximityInput(sectorIndex)] = sector.EggProximity;
            inputs[NeuralBrainSchema.VisionSectorCreatureDensityInput(sectorIndex)] = sector.CreatureDensity;
            inputs[NeuralBrainSchema.VisionSectorCreatureProximityInput(sectorIndex)] = sector.CreatureProximity;
        }
    }

    public static BrainOutputFrame ReadStandardOutputs(ReadOnlySpan<float> outputs)
    {
        if (outputs.Length < NeuralBrainSchema.OutputCount)
        {
            throw new ArgumentException("Output span is smaller than the neural brain schema requires.", nameof(outputs));
        }

        return new BrainOutputFrame(
            Math.Clamp(outputs[NeuralBrainSchema.MoveForwardOutput], 0f, 1f),
            Math.Clamp(outputs[NeuralBrainSchema.TurnOutput], -1f, 1f),
            outputs[NeuralBrainSchema.EatOutput],
            outputs[NeuralBrainSchema.ReproduceOutput],
            outputs[NeuralBrainSchema.AttackOutput]);
    }

    public static LegacyNeuralMemoryOutputFrame ReadMemoryOutputs(ReadOnlySpan<float> outputs)
    {
        if (outputs.Length < NeuralBrainSchema.OutputCount)
        {
            throw new ArgumentException("Output span is smaller than the neural brain schema requires.", nameof(outputs));
        }

        return new LegacyNeuralMemoryOutputFrame(
            Math.Clamp(outputs[NeuralBrainSchema.MemoryForwardOutput], -1f, 1f),
            Math.Clamp(outputs[NeuralBrainSchema.MemoryRightOutput], -1f, 1f));
    }
}
