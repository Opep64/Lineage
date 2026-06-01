namespace Lineage.Core;

/// <summary>
/// Birth/death record for a creature that has existed in the simulation.
/// </summary>
///
/// <remarks>
/// This is the first version of lineage tracking. It intentionally records stable
/// IDs and coarse facts, not references into mutable creature arrays.
/// </remarks>
public struct CreatureLineageRecord
{
    public EntityId Id { get; set; }

    public EntityId ParentId { get; set; }

    public long BirthTick { get; set; }

    public double BirthElapsedSeconds { get; set; }

    public SimVector2 BirthPosition { get; set; }

    public int Generation { get; set; }

    public int GenomeId { get; set; }

    public int BrainId { get; set; }

    public float BirthEnergy { get; set; }

    public float BirthMutationStrength { get; set; }

    public float BirthTraitMutationRate { get; set; }

    public float BirthBrainMutationRate { get; set; }

    public float MaxXReached { get; set; }

    public long? DeathTick { get; set; }

    public double? DeathElapsedSeconds { get; set; }

    public SimVector2? DeathPosition { get; set; }

    public CreatureDeathReason? DeathReason { get; set; }

    public EntityId DeathAttackerId { get; set; }

    public float TelemetryLivingSeconds { get; set; }

    public float TelemetryEatingSeconds { get; set; }

    public float TelemetryMeatEatingSeconds { get; set; }

    public float TelemetryFoodContactSeconds { get; set; }

    public float TelemetryCreatureContactSeconds { get; set; }

    public float TelemetrySimilarCreatureContactSeconds { get; set; }

    public float TelemetryAttackIntentSeconds { get; set; }

    public float TelemetryAttackIntentTouchingSeconds { get; set; }

    public float TelemetryAttackDamageDealingSeconds { get; set; }

    public float TelemetryMeatDetectedSeconds { get; set; }

    public float TelemetryFreshMeatDetectedSeconds { get; set; }

    public float TelemetryStaleMeatDetectedSeconds { get; set; }

    public float TelemetryRottenMeatScentDetectedSeconds { get; set; }

    public float TelemetryCaloriesEaten { get; set; }

    public float TelemetryPlantCaloriesEaten { get; set; }

    public float TelemetryCarcassCaloriesEaten { get; set; }

    public float TelemetryEggCaloriesEaten { get; set; }

    public float TelemetryFreshKillCaloriesEaten { get; set; }

    public float TelemetryFreshMeatCaloriesEaten { get; set; }

    public float TelemetryStaleMeatCaloriesEaten { get; set; }

    public float TelemetryRottenMeatDamage { get; set; }

    public float TelemetryAttackDamageDealt { get; set; }

    public float TelemetryAttackDamageTaken { get; set; }

    public bool IsFounder => ParentId == default;

    public bool IsAlive => DeathTick is null;
}
