namespace Lineage.Core;

/// <summary>
/// Stable input/output layout for the first evolvable brain model.
/// </summary>
public static class NeuralBrainSchema
{
    public const int InputSchemaVersion = 6;

    public const int OutputSchemaVersion = 3;

    public const int DefaultHiddenNodeCount = 4;

    public const int DefaultHiddenLayerNodeCount = 10;

    public const int HybridDeep8x8FirstLayerNodeCount = 8;

    public const int HybridDeep8x8SecondLayerNodeCount = 8;

    public const int HybridDeep8x8HiddenNodeCount =
        HybridDeep8x8FirstLayerNodeCount + HybridDeep8x8SecondLayerNodeCount;

    public const int MaxHiddenNodeCount = 64;

    public const int VisionSectorInputStart = 30;

    public const int VisionSectorChannelCount = 18;

    public const int FoodContactInput = VisionSectorInputStart + VisionSectorSet.SectorCount * VisionSectorChannelCount;

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

    public const int CurrentHabitatQualityInput = CreatureContactSimilarityInput + 1;

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

    public const int InputCount = RightThermalMismatchInput + 1;

    public const int OutputCount = 10;

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

    public const int VisionSectorPlantDensityOffset = 0;

    public const int VisionSectorPlantProximityOffset = 1;

    public const int VisionSectorPlantEnergyQualityOffset = 2;

    public const int VisionSectorPlantBiteEaseOffset = 3;

    public const int VisionSectorMeatDensityOffset = 4;

    public const int VisionSectorMeatProximityOffset = 5;

    public const int VisionSectorEggDensityOffset = 6;

    public const int VisionSectorEggProximityOffset = 7;

    public const int VisionSectorCreatureDensityOffset = 8;

    public const int VisionSectorCreatureProximityOffset = 9;

    public const int VisionSectorSmallerCreatureDensityOffset = 10;

    public const int VisionSectorSmallerCreatureProximityOffset = 11;

    public const int VisionSectorSimilarCreatureDensityOffset = 12;

    public const int VisionSectorSimilarCreatureProximityOffset = 13;

    public const int VisionSectorLargerCreatureDensityOffset = 14;

    public const int VisionSectorLargerCreatureProximityOffset = 15;

    public const int VisionSectorCreatureApproachRateOffset = 16;

    public const int VisionSectorCreatureFacingAlignmentOffset = 17;

    public const int VisiblePreyDensityInput = VisibleCreatureDensityInput;

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

    public static int VisionSectorPlantDensityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorPlantDensityOffset);
    }

    public static int VisionSectorPlantProximityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorPlantProximityOffset);
    }

    public static int VisionSectorPlantEnergyQualityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorPlantEnergyQualityOffset);
    }

    public static int VisionSectorPlantBiteEaseInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorPlantBiteEaseOffset);
    }

    public static int VisionSectorMeatDensityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorMeatDensityOffset);
    }

    public static int VisionSectorMeatProximityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorMeatProximityOffset);
    }

    public static int VisionSectorEggDensityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorEggDensityOffset);
    }

    public static int VisionSectorEggProximityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorEggProximityOffset);
    }

    public static int VisionSectorCreatureDensityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorCreatureDensityOffset);
    }

    public static int VisionSectorCreatureProximityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorCreatureProximityOffset);
    }

    public static int VisionSectorSmallerCreatureDensityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorSmallerCreatureDensityOffset);
    }

    public static int VisionSectorSmallerCreatureProximityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorSmallerCreatureProximityOffset);
    }

    public static int VisionSectorSimilarCreatureDensityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorSimilarCreatureDensityOffset);
    }

    public static int VisionSectorSimilarCreatureProximityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorSimilarCreatureProximityOffset);
    }

    public static int VisionSectorLargerCreatureDensityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorLargerCreatureDensityOffset);
    }

    public static int VisionSectorLargerCreatureProximityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorLargerCreatureProximityOffset);
    }

    public static int VisionSectorCreatureApproachRateInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorCreatureApproachRateOffset);
    }

    public static int VisionSectorCreatureFacingAlignmentInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorCreatureFacingAlignmentOffset);
    }

    public static int GetVisionSectorInput(int sectorIndex, int channelOffset)
    {
        if ((uint)sectorIndex >= VisionSectorSet.SectorCount)
        {
            throw new ArgumentOutOfRangeException(nameof(sectorIndex));
        }

        if ((uint)channelOffset >= VisionSectorChannelCount)
        {
            throw new ArgumentOutOfRangeException(nameof(channelOffset));
        }

        return VisionSectorInputStart + sectorIndex * VisionSectorChannelCount + channelOffset;
    }
}
