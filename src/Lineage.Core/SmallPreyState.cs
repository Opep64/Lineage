namespace Lineage.Core;

/// <summary>
/// Lightweight non-evolving mobile meat prey.
/// </summary>
public struct SmallPreyState
{
    public EntityId Id { get; set; }

    public SimVector2 Position { get; set; }

    public SimVector2 Velocity { get; set; }

    public float HeadingRadians { get; set; }

    public float Radius { get; set; }

    public float Calories { get; set; }

    public float MaxCalories { get; set; }

    public float Health { get; set; }

    public float MaxHealth { get; set; }

    public float AgeSeconds { get; set; }

    public float WanderSecondsRemaining { get; set; }

    public EntityId HeldByCreatureId { get; set; }

    public float GrabPressure { get; set; }
}
