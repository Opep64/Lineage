namespace Lineage.Core;

/// <summary>
/// Converts stored gut contents into usable energy over time.
/// </summary>
///
/// <remarks>
/// Eating fills plant and meat gut stores with raw calories. This system releases
/// energy from those stores at the creature's heritable digestion rate. Inefficient
/// food still takes gut space, which creates an opportunity cost for eating the
/// wrong food when better food is available.
/// </remarks>
public sealed class DigestionSystem : ISimulationSystem
{
    private readonly float _rottenMeatDamagePerRawKcal;

    public DigestionSystem(float rottenMeatDamagePerRawKcal = 0f)
    {
        _rottenMeatDamagePerRawKcal = ValidateNonNegative(
            rottenMeatDamagePerRawKcal,
            nameof(rottenMeatDamagePerRawKcal));
    }

    public void Update(WorldState state, float deltaSeconds)
    {
        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);

            creature.LastCaloriesDigested = 0f;
            creature.LastPlantDigestedEnergy = 0f;
            creature.LastTenderPlantDigestedEnergy = 0f;
            creature.LastRichPlantDigestedEnergy = 0f;
            creature.LastToughPlantDigestedEnergy = 0f;
            creature.LastMeatDigestedEnergy = 0f;
            creature.LastRottenMeatDamage = 0f;
            NormalizePlantSubtypes(ref creature);
            NormalizeMeatQuality(ref creature);
            ClampGutToCapacity(ref creature, genome);

            var totalGutCalories = creature.GutPlantCalories + creature.GutMeatCalories;
            var digestionBudget = CreatureGrowth.EffectiveDigestionCaloriesPerSecond(creature, genome) * deltaSeconds;
            var rawDigested = Math.Min(totalGutCalories, digestionBudget);
            if (rawDigested <= 0f || totalGutCalories <= 0f)
            {
                state.Creatures[i] = creature;
                continue;
            }

            var plantShare = creature.GutPlantCalories / totalGutCalories;
            var plantDigested = Math.Min(creature.GutPlantCalories, rawDigested * plantShare);
            var meatDigested = Math.Min(creature.GutMeatCalories, rawDigested - plantDigested);

            // Floating-point rounding can leave a little unused budget. Let the
            // other compartment consume it if possible.
            var remainingBudget = rawDigested - plantDigested - meatDigested;
            if (remainingBudget > 0f)
            {
                if (creature.GutPlantCalories > plantDigested)
                {
                    var extraPlant = Math.Min(creature.GutPlantCalories - plantDigested, remainingBudget);
                    plantDigested += extraPlant;
                    remainingBudget -= extraPlant;
                }

                if (remainingBudget > 0f && creature.GutMeatCalories > meatDigested)
                {
                    meatDigested += Math.Min(creature.GutMeatCalories - meatDigested, remainingBudget);
                }
            }

            var meatQuality = creature.GutMeatCalories > 0f
                ? Math.Clamp(creature.GutMeatQualityCalories / creature.GutMeatCalories, MeatQuality.MinimumFreshness, 1f)
                : 1f;

            var plantDigestion = DigestPlantCalories(ref creature, genome, plantDigested);
            creature.GutMeatCalories -= meatDigested;
            creature.GutMeatQualityCalories = Math.Max(0f, creature.GutMeatQualityCalories - meatDigested * meatQuality);

            var plantReleasedEnergy = plantDigestion.TotalEnergy;
            var meatReleasedEnergy = meatDigested * CreatureDigestion.MeatEnergyEfficiency(genome, meatQuality);
            var releasedEnergy = plantReleasedEnergy + meatReleasedEnergy;
            var rottenMeatDamage = CalculateRottenMeatDamage(genome, meatQuality, meatDigested);
            creature.Energy += releasedEnergy;
            creature.Health = Math.Max(0f, creature.Health - rottenMeatDamage);
            creature.LastCaloriesDigested = releasedEnergy;
            creature.LastPlantDigestedEnergy = plantReleasedEnergy;
            creature.LastTenderPlantDigestedEnergy = plantDigestion.TenderEnergy;
            creature.LastRichPlantDigestedEnergy = plantDigestion.RichEnergy;
            creature.LastToughPlantDigestedEnergy = plantDigestion.ToughEnergy;
            creature.LastMeatDigestedEnergy = meatReleasedEnergy;
            creature.LastRottenMeatDamage = rottenMeatDamage;

            state.Creatures[i] = creature;
        }
    }

    private float CalculateRottenMeatDamage(CreatureGenome genome, float meatQuality, float meatDigested)
    {
        if (_rottenMeatDamagePerRawKcal <= 0f || meatDigested <= 0f)
        {
            return 0f;
        }

        return meatDigested
            * _rottenMeatDamagePerRawKcal
            * CreatureDigestion.RottenMeatDamageMultiplier(genome, meatQuality);
    }

    private static void NormalizeMeatQuality(ref CreatureState creature)
    {
        if (creature.GutMeatCalories <= 0f)
        {
            creature.GutMeatQualityCalories = 0f;
            return;
        }

        if (!float.IsFinite(creature.GutMeatQualityCalories) || creature.GutMeatQualityCalories <= 0f)
        {
            creature.GutMeatQualityCalories = creature.GutMeatCalories;
            return;
        }

        creature.GutMeatQualityCalories = Math.Clamp(
            creature.GutMeatQualityCalories,
            creature.GutMeatCalories * MeatQuality.MinimumFreshness,
            creature.GutMeatCalories);
    }

    private static void NormalizePlantSubtypes(ref CreatureState creature)
    {
        if (creature.GutPlantCalories <= 0f)
        {
            creature.GutPlantCalories = 0f;
            creature.GutTenderPlantCalories = 0f;
            creature.GutRichPlantCalories = 0f;
            creature.GutToughPlantCalories = 0f;
            return;
        }

        creature.GutTenderPlantCalories = Math.Clamp(creature.GutTenderPlantCalories, 0f, creature.GutPlantCalories);
        creature.GutRichPlantCalories = Math.Clamp(creature.GutRichPlantCalories, 0f, creature.GutPlantCalories);
        creature.GutToughPlantCalories = Math.Clamp(creature.GutToughPlantCalories, 0f, creature.GutPlantCalories);

        var subtypeTotal = creature.GutTenderPlantCalories
            + creature.GutRichPlantCalories
            + creature.GutToughPlantCalories;
        if (subtypeTotal <= creature.GutPlantCalories || subtypeTotal <= 0f)
        {
            return;
        }

        var scale = creature.GutPlantCalories / subtypeTotal;
        creature.GutTenderPlantCalories *= scale;
        creature.GutRichPlantCalories *= scale;
        creature.GutToughPlantCalories *= scale;
    }

    private static PlantDigestionResult DigestPlantCalories(
        ref CreatureState creature,
        CreatureGenome genome,
        float plantDigested)
    {
        if (plantDigested <= 0f || creature.GutPlantCalories <= 0f)
        {
            return default;
        }

        NormalizePlantSubtypes(ref creature);

        var totalPlant = creature.GutPlantCalories;
        var tender = creature.GutTenderPlantCalories;
        var rich = creature.GutRichPlantCalories;
        var tough = creature.GutToughPlantCalories;
        var generic = Math.Max(0f, totalPlant - tender - rich - tough);
        var digestAmount = Math.Min(plantDigested, totalPlant);

        var genericDigested = ConsumeShare(generic, totalPlant, digestAmount);
        var tenderDigested = ConsumeShare(tender, totalPlant, digestAmount);
        var richDigested = ConsumeShare(rich, totalPlant, digestAmount);
        var toughDigested = ConsumeShare(tough, totalPlant, digestAmount);
        var consumed = genericDigested + tenderDigested + richDigested + toughDigested;
        var roundingLeft = digestAmount - consumed;
        if (roundingLeft > 0f)
        {
            var extraGeneric = Math.Min(generic - genericDigested, roundingLeft);
            genericDigested += extraGeneric;
            roundingLeft -= extraGeneric;
        }

        if (roundingLeft > 0f)
        {
            var extraTender = Math.Min(tender - tenderDigested, roundingLeft);
            tenderDigested += extraTender;
            roundingLeft -= extraTender;
        }

        if (roundingLeft > 0f)
        {
            var extraRich = Math.Min(rich - richDigested, roundingLeft);
            richDigested += extraRich;
            roundingLeft -= extraRich;
        }

        if (roundingLeft > 0f)
        {
            toughDigested += Math.Min(tough - toughDigested, roundingLeft);
        }

        var finalConsumed = genericDigested + tenderDigested + richDigested + toughDigested;
        creature.GutPlantCalories = Math.Max(0f, creature.GutPlantCalories - finalConsumed);
        creature.GutTenderPlantCalories = Math.Max(0f, creature.GutTenderPlantCalories - tenderDigested);
        creature.GutRichPlantCalories = Math.Max(0f, creature.GutRichPlantCalories - richDigested);
        creature.GutToughPlantCalories = Math.Max(0f, creature.GutToughPlantCalories - toughDigested);

        var plantEfficiency = CreatureDigestion.PlantEfficiency(genome);
        var genericEnergy = genericDigested * plantEfficiency;
        var tenderEnergy = tenderDigested
            * plantEfficiency
            * PlantResourceTraits.DigestionEnergyMultiplier(PlantResourceKind.Tender);
        var richEnergy = richDigested
            * plantEfficiency
            * PlantResourceTraits.DigestionEnergyMultiplier(PlantResourceKind.Rich);
        var toughEnergy = toughDigested
            * plantEfficiency
            * PlantResourceTraits.DigestionEnergyMultiplier(PlantResourceKind.Tough);

        return new PlantDigestionResult(
            genericEnergy + tenderEnergy + richEnergy + toughEnergy,
            tenderEnergy,
            richEnergy,
            toughEnergy);
    }

    private static float ConsumeShare(float sourceAmount, float totalAmount, float digestAmount)
    {
        return sourceAmount > 0f && totalAmount > 0f
            ? Math.Min(sourceAmount, digestAmount * sourceAmount / totalAmount)
            : 0f;
    }

    private static void ClampGutToCapacity(ref CreatureState creature, CreatureGenome genome)
    {
        var capacity = CreatureGrowth.EffectiveGutCapacityCalories(creature, genome);
        var totalGutCalories = creature.GutPlantCalories + creature.GutMeatCalories;
        if (totalGutCalories <= capacity || totalGutCalories <= 0f)
        {
            return;
        }

        var scale = Math.Max(0f, capacity) / totalGutCalories;
        creature.GutPlantCalories *= scale;
        creature.GutTenderPlantCalories *= scale;
        creature.GutRichPlantCalories *= scale;
        creature.GutToughPlantCalories *= scale;
        creature.GutMeatCalories *= scale;
        creature.GutMeatQualityCalories *= scale;
    }

    private static float ValidateNonNegative(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Rotten meat damage must be finite and non-negative.");
    }

    private readonly record struct PlantDigestionResult(
        float TotalEnergy,
        float TenderEnergy,
        float RichEnergy,
        float ToughEnergy);
}
