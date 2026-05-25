namespace Lineage.Core;

/// <summary>
/// Stable input/output layout for the first evolvable brain model.
/// </summary>
public static class NeuralBrainSchema
{
    public const int DefaultHiddenNodeCount = 4;

    public const int DefaultHiddenLayerNodeCount = 16;

    public const int MaxHiddenNodeCount = 64;

    public const int VisionSectorInputStart = 46;

    public const int VisionSectorChannelCount = 16;

    public const int FoodContactInput = VisionSectorInputStart + VisionSectorSet.SectorCount * VisionSectorChannelCount;

    public const int PlantFoodContactInput = FoodContactInput + 1;

    public const int MeatFoodContactInput = FoodContactInput + 2;

    public const int EggFoodContactInput = FoodContactInput + 3;

    public const int HealthRatioInput = EggFoodContactInput + 1;

    public const int InputCount = HealthRatioInput + 1;

    public const int OutputCount = 7;

    public const int BiasInput = 0;

    public const int EnergyRatioInput = 1;

    public const int HungerInput = 2;

    public const int FoodProximityInput = 3;

    public const int FoodForwardInput = 4;

    public const int FoodRightInput = 5;

    public const int VisibleFoodDensityInput = 6;

    public const int VisiblePlantDensityInput = 7;

    public const int PlantProximityInput = 8;

    public const int PlantForwardInput = 9;

    public const int PlantRightInput = 10;

    public const int VisibleMeatDensityInput = 11;

    public const int MeatProximityInput = 12;

    public const int MeatForwardInput = 13;

    public const int MeatRightInput = 14;

    public const int DietaryMeatBiasInput = 15;

    public const int EggReserveRatioInput = 16;

    public const int ReproductionReadinessInput = 17;

    public const int VisibleCreatureDensityInput = 18;

    public const int CreatureProximityInput = 19;

    public const int CreatureForwardInput = 20;

    public const int CreatureRightInput = 21;

    public const int MeatScentDensityInput = 22;

    public const int MeatScentForwardInput = 23;

    public const int MeatScentRightInput = 24;

    public const int CreatureRelativeBodySizeInput = 25;

    public const int CreatureRelativeSpeedInput = 26;

    public const int CreatureApproachRateInput = 27;

    public const int CreatureFacingAlignmentInput = 28;

    public const int CurrentTerrainDragInput = 29;

    public const int ForwardTerrainDragInput = 30;

    public const int LeftTerrainDragInput = 31;

    public const int RightTerrainDragInput = 32;

    public const int EnergySurplusInput = 33;

    public const int RecentFoodSuccessInput = 34;

    public const int MemoryForwardInput = 35;

    public const int MemoryRightInput = 36;

    public const int MemoryStrengthInput = 37;

    public const int VisibleMeatFreshnessInput = 38;

    public const int RottenMeatScentDensityInput = 39;

    public const int RottenMeatScentForwardInput = 40;

    public const int RottenMeatScentRightInput = 41;

    public const int ForwardObstacleInput = 42;

    public const int LeftObstacleInput = 43;

    public const int RightObstacleInput = 44;

    public const int MovementBlockedInput = 45;

    public const int VisionSectorPlantDensityOffset = 0;

    public const int VisionSectorPlantProximityOffset = 1;

    public const int VisionSectorMeatDensityOffset = 2;

    public const int VisionSectorMeatProximityOffset = 3;

    public const int VisionSectorEggDensityOffset = 4;

    public const int VisionSectorEggProximityOffset = 5;

    public const int VisionSectorCreatureDensityOffset = 6;

    public const int VisionSectorCreatureProximityOffset = 7;

    public const int VisionSectorSmallerCreatureDensityOffset = 8;

    public const int VisionSectorSmallerCreatureProximityOffset = 9;

    public const int VisionSectorSimilarCreatureDensityOffset = 10;

    public const int VisionSectorSimilarCreatureProximityOffset = 11;

    public const int VisionSectorLargerCreatureDensityOffset = 12;

    public const int VisionSectorLargerCreatureProximityOffset = 13;

    public const int VisionSectorCreatureApproachRateOffset = 14;

    public const int VisionSectorCreatureFacingAlignmentOffset = 15;

    public const int VisiblePreyDensityInput = VisibleCreatureDensityInput;

    public const int PreyProximityInput = CreatureProximityInput;

    public const int PreyForwardInput = CreatureForwardInput;

    public const int PreyRightInput = CreatureRightInput;

    public const int MoveForwardOutput = 0;

    public const int TurnOutput = 1;

    public const int EatOutput = 2;

    public const int ReproduceOutput = 3;

    public const int AttackOutput = 4;

    public const int MemoryForwardOutput = 5;

    public const int MemoryRightOutput = 6;

    public static int VisionSectorPlantDensityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorPlantDensityOffset);
    }

    public static int VisionSectorPlantProximityInput(int sectorIndex)
    {
        return GetVisionSectorInput(sectorIndex, VisionSectorPlantProximityOffset);
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
