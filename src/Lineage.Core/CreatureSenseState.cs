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
    public VisionSectorSet VisionSectors { get; set; }

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

    /// <summary>
    /// Freshness of the nearest visible meat-like food. Fresh meat or eggs are near 1; old carcasses trend lower.
    /// </summary>
    public float VisibleMeatFreshness { get; set; }

    public bool MeatScentDetected { get; set; }

    public float MeatScentDensity { get; set; }

    public float MeatScentDirectionForward { get; set; }

    public float MeatScentDirectionRight { get; set; }

    public bool RottenMeatScentDetected { get; set; }

    public float RottenMeatScentDensity { get; set; }

    public float RottenMeatScentDirectionForward { get; set; }

    public float RottenMeatScentDirectionRight { get; set; }

    public bool CreatureDetected { get; set; }

    public float CreatureProximity { get; set; }

    public float CreatureDirectionForward { get; set; }

    public float CreatureDirectionRight { get; set; }

    public float VisibleCreatureDensity { get; set; }

    /// <summary>
    /// Relative body radius of the nearest visible creature, where negative is smaller and positive is larger.
    /// </summary>
    public float CreatureRelativeBodySize { get; set; }

    /// <summary>
    /// Relative speed of the nearest visible creature compared with this creature, clamped to [-1, 1].
    /// </summary>
    public float CreatureRelativeSpeed { get; set; }

    /// <summary>
    /// Closing rate of the nearest visible creature. Positive means the distance is shrinking.
    /// </summary>
    public float CreatureApproachRate { get; set; }

    /// <summary>
    /// Facing alignment of the nearest visible creature. Positive means it is pointed toward this creature.
    /// </summary>
    public float CreatureFacingAlignment { get; set; }

    /// <summary>
    /// Local traversal drag at the creature's current body position. Neutral terrain is 0, slower terrain is positive.
    /// </summary>
    public float CurrentTerrainDrag { get; set; }

    /// <summary>
    /// Local traversal drag sampled a short distance in front of the creature's heading.
    /// </summary>
    public float ForwardTerrainDrag { get; set; }

    /// <summary>
    /// Local traversal drag sampled a short distance to the creature's left.
    /// </summary>
    public float LeftTerrainDrag { get; set; }

    /// <summary>
    /// Local traversal drag sampled a short distance to the creature's right.
    /// </summary>
    public float RightTerrainDrag { get; set; }

    /// <summary>
    /// Hard obstacle proximity sampled a short distance in front of the creature.
    /// </summary>
    public float ForwardObstacle { get; set; }

    /// <summary>
    /// Hard obstacle proximity sampled a short distance to the creature's left.
    /// </summary>
    public float LeftObstacle { get; set; }

    /// <summary>
    /// Hard obstacle proximity sampled a short distance to the creature's right.
    /// </summary>
    public float RightObstacle { get; set; }

    /// <summary>
    /// Contact cue from the previous movement pass; 1 means movement was blocked by an obstacle.
    /// </summary>
    public float MovementBlocked { get; set; }

    /// <summary>
    /// Contact cue from the previous eating pass; 1 means edible food was within body reach.
    /// </summary>
    public float FoodContact { get; set; }

    public float PlantFoodContact { get; set; }

    public float MeatFoodContact { get; set; }

    public float EggFoodContact { get; set; }

    // Legacy aliases kept populated for older report/snapshot readers and tests in progress.
    public bool PreyDetected { get; set; }

    public float PreyProximity { get; set; }

    public float PreyDirectionForward { get; set; }

    public float PreyDirectionRight { get; set; }

    public float VisiblePreyDensity { get; set; }

    public float EnergyRatio { get; set; }

    public float Hunger { get; set; }

    public float EggReserveRatio { get; set; }

    public float EnergySurplusRatio { get; set; }

    public float ReproductionReadiness { get; set; }

    public float RecentFoodSuccess { get; set; }

    /// <summary>
    /// Persistent memory vector projected onto the creature's forward direction.
    /// </summary>
    public float MemoryDirectionForward { get; set; }

    /// <summary>
    /// Persistent memory vector projected onto the creature's right direction.
    /// </summary>
    public float MemoryDirectionRight { get; set; }

    /// <summary>
    /// Magnitude of the creature's current memory vector, clamped to [0, 1].
    /// </summary>
    public float MemoryStrength { get; set; }
}
