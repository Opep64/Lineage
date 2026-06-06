namespace Lineage.Core;

/// <summary>
/// Stable input/output layout for the first evolvable brain model.
/// </summary>
public static class NeuralBrainSchema
{
    public const int InputSchemaVersion = 13;

    public const int OutputSchemaVersion = 3;

    public const int DefaultHiddenNodeCount = 4;

    public const int DefaultHiddenLayerNodeCount = 10;

    public const int HybridDeep8x8FirstLayerNodeCount = 8;

    public const int HybridDeep8x8SecondLayerNodeCount = 8;

    public const int HybridDeep8x8HiddenNodeCount =
        HybridDeep8x8FirstLayerNodeCount + HybridDeep8x8SecondLayerNodeCount;

    public const int MaxHiddenNodeCount = 64;

    public const int BiasInput = 0;

    public const int EnergyRatioInput = 1;

    public const int HungerInput = 2;

    public const int VisibleFoodDensityInput = 3;

    public const int VisiblePlantDensityInput = 4;

    public const int VisibleMeatDensityInput = 5;

    public const int DietaryMeatBiasInput = 6;

    public const int EggReserveRatioInput = 7;

    public const int ReproductionReadinessInput = 8;

    public const int VisibleCreatureDensityInput = 9;

    public const int MeatScentDensityInput = 10;

    public const int MeatScentForwardInput = 11;

    public const int MeatScentRightInput = 12;

    public const int CurrentTerrainDragInput = 13;

    public const int ForwardTerrainDragInput = 14;

    public const int LeftTerrainDragInput = 15;

    public const int RightTerrainDragInput = 16;

    public const int EnergySurplusInput = 17;

    public const int RecentFoodSuccessInput = 18;

    public const int MemoryForwardInput = 19;

    public const int MemoryRightInput = 20;

    public const int MemoryStrengthInput = 21;

    public const int VisibleMeatFreshnessInput = 22;

    public const int RottenMeatScentDensityInput = 23;

    public const int RottenMeatScentForwardInput = 24;

    public const int RottenMeatScentRightInput = 25;

    public const int ForwardObstacleInput = 26;

    public const int LeftObstacleInput = 27;

    public const int RightObstacleInput = 28;

    public const int MovementBlockedInput = 29;

    public const int FoodProximityInput = 30;

    public const int FoodDirectionForwardInput = FoodProximityInput + 1;

    public const int FoodDirectionRightInput = FoodDirectionForwardInput + 1;

    public const int PlantProximityInput = FoodDirectionRightInput + 1;

    public const int PlantDirectionForwardInput = PlantProximityInput + 1;

    public const int PlantDirectionRightInput = PlantDirectionForwardInput + 1;

    public const int MeatProximityInput = PlantDirectionRightInput + 1;

    public const int MeatDirectionForwardInput = MeatProximityInput + 1;

    public const int MeatDirectionRightInput = MeatDirectionForwardInput + 1;

    public const int VisibleEggDensityInput = MeatDirectionRightInput + 1;

    public const int EggProximityInput = VisibleEggDensityInput + 1;

    public const int EggDirectionForwardInput = EggProximityInput + 1;

    public const int EggDirectionRightInput = EggDirectionForwardInput + 1;

    public const int EggVisualLineageSimilarityInput = EggDirectionRightInput + 1;

    public const int EggVisualIdentitySimilarityInput = EggVisualLineageSimilarityInput + 1;

    public const int VisibleSmallPreyDensityInput = EggVisualIdentitySimilarityInput + 1;

    public const int SmallPreyProximityInput = VisibleSmallPreyDensityInput + 1;

    public const int SmallPreyDirectionForwardInput = SmallPreyProximityInput + 1;

    public const int SmallPreyDirectionRightInput = SmallPreyDirectionForwardInput + 1;

    public const int SmallPreyGrabOpportunityInput = SmallPreyDirectionRightInput + 1;

    public const int CreatureProximityInput = SmallPreyGrabOpportunityInput + 1;

    public const int CreatureDirectionForwardInput = CreatureProximityInput + 1;

    public const int CreatureDirectionRightInput = CreatureDirectionForwardInput + 1;

    public const int CreatureRelativeBodySizeInput = CreatureDirectionRightInput + 1;

    public const int CreatureRelativeSpeedInput = CreatureRelativeBodySizeInput + 1;

    public const int CreatureApproachRateInput = CreatureRelativeSpeedInput + 1;

    public const int CreatureFacingAlignmentInput = CreatureApproachRateInput + 1;

    public const int CreatureVisualTraitSimilarityInput = CreatureFacingAlignmentInput + 1;

    public const int CreatureVisualLineageSimilarityInput = CreatureVisualTraitSimilarityInput + 1;

    public const int CreatureVisualIdentitySimilarityInput = CreatureVisualLineageSimilarityInput + 1;

    public const int FoodContactInput = CreatureVisualIdentitySimilarityInput + 1;

    public const int PlantFoodContactInput = FoodContactInput + 1;

    public const int MeatFoodContactInput = FoodContactInput + 2;

    public const int EggFoodContactInput = FoodContactInput + 3;

    public const int CreatureContactInput = EggFoodContactInput + 1;

    public const int HealthRatioInput = CreatureContactInput + 1;

    public const int VisiblePlantEnergyQualityInput = HealthRatioInput + 1;

    public const int VisiblePlantBiteEaseInput = VisiblePlantEnergyQualityInput + 1;

    public const int PlantFoodContactEnergyQualityInput = VisiblePlantBiteEaseInput + 1;

    public const int PlantFoodContactBiteEaseInput = PlantFoodContactEnergyQualityInput + 1;

    public const int RecentPlantRawYieldInput = PlantFoodContactBiteEaseInput + 1;

    public const int RecentPlantEnergyYieldInput = RecentPlantRawYieldInput + 1;

    public const int RecentFoodEnergyYieldInput = RecentPlantEnergyYieldInput + 1;

    public const int RecentTenderPlantEnergyYieldInput = RecentFoodEnergyYieldInput + 1;

    public const int RecentRichPlantEnergyYieldInput = RecentTenderPlantEnergyYieldInput + 1;

    public const int RecentToughPlantEnergyYieldInput = RecentRichPlantEnergyYieldInput + 1;

    public const int TenderPlantPayoffTraceInput = RecentToughPlantEnergyYieldInput + 1;

    public const int RichPlantPayoffTraceInput = TenderPlantPayoffTraceInput + 1;

    public const int ToughPlantPayoffTraceInput = RichPlantPayoffTraceInput + 1;

    public const int PlantPreferenceDensityInput = ToughPlantPayoffTraceInput + 1;

    public const int PlantPreferenceForwardInput = PlantPreferenceDensityInput + 1;

    public const int PlantPreferenceRightInput = PlantPreferenceForwardInput + 1;

    public const int PlantFoodContactPreferenceInput = PlantPreferenceRightInput + 1;

    public const int CreatureSimilarityScentDensityInput = PlantFoodContactPreferenceInput + 1;

    public const int CreatureSimilarityScentForwardInput = CreatureSimilarityScentDensityInput + 1;

    public const int CreatureSimilarityScentRightInput = CreatureSimilarityScentForwardInput + 1;

    public const int CreatureContactSimilarityInput = CreatureSimilarityScentRightInput + 1;

    public const int CreatureLineageScentDensityInput = CreatureContactSimilarityInput + 1;

    public const int CreatureLineageScentForwardInput = CreatureLineageScentDensityInput + 1;

    public const int CreatureLineageScentRightInput = CreatureLineageScentForwardInput + 1;

    public const int CreatureContactLineageSimilarityInput = CreatureLineageScentRightInput + 1;

    public const int EggContactLineageSimilarityInput = CreatureContactLineageSimilarityInput + 1;

    public const int CurrentHabitatQualityInput = EggContactLineageSimilarityInput + 1;

    public const int ForwardHabitatQualityInput = CurrentHabitatQualityInput + 1;

    public const int LeftHabitatQualityInput = ForwardHabitatQualityInput + 1;

    public const int RightHabitatQualityInput = LeftHabitatQualityInput + 1;

    public const int GrabPressureInput = RightHabitatQualityInput + 1;

    public const int GrabDirectionForwardInput = GrabPressureInput + 1;

    public const int GrabDirectionRightInput = GrabDirectionForwardInput + 1;

    public const int CanGrabCreatureInput = GrabDirectionRightInput + 1;

    public const int IsHoldingCreatureInput = CanGrabCreatureInput + 1;

    public const int SoundDensityInput = IsHoldingCreatureInput + 1;

    public const int SoundDirectionForwardInput = SoundDensityInput + 1;

    public const int SoundDirectionRightInput = SoundDirectionForwardInput + 1;

    public const int SoundToneInput = SoundDirectionRightInput + 1;

    public const int SoundToneClarityInput = SoundToneInput + 1;

    public const int FatRatioInput = SoundToneClarityInput + 1;

    public const int MassBurdenInput = FatRatioInput + 1;

    public const int CurrentTemperatureInput = MassBurdenInput + 1;

    public const int ForwardTemperatureInput = CurrentTemperatureInput + 1;

    public const int LeftTemperatureInput = ForwardTemperatureInput + 1;

    public const int RightTemperatureInput = LeftTemperatureInput + 1;

    public const int CurrentThermalMismatchInput = RightTemperatureInput + 1;

    public const int ForwardThermalMismatchInput = CurrentThermalMismatchInput + 1;

    public const int LeftThermalMismatchInput = ForwardThermalMismatchInput + 1;

    public const int RightThermalMismatchInput = LeftThermalMismatchInput + 1;

    public const int EnergyFullnessInput = RightThermalMismatchInput + 1;

    public const int GutFullnessInput = EnergyFullnessInput + 1;

    public const int EggLineageScentDensityInput = GutFullnessInput + 1;

    public const int EggLineageScentForwardInput = EggLineageScentDensityInput + 1;

    public const int EggLineageScentRightInput = EggLineageScentForwardInput + 1;

    public const int CreatureIdentityScentDensityInput = EggLineageScentRightInput + 1;

    public const int CreatureIdentityScentForwardInput = CreatureIdentityScentDensityInput + 1;

    public const int CreatureIdentityScentRightInput = CreatureIdentityScentForwardInput + 1;

    public const int EggIdentityScentDensityInput = CreatureIdentityScentRightInput + 1;

    public const int EggIdentityScentForwardInput = EggIdentityScentDensityInput + 1;

    public const int EggIdentityScentRightInput = EggIdentityScentForwardInput + 1;

    public const int CreatureContactIdentitySimilarityInput = EggIdentityScentRightInput + 1;

    public const int EggContactIdentitySimilarityInput = CreatureContactIdentitySimilarityInput + 1;

    public const int InjuryMemoryForwardInput = EggContactIdentitySimilarityInput + 1;

    public const int InjuryMemoryRightInput = InjuryMemoryForwardInput + 1;

    public const int InjuryMemoryStrengthInput = InjuryMemoryRightInput + 1;

    public const int MaturityProgressInput = InjuryMemoryStrengthInput + 1;

    public const int InputCount = MaturityProgressInput + 1;

    public const int VisiblePreyDensityInput = VisibleSmallPreyDensityInput;

    public const int OutputCount = 10;

    public const int MoveForwardOutput = 0;

    public const int TurnOutput = 1;

    public const int EatOutput = 2;

    public const int ReproduceOutput = 3;

    public const int AttackOutput = 4;

    public const int GrabOutput = 5;

    public const int SoundAmplitudeOutput = 6;

    public const int SoundToneOutput = 7;

    public const int MemoryForwardOutput = 8;

    public const int MemoryRightOutput = 9;
}
