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
        Span<float> inputs)
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
        inputs[NeuralBrainSchema.VisiblePlantEnergyQualityInput] = frame.Vision.PlantEnergyQuality;
        inputs[NeuralBrainSchema.VisiblePlantBiteEaseInput] = frame.Vision.PlantBiteEase;
        inputs[NeuralBrainSchema.VisibleMeatDensityInput] = frame.Vision.Meat.Density;
        inputs[NeuralBrainSchema.DietaryMeatBiasInput] = frame.Internal.DietaryMeatBias;
        inputs[NeuralBrainSchema.EggReserveRatioInput] = frame.Internal.EggReserveRatio;
        inputs[NeuralBrainSchema.ReproductionReadinessInput] = frame.Internal.ReproductionReadiness;
        inputs[NeuralBrainSchema.VisibleCreatureDensityInput] = frame.Vision.Creature.Density;
        inputs[NeuralBrainSchema.MeatScentDensityInput] = frame.Scent.Meat.Density;
        inputs[NeuralBrainSchema.MeatScentForwardInput] = frame.Scent.Meat.DirectionForward;
        inputs[NeuralBrainSchema.MeatScentRightInput] = frame.Scent.Meat.DirectionRight;
        inputs[NeuralBrainSchema.CurrentTerrainDragInput] = frame.Body.CurrentTerrainDrag;
        inputs[NeuralBrainSchema.ForwardTerrainDragInput] = frame.Body.ForwardTerrainDrag;
        inputs[NeuralBrainSchema.LeftTerrainDragInput] = frame.Body.LeftTerrainDrag;
        inputs[NeuralBrainSchema.RightTerrainDragInput] = frame.Body.RightTerrainDrag;
        inputs[NeuralBrainSchema.CurrentHabitatQualityInput] = frame.Body.CurrentHabitatQuality;
        inputs[NeuralBrainSchema.ForwardHabitatQualityInput] = frame.Body.ForwardHabitatQuality;
        inputs[NeuralBrainSchema.LeftHabitatQualityInput] = frame.Body.LeftHabitatQuality;
        inputs[NeuralBrainSchema.RightHabitatQualityInput] = frame.Body.RightHabitatQuality;
        inputs[NeuralBrainSchema.EnergySurplusInput] = frame.Internal.EnergySurplusRatio;
        inputs[NeuralBrainSchema.RecentFoodSuccessInput] = frame.Internal.RecentFoodSuccess;
        inputs[NeuralBrainSchema.RecentPlantRawYieldInput] = frame.Internal.RecentPlantRawYield;
        inputs[NeuralBrainSchema.RecentMeatRawYieldInput] = frame.Internal.RecentMeatRawYield;
        inputs[NeuralBrainSchema.RecentPlantEnergyYieldInput] = frame.Internal.RecentPlantEnergyYield;
        inputs[NeuralBrainSchema.RecentMeatEnergyYieldInput] = frame.Internal.RecentMeatEnergyYield;
        inputs[NeuralBrainSchema.RecentFoodEnergyYieldInput] = frame.Internal.RecentFoodEnergyYield;
        inputs[NeuralBrainSchema.RecentTenderPlantEnergyYieldInput] = frame.Internal.RecentTenderPlantEnergyYield;
        inputs[NeuralBrainSchema.RecentRichPlantEnergyYieldInput] = frame.Internal.RecentRichPlantEnergyYield;
        inputs[NeuralBrainSchema.RecentToughPlantEnergyYieldInput] = frame.Internal.RecentToughPlantEnergyYield;
        inputs[NeuralBrainSchema.RecentFreshMeatEnergyYieldInput] = frame.Internal.RecentFreshMeatEnergyYield;
        inputs[NeuralBrainSchema.RecentStaleMeatEnergyYieldInput] = frame.Internal.RecentStaleMeatEnergyYield;
        inputs[NeuralBrainSchema.TenderPlantPayoffTraceInput] = frame.Internal.TenderPlantPayoffTrace;
        inputs[NeuralBrainSchema.RichPlantPayoffTraceInput] = frame.Internal.RichPlantPayoffTrace;
        inputs[NeuralBrainSchema.ToughPlantPayoffTraceInput] = frame.Internal.ToughPlantPayoffTrace;
        inputs[NeuralBrainSchema.FreshMeatPayoffTraceInput] = frame.Internal.FreshMeatPayoffTrace;
        inputs[NeuralBrainSchema.StaleMeatPayoffTraceInput] = frame.Internal.StaleMeatPayoffTrace;
        inputs[NeuralBrainSchema.PlantPreferenceDensityInput] = frame.Vision.PlantPreferenceDensity;
        inputs[NeuralBrainSchema.PlantPreferenceForwardInput] = frame.Vision.PlantPreferenceDirectionForward;
        inputs[NeuralBrainSchema.PlantPreferenceRightInput] = frame.Vision.PlantPreferenceDirectionRight;
        inputs[NeuralBrainSchema.PlantFoodContactPreferenceInput] = frame.Body.PlantFoodContactPreference;
        inputs[NeuralBrainSchema.CreatureSimilarityScentDensityInput] = frame.Scent.CreatureSimilarity.Density;
        inputs[NeuralBrainSchema.CreatureSimilarityScentForwardInput] = frame.Scent.CreatureSimilarity.DirectionForward;
        inputs[NeuralBrainSchema.CreatureSimilarityScentRightInput] = frame.Scent.CreatureSimilarity.DirectionRight;
        inputs[NeuralBrainSchema.CreatureLineageScentDensityInput] = frame.Scent.CreatureLineage.Density;
        inputs[NeuralBrainSchema.CreatureLineageScentForwardInput] = frame.Scent.CreatureLineage.DirectionForward;
        inputs[NeuralBrainSchema.CreatureLineageScentRightInput] = frame.Scent.CreatureLineage.DirectionRight;
        inputs[NeuralBrainSchema.EggLineageScentDensityInput] = frame.Scent.EggLineage.Density;
        inputs[NeuralBrainSchema.EggLineageScentForwardInput] = frame.Scent.EggLineage.DirectionForward;
        inputs[NeuralBrainSchema.EggLineageScentRightInput] = frame.Scent.EggLineage.DirectionRight;
        inputs[NeuralBrainSchema.CreatureIdentityScentDensityInput] = frame.Scent.CreatureIdentity.Density;
        inputs[NeuralBrainSchema.CreatureIdentityScentForwardInput] = frame.Scent.CreatureIdentity.DirectionForward;
        inputs[NeuralBrainSchema.CreatureIdentityScentRightInput] = frame.Scent.CreatureIdentity.DirectionRight;
        inputs[NeuralBrainSchema.EggIdentityScentDensityInput] = frame.Scent.EggIdentity.Density;
        inputs[NeuralBrainSchema.EggIdentityScentForwardInput] = frame.Scent.EggIdentity.DirectionForward;
        inputs[NeuralBrainSchema.EggIdentityScentRightInput] = frame.Scent.EggIdentity.DirectionRight;
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
        inputs[NeuralBrainSchema.FoodProximityInput] = frame.Vision.Food.Proximity;
        inputs[NeuralBrainSchema.FoodDirectionForwardInput] = frame.Vision.Food.DirectionForward;
        inputs[NeuralBrainSchema.FoodDirectionRightInput] = frame.Vision.Food.DirectionRight;
        inputs[NeuralBrainSchema.PlantProximityInput] = frame.Vision.Plant.Proximity;
        inputs[NeuralBrainSchema.PlantDirectionForwardInput] = frame.Vision.Plant.DirectionForward;
        inputs[NeuralBrainSchema.PlantDirectionRightInput] = frame.Vision.Plant.DirectionRight;
        inputs[NeuralBrainSchema.MeatProximityInput] = frame.Vision.Meat.Proximity;
        inputs[NeuralBrainSchema.MeatDirectionForwardInput] = frame.Vision.Meat.DirectionForward;
        inputs[NeuralBrainSchema.MeatDirectionRightInput] = frame.Vision.Meat.DirectionRight;
        inputs[NeuralBrainSchema.VisibleEggDensityInput] = frame.Vision.Egg.Density;
        inputs[NeuralBrainSchema.EggProximityInput] = frame.Vision.Egg.Proximity;
        inputs[NeuralBrainSchema.EggDirectionForwardInput] = frame.Vision.Egg.DirectionForward;
        inputs[NeuralBrainSchema.EggDirectionRightInput] = frame.Vision.Egg.DirectionRight;
        inputs[NeuralBrainSchema.EggVisualLineageSimilarityInput] = frame.Vision.EggLineageSimilarity;
        inputs[NeuralBrainSchema.EggVisualIdentitySimilarityInput] = frame.Vision.EggIdentitySimilarity;
        inputs[NeuralBrainSchema.VisibleSmallPreyDensityInput] = frame.Vision.SmallPrey.Density;
        inputs[NeuralBrainSchema.SmallPreyProximityInput] = frame.Vision.SmallPrey.Proximity;
        inputs[NeuralBrainSchema.SmallPreyDirectionForwardInput] = frame.Vision.SmallPrey.DirectionForward;
        inputs[NeuralBrainSchema.SmallPreyDirectionRightInput] = frame.Vision.SmallPrey.DirectionRight;
        inputs[NeuralBrainSchema.SmallPreyGrabOpportunityInput] = frame.Vision.SmallPreyGrabOpportunity;
        inputs[NeuralBrainSchema.CreatureProximityInput] = frame.Vision.Creature.Proximity;
        inputs[NeuralBrainSchema.CreatureDirectionForwardInput] = frame.Vision.Creature.DirectionForward;
        inputs[NeuralBrainSchema.CreatureDirectionRightInput] = frame.Vision.Creature.DirectionRight;
        inputs[NeuralBrainSchema.CreatureRelativeBodySizeInput] = frame.Vision.CreatureRelativeBodySize;
        inputs[NeuralBrainSchema.CreatureRelativeSpeedInput] = frame.Vision.CreatureRelativeSpeed;
        inputs[NeuralBrainSchema.CreatureApproachRateInput] = frame.Vision.CreatureApproachRate;
        inputs[NeuralBrainSchema.CreatureFacingAlignmentInput] = frame.Vision.CreatureFacingAlignment;
        inputs[NeuralBrainSchema.CreatureVisualTraitSimilarityInput] = frame.Vision.CreatureTraitSimilarity;
        inputs[NeuralBrainSchema.CreatureVisualLineageSimilarityInput] = frame.Vision.CreatureLineageSimilarity;
        inputs[NeuralBrainSchema.CreatureVisualIdentitySimilarityInput] = frame.Vision.CreatureIdentitySimilarity;
        inputs[NeuralBrainSchema.FoodContactInput] = frame.Body.FoodContact;
        inputs[NeuralBrainSchema.PlantFoodContactInput] = frame.Body.PlantFoodContact;
        inputs[NeuralBrainSchema.PlantFoodContactEnergyQualityInput] = frame.Body.PlantFoodContactEnergyQuality;
        inputs[NeuralBrainSchema.PlantFoodContactBiteEaseInput] = frame.Body.PlantFoodContactBiteEase;
        inputs[NeuralBrainSchema.MeatFoodContactInput] = frame.Body.MeatFoodContact;
        inputs[NeuralBrainSchema.EggFoodContactInput] = frame.Body.EggFoodContact;
        inputs[NeuralBrainSchema.CreatureContactInput] = frame.Body.CreatureContact;
        inputs[NeuralBrainSchema.CreatureContactSimilarityInput] = frame.Body.CreatureContactSimilarity;
        inputs[NeuralBrainSchema.CreatureContactLineageSimilarityInput] = frame.Body.CreatureContactLineageSimilarity;
        inputs[NeuralBrainSchema.EggContactLineageSimilarityInput] = frame.Body.EggContactLineageSimilarity;
        inputs[NeuralBrainSchema.CreatureContactIdentitySimilarityInput] = frame.Body.CreatureContactIdentitySimilarity;
        inputs[NeuralBrainSchema.EggContactIdentitySimilarityInput] = frame.Body.EggContactIdentitySimilarity;
        inputs[NeuralBrainSchema.GrabPressureInput] = frame.Body.GrabPressure;
        inputs[NeuralBrainSchema.GrabDirectionForwardInput] = frame.Body.GrabDirectionForward;
        inputs[NeuralBrainSchema.GrabDirectionRightInput] = frame.Body.GrabDirectionRight;
        inputs[NeuralBrainSchema.CanGrabCreatureInput] = frame.Body.CanGrabCreature;
        inputs[NeuralBrainSchema.IsHoldingCreatureInput] = frame.Body.IsHoldingCreature;
        inputs[NeuralBrainSchema.SoundDensityInput] = frame.Communication.Sound.Density;
        inputs[NeuralBrainSchema.SoundDirectionForwardInput] = frame.Communication.Sound.DirectionForward;
        inputs[NeuralBrainSchema.SoundDirectionRightInput] = frame.Communication.Sound.DirectionRight;
        inputs[NeuralBrainSchema.SoundToneInput] = frame.Communication.Sound.Tone;
        inputs[NeuralBrainSchema.SoundToneClarityInput] = frame.Communication.Sound.ToneClarity;
        inputs[NeuralBrainSchema.FatRatioInput] = frame.Internal.FatRatio;
        inputs[NeuralBrainSchema.MassBurdenInput] = frame.Internal.MassBurdenRatio;
        inputs[NeuralBrainSchema.CurrentTemperatureInput] = frame.Body.CurrentTemperature;
        inputs[NeuralBrainSchema.ForwardTemperatureInput] = frame.Body.ForwardTemperature;
        inputs[NeuralBrainSchema.LeftTemperatureInput] = frame.Body.LeftTemperature;
        inputs[NeuralBrainSchema.RightTemperatureInput] = frame.Body.RightTemperature;
        inputs[NeuralBrainSchema.CurrentThermalMismatchInput] = frame.Body.CurrentThermalMismatch;
        inputs[NeuralBrainSchema.ForwardThermalMismatchInput] = frame.Body.ForwardThermalMismatch;
        inputs[NeuralBrainSchema.LeftThermalMismatchInput] = frame.Body.LeftThermalMismatch;
        inputs[NeuralBrainSchema.RightThermalMismatchInput] = frame.Body.RightThermalMismatch;
        inputs[NeuralBrainSchema.EnergyFullnessInput] = frame.Internal.EnergyFullnessRatio;
        inputs[NeuralBrainSchema.GutFullnessInput] = frame.Internal.GutFullnessRatio;
        inputs[NeuralBrainSchema.InjuryMemoryForwardInput] = frame.Internal.InjuryMemoryDirectionForward;
        inputs[NeuralBrainSchema.InjuryMemoryRightInput] = frame.Internal.InjuryMemoryDirectionRight;
        inputs[NeuralBrainSchema.InjuryMemoryStrengthInput] = frame.Internal.InjuryMemoryStrength;
        inputs[NeuralBrainSchema.MaturityProgressInput] = frame.Internal.MaturityProgress;
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
            outputs[NeuralBrainSchema.AttackOutput],
            Math.Clamp(outputs[NeuralBrainSchema.GrabOutput], 0f, 1f),
            Math.Clamp(outputs[NeuralBrainSchema.SoundAmplitudeOutput], 0f, 1f),
            Math.Clamp(outputs[NeuralBrainSchema.SoundToneOutput], -1f, 1f));
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
