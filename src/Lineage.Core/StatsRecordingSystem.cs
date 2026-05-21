namespace Lineage.Core;

/// <summary>
/// Captures aggregate simulation metrics after a tick's behavioral systems run.
/// </summary>
public sealed class StatsRecordingSystem(int sampleIntervalTicks = 1) : ISimulationSystem
{
    private readonly int _sampleIntervalTicks = sampleIntervalTicks > 0
        ? sampleIntervalTicks
        : throw new ArgumentOutOfRangeException(nameof(sampleIntervalTicks), "Stats sample interval must be positive.");

    public void Update(WorldState state, float deltaSeconds)
    {
        if (state.Tick % _sampleIntervalTicks != 0)
        {
            return;
        }

        var totalCreatureEnergy = 0f;
        var totalVisibleFoodDensity = 0f;
        var totalVisiblePlantDensity = 0f;
        var totalVisibleMeatDensity = 0f;
        var totalVisiblePreyDensity = 0f;
        var totalCaloriesEaten = 0f;
        var totalAttackDamage = 0f;
        var totalSecondsSinceLastMeal = 0f;
        var totalBirthInvestmentRatio = 0f;
        var totalVisionRange = 0f;
        var totalVisionAngle = 0f;
        var foodDetectedCreatureCount = 0;
        var plantDetectedCreatureCount = 0;
        var meatDetectedCreatureCount = 0;
        var foodContactCreatureCount = 0;
        var eatingCreatureCount = 0;
        var attackingCreatureCount = 0;
        var maxGeneration = 0;

        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            totalCreatureEnergy += creature.Energy;
            totalVisibleFoodDensity += creature.Senses.VisibleFoodDensity;
            totalVisiblePlantDensity += creature.Senses.VisiblePlantDensity;
            totalVisibleMeatDensity += creature.Senses.VisibleMeatDensity;
            totalVisiblePreyDensity += creature.Senses.VisiblePreyDensity;
            totalCaloriesEaten += creature.LastCaloriesEaten;
            totalAttackDamage += creature.LastAttackDamageDealt;
            totalSecondsSinceLastMeal += creature.SecondsSinceLastMeal;
            totalBirthInvestmentRatio += OffspringDevelopment.NormalizeInvestmentRatio(creature.BirthInvestmentRatio);
            totalVisionRange += CreatureGrowth.EffectiveSenseRadius(creature, genome);
            totalVisionAngle += CreatureGrowth.EffectiveVisionAngleRadians(creature, genome);
            maxGeneration = Math.Max(maxGeneration, creature.Generation);

            if (creature.Senses.FoodDetected)
            {
                foodDetectedCreatureCount++;
            }

            if (creature.Senses.PlantDetected)
            {
                plantDetectedCreatureCount++;
            }

            if (creature.Senses.MeatDetected)
            {
                meatDetectedCreatureCount++;
            }

            if (creature.IsTouchingFood)
            {
                foodContactCreatureCount++;
            }

            if (creature.LastCaloriesEaten > 0f)
            {
                eatingCreatureCount++;
            }

            if (creature.LastAttackDamageDealt > 0f)
            {
                attackingCreatureCount++;
            }
        }

        var totalResourceCalories = 0f;
        var totalPlantCalories = 0f;
        var totalMeatCalories = 0f;
        var plantResourceCount = 0;
        var meatResourceCount = 0;
        for (var i = 0; i < state.Resources.Count; i++)
        {
            var resource = state.Resources[i];
            totalResourceCalories += resource.Calories;
            if (resource.Kind == ResourceKind.Meat)
            {
                meatResourceCount++;
                totalMeatCalories += resource.Calories;
            }
            else
            {
                plantResourceCount++;
                totalPlantCalories += resource.Calories;
            }
        }

        var totalEggEnergy = 0f;
        var totalEggHealth = 0f;
        var totalEggHealthRatio = 0f;
        for (var i = 0; i < state.Eggs.Count; i++)
        {
            var egg = state.Eggs[i];
            totalEggEnergy += egg.Energy;
            totalEggHealth += egg.Health;
            totalEggHealthRatio += egg.MaxHealth > 0f
                ? Math.Clamp(egg.Health / egg.MaxHealth, 0f, 1f)
                : 1f;
        }

        var creatureCount = state.Creatures.Count;
        var divisor = Math.Max(1, creatureCount);
        var caloriesEatenPerSecond = deltaSeconds > 0f
            ? totalCaloriesEaten / deltaSeconds
            : 0f;
        var attackDamagePerSecond = deltaSeconds > 0f
            ? totalAttackDamage / deltaSeconds
            : 0f;

        state.Stats.RecordSnapshot(new SimulationStatsSnapshot(
            state.Tick,
            state.ElapsedSeconds,
            creatureCount,
            state.Eggs.Count,
            state.Resources.Count,
            plantResourceCount,
            meatResourceCount,
            state.Genomes.Count,
            state.Brains.Count,
            maxGeneration,
            totalCreatureEnergy,
            totalEggEnergy,
            totalEggHealth,
            totalResourceCalories,
            totalPlantCalories,
            totalMeatCalories,
            foodDetectedCreatureCount,
            plantDetectedCreatureCount,
            meatDetectedCreatureCount,
            foodContactCreatureCount,
            eatingCreatureCount,
            totalVisibleFoodDensity / divisor,
            totalVisiblePlantDensity / divisor,
            totalVisibleMeatDensity / divisor,
            totalVisiblePreyDensity / divisor,
            caloriesEatenPerSecond,
            attackingCreatureCount,
            attackDamagePerSecond,
            totalSecondsSinceLastMeal / divisor,
            totalBirthInvestmentRatio / divisor,
            totalEggHealthRatio / Math.Max(1, state.Eggs.Count),
            totalVisionRange / divisor,
            totalVisionAngle / divisor,
            state.Stats.CreatureBirthCount,
            state.Stats.EggLaidCount,
            state.Stats.EggHatchedCount,
            state.Stats.EggDeathCount,
            state.Stats.EggPredationDeathCount,
            state.Stats.CreatureDeathCount,
            state.Stats.StarvationDeathCount,
            state.Stats.InjuryDeathCount));
    }
}
