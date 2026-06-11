namespace Lineage.Core;

/// <summary>
/// Ephemeral per-creature energy debit breakdown for the most recent simulation tick.
/// </summary>
public struct CreatureEnergyLedger
{
    public float BasalCalories { get; set; }

    public float BodyUpkeepCalories { get; set; }

    public float SpeedUpkeepCalories { get; set; }

    public float TurnUpkeepCalories { get; set; }

    public float SenseUpkeepCalories { get; set; }

    public float VisionUpkeepCalories { get; set; }

    public float EatRateUpkeepCalories { get; set; }

    public float GutCapacityUpkeepCalories { get; set; }

    public float DigestionUpkeepCalories { get; set; }

    public float BiteStrengthUpkeepCalories { get; set; }

    public float DamageResistanceUpkeepCalories { get; set; }

    public float PlantSpecializationUpkeepCalories { get; set; }

    public float MemoryUpkeepCalories { get; set; }

    public float BrainUpkeepCalories { get; set; }

    public float MovementCalories { get; set; }

    public float AttackCalories { get; set; }

    public float ReproductionCalories { get; set; }

    public float HealingCalories { get; set; }

    public float TraitUpkeepCalories()
    {
        return BodyUpkeepCalories
            + SpeedUpkeepCalories
            + TurnUpkeepCalories
            + SenseUpkeepCalories
            + VisionUpkeepCalories
            + EatRateUpkeepCalories
            + GutCapacityUpkeepCalories
            + DigestionUpkeepCalories
            + BiteStrengthUpkeepCalories
            + DamageResistanceUpkeepCalories
            + PlantSpecializationUpkeepCalories
            + MemoryUpkeepCalories
            + BrainUpkeepCalories;
    }

    public float TotalCostCalories()
    {
        return BasalCalories
            + TraitUpkeepCalories()
            + MovementCalories
            + AttackCalories
            + ReproductionCalories
            + HealingCalories;
    }

    public bool HasAnyCost()
    {
        return TotalCostCalories() > 0f;
    }

    public CreatureEnergyLedger Normalized()
    {
        return new CreatureEnergyLedger
        {
            BasalCalories = NonNegative(BasalCalories),
            BodyUpkeepCalories = NonNegative(BodyUpkeepCalories),
            SpeedUpkeepCalories = NonNegative(SpeedUpkeepCalories),
            TurnUpkeepCalories = NonNegative(TurnUpkeepCalories),
            SenseUpkeepCalories = NonNegative(SenseUpkeepCalories),
            VisionUpkeepCalories = NonNegative(VisionUpkeepCalories),
            EatRateUpkeepCalories = NonNegative(EatRateUpkeepCalories),
            GutCapacityUpkeepCalories = NonNegative(GutCapacityUpkeepCalories),
            DigestionUpkeepCalories = NonNegative(DigestionUpkeepCalories),
            BiteStrengthUpkeepCalories = NonNegative(BiteStrengthUpkeepCalories),
            DamageResistanceUpkeepCalories = NonNegative(DamageResistanceUpkeepCalories),
            PlantSpecializationUpkeepCalories = NonNegative(PlantSpecializationUpkeepCalories),
            MemoryUpkeepCalories = NonNegative(MemoryUpkeepCalories),
            BrainUpkeepCalories = NonNegative(BrainUpkeepCalories),
            MovementCalories = NonNegative(MovementCalories),
            AttackCalories = NonNegative(AttackCalories),
            ReproductionCalories = NonNegative(ReproductionCalories),
            HealingCalories = NonNegative(HealingCalories)
        };
    }

    private static float NonNegative(float value)
    {
        return float.IsFinite(value) && value > 0f
            ? value
            : 0f;
    }
}
