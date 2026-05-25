namespace Lineage.Core;

/// <summary>
/// Captures aggregate simulation metrics after a tick's behavioral systems run.
/// </summary>
public sealed class StatsRecordingSystem(
    int sampleIntervalTicks = 1,
    BiomePressureProfile? biomeMovementCostProfile = null,
    BiomePressureProfile? biomeBasalCostProfile = null,
    BiomePressureProfile? biomeSpeedProfile = null,
    bool enableSeasons = false,
    float seasonLengthSeconds = 900f,
    float seasonFertilityAmplitude = 0.3f,
    float seasonPhaseOffsetSeconds = 0f,
    SeasonPhaseMode seasonPhaseMode = SeasonPhaseMode.Global) : ISimulationSystem
{
    private const float ActiveHiddenOutputWeightThreshold = 0.05f;
    private const float RawAttackPositiveThreshold = 0f;
    private const int PlantPatchinessGridAxisCells = 10;
    private const int PlantPatchinessGridCellCount = PlantPatchinessGridAxisCells * PlantPatchinessGridAxisCells;

    private readonly int _sampleIntervalTicks = sampleIntervalTicks > 0
        ? sampleIntervalTicks
        : throw new ArgumentOutOfRangeException(nameof(sampleIntervalTicks), "Stats sample interval must be positive.");
    private readonly BiomePressureProfile _biomeMovementCostProfile =
        BiomePressureProfile.Validate(biomeMovementCostProfile ?? BiomePressureProfile.Neutral, nameof(biomeMovementCostProfile));
    private readonly BiomePressureProfile _biomeBasalCostProfile =
        BiomePressureProfile.Validate(biomeBasalCostProfile ?? BiomePressureProfile.Neutral, nameof(biomeBasalCostProfile));
    private readonly BiomePressureProfile _biomeSpeedProfile =
        BiomePressureProfile.Validate(biomeSpeedProfile ?? BiomePressureProfile.Neutral, nameof(biomeSpeedProfile));
    private readonly bool _enableSeasons = enableSeasons;
    private readonly float _seasonLengthSeconds = EnsurePositive(seasonLengthSeconds, nameof(seasonLengthSeconds));
    private readonly float _seasonFertilityAmplitude = EnsureRange(seasonFertilityAmplitude, 0f, 0.95f, nameof(seasonFertilityAmplitude));
    private readonly float _seasonPhaseOffsetSeconds = EnsureFinite(seasonPhaseOffsetSeconds, nameof(seasonPhaseOffsetSeconds));
    private readonly SeasonPhaseMode _seasonPhaseMode = seasonPhaseMode;

    public void Update(WorldState state, float deltaSeconds)
    {
        if (state.Tick % _sampleIntervalTicks != 0)
        {
            return;
        }

        var totalCreatureEnergy = 0f;
        var totalCreatureX = 0f;
        var maxCreatureX = 0f;
        var totalMaxCreatureXReached = 0f;
        var maxCreatureXReached = 0f;
        var totalVisibleFoodDensity = 0f;
        var totalVisiblePlantDensity = 0f;
        var totalVisibleMeatDensity = 0f;
        var totalVisibleMeatFreshness = 0f;
        var totalMeatScentDensity = 0f;
        var totalRottenMeatScentDensity = 0f;
        var totalVisibleCreatureDensity = 0f;
        var totalCaloriesEaten = 0f;
        var totalPlantCaloriesEaten = 0f;
        var totalTenderPlantCaloriesEaten = 0f;
        var totalRichPlantCaloriesEaten = 0f;
        var totalToughPlantCaloriesEaten = 0f;
        var totalCarcassCaloriesEaten = 0f;
        var totalEggCaloriesEaten = 0f;
        var totalLivePreyCaloriesEaten = 0f;
        var totalFreshMeatCaloriesEaten = 0f;
        var totalStaleMeatCaloriesEaten = 0f;
        var totalCaloriesDigested = 0f;
        var totalPlantDigestedEnergy = 0f;
        var totalTenderPlantDigestedEnergy = 0f;
        var totalRichPlantDigestedEnergy = 0f;
        var totalToughPlantDigestedEnergy = 0f;
        var totalMeatDigestedEnergy = 0f;
        var totalRottenMeatDamage = 0f;
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
        var totalRecentFoodEnergyYield = 0f;
        var totalMemoryStrength = 0f;
        var memoryUserCaloriesEaten = 0f;
        var nonMemoryUserCaloriesEaten = 0f;
        var memoryUserDistanceTraveled = 0f;
        var nonMemoryUserDistanceTraveled = 0f;
        var memoryUserSecondsSinceLastMeal = 0f;
        var nonMemoryUserSecondsSinceLastMeal = 0f;
        var memoryUserDistanceSinceLastMeal = 0f;
        var nonMemoryUserDistanceSinceLastMeal = 0f;
        var memoryUserRecentFoodSuccess = 0f;
        var nonMemoryUserRecentFoodSuccess = 0f;
        var memoryUserGenerationTotal = 0;
        var nonMemoryUserGenerationTotal = 0;
        var memoryUserMaxXReachedTotal = 0f;
        var nonMemoryUserMaxXReachedTotal = 0f;
        var totalVisionRange = 0f;
        var totalVisionAngle = 0f;
        var totalDietaryAdaptation = 0f;
        var totalCarrionAdaptation = 0f;
        var totalTenderPlantAdaptation = 0f;
        var totalRichPlantAdaptation = 0f;
        var totalToughPlantAdaptation = 0f;
        var totalBiteStrength = 0f;
        var totalDamageResistance = 0f;
        var totalBiomeMovementCostMultiplier = 0f;
        var totalBiomeBasalCostMultiplier = 0f;
        var totalBiomeSpeedMultiplier = 0f;
        var totalForwardObstacle = 0f;
        var totalLeftObstacle = 0f;
        var totalRightObstacle = 0f;
        var barrenCaloriesEaten = 0f;
        var sparseCaloriesEaten = 0f;
        var grasslandCaloriesEaten = 0f;
        var richCaloriesEaten = 0f;
        var attackerTotalDietaryAdaptation = 0f;
        var attackerTotalBiteStrength = 0f;
        var attackerTotalDamageResistance = 0f;
        var nonAttackerTotalDietaryAdaptation = 0f;
        var nonAttackerTotalBiteStrength = 0f;
        var nonAttackerTotalDamageResistance = 0f;
        var foodDetectedCreatureCount = 0;
        var plantDetectedCreatureCount = 0;
        var meatDetectedCreatureCount = 0;
        var freshMeatDetectedCreatureCount = 0;
        var staleMeatDetectedCreatureCount = 0;
        var staleMeatAvoidedCreatureCount = 0;
        var creatureDetectedCreatureCount = 0;
        var meatScentDetectedCreatureCount = 0;
        var rottenMeatScentDetectedCreatureCount = 0;
        var foodContactCreatureCount = 0;
        var eatingCreatureCount = 0;
        var rottenMeatDamagedCreatureCount = 0;
        var attackingCreatureCount = 0;
        var creatureContactCreatureCount = 0;
        var attackIntentCreatureCount = 0;
        var attackIntentWhileTouchingCreatureCount = 0;
        var attackNoIntentContactCreatureCount = 0;
        var rawAttackPositiveCreatureCount = 0;
        var rawAttackNearGateCreatureCount = 0;
        var rawAttackNearGateWhileTouchingCreatureCount = 0;
        var reproductionReadyCreatureCount = 0;
        var reproductionIntentCreatureCount = 0;
        var activeMemoryCreatureCount = 0;
        var obstacleBlockedCreatureCount = 0;
        var obstacleSensedCreatureCount = 0;
        var nonMemoryCreatureCount = 0;
        var memoryUserFoodContactCount = 0;
        var nonMemoryUserFoodContactCount = 0;
        var memoryUserEatingCount = 0;
        var nonMemoryUserEatingCount = 0;
        var memoryUserRightRegionCount = 0;
        var nonMemoryUserRightRegionCount = 0;
        var nonAttackingCreatureCount = 0;
        var totalAttackOutput = 0f;
        var totalTouchingAttackOutput = 0f;
        var barrenCreatureCount = 0;
        var sparseCreatureCount = 0;
        var grasslandCreatureCount = 0;
        var richCreatureCount = 0;
        var leftRegionCreatureCount = 0;
        var middleRegionCreatureCount = 0;
        var rightRegionCreatureCount = 0;
        var leftRegionGenerationTotal = 0;
        var middleRegionGenerationTotal = 0;
        var rightRegionGenerationTotal = 0;
        var maxGeneration = 0;

        for (var i = 0; i < state.Creatures.Count; i++)
        {
            var creature = state.Creatures[i];
            var genome = state.GetGenome(creature.GenomeId);
            totalCreatureEnergy += creature.Energy;
            totalCreatureX += creature.Position.X;
            maxCreatureX = Math.Max(maxCreatureX, creature.Position.X);
            totalMaxCreatureXReached += creature.MaxXReached;
            maxCreatureXReached = Math.Max(maxCreatureXReached, creature.MaxXReached);
            totalVisibleFoodDensity += creature.Senses.VisibleFoodDensity;
            totalVisiblePlantDensity += creature.Senses.VisiblePlantDensity;
            totalVisibleMeatDensity += creature.Senses.VisibleMeatDensity;
            totalVisibleMeatFreshness += creature.Senses.MeatDetected
                ? creature.Senses.VisibleMeatFreshness
                : 0f;
            totalMeatScentDensity += creature.Senses.MeatScentDensity;
            totalRottenMeatScentDensity += creature.Senses.RottenMeatScentDensity;
            totalVisibleCreatureDensity += creature.Senses.VisibleCreatureDensity;
            totalCaloriesEaten += creature.LastCaloriesEaten;
            totalPlantCaloriesEaten += creature.LastPlantCaloriesEaten;
            totalTenderPlantCaloriesEaten += creature.LastTenderPlantCaloriesEaten;
            totalRichPlantCaloriesEaten += creature.LastRichPlantCaloriesEaten;
            totalToughPlantCaloriesEaten += creature.LastToughPlantCaloriesEaten;
            totalCarcassCaloriesEaten += creature.LastCarcassCaloriesEaten;
            totalEggCaloriesEaten += creature.LastEggCaloriesEaten;
            totalLivePreyCaloriesEaten += creature.LastLivePreyCaloriesEaten;
            totalFreshMeatCaloriesEaten += creature.LastFreshMeatCaloriesEaten;
            totalStaleMeatCaloriesEaten += creature.LastStaleMeatCaloriesEaten;
            totalCaloriesDigested += creature.LastCaloriesDigested;
            totalPlantDigestedEnergy += creature.LastPlantDigestedEnergy;
            totalTenderPlantDigestedEnergy += creature.LastTenderPlantDigestedEnergy;
            totalRichPlantDigestedEnergy += creature.LastRichPlantDigestedEnergy;
            totalToughPlantDigestedEnergy += creature.LastToughPlantDigestedEnergy;
            totalMeatDigestedEnergy += creature.LastMeatDigestedEnergy;
            totalRottenMeatDamage += creature.LastRottenMeatDamage;
            if (creature.LastRottenMeatDamage > 0f)
            {
                rottenMeatDamagedCreatureCount++;
            }

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
            totalRecentFoodEnergyYield += creature.Senses.RecentFoodEnergyYield;
            var memoryStrength = Math.Clamp(creature.MemoryVector.Length, 0f, 1f);
            totalMemoryStrength += memoryStrength;
            var isActiveMemoryUser = memoryStrength > 0.05f;
            if (isActiveMemoryUser)
            {
                activeMemoryCreatureCount++;
                memoryUserCaloriesEaten += creature.LastCaloriesEaten;
                memoryUserDistanceTraveled += creature.LastDistanceTraveled;
                memoryUserSecondsSinceLastMeal += creature.SecondsSinceLastMeal;
                memoryUserDistanceSinceLastMeal += creature.DistanceSinceLastMeal;
                memoryUserRecentFoodSuccess += creature.Senses.RecentFoodSuccess;
                memoryUserGenerationTotal += creature.Generation;
                memoryUserMaxXReachedTotal += creature.MaxXReached;
                if (creature.IsTouchingFood)
                {
                    memoryUserFoodContactCount++;
                }

                if (creature.LastCaloriesEaten > 0f)
                {
                    memoryUserEatingCount++;
                }
            }
            else
            {
                nonMemoryCreatureCount++;
                nonMemoryUserCaloriesEaten += creature.LastCaloriesEaten;
                nonMemoryUserDistanceTraveled += creature.LastDistanceTraveled;
                nonMemoryUserSecondsSinceLastMeal += creature.SecondsSinceLastMeal;
                nonMemoryUserDistanceSinceLastMeal += creature.DistanceSinceLastMeal;
                nonMemoryUserRecentFoodSuccess += creature.Senses.RecentFoodSuccess;
                nonMemoryUserGenerationTotal += creature.Generation;
                nonMemoryUserMaxXReachedTotal += creature.MaxXReached;
                if (creature.IsTouchingFood)
                {
                    nonMemoryUserFoodContactCount++;
                }

                if (creature.LastCaloriesEaten > 0f)
                {
                    nonMemoryUserEatingCount++;
                }
            }

            totalVisionRange += CreatureGrowth.EffectiveSenseRadius(creature, genome);
            totalVisionAngle += CreatureGrowth.EffectiveVisionAngleRadians(creature, genome);
            totalDietaryAdaptation += genome.DietaryAdaptation;
            totalCarrionAdaptation += genome.CarrionAdaptation;
            totalTenderPlantAdaptation += genome.TenderPlantAdaptation;
            totalRichPlantAdaptation += genome.RichPlantAdaptation;
            totalToughPlantAdaptation += genome.ToughPlantAdaptation;
            totalBiteStrength += genome.BiteStrength;
            totalDamageResistance += genome.DamageResistance;
            var biome = state.Biomes.GetKindAt(creature.Position);
            totalBiomeMovementCostMultiplier += _biomeMovementCostProfile.For(biome);
            totalBiomeBasalCostMultiplier += _biomeBasalCostProfile.For(biome);
            totalBiomeSpeedMultiplier += _biomeSpeedProfile.For(biome);
            totalForwardObstacle += creature.Senses.ForwardObstacle;
            totalLeftObstacle += creature.Senses.LeftObstacle;
            totalRightObstacle += creature.Senses.RightObstacle;
            if (creature.LastMovementBlocked)
            {
                obstacleBlockedCreatureCount++;
            }

            if (creature.Senses.ForwardObstacle > 0f
                || creature.Senses.LeftObstacle > 0f
                || creature.Senses.RightObstacle > 0f)
            {
                obstacleSensedCreatureCount++;
            }

            AddBiomeValue(
                biome,
                creature.LastCaloriesEaten,
                ref barrenCaloriesEaten,
                ref sparseCaloriesEaten,
                ref grasslandCaloriesEaten,
                ref richCaloriesEaten);
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

            var horizontalRegion = GetHorizontalRegion(state.Bounds, creature.Position);
            if (horizontalRegion == HorizontalRegion.Right)
            {
                if (isActiveMemoryUser)
                {
                    memoryUserRightRegionCount++;
                }
                else
                {
                    nonMemoryUserRightRegionCount++;
                }
            }

            switch (horizontalRegion)
            {
                case HorizontalRegion.Left:
                    leftRegionCreatureCount++;
                    leftRegionGenerationTotal += creature.Generation;
                    break;
                case HorizontalRegion.Right:
                    rightRegionCreatureCount++;
                    rightRegionGenerationTotal += creature.Generation;
                    break;
                default:
                    middleRegionCreatureCount++;
                    middleRegionGenerationTotal += creature.Generation;
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
                if (MeatQuality.IsFresh(creature.Senses.VisibleMeatFreshness))
                {
                    freshMeatDetectedCreatureCount++;
                }
                else
                {
                    staleMeatDetectedCreatureCount++;
                    if (creature.LastStaleMeatCaloriesEaten <= 0f)
                    {
                        staleMeatAvoidedCreatureCount++;
                    }
                }
            }

            if (creature.Senses.CreatureDetected)
            {
                creatureDetectedCreatureCount++;
            }

            if (creature.Senses.MeatScentDetected)
            {
                meatScentDetectedCreatureCount++;
            }

            if (creature.Senses.RottenMeatScentDetected)
            {
                rottenMeatScentDetectedCreatureCount++;
            }

            if (creature.IsTouchingFood)
            {
                foodContactCreatureCount++;
            }

            if (creature.LastCaloriesEaten > 0f)
            {
                eatingCreatureCount++;
            }

            totalAttackOutput += creature.Actions.AttackOutput;
            if (creature.Actions.AttackOutput > RawAttackPositiveThreshold)
            {
                rawAttackPositiveCreatureCount++;
                if (creature.Actions.AttackOutput <= NeuralControllerSystem.DefaultAttackThreshold)
                {
                    rawAttackNearGateCreatureCount++;
                }
            }

            if (creature.Actions.WantsAttack)
            {
                attackIntentCreatureCount++;
            }

            if (creature.IsTouchingCreature)
            {
                creatureContactCreatureCount++;
                totalTouchingAttackOutput += creature.Actions.AttackOutput;
                if (creature.Actions.WantsAttack)
                {
                    attackIntentWhileTouchingCreatureCount++;
                }
                else
                {
                    attackNoIntentContactCreatureCount++;
                }

                if (creature.Actions.AttackOutput > RawAttackPositiveThreshold
                    && creature.Actions.AttackOutput <= NeuralControllerSystem.DefaultAttackThreshold)
                {
                    rawAttackNearGateWhileTouchingCreatureCount++;
                }
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
        var tenderPlantTypeResourceCount = 0;
        var richPlantTypeResourceCount = 0;
        var toughPlantTypeResourceCount = 0;
        var meatResourceCount = 0;
        var dormantPlantResourceCount = 0;
        var totalDormantPlantSecondsRemaining = 0f;
        var leftRegionPlantCalories = 0f;
        var middleRegionPlantCalories = 0f;
        var rightRegionPlantCalories = 0f;
        var leftRegionMeatCalories = 0f;
        var middleRegionMeatCalories = 0f;
        var rightRegionMeatCalories = 0f;
        var barrenPlantCalories = 0f;
        var sparsePlantCalories = 0f;
        var grasslandPlantCalories = 0f;
        var richPlantCalories = 0f;
        var tenderPlantTypeCalories = 0f;
        var richPlantTypeCalories = 0f;
        var toughPlantTypeCalories = 0f;
        var barrenMeatCalories = 0f;
        var sparseMeatCalories = 0f;
        var grasslandMeatCalories = 0f;
        var richMeatCalories = 0f;
        Span<float> plantCaloriesByPatchCell = stackalloc float[PlantPatchinessGridCellCount];
        for (var i = 0; i < state.Resources.Count; i++)
        {
            var resource = state.Resources[i];
            if (resource.Kind == ResourceKind.Plant && resource.RespawnSecondsRemaining > 0f)
            {
                dormantPlantResourceCount++;
                totalDormantPlantSecondsRemaining += resource.RespawnSecondsRemaining;
            }

            if (resource.Calories <= 0f)
            {
                continue;
            }

            activeResourceCount++;
            totalResourceCalories += resource.Calories;
            var region = GetHorizontalRegion(state.Bounds, resource.Position);
            var biome = state.Biomes.GetKindAt(resource.Position);
            if (resource.Kind == ResourceKind.Meat)
            {
                meatResourceCount++;
                totalMeatCalories += resource.Calories;
                totalMeatFreshnessWeightedCalories += resource.Calories * MeatQuality.Freshness(resource);
                AddRegionValue(region, resource.Calories, ref leftRegionMeatCalories, ref middleRegionMeatCalories, ref rightRegionMeatCalories);
                AddBiomeValue(
                    biome,
                    resource.Calories,
                    ref barrenMeatCalories,
                    ref sparseMeatCalories,
                    ref grasslandMeatCalories,
                    ref richMeatCalories);
            }
            else
            {
                plantResourceCount++;
                totalPlantCalories += resource.Calories;
                switch (resource.PlantKind)
                {
                    case PlantResourceKind.Tender:
                        tenderPlantTypeResourceCount++;
                        tenderPlantTypeCalories += resource.Calories;
                        break;
                    case PlantResourceKind.Rich:
                        richPlantTypeResourceCount++;
                        richPlantTypeCalories += resource.Calories;
                        break;
                    case PlantResourceKind.Tough:
                        toughPlantTypeResourceCount++;
                        toughPlantTypeCalories += resource.Calories;
                        break;
                }

                AddPlantPatchCell(plantCaloriesByPatchCell, state.Bounds, resource.Position, resource.Calories);
                AddRegionValue(region, resource.Calories, ref leftRegionPlantCalories, ref middleRegionPlantCalories, ref rightRegionPlantCalories);
                AddBiomeValue(
                    biome,
                    resource.Calories,
                    ref barrenPlantCalories,
                    ref sparsePlantCalories,
                    ref grasslandPlantCalories,
                    ref richPlantCalories);
            }
        }
        var plantPatchSummary = CalculatePlantPatchSummary(plantCaloriesByPatchCell, totalPlantCalories);
        var averageDormantPlantSecondsRemaining = dormantPlantResourceCount > 0
            ? totalDormantPlantSecondsRemaining / dormantPlantResourceCount
            : 0f;

        var totalEggEnergy = 0f;
        var totalEggHealth = 0f;
        var totalEggHealthRatio = 0f;
        var leftRegionEggCount = 0;
        var middleRegionEggCount = 0;
        var rightRegionEggCount = 0;
        for (var i = 0; i < state.Eggs.Count; i++)
        {
            var egg = state.Eggs[i];
            totalEggEnergy += egg.Energy;
            totalEggHealth += egg.Health;
            totalEggHealthRatio += egg.MaxHealth > 0f
                ? Math.Clamp(egg.Health / egg.MaxHealth, 0f, 1f)
                : 1f;
            switch (GetHorizontalRegion(state.Bounds, egg.Position))
            {
                case HorizontalRegion.Left:
                    leftRegionEggCount++;
                    break;
                case HorizontalRegion.Right:
                    rightRegionEggCount++;
                    break;
                default:
                    middleRegionEggCount++;
                    break;
            }
        }

        var creatureCount = state.Creatures.Count;
        var divisor = Math.Max(1, creatureCount);
        var caloriesEatenPerSecond = deltaSeconds > 0f
            ? totalCaloriesEaten / deltaSeconds
            : 0f;
        var plantCaloriesEatenPerSecond = deltaSeconds > 0f
            ? totalPlantCaloriesEaten / deltaSeconds
            : 0f;
        var tenderPlantCaloriesEatenPerSecond = deltaSeconds > 0f
            ? totalTenderPlantCaloriesEaten / deltaSeconds
            : 0f;
        var richPlantCaloriesEatenPerSecond = deltaSeconds > 0f
            ? totalRichPlantCaloriesEaten / deltaSeconds
            : 0f;
        var toughPlantCaloriesEatenPerSecond = deltaSeconds > 0f
            ? totalToughPlantCaloriesEaten / deltaSeconds
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
        var tenderPlantDigestedEnergyPerSecond = deltaSeconds > 0f
            ? totalTenderPlantDigestedEnergy / deltaSeconds
            : 0f;
        var richPlantDigestedEnergyPerSecond = deltaSeconds > 0f
            ? totalRichPlantDigestedEnergy / deltaSeconds
            : 0f;
        var toughPlantDigestedEnergyPerSecond = deltaSeconds > 0f
            ? totalToughPlantDigestedEnergy / deltaSeconds
            : 0f;
        var meatDigestedEnergyPerSecond = deltaSeconds > 0f
            ? totalMeatDigestedEnergy / deltaSeconds
            : 0f;
        var attackDamagePerSecond = deltaSeconds > 0f
            ? totalAttackDamage / deltaSeconds
            : 0f;
        var rottenMeatDamagePerSecond = deltaSeconds > 0f
            ? totalRottenMeatDamage / deltaSeconds
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
        var memoryUserDivisor = Math.Max(1, activeMemoryCreatureCount);
        var nonMemoryUserDivisor = Math.Max(1, nonMemoryCreatureCount);
        var visibleMeatDivisor = Math.Max(1, meatDetectedCreatureCount);
        var memoryUserCaloriesEatenPerDistance = memoryUserDistanceTraveled > 0f
            ? memoryUserCaloriesEaten / memoryUserDistanceTraveled
            : 0f;
        var nonMemoryUserCaloriesEatenPerDistance = nonMemoryUserDistanceTraveled > 0f
            ? nonMemoryUserCaloriesEaten / nonMemoryUserDistanceTraveled
            : 0f;
        var memoryUserAverageMaxXProgressShare = activeMemoryCreatureCount > 0
            ? EastProgressShare(memoryUserMaxXReachedTotal / activeMemoryCreatureCount, state.Bounds)
            : 0f;
        var nonMemoryUserAverageMaxXProgressShare = nonMemoryCreatureCount > 0
            ? EastProgressShare(nonMemoryUserMaxXReachedTotal / nonMemoryCreatureCount, state.Bounds)
            : 0f;
        var attackerDivisor = Math.Max(1, attackingCreatureCount);
        var nonAttackerDivisor = Math.Max(1, nonAttackingCreatureCount);
        var totalBrainHiddenNodeCount = 0;
        var maxBrainHiddenNodeCount = 0;
        var totalHiddenInputWeightMagnitude = 0f;
        var totalHiddenOutputWeightMagnitude = 0f;
        var hiddenInputWeightCount = 0;
        var hiddenOutputWeightCount = 0;
        var activeHiddenOutputWeightCount = 0;
        for (var i = 0; i < state.Brains.Count; i++)
        {
            var brain = state.Brains[i];
            var hiddenNodeCount = brain.HiddenNodeCount;
            totalBrainHiddenNodeCount += hiddenNodeCount;
            maxBrainHiddenNodeCount = Math.Max(maxBrainHiddenNodeCount, hiddenNodeCount);
            totalHiddenInputWeightMagnitude += brain.SumAbsoluteHiddenInputWeights();
            totalHiddenOutputWeightMagnitude += brain.SumAbsoluteHiddenOutputWeights();
            hiddenInputWeightCount += brain.HiddenInputWeightCount;
            hiddenOutputWeightCount += brain.HiddenOutputWeightCount;
            activeHiddenOutputWeightCount += brain.CountActiveHiddenOutputWeights(ActiveHiddenOutputWeightThreshold);
        }

        var averageBrainHiddenNodeCount = state.Brains.Count > 0
            ? totalBrainHiddenNodeCount / (float)state.Brains.Count
            : 0f;
        var averageHiddenInputWeightMagnitude = hiddenInputWeightCount > 0
            ? totalHiddenInputWeightMagnitude / hiddenInputWeightCount
            : 0f;
        var averageHiddenOutputWeightMagnitude = hiddenOutputWeightCount > 0
            ? totalHiddenOutputWeightMagnitude / hiddenOutputWeightCount
            : 0f;
        var activeHiddenOutputShare = hiddenOutputWeightCount > 0
            ? activeHiddenOutputWeightCount / (float)hiddenOutputWeightCount
            : 0f;
        var season = SeasonalFertility.Calculate(
            _enableSeasons,
            state.ElapsedSeconds,
            _seasonLengthSeconds,
            _seasonFertilityAmplitude,
            _seasonPhaseOffsetSeconds);
        var localFertilitySummary = state.LocalFertility.Summarize();
        var leftRegionSeason = CalculateRegionSeason(state, 1f / 6f);
        var middleRegionSeason = CalculateRegionSeason(state, 0.5f);
        var rightRegionSeason = CalculateRegionSeason(state, 5f / 6f);
        state.Stats.RecordEastwardProgress(maxCreatureXReached);
        state.Stats.RecordSnapshot(new SimulationStatsSnapshot(
            state.Tick,
            state.ElapsedSeconds,
            season.Phase,
            season.FertilityMultiplier,
            creatureCount,
            state.Eggs.Count,
            activeResourceCount,
            plantResourceCount,
            meatResourceCount,
            dormantPlantResourceCount,
            totalDormantPlantSecondsRemaining,
            averageDormantPlantSecondsRemaining,
            plantPatchSummary.OccupiedCellShare,
            plantPatchSummary.TopDecileCaloriesShare,
            plantPatchSummary.Patchiness,
            localFertilitySummary.CellCount,
            localFertilitySummary.AverageMultiplier,
            localFertilitySummary.MinimumMultiplier,
            localFertilitySummary.DepletedCellShare,
            state.Genomes.Count,
            state.Brains.Count,
            averageBrainHiddenNodeCount,
            maxBrainHiddenNodeCount,
            averageHiddenInputWeightMagnitude,
            averageHiddenOutputWeightMagnitude,
            activeHiddenOutputShare,
            maxGeneration,
            totalCreatureEnergy,
            totalEggEnergy,
            totalEggHealth,
            totalResourceCalories,
            totalPlantCalories,
            tenderPlantTypeResourceCount,
            richPlantTypeResourceCount,
            toughPlantTypeResourceCount,
            tenderPlantTypeCalories,
            richPlantTypeCalories,
            toughPlantTypeCalories,
            totalMeatCalories,
            barrenCreatureCount,
            sparseCreatureCount,
            grasslandCreatureCount,
            richCreatureCount,
            totalBiomeMovementCostMultiplier / divisor,
            totalBiomeBasalCostMultiplier / divisor,
            totalBiomeSpeedMultiplier / divisor,
            obstacleBlockedCreatureCount,
            obstacleSensedCreatureCount,
            totalForwardObstacle / divisor,
            totalLeftObstacle / divisor,
            totalRightObstacle / divisor,
            barrenPlantCalories,
            sparsePlantCalories,
            grasslandPlantCalories,
            richPlantCalories,
            barrenMeatCalories,
            sparseMeatCalories,
            grasslandMeatCalories,
            richMeatCalories,
            Rate(barrenCaloriesEaten, deltaSeconds),
            Rate(sparseCaloriesEaten, deltaSeconds),
            Rate(grasslandCaloriesEaten, deltaSeconds),
            Rate(richCaloriesEaten, deltaSeconds),
            state.Stats.BarrenDeathCount,
            state.Stats.SparseDeathCount,
            state.Stats.GrasslandDeathCount,
            state.Stats.RichDeathCount,
            totalCreatureX / divisor,
            maxCreatureX,
            totalMaxCreatureXReached / divisor,
            maxCreatureXReached,
            Math.Max(state.Stats.MaxCreatureXReached, maxCreatureXReached),
            EastProgressShare(maxCreatureX, state.Bounds),
            EastProgressShare(Math.Max(state.Stats.MaxCreatureXReached, maxCreatureXReached), state.Bounds),
            foodDetectedCreatureCount,
            plantDetectedCreatureCount,
            meatDetectedCreatureCount,
            foodContactCreatureCount,
            eatingCreatureCount,
            totalVisibleFoodDensity / divisor,
            totalVisiblePlantDensity / divisor,
            totalVisibleMeatDensity / divisor,
            freshMeatDetectedCreatureCount,
            staleMeatDetectedCreatureCount,
            staleMeatAvoidedCreatureCount,
            totalVisibleMeatFreshness / visibleMeatDivisor,
            meatScentDetectedCreatureCount,
            totalMeatScentDensity / divisor,
            rottenMeatScentDetectedCreatureCount,
            totalRottenMeatScentDensity / divisor,
            totalVisibleCreatureDensity / divisor,
            caloriesEatenPerSecond,
            plantCaloriesEatenPerSecond,
            tenderPlantCaloriesEatenPerSecond,
            richPlantCaloriesEatenPerSecond,
            toughPlantCaloriesEatenPerSecond,
            carcassCaloriesEatenPerSecond,
            eggCaloriesEatenPerSecond,
            livePreyCaloriesEatenPerSecond,
            caloriesDigestedPerSecond,
            plantDigestedEnergyPerSecond,
            tenderPlantDigestedEnergyPerSecond,
            richPlantDigestedEnergyPerSecond,
            toughPlantDigestedEnergyPerSecond,
            meatDigestedEnergyPerSecond,
            totalGutFillRatio / divisor,
            totalGutPlantShare / divisor,
            totalGutMeatShare / divisor,
            attackingCreatureCount,
            creatureContactCreatureCount,
            attackIntentCreatureCount,
            attackIntentWhileTouchingCreatureCount,
            attackNoIntentContactCreatureCount,
            rawAttackPositiveCreatureCount,
            rawAttackNearGateCreatureCount,
            rawAttackNearGateWhileTouchingCreatureCount,
            totalAttackOutput / divisor,
            creatureContactCreatureCount > 0 ? totalTouchingAttackOutput / creatureContactCreatureCount : 0f,
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
            state.Stats.RottenMeatDeathCount,
            state.Stats.PlantDepletionCount,
            state.Stats.PlantLocalDispersalCount,
            state.Stats.PlantClusterRelocationCount,
            state.Stats.PlantGlobalRelocationCount,
            state.Stats.PlantDormancyStartedCount,
            state.Stats.PlantDormancyCompletedCount,
            state.Stats.AveragePlantDormancyScheduledSeconds,
            state.Stats.AveragePlantDormancyCompletedSeconds,
            creatureDetectedCreatureCount,
            meatCaloriesEatenShare,
            freshKillCaloriesEatenShare,
            meatDigestedEnergyShare,
            totalDietaryAdaptation / divisor,
            totalCarrionAdaptation / divisor,
            totalTenderPlantAdaptation / divisor,
            totalRichPlantAdaptation / divisor,
            totalToughPlantAdaptation / divisor,
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
            rottenMeatDamagePerSecond,
            rottenMeatDamagedCreatureCount,
            state.Stats.AverageDeadCreatureLifespanSeconds,
            state.Stats.MedianDeadCreatureLifespanSeconds,
            reproductionReadyCreatureCount,
            reproductionIntentCreatureCount,
            totalEggReserveRatio / divisor,
            totalEnergySurplusRatio / divisor,
            totalRecentFoodSuccess / divisor,
            totalRecentFoodEnergyYield / divisor,
            activeMemoryCreatureCount,
            totalMemoryStrength / divisor,
            memoryUserFoodContactCount / (float)memoryUserDivisor,
            nonMemoryUserFoodContactCount / (float)nonMemoryUserDivisor,
            memoryUserEatingCount / (float)memoryUserDivisor,
            nonMemoryUserEatingCount / (float)nonMemoryUserDivisor,
            memoryUserCaloriesEatenPerDistance,
            nonMemoryUserCaloriesEatenPerDistance,
            memoryUserSecondsSinceLastMeal / memoryUserDivisor,
            nonMemoryUserSecondsSinceLastMeal / nonMemoryUserDivisor,
            memoryUserDistanceSinceLastMeal / memoryUserDivisor,
            nonMemoryUserDistanceSinceLastMeal / nonMemoryUserDivisor,
            memoryUserRecentFoodSuccess / memoryUserDivisor,
            nonMemoryUserRecentFoodSuccess / nonMemoryUserDivisor,
            memoryUserGenerationTotal / (float)memoryUserDivisor,
            nonMemoryUserGenerationTotal / (float)nonMemoryUserDivisor,
            memoryUserAverageMaxXProgressShare,
            nonMemoryUserAverageMaxXProgressShare,
            memoryUserRightRegionCount / (float)memoryUserDivisor,
            nonMemoryUserRightRegionCount / (float)nonMemoryUserDivisor,
            leftRegionCreatureCount,
            middleRegionCreatureCount,
            rightRegionCreatureCount,
            leftRegionEggCount,
            middleRegionEggCount,
            rightRegionEggCount,
            leftRegionPlantCalories,
            middleRegionPlantCalories,
            rightRegionPlantCalories,
            leftRegionMeatCalories,
            middleRegionMeatCalories,
            rightRegionMeatCalories,
            AverageRegionGeneration(leftRegionGenerationTotal, leftRegionCreatureCount),
            AverageRegionGeneration(middleRegionGenerationTotal, middleRegionCreatureCount),
            AverageRegionGeneration(rightRegionGenerationTotal, rightRegionCreatureCount),
            leftRegionSeason.FertilityMultiplier,
            middleRegionSeason.FertilityMultiplier,
            rightRegionSeason.FertilityMultiplier));
    }

    private SeasonalFertilityState CalculateRegionSeason(WorldState state, float xFraction)
    {
        var position = new SimVector2(
            Math.Clamp(state.Bounds.Width * xFraction, 0f, state.Bounds.Width),
            state.Bounds.Height * 0.5f);
        return SeasonalFertility.CalculateAt(
            _enableSeasons,
            state.ElapsedSeconds,
            _seasonLengthSeconds,
            _seasonFertilityAmplitude,
            _seasonPhaseOffsetSeconds,
            _seasonPhaseMode,
            state.Bounds,
            position);
    }

    private static float AverageRegionGeneration(int generationTotal, int creatureCount)
    {
        return creatureCount > 0
            ? generationTotal / (float)creatureCount
            : 0f;
    }

    private static HorizontalRegion GetHorizontalRegion(WorldBounds bounds, SimVector2 position)
    {
        var third = bounds.Width / 3f;
        if (position.X < third)
        {
            return HorizontalRegion.Left;
        }

        return position.X >= third * 2f
            ? HorizontalRegion.Right
            : HorizontalRegion.Middle;
    }

    private static void AddRegionValue(
        HorizontalRegion region,
        float value,
        ref float left,
        ref float middle,
        ref float right)
    {
        switch (region)
        {
            case HorizontalRegion.Left:
                left += value;
                break;
            case HorizontalRegion.Right:
                right += value;
                break;
            default:
                middle += value;
                break;
        }
    }

    private static void AddBiomeValue(
        BiomeKind biome,
        float value,
        ref float barren,
        ref float sparse,
        ref float grassland,
        ref float rich)
    {
        switch (biome)
        {
            case BiomeKind.Barren:
                barren += value;
                break;
            case BiomeKind.Sparse:
                sparse += value;
                break;
            case BiomeKind.Rich:
                rich += value;
                break;
            default:
                grassland += value;
                break;
        }
    }

    private static void AddPlantPatchCell(
        Span<float> plantCaloriesByPatchCell,
        WorldBounds bounds,
        SimVector2 position,
        float calories)
    {
        if (calories <= 0f || bounds.Width <= 0f || bounds.Height <= 0f)
        {
            return;
        }

        var x = Math.Clamp(
            (int)(position.X / bounds.Width * PlantPatchinessGridAxisCells),
            0,
            PlantPatchinessGridAxisCells - 1);
        var y = Math.Clamp(
            (int)(position.Y / bounds.Height * PlantPatchinessGridAxisCells),
            0,
            PlantPatchinessGridAxisCells - 1);
        plantCaloriesByPatchCell[y * PlantPatchinessGridAxisCells + x] += calories;
    }

    private static PlantPatchSummary CalculatePlantPatchSummary(
        ReadOnlySpan<float> plantCaloriesByPatchCell,
        float totalPlantCalories)
    {
        if (totalPlantCalories <= 0f || plantCaloriesByPatchCell.Length == 0)
        {
            return default;
        }

        var occupiedCellCount = 0;
        var mean = totalPlantCalories / plantCaloriesByPatchCell.Length;
        var varianceSum = 0f;
        Span<float> topDecile = stackalloc float[Math.Max(1, PlantPatchinessGridCellCount / 10)];
        for (var i = 0; i < plantCaloriesByPatchCell.Length; i++)
        {
            var calories = plantCaloriesByPatchCell[i];
            if (calories > 0f)
            {
                occupiedCellCount++;
                AddTopValue(topDecile, calories);
            }

            var delta = calories - mean;
            varianceSum += delta * delta;
        }

        var topDecileCalories = 0f;
        for (var i = 0; i < topDecile.Length; i++)
        {
            topDecileCalories += topDecile[i];
        }

        var standardDeviation = MathF.Sqrt(varianceSum / plantCaloriesByPatchCell.Length);
        return new PlantPatchSummary(
            occupiedCellCount / (float)plantCaloriesByPatchCell.Length,
            topDecileCalories / totalPlantCalories,
            mean > 0f ? standardDeviation / mean : 0f);
    }

    private static void AddTopValue(Span<float> topValues, float value)
    {
        if (topValues.Length == 0 || value <= topValues[0])
        {
            return;
        }

        topValues[0] = value;
        for (var i = 1; i < topValues.Length && topValues[i - 1] > topValues[i]; i++)
        {
            (topValues[i - 1], topValues[i]) = (topValues[i], topValues[i - 1]);
        }
    }

    private static float Rate(float value, float seconds)
    {
        return seconds > 0f ? value / seconds : 0f;
    }

    private static float EastProgressShare(float x, WorldBounds bounds)
    {
        return bounds.Width > 0f
            ? Math.Clamp(x / bounds.Width, 0f, 1f)
            : 0f;
    }

    private enum HorizontalRegion
    {
        Left,
        Middle,
        Right
    }

    private readonly record struct PlantPatchSummary(
        float OccupiedCellShare,
        float TopDecileCaloriesShare,
        float Patchiness);

    private static float EnsurePositive(float value, string name)
    {
        if (!float.IsFinite(value) || value <= 0f)
        {
            throw new ArgumentOutOfRangeException(name, $"{name} must be finite and positive.");
        }

        return value;
    }

    private static float EnsureRange(float value, float inclusiveMin, float inclusiveMax, string name)
    {
        if (!float.IsFinite(value) || value < inclusiveMin || value > inclusiveMax)
        {
            throw new ArgumentOutOfRangeException(name, $"{name} must be finite and between {inclusiveMin} and {inclusiveMax}.");
        }

        return value;
    }

    private static float EnsureFinite(float value, string name)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(name, $"{name} must be finite.");
        }

        return value;
    }
}
