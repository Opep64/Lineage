namespace Lineage.Core;

/// <summary>
/// Captures aggregate simulation metrics after a tick's behavioral systems run.
/// </summary>
public sealed class StatsRecordingSystem(
    int sampleIntervalTicks = 1,
    BiomePressureProfile? biomeMovementCostProfile = null,
    BiomePressureProfile? biomeBasalCostProfile = null,
    BiomePressureProfile? biomeSpeedProfile = null) : ISimulationSystem
{
    private readonly int _sampleIntervalTicks = sampleIntervalTicks > 0
        ? sampleIntervalTicks
        : throw new ArgumentOutOfRangeException(nameof(sampleIntervalTicks), "Stats sample interval must be positive.");
    private readonly BiomePressureProfile _biomeMovementCostProfile =
        BiomePressureProfile.Validate(biomeMovementCostProfile ?? BiomePressureProfile.Neutral, nameof(biomeMovementCostProfile));
    private readonly BiomePressureProfile _biomeBasalCostProfile =
        BiomePressureProfile.Validate(biomeBasalCostProfile ?? BiomePressureProfile.Neutral, nameof(biomeBasalCostProfile));
    private readonly BiomePressureProfile _biomeSpeedProfile =
        BiomePressureProfile.Validate(biomeSpeedProfile ?? BiomePressureProfile.Neutral, nameof(biomeSpeedProfile));

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
        var totalMeatScentDensity = 0f;
        var totalVisibleCreatureDensity = 0f;
        var totalCaloriesEaten = 0f;
        var totalPlantCaloriesEaten = 0f;
        var totalCarcassCaloriesEaten = 0f;
        var totalEggCaloriesEaten = 0f;
        var totalLivePreyCaloriesEaten = 0f;
        var totalFreshMeatCaloriesEaten = 0f;
        var totalStaleMeatCaloriesEaten = 0f;
        var totalCaloriesDigested = 0f;
        var totalPlantDigestedEnergy = 0f;
        var totalMeatDigestedEnergy = 0f;
        var totalGutFillRatio = 0f;
        var totalGutPlantShare = 0f;
        var totalGutMeatShare = 0f;
        var totalAttackDamage = 0f;
        var totalSecondsSinceLastMeal = 0f;
        var totalDistanceTraveled = 0f;
        var totalDistanceSinceLastMeal = 0f;
        var totalBirthInvestmentRatio = 0f;
        var totalEggReserveRatio = 0f;
        var totalEnergySurplusRatio = 0f;
        var totalRecentFoodSuccess = 0f;
        var totalVisionRange = 0f;
        var totalVisionAngle = 0f;
        var totalDietaryAdaptation = 0f;
        var totalCarrionAdaptation = 0f;
        var totalBiteStrength = 0f;
        var totalDamageResistance = 0f;
        var totalBiomeMovementCostMultiplier = 0f;
        var totalBiomeBasalCostMultiplier = 0f;
        var totalBiomeSpeedMultiplier = 0f;
        var attackerTotalDietaryAdaptation = 0f;
        var attackerTotalBiteStrength = 0f;
        var attackerTotalDamageResistance = 0f;
        var nonAttackerTotalDietaryAdaptation = 0f;
        var nonAttackerTotalBiteStrength = 0f;
        var nonAttackerTotalDamageResistance = 0f;
        var foodDetectedCreatureCount = 0;
        var plantDetectedCreatureCount = 0;
        var meatDetectedCreatureCount = 0;
        var creatureDetectedCreatureCount = 0;
        var meatScentDetectedCreatureCount = 0;
        var foodContactCreatureCount = 0;
        var eatingCreatureCount = 0;
        var attackingCreatureCount = 0;
        var reproductionReadyCreatureCount = 0;
        var reproductionIntentCreatureCount = 0;
        var nonAttackingCreatureCount = 0;
        var barrenCreatureCount = 0;
        var sparseCreatureCount = 0;
        var grasslandCreatureCount = 0;
        var richCreatureCount = 0;
        var maxGeneration = 0;

        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            totalCreatureEnergy += creature.Energy;
            totalVisibleFoodDensity += creature.Senses.VisibleFoodDensity;
            totalVisiblePlantDensity += creature.Senses.VisiblePlantDensity;
            totalVisibleMeatDensity += creature.Senses.VisibleMeatDensity;
            totalMeatScentDensity += creature.Senses.MeatScentDensity;
            totalVisibleCreatureDensity += creature.Senses.VisibleCreatureDensity;
            totalCaloriesEaten += creature.LastCaloriesEaten;
            totalPlantCaloriesEaten += creature.LastPlantCaloriesEaten;
            totalCarcassCaloriesEaten += creature.LastCarcassCaloriesEaten;
            totalEggCaloriesEaten += creature.LastEggCaloriesEaten;
            totalLivePreyCaloriesEaten += creature.LastLivePreyCaloriesEaten;
            totalFreshMeatCaloriesEaten += creature.LastFreshMeatCaloriesEaten;
            totalStaleMeatCaloriesEaten += creature.LastStaleMeatCaloriesEaten;
            totalCaloriesDigested += creature.LastCaloriesDigested;
            totalPlantDigestedEnergy += creature.LastPlantDigestedEnergy;
            totalMeatDigestedEnergy += creature.LastMeatDigestedEnergy;
            var gutTotal = creature.GutPlantCalories + creature.GutMeatCalories;
            var gutCapacity = CreatureGrowth.EffectiveGutCapacityCalories(creature, genome);
            totalGutFillRatio += gutCapacity > 0f
                ? Math.Clamp(gutTotal / gutCapacity, 0f, 1f)
                : 0f;
            totalGutPlantShare += gutTotal > 0f
                ? creature.GutPlantCalories / gutTotal
                : 0f;
            totalGutMeatShare += gutTotal > 0f
                ? creature.GutMeatCalories / gutTotal
                : 0f;
            totalAttackDamage += creature.LastAttackDamageDealt;
            totalSecondsSinceLastMeal += creature.SecondsSinceLastMeal;
            totalDistanceTraveled += creature.LastDistanceTraveled;
            totalDistanceSinceLastMeal += creature.DistanceSinceLastMeal;
            totalBirthInvestmentRatio += OffspringDevelopment.NormalizeInvestmentRatio(creature.BirthInvestmentRatio);
            totalEggReserveRatio += creature.Senses.EggReserveRatio;
            totalEnergySurplusRatio += creature.Senses.EnergySurplusRatio;
            totalRecentFoodSuccess += creature.Senses.RecentFoodSuccess;
            totalVisionRange += CreatureGrowth.EffectiveSenseRadius(creature, genome);
            totalVisionAngle += CreatureGrowth.EffectiveVisionAngleRadians(creature, genome);
            totalDietaryAdaptation += genome.DietaryAdaptation;
            totalCarrionAdaptation += genome.CarrionAdaptation;
            totalBiteStrength += genome.BiteStrength;
            totalDamageResistance += genome.DamageResistance;
            var biome = state.Biomes.GetKindAt(creature.Position);
            totalBiomeMovementCostMultiplier += _biomeMovementCostProfile.For(biome);
            totalBiomeBasalCostMultiplier += _biomeBasalCostProfile.For(biome);
            totalBiomeSpeedMultiplier += _biomeSpeedProfile.For(biome);
            switch (biome)
            {
                case BiomeKind.Barren:
                    barrenCreatureCount++;
                    break;
                case BiomeKind.Sparse:
                    sparseCreatureCount++;
                    break;
                case BiomeKind.Rich:
                    richCreatureCount++;
                    break;
                default:
                    grasslandCreatureCount++;
                    break;
            }

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

            if (creature.Senses.CreatureDetected)
            {
                creatureDetectedCreatureCount++;
            }

            if (creature.Senses.MeatScentDetected)
            {
                meatScentDetectedCreatureCount++;
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
                attackerTotalDietaryAdaptation += genome.DietaryAdaptation;
                attackerTotalBiteStrength += genome.BiteStrength;
                attackerTotalDamageResistance += genome.DamageResistance;
            }
            else
            {
                nonAttackingCreatureCount++;
                nonAttackerTotalDietaryAdaptation += genome.DietaryAdaptation;
                nonAttackerTotalBiteStrength += genome.BiteStrength;
                nonAttackerTotalDamageResistance += genome.DamageResistance;
            }

            if (creature.Senses.ReproductionReadiness > 0.5f)
            {
                reproductionReadyCreatureCount++;
            }

            if (creature.Actions.WantsReproduce)
            {
                reproductionIntentCreatureCount++;
            }
        }

        var totalResourceCalories = 0f;
        var totalPlantCalories = 0f;
        var totalMeatCalories = 0f;
        var totalMeatFreshnessWeightedCalories = 0f;
        var activeResourceCount = 0;
        var plantResourceCount = 0;
        var meatResourceCount = 0;
        for (var i = 0; i < state.Resources.Count; i++)
        {
            var resource = state.Resources[i];
            if (resource.Calories <= 0f)
            {
                continue;
            }

            activeResourceCount++;
            totalResourceCalories += resource.Calories;
            if (resource.Kind == ResourceKind.Meat)
            {
                meatResourceCount++;
                totalMeatCalories += resource.Calories;
                totalMeatFreshnessWeightedCalories += resource.Calories * MeatQuality.Freshness(resource);
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
        var plantCaloriesEatenPerSecond = deltaSeconds > 0f
            ? totalPlantCaloriesEaten / deltaSeconds
            : 0f;
        var carcassCaloriesEatenPerSecond = deltaSeconds > 0f
            ? totalCarcassCaloriesEaten / deltaSeconds
            : 0f;
        var eggCaloriesEatenPerSecond = deltaSeconds > 0f
            ? totalEggCaloriesEaten / deltaSeconds
            : 0f;
        var livePreyCaloriesEatenPerSecond = deltaSeconds > 0f
            ? totalLivePreyCaloriesEaten / deltaSeconds
            : 0f;
        var freshMeatCaloriesEatenPerSecond = deltaSeconds > 0f
            ? totalFreshMeatCaloriesEaten / deltaSeconds
            : 0f;
        var staleMeatCaloriesEatenPerSecond = deltaSeconds > 0f
            ? totalStaleMeatCaloriesEaten / deltaSeconds
            : 0f;
        var caloriesDigestedPerSecond = deltaSeconds > 0f
            ? totalCaloriesDigested / deltaSeconds
            : 0f;
        var plantDigestedEnergyPerSecond = deltaSeconds > 0f
            ? totalPlantDigestedEnergy / deltaSeconds
            : 0f;
        var meatDigestedEnergyPerSecond = deltaSeconds > 0f
            ? totalMeatDigestedEnergy / deltaSeconds
            : 0f;
        var attackDamagePerSecond = deltaSeconds > 0f
            ? totalAttackDamage / deltaSeconds
            : 0f;
        var distanceTraveledPerSecond = deltaSeconds > 0f
            ? totalDistanceTraveled / deltaSeconds
            : 0f;
        var meatCaloriesEaten = totalCarcassCaloriesEaten + totalEggCaloriesEaten + totalLivePreyCaloriesEaten;
        var carcassMeatCaloriesEaten = totalFreshMeatCaloriesEaten + totalStaleMeatCaloriesEaten;
        var meatCaloriesEatenShare = totalCaloriesEaten > 0f
            ? meatCaloriesEaten / totalCaloriesEaten
            : 0f;
        var freshMeatCaloriesEatenShare = carcassMeatCaloriesEaten > 0f
            ? totalFreshMeatCaloriesEaten / carcassMeatCaloriesEaten
            : 0f;
        var staleMeatCaloriesEatenShare = carcassMeatCaloriesEaten > 0f
            ? totalStaleMeatCaloriesEaten / carcassMeatCaloriesEaten
            : 0f;
        var freshKillCaloriesEatenShare = totalCaloriesEaten > 0f
            ? totalLivePreyCaloriesEaten / totalCaloriesEaten
            : 0f;
        var averageMeatFreshness = totalMeatCalories > 0f
            ? totalMeatFreshnessWeightedCalories / totalMeatCalories
            : 0f;
        var meatDigestedEnergyShare = totalCaloriesDigested > 0f
            ? totalMeatDigestedEnergy / totalCaloriesDigested
            : 0f;
        var caloriesEatenPerDistance = totalDistanceTraveled > 0f
            ? totalCaloriesEaten / totalDistanceTraveled
            : 0f;
        var caloriesDigestedPerDistance = totalDistanceTraveled > 0f
            ? totalCaloriesDigested / totalDistanceTraveled
            : 0f;
        var caloriesEatenPerFoodVisionEvent = foodDetectedCreatureCount > 0
            ? totalCaloriesEaten / foodDetectedCreatureCount
            : 0f;
        var attackerDivisor = Math.Max(1, attackingCreatureCount);
        var nonAttackerDivisor = Math.Max(1, nonAttackingCreatureCount);
        state.Stats.RecordSnapshot(new SimulationStatsSnapshot(
            state.Tick,
            state.ElapsedSeconds,
            creatureCount,
            state.Eggs.Count,
            activeResourceCount,
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
            barrenCreatureCount,
            sparseCreatureCount,
            grasslandCreatureCount,
            richCreatureCount,
            totalBiomeMovementCostMultiplier / divisor,
            totalBiomeBasalCostMultiplier / divisor,
            totalBiomeSpeedMultiplier / divisor,
            foodDetectedCreatureCount,
            plantDetectedCreatureCount,
            meatDetectedCreatureCount,
            foodContactCreatureCount,
            eatingCreatureCount,
            totalVisibleFoodDensity / divisor,
            totalVisiblePlantDensity / divisor,
            totalVisibleMeatDensity / divisor,
            meatScentDetectedCreatureCount,
            totalMeatScentDensity / divisor,
            totalVisibleCreatureDensity / divisor,
            caloriesEatenPerSecond,
            plantCaloriesEatenPerSecond,
            carcassCaloriesEatenPerSecond,
            eggCaloriesEatenPerSecond,
            livePreyCaloriesEatenPerSecond,
            caloriesDigestedPerSecond,
            plantDigestedEnergyPerSecond,
            meatDigestedEnergyPerSecond,
            totalGutFillRatio / divisor,
            totalGutPlantShare / divisor,
            totalGutMeatShare / divisor,
            attackingCreatureCount,
            attackDamagePerSecond,
            totalSecondsSinceLastMeal / divisor,
            distanceTraveledPerSecond,
            totalDistanceSinceLastMeal / divisor,
            caloriesEatenPerDistance,
            caloriesDigestedPerDistance,
            caloriesEatenPerFoodVisionEvent,
            totalBirthInvestmentRatio / divisor,
            totalEggHealthRatio / Math.Max(1, state.Eggs.Count),
            totalVisionRange / divisor,
            totalVisionAngle / divisor,
            state.Stats.CreatureBirthCount,
            state.Stats.EggLaidCount,
            state.Stats.ReproductionAttemptCount,
            state.Stats.EggHatchedCount,
            state.Stats.EggDeathCount,
            state.Stats.EggPredationDeathCount,
            state.Stats.CreatureDeathCount,
            state.Stats.StarvationDeathCount,
            state.Stats.InjuryDeathCount,
            creatureDetectedCreatureCount,
            meatCaloriesEatenShare,
            freshKillCaloriesEatenShare,
            meatDigestedEnergyShare,
            totalDietaryAdaptation / divisor,
            totalCarrionAdaptation / divisor,
            totalBiteStrength / divisor,
            totalDamageResistance / divisor,
            attackerTotalDietaryAdaptation / attackerDivisor,
            attackerTotalBiteStrength / attackerDivisor,
            attackerTotalDamageResistance / attackerDivisor,
            nonAttackerTotalDietaryAdaptation / nonAttackerDivisor,
            nonAttackerTotalBiteStrength / nonAttackerDivisor,
            nonAttackerTotalDamageResistance / nonAttackerDivisor,
            averageMeatFreshness,
            freshMeatCaloriesEatenPerSecond,
            staleMeatCaloriesEatenPerSecond,
            freshMeatCaloriesEatenShare,
            staleMeatCaloriesEatenShare,
            state.Stats.AverageDeadCreatureLifespanSeconds,
            state.Stats.MedianDeadCreatureLifespanSeconds,
            reproductionReadyCreatureCount,
            reproductionIntentCreatureCount,
            totalEggReserveRatio / divisor,
            totalEnergySurplusRatio / divisor,
            totalRecentFoodSuccess / divisor));
    }
}
