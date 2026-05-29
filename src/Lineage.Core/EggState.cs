namespace Lineage.Core;

/// <summary>
/// Immobile offspring state between reproduction and hatching into a creature.
/// </summary>
public struct EggState
{
    public EntityId Id { get; set; }

    public EntityId ParentId { get; set; }

    public SimVector2 Position { get; set; }

    public float AgeSeconds { get; set; }

    public float IncubationSeconds { get; set; }

    public float Energy { get; set; }

    public float Health { get; set; }

    public float MaxHealth { get; set; }

    public EggDeathReason PendingDeathReason { get; set; }

    public float InvestmentRatio { get; set; }

    public int Generation { get; set; }

    public int GenomeId { get; set; }

    public int BrainId { get; set; }

    public float BirthMutationStrength { get; set; }

    public float BirthTraitMutationRate { get; set; }

    public float BirthBrainMutationRate { get; set; }
}
