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
    /// Calories transferred from food during the most recent eating pass.
    /// </summary>
    public float LastCaloriesEaten { get; set; }

    public float LastAttackDamageDealt { get; set; }

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
