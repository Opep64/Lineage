using System.Globalization;
using System.Net;
using Lineage.Core;

namespace Lineage.Viewer;

/// <summary>
/// Writes a lightweight HTML report for the simulation currently running in Godot.
/// </summary>
///
/// <remarks>
/// This intentionally lives in the viewer instead of referencing Lineage.Cli. The
/// CLI reports remain richer experiment artifacts, while this report captures the
/// in-memory viewer state without rerunning the scenario.
/// </remarks>
public static class ViewerReportWriter
{
    private const int ReportTrendRowCount = 8;

    public static void Write(string path, SimulationScenario scenario, Simulation simulation)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(path);
        var state = simulation.State;
        var snapshots = state.Stats.Snapshots;
        var snapshot = snapshots.Count > 0 ? snapshots[^1] : default;
        var totalResourceCalories = 0f;
        var resourceCapacity = 0f;
        var activeResourceCount = 0;
        foreach (var resource in state.Resources)
        {
            if (resource.Calories <= 0f)
            {
                continue;
            }

            activeResourceCount++;
            totalResourceCalories += resource.Calories;
            resourceCapacity += MathF.Max(resource.MaxCalories, resource.Calories);
        }

        var worldArea = MathF.Max(1f, state.Bounds.Width * state.Bounds.Height);
        var resourceDensity = activeResourceCount / worldArea * 1_000_000f;
        var traitSummary = SummarizeTraits(state);
        var biomeSummaries = state.Biomes.SummarizeResources(state.Resources);
        var allFounderSummaries = SummarizeFounders(state.LineageRecords)
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenByDescending(summary => summary.TotalCreatures)
            .ThenBy(summary => summary.FounderId.Value)
            .ToArray();
        var founderSummaries = allFounderSummaries
            .Take(10)
            .ToArray();
        var behaviorSummary = BehaviorAssay.Analyze(state);
        var lineageBehaviorSummaries = BehaviorAssay.AnalyzeTopFounderLineages(state, 10);
        var speciesSummaries = SpeciesClusterAnalyzer.Analyze(state, 10);
        var speciesBehaviorFingerprints = SpeciesClusterAnalyzer.AnalyzeBehaviorFingerprints(state, 10);
        var speciesBrainInputDiagnostics = SpeciesClusterAnalyzer.AnalyzeBrainInputDiagnostics(state, 10);
        var speciesHistory = SpeciesClusterAnalyzer.AnalyzeHistory(state, snapshots, 10);
        var speciesBehaviorChanges = SpeciesClusterAnalyzer.AnalyzeBehaviorChanges(state, speciesHistory, 10);
        var brainInputDiagnostics = BrainInputDiagnostics.Analyze(state);
        var lineageBrainInputDiagnostics = BrainInputDiagnostics.AnalyzeTopFounderLineages(state, 10);
        var seasonPressure = SeasonPressureAnalysis.Analyze(scenario, snapshots);

        WriteDocumentStart(writer, $"Lineage Viewer Report - {scenario.Name}");

        writer.WriteLine("<header>");
        writer.WriteLine("<div class=\"page-width\">");
        writer.WriteLine("<p class=\"eyebrow\">Lineage Viewer</p>");
        writer.WriteLine("<h1>Current Simulation Report</h1>");
        writer.WriteLine($"<p>{Html(scenario.Name)} at tick {Html(state.Tick)}. This report was written from the running Godot viewer.</p>");
        writer.WriteLine("</div>");
        writer.WriteLine("</header>");
        writer.WriteLine("<main class=\"page-width\">");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Run</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Scenario", scenario.Name);
        WriteMetric(writer, "Pipeline", scenario.PipelineKind.ToString());
        WriteMetric(writer, "Brain architecture", FormatBrainArchitectureKind(scenario.BrainArchitectureKind));
        WriteMetric(writer, "Initial brain", FormatInitialBrainKind(scenario.InitialBrainKind));
        WriteMetric(writer, "Brain hidden nodes", scenario.BrainHiddenNodeCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Legacy nearest food vision inputs", scenario.EnableLegacyNearestFoodVisionInputs ? "enabled" : "disabled");
        WriteMetric(writer, "Seed", scenario.Seed.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "World size", $"{state.Bounds.Width:0} x {state.Bounds.Height:0}");
        WriteMetric(writer, "Resources per 1M area", resourceDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Tick", state.Tick.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Simulated seconds", state.ElapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Snapshot interval", $"{scenario.StatsSnapshotIntervalTicks} ticks");
        WriteMetric(writer, "Seasons", scenario.EnableSeasons ? "Enabled" : "Disabled");
        WriteMetric(writer, "Season length", $"{scenario.SeasonLengthSeconds:0.###} seconds");
        WriteMetric(writer, "Season fertility swing", FormatPercent(scenario.SeasonFertilityAmplitude));
        WriteMetric(writer, "Season phase mode", scenario.SeasonPhaseMode.ToString());
        WriteMetric(writer, "Scenario species roster", FormatScenarioSpeciesSeeds(scenario));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Outcome</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Living creatures", state.Creatures.Count.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Eggs", state.Eggs.Count.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Active resources", activeResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Resource slots", state.Resources.Count.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plants", snapshot.PlantResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Meat", snapshot.MeatResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Resource calories", totalResourceCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plant calories", snapshot.TotalPlantCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Meat calories", snapshot.TotalMeatCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Dormant plants", snapshot.DormantPlantResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg dormancy remaining", $"{snapshot.AverageDormantPlantSecondsRemaining:0.###} seconds");
        WriteMetric(writer, "Plant patch occupied", FormatPercent(snapshot.PlantPatchOccupiedCellShare));
        WriteMetric(writer, "Plant top-decile calories", FormatPercent(snapshot.PlantPatchTopDecileCaloriesShare));
        WriteMetric(writer, "Plant patchiness", snapshot.PlantPatchiness.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg local fertility", $"{snapshot.AverageLocalFertilityMultiplier:0.###}x");
        WriteMetric(writer, "Min local fertility", $"{snapshot.MinimumLocalFertilityMultiplier:0.###}x");
        WriteMetric(writer, "Depleted fertility cells", FormatPercent(snapshot.DepletedLocalFertilityCellShare));
        WriteMetric(writer, "Plant depletions", state.Stats.PlantDepletionCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plant relocations", FormatPlantRelocations(state.Stats));
        WriteMetric(writer, "Avg dormancy scheduled", $"{state.Stats.AveragePlantDormancyScheduledSeconds:0.###} seconds");
        WriteMetric(writer, "Avg dormancy completed", $"{state.Stats.AveragePlantDormancyCompletedSeconds:0.###} seconds");
        WriteMetric(writer, "Avg meat freshness", FormatPercent(snapshot.AverageMeatFreshness));
        WriteMetric(writer, "Resource fullness", resourceCapacity > 0f ? $"{totalResourceCalories / resourceCapacity * 100f:0.0}%" : "n/a");
        WriteMetric(writer, "Births", state.Stats.CreatureBirthCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Eggs laid", state.Stats.EggLaidCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Reproduction attempts", state.Stats.ReproductionAttemptCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Attempt success", FormatPercent(Share(state.Stats.EggLaidCount, state.Stats.ReproductionAttemptCount)));
        WriteMetric(writer, "Eggs hatched", state.Stats.EggHatchedCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Egg deaths", state.Stats.EggDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Egg predation deaths", state.Stats.EggPredationDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Egg survival", FormatPercent(Share(state.Stats.EggHatchedCount, state.Stats.EggLaidCount)));
        WriteMetric(writer, "Offspring alive", FormatPercent(Share(state.Creatures.Count(creature => creature.Generation > 0), state.Stats.EggHatchedCount)));
        WriteMetric(writer, "Egg health", $"{snapshot.AverageEggHealthRatio * 100f:0.0}%");
        WriteMetric(writer, "Birth investment", $"{snapshot.AverageBirthInvestmentRatio:0.###}x");
        WriteMetric(writer, "Reproduction intent", FormatPercent(Share(snapshot.ReproductionIntentCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Ready to lay", FormatPercent(Share(snapshot.ReproductionReadyCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Egg reserve", FormatPercent(snapshot.AverageEggReserveRatio));
        WriteMetric(writer, "Energy surplus", FormatPercent(snapshot.AverageEnergySurplusRatio));
        WriteMetric(writer, "Food success", FormatPercent(snapshot.AverageRecentFoodSuccess));
        WriteMetric(writer, "Active memory", $"{FormatPercent(Share(snapshot.ActiveMemoryCreatureCount, snapshot.CreatureCount))} ({snapshot.ActiveMemoryCreatureCount})");
        WriteMetric(writer, "Memory strength", snapshot.AverageMemoryStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Deaths", state.Stats.CreatureDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Starvation deaths", state.Stats.StarvationDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Rotten meat deaths", state.Stats.RottenMeatDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average lifespan", $"{snapshot.AverageLifespanSeconds:0.###} seconds");
        WriteMetric(writer, "Median lifespan", $"{snapshot.MedianLifespanSeconds:0.###} seconds");
        WriteMetric(writer, "Max generation", snapshot.MaxGeneration.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg hidden input weight", snapshot.AverageBrainHiddenInputWeightMagnitude.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg hidden output weight", snapshot.AverageBrainHiddenOutputWeightMagnitude.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Active hidden outputs", FormatPercent(snapshot.ActiveBrainHiddenOutputShare));
        WriteMetric(writer, "Avg movement biome cost", $"{snapshot.AverageBiomeMovementCostMultiplier:0.###}x");
        WriteMetric(writer, "Avg basal biome cost", $"{snapshot.AverageBiomeBasalCostMultiplier:0.###}x");
        WriteMetric(writer, "Avg biome speed", $"{snapshot.AverageBiomeSpeedMultiplier:0.###}x");
        WriteMetric(writer, "Season phase", FormatPercent(snapshot.SeasonPhase));
        WriteMetric(writer, "Season fertility", $"{snapshot.SeasonFertilityMultiplier:0.###}x");
        WriteMetric(writer, "Region season fertility", FormatRegionValues(
            snapshot.LeftRegionSeasonFertilityMultiplier,
            snapshot.MiddleRegionSeasonFertilityMultiplier,
            snapshot.RightRegionSeasonFertilityMultiplier,
            "0.###x"));
        WriteMetric(writer, "Region population", FormatRegionCounts(
            snapshot.LeftRegionCreatureCount,
            snapshot.MiddleRegionCreatureCount,
            snapshot.RightRegionCreatureCount));
        WriteMetric(writer, "Region plant kcal", FormatRegionValues(
            snapshot.LeftRegionPlantCalories,
            snapshot.MiddleRegionPlantCalories,
            snapshot.RightRegionPlantCalories,
            "0"));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        WriteSeasonPressureSection(
            writer,
            seasonPressure,
            allFounderSummaries.Count(summary => summary.LivingCreatures > 0),
            allFounderSummaries.Length);

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Foraging Diagnostics</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Seeing food", FormatPercent(Share(snapshot.FoodDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Seeing plants", FormatPercent(Share(snapshot.PlantDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Seeing meat", FormatPercent(Share(snapshot.MeatDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Seeing fresh meat", FormatPercent(Share(snapshot.FreshMeatDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Seeing stale meat", FormatPercent(Share(snapshot.StaleMeatDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Stale seen but not eaten", FormatPercent(Share(snapshot.StaleMeatAvoidedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Visible meat freshness", FormatPercent(snapshot.AverageVisibleMeatFreshness));
        WriteMetric(writer, "Smelling meat", FormatPercent(Share(snapshot.MeatScentDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Smelling rot", FormatPercent(Share(snapshot.RottenMeatScentDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Seeing creatures", FormatPercent(Share(snapshot.CreatureDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Touching food", FormatPercent(Share(snapshot.FoodContactCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Eating this tick", FormatPercent(Share(snapshot.EatingCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Visible food density", snapshot.AverageVisibleFoodDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible plant density", snapshot.AverageVisiblePlantDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible meat density", snapshot.AverageVisibleMeatDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Meat scent density", snapshot.AverageMeatScentDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Rot scent density", snapshot.AverageRottenMeatScentDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible creature density", snapshot.AverageVisibleCreatureDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Obstacle sensed", FormatPercent(Share(snapshot.ObstacleSensedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Movement blocked", FormatPercent(Share(snapshot.ObstacleBlockedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Obstacle pressure", $"{snapshot.AverageForwardObstacle:0.###} fwd / {snapshot.AverageLeftObstacle:0.###} left / {snapshot.AverageRightObstacle:0.###} right");
        WriteMetric(writer, "Calories eaten", $"{snapshot.TotalCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Plant eaten", $"{snapshot.TotalPlantCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Carcass eaten", $"{snapshot.TotalCarcassCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Fresh meat eaten", $"{snapshot.TotalFreshMeatCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Stale meat eaten", $"{snapshot.TotalStaleMeatCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Rotten damage", $"{snapshot.TotalRottenMeatDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Rotten affected", FormatPercent(Share(snapshot.RottenMeatDamagedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Egg eaten", $"{snapshot.TotalEggCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Fresh kill eaten", $"{snapshot.TotalLivePreyCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Calories digested", $"{snapshot.TotalCaloriesDigestedPerSecond:0.###} energy/s");
        WriteMetric(writer, "Plant energy", $"{snapshot.TotalPlantDigestedEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Meat energy", $"{snapshot.TotalMeatDigestedEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Gut fullness", FormatPercent(snapshot.AverageGutFillRatio));
        WriteMetric(writer, "Gut plant share", FormatPercent(snapshot.AverageGutPlantShare));
        WriteMetric(writer, "Gut meat share", FormatPercent(snapshot.AverageGutMeatShare));
        WriteMetric(writer, "Time since meal", $"{snapshot.AverageSecondsSinceLastMeal:0.###} s avg");
        WriteMetric(writer, "Distance moved", $"{snapshot.TotalDistanceTraveledPerSecond:0.###} units/s");
        WriteMetric(writer, "Distance since meal", $"{snapshot.AverageDistanceSinceLastMeal:0.###} units avg");
        WriteMetric(writer, "Raw per distance", $"{snapshot.CaloriesEatenPerDistance:0.###} kcal/unit");
        WriteMetric(writer, "Energy per distance", $"{snapshot.CaloriesDigestedPerDistance:0.###} energy/unit");
        WriteMetric(writer, "Raw per food vision", $"{snapshot.CaloriesEatenPerFoodVisionEvent:0.###} kcal/event");
        WriteMetric(writer, "Average vision range", snapshot.AverageVisionRange.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average vision angle", $"{ToDegrees(snapshot.AverageVisionAngleRadians):0.###} degrees");
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Memory Diagnostics</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Active memory", $"{FormatPercent(Share(snapshot.ActiveMemoryCreatureCount, snapshot.CreatureCount))} ({snapshot.ActiveMemoryCreatureCount})");
        WriteMetric(writer, "Avg memory strength", snapshot.AverageMemoryStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Food contact", $"{FormatPercent(snapshot.MemoryUserFoodContactShare)} memory / {FormatPercent(snapshot.NonMemoryUserFoodContactShare)} non");
        WriteMetric(writer, "Eating", $"{FormatPercent(snapshot.MemoryUserEatingShare)} memory / {FormatPercent(snapshot.NonMemoryUserEatingShare)} non");
        WriteMetric(writer, "Food success", $"{FormatPercent(snapshot.MemoryUserAverageRecentFoodSuccess)} memory / {FormatPercent(snapshot.NonMemoryUserAverageRecentFoodSuccess)} non");
        WriteMetric(writer, "Raw per distance", $"{snapshot.MemoryUserCaloriesEatenPerDistance:0.###} memory / {snapshot.NonMemoryUserCaloriesEatenPerDistance:0.###} non");
        WriteMetric(writer, "Meal gap", $"{snapshot.MemoryUserAverageSecondsSinceLastMeal:0.###}s memory / {snapshot.NonMemoryUserAverageSecondsSinceLastMeal:0.###}s non");
        WriteMetric(writer, "Meal distance", $"{snapshot.MemoryUserAverageDistanceSinceLastMeal:0.###}u memory / {snapshot.NonMemoryUserAverageDistanceSinceLastMeal:0.###}u non");
        WriteMetric(writer, "Generation", $"{snapshot.MemoryUserAverageGeneration:0.###} memory / {snapshot.NonMemoryUserAverageGeneration:0.###} non");
        WriteMetric(writer, "Avg max-X progress", $"{FormatPercent(snapshot.MemoryUserAverageMaxXProgressShare)} memory / {FormatPercent(snapshot.NonMemoryUserAverageMaxXProgressShare)} non");
        WriteMetric(writer, "Right-region share", $"{FormatPercent(snapshot.MemoryUserRightRegionShare)} memory / {FormatPercent(snapshot.NonMemoryUserRightRegionShare)} non");
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        var attackDamagePerAttacker = snapshot.AttackingCreatureCount > 0
            ? snapshot.TotalAttackDamagePerSecond / snapshot.AttackingCreatureCount
            : 0f;
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Predation Diagnostics</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Seeing creatures", FormatPercent(Share(snapshot.CreatureDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Attacking this tick", FormatPercent(Share(snapshot.AttackingCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Attack damage", $"{snapshot.TotalAttackDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Damage per attacker", $"{attackDamagePerAttacker:0.###} health/s");
        WriteMetric(writer, "Injury deaths", state.Stats.InjuryDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Rotten meat deaths", state.Stats.RottenMeatDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Fresh kill share", FormatPercent(snapshot.FreshKillCaloriesEatenShare));
        WriteMetric(writer, "Meat raw share", FormatPercent(snapshot.MeatCaloriesEatenShare));
        WriteMetric(writer, "Fresh meat share", FormatPercent(snapshot.FreshMeatCaloriesEatenShare));
        WriteMetric(writer, "Stale meat share", FormatPercent(snapshot.StaleMeatCaloriesEatenShare));
        WriteMetric(writer, "Rotten damage", $"{snapshot.TotalRottenMeatDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Rotten affected", FormatPercent(Share(snapshot.RottenMeatDamagedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Meat energy share", FormatPercent(snapshot.MeatDigestedEnergyShare));
        WriteMetric(writer, "Average diet", snapshot.AverageDietaryAdaptation.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average carrion", snapshot.AverageCarrionAdaptation.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average bite", snapshot.AverageBiteStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average resistance", snapshot.AverageDamageResistance.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Attacker diet", snapshot.AttackerAverageDietaryAdaptation.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Attacker bite", snapshot.AttackerAverageBiteStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Attacker resistance", snapshot.AttackerAverageDamageResistance.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Non-attacker diet", snapshot.NonAttackerAverageDietaryAdaptation.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Non-attacker bite", snapshot.NonAttackerAverageBiteStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Non-attacker resistance", snapshot.NonAttackerAverageDamageResistance.ToString("0.###", CultureInfo.InvariantCulture));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        var startingGenome = CreatureGenome.Baseline with
        {
            DietaryAdaptation = scenario.DietaryAdaptation,
            CarrionAdaptation = scenario.CarrionAdaptation
        };

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Pressure Settings</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Initial creatures", scenario.InitialCreatureCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Initial creature spawn", scenario.InitialCreatureSpawnRegion.ToString());
        WriteMetric(writer, "World sense interval", $"{scenario.WorldSenseIntervalTicks} ticks");
        WriteMetric(writer, "Close sense refresh", FormatPercent(scenario.CloseSenseRefreshProximity));
        WriteMetric(writer, "Initial resource density", $"{scenario.InitialResourcesPerMillionArea:0.###} per 1M area");
        WriteMetric(writer, "Initial resource patches", scenario.CalculateInitialResourceCount().ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Biomes", scenario.EnableBiomes ? "Enabled" : "Disabled");
        WriteMetric(writer, "Biome map", scenario.BiomeMapKind.ToString());
        WriteMetric(writer, "Biome cell size", scenario.BiomeCellSize.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Obstacles", scenario.EnableObstacles ? "Enabled" : "Disabled");
        WriteMetric(writer, "Obstacle map", scenario.ObstacleMapKind.ToString());
        WriteMetric(writer, "Obstacle cell size", scenario.ObstacleCellSize.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Resource void border", $"{scenario.ResourceVoidBorderWidth:0.###} world units");
        WriteMetric(writer, "Resource calories", FormatRange(scenario.ResourceCaloriesMin, scenario.ResourceCaloriesMax));
        WriteMetric(writer, "Resource regrowth", $"{FormatRange(scenario.ResourceRegrowthMin, scenario.ResourceRegrowthMax)} kcal/s");
        WriteMetric(writer, "Depleted resources relocate", scenario.RelocateDepletedResources ? "Yes" : "No");
        WriteMetric(writer, "Plant respawn delay", $"{FormatRange(scenario.PlantRespawnDelaySecondsMin, scenario.PlantRespawnDelaySecondsMax)} seconds");
        WriteMetric(writer, "Resource clustering", FormatPercent(scenario.ResourceClusterStrength));
        WriteMetric(writer, "Resource cluster radius", $"{scenario.ResourceClusterRadius:0.###} world units");
        WriteMetric(writer, "Plant local dispersal", FormatPercent(scenario.PlantLocalDispersalChance));
        WriteMetric(writer, "Plant local dispersal radius", $"{scenario.PlantLocalDispersalRadius:0.###} world units");
        WriteMetric(writer, "Local fertility", scenario.EnableLocalFertility ? "Enabled" : "Disabled");
        WriteMetric(writer, "Local fertility cell size", $"{scenario.LocalFertilityCellSize:0.###} world units");
        WriteMetric(writer, "Local fertility minimum", $"{scenario.LocalFertilityMinimumMultiplier:0.###}x");
        WriteMetric(writer, "Local fertility recovery", $"{scenario.LocalFertilityRecoveryPerSecond:0.######}/s");
        WriteMetric(writer, "Local fertility depletion", $"{scenario.LocalFertilityDepletionPerPlant:0.###}x per plant");
        WriteMetric(writer, "Local fertility spread", FormatPercent(scenario.LocalFertilityNeighborDepletionShare));
        WriteMetric(writer, "Biome season response", FormatBiomePressureProfile(scenario.CreateBiomeSeasonalAmplitudeProfile()));
        WriteMetric(writer, "Biome movement costs", FormatBiomePressureProfile(scenario.CreateBiomeMovementCostProfile()));
        WriteMetric(writer, "Biome basal costs", FormatBiomePressureProfile(scenario.CreateBiomeBasalCostProfile()));
        WriteMetric(writer, "Biome speed", FormatBiomePressureProfile(scenario.CreateBiomeSpeedProfile()));
        WriteMetric(writer, "Basal upkeep", $"{scenario.BasalEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Body radius upkeep", $"{scenario.BodyRadiusEnergyCostPerSecond:0.###} energy/radius/s");
        WriteMetric(writer, "Max speed upkeep", $"{scenario.MaxSpeedEnergyCostPerSecond:0.######} energy/speed/s");
        WriteMetric(writer, "Turn rate upkeep", $"{scenario.TurnRateEnergyCostPerSecond:0.######} energy/rad/s/s");
        WriteMetric(writer, "Sense radius upkeep", $"{scenario.SenseRadiusEnergyCostPerSecond:0.######} energy/radius/s");
        WriteMetric(writer, "Vision angle", $"{ToDegrees(scenario.VisionAngleRadians):0.###} degrees");
        WriteMetric(writer, "Vision angle upkeep", $"{scenario.VisionAngleEnergyCostPerSecond:0.######} energy/radian/s");
        WriteMetric(writer, "Eat rate upkeep", $"{scenario.EatRateEnergyCostPerSecond:0.######} energy/rate/s");
        WriteMetric(writer, "Gut capacity upkeep", $"{scenario.GutCapacityEnergyCostPerSecond:0.######} energy/capacity/s");
        WriteMetric(writer, "Digestion rate upkeep", $"{scenario.DigestionRateEnergyCostPerSecond:0.######} energy/rate/s");
        WriteMetric(writer, "Bite strength upkeep", $"{scenario.BiteStrengthEnergyCostPerSecond:0.######} energy/strength/s");
        WriteMetric(writer, "Damage resistance upkeep", $"{scenario.DamageResistanceEnergyCostPerSecond:0.######} energy/resistance/s");
        WriteMetric(writer, "Active memory upkeep", $"{scenario.MemoryEnergyCostPerSecond:0.######} energy/full-memory/s");
        WriteMetric(writer, "Memory decay", $"{scenario.MemoryDecayPerSecond:0.######}/s");
        WriteMetric(writer, "Memory write rate", $"{scenario.MemoryWriteRatePerSecond:0.######}/s");
        WriteMetric(writer, "Egg upkeep", $"{scenario.EggEnergyCostPerSecond:0.######} energy/egg/s");
        WriteMetric(writer, "Egg exposure damage", $"{scenario.EggEnvironmentalDamagePerSecond:0.######} health/s");
        WriteMetric(writer, "Movement upkeep", $"{scenario.MovementEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Movement speed cost exponent", scenario.MovementSpeedCostExponent.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Eat rate", $"{scenario.EatCaloriesPerSecond:0.###} kcal/s");
        WriteMetric(writer, "Gut capacity", $"{scenario.GutCapacityCalories:0.###} kcal");
        WriteMetric(writer, "Digestion rate", $"{scenario.DigestionCaloriesPerSecond:0.###} kcal/s");
        WriteMetric(writer, "Egg production", $"{scenario.EggProductionEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Egg incubation", $"{scenario.EggIncubationSeconds:0.###} seconds");
        WriteMetric(writer, "Maturity age", $"{scenario.MaturityAgeSeconds:0.###} seconds");
        WriteMetric(writer, "Prime fertility age", $"{scenario.ReproductivePrimeAgeSeconds:0.###} seconds");
        WriteMetric(writer, "Senescence age", $"{scenario.ReproductiveSenescenceAgeSeconds:0.###} seconds");
        WriteMetric(writer, "Senescent fertility", FormatPercent(scenario.SenescentFertilityMultiplier));
        WriteMetric(writer, "Crowding fertility penalty", FormatPercent(scenario.CrowdingFertilityPenalty));
        WriteMetric(writer, "Starting diet", $"{scenario.DietaryAdaptation:0.###} meat bias");
        WriteMetric(writer, "Starting carrion", $"{scenario.CarrionAdaptation:0.###} stale-meat bias");
        WriteMetric(writer, "Starting bite strength", scenario.BiteStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Starting damage resistance", scenario.DamageResistance.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Starting plant digestion", FormatPercent(CreatureDigestion.PlantEfficiency(startingGenome)));
        WriteMetric(writer, "Starting meat digestion", FormatPercent(CreatureDigestion.MeatEfficiency(startingGenome)));
        WriteMetric(writer, "Starting fresh meat digestion", FormatPercent(CreatureDigestion.FreshMeatEnergyEfficiency(startingGenome)));
        WriteMetric(writer, "Starting stale meat digestion", FormatPercent(CreatureDigestion.StaleMeatEnergyEfficiency(startingGenome)));
        WriteMetric(writer, "Death meat body calories", $"{scenario.DeathMeatCaloriesPerBodyRadius:0.###} kcal/radius");
        WriteMetric(writer, "Death meat energy fraction", FormatPercent(scenario.DeathMeatEnergyFraction));
        WriteMetric(writer, "Meat decay", $"{scenario.MeatDecayCaloriesPerSecond:0.###} kcal/s");
        WriteMetric(writer, "Rotten meat damage", $"{scenario.RottenMeatDamagePerRawKcal:0.####} health/raw kcal");
        WriteMetric(writer, "Meat scent range", $"{scenario.MeatScentRangeMultiplier:0.###}x vision");
        WriteMetric(writer, "Meat scent full strength", $"{scenario.MeatScentCaloriesForFullStrength:0.###} kcal");
        WriteMetric(writer, "Meat scent saturation", scenario.MeatScentDensitySaturation.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Bite damage", $"{scenario.BiteDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Bite energy cost", $"{scenario.BiteEnergyCostPerSecond:0.###} energy/s");
        WriteMetric(writer, "Bite reach", $"{scenario.BiteRangePadding:0.###} world units");
        WriteMetric(writer, "Mutation strength", scenario.MutationStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Trait mutation rate", FormatPercent(scenario.TraitMutationRate));
        WriteMetric(writer, "Brain mutation rate", FormatPercent(scenario.BrainMutationRate));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Biomes</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Biome</th><th>Area Share</th><th>Density Mult</th><th>Regrowth Mult</th><th>Season Amp</th><th>Move Cost</th><th>Basal Cost</th><th>Speed</th><th>Resources</th><th>Resources/M</th><th>Calories</th><th>Living</th><th>Living Share</th></tr></thead>");
        writer.WriteLine("<tbody>");
        var movementCostProfile = scenario.CreateBiomeMovementCostProfile();
        var basalCostProfile = scenario.CreateBiomeBasalCostProfile();
        var speedProfile = scenario.CreateBiomeSpeedProfile();
        var seasonalAmplitudeProfile = scenario.CreateBiomeSeasonalAmplitudeProfile();
        foreach (var summary in biomeSummaries)
        {
            var resourcesPerMillion = summary.Area > 0f
                ? summary.ResourceCount / summary.Area * SimulationScenario.ResourceDensityAreaUnits
                : 0f;
            var livingCreatureCount = CreatureCountForBiome(snapshot, summary.Kind);
            var movementCost = movementCostProfile.For(summary.Kind).ToString("0.###", CultureInfo.InvariantCulture);
            var basalCost = basalCostProfile.For(summary.Kind).ToString("0.###", CultureInfo.InvariantCulture);
            var speed = speedProfile.For(summary.Kind).ToString("0.###", CultureInfo.InvariantCulture);
            var seasonalAmplitude = seasonalAmplitudeProfile.For(summary.Kind).ToString("0.###", CultureInfo.InvariantCulture);
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.Kind)}</td>" +
                $"<td>{Html(FormatPercent(summary.Area / worldArea))}</td>" +
                $"<td>{Html(summary.ResourceDensityMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ResourceRegrowthMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html($"{seasonalAmplitude}x")}</td>" +
                $"<td>{Html($"{movementCost}x")}</td>" +
                $"<td>{Html($"{basalCost}x")}</td>" +
                $"<td>{Html($"{speed}x")}</td>" +
                $"<td>{Html(summary.ResourceCount)}</td>" +
                $"<td>{Html(resourcesPerMillion.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ResourceCalories.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(livingCreatureCount)}</td>" +
                $"<td>{Html(FormatPercent(Share(livingCreatureCount, snapshot.CreatureCount)))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        WriteChartsSection(writer, state.Stats.Snapshots);
        WriteSpeciesClusterSection(writer, speciesSummaries);
        WriteSpeciesBehaviorFingerprintSection(writer, speciesBehaviorFingerprints);
        WriteSpeciesBrainInputDiagnosticsSection(writer, speciesBrainInputDiagnostics);
        WriteSpeciesBehaviorChangeSection(writer, speciesBehaviorChanges);
        WriteSpeciesClusterInterpretationSection(writer, SpeciesClusterAnalyzer.InterpretClusters(speciesSummaries, speciesHistory, 10));
        WriteSpeciesClusterHistorySection(writer, speciesHistory);
        WriteBehaviorAssaySection(writer, behaviorSummary);
        WriteLineageBehaviorAssaySection(writer, lineageBehaviorSummaries);
        WriteBrainInputDiagnosticsSection(writer, brainInputDiagnostics);
        WriteLineageBrainInputDiagnosticsSection(writer, lineageBrainInputDiagnostics);

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Final Living Traits</h2>");
        if (traitSummary.Count == 0)
        {
            writer.WriteLine("<p>No living creatures remain.</p>");
        }
        else
        {
            writer.WriteLine("<div class=\"table-wrap\"><table>");
            writer.WriteLine("<thead><tr><th>Trait</th><th>Average</th><th>Min</th><th>Max</th></tr></thead>");
            writer.WriteLine("<tbody>");
            WriteTraitRow(writer, "Body radius", traitSummary.BodyRadius);
            WriteTraitRow(writer, "Max speed", traitSummary.MaxSpeed);
            WriteTraitRow(writer, "Vision range", traitSummary.SenseRadius);
            WriteDegreesTraitRow(writer, "Vision angle degrees", traitSummary.VisionAngleRadians);
            WriteTraitRow(writer, "Reproduction threshold", traitSummary.ReproductionThreshold);
            WriteTraitRow(writer, "Offspring investment", traitSummary.OffspringInvestment);
            WriteTraitRow(writer, "Egg production per second", traitSummary.EggProductionEnergyPerSecond);
            WriteTraitRow(writer, "Egg incubation seconds", traitSummary.EggIncubationSeconds);
            WriteTraitRow(writer, "Maturity age seconds", traitSummary.MaturityAgeSeconds);
            WriteTraitRow(writer, "Dietary adaptation meat bias", traitSummary.DietaryAdaptation);
            WriteTraitRow(writer, "Carrion adaptation stale bias", traitSummary.CarrionAdaptation);
            WriteTraitRow(writer, "Plant digestion efficiency", traitSummary.PlantDigestion);
            WriteTraitRow(writer, "Meat digestion efficiency", traitSummary.MeatDigestion);
            WriteTraitRow(writer, "Fresh meat digestion efficiency", traitSummary.FreshMeatDigestion);
            WriteTraitRow(writer, "Stale meat digestion efficiency", traitSummary.StaleMeatDigestion);
            WriteTraitRow(writer, "Gut capacity", traitSummary.GutCapacityCalories);
            WriteTraitRow(writer, "Digestion rate", traitSummary.DigestionCaloriesPerSecond);
            WriteTraitRow(writer, "Bite strength", traitSummary.BiteStrength);
            WriteTraitRow(writer, "Damage resistance", traitSummary.DamageResistance);
            WriteTraitRow(writer, "Mutation strength", traitSummary.MutationStrength);
            WriteTraitRow(writer, "Trait mutation rate", traitSummary.TraitMutationRate);
            WriteTraitRow(writer, "Brain mutation rate", traitSummary.BrainMutationRate);
            writer.WriteLine("</tbody></table></div>");
        }

        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Top Founder Lineages</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Founder</th><th>Total Creatures</th><th>Living</th><th>Dead</th><th>Max Generation</th><th>Living Share</th></tr></thead>");
        writer.WriteLine("<tbody>");
        if (founderSummaries.Length == 0)
        {
            writer.WriteLine("<tr><td class=\"empty\" colspan=\"6\">No lineage records are present.</td></tr>");
        }
        else
        {
            foreach (var summary in founderSummaries)
            {
                var livingShare = state.Creatures.Count > 0
                    ? summary.LivingCreatures / (float)state.Creatures.Count
                    : 0f;
                writer.WriteLine(
                    "<tr>" +
                    $"<td>#{Html(summary.FounderId.Value)}</td>" +
                    $"<td>{Html(summary.TotalCreatures)}</td>" +
                    $"<td>{Html(summary.LivingCreatures)}</td>" +
                    $"<td>{Html(summary.DeadCreatures)}</td>" +
                    $"<td>{Html(summary.MaxGeneration)}</td>" +
                    $"<td>{Html($"{livingShare * 100f:0.0}%")}</td>" +
                    "</tr>");
            }
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        writer.WriteLine("</main>");
        WriteDocumentEnd(writer);
    }

    private static TraitSummary SummarizeTraits(WorldState state)
    {
        var summary = new TraitSummary();
        foreach (var creature in state.Creatures)
        {
            var genome = state.GetGenome(creature.GenomeId);
            summary.BodyRadius.Add(genome.BodyRadius);
            summary.MaxSpeed.Add(genome.MaxSpeed);
            summary.SenseRadius.Add(genome.SenseRadius);
            summary.VisionAngleRadians.Add(genome.VisionAngleRadians);
            summary.ReproductionThreshold.Add(genome.ReproductionEnergyThreshold);
            summary.OffspringInvestment.Add(genome.OffspringEnergyInvestment);
            summary.EggProductionEnergyPerSecond.Add(genome.EggProductionEnergyPerSecond);
            summary.EggIncubationSeconds.Add(genome.EggIncubationSeconds);
            summary.MaturityAgeSeconds.Add(genome.MaturityAgeSeconds);
            summary.DietaryAdaptation.Add(genome.DietaryAdaptation);
            summary.CarrionAdaptation.Add(genome.CarrionAdaptation);
            summary.PlantDigestion.Add(CreatureDigestion.PlantEfficiency(genome));
            summary.MeatDigestion.Add(CreatureDigestion.MeatEfficiency(genome));
            summary.FreshMeatDigestion.Add(CreatureDigestion.FreshMeatEnergyEfficiency(genome));
            summary.StaleMeatDigestion.Add(CreatureDigestion.StaleMeatEnergyEfficiency(genome));
            summary.GutCapacityCalories.Add(genome.GutCapacityCalories);
            summary.DigestionCaloriesPerSecond.Add(genome.DigestionCaloriesPerSecond);
            summary.BiteStrength.Add(genome.BiteStrength);
            summary.DamageResistance.Add(genome.DamageResistance);
            summary.MutationStrength.Add(genome.MutationStrength);
            summary.TraitMutationRate.Add(genome.TraitMutationRate);
            summary.BrainMutationRate.Add(genome.BrainMutationRate);
            summary.Count++;
        }

        return summary;
    }

    private static IReadOnlyList<FounderSummary> SummarizeFounders(IReadOnlyList<CreatureLineageRecord> records)
    {
        var byId = records.ToDictionary(record => record.Id);
        var summaries = new Dictionary<EntityId, MutableFounderSummary>();

        foreach (var record in records)
        {
            var founderId = FindFounderId(record, byId);
            if (!summaries.TryGetValue(founderId, out var summary))
            {
                summary = new MutableFounderSummary { FounderId = founderId };
                summaries.Add(founderId, summary);
            }

            summary.TotalCreatures++;
            summary.LivingCreatures += record.IsAlive ? 1 : 0;
            summary.DeadCreatures += record.IsAlive ? 0 : 1;
            summary.MaxGeneration = Math.Max(summary.MaxGeneration, record.Generation);
        }

        return summaries.Values
            .Select(summary => new FounderSummary(
                summary.FounderId,
                summary.TotalCreatures,
                summary.LivingCreatures,
                summary.DeadCreatures,
                summary.MaxGeneration))
            .ToArray();
    }

    private static EntityId FindFounderId(
        CreatureLineageRecord record,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> byId)
    {
        var current = record;
        for (var depth = 0; depth < 512; depth++)
        {
            if (current.IsFounder || !byId.TryGetValue(current.ParentId, out var parent))
            {
                return current.Id;
            }

            current = parent;
        }

        return record.Id;
    }

    private static void WriteDocumentStart(StreamWriter writer, string title)
    {
        writer.WriteLine("<!doctype html>");
        writer.WriteLine("<html lang=\"en\">");
        writer.WriteLine("<head>");
        writer.WriteLine("<meta charset=\"utf-8\">");
        writer.WriteLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        writer.WriteLine($"<title>{Html(title)}</title>");
        writer.WriteLine(
            """
            <style>
            :root {
              color-scheme: light;
              --bg: #f6f7f2;
              --text: #172015;
              --muted: #62705e;
              --panel: #ffffff;
              --line: #dfe5d9;
              --accent: #2f7d45;
            }
            body {
              margin: 0;
              background: var(--bg);
              color: var(--text);
              font-family: "Segoe UI", system-ui, sans-serif;
              line-height: 1.45;
            }
            header {
              padding: 34px 0 24px;
              background: #162015;
              color: #f4f7ef;
            }
            .page-width {
              width: min(1120px, calc(100% - 32px));
              margin: 0 auto;
            }
            .eyebrow {
              margin: 0 0 6px;
              color: #a9c9aa;
              font-size: 0.78rem;
              letter-spacing: 0;
              text-transform: uppercase;
            }
            h1, h2 { margin: 0; }
            h1 { font-size: 2rem; }
            h2 { margin-bottom: 14px; font-size: 1.15rem; }
            main { padding: 22px 0 40px; }
            section {
              margin-top: 16px;
              padding: 18px;
              background: var(--panel);
              border: 1px solid var(--line);
              border-radius: 8px;
            }
            .metric-grid {
              display: grid;
              grid-template-columns: repeat(auto-fit, minmax(190px, 1fr));
              gap: 10px;
            }
            .metric {
              padding: 10px 12px;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #fbfcf8;
            }
            .metric-label {
              color: var(--muted);
              font-size: 0.75rem;
              text-transform: uppercase;
            }
            .metric-value {
              display: block;
              margin-top: 4px;
              overflow-wrap: anywhere;
              font-weight: 650;
            }
            .chart-grid {
              display: grid;
              grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
              gap: 14px;
            }
            .chart-card {
              padding: 12px;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #fbfcf8;
            }
            .chart-card h3 {
              margin: 0 0 8px;
              font-size: 0.95rem;
            }
            .chart-card svg {
              display: block;
              width: 100%;
              height: auto;
              overflow: visible;
            }
            .chart-axis {
              stroke: #9daa95;
              stroke-width: 1;
            }
            .chart-label {
              fill: var(--muted);
              font-size: 11px;
            }
            .chart-legend {
              display: flex;
              flex-wrap: wrap;
              gap: 8px 14px;
              color: var(--muted);
              font-size: 0.82rem;
            }
            .legend-swatch {
              display: inline-block;
              width: 0.8em;
              height: 0.8em;
              margin-right: 5px;
              border-radius: 999px;
              vertical-align: -0.05em;
            }
            .table-wrap { overflow-x: auto; }
            table {
              width: 100%;
              border-collapse: collapse;
              font-size: 0.92rem;
            }
            th, td {
              padding: 8px 10px;
              border-bottom: 1px solid var(--line);
              text-align: right;
              white-space: nowrap;
            }
            th:first-child, td:first-child { text-align: left; }
            th {
              color: var(--muted);
              font-size: 0.76rem;
              text-transform: uppercase;
            }
            .empty { color: var(--muted); text-align: left; }
            </style>
            """);
        writer.WriteLine("</head>");
        writer.WriteLine("<body>");
    }

    private static void WriteDocumentEnd(StreamWriter writer)
    {
        writer.WriteLine("</body>");
        writer.WriteLine("</html>");
    }

    private static void WriteMetric(StreamWriter writer, string label, string value)
    {
        writer.WriteLine("<div class=\"metric\">");
        writer.WriteLine($"<span class=\"metric-label\">{Html(label)}</span>");
        writer.WriteLine($"<span class=\"metric-value\">{Html(value)}</span>");
        writer.WriteLine("</div>");
    }

    private static void WriteSeasonPressureSection(
        StreamWriter writer,
        SeasonPressureSummary summary,
        int livingFounderLineages,
        int totalFounderLineages)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Season Pressure</h2>");
        if (!summary.Enabled)
        {
            writer.WriteLine("<p class=\"empty\">Seasons are disabled for this scenario.</p>");
            writer.WriteLine("</section>");
            return;
        }

        if (summary.SnapshotCount == 0)
        {
            writer.WriteLine("<p class=\"empty\">No stats snapshots were recorded, so season pressure could not be analyzed.</p>");
            writer.WriteLine("</section>");
            return;
        }

        var low = summary.LowFertility;
        var high = summary.HighFertility;
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Cycles observed", summary.CyclesObserved.ToString("0.##", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Fertility range", FormatFertilityRange(summary));
        WriteMetric(writer, "Low fertility population", FormatSeasonPopulation(low));
        WriteMetric(writer, "High fertility population", FormatSeasonPopulation(high));
        WriteMetric(writer, "Low fertility plants", FormatSeasonPlantCalories(low));
        WriteMetric(writer, "High fertility plants", FormatSeasonPlantCalories(high));
        WriteMetric(writer, "Low starvation rate", FormatSeasonRate(low, band => band.StarvationDeathsPerSecond));
        WriteMetric(writer, "High starvation rate", FormatSeasonRate(high, band => band.StarvationDeathsPerSecond));
        WriteMetric(writer, "Low eggs laid rate", FormatSeasonRate(low, band => band.EggsLaidPerSecond));
        WriteMetric(writer, "High eggs laid rate", FormatSeasonRate(high, band => band.EggsLaidPerSecond));
        WriteMetric(writer, "Living founder lineages", totalFounderLineages > 0 ? $"{livingFounderLineages} / {totalFounderLineages}" : "n/a");
        writer.WriteLine("</div>");

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Season phase</th><th>Samples</th><th>Avg fertility</th><th>Avg pop</th><th>Min pop</th><th>Avg plant kcal</th><th>Min plant kcal</th><th>Births/s</th><th>Eggs/s</th><th>Deaths/s</th><th>Starvation/s</th><th>Food seen</th><th>Meal gap</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var bin in summary.PhaseBins)
        {
            WriteSeasonPressureRow(writer, bin);
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSeasonPressureRow(StreamWriter writer, SeasonPressureBand bin)
    {
        if (bin.SampleCount == 0)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(bin.Label)}</td>" +
                $"<td>{Html(bin.SampleCount)}</td>" +
                "<td colspan=\"11\" class=\"empty\">No snapshots in this phase.</td>" +
                "</tr>");
            return;
        }

        writer.WriteLine(
            "<tr>" +
            $"<td>{Html(bin.Label)}</td>" +
            $"<td>{Html(bin.SampleCount)}</td>" +
            $"<td>{Html($"{bin.AverageFertility:0.###}x")}</td>" +
            $"<td>{Html(bin.AveragePopulation.ToString("0.#", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(bin.MinPopulation)}</td>" +
            $"<td>{Html(bin.AveragePlantCalories.ToString("0.#", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(bin.MinPlantCalories.ToString("0.#", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(bin.BirthsPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(bin.EggsLaidPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(bin.DeathsPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(bin.StarvationDeathsPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(FormatPercent(bin.AverageFoodSeenShare))}</td>" +
            $"<td>{Html($"{bin.AverageMealGapSeconds:0.#}s")}</td>" +
            "</tr>");
    }

    private static string FormatSeasonPopulation(SeasonPressureBand band)
    {
        return band.SampleCount > 0
            ? $"{band.AveragePopulation:0.#} avg, {band.MinPopulation} min"
            : "No samples";
    }

    private static string FormatSeasonPlantCalories(SeasonPressureBand band)
    {
        return band.SampleCount > 0
            ? $"{band.AveragePlantCalories:0} kcal avg, {band.MinPlantCalories:0} min"
            : "No samples";
    }

    private static string FormatSeasonRate(SeasonPressureBand band, Func<SeasonPressureBand, float> selector)
    {
        return band.SampleCount > 0
            ? $"{selector(band):0.###}/s"
            : "No samples";
    }

    private static string FormatFertilityRange(SeasonPressureSummary summary)
    {
        var observed = summary.PhaseBins
            .Where(bin => bin.SampleCount > 0)
            .ToArray();
        if (observed.Length == 0)
        {
            return "n/a";
        }

        return $"{observed.Min(bin => bin.MinFertility):0.###}x-{observed.Max(bin => bin.MaxFertility):0.###}x";
    }

    private static void WriteChartsSection(StreamWriter writer, IReadOnlyList<SimulationStatsSnapshot> snapshots)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Graphs</h2>");
        if (snapshots.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No stats snapshots were recorded, so no graphs are available.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"chart-grid\">");
        WriteLineChart(
            writer,
            "Population and eggs",
            "",
            snapshots,
            new ChartSeries("Creatures", "#2f7d4f", snapshots.Select(snapshot => (float)snapshot.CreatureCount).ToArray()),
            new ChartSeries("Eggs", "#d69d2f", snapshots.Select(snapshot => (float)snapshot.EggCount).ToArray()));
        WriteLineChart(
            writer,
            "Dead-creature lifespan",
            " s",
            snapshots,
            new ChartSeries("Average", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageLifespanSeconds).ToArray()),
            new ChartSeries("Median", "#8f4cb8", snapshots.Select(snapshot => snapshot.MedianLifespanSeconds).ToArray()));
        WriteLineChart(
            writer,
            "Reproduction state",
            "%",
            snapshots,
            new ChartSeries("Intent", "#d69d2f", snapshots.Select(snapshot => Share(snapshot.ReproductionIntentCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Ready", "#8f4cb8", snapshots.Select(snapshot => Share(snapshot.ReproductionReadyCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Reserve", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageEggReserveRatio * 100f).ToArray()),
            new ChartSeries("Surplus", "#2f7d4f", snapshots.Select(snapshot => snapshot.AverageEnergySurplusRatio * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Resource calories",
            " kcal",
            snapshots,
            new ChartSeries("Plants", "#35a862", snapshots.Select(snapshot => snapshot.TotalPlantCalories).ToArray()),
            new ChartSeries("Meat", "#b84a4a", snapshots.Select(snapshot => snapshot.TotalMeatCalories).ToArray()));
        WriteLineChart(
            writer,
            "Plant patch structure",
            "%",
            snapshots,
            new ChartSeries("Occupied cells", "#35a862", snapshots.Select(snapshot => snapshot.PlantPatchOccupiedCellShare * 100f).ToArray()),
            new ChartSeries("Top decile kcal", "#d69d2f", snapshots.Select(snapshot => snapshot.PlantPatchTopDecileCaloriesShare * 100f).ToArray()),
            new ChartSeries("Patchiness", "#8f4cb8", snapshots.Select(snapshot => snapshot.PlantPatchiness * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Local fertility",
            "%",
            snapshots,
            new ChartSeries("Average", "#35a862", snapshots.Select(snapshot => snapshot.AverageLocalFertilityMultiplier * 100f).ToArray()),
            new ChartSeries("Minimum", "#d69d2f", snapshots.Select(snapshot => snapshot.MinimumLocalFertilityMultiplier * 100f).ToArray()),
            new ChartSeries("Depleted cells", "#8f4cb8", snapshots.Select(snapshot => snapshot.DepletedLocalFertilityCellShare * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Season fertility",
            "x",
            snapshots,
            new ChartSeries("Global", "#6a8fce", snapshots.Select(snapshot => snapshot.SeasonFertilityMultiplier).ToArray()),
            new ChartSeries("Left", "#35a862", snapshots.Select(snapshot => snapshot.LeftRegionSeasonFertilityMultiplier).ToArray()),
            new ChartSeries("Middle", "#d69d2f", snapshots.Select(snapshot => snapshot.MiddleRegionSeasonFertilityMultiplier).ToArray()),
            new ChartSeries("Right", "#8f4cb8", snapshots.Select(snapshot => snapshot.RightRegionSeasonFertilityMultiplier).ToArray()));
        WriteLineChart(
            writer,
            "Migration regions",
            "%",
            snapshots,
            new ChartSeries("Left", "#35a862", snapshots.Select(snapshot => Share(snapshot.LeftRegionCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Middle", "#d69d2f", snapshots.Select(snapshot => Share(snapshot.MiddleRegionCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Right", "#8f4cb8", snapshots.Select(snapshot => Share(snapshot.RightRegionCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Regional plant calories",
            " kcal",
            snapshots,
            new ChartSeries("Left", "#35a862", snapshots.Select(snapshot => snapshot.LeftRegionPlantCalories).ToArray()),
            new ChartSeries("Middle", "#d69d2f", snapshots.Select(snapshot => snapshot.MiddleRegionPlantCalories).ToArray()),
            new ChartSeries("Right", "#8f4cb8", snapshots.Select(snapshot => snapshot.RightRegionPlantCalories).ToArray()));
        WriteLineChart(
            writer,
            "Biome occupancy",
            "%",
            snapshots,
            new ChartSeries("Barren", "#9a6b3b", snapshots.Select(snapshot => Share(snapshot.BarrenCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Sparse", "#7f8f3a", snapshots.Select(snapshot => Share(snapshot.SparseCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Grassland", "#35a862", snapshots.Select(snapshot => Share(snapshot.GrasslandCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => Share(snapshot.RichCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Biome pressure",
            "x",
            snapshots,
            new ChartSeries("Move cost", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageBiomeMovementCostMultiplier).ToArray()),
            new ChartSeries("Basal cost", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageBiomeBasalCostMultiplier).ToArray()),
            new ChartSeries("Speed", "#2f7d4f", snapshots.Select(snapshot => snapshot.AverageBiomeSpeedMultiplier).ToArray()));
        WriteLineChart(
            writer,
            "Foraging signals",
            "%",
            snapshots,
            new ChartSeries("Seeing food", "#2f7d4f", snapshots.Select(snapshot => Share(snapshot.FoodDetectedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Smelling meat", "#b84a4a", snapshots.Select(snapshot => Share(snapshot.MeatScentDetectedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Smelling rot", "#7d5546", snapshots.Select(snapshot => Share(snapshot.RottenMeatScentDetectedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Touching food", "#6a8fce", snapshots.Select(snapshot => Share(snapshot.FoodContactCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Eating", "#d69d2f", snapshots.Select(snapshot => Share(snapshot.EatingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Obstacle pressure",
            "%",
            snapshots,
            new ChartSeries("Sensed", "#6a8fce", snapshots.Select(snapshot => Share(snapshot.ObstacleSensedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Blocked", "#d96b3b", snapshots.Select(snapshot => Share(snapshot.ObstacleBlockedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Forward cue", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageForwardObstacle * 100f).ToArray()),
            new ChartSeries("Side cue", "#2f7d4f", snapshots.Select(snapshot => MathF.Max(snapshot.AverageLeftObstacle, snapshot.AverageRightObstacle) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Search Efficiency",
            "",
            snapshots,
            new ChartSeries("Distance/s", "#6a8fce", snapshots.Select(snapshot => snapshot.TotalDistanceTraveledPerSecond).ToArray()),
            new ChartSeries("Meal distance", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageDistanceSinceLastMeal).ToArray()),
            new ChartSeries("Raw kcal/unit", "#2f7d4f", snapshots.Select(snapshot => snapshot.CaloriesEatenPerDistance).ToArray()),
            new ChartSeries("Raw kcal/vision", "#8f4cb8", snapshots.Select(snapshot => snapshot.CaloriesEatenPerFoodVisionEvent).ToArray()));
        WriteLineChart(
            writer,
            "Combat pressure",
            "",
            snapshots,
            new ChartSeries("Attacking %", "#d96b3b", snapshots.Select(snapshot => Share(snapshot.AttackingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Attack damage", "#9d3434", snapshots.Select(snapshot => snapshot.TotalAttackDamagePerSecond).ToArray()));
        WriteLineChart(
            writer,
            "Digestion",
            "",
            snapshots,
            new ChartSeries("Raw eaten/s", "#d69d2f", snapshots.Select(snapshot => snapshot.TotalCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Digested/s", "#2f7d4f", snapshots.Select(snapshot => snapshot.TotalCaloriesDigestedPerSecond).ToArray()),
            new ChartSeries("Gut fullness %", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageGutFillRatio * 100f).ToArray()),
            new ChartSeries("Rotten dmg/s", "#7d5546", snapshots.Select(snapshot => snapshot.TotalRottenMeatDamagePerSecond).ToArray()));
        WriteLineChart(
            writer,
            "Food Source Intake",
            " kcal/s",
            snapshots,
            new ChartSeries("Plant eaten/s", "#35a862", snapshots.Select(snapshot => snapshot.TotalPlantCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Carcass eaten/s", "#b84a4a", snapshots.Select(snapshot => snapshot.TotalCarcassCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Fresh meat/s", "#e05a47", snapshots.Select(snapshot => snapshot.TotalFreshMeatCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Stale meat/s", "#7d5546", snapshots.Select(snapshot => snapshot.TotalStaleMeatCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Egg eaten/s", "#d69d2f", snapshots.Select(snapshot => snapshot.TotalEggCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Fresh kill eaten/s", "#8f4cb8", snapshots.Select(snapshot => snapshot.TotalLivePreyCaloriesEatenPerSecond).ToArray()));
        WriteLineChart(
            writer,
            "Predation Diagnostics",
            "%",
            snapshots,
            new ChartSeries("Seeing creatures", "#6a8fce", snapshots.Select(snapshot => Share(snapshot.CreatureDetectedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Attacking", "#e05a47", snapshots.Select(snapshot => Share(snapshot.AttackingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Fresh kill share", "#8f4cb8", snapshots.Select(snapshot => snapshot.FreshKillCaloriesEatenShare * 100f).ToArray()),
            new ChartSeries("Fresh meat share", "#d69d2f", snapshots.Select(snapshot => snapshot.FreshMeatCaloriesEatenShare * 100f).ToArray()),
            new ChartSeries("Meat energy share", "#b84a4a", snapshots.Select(snapshot => snapshot.MeatDigestedEnergyShare * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Meat Freshness",
            "%",
            snapshots,
            new ChartSeries("Avg freshness", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageMeatFreshness * 100f).ToArray()),
            new ChartSeries("Visible freshness", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageVisibleMeatFreshness * 100f).ToArray()),
            new ChartSeries("Fresh eaten share", "#35a862", snapshots.Select(snapshot => snapshot.FreshMeatCaloriesEatenShare * 100f).ToArray()),
            new ChartSeries("Stale eaten share", "#b84a4a", snapshots.Select(snapshot => snapshot.StaleMeatCaloriesEatenShare * 100f).ToArray()),
            new ChartSeries("Stale seen", "#7d5546", snapshots.Select(snapshot => Share(snapshot.StaleMeatDetectedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Stale avoided", "#9a6b3b", snapshots.Select(snapshot => Share(snapshot.StaleMeatAvoidedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Rotten affected", "#8f4cb8", snapshots.Select(snapshot => Share(snapshot.RottenMeatDamagedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Diet Traits",
            "",
            snapshots,
            new ChartSeries("Diet meat bias", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageDietaryAdaptation).ToArray()),
            new ChartSeries("Carrion bias", "#7d5546", snapshots.Select(snapshot => snapshot.AverageCarrionAdaptation).ToArray()));
        WriteLineChart(
            writer,
            "Digested Energy Source",
            " energy/s",
            snapshots,
            new ChartSeries("Plant energy/s", "#35a862", snapshots.Select(snapshot => snapshot.TotalPlantDigestedEnergyPerSecond).ToArray()),
            new ChartSeries("Meat energy/s", "#b84a4a", snapshots.Select(snapshot => snapshot.TotalMeatDigestedEnergyPerSecond).ToArray()));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");
    }

    private static void WriteLineChart(
        StreamWriter writer,
        string title,
        string unit,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        params ChartSeries[] series)
    {
        const float width = 720f;
        const float height = 240f;
        const float left = 46f;
        const float right = 14f;
        const float top = 16f;
        const float bottom = 34f;
        var plotWidth = width - left - right;
        var plotHeight = height - top - bottom;

        var min = 0f;
        var max = 1f;
        var hasValue = false;
        foreach (var chartSeries in series)
        {
            foreach (var value in chartSeries.Values)
            {
                if (!float.IsFinite(value))
                {
                    continue;
                }

                if (!hasValue)
                {
                    min = value;
                    max = value;
                    hasValue = true;
                }
                else
                {
                    min = Math.Min(min, value);
                    max = Math.Max(max, value);
                }
            }
        }

        min = Math.Min(0f, min);
        if (Math.Abs(max - min) < 0.000001f)
        {
            max = min + 1f;
        }

        writer.WriteLine("<div class=\"chart-card\">");
        writer.WriteLine($"<h3>{Html(title)}</h3>");
        writer.WriteLine($"<svg viewBox=\"0 0 {width:0} {height:0}\" role=\"img\" aria-label=\"{Html(title)} chart\">");
        writer.WriteLine($"<line class=\"chart-axis\" x1=\"{left:0}\" y1=\"{top:0}\" x2=\"{left:0}\" y2=\"{height - bottom:0}\" />");
        writer.WriteLine($"<line class=\"chart-axis\" x1=\"{left:0}\" y1=\"{height - bottom:0}\" x2=\"{width - right:0}\" y2=\"{height - bottom:0}\" />");
        writer.WriteLine($"<text class=\"chart-label\" x=\"4\" y=\"{top + 4:0}\">{Html(FormatChartValue(max, unit))}</text>");
        writer.WriteLine($"<text class=\"chart-label\" x=\"4\" y=\"{height - bottom:0}\">{Html(FormatChartValue(min, unit))}</text>");

        foreach (var chartSeries in series)
        {
            if (chartSeries.Values.Length == 0)
            {
                continue;
            }

            var points = new string[chartSeries.Values.Length];
            for (var i = 0; i < chartSeries.Values.Length; i++)
            {
                var x = chartSeries.Values.Length == 1
                    ? left
                    : left + i / (float)(chartSeries.Values.Length - 1) * plotWidth;
                var y = top + (max - chartSeries.Values[i]) / (max - min) * plotHeight;
                points[i] = $"{x.ToString("0.###", CultureInfo.InvariantCulture)},{y.ToString("0.###", CultureInfo.InvariantCulture)}";
            }

            writer.WriteLine($"<polyline points=\"{Html(string.Join(' ', points))}\" fill=\"none\" stroke=\"{Html(chartSeries.Color)}\" stroke-width=\"2.4\" stroke-linejoin=\"round\" stroke-linecap=\"round\" />");
        }

        writer.WriteLine("</svg>");
        writer.WriteLine("<div class=\"chart-legend\">");
        foreach (var chartSeries in series)
        {
            var final = chartSeries.Values.Length > 0 ? chartSeries.Values[^1] : 0f;
            writer.WriteLine(
                $"<span><span class=\"legend-swatch\" style=\"background:{Html(chartSeries.Color)}\"></span>{Html(chartSeries.Label)} {Html(FormatChartValue(final, unit))}</span>");
        }

        writer.WriteLine("</div>");
        writer.WriteLine("</div>");
    }

    private static void WriteBehaviorAssaySection(StreamWriter writer, BehaviorAssaySummary summary)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Behavior Assays</h2>");
        if (summary.EvaluatedCreatureCount == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living neural creatures were available for behavior assays.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Brains evaluated", summary.EvaluatedCreatureCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Movement style", summary.MovementStyle);
        WriteMetric(writer, "Search response", summary.SearchTendency);
        WriteMetric(writer, "Population ecotype", summary.Ecotype);
        WriteMetric(writer, "Food response", summary.ForagingBias);
        WriteMetric(writer, "Creature attack response", summary.PredatorTendency);
        WriteMetric(writer, "Risk response", summary.RiskResponse);
        WriteMetric(writer, "Terrain response", summary.TerrainResponse);
        WriteMetric(writer, "Egg laying", summary.ReproductionTendency);
        WriteMetric(writer, "Rotten meat response", summary.RottenMeatResponse);
        WriteMetric(writer, "Fresh meat preference", summary.FreshMeatPreferenceScore.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Rot scent avoidance", summary.RottenScentAvoidanceScore.ToString("0.###", CultureInfo.InvariantCulture));
        writer.WriteLine("</div>");

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Assay</th><th>Move</th><th>Turn</th><th>Eat</th><th>Reproduce</th><th>Attack</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var result in summary.Results)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(result.Name)}</td>" +
                $"<td>{Html(result.MoveForward.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(result.Turn.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(result.EatShare))}</td>" +
                $"<td>{Html(FormatPercent(result.ReproduceShare))}</td>" +
                $"<td>{Html(FormatPercent(result.AttackShare))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpeciesClusterSection(
        StreamWriter writer,
        IReadOnlyList<SpeciesClusterSummary> summaries)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Top Species Clusters</h2>");
        if (summaries.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living creatures were available for species clustering.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Living</th><th>Share</th><th>Founders</th><th>Dominant Founder</th><th>Representative</th><th>Generation</th><th>Diet</th><th>Tactic</th><th>Region</th><th>Genome Div</th><th>Brain Div</th><th>Plant Digest</th><th>Meat Digest</th><th>Attack</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in summaries)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.Rank)}</td>" +
                $"<td>{Html(summary.Name)}</td>" +
                $"<td>{Html(summary.LivingCreatures)}</td>" +
                $"<td>{Html(FormatPercent(summary.LivingShare))}</td>" +
                $"<td>{Html(summary.FounderCount)}</td>" +
                $"<td>#{Html(summary.DominantFounderId.Value)} ({Html(summary.DominantFounderLivingCreatures)})</td>" +
                $"<td>#{Html(summary.RepresentativeCreatureId.Value)} ({Html(summary.RepresentativeDistance.ToString("0.###", CultureInfo.InvariantCulture))})</td>" +
                $"<td>{Html($"{summary.MinGeneration}/{summary.AverageGeneration:0.#}/{summary.MaxGeneration}")}</td>" +
                $"<td>{Html(summary.DietLabel)}</td>" +
                $"<td>{Html(summary.TacticLabel)}</td>" +
                $"<td>{Html(summary.RegionLabel)}</td>" +
                $"<td>{Html(summary.AverageGenomeDistance.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.AverageBrainDistance.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(summary.AveragePlantDigestion))}</td>" +
                $"<td>{Html(FormatPercent(summary.AverageMeatDigestion))}</td>" +
                $"<td>{Html(FormatPercent(summary.AttackShare))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpeciesBehaviorFingerprintSection(
        StreamWriter writer,
        IReadOnlyList<SpeciesClusterBehaviorFingerprint> fingerprints)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Species Behavior Fingerprints</h2>");
        if (fingerprints.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living neural creatures were available for species behavior fingerprints.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Living</th><th>Evaluated</th><th>Ecotype</th><th>Food</th><th>Rotten Meat</th><th>Risk</th><th>Terrain</th><th>Attack</th><th>Movement</th><th>Search</th><th>Egg Laying</th><th>Plant Move</th><th>Meat Move</th><th>Rot Scent Move</th><th>Small Attack</th><th>Large Attack</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var fingerprint in fingerprints)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(fingerprint.Rank)}</td>" +
                $"<td>{Html(fingerprint.Name)}</td>" +
                $"<td>{Html($"{fingerprint.LivingCreatures} ({FormatPercent(fingerprint.LivingShare)})")}</td>" +
                $"<td>{Html(fingerprint.EvaluatedCreatureCount)}</td>" +
                $"<td>{Html(fingerprint.Ecotype)}</td>" +
                $"<td>{Html(fingerprint.ForagingBias)}</td>" +
                $"<td>{Html(fingerprint.RottenMeatResponse)}</td>" +
                $"<td>{Html(fingerprint.RiskResponse)}</td>" +
                $"<td>{Html(fingerprint.TerrainResponse)}</td>" +
                $"<td>{Html(fingerprint.PredatorTendency)}</td>" +
                $"<td>{Html(fingerprint.MovementStyle)}</td>" +
                $"<td>{Html(fingerprint.SearchTendency)}</td>" +
                $"<td>{Html(fingerprint.ReproductionTendency)}</td>" +
                $"<td>{Html(fingerprint.PlantAheadMoveForward.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(fingerprint.MeatAheadMoveForward.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(fingerprint.RottenScentAheadMoveForward.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPercent(fingerprint.SmallCreatureAttackShare))}</td>" +
                $"<td>{Html(FormatPercent(fingerprint.LargeApproachAttackShare))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpeciesBehaviorChangeSection(
        StreamWriter writer,
        IReadOnlyList<SpeciesClusterBehaviorChange> changes)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Species Behavior Change</h2>");
        if (changes.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No species behavior change comparison was available.</p>");
            writer.WriteLine("</section>");
            return;
        }

        var notableChanges = SpeciesClusterAnalyzer.FindNotableBehaviorChanges(changes);
        writer.WriteLine("<h3>Notable Shifts</h3>");
        if (notableChanges.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No notable behavior shifts crossed the report thresholds.</p>");
        }
        else
        {
            writer.WriteLine("<div class=\"table-wrap\"><table>");
            writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Score</th><th>Change</th></tr></thead>");
            writer.WriteLine("<tbody>");
            foreach (var change in notableChanges)
            {
                writer.WriteLine(
                    "<tr>" +
                    $"<td>{Html(change.Rank)}</td>" +
                    $"<td>{Html(change.Name)}</td>" +
                    $"<td>{Html(change.Score.ToString("0.##", CultureInfo.InvariantCulture))}</td>" +
                    $"<td>{Html(change.Summary)}</td>" +
                    "</tr>");
            }

            writer.WriteLine("</tbody></table></div>");
        }

        writer.WriteLine("<h3>All Cluster Comparisons</h3>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Samples</th><th>Ecotype</th><th>Food</th><th>Rotten Meat</th><th>Risk</th><th>Terrain</th><th>Attack</th><th>Movement</th><th>Eggs</th><th>Plant Move</th><th>Meat Move</th><th>Rot Move</th><th>Small Attack</th><th>Egg Laying</th><th>Summary</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var change in changes)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(change.Rank)}</td>" +
                $"<td>{Html(change.Name)}</td>" +
                $"<td>{Html($"{change.EarlySampleCount} early / {change.FinalSampleCount} {change.FinalSampleKind}")}</td>" +
                $"<td>{Html(FormatChange(change.EarlyEcotype, change.FinalEcotype))}</td>" +
                $"<td>{Html(FormatChange(change.EarlyForagingBias, change.FinalForagingBias))}</td>" +
                $"<td>{Html(FormatChange(change.EarlyRottenMeatResponse, change.FinalRottenMeatResponse))}</td>" +
                $"<td>{Html(FormatChange(change.EarlyRiskResponse, change.FinalRiskResponse))}</td>" +
                $"<td>{Html(FormatChange(change.EarlyTerrainResponse, change.FinalTerrainResponse))}</td>" +
                $"<td>{Html(FormatChange(change.EarlyPredatorTendency, change.FinalPredatorTendency))}</td>" +
                $"<td>{Html(FormatChange(change.EarlyMovementStyle, change.FinalMovementStyle))}</td>" +
                $"<td>{Html(FormatChange(change.EarlyReproductionTendency, change.FinalReproductionTendency))}</td>" +
                $"<td>{Html(FormatDelta(change.PlantMoveDelta))}</td>" +
                $"<td>{Html(FormatDelta(change.MeatMoveDelta))}</td>" +
                $"<td>{Html(FormatDelta(change.RotScentMoveDelta))}</td>" +
                $"<td>{Html(FormatDelta(change.SmallAttackDelta))}</td>" +
                $"<td>{Html(FormatDelta(change.EggLayingDelta))}</td>" +
                $"<td>{Html(change.Summary)}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpeciesClusterInterpretationSection(
        StreamWriter writer,
        IReadOnlyList<SpeciesClusterInterpretation> interpretations)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Why These Clusters Matter</h2>");
        if (interpretations.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No species cluster interpretation was available.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Role</th><th>Ancestry</th><th>Trend</th><th>Why It Matters</th><th>Evidence</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var interpretation in interpretations)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(interpretation.Rank)}</td>" +
                $"<td>{Html(interpretation.Name)}</td>" +
                $"<td>{Html(interpretation.RoleLabel)}</td>" +
                $"<td>{Html(interpretation.AncestryLabel)}</td>" +
                $"<td>{Html(interpretation.TrendLabel)}</td>" +
                $"<td>{Html(interpretation.ImportanceLabel)}</td>" +
                $"<td>{Html(interpretation.EvidenceLabel)}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpeciesClusterHistorySection(StreamWriter writer, SpeciesClusterHistory history)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Species Cluster History</h2>");
        if (history.Clusters.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No lineage records were available for species history reconstruction.</p>");
            writer.WriteLine("</section>");
            return;
        }

        if (history.Notes.Count > 0)
        {
            writer.WriteLine("<ul>");
            foreach (var note in history.Notes)
            {
                writer.WriteLine($"<li>{Html(note)}</li>");
            }

            writer.WriteLine("</ul>");
        }

        if (history.DiversityRows.Count > 0)
        {
            var finalDiversity = history.DiversityRows[^1];
            var peakDiversity = history.DiversityRows
                .OrderByDescending(row => row.ActiveClusterCount)
                .ThenBy(row => row.Tick)
                .First();
            var totalTurnover = history.DiversityRows.Sum(row => row.TurnoverClusters);

            writer.WriteLine("<div class=\"metric-grid\">");
            WriteMetric(writer, "Final active clusters", finalDiversity.ActiveClusterCount.ToString(CultureInfo.InvariantCulture));
            WriteMetric(writer, "Peak active clusters", $"{peakDiversity.ActiveClusterCount} at tick {peakDiversity.Tick}");
            WriteMetric(writer, "Final dominant cluster", $"{finalDiversity.DominantName} ({FormatPercent(finalDiversity.DominantLivingShare)})");
            WriteMetric(writer, "Sampled turnover", totalTurnover.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine("</div>");

            writer.WriteLine("<div class=\"chart-grid\">");
            WriteLineChart(
                writer,
                "Species Diversity",
                "",
                Array.Empty<SimulationStatsSnapshot>(),
                new ChartSeries("Active clusters", "#6a8fce", history.DiversityRows.Select(row => (float)row.ActiveClusterCount).ToArray()),
                new ChartSeries("Dominant share %", "#2f7d4f", history.DiversityRows.Select(row => row.DominantLivingShare * 100f).ToArray()),
                new ChartSeries("Turnover", "#d69d2f", history.DiversityRows.Select(row => (float)row.TurnoverClusters).ToArray()));
            writer.WriteLine("</div>");
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Status</th><th>Lifecycle</th><th>Births</th><th>Deaths</th><th>Final</th><th>Peak</th><th>First Birth</th><th>Peak Tick</th><th>Last Seen</th><th>Generation</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in history.Clusters)
        {
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.Rank)}</td>" +
                $"<td>{Html(summary.Name)}</td>" +
                $"<td>{Html(summary.Status)}</td>" +
                $"<td>{Html(summary.LifecycleLabel)}</td>" +
                $"<td>{Html(summary.Births)}</td>" +
                $"<td>{Html(summary.Deaths)}</td>" +
                $"<td>{Html($"{summary.FinalLivingCreatures} ({FormatPercent(summary.FinalLivingShare)})")}</td>" +
                $"<td>{Html($"{summary.PeakLivingCreatures} ({FormatPercent(summary.PeakLivingShare)})")}</td>" +
                $"<td>{Html(summary.FirstBirthTick)}</td>" +
                $"<td>{Html(summary.PeakTick)}</td>" +
                $"<td>{Html(summary.LastLivingTick)}</td>" +
                $"<td>{Html(FormatGenerationRange(summary.MinGeneration, summary.AverageGeneration, summary.MaxGeneration))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");

        var selectedClusters = history.Clusters.Take(5).ToArray();
        var selectedTicks = SelectReportTicks(history.DiversityRows
            .Select(row => row.Tick)
            .OrderBy(tick => tick)
            .ToArray());
        if (selectedTicks.Count > 0)
        {
            var diversityByTick = history.DiversityRows.ToDictionary(row => row.Tick);
            writer.WriteLine("<h3>Diversity Over Time</h3>");
            writer.WriteLine("<div class=\"table-wrap\"><table>");
            writer.WriteLine("<thead><tr><th>Tick</th><th>Time</th><th>Active Clusters</th><th>Total Living</th><th>Dominant</th><th>Dominant Share</th><th>Entering</th><th>Exiting</th></tr></thead>");
            writer.WriteLine("<tbody>");
            foreach (var tick in selectedTicks)
            {
                if (!diversityByTick.TryGetValue(tick, out var row))
                {
                    continue;
                }

                writer.WriteLine(
                    "<tr>" +
                    $"<td>{Html(row.Tick)}</td>" +
                    $"<td>{Html(row.ElapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                    $"<td>{Html(row.ActiveClusterCount)}</td>" +
                    $"<td>{Html(row.TotalLiving)}</td>" +
                    $"<td>{Html(row.DominantName)}</td>" +
                    $"<td>{Html(FormatPercent(row.DominantLivingShare))}</td>" +
                    $"<td>{Html(row.EnteringClusters)}</td>" +
                    $"<td>{Html(row.ExitingClusters)}</td>" +
                    "</tr>");
            }

            writer.WriteLine("</tbody></table></div>");
        }

        if (selectedClusters.Length > 0 && selectedTicks.Count > 0)
        {
            var rowByTickSpecies = history.Rows.ToDictionary(row => (row.Tick, row.SpeciesId));
            var rowsByTick = history.Rows
                .GroupBy(row => row.Tick)
                .ToDictionary(group => group.Key, group => group.First().ElapsedSeconds);

            writer.WriteLine("<h3>Cluster Counts Over Time</h3>");
            writer.WriteLine("<div class=\"table-wrap\"><table>");
            writer.Write("<thead><tr><th>Tick</th><th>Time</th><th>Total</th>");
            foreach (var cluster in selectedClusters)
            {
                writer.Write($"<th>{Html(cluster.Name)}</th>");
            }

            writer.WriteLine("</tr></thead>");
            writer.WriteLine("<tbody>");
            foreach (var tick in selectedTicks)
            {
                var totalLiving = history.Rows
                    .Where(row => row.Tick == tick)
                    .Select(row => row.TotalLiving)
                    .FirstOrDefault();
                rowsByTick.TryGetValue(tick, out var elapsedSeconds);
                writer.Write(
                    "<tr>" +
                    $"<td>{Html(tick)}</td>" +
                    $"<td>{Html(elapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                    $"<td>{Html(totalLiving)}</td>");

                foreach (var cluster in selectedClusters)
                {
                    if (rowByTickSpecies.TryGetValue((tick, cluster.SpeciesId), out var row))
                    {
                        writer.Write($"<td>{Html($"{row.LivingCreatures} ({FormatPercent(row.LivingShare)})")}</td>");
                    }
                    else
                    {
                        writer.Write("<td>0</td>");
                    }
                }

                writer.WriteLine("</tr>");
            }

            writer.WriteLine("</tbody></table></div>");
        }

        writer.WriteLine("</section>");
    }

    private static void WriteLineageBehaviorAssaySection(StreamWriter writer, IReadOnlyList<LineageBehaviorAssaySummary> summaries)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Lineage Behavior Assays</h2>");
        if (summaries.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living founder lineages were available for behavior assays.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Founder</th><th>Living</th><th>Share</th><th>Ecotype</th><th>Food</th><th>Rotten Meat</th><th>Risk</th><th>Terrain</th><th>Attack</th><th>Movement</th><th>Egg Laying</th><th>Small Attack</th><th>Large Approach Attack</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in summaries)
        {
            var behavior = summary.Behavior;
            writer.WriteLine(
                "<tr>" +
                $"<td>#{Html(summary.FounderId.Value)}</td>" +
                $"<td>{Html(summary.LivingCreatures)}</td>" +
                $"<td>{Html(FormatPercent(summary.LivingShare))}</td>" +
                $"<td>{Html(behavior.Ecotype)}</td>" +
                $"<td>{Html(behavior.ForagingBias)}</td>" +
                $"<td>{Html(behavior.RottenMeatResponse)}</td>" +
                $"<td>{Html(behavior.RiskResponse)}</td>" +
                $"<td>{Html(behavior.TerrainResponse)}</td>" +
                $"<td>{Html(behavior.PredatorTendency)}</td>" +
                $"<td>{Html(behavior.MovementStyle)}</td>" +
                $"<td>{Html(behavior.ReproductionTendency)}</td>" +
                $"<td>{Html(FormatPercent(behavior.SmallCreatureAhead.AttackShare))}</td>" +
                $"<td>{Html(FormatPercent(behavior.LargeCreatureApproaching.AttackShare))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteBrainInputDiagnosticsSection(StreamWriter writer, BrainInputDiagnosticSummary summary)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Freshness Brain Wiring</h2>");
        if (summary.EvaluatedCreatureCount == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living neural creatures were available for brain-input diagnostics.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Brains evaluated", summary.EvaluatedCreatureCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Direct freshness magnitude", FormatBrainWeight(summary.DirectFreshnessWeightMagnitude));
        WriteMetric(writer, "Direct rot-scent magnitude", FormatBrainWeight(summary.DirectRotScentWeightMagnitude));
        WriteMetric(writer, "Hidden freshness magnitude", FormatBrainWeight(summary.HiddenFreshnessWeightMagnitude));
        WriteMetric(writer, "Hidden rot-scent magnitude", FormatBrainWeight(summary.HiddenRotScentWeightMagnitude));
        WriteMetric(writer, "Move from freshness", FormatSignedBrainWeight(summary.MoveFreshnessWeight));
        WriteMetric(writer, "Eat from freshness", FormatSignedBrainWeight(summary.EatFreshnessWeight));
        WriteMetric(writer, "Move from rot ahead", FormatSignedBrainWeight(summary.MoveRotScentForwardWeight));
        WriteMetric(writer, "Turn from rot right", FormatSignedBrainWeight(summary.TurnRotScentRightWeight));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");
    }

    private static void WriteLineageBrainInputDiagnosticsSection(
        StreamWriter writer,
        IReadOnlyList<LineageBrainInputDiagnosticSummary> summaries)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Lineage Freshness Wiring</h2>");
        if (summaries.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living founder lineages were available for brain-input diagnostics.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Founder</th><th>Living</th><th>Share</th><th>Fresh Direct</th><th>Rot Direct</th><th>Fresh Hidden</th><th>Rot Hidden</th><th>Move Fresh</th><th>Eat Fresh</th><th>Move Rot Ahead</th><th>Turn Rot Right</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in summaries)
        {
            var diagnostics = summary.Diagnostics;
            writer.WriteLine(
                "<tr>" +
                $"<td>#{Html(summary.FounderId.Value)}</td>" +
                $"<td>{Html(summary.LivingCreatures)}</td>" +
                $"<td>{Html(FormatPercent(summary.LivingShare))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectFreshnessWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectRotScentWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenFreshnessWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenRotScentWeightMagnitude))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveFreshnessWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.EatFreshnessWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveRotScentForwardWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.TurnRotScentRightWeight))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpeciesBrainInputDiagnosticsSection(
        StreamWriter writer,
        IReadOnlyList<SpeciesClusterBrainInputDiagnosticSummary> summaries)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Species Freshness Wiring</h2>");
        if (summaries.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living neural species clusters were available for brain-input diagnostics.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Living</th><th>Evaluated</th><th>Fresh Direct</th><th>Rot Direct</th><th>Fresh Hidden</th><th>Rot Hidden</th><th>Move Fresh</th><th>Eat Fresh</th><th>Move Rot Density</th><th>Turn Rot Density</th><th>Move Rot Ahead</th><th>Turn Rot Right</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in summaries)
        {
            var diagnostics = summary.Diagnostics;
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.Rank)}</td>" +
                $"<td>{Html(summary.Name)}</td>" +
                $"<td>{Html($"{summary.LivingCreatures} ({FormatPercent(summary.LivingShare)})")}</td>" +
                $"<td>{Html(diagnostics.EvaluatedCreatureCount)}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectFreshnessWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectRotScentWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenFreshnessWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenRotScentWeightMagnitude))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveFreshnessWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.EatFreshnessWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveRotScentDensityWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.TurnRotScentDensityWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveRotScentForwardWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.TurnRotScentRightWeight))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteTraitRow(StreamWriter writer, string name, FloatAccumulator summary)
    {
        writer.WriteLine(
            "<tr>" +
            $"<td>{Html(name)}</td>" +
            $"<td>{Html(summary.Average.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(summary.Min.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(summary.Max.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            "</tr>");
    }

    private static void WriteDegreesTraitRow(StreamWriter writer, string name, FloatAccumulator summary)
    {
        writer.WriteLine(
            "<tr>" +
            $"<td>{Html(name)}</td>" +
            $"<td>{Html(ToDegrees(summary.Average).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(ToDegrees(summary.Min).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(ToDegrees(summary.Max).ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            "</tr>");
    }

    private static string FormatRange(float min, float max)
    {
        return $"{min.ToString("0.###", CultureInfo.InvariantCulture)}-{max.ToString("0.###", CultureInfo.InvariantCulture)}";
    }

    private static string FormatInitialBrainKind(InitialBrainKind kind)
    {
        return kind switch
        {
            InitialBrainKind.SeedForager => "Seed forager",
            InitialBrainKind.ExplorerForager => "Explorer forager",
            InitialBrainKind.SectorForager => "Sector forager",
            InitialBrainKind.ScavengerForager => "Scavenger forager",
            InitialBrainKind.FreshnessAwareScavenger => "Freshness-aware scavenger",
            InitialBrainKind.ForagerPredator => "Forager predator",
            InitialBrainKind.RandomPerFounder => "Per-founder random weights",
            _ => kind.ToString()
        };
    }

    private static string FormatBrainArchitectureKind(BrainArchitectureKind kind)
    {
        return kind switch
        {
            BrainArchitectureKind.HybridNeural => "Hybrid neural",
            BrainArchitectureKind.HiddenLayerNeural => "Hidden-layer neural",
            _ => kind.ToString()
        };
    }

    private static string FormatScenarioSpeciesSeeds(SimulationScenario scenario)
    {
        var seeds = scenario.EnabledSpeciesSeeds().ToArray();
        if (seeds.Length == 0)
        {
            return "None";
        }

        return string.Join(
            ", ",
            seeds.Select(seed =>
            {
                var energy = seed.EnergyOverride is null
                    ? "profile energy"
                    : $"{seed.EnergyOverride.Value:0.###} energy";
                return $"{seed.Count} x {Path.GetFileName(seed.ProfilePath)} in {seed.SpawnRegion} ({energy})";
            }));
    }

    private static string FormatBiomePressureProfile(BiomePressureProfile profile)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Barren {profile.Barren:0.###}x, Sparse {profile.Sparse:0.###}x, Grassland {profile.Grassland:0.###}x, Rich {profile.Rich:0.###}x");
    }

    private static string FormatRegionCounts(int left, int middle, int right)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Left {left}, Middle {middle}, Right {right}");
    }

    private static string FormatRegionValues(float left, float middle, float right, string format)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Left {left.ToString(format, CultureInfo.InvariantCulture)}, Middle {middle.ToString(format, CultureInfo.InvariantCulture)}, Right {right.ToString(format, CultureInfo.InvariantCulture)}");
    }

    private static int CreatureCountForBiome(SimulationStatsSnapshot snapshot, BiomeKind biome)
    {
        return biome switch
        {
            BiomeKind.Barren => snapshot.BarrenCreatureCount,
            BiomeKind.Sparse => snapshot.SparseCreatureCount,
            BiomeKind.Rich => snapshot.RichCreatureCount,
            _ => snapshot.GrasslandCreatureCount
        };
    }

    private static string FormatChartValue(float value, string unit)
    {
        return unit switch
        {
            "%" => $"{value:0.#}%",
            " kcal" => $"{value:0.#} kcal",
            "x" => $"{value:0.###}x",
            _ => value.ToString("0.###", CultureInfo.InvariantCulture)
        };
    }

    private static string FormatPercent(float value)
    {
        return $"{value * 100f:0.0}%";
    }

    private static string FormatChange(string earlyValue, string finalValue)
    {
        return string.Equals(earlyValue, finalValue, StringComparison.Ordinal)
            ? finalValue
            : $"{earlyValue} -> {finalValue}";
    }

    private static string FormatDelta(float value)
    {
        return value.ToString("+0.###;-0.###;0", CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<long> SelectReportTicks(IReadOnlyList<long> ticks)
    {
        if (ticks.Count <= ReportTrendRowCount)
        {
            return ticks;
        }

        var selected = new List<long>();
        long? lastTick = null;
        for (var i = 0; i < ReportTrendRowCount; i++)
        {
            var index = (int)Math.Round(i * (ticks.Count - 1) / (double)(ReportTrendRowCount - 1));
            var tick = ticks[index];
            if (tick == lastTick)
            {
                continue;
            }

            selected.Add(tick);
            lastTick = tick;
        }

        return selected;
    }

    private static string FormatGenerationRange(int min, float average, int max)
    {
        return min == max
            ? $"{min} avg {average:0.##}"
            : $"{min}-{max} avg {average:0.##}";
    }

    private static string FormatPlantRelocations(SimulationStats stats)
    {
        return $"local {stats.PlantLocalDispersalCount}, cluster {stats.PlantClusterRelocationCount}, global {stats.PlantGlobalRelocationCount}";
    }

    private static string FormatBrainWeight(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatSignedBrainWeight(float value)
    {
        return value.ToString("+0.###;-0.###;0", CultureInfo.InvariantCulture);
    }

    private static float Share(int count, int total)
    {
        return total > 0 ? count / (float)total : 0f;
    }

    private static float ToDegrees(float radians)
    {
        return radians * 180f / MathF.PI;
    }

    private static string Html(object? value)
    {
        return WebUtility.HtmlEncode(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
    }

    private sealed class MutableFounderSummary
    {
        public EntityId FounderId;
        public int TotalCreatures;
        public int LivingCreatures;
        public int DeadCreatures;
        public int MaxGeneration;
    }

    private readonly record struct FounderSummary(
        EntityId FounderId,
        int TotalCreatures,
        int LivingCreatures,
        int DeadCreatures,
        int MaxGeneration);

    private readonly record struct ChartSeries(string Label, string Color, float[] Values);

    private sealed class TraitSummary
    {
        public int Count;
        public FloatAccumulator BodyRadius;
        public FloatAccumulator MaxSpeed;
        public FloatAccumulator SenseRadius;
        public FloatAccumulator VisionAngleRadians;
        public FloatAccumulator ReproductionThreshold;
        public FloatAccumulator OffspringInvestment;
        public FloatAccumulator EggProductionEnergyPerSecond;
        public FloatAccumulator EggIncubationSeconds;
        public FloatAccumulator MaturityAgeSeconds;
        public FloatAccumulator DietaryAdaptation;
        public FloatAccumulator CarrionAdaptation;
        public FloatAccumulator PlantDigestion;
        public FloatAccumulator MeatDigestion;
        public FloatAccumulator FreshMeatDigestion;
        public FloatAccumulator StaleMeatDigestion;
        public FloatAccumulator GutCapacityCalories;
        public FloatAccumulator DigestionCaloriesPerSecond;
        public FloatAccumulator BiteStrength;
        public FloatAccumulator DamageResistance;
        public FloatAccumulator MutationStrength;
        public FloatAccumulator TraitMutationRate;
        public FloatAccumulator BrainMutationRate;
    }

    private struct FloatAccumulator
    {
        private float _sum;
        private float _min;
        private float _max;

        public float Average { get; private set; }

        public float Min => Count > 0 ? _min : 0f;

        public float Max => Count > 0 ? _max : 0f;

        public int Count { get; private set; }

        public void Add(float value)
        {
            if (Count == 0)
            {
                _min = value;
                _max = value;
            }
            else
            {
                _min = Math.Min(_min, value);
                _max = Math.Max(_max, value);
            }

            _sum += value;
            Count++;
            Average = _sum / Count;
        }
    }
}
