namespace Lineage.Core;

/// <summary>
/// Explicit local and internal inputs available to a creature controller for one tick.
/// </summary>
///
/// <remarks>
/// These values are deliberately local and imperfect. A creature can sense food-like
/// cues relative to its body heading and its own internal state, but not absolute map
/// truth such as "nearest biome direction" or current world coordinates.
/// </remarks>
public struct CreatureSenseState
{
    public bool FoodDetected { get; set; }

    public float FoodProximity { get; set; }

    public float FoodDirectionForward { get; set; }

    public float FoodDirectionRight { get; set; }

    public float VisibleFoodDensity { get; set; }

    public bool PlantDetected { get; set; }

    public float PlantProximity { get; set; }

    public float PlantDirectionForward { get; set; }

    public float PlantDirectionRight { get; set; }

    public float VisiblePlantDensity { get; set; }

    public bool MeatDetected { get; set; }

    public float MeatProximity { get; set; }

    public float MeatDirectionForward { get; set; }

    public float MeatDirectionRight { get; set; }

    public float VisibleMeatDensity { get; set; }

    public bool PreyDetected { get; set; }

    public float PreyProximity { get; set; }

    public float PreyDirectionForward { get; set; }

    public float PreyDirectionRight { get; set; }

    public float VisiblePreyDensity { get; set; }

    public float EnergyRatio { get; set; }

    public float Hunger { get; set; }

    public float EggReserveRatio { get; set; }

    public float ReproductionReadiness { get; set; }
}
