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
            creature.LastMeatDigestedEnergy = 0f;
            creature.LastRottenMeatDamage = 0f;
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

            creature.GutPlantCalories -= plantDigested;
            creature.GutMeatCalories -= meatDigested;
            creature.GutMeatQualityCalories = Math.Max(0f, creature.GutMeatQualityCalories - meatDigested * meatQuality);

            var plantReleasedEnergy = plantDigested * CreatureDigestion.PlantEfficiency(genome);
            var meatReleasedEnergy = meatDigested * CreatureDigestion.MeatEnergyEfficiency(genome, meatQuality);
            var releasedEnergy = plantReleasedEnergy + meatReleasedEnergy;
            var rottenMeatDamage = CalculateRottenMeatDamage(genome, meatQuality, meatDigested);
            creature.Energy += releasedEnergy;
            creature.Health = Math.Max(0f, creature.Health - rottenMeatDamage);
            creature.LastCaloriesDigested = releasedEnergy;
            creature.LastPlantDigestedEnergy = plantReleasedEnergy;
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
        creature.GutMeatCalories *= scale;
        creature.GutMeatQualityCalories *= scale;
    }

    private static float ValidateNonNegative(float value, string name)
    {
        return float.IsFinite(value) && value >= 0f
            ? value
            : throw new ArgumentOutOfRangeException(name, "Rotten meat damage must be finite and non-negative.");
    }
}
