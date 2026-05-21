namespace Lineage.Core;

/// <summary>
/// Stable input/output layout for the first evolvable brain model.
/// </summary>
public static class NeuralBrainSchema
{
    public const int InputCount = 22;

    public const int OutputCount = 5;

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

    public const int VisiblePreyDensityInput = 18;

    public const int PreyProximityInput = 19;

    public const int PreyForwardInput = 20;

    public const int PreyRightInput = 21;

    public const int MoveForwardOutput = 0;

    public const int TurnOutput = 1;

    public const int EatOutput = 2;

    public const int ReproduceOutput = 3;

    public const int AttackOutput = 4;
}
