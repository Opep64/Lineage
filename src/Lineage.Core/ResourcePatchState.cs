namespace Lineage.Core;

/// <summary>
/// Early placeholder for food/resource entities.
/// </summary>
///
/// <remarks>
/// Later versions may split plants, carrion, water, and minerals into separate data
/// sets. Keeping the first resource state generic makes the v0.1 loop easier to grow.
/// </remarks>
public struct ResourcePatchState
{
    public EntityId Id { get; set; }

    public ResourceKind Kind { get; set; }

    public SimVector2 Position { get; set; }

    public float Radius { get; set; }

    public float Calories { get; set; }

    public float MaxCalories { get; set; }

    public float RegrowthCaloriesPerSecond { get; set; }

    public float DecayCaloriesPerSecond { get; set; }

    /// <summary>
    /// Remaining dormant time before a depleted plant can reappear as edible food.
    /// </summary>
    public float RespawnSecondsRemaining { get; set; }

    /// <summary>
    /// Original dormant duration sampled when the plant entered dormancy.
    /// </summary>
    public float RespawnSecondsTotal { get; set; }

    /// <summary>
    /// Seconds since this meat patch was created. Freshness falls as meat ages.
    /// </summary>
    public float MeatAgeSeconds { get; set; }

    /// <summary>
    /// Creature credited with creating this meat patch while the fresh-kill window is active.
    /// </summary>
    public EntityId FreshKillAttackerId { get; set; }

    /// <summary>
    /// Creature whose injury death produced this meat patch.
    /// </summary>
    public EntityId FreshKillPreyId { get; set; }

    /// <summary>
    /// Remaining seconds during which the credited attacker treats this meat as fresh-kill prey.
    /// </summary>
    public float FreshKillSecondsRemaining { get; set; }
}
