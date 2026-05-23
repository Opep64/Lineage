namespace Lineage.Core;

/// <summary>
/// Hot per-creature state that systems will update every tick.
/// </summary>
public struct CreatureState
{
    public EntityId Id { get; set; }

    /// <summary>
    /// Parent entity ID, or the default ID for founding creatures.
    /// </summary>
    public EntityId ParentId { get; set; }

    public SimVector2 Position { get; set; }

    /// <summary>
    /// Farthest eastward world position this creature has reached.
    /// </summary>
    public float MaxXReached { get; set; }

    public SimVector2 Velocity { get; set; }

    /// <summary>
    /// Movement request produced by the current controller before movement is resolved.
    /// </summary>
    public SimVector2 DesiredVelocity { get; set; }

    public float HeadingRadians { get; set; }

    public CreatureSenseState Senses { get; set; }

    public CreatureActionState Actions { get; set; }

    public float AgeSeconds { get; set; }

    public float Energy { get; set; }

    /// <summary>
    /// Energy already committed to the next egg before it is laid.
    /// </summary>
    public float ReproductiveEnergy { get; set; }

    /// <summary>
    /// True when the creature's body is close enough to a resource patch or egg to eat it.
    /// </summary>
    public bool IsTouchingFood { get; set; }

    /// <summary>
    /// Kind of edible target currently within eating range.
    /// </summary>
    public FoodContactKind FoodContactKind { get; set; }

    /// <summary>
    /// Resource or egg currently within eating range, or the default ID when no food is in contact.
    /// </summary>
    public EntityId FoodContactResourceId { get; set; }

    /// <summary>
    /// Distance from the creature center to the contacted food edge.
    /// </summary>
    public float FoodContactEdgeDistance { get; set; }

    /// <summary>
    /// Calories remaining in the contacted food before this tick's eating transfer.
    /// </summary>
    public float FoodContactCalories { get; set; }

    public bool IsTouchingCreature { get; set; }

    public EntityId CreatureContactId { get; set; }

    public float CreatureContactEdgeDistance { get; set; }

    /// <summary>
    /// Raw calories transferred from food into the gut during the most recent eating pass.
    /// </summary>
    public float LastCaloriesEaten { get; set; }

    public float LastPlantCaloriesEaten { get; set; }

    public float LastCarcassCaloriesEaten { get; set; }

    public float LastEggCaloriesEaten { get; set; }

    public float LastLivePreyCaloriesEaten { get; set; }

    public float LastFreshMeatCaloriesEaten { get; set; }

    public float LastStaleMeatCaloriesEaten { get; set; }

    /// <summary>
    /// World units moved during the most recent movement pass.
    /// </summary>
    public float LastDistanceTraveled { get; set; }

    /// <summary>
    /// World units moved since this creature last transferred calories into its gut.
    /// </summary>
    public float DistanceSinceLastMeal { get; set; }

    /// <summary>
    /// Energy released from gut contents during the most recent digestion pass.
    /// </summary>
    public float LastCaloriesDigested { get; set; }

    public float LastPlantDigestedEnergy { get; set; }

    public float LastMeatDigestedEnergy { get; set; }

    public float GutPlantCalories { get; set; }

    public float GutMeatCalories { get; set; }

    /// <summary>
    /// Meat gut calories weighted by freshness, used to release less energy from stale meat.
    /// </summary>
    public float GutMeatQualityCalories { get; set; }

    public float LastAttackDamageDealt { get; set; }

    /// <summary>
    /// Most recent creature that damaged this one, used to attribute fresh-kill meat after injury deaths.
    /// </summary>
    public EntityId LastDamagingCreatureId { get; set; }

    /// <summary>
    /// Seconds since this creature last transferred calories from food.
    /// </summary>
    public float SecondsSinceLastMeal { get; set; }

    public float Health { get; set; }

    /// <summary>
    /// Relative reproductive investment received at birth; founders use neutral quality.
    /// </summary>
    public float BirthInvestmentRatio { get; set; }

    public float ReproductionCooldownSeconds { get; set; }

    public int Generation { get; set; }

    public int GenomeId { get; set; }

    public int BrainId { get; set; }
}
