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

    public int Generation { get; set; }

    public int GenomeId { get; set; }

    public int BrainId { get; set; }

    public float BirthEnergy { get; set; }

    public float MaxXReached { get; set; }

    public long? DeathTick { get; set; }

    public double? DeathElapsedSeconds { get; set; }

    public CreatureDeathReason? DeathReason { get; set; }

    public bool IsFounder => ParentId == default;

    public bool IsAlive => DeathTick is null;
}
