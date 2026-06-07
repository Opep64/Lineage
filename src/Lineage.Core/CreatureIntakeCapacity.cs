namespace Lineage.Core;

/// <summary>
/// Shared raw intake limits for systems that need to explain why food contact
/// did or did not turn into calories.
/// </summary>
public static class CreatureIntakeCapacity
{
    public static float AvailableGutCapacity(CreatureState creature, CreatureGenome genome)
    {
        var capacity = CreatureGrowth.EffectiveGutCapacityCalories(creature, genome);
        return Math.Max(0f, capacity - creature.GutPlantCalories - creature.GutMeatCalories);
    }

    public static float AvailableRawIntakeCapacity(
        CreatureState creature,
        CreatureGenome genome,
        float sourceEnergyEfficiency)
    {
        var gutCapacity = AvailableGutCapacity(creature, genome);
        if (gutCapacity <= 0f)
        {
            return 0f;
        }

        var sourceEfficiency = Math.Max(0.0001f, sourceEnergyEfficiency);
        var usableRoom = AvailableUsableEnergyCapacity(creature, genome) - PendingGutEnergy(creature, genome);
        if (usableRoom <= 0f)
        {
            return 0f;
        }

        return Math.Min(gutCapacity, usableRoom / sourceEfficiency);
    }

    public static float AvailableUsableEnergyCapacity(CreatureState creature, CreatureGenome genome)
    {
        var energyRoom = Math.Max(0f, CreatureGrowth.EffectiveEnergyCapacityCalories(creature, genome) - creature.Energy);
        var fatCapacity = CreatureGrowth.EffectiveFatStorageCapacityCalories(creature, genome);
        if (fatCapacity <= 0f)
        {
            return energyRoom;
        }

        var clampedFat = Math.Clamp(creature.FatCalories, 0f, fatCapacity);
        var fatRoom = Math.Max(0f, fatCapacity - clampedFat);
        var fatEfficiency = Math.Clamp(genome.FatStorageEfficiency, 0.05f, 1f);
        return energyRoom + fatRoom / fatEfficiency;
    }

    public static float PendingGutEnergy(CreatureState creature, CreatureGenome genome)
    {
        var plantCalories = Math.Max(0f, creature.GutPlantCalories);
        var tenderPlantCalories = Math.Clamp(creature.GutTenderPlantCalories, 0f, plantCalories);
        var richPlantCalories = Math.Clamp(creature.GutRichPlantCalories, 0f, plantCalories);
        var toughPlantCalories = Math.Clamp(creature.GutToughPlantCalories, 0f, plantCalories);
        var typedPlantCalories = tenderPlantCalories + richPlantCalories + toughPlantCalories;
        if (typedPlantCalories > plantCalories && typedPlantCalories > 0f)
        {
            var scale = plantCalories / typedPlantCalories;
            tenderPlantCalories *= scale;
            richPlantCalories *= scale;
            toughPlantCalories *= scale;
            typedPlantCalories = plantCalories;
        }

        var genericPlantCalories = Math.Max(0f, plantCalories - typedPlantCalories);
        var plantEnergy = genericPlantCalories * CreatureDigestion.PlantTypeEnergyEfficiency(genome, PlantResourceKind.Generic)
            + tenderPlantCalories * CreatureDigestion.PlantTypeEnergyEfficiency(genome, PlantResourceKind.Tender)
            + richPlantCalories * CreatureDigestion.PlantTypeEnergyEfficiency(genome, PlantResourceKind.Rich)
            + toughPlantCalories * CreatureDigestion.PlantTypeEnergyEfficiency(genome, PlantResourceKind.Tough);

        var meatCalories = Math.Max(0f, creature.GutMeatCalories);
        var meatFreshness = meatCalories > 0f
            ? Math.Clamp(creature.GutMeatQualityCalories / meatCalories, MeatQuality.MinimumFreshness, 1f)
            : 1f;
        var meatEnergy = meatCalories * CreatureDigestion.MeatEnergyEfficiency(genome, meatFreshness);

        return plantEnergy + meatEnergy;
    }
}
