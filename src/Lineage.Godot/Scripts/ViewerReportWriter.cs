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
    private const int ReportTimelineSampleLimit = 1200;
    private const int SurvivorLineageTreeNodeRenderLimit = 260;
    private const int RtNeatGraphRenderLimit = 3;

    private readonly record struct SpatialHeatmapLayer(
        string Title,
        string Units,
        IReadOnlyList<float> Values,
        string Color,
        string Description);

    private readonly record struct RtNeatBrainGraphCandidate(
        int BrainId,
        int LivingCreatures,
        int Eggs,
        RtNeatBrainGenome Brain);

    public static void Write(
        string path,
        SimulationScenario scenario,
        Simulation simulation,
        IReadOnlyList<SpeciesInjectionResult>? speciesInjections = null)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(path);
        var state = simulation.State;
        var snapshots = state.Stats.Snapshots;
        var reportSnapshots = SelectReportSnapshots(snapshots);
        var snapshot = snapshots.Count > 0 ? snapshots[^1] : default;
        var averageSmallPreyCaloriesEatenPerSecond = AverageSnapshotValue(
            snapshots,
            value => value.TotalSmallPreyCaloriesEatenPerSecond);
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
        var rosterSummaries = RosterLineageAnalyzer.Analyze(
                state.LineageRecords,
                speciesInjections ?? Array.Empty<SpeciesInjectionResult>())
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenByDescending(summary => summary.TotalCreatures)
            .ThenBy(summary => summary.ProfileName, StringComparer.Ordinal)
            .ToArray();
        var behaviorSummary = BehaviorAssay.Analyze(state);
        var lineageBehaviorSummaries = BehaviorAssay.AnalyzeTopFounderLineages(state, 10);
        var speciesSummaries = SpeciesClusterAnalyzer.Analyze(state, 10);
        var speciesBehaviorFingerprints = SpeciesClusterAnalyzer.AnalyzeBehaviorFingerprints(state, 10);
        var speciesBrainInputDiagnostics = SpeciesClusterAnalyzer.AnalyzeBrainInputDiagnostics(state, 10);
        var speciesHistory = SpeciesClusterAnalyzer.AnalyzeHistory(state, reportSnapshots, 10);
        var speciesBehaviorChanges = SpeciesClusterAnalyzer.AnalyzeBehaviorChanges(state, speciesHistory, 10);
        var brainInputDiagnostics = BrainInputDiagnostics.Analyze(state);
        var lineageBrainInputDiagnostics = BrainInputDiagnostics.AnalyzeTopFounderLineages(state, 10);
        var seasonPressure = SeasonPressureAnalysis.Analyze(scenario, snapshots);
        var survivorAncestry = SurvivorLineageAnalyzer.Analyze(state);

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
        var hasSpeciesRoster = scenario.EnabledSpeciesSeeds().Any();
        WriteMetric(writer, "Scenario", scenario.Name);
        WriteMetric(writer, "Pipeline", scenario.PipelineKind.ToString());
        WriteMetric(writer, hasSpeciesRoster ? "Default brain architecture" : "Brain architecture", FormatBrainArchitectureKind(scenario.BrainArchitectureKind));
        WriteMetric(writer, hasSpeciesRoster ? "Default initial brain" : "Initial brain", FormatInitialBrainKind(scenario.InitialBrainKind));
        WriteMetric(writer, hasSpeciesRoster ? "Default brain hidden nodes" : "Brain hidden nodes", scenario.BrainHiddenNodeCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Seed", scenario.Seed.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "World size", $"{state.Bounds.Width:0} x {state.Bounds.Height:0}");
        WriteMetric(writer, "Resources per 1M area", resourceDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Tick", state.Tick.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Simulated seconds", state.ElapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Snapshot interval", $"{scenario.StatsSnapshotIntervalTicks} ticks");
        WriteMetric(writer, "Plant payoff trace half-life", $"{scenario.PlantPayoffTraceHalfLifeSeconds:0.###} seconds");
        WriteMetric(writer, "Seasons", scenario.EnableSeasons ? "Enabled" : "Disabled");
        WriteMetric(writer, "Season length", $"{scenario.SeasonLengthSeconds:0.###} seconds");
        WriteMetric(writer, "Season fertility swing", FormatPercent(scenario.SeasonFertilityAmplitude));
        WriteMetric(writer, "Season phase mode", scenario.SeasonPhaseMode.ToString());
        WriteMetric(writer, "Starting roster", FormatScenarioSpeciesSeeds(scenario));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        WriteScenarioSpeciesRosterSection(writer, scenario);

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Outcome</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Living creatures", state.Creatures.Count.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Eggs", state.Eggs.Count.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Active resources", activeResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Resource slots", state.Resources.Count.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plants", snapshot.PlantResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Meat", snapshot.MeatResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Small prey", snapshot.SmallPreyCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Resource calories", totalResourceCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plant calories", snapshot.TotalPlantCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plant type calories", FormatPlantTypeCalories(snapshot));
        WriteMetric(writer, "Meat calories", snapshot.TotalMeatCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Small prey calories", snapshot.TotalSmallPreyCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Dormant plants", snapshot.DormantPlantResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg dormancy remaining", $"{snapshot.AverageDormantPlantSecondsRemaining:0.###} seconds");
        WriteMetric(writer, "Plant patch occupied", FormatPercent(snapshot.PlantPatchOccupiedCellShare));
        WriteMetric(writer, "Plant top-decile calories", FormatPercent(snapshot.PlantPatchTopDecileCaloriesShare));
        WriteMetric(writer, "Plant patchiness", snapshot.PlantPatchiness.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg local fertility", $"{snapshot.AverageLocalFertilityMultiplier:0.###}x");
        WriteMetric(writer, "Min local fertility", $"{snapshot.MinimumLocalFertilityMultiplier:0.###}x");
        WriteMetric(writer, "Depleted fertility cells", FormatPercent(snapshot.DepletedLocalFertilityCellShare));
        WriteMetric(writer, "Temperature cells", snapshot.TemperatureCellCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg map temperature", FormatTemperatureIndex(snapshot.AverageMapTemperature));
        WriteMetric(writer, "Map temperature range", $"{FormatTemperatureIndex(snapshot.MinimumMapTemperature)} - {FormatTemperatureIndex(snapshot.MaximumMapTemperature)}");
        WriteMetric(writer, "Avg creature temperature", FormatTemperatureIndex(snapshot.AverageCreatureTemperature));
        WriteMetric(writer, "Avg thermal optimum", FormatTemperatureIndex(snapshot.AverageThermalOptimum));
        WriteMetric(writer, "Avg thermal tolerance", FormatTemperatureIndex(snapshot.AverageThermalTolerance));
        WriteMetric(writer, "Avg thermal mismatch", FormatPercent(snapshot.AverageCreatureThermalMismatch));
        WriteMetric(writer, "Hot/cold mismatch", $"{snapshot.HotThermalMismatchCreatureCount} hot / {snapshot.ColdThermalMismatchCreatureCount} cold");
        WriteMetric(writer, "Thermal basal cost", $"{snapshot.ThermalBasalEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Thermal stress mix", $"{snapshot.ComfortableThermalCreatureCount} comfortable / {snapshot.ColdThermalStressCreatureCount} cold / {snapshot.HotThermalStressCreatureCount} hot");
        WriteMetric(writer, "Temp-band creatures", $"{snapshot.ColdTemperatureCreatureCount} cold / {snapshot.TemperateTemperatureCreatureCount} temperate / {snapshot.HotTemperatureCreatureCount} hot");
        WriteMetric(writer, "Temp-band plant kcal", $"{snapshot.ColdTemperaturePlantCalories:0.#} cold / {snapshot.TemperateTemperaturePlantCalories:0.#} temperate / {snapshot.HotTemperaturePlantCalories:0.#} hot");
        WriteMetric(writer, "Temp-band births", $"{snapshot.ColdTemperatureBirths:0.#} cold / {snapshot.TemperateTemperatureBirths:0.#} temperate / {snapshot.HotTemperatureBirths:0.#} hot");
        WriteMetric(writer, "Temp-band deaths", $"{snapshot.ColdTemperatureDeaths:0.#} cold / {snapshot.TemperateTemperatureDeaths:0.#} temperate / {snapshot.HotTemperatureDeaths:0.#} hot");
        WriteMetric(writer, "Avg plant temperature", FormatTemperatureIndex(snapshot.AveragePlantTemperature));
        WriteMetric(writer, "Avg small prey temperature", FormatTemperatureIndex(snapshot.AverageSmallPreyTemperature));
        WriteMetric(writer, "Plant depletions", state.Stats.PlantDepletionCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plant relocations", FormatPlantRelocations(state.Stats));
        WriteMetric(writer, "Small prey spawn/kill/eat", $"{snapshot.SmallPreySpawnedCount}/{snapshot.SmallPreyKilledCount}/{snapshot.SmallPreyEatenCount}");
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
        WriteMetric(writer, "Fat reserve", FormatPercent(snapshot.AverageFatRatio));
        WriteMetric(writer, "Fat calories", snapshot.TotalFatCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Fat mass burden", FormatPercent(snapshot.AverageMassBurdenRatio));
        WriteMetric(writer, "Fat speed retained", $"{snapshot.AverageFatSpeedMultiplier:0.###}x");
        WriteMetric(writer, "Fat genes", $"capacity {snapshot.AverageFatStorageCapacityCalories:0.###}, efficiency {FormatPercent(snapshot.AverageFatStorageEfficiency)}");
        WriteMetric(writer, "Fat flow", $"{snapshot.TotalFatStoredCaloriesPerSecond:0.###}/s stored, {snapshot.TotalFatReleasedCaloriesPerSecond:0.###}/s released");
        WriteMetric(writer, "Food success", FormatPercent(snapshot.AverageRecentFoodSuccess));
        WriteMetric(writer, "Food energy yield", FormatPercent(snapshot.AverageRecentFoodEnergyYield));
        WriteMetric(writer, "Plant payoff traces", FormatPlantPayoffTraces(snapshot));
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
        if (snapshot.RtNeatBrainCount > 0)
        {
            WriteMetric(writer, "rtNEAT brains", $"{FormatPercent(snapshot.RtNeatBrainShare)} ({snapshot.RtNeatBrainCount})");
            WriteMetric(writer, "rtNEAT hidden nodes", $"{snapshot.AverageRtNeatHiddenNodeCount:0.###} avg / {snapshot.MaxRtNeatHiddenNodeCount} max");
            WriteMetric(writer, "rtNEAT connections", $"{snapshot.AverageRtNeatConnectionCount:0.###} avg / {snapshot.MaxRtNeatConnectionCount} max");
            WriteMetric(writer, "rtNEAT enabled conn", $"{snapshot.AverageRtNeatEnabledConnectionCount:0.###} avg / {snapshot.MaxRtNeatEnabledConnectionCount} max");
        }
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
        WriteMetric(writer, "Plant type eaten", FormatPlantTypeIntake(snapshot));
        WriteMetric(writer, "Carcass eaten", $"{snapshot.TotalCarcassCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Fresh meat eaten", $"{snapshot.TotalFreshMeatCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Stale meat eaten", $"{snapshot.TotalStaleMeatCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Rotten damage", $"{snapshot.TotalRottenMeatDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Rotten affected", FormatPercent(Share(snapshot.RottenMeatDamagedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Egg eaten", $"{snapshot.TotalEggCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Fresh kill eaten", $"{snapshot.TotalLivePreyCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Small prey eaten", $"{snapshot.TotalSmallPreyCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Avg small prey eaten", $"{averageSmallPreyCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Calories digested", $"{snapshot.TotalCaloriesDigestedPerSecond:0.###} energy/s");
        WriteMetric(writer, "Plant energy", $"{snapshot.TotalPlantDigestedEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Plant type energy", FormatPlantTypeDigestion(snapshot));
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

        WritePlantTypeDiagnosticsSection(writer, snapshot);

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
        WriteMetric(writer, "Similarity scent", $"{FormatPercent(Share(snapshot.CreatureSimilarityScentDetectedCreatureCount, snapshot.CreatureCount))} @ {snapshot.AverageCreatureSimilarityScentDensity:0.###}");
        WriteMetric(writer, "Creature contact", FormatPercent(Share(snapshot.CreatureContactCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Similar contact", $"{FormatPercent(Share(snapshot.SimilarCreatureContactCreatureCount, snapshot.CreatureCount))} avg {snapshot.AverageCreatureContactSimilarity:0.###}");
        WriteMetric(writer, "Attack intent", FormatPercent(Share(snapshot.AttackIntentCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Intent while touching", FormatPercent(Share(snapshot.AttackIntentWhileTouchingCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Intent on similar touch", FormatPercent(Share(snapshot.AttackIntentWhileTouchingSimilarCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Touch no intent", FormatPercent(Share(snapshot.AttackNoIntentContactCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Raw attack > 0", FormatPercent(Share(snapshot.RawAttackPositiveCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Raw attack near gate", FormatPercent(Share(snapshot.RawAttackNearGateCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Near gate while touching", FormatPercent(Share(snapshot.RawAttackNearGateWhileTouchingCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Avg raw attack", snapshot.AverageAttackOutput.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg touching attack", snapshot.AverageTouchingAttackOutput.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Damage-dealing this tick", FormatPercent(Share(snapshot.AttackingCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Attack damage", $"{snapshot.TotalAttackDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Damage per attacker", $"{attackDamagePerAttacker:0.###} health/s");
        WriteMetric(writer, "Grab intent", FormatPercent(Share(snapshot.GrabIntentCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Can grab", FormatPercent(Share(snapshot.CanGrabCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Grab while touching", FormatPercent(Share(snapshot.GrabIntentWhileCanGrabCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Grab off contact", FormatPercent(Share(snapshot.GrabIntentWithoutCanGrabCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Holding", $"{FormatPercent(Share(snapshot.HoldingCreatureCount, snapshot.CreatureCount))} ({snapshot.HoldingCreatureCount})");
        WriteMetric(writer, "Grabbed", $"{FormatPercent(Share(snapshot.GrabbedCreatureCount, snapshot.CreatureCount))} ({snapshot.GrabbedCreatureCount})");
        WriteMetric(writer, "Avg grab output", snapshot.AverageGrabOutput.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg touch grab output", snapshot.AverageCanGrabGrabOutput.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg grab pressure", snapshot.AverageGrabPressure.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg grab strength", snapshot.AverageGrabStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Sound emitting", FormatPercent(Share(snapshot.SoundEmittingCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Sound heard", FormatPercent(Share(snapshot.SoundHeardCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Avg sound amp", snapshot.AverageSoundAmplitude.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg sound density", snapshot.AverageSoundDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg sound clarity", snapshot.AverageSoundToneClarity.ToString("0.###", CultureInfo.InvariantCulture));
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
        WriteMetric(writer, "Average plant adaptation", $"T {snapshot.AverageTenderPlantAdaptation:0.###}, R {snapshot.AverageRichPlantAdaptation:0.###}, Tough {snapshot.AverageToughPlantAdaptation:0.###}");
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
            CarrionAdaptation = scenario.CarrionAdaptation,
            TenderPlantAdaptation = scenario.TenderPlantAdaptation,
            RichPlantAdaptation = scenario.RichPlantAdaptation,
            ToughPlantAdaptation = scenario.ToughPlantAdaptation,
            ThermalOptimum = scenario.ThermalOptimum,
            ThermalTolerance = scenario.ThermalTolerance
        };

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Pressure Settings</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Initial creatures", scenario.InitialCreatureCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Initial creature spawn", scenario.InitialCreatureSpawnRegion.ToString());
        WriteMetric(writer, "World sense interval", $"{scenario.WorldSenseIntervalTicks} ticks");
        WriteMetric(writer, "Close sense refresh", FormatPercent(scenario.CloseSenseRefreshProximity));
        WriteMetric(writer, "Close refresh minimum", $"{scenario.CloseSenseRefreshMinimumTicks} ticks");
        WriteMetric(writer, "Initial resource density", $"{scenario.InitialResourcesPerMillionArea:0.###} per 1M area");
        WriteMetric(writer, "Initial resource patches", scenario.CalculateInitialResourceCount().ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Biomes", scenario.EnableBiomes ? "Enabled" : "Disabled");
        WriteMetric(writer, "Biome map", scenario.BiomeMapKind.ToString());
        WriteMetric(writer, "Biome cell size", scenario.BiomeCellSize.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Temperature", scenario.EnableTemperature ? "Enabled" : "Disabled");
        WriteMetric(writer, "Obstacles", scenario.EnableObstacles ? "Enabled" : "Disabled");
        WriteMetric(writer, "Obstacle map", scenario.ObstacleMapKind.ToString());
        WriteMetric(writer, "Obstacle cell size", scenario.ObstacleCellSize.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Resource void border", $"{scenario.ResourceVoidBorderWidth:0.###} world units");
        WriteMetric(writer, "Resource calories", FormatRange(scenario.ResourceCaloriesMin, scenario.ResourceCaloriesMax));
        WriteMetric(writer, "Resource regrowth", $"{FormatRange(scenario.ResourceRegrowthMin, scenario.ResourceRegrowthMax)} kcal/s");
        WriteMetric(writer, "Plant type mix", FormatPlantTypeMix(scenario));
        WriteMetric(writer, "Plant habitat affinity", scenario.EnablePlantTypeHabitatAffinity ? "Enabled" : "Disabled");
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
        WriteMetric(writer, "Biome vision range", FormatBiomePressureProfile(scenario.CreateBiomeVisionRangeProfile()));
        WriteMetric(writer, "Basal upkeep", $"{scenario.BasalEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Thermal mismatch basal cost", $"{scenario.ThermalMismatchBasalCostMultiplier:0.###}x at full mismatch");
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
        WriteMetric(writer, "Plant specialization upkeep", $"{scenario.PlantSpecializationEnergyCostPerSecond:0.######} energy/unit/s");
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
        WriteMetric(writer, "Starting plant adaptation", $"T {scenario.TenderPlantAdaptation:0.###}, R {scenario.RichPlantAdaptation:0.###}, Tough {scenario.ToughPlantAdaptation:0.###}");
        WriteMetric(writer, "Starting thermal genes", $"opt {FormatTemperatureIndex(scenario.ThermalOptimum)}, tol {FormatTemperatureIndex(scenario.ThermalTolerance)}");
        WriteMetric(writer, "Starting fat storage", $"capacity {scenario.FatStorageCapacityCalories:0.###}, efficiency {FormatPercent(scenario.FatStorageEfficiency)}");
        WriteMetric(writer, "Starting bite strength", scenario.BiteStrength.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Starting damage resistance", scenario.DamageResistance.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Starting plant digestion", FormatPercent(CreatureDigestion.PlantEfficiency(startingGenome)));
        WriteMetric(writer, "Starting tender digestion", FormatPercent(CreatureDigestion.PlantTypeEnergyEfficiency(startingGenome, PlantResourceKind.Tender)));
        WriteMetric(writer, "Starting rich digestion", FormatPercent(CreatureDigestion.PlantTypeEnergyEfficiency(startingGenome, PlantResourceKind.Rich)));
        WriteMetric(writer, "Starting tough digestion", FormatPercent(CreatureDigestion.PlantTypeEnergyEfficiency(startingGenome, PlantResourceKind.Tough)));
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

        WriteBiomeMapSection(writer, state.Biomes);
        WriteTemperatureMapSection(writer, state.Temperature);

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Biomes</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Biome</th><th>Area Share</th><th>Density Mult</th><th>Regrowth Mult</th><th>Season Amp</th><th>Move Cost</th><th>Basal Cost</th><th>Speed</th><th>Vision</th><th>Resources</th><th>Resources/M</th><th>Calories</th><th>Living</th><th>Living Share</th></tr></thead>");
        writer.WriteLine("<tbody>");
        var movementCostProfile = scenario.CreateBiomeMovementCostProfile();
        var basalCostProfile = scenario.CreateBiomeBasalCostProfile();
        var speedProfile = scenario.CreateBiomeSpeedProfile();
        var visionProfile = scenario.CreateBiomeVisionRangeProfile();
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
            var vision = visionProfile.For(summary.Kind).ToString("0.###", CultureInfo.InvariantCulture);
            var seasonalAmplitude = seasonalAmplitudeProfile.For(summary.Kind).ToString("0.###", CultureInfo.InvariantCulture);
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(BiomeKinds.Canonicalize(summary.Kind))}</td>" +
                $"<td>{Html(FormatPercent(summary.Area / worldArea))}</td>" +
                $"<td>{Html(summary.ResourceDensityMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ResourceRegrowthMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html($"{seasonalAmplitude}x")}</td>" +
                $"<td>{Html($"{movementCost}x")}</td>" +
                $"<td>{Html($"{basalCost}x")}</td>" +
                $"<td>{Html($"{speed}x")}</td>" +
                $"<td>{Html($"{vision}x")}</td>" +
                $"<td>{Html(summary.ResourceCount)}</td>" +
                $"<td>{Html(resourcesPerMillion.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ResourceCalories.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(livingCreatureCount)}</td>" +
                $"<td>{Html(FormatPercent(Share(livingCreatureCount, snapshot.CreatureCount)))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        WriteBiomePreferenceSection(writer, snapshots, biomeSummaries, worldArea);
        WriteBiomeRiskRewardSection(writer, snapshots, biomeSummaries, worldArea);
        WriteBiomePreferenceByGenerationSection(writer, state.Creatures, state.Biomes, biomeSummaries, worldArea);

        WriteDeathCausesByBiomeSection(writer, state.Stats.CreatureDeathCausesByBiome);
        WriteSpatialHeatmapSection(writer, state.Biomes, state.Stats.SpatialHeatmaps);

        WriteChartsSection(writer, reportSnapshots, snapshots.Count);
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
        WriteRtNeatBrainGraphSection(writer, state);

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
            WriteTraitRow(writer, "Tender plant adaptation", traitSummary.TenderPlantAdaptation);
            WriteTraitRow(writer, "Rich plant adaptation", traitSummary.RichPlantAdaptation);
            WriteTraitRow(writer, "Tough plant adaptation", traitSummary.ToughPlantAdaptation);
            WriteTraitRow(writer, "Plant digestion efficiency", traitSummary.PlantDigestion);
            WriteTraitRow(writer, "Meat digestion efficiency", traitSummary.MeatDigestion);
            WriteTraitRow(writer, "Fresh meat digestion efficiency", traitSummary.FreshMeatDigestion);
            WriteTraitRow(writer, "Stale meat digestion efficiency", traitSummary.StaleMeatDigestion);
            WriteTraitRow(writer, "Gut capacity", traitSummary.GutCapacityCalories);
            WriteTraitRow(writer, "Digestion rate", traitSummary.DigestionCaloriesPerSecond);
            WriteTraitRow(writer, "Bite strength", traitSummary.BiteStrength);
            WriteTraitRow(writer, "Damage resistance", traitSummary.DamageResistance);
            WriteTraitRow(writer, "Thermal optimum", traitSummary.ThermalOptimum);
            WriteTraitRow(writer, "Thermal tolerance", traitSummary.ThermalTolerance);
            WriteTraitRow(writer, "Mutation strength", traitSummary.MutationStrength);
            WriteTraitRow(writer, "Trait mutation rate", traitSummary.TraitMutationRate);
            WriteTraitRow(writer, "Brain mutation rate", traitSummary.BrainMutationRate);
            writer.WriteLine("</tbody></table></div>");
        }

        writer.WriteLine("</section>");

        if (rosterSummaries.Length > 0)
        {
            writer.WriteLine("<section>");
            writer.WriteLine("<h2>Injected Profile Lineages</h2>");
            writer.WriteLine("<div class=\"table-wrap\"><table>");
            writer.WriteLine("<thead><tr><th>Profile</th><th>Founders</th><th>Total</th><th>Descendants</th><th>Living</th><th>Dead</th><th>Max Generation</th><th>Starved</th><th>Injury</th><th>Rotten</th><th>Other</th></tr></thead>");
            writer.WriteLine("<tbody>");
            foreach (var summary in rosterSummaries)
            {
                writer.WriteLine(
                    "<tr>" +
                    $"<td>{Html(summary.ProfileName)}</td>" +
                    $"<td>{Html(summary.FounderCount)}</td>" +
                    $"<td>{Html(summary.TotalCreatures)}</td>" +
                    $"<td>{Html(summary.DescendantCount)}</td>" +
                    $"<td>{Html(summary.LivingCreatures)}</td>" +
                    $"<td>{Html(summary.DeadCreatures)}</td>" +
                    $"<td>{Html(summary.MaxGeneration)}</td>" +
                    $"<td>{Html(summary.StarvationDeaths)}</td>" +
                    $"<td>{Html(summary.InjuryDeaths)}</td>" +
                    $"<td>{Html(summary.RottenMeatDeaths)}</td>" +
                    $"<td>{Html(summary.UnknownDeaths)}</td>" +
                    "</tr>");
            }

            writer.WriteLine("</tbody></table></div>");
            writer.WriteLine("</section>");
        }

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Top Founder Lineages</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Founder</th><th>Total Creatures</th><th>Living</th><th>Dead</th><th>Max Generation</th><th>Living Share</th><th>Thermal Niche</th><th>Avg Temp</th><th>Mismatch</th><th>Cold/Temp/Hot</th><th>Cold/Hot Stress</th></tr></thead>");
        writer.WriteLine("<tbody>");
        if (founderSummaries.Length == 0)
        {
            writer.WriteLine("<tr><td class=\"empty\" colspan=\"11\">No lineage records are present.</td></tr>");
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
                    $"<td>{Html(summary.ThermalNiche.NicheLabel)}</td>" +
                    $"<td>{Html(summary.ThermalNiche.AverageOccupiedTemperature.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                    $"<td>{Html(summary.ThermalNiche.AverageThermalMismatch.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                    $"<td>{Html(FormatThermalShares(summary.ThermalNiche))}</td>" +
                    $"<td>{Html(FormatStressShares(summary.ThermalNiche))}</td>" +
                    "</tr>");
            }
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

        WriteSurvivorLineageTreeSection(writer, survivorAncestry, state, speciesHistory);

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
            summary.TenderPlantAdaptation.Add(genome.TenderPlantAdaptation);
            summary.RichPlantAdaptation.Add(genome.RichPlantAdaptation);
            summary.ToughPlantAdaptation.Add(genome.ToughPlantAdaptation);
            summary.PlantDigestion.Add(CreatureDigestion.PlantEfficiency(genome));
            summary.MeatDigestion.Add(CreatureDigestion.MeatEfficiency(genome));
            summary.FreshMeatDigestion.Add(CreatureDigestion.FreshMeatEnergyEfficiency(genome));
            summary.StaleMeatDigestion.Add(CreatureDigestion.StaleMeatEnergyEfficiency(genome));
            summary.GutCapacityCalories.Add(genome.GutCapacityCalories);
            summary.DigestionCaloriesPerSecond.Add(genome.DigestionCaloriesPerSecond);
            summary.BiteStrength.Add(genome.BiteStrength);
            summary.DamageResistance.Add(genome.DamageResistance);
            summary.ThermalOptimum.Add(CreatureThermal.NormalizeOptimum(genome.ThermalOptimum));
            summary.ThermalTolerance.Add(CreatureThermal.NormalizeTolerance(genome.ThermalTolerance));
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
        var summaries = new Dictionary<EntityId, List<CreatureLineageRecord>>();

        foreach (var record in records)
        {
            var founderId = FindFounderId(record, byId);
            if (!summaries.TryGetValue(founderId, out var founderRecords))
            {
                founderRecords = [];
                summaries.Add(founderId, founderRecords);
            }

            founderRecords.Add(record);
        }

        return summaries
            .Select(pair =>
            {
                var founderRecords = pair.Value;
                var totalCreatures = founderRecords.Count;
                var livingCreatures = founderRecords.Count(record => record.IsAlive);
                return new FounderSummary(
                    pair.Key,
                    totalCreatures,
                    livingCreatures,
                    Math.Max(0, totalCreatures - livingCreatures),
                    founderRecords.Count == 0 ? 0 : founderRecords.Max(record => record.Generation),
                    ThermalNicheTelemetry.SummarizeRecords(founderRecords));
            })
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
              width: min(1760px, calc(100% - 32px));
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
            .biome-map-note {
              margin: 0 0 12px;
              color: var(--muted);
              font-size: 0.9rem;
            }
            .biome-map-frame {
              overflow: auto;
              padding: 10px;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #eef3e8;
            }
            .biome-map {
              display: block;
              width: 100%;
              max-height: 620px;
              height: auto;
            }
            .biome-map-void {
              fill: none;
              stroke: rgba(23, 32, 21, 0.68);
              stroke-width: 8;
              stroke-dasharray: 26 18;
              vector-effect: non-scaling-stroke;
            }
            .biome-legend {
              display: flex;
              flex-wrap: wrap;
              gap: 8px 14px;
              margin-top: 10px;
              color: var(--muted);
              font-size: 0.86rem;
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
              cursor: zoom-in;
            }
            .chart-card:focus-visible {
              outline: 2px solid var(--accent);
              outline-offset: 3px;
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
            .rtneat-panel {
              display: grid;
              grid-template-columns: minmax(0, 1fr) minmax(220px, 320px);
              gap: 14px;
              align-items: start;
              margin-top: 14px;
            }
            .rtneat-graph-frame {
              overflow: auto;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #fbfcf8;
            }
            .rtneat-graph {
              display: block;
              min-width: 920px;
              width: 100%;
              height: auto;
              font-family: "Segoe UI", system-ui, sans-serif;
            }
            .rtneat-graph text {
              font-size: 10px;
              fill: var(--text);
              pointer-events: none;
            }
            .rtneat-node-label { font-weight: 650; }
            .rtneat-node-kind {
              fill: var(--muted);
              font-size: 8px;
              text-transform: uppercase;
            }
            .rtneat-detail {
              border: 1px solid var(--line);
              border-radius: 6px;
              padding: 12px;
              background: #fbfcf8;
            }
            .rtneat-detail h3 {
              margin: 0 0 10px;
              font-size: 1rem;
            }
            .rtneat-detail dl {
              display: grid;
              grid-template-columns: auto 1fr;
              gap: 6px 12px;
              margin: 0;
            }
            .rtneat-detail dt {
              color: var(--muted);
              font-size: 0.78rem;
              text-transform: uppercase;
            }
            .rtneat-detail dd {
              margin: 0;
              font-weight: 650;
              overflow-wrap: anywhere;
            }
            .lineage-tree-frame {
              position: relative;
              overflow: auto;
              padding: 10px;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #fbfcf8;
            }
            .lineage-toolbar {
              display: flex;
              flex-wrap: wrap;
              gap: 8px;
              align-items: center;
              justify-content: space-between;
              margin: 0 0 8px;
              color: var(--muted);
              font-size: 0.82rem;
            }
            .lineage-toolbar button {
              padding: 5px 9px;
              border: 1px solid var(--line);
              border-radius: 5px;
              background: #fff;
              color: var(--text);
              font: inherit;
              cursor: pointer;
            }
            .lineage-report-grid {
              display: grid;
              grid-template-columns: minmax(0, 1fr) minmax(300px, 380px);
              gap: 14px;
              margin-top: 14px;
              align-items: start;
            }
            .lineage-tree {
              display: block;
              width: 100%;
              max-width: 100%;
              height: auto;
              font-family: "Segoe UI", system-ui, sans-serif;
              touch-action: none;
              user-select: none;
            }
            .lineage-tree text {
              fill: var(--muted);
              font-size: 11px;
            }
            .lineage-segment-node {
              cursor: pointer;
            }
            .lineage-segment-node text {
              pointer-events: none;
            }
            .lineage-segment-node rect {
              transition: stroke-width 0.12s ease, filter 0.12s ease;
            }
            .lineage-segment-node.is-selected rect,
            .lineage-segment-node:focus-visible rect {
              stroke: #172015;
              stroke-width: 3;
              filter: drop-shadow(0 2px 4px rgba(23, 32, 21, 0.24));
            }
            .lineage-detail-panel {
              padding: 12px;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #fbfcf8;
            }
            .lineage-detail-panel h3 {
              margin: 0 0 8px;
              font-size: 1rem;
            }
            .lineage-detail-panel dl {
              display: grid;
              grid-template-columns: auto 1fr;
              gap: 6px 10px;
              margin: 0;
            }
            .lineage-detail-panel dt {
              color: var(--muted);
              font-size: 0.78rem;
              text-transform: uppercase;
            }
            .lineage-detail-panel dd {
              margin: 0;
              font-weight: 650;
              overflow-wrap: anywhere;
            }
            .lineage-detail-panel dd + dt {
              margin-top: 3px;
            }
            .lineage-tree-legend {
              display: flex;
              flex-wrap: wrap;
              gap: 8px 14px;
              margin-top: 10px;
              color: var(--muted);
              font-size: 0.84rem;
            }
            @media (max-width: 820px) {
              .lineage-report-grid { grid-template-columns: 1fr; }
            }
            .heatmap-grid {
              display: grid;
              grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
              gap: 16px;
            }
            .heatmap-card p {
              margin: 0 0 8px;
              color: var(--muted);
              font-size: 0.84rem;
            }
            .heatmap {
              display: block;
              width: 100%;
              height: auto;
              max-height: 420px;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #eef3e8;
            }
            .heatmap-legend {
              display: flex;
              flex-wrap: wrap;
              gap: 8px 12px;
              margin-top: 8px;
              color: var(--muted);
              font-size: 0.82rem;
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
            .chart-series-line {
              transition: opacity 0.12s ease, stroke-width 0.12s ease;
            }
            .chart-legend-item {
              padding: 1px 3px;
              border-radius: 4px;
              transition: opacity 0.12s ease, background-color 0.12s ease, color 0.12s ease;
            }
            .chart-card.is-series-highlighted .chart-series-line {
              opacity: 0.18;
              stroke-width: 1.6;
            }
            .chart-card.is-series-highlighted .chart-series-line.is-highlighted {
              opacity: 1;
              stroke-width: 4.8;
            }
            .chart-card.is-series-highlighted .chart-legend-item {
              opacity: 0.48;
            }
            .chart-card.is-series-highlighted .chart-legend-item.is-highlighted {
              opacity: 1;
              color: var(--accent);
              background: #eef2e9;
            }
            .legend-swatch {
              display: inline-block;
              width: 0.8em;
              height: 0.8em;
              margin-right: 5px;
              border-radius: 999px;
              vertical-align: -0.05em;
            }
            body.chart-lightbox-open { overflow: hidden; }
            .chart-lightbox[hidden] { display: none; }
            .chart-lightbox {
              position: fixed;
              inset: 0;
              z-index: 1000;
              display: flex;
              align-items: center;
              justify-content: center;
              padding: clamp(16px, 3vw, 36px);
              background: rgba(22, 32, 21, 0.72);
            }
            .chart-lightbox-panel {
              width: min(1180px, 100%);
              max-height: calc(100vh - 48px);
              overflow: auto;
              padding: 16px;
              border: 1px solid var(--line);
              border-radius: 8px;
              background: var(--panel);
              box-shadow: 0 24px 80px rgba(0, 0, 0, 0.28);
            }
            .chart-lightbox-close {
              display: block;
              margin: 0 0 12px auto;
              padding: 6px 10px;
              border: 1px solid var(--line);
              border-radius: 6px;
              background: #fbfcf8;
              color: var(--text);
              font: inherit;
              cursor: pointer;
            }
            .chart-lightbox-content .chart-card {
              padding: 0;
              border: 0;
              background: transparent;
              cursor: default;
            }
            .chart-lightbox-content .chart-card svg {
              width: 100%;
              max-height: 72vh;
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
        writer.WriteLine(
            """
            <script>
            (() => {
              const cards = Array.from(document.querySelectorAll(".chart-card"));
              if (cards.length === 0) {
                return;
              }

              const overlay = document.createElement("div");
              overlay.className = "chart-lightbox";
              overlay.hidden = true;
              overlay.setAttribute("role", "dialog");
              overlay.setAttribute("aria-modal", "true");
              overlay.innerHTML = "<div class=\"chart-lightbox-panel\"><button class=\"chart-lightbox-close\" type=\"button\" aria-label=\"Close enlarged chart\">Close</button><div class=\"chart-lightbox-content\"></div></div>";
              document.body.appendChild(overlay);

              const content = overlay.querySelector(".chart-lightbox-content");
              const closeButton = overlay.querySelector(".chart-lightbox-close");
              let previousFocus = null;

              function setSeriesHighlight(legendItem) {
                const card = legendItem.closest(".chart-card");
                if (!card) {
                  return;
                }

                const seriesIndex = legendItem.getAttribute("data-series-index");
                card.classList.add("is-series-highlighted");
                for (const line of card.querySelectorAll(".chart-series-line")) {
                  line.classList.toggle("is-highlighted", line.getAttribute("data-series-index") === seriesIndex);
                }

                for (const item of card.querySelectorAll(".chart-legend-item")) {
                  item.classList.toggle("is-highlighted", item.getAttribute("data-series-index") === seriesIndex);
                }
              }

              function clearSeriesHighlight(legendItem) {
                const card = legendItem.closest(".chart-card");
                if (!card) {
                  return;
                }

                card.classList.remove("is-series-highlighted");
                for (const active of card.querySelectorAll(".is-highlighted")) {
                  active.classList.remove("is-highlighted");
                }
              }

              function openChart(card) {
                previousFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
                content.replaceChildren(card.cloneNode(true));
                const clone = content.querySelector(".chart-card");
                if (clone) {
                  clone.removeAttribute("role");
                  clone.removeAttribute("tabindex");
                  clone.removeAttribute("aria-label");
                }

                overlay.hidden = false;
                document.body.classList.add("chart-lightbox-open");
                closeButton.focus();
              }

              function closeChart() {
                overlay.hidden = true;
                content.replaceChildren();
                document.body.classList.remove("chart-lightbox-open");
                if (previousFocus) {
                  previousFocus.focus();
                }
              }

              for (const card of cards) {
                card.addEventListener("click", () => openChart(card));
                card.addEventListener("keydown", event => {
                  if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    openChart(card);
                  }
                });
              }

              document.addEventListener("pointerover", event => {
                const item = event.target.closest(".chart-legend-item");
                if (item) {
                  setSeriesHighlight(item);
                }
              });
              document.addEventListener("pointerout", event => {
                const item = event.target.closest(".chart-legend-item");
                if (item && !item.contains(event.relatedTarget)) {
                  clearSeriesHighlight(item);
                }
              });
              closeButton.addEventListener("click", closeChart);
              overlay.addEventListener("click", event => {
                if (event.target === overlay) {
                  closeChart();
                }
              });
              document.addEventListener("keydown", event => {
                if (!overlay.hidden && event.key === "Escape") {
                  closeChart();
                }
              });
            })();
            </script>
            """);
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

    private static void WriteBiomeMapSection(TextWriter writer, BiomeMap map)
    {
        var width = MathF.Max(1f, map.Bounds.Width);
        var height = MathF.Max(1f, map.Bounds.Height);
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Biome Layout</h2>");
        writer.WriteLine(
            $"<p class=\"biome-map-note\">{Html(map.CellCountX)} x {Html(map.CellCountY)} cells at {Html(map.CellSize.ToString("0.###", CultureInfo.InvariantCulture))} world units per cell. Dashed outline marks the resource spawn area when a void border is configured.</p>");
        writer.WriteLine("<div class=\"biome-map-frame\">");
        writer.WriteLine($"<svg class=\"biome-map\" viewBox=\"0 0 {SvgNumber(width)} {SvgNumber(height)}\" role=\"img\" aria-label=\"Biome map layout\" preserveAspectRatio=\"xMidYMid meet\" shape-rendering=\"crispEdges\">");
        for (var y = 0; y < map.CellCountY; y++)
        {
            for (var x = 0; x < map.CellCountX; x++)
            {
                var cell = map.GetCellBounds(x, y);
                if (cell.Width <= 0f || cell.Height <= 0f)
                {
                    continue;
                }

                var kind = map.GetKind(x, y);
                writer.WriteLine(
                    $"<rect x=\"{SvgNumber(cell.X)}\" y=\"{SvgNumber(cell.Y)}\" width=\"{SvgNumber(cell.Width)}\" height=\"{SvgNumber(cell.Height)}\" fill=\"{Html(BiomeColor(kind))}\" />");
            }
        }

        if (map.ResourceVoidBorderWidth > 0f
            && map.ResourceVoidBorderWidth * 2f < width
            && map.ResourceVoidBorderWidth * 2f < height)
        {
            var border = map.ResourceVoidBorderWidth;
            writer.WriteLine(
                $"<rect class=\"biome-map-void\" x=\"{SvgNumber(border)}\" y=\"{SvgNumber(border)}\" width=\"{SvgNumber(width - border * 2f)}\" height=\"{SvgNumber(height - border * 2f)}\" />");
        }

        writer.WriteLine("</svg>");
        writer.WriteLine("</div>");
        writer.WriteLine("<div class=\"biome-legend\">");
        foreach (var biome in BiomeKinds.All)
        {
            writer.WriteLine(
                $"<span><span class=\"legend-swatch\" style=\"background:{Html(BiomeColor(biome))}\"></span>{Html(FormatBiomeKind(biome))}</span>");
        }

        writer.WriteLine("</div>");
        writer.WriteLine("</section>");
    }

    private static void WriteTemperatureMapSection(TextWriter writer, TemperatureMap map)
    {
        var width = MathF.Max(1f, map.Bounds.Width);
        var height = MathF.Max(1f, map.Bounds.Height);
        var summary = map.Summarize();
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Temperature Layout</h2>");
        writer.WriteLine(
            $"<p class=\"biome-map-note\">{Html(map.CellCountX)} x {Html(map.CellCountY)} cells at {Html(map.CellSize.ToString("0.###", CultureInfo.InvariantCulture))} world units per cell. Temperature index runs 0 cold, 50 temperate, 100 hot. Average {Html(FormatTemperatureIndex(summary.AverageTemperature))}, range {Html(FormatTemperatureIndex(summary.MinimumTemperature))} - {Html(FormatTemperatureIndex(summary.MaximumTemperature))}.</p>");
        writer.WriteLine("<div class=\"biome-map-frame\">");
        writer.WriteLine($"<svg class=\"biome-map\" viewBox=\"0 0 {SvgNumber(width)} {SvgNumber(height)}\" role=\"img\" aria-label=\"Temperature map layout\" preserveAspectRatio=\"xMidYMid meet\" shape-rendering=\"crispEdges\">");
        for (var y = 0; y < map.CellCountY; y++)
        {
            for (var x = 0; x < map.CellCountX; x++)
            {
                var cell = map.GetCellBounds(x, y);
                if (cell.Width <= 0f || cell.Height <= 0f)
                {
                    continue;
                }

                writer.WriteLine(
                    $"<rect x=\"{SvgNumber(cell.X)}\" y=\"{SvgNumber(cell.Y)}\" width=\"{SvgNumber(cell.Width)}\" height=\"{SvgNumber(cell.Height)}\" fill=\"{Html(TemperatureColor(map.GetTemperature(x, y)))}\" />");
            }
        }

        writer.WriteLine("</svg>");
        writer.WriteLine("</div>");
        writer.WriteLine("<div class=\"biome-legend\">");
        writer.WriteLine($"<span><span class=\"legend-swatch\" style=\"background:{Html(TemperatureColor(0f))}\"></span>cold</span>");
        writer.WriteLine($"<span><span class=\"legend-swatch\" style=\"background:{Html(TemperatureColor(0.5f))}\"></span>temperate</span>");
        writer.WriteLine($"<span><span class=\"legend-swatch\" style=\"background:{Html(TemperatureColor(1f))}\"></span>hot</span>");
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpatialHeatmapSection(
        TextWriter writer,
        BiomeMap biomeMap,
        SimulationSpatialHeatmaps heatmaps)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Spatial Heatmaps</h2>");
        if (heatmaps.CellCountX <= 0
            || heatmaps.CellCountY <= 0
            || !heatmaps.HasData)
        {
            writer.WriteLine("<p class=\"empty\">No spatial event heatmap data was recorded for this run.</p>");
            writer.WriteLine("</section>");
            return;
        }

        var meatCalories = CombineHeatmaps(heatmaps.MeatCaloriesEaten, heatmaps.EggCaloriesEaten);
        var exposureHours = ScaleHeatmap(heatmaps.CreatureExposureSeconds, 1f / 3600f);
        var layers = new[]
        {
            new SpatialHeatmapLayer(
                "Creature Exposure",
                "creature-hr",
                exposureHours,
                "#255f85",
                "Sampled creature-hours by location, based on the stats snapshot interval."),
            new SpatialHeatmapLayer(
                "Births",
                "births",
                heatmaps.Births,
                "#2f8f43",
                "Creature birth locations, including founders and hatched offspring."),
            new SpatialHeatmapLayer(
                "Deaths",
                "deaths",
                heatmaps.Deaths,
                "#b42318",
                "All creature death locations, regardless of cause."),
            new SpatialHeatmapLayer(
                "Starvation Deaths",
                "deaths",
                heatmaps.StarvationDeaths,
                "#d78325",
                "Creature death locations where starvation was the recorded cause."),
            new SpatialHeatmapLayer(
                "Injury Deaths",
                "deaths",
                heatmaps.InjuryDeaths,
                "#932f6d",
                "Creature death locations where attack injury was the recorded cause."),
            new SpatialHeatmapLayer(
                "Rotten Meat Deaths",
                "deaths",
                heatmaps.RottenMeatDeaths,
                "#5f4b8b",
                "Creature death locations attributed to rotten meat damage."),
            new SpatialHeatmapLayer(
                "Plant Eating",
                "raw kcal",
                heatmaps.PlantCaloriesEaten,
                "#6aaa2a",
                "Raw plant calories eaten at the plant patch location."),
            new SpatialHeatmapLayer(
                "Meat and Egg Eating",
                "raw kcal",
                meatCalories,
                "#c64b35",
                "Raw meat and egg calories eaten at the food location."),
            new SpatialHeatmapLayer(
                "Attack Damage",
                "damage",
                heatmaps.AttackDamage,
                "#3c5aa6",
                "Bite damage applied at the target creature location."),
            new SpatialHeatmapLayer(
                "Births per Creature Hour",
                "births/creature-hr",
                DivideHeatmaps(heatmaps.Births, exposureHours),
                "#1f7f4c",
                "Birth intensity normalized by sampled creature-hours in each cell."),
            new SpatialHeatmapLayer(
                "Deaths per Creature Hour",
                "deaths/creature-hr",
                DivideHeatmaps(heatmaps.Deaths, exposureHours),
                "#9d1f1f",
                "Death risk normalized by sampled creature-hours in each cell."),
            new SpatialHeatmapLayer(
                "Plant Eating per Creature Hour",
                "raw kcal/creature-hr",
                DivideHeatmaps(heatmaps.PlantCaloriesEaten, exposureHours),
                "#5f9d1f",
                "Plant calories eaten normalized by sampled creature-hours in each cell."),
            new SpatialHeatmapLayer(
                "Meat and Egg Eating per Creature Hour",
                "raw kcal/creature-hr",
                DivideHeatmaps(meatCalories, exposureHours),
                "#a8442f",
                "Meat and egg calories eaten normalized by sampled creature-hours in each cell."),
            new SpatialHeatmapLayer(
                "Attack Damage per Creature Hour",
                "damage/creature-hr",
                DivideHeatmaps(heatmaps.AttackDamage, exposureHours),
                "#2f4f9d",
                "Bite damage normalized by sampled creature-hours in each cell.")
        }.Where(layer => HeatmapTotal(layer.Values) > 0f).ToArray();

        if (layers.Length == 0)
        {
            writer.WriteLine("<p class=\"empty\">Spatial heatmaps were initialized, but every event layer is empty.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine(
            $"<p class=\"biome-map-note\">Events are aggregated into a {Html(heatmaps.CellCountX)} x {Html(heatmaps.CellCountY)} report grid. Creature exposure is sampled on the stats snapshot interval; per-creature-hour layers are estimates from those samples. Biome colors are shown faintly under each heat layer.</p>");
        writer.WriteLine("<div class=\"heatmap-grid\">");
        foreach (var layer in layers)
        {
            WriteSpatialHeatmapCard(writer, biomeMap, heatmaps, layer);
        }

        writer.WriteLine("</div>");
        writer.WriteLine("</section>");
    }

    private static void WriteSpatialHeatmapCard(
        TextWriter writer,
        BiomeMap biomeMap,
        SimulationSpatialHeatmaps heatmaps,
        SpatialHeatmapLayer layer)
    {
        var total = HeatmapTotal(layer.Values);
        var max = HeatmapMax(layer.Values);
        var width = MathF.Max(1f, heatmaps.WorldWidth);
        var height = MathF.Max(1f, heatmaps.WorldHeight);
        var cellWidth = width / Math.Max(1, heatmaps.CellCountX);
        var cellHeight = height / Math.Max(1, heatmaps.CellCountY);
        writer.WriteLine(
            $"<article class=\"chart-card heatmap-card\" role=\"button\" tabindex=\"0\" aria-label=\"Open {Html(layer.Title)} heatmap\">");
        writer.WriteLine($"<h3>{Html(layer.Title)}</h3>");
        writer.WriteLine($"<p>{Html(layer.Description)}</p>");
        writer.WriteLine($"<svg class=\"heatmap\" viewBox=\"0 0 {SvgNumber(width)} {SvgNumber(height)}\" role=\"img\" aria-label=\"{Html(layer.Title)} spatial heatmap\" preserveAspectRatio=\"xMidYMid meet\" shape-rendering=\"crispEdges\">");
        WriteBiomeHeatmapBackground(writer, biomeMap);
        for (var y = 0; y < heatmaps.CellCountY; y++)
        {
            for (var x = 0; x < heatmaps.CellCountX; x++)
            {
                var index = y * heatmaps.CellCountX + x;
                if (index < 0 || index >= layer.Values.Count)
                {
                    continue;
                }

                var value = layer.Values[index];
                if (value <= 0f)
                {
                    continue;
                }

                var opacity = 0.14f + 0.78f * MathF.Sqrt(value / MathF.Max(0.000001f, max));
                writer.WriteLine(
                    $"<rect x=\"{SvgNumber(x * cellWidth)}\" y=\"{SvgNumber(y * cellHeight)}\" width=\"{SvgNumber(cellWidth)}\" height=\"{SvgNumber(cellHeight)}\" fill=\"{Html(layer.Color)}\" fill-opacity=\"{SvgNumber(opacity)}\">" +
                    $"<title>{Html(FormatHeatmapValue(value, layer.Units))}</title></rect>");
            }
        }

        writer.WriteLine("</svg>");
        writer.WriteLine("<div class=\"heatmap-legend\">");
        if (IsRateHeatmapUnit(layer.Units))
        {
            var activeCellCount = Math.Max(1, HeatmapActiveCellCount(layer.Values));
            writer.WriteLine(
                $"<span><span class=\"legend-swatch\" style=\"background:{Html(layer.Color)}\"></span>Mean active cell {Html(FormatHeatmapValue(total / activeCellCount, layer.Units))}</span>");
        }
        else
        {
            writer.WriteLine(
                $"<span><span class=\"legend-swatch\" style=\"background:{Html(layer.Color)}\"></span>Total {Html(FormatHeatmapValue(total, layer.Units))}</span>");
        }

        writer.WriteLine($"<span>Peak cell {Html(FormatHeatmapValue(max, layer.Units))}</span>");
        writer.WriteLine("</div>");
        writer.WriteLine("</article>");
    }

    private static void WriteBiomeHeatmapBackground(TextWriter writer, BiomeMap map)
    {
        for (var y = 0; y < map.CellCountY; y++)
        {
            for (var x = 0; x < map.CellCountX; x++)
            {
                var cell = map.GetCellBounds(x, y);
                if (cell.Width <= 0f || cell.Height <= 0f)
                {
                    continue;
                }

                writer.WriteLine(
                    $"<rect x=\"{SvgNumber(cell.X)}\" y=\"{SvgNumber(cell.Y)}\" width=\"{SvgNumber(cell.Width)}\" height=\"{SvgNumber(cell.Height)}\" fill=\"{Html(BiomeColor(map.GetKind(x, y)))}\" fill-opacity=\"0.32\" />");
            }
        }
    }

    private static float[] CombineHeatmaps(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        var length = Math.Max(left.Count, right.Count);
        var combined = new float[length];
        for (var i = 0; i < combined.Length; i++)
        {
            combined[i] = (i < left.Count ? left[i] : 0f) + (i < right.Count ? right[i] : 0f);
        }

        return combined;
    }

    private static float[] ScaleHeatmap(IReadOnlyList<float> values, float scale)
    {
        var scaled = new float[values.Count];
        if (!float.IsFinite(scale) || scale <= 0f)
        {
            return scaled;
        }

        for (var i = 0; i < scaled.Length; i++)
        {
            var value = values[i];
            scaled[i] = float.IsFinite(value) && value > 0f ? value * scale : 0f;
        }

        return scaled;
    }

    private static float[] DivideHeatmaps(IReadOnlyList<float> numerator, IReadOnlyList<float> denominator)
    {
        var length = Math.Max(numerator.Count, denominator.Count);
        var divided = new float[length];
        for (var i = 0; i < divided.Length; i++)
        {
            var top = i < numerator.Count ? numerator[i] : 0f;
            var bottom = i < denominator.Count ? denominator[i] : 0f;
            divided[i] = float.IsFinite(top) && top > 0f && float.IsFinite(bottom) && bottom > 0f
                ? top / bottom
                : 0f;
        }

        return divided;
    }

    private static float HeatmapTotal(IReadOnlyList<float> values)
    {
        var total = 0f;
        foreach (var value in values)
        {
            if (float.IsFinite(value) && value > 0f)
            {
                total += value;
            }
        }

        return total;
    }

    private static float HeatmapMax(IReadOnlyList<float> values)
    {
        var max = 0f;
        foreach (var value in values)
        {
            if (float.IsFinite(value) && value > max)
            {
                max = value;
            }
        }

        return max;
    }

    private static int HeatmapActiveCellCount(IReadOnlyList<float> values)
    {
        var count = 0;
        foreach (var value in values)
        {
            if (float.IsFinite(value) && value > 0f)
            {
                count++;
            }
        }

        return count;
    }

    private static string FormatHeatmapValue(float value, string units)
    {
        var formatted = units is "births" or "deaths"
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.###", CultureInfo.InvariantCulture);
        return $"{formatted} {units}";
    }

    private static bool IsRateHeatmapUnit(string units)
    {
        return units.Contains('/', StringComparison.Ordinal);
    }

    private static void WriteSurvivorLineageTreeSection(
        StreamWriter writer,
        SurvivorLineageAnalysis analysis,
        WorldState state,
        SpeciesClusterHistory speciesHistory)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Survivor Ancestry Tree</h2>");
        if (analysis.LivingCreatureCount == 0 || analysis.Segments.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living creatures remain, so there is no survivor ancestry tree to draw.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine(
            "<p class=\"biome-map-note\">This view collapses straight creature ancestry into survivor lineage segments. Oldest ancestry is at the top, youngest survivors are at the bottom, and short extinct side branches are omitted.</p>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Living endpoints", analysis.LivingCreatureCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Ancestor nodes", analysis.AncestorCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Lineage segments", analysis.SegmentCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Founder roots", analysis.FounderCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Max generation", analysis.MaxGeneration.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Dominant founder", analysis.DominantFounderId == default ? "n/a" : $"#{analysis.DominantFounderId.Value}");
        WriteMetric(writer, "Dominant living", analysis.DominantFounderLivingDescendants.ToString(CultureInfo.InvariantCulture));
        writer.WriteLine("</div>");

        var graphSegments = SelectLineageSegmentsForGraph(analysis);
        var graphTruncated = false;
        if (graphSegments.Count > SurvivorLineageTreeNodeRenderLimit)
        {
            graphTruncated = true;
            graphSegments = analysis.Segments
                .Where(segment => segment.IsDominantPath)
                .ToArray();
            writer.WriteLine(
                $"<p class=\"empty\">The complete survivor ancestry has {Html(analysis.Segments.Count)} lineage segments, so the graph is limited to the dominant path. The lineage records still retain the full ancestry data.</p>");
        }
        else if (graphSegments.Count < analysis.Segments.Count)
        {
            writer.WriteLine(
                $"<p class=\"empty\">Displaying {Html(graphSegments.Count)} major lineage segments. Single-survivor terminal twigs are hidden to keep the tree readable; the dominant path and major surviving branches are retained.</p>");
        }

        WriteSurvivorLineageSegmentGraph(writer, graphSegments, analysis, state, speciesHistory, graphTruncated);
        WriteDominantLineagePathTable(writer, analysis);
        WriteLineageSegmentScript(writer);
        writer.WriteLine("</section>");
    }

    private static void WriteSurvivorLineageSegmentGraph(
        TextWriter writer,
        IReadOnlyList<SurvivorLineageSegment> segments,
        SurvivorLineageAnalysis analysis,
        WorldState state,
        SpeciesClusterHistory speciesHistory,
        bool graphTruncated)
    {
        if (segments.Count == 0)
        {
            return;
        }

        var layout = LayoutLineageSegments(segments, analysis.MaxGeneration);
        var width = layout.Width;
        var height = layout.Height;
        var generationStride = analysis.MaxGeneration <= 24 ? 1 : analysis.MaxGeneration <= 120 ? 5 : 25;

        writer.WriteLine("<div class=\"lineage-report-grid\" data-lineage-section>");
        writer.WriteLine("<div>");
        writer.WriteLine("<div class=\"lineage-tree-frame\">");
        writer.WriteLine("<div class=\"lineage-toolbar\"><span>Drag to pan. Wheel to zoom. Click a card for details.</span><button type=\"button\" data-lineage-reset>Reset view</button></div>");
        writer.WriteLine($"<svg class=\"lineage-tree\" data-lineage-panzoom data-lineage-viewbox=\"0 0 {SvgNumber(width)} {SvgNumber(height)}\" viewBox=\"0 0 {SvgNumber(width)} {SvgNumber(height)}\" role=\"img\" aria-label=\"Survivor lineage segment tree\">");
        writer.WriteLine("<rect x=\"0\" y=\"0\" width=\"100%\" height=\"100%\" fill=\"#fbfcf8\" />");
        for (var generation = 0; generation <= analysis.MaxGeneration; generation += generationStride)
        {
            var y = layout.YForGeneration(generation);
            writer.WriteLine($"<line x1=\"24\" y1=\"{SvgNumber(y)}\" x2=\"{SvgNumber(width - 24f)}\" y2=\"{SvgNumber(y)}\" stroke=\"#e3e8dc\" stroke-width=\"1\" />");
            writer.WriteLine($"<text x=\"28\" y=\"{SvgNumber(y - 4f)}\">g{Html(generation)}</text>");
        }

        foreach (var segment in segments)
        {
            if (segment.ParentSegmentId is null || !layout.ById.TryGetValue(segment.ParentSegmentId, out var parent))
            {
                continue;
            }

            var child = layout.ById[segment.SegmentId];
            var midY = (parent.BoxBottomY + child.BoxTopY) * 0.5f;
            var stroke = parent.Segment.IsDominantPath && child.Segment.IsDominantPath ? "#172015" : "#aab5a4";
            var strokeWidth = parent.Segment.IsDominantPath && child.Segment.IsDominantPath ? 2.4f : 1.2f;
            writer.WriteLine(
                $"<path d=\"M {SvgNumber(parent.X)} {SvgNumber(parent.BoxBottomY)} C {SvgNumber(parent.X)} {SvgNumber(midY)} {SvgNumber(child.X)} {SvgNumber(midY)} {SvgNumber(child.X)} {SvgNumber(child.BoxTopY)}\" fill=\"none\" stroke=\"{stroke}\" stroke-width=\"{SvgNumber(strokeWidth)}\" stroke-opacity=\"0.8\" />");
        }

        foreach (var item in layout.Items)
        {
            var segment = item.Segment;
            var strokeWidth = segment.IsDominantPath ? 2.4f : 1.1f;
            var branchWidth = 1.8f + 10f * MathF.Sqrt(segment.LivingDescendantCount / MathF.Max(1f, analysis.LivingCreatureCount));
            writer.WriteLine(
                $"<line x1=\"{SvgNumber(item.X)}\" y1=\"{SvgNumber(item.StartY)}\" x2=\"{SvgNumber(item.X)}\" y2=\"{SvgNumber(item.BoxTopY)}\" stroke=\"{Html(LineageSegmentColor(segment))}\" stroke-width=\"{SvgNumber(branchWidth)}\" stroke-linecap=\"round\" stroke-opacity=\"0.34\" />");
            var fill = segment.IsDominantPath
                ? "#172015"
                : segment.IsLivingEndpoint
                    ? "#2f8f43"
                    : segment.ChildSegmentCount > 1
                        ? "#d69d2f"
                        : "#6a8fce";
            var stroke = segment.HasGenomePayload && segment.HasBrainPayload ? "#ffffff" : "#b45309";
            var title = FormatLineageSegmentGraphTitle(segment);
            var speciesLabel = TryResolveLineageSpeciesName(segment.EndRecord, speciesHistory, out var speciesName)
                ? speciesName
                : "Species unclustered";
            var detail = FormatLineageSegmentDetailData(segment, state, speciesHistory);
            var tooltip = FormatLineageSegmentPlainDetail(segment, state, speciesHistory);
            writer.WriteLine(
                $"<g class=\"lineage-segment-node{(segment.IsDominantPath ? " is-dominant" : string.Empty)}\" tabindex=\"0\" role=\"button\" data-lineage-title=\"{Html(title)}\" data-lineage-detail=\"{Html(detail)}\">");
            writer.WriteLine(
                $"<rect x=\"{SvgNumber(item.BoxX)}\" y=\"{SvgNumber(item.BoxY)}\" width=\"{SvgNumber(item.BoxWidth)}\" height=\"{SvgNumber(item.BoxHeight)}\" rx=\"6\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{SvgNumber(strokeWidth)}\"><title>{Html(tooltip)}</title></rect>");
            writer.WriteLine($"<text x=\"{SvgNumber(item.BoxX + 8f)}\" y=\"{SvgNumber(item.BoxY + 17f)}\" style=\"fill:#fff\">{Html(TrimLineageGraphLabel(title, 22))}</text>");
            writer.WriteLine($"<text x=\"{SvgNumber(item.BoxX + 8f)}\" y=\"{SvgNumber(item.BoxY + 33f)}\" style=\"fill:#fff; opacity:0.82\">{Html(TrimLineageGraphLabel(speciesLabel, 22))}</text>");
            writer.WriteLine($"<text x=\"{SvgNumber(item.BoxX + 8f)}\" y=\"{SvgNumber(item.BoxY + 49f)}\" style=\"fill:#fff\">{Html($"g{segment.StartRecord.Generation}-{segment.EndRecord.Generation}, {segment.LivingDescendantCount} living")}</text>");
            writer.WriteLine("</g>");
        }

        writer.WriteLine("</svg>");
        writer.WriteLine("</div>");
        writer.WriteLine("<div class=\"lineage-tree-legend\">");
        writer.WriteLine("<span><span class=\"legend-swatch\" style=\"background:#172015\"></span>Representative path from dominant founder</span>");
        writer.WriteLine("<span><span class=\"legend-swatch\" style=\"background:#2f8f43\"></span>Living endpoint segment</span>");
        writer.WriteLine("<span><span class=\"legend-swatch\" style=\"background:#d69d2f\"></span>Branching segment</span>");
        writer.WriteLine("<span><span class=\"legend-swatch\" style=\"background:#6a8fce\"></span>Linear segment</span>");
        writer.WriteLine("<span><span class=\"legend-swatch\" style=\"background:#b45309\"></span>Orange border: missing genome/brain payload</span>");
        if (graphTruncated)
        {
            writer.WriteLine("<span>Graph is dominant-path only because the complete tree is too large for an inline SVG.</span>");
        }

        writer.WriteLine("</div>");
        writer.WriteLine("</div>");
        writer.WriteLine("<aside class=\"lineage-detail-panel\" aria-live=\"polite\">");
        writer.WriteLine("<h3 data-lineage-detail-title>Lineage detail</h3>");
        writer.WriteLine("<dl data-lineage-detail-body><dt>Select</dt><dd>Click a lineage box in the graph.</dd></dl>");
        writer.WriteLine("</aside>");
        writer.WriteLine("</div>");
    }

    private static IReadOnlyList<SurvivorLineageSegment> SelectLineageSegmentsForGraph(SurvivorLineageAnalysis analysis)
    {
        if (analysis.Segments.Count <= 48)
        {
            return analysis.Segments;
        }

        var byId = analysis.Segments.ToDictionary(segment => segment.SegmentId, StringComparer.Ordinal);
        var keep = new HashSet<string>(StringComparer.Ordinal);
        var minLivingDescendants = Math.Max(2, (int)MathF.Ceiling(analysis.LivingCreatureCount * 0.015f));

        foreach (var segment in analysis.Segments
            .Where(segment => segment.IsDominantPath
                || segment.ChildSegmentCount > 0
                || segment.LivingDescendantCount >= minLivingDescendants))
        {
            AddLineageSegmentAndParents(segment, byId, keep);
        }

        var rootSegments = analysis.Segments
            .Where(segment => segment.ParentSegmentId is null)
            .OrderByDescending(segment => segment.LivingDescendantCount)
            .ThenBy(segment => segment.SegmentId, StringComparer.Ordinal)
            .Take(16);
        foreach (var root in rootSegments)
        {
            AddLineageSegmentAndParents(root, byId, keep);
        }

        var selected = analysis.Segments
            .Where(segment => keep.Contains(segment.SegmentId))
            .ToArray();
        if (selected.Length <= SurvivorLineageTreeNodeRenderLimit)
        {
            return selected;
        }

        keep.Clear();
        foreach (var segment in analysis.Segments
            .Where(segment => segment.IsDominantPath || segment.ChildSegmentCount > 0)
            .Concat(analysis.Segments
                .OrderByDescending(segment => segment.LivingDescendantCount)
                .ThenBy(segment => segment.SegmentId, StringComparer.Ordinal)
                .Take(SurvivorLineageTreeNodeRenderLimit / 2)))
        {
            AddLineageSegmentAndParents(segment, byId, keep);
        }

        return analysis.Segments
            .Where(segment => keep.Contains(segment.SegmentId))
            .Take(SurvivorLineageTreeNodeRenderLimit)
            .ToArray();
    }

    private static void AddLineageSegmentAndParents(
        SurvivorLineageSegment segment,
        IReadOnlyDictionary<string, SurvivorLineageSegment> byId,
        ISet<string> keep)
    {
        var current = segment;
        while (keep.Add(current.SegmentId)
            && current.ParentSegmentId is not null
            && byId.TryGetValue(current.ParentSegmentId, out var parent))
        {
            current = parent;
        }
    }

    private static LineageSegmentLayout LayoutLineageSegments(
        IReadOnlyList<SurvivorLineageSegment> segments,
        int maxGeneration)
    {
        var byId = segments.ToDictionary(segment => segment.SegmentId);
        var childrenByParent = segments
            .Where(segment => segment.ParentSegmentId is not null && byId.ContainsKey(segment.ParentSegmentId))
            .GroupBy(segment => segment.ParentSegmentId!)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(segment => segment.LivingDescendantCount)
                    .ThenBy(segment => segment.EndRecord.Generation)
                    .ThenBy(segment => segment.SegmentId, StringComparer.Ordinal)
                    .ToArray());
        var roots = segments
            .Where(segment => segment.ParentSegmentId is null || !byId.ContainsKey(segment.ParentSegmentId))
            .OrderByDescending(segment => segment.LivingDescendantCount)
            .ThenBy(segment => segment.SegmentId, StringComparer.Ordinal)
            .ToArray();
        var laneById = new Dictionary<string, float>(StringComparer.Ordinal);
        var nextLane = 0;

        float AssignLane(SurvivorLineageSegment segment)
        {
            if (laneById.TryGetValue(segment.SegmentId, out var existing))
            {
                return existing;
            }

            if (!childrenByParent.TryGetValue(segment.SegmentId, out var children) || children.Length == 0)
            {
                var leafLane = nextLane++;
                laneById[segment.SegmentId] = leafLane;
                return leafLane;
            }

            var total = 0f;
            foreach (var child in children)
            {
                total += AssignLane(child);
            }

            var lane = total / children.Length;
            laneById[segment.SegmentId] = lane;
            return lane;
        }

        foreach (var root in roots)
        {
            AssignLane(root);
        }

        foreach (var segment in segments)
        {
            AssignLane(segment);
        }

        var laneCount = Math.Max(1, nextLane);
        const float plotLeft = 112f;
        const float laneStride = 156f;
        var plotWidth = MathF.Max(900f, MathF.Max(1f, laneCount - 1f) * laneStride);
        const float top = 48f;
        var plotHeight = MathF.Max(640f, Math.Max(1, maxGeneration) * 72f);
        const float boxWidth = 146f;
        const float boxHeight = 58f;
        var width = plotLeft + plotWidth + 112f;
        var items = segments
            .Select(segment =>
            {
                var x = laneCount == 1
                    ? width * 0.5f
                    : plotLeft + laneById[segment.SegmentId] / MathF.Max(1f, laneCount - 1f) * plotWidth;
                var startY = top + segment.StartRecord.Generation / MathF.Max(1f, maxGeneration) * plotHeight;
                var endY = top + segment.EndRecord.Generation / MathF.Max(1f, maxGeneration) * plotHeight;
                var boxY = endY - boxHeight * 0.5f;
                return new LineageSegmentLayoutItem(
                    segment,
                    x,
                    startY,
                    endY,
                    x - boxWidth * 0.5f,
                    boxY,
                    boxWidth,
                    boxHeight);
            })
            .OrderBy(item => item.Segment.StartRecord.Generation)
            .ThenByDescending(item => item.Segment.LivingDescendantCount)
            .ThenBy(item => item.Segment.SegmentId, StringComparer.Ordinal)
            .ToArray();
        return new LineageSegmentLayout(
            items,
            items.ToDictionary(item => item.Segment.SegmentId, StringComparer.Ordinal),
            width,
            top + plotHeight + 64f,
            generation => top + generation / MathF.Max(1f, maxGeneration) * plotHeight);
    }

    private static void WriteDominantLineagePathTable(
        StreamWriter writer,
        SurvivorLineageAnalysis analysis)
    {
        writer.WriteLine("<h3>Dominant Ancestor Path</h3>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Step</th><th>Creature</th><th>Generation</th><th>Living Descendants</th><th>Surviving Children</th><th>Born</th><th>Status</th><th>Payload</th></tr></thead>");
        writer.WriteLine("<tbody>");
        for (var i = 0; i < analysis.DominantPath.Count; i++)
        {
            var step = analysis.DominantPath[i];
            var record = step.Record;
            var payload = record.GenomeId >= 0 && record.BrainId >= 0 ? "Genome+brain kept" : "Payload pruned";
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(i + 1)}</td>" +
                $"<td>#{Html(record.Id.Value)}</td>" +
                $"<td>{Html(record.Generation)}</td>" +
                $"<td>{Html(step.LivingDescendantCount)}</td>" +
                $"<td>{Html(step.ChildCount)}</td>" +
                $"<td>{Html($"tick {record.BirthTick}")}</td>" +
                $"<td>{Html(FormatLineageStatus(record))}</td>" +
                $"<td>{Html(payload)}</td>" +
                "</tr>");
        }

        if (analysis.DominantPath.Count == 0)
        {
            writer.WriteLine("<tr><td class=\"empty\" colspan=\"8\">No dominant path could be reconstructed.</td></tr>");
        }

        writer.WriteLine("</tbody></table></div>");
    }

    private static string FormatLineageNodeTitle(SurvivorLineageNode node)
    {
        return $"Creature #{node.Record.Id.Value}, generation {node.Record.Generation}, {node.LivingDescendantCount} living descendants, {FormatLineageStatus(node.Record)}";
    }

    private static string FormatLineageSegmentName(
        SurvivorLineageSegment segment,
        SpeciesClusterHistory speciesHistory)
    {
        if (TryResolveLineageSpeciesName(segment.EndRecord, speciesHistory, out var name))
        {
            return name;
        }

        return segment.StartRecord.Id == segment.EndRecord.Id
            ? $"Lineage #{segment.EndRecord.Id.Value}"
            : $"Lineage #{segment.StartRecord.Id.Value}-{segment.EndRecord.Id.Value}";
    }

    private static string FormatLineageSegmentGraphTitle(SurvivorLineageSegment segment)
    {
        return segment.ParentSegmentId is null && segment.StartRecord.Generation == 0
            ? $"Founder #{segment.StartRecord.Id.Value}"
            : FormatLineageSegmentFallbackId(segment);
    }

    private static bool TryResolveLineageSpeciesName(
        CreatureLineageRecord record,
        SpeciesClusterHistory speciesHistory,
        out string name)
    {
        if (speciesHistory.RecordClusterById.TryGetValue(record.Id, out var speciesId))
        {
            name = SpeciesClusterAnalyzer.GenerateName(speciesId);
            return true;
        }

        name = string.Empty;
        return false;
    }

    private static string TrimLineageGraphLabel(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return string.Concat(value.AsSpan(0, Math.Max(1, maxLength - 1)), "...");
    }

    private static string FormatLineageSegmentFallbackId(SurvivorLineageSegment segment)
    {
        return segment.StartRecord.Id == segment.EndRecord.Id
            ? $"L {segment.EndRecord.Id.Value}"
            : $"L {segment.StartRecord.Id.Value}-{segment.EndRecord.Id.Value}";
    }

    private static string FormatLineageSegmentDetailData(
        SurvivorLineageSegment segment,
        WorldState state,
        SpeciesClusterHistory speciesHistory)
    {
        return string.Join(
            "||",
            BuildLineageSegmentDetailEntries(segment, state, speciesHistory)
                .Select(entry => $"{entry.Label}::{entry.Value}"));
    }

    private static string FormatLineageSegmentPlainDetail(
        SurvivorLineageSegment segment,
        WorldState state,
        SpeciesClusterHistory speciesHistory)
    {
        return string.Join(
            " | ",
            BuildLineageSegmentDetailEntries(segment, state, speciesHistory)
                .Select(entry => $"{entry.Label}: {entry.Value}"));
    }

    private static IReadOnlyList<LineageDetailEntry> BuildLineageSegmentDetailEntries(
        SurvivorLineageSegment segment,
        WorldState state,
        SpeciesClusterHistory speciesHistory)
    {
        var payload = segment.HasGenomePayload && segment.HasBrainPayload ? "kept" : "partly pruned";
        var entries = new List<LineageDetailEntry>
        {
            new("Species", FormatLineageSegmentName(segment, speciesHistory)),
            new("Segment", FormatLineageSegmentFallbackId(segment)),
            new("Starts at", $"#{segment.StartRecord.Id.Value}, generation {segment.StartRecord.Generation}"),
            new("Endpoint", $"#{segment.EndRecord.Id.Value} ({FormatLineageStatus(segment.EndRecord)})"),
            new("Generations", $"{segment.StartRecord.Generation}-{segment.EndRecord.Generation}"),
            new("Ancestor nodes", segment.AncestorCount.ToString(CultureInfo.InvariantCulture)),
            new("Living descendants", segment.LivingDescendantCount.ToString(CultureInfo.InvariantCulture)),
            new("Child segments", segment.ChildSegmentCount.ToString(CultureInfo.InvariantCulture)),
            new("Graph role", FormatLineageSegmentGraphRole(segment)),
            new("Birth ticks", $"{segment.StartRecord.BirthTick}-{segment.EndRecord.BirthTick}"),
            new("Payload", payload)
        };

        if (state.TryGetGenome(segment.EndRecord.GenomeId, out var genome))
        {
            entries.Add(new("Body genes", $"radius {FormatCompactNumber(genome.BodyRadius)}, speed {FormatCompactNumber(genome.MaxSpeed)}, turn {FormatCompactNumber(genome.MaxTurnRadiansPerSecond)} rad/s"));
            entries.Add(new("Sense genes", $"range {FormatCompactNumber(genome.SenseRadius)}, vision {FormatCompactNumber(genome.VisionAngleRadians * 180f / MathF.PI)} deg"));
            entries.Add(new("Energy genes", $"basal {FormatCompactNumber(genome.BasalEnergyPerSecond)}/s, move {FormatCompactNumber(genome.MovementEnergyPerSecond)}/s, eat {FormatCompactNumber(genome.EatCaloriesPerSecond)}/s"));
            entries.Add(new("Repro genes", $"threshold {FormatCompactNumber(genome.ReproductionEnergyThreshold)}, investment {FormatCompactNumber(genome.OffspringEnergyInvestment)}, cooldown {FormatCompactNumber(genome.ReproductionCooldownSeconds)}s"));
            entries.Add(new("Diet genes", $"diet {FormatCompactNumber(genome.DietaryAdaptation)}, carrion {FormatCompactNumber(genome.CarrionAdaptation)}, tender/rich/tough {FormatCompactNumber(genome.TenderPlantAdaptation)}/{FormatCompactNumber(genome.RichPlantAdaptation)}/{FormatCompactNumber(genome.ToughPlantAdaptation)}"));
            entries.Add(new("Combat genes", $"bite {FormatCompactNumber(genome.BiteStrength)}, resist {FormatCompactNumber(genome.DamageResistance)}"));
            entries.Add(new("Digest genes", $"gut {FormatCompactNumber(genome.GutCapacityCalories)}, digest {FormatCompactNumber(genome.DigestionCaloriesPerSecond)}/s"));
            entries.Add(new("Fat genes", $"capacity {FormatCompactNumber(genome.FatStorageCapacityCalories)}, efficiency {FormatCompactNumber(genome.FatStorageEfficiency)}"));
        }
        else
        {
            entries.Add(new("Genome", "pruned or unavailable"));
        }

        if (state.TryGetBrain(segment.EndRecord.BrainId, out var brain) && brain is not null)
        {
            var architecture = state.GetBrainArchitectureKind(segment.EndRecord.BrainId);
            var directMean = brain.Weights.Length > 0
                ? brain.Weights.Take(NeuralBrainGenome.DirectWeightCount).Average(weight => Math.Abs(weight))
                : 0f;
            entries.Add(new("Brain", $"{FormatBrainArchitectureKind(architecture)}, hidden {brain.HiddenNodeCount}, weights {brain.Weights.Length}"));
            entries.Add(new("Brain magnitude", $"direct mean |w| {FormatCompactNumber((float)directMean)}, hidden in {FormatCompactNumber(brain.SumAbsoluteHiddenInputWeights())}, hidden out {FormatCompactNumber(brain.SumAbsoluteHiddenOutputWeights())}"));
            entries.Add(new("Forage weights", $"plant center {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.VisionSectorPlantProximityInput(VisionSectorSet.CenterSectorIndex)))}, meat center {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.VisionSectorMeatProximityInput(VisionSectorSet.CenterSectorIndex)))}, eat freshness {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.EatOutput, NeuralBrainSchema.VisibleMeatFreshnessInput))}"));
            entries.Add(new("Risk weights", $"rot move {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.RottenMeatScentForwardInput))}, terrain drag {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.MoveForwardOutput, NeuralBrainSchema.ForwardTerrainDragInput))}"));
            entries.Add(new("Attack weights", $"small center {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.AttackOutput, NeuralBrainSchema.VisionSectorSmallerCreatureProximityInput(VisionSectorSet.CenterSectorIndex)))}, contact {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.AttackOutput, NeuralBrainSchema.CreatureContactInput))}, approach {FormatSignedBrainWeight(brain.GetWeight(NeuralBrainSchema.AttackOutput, NeuralBrainSchema.VisionSectorCreatureApproachRateInput(VisionSectorSet.CenterSectorIndex)))}"));
        }
        else
        {
            entries.Add(new("Brain", "pruned or unavailable"));
        }

        return entries;
    }

    private static string FormatCompactNumber(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string LineageSegmentColor(SurvivorLineageSegment segment)
    {
        return segment.IsDominantPath
            ? "#172015"
            : segment.IsLivingEndpoint
                ? "#2f8f43"
                : segment.ChildSegmentCount > 1
                    ? "#d69d2f"
                    : "#6a8fce";
    }

    private static string FormatLineageSegmentGraphRole(SurvivorLineageSegment segment)
    {
        if (segment.IsDominantPath)
        {
            return "representative path from dominant founder";
        }

        if (segment.IsLivingEndpoint)
        {
            return "living endpoint segment";
        }

        return segment.ChildSegmentCount > 1
            ? "branching segment"
            : "linear segment";
    }

    private static void WriteLineageSegmentScript(TextWriter writer)
    {
        writer.WriteLine(
            """
            <script>
            (() => {
              for (const section of document.querySelectorAll('[data-lineage-section]')) {
                const title = section.querySelector('[data-lineage-detail-title]');
                const body = section.querySelector('[data-lineage-detail-body]');
                const nodes = Array.from(section.querySelectorAll('.lineage-segment-node'));
                const select = node => {
                  for (const other of nodes) {
                    other.classList.toggle('is-selected', other === node);
                  }
                  if (title) {
                    title.textContent = node.getAttribute('data-lineage-title') || 'Lineage detail';
                  }
                  if (body) {
                    body.replaceChildren();
                    const parts = (node.getAttribute('data-lineage-detail') || '').split('||').filter(Boolean);
                    for (const part of parts) {
                      const index = part.indexOf('::');
                      const label = index > 0 ? part.slice(0, index) : 'Detail';
                      const value = index > 0 ? part.slice(index + 2) : part;
                      const dt = document.createElement('dt');
                      const dd = document.createElement('dd');
                      dt.textContent = label;
                      dd.textContent = value;
                      body.append(dt, dd);
                    }
                  }
                };
                for (const node of nodes) {
                  node.addEventListener('pointerdown', event => event.stopPropagation());
                  node.addEventListener('click', event => {
                    event.stopPropagation();
                    select(node);
                  });
                  node.addEventListener('keydown', event => {
                    if (event.key === 'Enter' || event.key === ' ') {
                      event.preventDefault();
                      select(node);
                    }
                  });
                }
                const initial = section.querySelector('.lineage-segment-node.is-dominant') || nodes[0];
                if (initial) {
                  select(initial);
                }

                const resetButtons = Array.from(section.querySelectorAll('[data-lineage-reset]'));
                const svgs = Array.from(section.querySelectorAll('[data-lineage-panzoom]'));
                const resetSvg = svg => {
                  const raw = svg.getAttribute('data-lineage-viewbox') || svg.getAttribute('viewBox');
                  if (raw) {
                    svg.setAttribute('viewBox', raw);
                  }
                };
                resetButtons.forEach(button => {
                  button.addEventListener('click', () => svgs.forEach(resetSvg));
                });
                for (const svg of svgs) {
                  let viewBox = (svg.getAttribute('viewBox') || '0 0 1 1').split(/\s+/).map(Number);
                  let dragStart = null;
                  const apply = () => svg.setAttribute('viewBox', viewBox.map(value => Number.isFinite(value) ? value.toFixed(3) : '0').join(' '));
                  const point = event => {
                    const rect = svg.getBoundingClientRect();
                    return {
                      x: viewBox[0] + (event.clientX - rect.left) / Math.max(1, rect.width) * viewBox[2],
                      y: viewBox[1] + (event.clientY - rect.top) / Math.max(1, rect.height) * viewBox[3]
                    };
                  };
                  svg.addEventListener('wheel', event => {
                    event.preventDefault();
                    viewBox = (svg.getAttribute('viewBox') || '0 0 1 1').split(/\s+/).map(Number);
                    const before = point(event);
                    const factor = event.deltaY < 0 ? 0.82 : 1.22;
                    const nextWidth = Math.min(Math.max(viewBox[2] * factor, 220), 12000);
                    const nextHeight = Math.min(Math.max(viewBox[3] * factor, 180), 12000);
                    viewBox[0] = before.x - (before.x - viewBox[0]) * (nextWidth / viewBox[2]);
                    viewBox[1] = before.y - (before.y - viewBox[1]) * (nextHeight / viewBox[3]);
                    viewBox[2] = nextWidth;
                    viewBox[3] = nextHeight;
                    apply();
                  }, { passive: false });
                  svg.addEventListener('pointerdown', event => {
                    if (event.button !== 0) return;
                    if (event.target.closest && event.target.closest('.lineage-segment-node')) return;
                    viewBox = (svg.getAttribute('viewBox') || '0 0 1 1').split(/\s+/).map(Number);
                    dragStart = { x: event.clientX, y: event.clientY, viewBox: [...viewBox] };
                    svg.setPointerCapture(event.pointerId);
                  });
                  svg.addEventListener('pointermove', event => {
                    if (!dragStart) return;
                    const rect = svg.getBoundingClientRect();
                    viewBox[0] = dragStart.viewBox[0] - (event.clientX - dragStart.x) / Math.max(1, rect.width) * dragStart.viewBox[2];
                    viewBox[1] = dragStart.viewBox[1] - (event.clientY - dragStart.y) / Math.max(1, rect.height) * dragStart.viewBox[3];
                    apply();
                  });
                  const clearDrag = () => { dragStart = null; };
                  svg.addEventListener('pointerup', clearDrag);
                  svg.addEventListener('pointercancel', clearDrag);
                }
              }
            })();
            </script>
            """);
    }

    private sealed record LineageSegmentLayout(
        IReadOnlyList<LineageSegmentLayoutItem> Items,
        IReadOnlyDictionary<string, LineageSegmentLayoutItem> ById,
        float Width,
        float Height,
        Func<int, float> YForGeneration);

    private sealed record LineageSegmentLayoutItem(
        SurvivorLineageSegment Segment,
        float X,
        float StartY,
        float EndY,
        float BoxX,
        float BoxY,
        float BoxWidth,
        float BoxHeight)
    {
        public float BoxTopY => BoxY;

        public float BoxBottomY => BoxY + BoxHeight;
    }

    private readonly record struct LineageDetailEntry(string Label, string Value);

    private static string FormatLineageStatus(CreatureLineageRecord record)
    {
        if (record.IsAlive)
        {
            return "alive";
        }

        var reason = record.DeathReason?.ToString() ?? "dead";
        return record.DeathTick.HasValue
            ? $"{reason} at tick {record.DeathTick.Value}"
            : reason;
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

    private static void WriteChartsSection(
        StreamWriter writer,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        int sourceSnapshotCount)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Graphs</h2>");
        if (snapshots.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No stats snapshots were recorded, so no graphs are available.</p>");
            writer.WriteLine("</section>");
            return;
        }

        if (sourceSnapshotCount > snapshots.Count)
        {
            writer.WriteLine($"<p class=\"empty\">Graphs and timeline report sections are sampled to {snapshots.Count.ToString("N0", CultureInfo.InvariantCulture)} of {sourceSnapshotCount.ToString("N0", CultureInfo.InvariantCulture)} stats snapshots. CSV sidecars retain the full-resolution data.</p>");
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
            "Fat storage",
            "%",
            snapshots,
            new ChartSeries("Reserve", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageFatRatio * 100f).ToArray()),
            new ChartSeries("Mass burden", "#b84a4a", snapshots.Select(snapshot => snapshot.AverageMassBurdenRatio * 100f).ToArray()),
            new ChartSeries("Speed retained", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageFatSpeedMultiplier * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Fat Storage Genes",
            " value",
            snapshots,
            new ChartSeries("Capacity", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageFatStorageCapacityCalories).ToArray()),
            new ChartSeries("Efficiency %", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageFatStorageEfficiency * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Resource calories",
            " kcal",
            snapshots,
            new ChartSeries("Plants", "#35a862", snapshots.Select(snapshot => snapshot.TotalPlantCalories).ToArray()),
            new ChartSeries("Meat", "#b84a4a", snapshots.Select(snapshot => snapshot.TotalMeatCalories).ToArray()));
        WriteLineChart(
            writer,
            "Plant type calories",
            " kcal",
            snapshots,
            new ChartSeries("Generic", "#35a862", snapshots.Select(GenericPlantTypeCalories).ToArray()),
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => snapshot.TenderPlantTypeCalories).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => snapshot.RichPlantTypeCalories).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => snapshot.ToughPlantTypeCalories).ToArray()));
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
            "Temperature exposure",
            "",
            snapshots,
            new ChartSeries("Map avg", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageMapTemperature * 100f).ToArray()),
            new ChartSeries("Creatures", "#c9492e", snapshots.Select(snapshot => snapshot.AverageCreatureTemperature * 100f).ToArray()),
            new ChartSeries("Plants", "#4b9b44", snapshots.Select(snapshot => snapshot.AveragePlantTemperature * 100f).ToArray()),
            new ChartSeries("Small prey", "#1b91a8", snapshots.Select(snapshot => snapshot.AverageSmallPreyTemperature * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Thermal adaptation",
            "",
            snapshots,
            new ChartSeries("Optimum", "#c9492e", snapshots.Select(snapshot => snapshot.AverageThermalOptimum * 100f).ToArray()),
            new ChartSeries("Tolerance", "#d69d2f", snapshots.Select(snapshot => snapshot.AverageThermalTolerance * 100f).ToArray()),
            new ChartSeries("Mismatch", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageCreatureThermalMismatch * 100f).ToArray()),
            new ChartSeries("Hot mismatch", "#b83a2e", snapshots.Select(snapshot => Share(snapshot.HotThermalMismatchCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Cold mismatch", "#2f74bc", snapshots.Select(snapshot => Share(snapshot.ColdThermalMismatchCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
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
            new ChartSeries("Desert", "#9a6b3b", snapshots.Select(snapshot => Share(snapshot.BarrenCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Scrubland", "#7f8f3a", snapshots.Select(snapshot => Share(snapshot.SparseCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Grassland", "#35a862", snapshots.Select(snapshot => Share(snapshot.GrasslandCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Fertile", "#178a4a", snapshots.Select(snapshot => Share(snapshot.RichCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Forest", "#0b5f2a", snapshots.Select(snapshot => Share(snapshot.ForestCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Wetland", "#15807b", snapshots.Select(snapshot => Share(snapshot.WetlandCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Tundra", "#9ab1b6", snapshots.Select(snapshot => Share(snapshot.TundraCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Highland", "#817565", snapshots.Select(snapshot => Share(snapshot.HighlandCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
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
            new ChartSeries("Damage-dealing %", "#d96b3b", snapshots.Select(snapshot => Share(snapshot.AttackingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Avg raw attack", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageAttackOutput).ToArray()),
            new ChartSeries("Avg touch attack", "#8f4cb8", snapshots.Select(snapshot => snapshot.AverageTouchingAttackOutput).ToArray()),
            new ChartSeries("Avg grab output", "#ff8a30", snapshots.Select(snapshot => snapshot.AverageGrabOutput).ToArray()),
            new ChartSeries("Avg touch grab", "#ffcc66", snapshots.Select(snapshot => snapshot.AverageCanGrabGrabOutput).ToArray()),
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
            "Plant type digestion",
            " energy/s",
            snapshots,
            new ChartSeries("Generic", "#35a862", snapshots.Select(GenericPlantDigestedEnergyPerSecond).ToArray()),
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => snapshot.TenderPlantDigestedEnergyPerSecond).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => snapshot.RichPlantDigestedEnergyPerSecond).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => snapshot.ToughPlantDigestedEnergyPerSecond).ToArray()));
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
            new ChartSeries("Fresh kill eaten/s", "#8f4cb8", snapshots.Select(snapshot => snapshot.TotalLivePreyCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Small prey/s", "#1aa6a0", snapshots.Select(snapshot => snapshot.TotalSmallPreyCaloriesEatenPerSecond).ToArray()));
        WriteLineChart(
            writer,
            "Plant type intake",
            " kcal/s",
            snapshots,
            new ChartSeries("Generic", "#35a862", snapshots.Select(GenericPlantCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => snapshot.TenderPlantCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => snapshot.RichPlantCaloriesEatenPerSecond).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => snapshot.ToughPlantCaloriesEatenPerSecond).ToArray()));
        WriteLineChart(
            writer,
            "Plant type intake share",
            "%",
            snapshots,
            new ChartSeries("Generic", "#35a862", snapshots.Select(snapshot => PlantTypeShare(GenericPlantCaloriesEatenPerSecond(snapshot), snapshot.TotalPlantCaloriesEatenPerSecond) * 100f).ToArray()),
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => PlantTypeShare(snapshot.TenderPlantCaloriesEatenPerSecond, snapshot.TotalPlantCaloriesEatenPerSecond) * 100f).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => PlantTypeShare(snapshot.RichPlantCaloriesEatenPerSecond, snapshot.TotalPlantCaloriesEatenPerSecond) * 100f).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => PlantTypeShare(snapshot.ToughPlantCaloriesEatenPerSecond, snapshot.TotalPlantCaloriesEatenPerSecond) * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Plant type intake per resource",
            " kcal/s/resource",
            snapshots,
            new ChartSeries("Generic", "#35a862", snapshots.Select(snapshot => PlantTypeIntakePerResource(GenericPlantCaloriesEatenPerSecond(snapshot), GenericPlantTypeResourceCount(snapshot))).ToArray()),
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => PlantTypeIntakePerResource(snapshot.TenderPlantCaloriesEatenPerSecond, snapshot.TenderPlantTypeResourceCount)).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => PlantTypeIntakePerResource(snapshot.RichPlantCaloriesEatenPerSecond, snapshot.RichPlantTypeResourceCount)).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => PlantTypeIntakePerResource(snapshot.ToughPlantCaloriesEatenPerSecond, snapshot.ToughPlantTypeResourceCount)).ToArray()));
        WriteLineChart(
            writer,
            "Plant payoff trace",
            "",
            snapshots,
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => snapshot.AverageTenderPlantPayoffTrace).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => snapshot.AverageRichPlantPayoffTrace).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => snapshot.AverageToughPlantPayoffTrace).ToArray()));
        WriteLineChart(
            writer,
            "Predation Diagnostics",
            "%",
            snapshots,
            new ChartSeries("Seeing creatures", "#6a8fce", snapshots.Select(snapshot => Share(snapshot.CreatureDetectedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Contact", "#d69d2f", snapshots.Select(snapshot => Share(snapshot.CreatureContactCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Intent", "#e05a47", snapshots.Select(snapshot => Share(snapshot.AttackIntentCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Intent touch", "#9d3434", snapshots.Select(snapshot => Share(snapshot.AttackIntentWhileTouchingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Near gate touch", "#8f4cb8", snapshots.Select(snapshot => Share(snapshot.RawAttackNearGateWhileTouchingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Fresh kill share", "#8f4cb8", snapshots.Select(snapshot => snapshot.FreshKillCaloriesEatenShare * 100f).ToArray()),
            new ChartSeries("Fresh meat share", "#d69d2f", snapshots.Select(snapshot => snapshot.FreshMeatCaloriesEatenShare * 100f).ToArray()),
            new ChartSeries("Meat energy share", "#b84a4a", snapshots.Select(snapshot => snapshot.MeatDigestedEnergyShare * 100f).ToArray()));
        WriteLineChart(
            writer,
            "Grab And Sound",
            "%",
            snapshots,
            new ChartSeries("Can grab", "#f5c26b", snapshots.Select(snapshot => Share(snapshot.CanGrabCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Grab intent", "#ff8a30", snapshots.Select(snapshot => Share(snapshot.GrabIntentCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Grab+touch", "#ffcc66", snapshots.Select(snapshot => Share(snapshot.GrabIntentWhileCanGrabCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Off-touch grab", "#b96cff", snapshots.Select(snapshot => Share(snapshot.GrabIntentWithoutCanGrabCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Holding", "#d96b3b", snapshots.Select(snapshot => Share(snapshot.HoldingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Grabbed", "#9d3434", snapshots.Select(snapshot => Share(snapshot.GrabbedCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Sound emit", "#29b6f6", snapshots.Select(snapshot => Share(snapshot.SoundEmittingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Sound heard", "#8f4cb8", snapshots.Select(snapshot => Share(snapshot.SoundHeardCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
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
            "Plant adaptation traits",
            "",
            snapshots,
            new ChartSeries("Tender", "#8fd36b", snapshots.Select(snapshot => snapshot.AverageTenderPlantAdaptation).ToArray()),
            new ChartSeries("Rich", "#178a4a", snapshots.Select(snapshot => snapshot.AverageRichPlantAdaptation).ToArray()),
            new ChartSeries("Tough", "#7f8f3a", snapshots.Select(snapshot => snapshot.AverageToughPlantAdaptation).ToArray()));
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
        series = DownsampleChartSeries(series, ReportTimelineSampleLimit);

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

        writer.WriteLine($"<div class=\"chart-card\" role=\"button\" tabindex=\"0\" aria-label=\"Open larger {Html(title)} chart\">");
        writer.WriteLine($"<h3>{Html(title)}</h3>");
        writer.WriteLine($"<svg viewBox=\"0 0 {width:0} {height:0}\" role=\"img\" aria-label=\"{Html(title)} chart\">");
        writer.WriteLine($"<line class=\"chart-axis\" x1=\"{left:0}\" y1=\"{top:0}\" x2=\"{left:0}\" y2=\"{height - bottom:0}\" />");
        writer.WriteLine($"<line class=\"chart-axis\" x1=\"{left:0}\" y1=\"{height - bottom:0}\" x2=\"{width - right:0}\" y2=\"{height - bottom:0}\" />");
        writer.WriteLine($"<text class=\"chart-label\" x=\"4\" y=\"{top + 4:0}\">{Html(FormatChartValue(max, unit))}</text>");
        writer.WriteLine($"<text class=\"chart-label\" x=\"4\" y=\"{height - bottom:0}\">{Html(FormatChartValue(min, unit))}</text>");

        for (var seriesIndex = 0; seriesIndex < series.Length; seriesIndex++)
        {
            var chartSeries = series[seriesIndex];
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

            writer.WriteLine($"<polyline class=\"chart-series-line\" data-series-index=\"{seriesIndex}\" points=\"{Html(string.Join(' ', points))}\" fill=\"none\" stroke=\"{Html(chartSeries.Color)}\" stroke-width=\"2.4\" stroke-linejoin=\"round\" stroke-linecap=\"round\" />");
        }

        writer.WriteLine("</svg>");
        writer.WriteLine("<div class=\"chart-legend\">");
        for (var seriesIndex = 0; seriesIndex < series.Length; seriesIndex++)
        {
            var chartSeries = series[seriesIndex];
            var final = chartSeries.Values.Length > 0 ? chartSeries.Values[^1] : 0f;
            writer.WriteLine(
                $"<span class=\"chart-legend-item\" data-series-index=\"{seriesIndex}\" title=\"Highlight {Html(chartSeries.Label)}\"><span class=\"legend-swatch\" style=\"background:{Html(chartSeries.Color)}\"></span>{Html(chartSeries.Label)} {Html(FormatChartValue(final, unit))}</span>");
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
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Living</th><th>Share</th><th>Founders</th><th>Dominant Founder</th><th>Representative</th><th>Generation</th><th>Diet</th><th>Tactic</th><th>Region</th><th>Thermal Niche</th><th>Current Temp</th><th>Lifetime Temp</th><th>Mismatch</th><th>Living C/T/H</th><th>Genome Div</th><th>Brain Div</th><th>Plant Adapt</th><th>Plant Digest</th><th>Meat Digest</th><th>Attack</th></tr></thead>");
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
                $"<td>{Html(summary.ThermalNicheLabel)}</td>" +
                $"<td>{Html(summary.AverageCurrentTemperature.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.AverageOccupiedTemperature.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.AverageOccupiedThermalMismatch.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html($"{summary.ColdTemperatureLivingCreatures}/{summary.TemperateTemperatureLivingCreatures}/{summary.HotTemperatureLivingCreatures}")}</td>" +
                $"<td>{Html(summary.AverageGenomeDistance.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.AverageBrainDistance.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatPlantAdaptation(summary))}</td>" +
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

    private static void WriteRtNeatBrainGraphSection(StreamWriter writer, WorldState state)
    {
        var candidates = SelectRtNeatBrainGraphs(state, RtNeatGraphRenderLimit);
        if (candidates.Count == 0)
        {
            return;
        }

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>rtNEAT Brain Graphs</h2>");
        writer.WriteLine("<p class=\"biome-map-note\">Representative living graph brains. Connected inputs are shown on the left, hidden nodes in the middle, and physical action outputs on the right. Green links are positive weights, red links are negative weights, and gray links are disabled.</p>");
        foreach (var candidate in candidates)
        {
            writer.WriteLine("<div class=\"rtneat-panel\">");
            writer.WriteLine("<div class=\"rtneat-graph-frame\">");
            WriteRtNeatBrainGraphSvg(writer, candidate.Brain);
            writer.WriteLine("</div>");
            writer.WriteLine("<aside class=\"rtneat-detail\">");
            writer.WriteLine($"<h3>Brain #{Html(candidate.BrainId)}</h3>");
            writer.WriteLine("<dl>");
            WriteDefinition(writer, "Living", candidate.LivingCreatures.ToString(CultureInfo.InvariantCulture));
            WriteDefinition(writer, "Eggs", candidate.Eggs.ToString(CultureInfo.InvariantCulture));
            WriteDefinition(writer, "Nodes", candidate.Brain.Nodes.Length.ToString(CultureInfo.InvariantCulture));
            WriteDefinition(writer, "Hidden", candidate.Brain.HiddenNodeCount.ToString(CultureInfo.InvariantCulture));
            WriteDefinition(writer, "Connections", candidate.Brain.ConnectionCount.ToString(CultureInfo.InvariantCulture));
            WriteDefinition(writer, "Enabled", candidate.Brain.EnabledConnectionCount.ToString(CultureInfo.InvariantCulture));
            WriteDefinition(writer, "Weights", candidate.Brain.WeightCount.ToString(CultureInfo.InvariantCulture));
            WriteDefinition(writer, "Schema", $"input v{candidate.Brain.InputSchemaVersion}, output v{candidate.Brain.OutputSchemaVersion}");
            writer.WriteLine("</dl>");
            writer.WriteLine("</aside>");
            writer.WriteLine("</div>");
        }

        writer.WriteLine("</section>");
    }

    private static IReadOnlyList<RtNeatBrainGraphCandidate> SelectRtNeatBrainGraphs(WorldState state, int limit)
    {
        var livingByBrain = new Dictionary<int, int>();
        foreach (var creature in state.Creatures)
        {
            if (creature.BrainId >= 0)
            {
                livingByBrain[creature.BrainId] = livingByBrain.GetValueOrDefault(creature.BrainId) + 1;
            }
        }

        var eggsByBrain = new Dictionary<int, int>();
        foreach (var egg in state.Eggs)
        {
            if (egg.BrainId >= 0)
            {
                eggsByBrain[egg.BrainId] = eggsByBrain.GetValueOrDefault(egg.BrainId) + 1;
            }
        }

        return livingByBrain.Keys
            .Concat(eggsByBrain.Keys)
            .Distinct()
            .Select(brainId =>
            {
                if (!state.TryGetBrain(brainId, out var brain) || brain?.RtNeat is null)
                {
                    return default;
                }

                return new RtNeatBrainGraphCandidate(
                    brainId,
                    livingByBrain.GetValueOrDefault(brainId),
                    eggsByBrain.GetValueOrDefault(brainId),
                    brain.RtNeat);
            })
            .Where(candidate => candidate.Brain is not null)
            .OrderByDescending(candidate => candidate.LivingCreatures)
            .ThenByDescending(candidate => candidate.Eggs)
            .ThenByDescending(candidate => candidate.Brain.HiddenNodeCount)
            .ThenByDescending(candidate => candidate.Brain.ConnectionCount)
            .ThenByDescending(candidate => candidate.Brain.EnabledConnectionCount)
            .ThenBy(candidate => candidate.BrainId)
            .Take(limit)
            .ToArray();
    }

    private static void WriteRtNeatBrainGraphSvg(TextWriter writer, RtNeatBrainGenome brain)
    {
        const float width = 980f;
        const float leftX = 145f;
        const float outputX = 835f;
        const float topPadding = 54f;
        const float bottomPadding = 38f;

        var nodeById = brain.Nodes.ToDictionary(node => node.Id);
        var visibleNodeIds = new HashSet<int>();
        foreach (var connection in brain.Connections)
        {
            visibleNodeIds.Add(connection.SourceNodeId);
            visibleNodeIds.Add(connection.TargetNodeId);
        }

        foreach (var node in brain.Nodes)
        {
            if (node.Kind != RtNeatNodeKind.Input)
            {
                visibleNodeIds.Add(node.Id);
            }
        }

        var visibleNodes = brain.Nodes
            .Where(node => visibleNodeIds.Contains(node.Id))
            .ToArray();
        var inputs = visibleNodes
            .Where(node => node.Kind == RtNeatNodeKind.Input)
            .OrderBy(node => node.Key, StringComparer.Ordinal)
            .ToArray();
        var hidden = visibleNodes
            .Where(node => node.Kind == RtNeatNodeKind.Hidden)
            .OrderBy(node => node.Depth)
            .ThenBy(node => node.Id)
            .ToArray();
        var outputs = visibleNodes
            .Where(node => node.Kind == RtNeatNodeKind.Output)
            .OrderBy(node => node.Key, StringComparer.Ordinal)
            .ToArray();
        var rowCount = Math.Max(1, Math.Max(inputs.Length, Math.Max(hidden.Length, outputs.Length)));
        var height = MathF.Max(360f, topPadding + bottomPadding + rowCount * 44f);
        var positions = new Dictionary<int, (float X, float Y)>();

        for (var i = 0; i < inputs.Length; i++)
        {
            positions[inputs[i].Id] = (leftX, DistributeY(i, inputs.Length, height, topPadding, bottomPadding));
        }

        for (var i = 0; i < hidden.Length; i++)
        {
            var depth = Math.Clamp(hidden[i].Depth, 0.05f, 0.95f);
            positions[hidden[i].Id] = (250f + depth * 470f, DistributeY(i, hidden.Length, height, topPadding, bottomPadding));
        }

        for (var i = 0; i < outputs.Length; i++)
        {
            positions[outputs[i].Id] = (outputX, DistributeY(i, outputs.Length, height, topPadding, bottomPadding));
        }

        writer.WriteLine($"<svg class=\"rtneat-graph\" viewBox=\"0 0 {Svg(width)} {Svg(height)}\" role=\"img\" aria-label=\"rtNEAT graph brain\">");
        writer.WriteLine($"<rect x=\"0\" y=\"0\" width=\"{Svg(width)}\" height=\"{Svg(height)}\" fill=\"#fbfcf8\"/>");
        writer.WriteLine("<text x=\"50\" y=\"26\" class=\"rtneat-node-kind\">inputs</text>");
        writer.WriteLine("<text x=\"470\" y=\"26\" text-anchor=\"middle\" class=\"rtneat-node-kind\">hidden</text>");
        writer.WriteLine("<text x=\"760\" y=\"26\" class=\"rtneat-node-kind\">actions</text>");

        foreach (var connection in brain.Connections.OrderBy(connection => connection.Enabled ? 1 : 0).ThenBy(connection => Math.Abs(connection.Weight)))
        {
            if (!positions.TryGetValue(connection.SourceNodeId, out var source)
                || !positions.TryGetValue(connection.TargetNodeId, out var target)
                || !nodeById.TryGetValue(connection.SourceNodeId, out var sourceNode)
                || !nodeById.TryGetValue(connection.TargetNodeId, out var targetNode))
            {
                continue;
            }

            var weightMagnitude = Math.Abs(connection.Weight);
            var color = !connection.Enabled
                ? "#9ca3af"
                : connection.Weight >= 0f ? "#15803d" : "#dc2626";
            var opacity = !connection.Enabled
                ? 0.18f
                : Math.Clamp(0.25f + weightMagnitude / 5f, 0.25f, 0.82f);
            var strokeWidth = !connection.Enabled
                ? 1f
                : Math.Clamp(1f + weightMagnitude * 0.85f, 1f, 5f);
            var curveA = source.X + MathF.Max(70f, (target.X - source.X) * 0.42f);
            var curveB = target.X - MathF.Max(70f, (target.X - source.X) * 0.42f);

            writer.WriteLine(
                $"<path d=\"M {Svg(source.X)} {Svg(source.Y)} C {Svg(curveA)} {Svg(source.Y)}, {Svg(curveB)} {Svg(target.Y)}, {Svg(target.X)} {Svg(target.Y)}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"{Svg(strokeWidth)}\" opacity=\"{Svg(opacity)}\">" +
                $"<title>{Html(ShortRtNeatNodeLabel(sourceNode))} -> {Html(ShortRtNeatNodeLabel(targetNode))}: {Html(connection.Weight.ToString("0.###", CultureInfo.InvariantCulture))}{(connection.Enabled ? string.Empty : " disabled")}</title></path>");
        }

        foreach (var node in inputs)
        {
            WriteRtNeatRectNode(writer, node, positions[node.Id], 190f, "#e0f2fe", "#0369a1");
        }

        foreach (var node in outputs)
        {
            WriteRtNeatRectNode(writer, node, positions[node.Id], 190f, "#ecfdf5", "#15803d");
        }

        foreach (var node in hidden)
        {
            var position = positions[node.Id];
            writer.WriteLine($"<circle cx=\"{Svg(position.X)}\" cy=\"{Svg(position.Y)}\" r=\"18\" fill=\"#fef3c7\" stroke=\"#b45309\" stroke-width=\"2\"><title>{Html(ShortRtNeatNodeLabel(node))}, bias {Html(node.Bias.ToString("0.###", CultureInfo.InvariantCulture))}, {Html(node.Activation)}</title></circle>");
            writer.WriteLine($"<text x=\"{Svg(position.X)}\" y=\"{Svg(position.Y - 2f)}\" text-anchor=\"middle\" class=\"rtneat-node-label\">h{Html(node.Id)}</text>");
            writer.WriteLine($"<text x=\"{Svg(position.X)}\" y=\"{Svg(position.Y + 10f)}\" text-anchor=\"middle\" class=\"rtneat-node-kind\">{Html(ShortRtNeatActivationLabel(node.Activation))} b{Html(FormatRtNeatBias(node.Bias))}</text>");
        }

        writer.WriteLine("</svg>");
    }

    private static void WriteRtNeatRectNode(
        TextWriter writer,
        RtNeatNodeGene node,
        (float X, float Y) position,
        float width,
        string fill,
        string stroke)
    {
        var x = position.X - width / 2f;
        var y = position.Y - 17f;
        writer.WriteLine($"<rect x=\"{Svg(x)}\" y=\"{Svg(y)}\" width=\"{Svg(width)}\" height=\"34\" rx=\"6\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"1.5\"><title>{Html(node.Key)}, bias {Html(node.Bias.ToString("0.###", CultureInfo.InvariantCulture))}</title></rect>");
        writer.WriteLine($"<text x=\"{Svg(position.X)}\" y=\"{Svg(position.Y - 1f)}\" text-anchor=\"middle\" class=\"rtneat-node-label\">{Html(ShortRtNeatNodeLabel(node))}</text>");
        writer.WriteLine($"<text x=\"{Svg(position.X)}\" y=\"{Svg(position.Y + 11f)}\" text-anchor=\"middle\" class=\"rtneat-node-kind\">{Html(node.Kind)}</text>");
    }

    private static void WriteDefinition(TextWriter writer, string term, string value)
    {
        writer.WriteLine($"<dt>{Html(term)}</dt><dd>{Html(value)}</dd>");
    }

    private static float DistributeY(int index, int count, float height, float topPadding, float bottomPadding)
    {
        if (count <= 1)
        {
            return (topPadding + height - bottomPadding) * 0.5f;
        }

        return topPadding + index * ((height - topPadding - bottomPadding) / (count - 1));
    }

    private static string ShortRtNeatNodeLabel(RtNeatNodeGene node)
    {
        if (node.Kind == RtNeatNodeKind.Hidden)
        {
            return $"h{node.Id}";
        }

        var key = node.Key
            .Replace("vision.", "vis.", StringComparison.Ordinal)
            .Replace("internal.", "int.", StringComparison.Ordinal)
            .Replace("contact.", "touch.", StringComparison.Ordinal)
            .Replace("terrain.", "ter.", StringComparison.Ordinal)
            .Replace("habitat.", "hab.", StringComparison.Ordinal)
            .Replace("obstacle.", "obs.", StringComparison.Ordinal)
            .Replace("action.", string.Empty, StringComparison.Ordinal);
        return key.Length <= 24 ? key : $"{key[..21]}...";
    }

    private static string ShortRtNeatActivationLabel(RtNeatActivationKind activation)
    {
        return activation switch
        {
            RtNeatActivationKind.Tanh => "tanh",
            RtNeatActivationKind.Sigmoid => "sig",
            RtNeatActivationKind.Relu => "relu",
            RtNeatActivationKind.Linear => "lin",
            _ => activation.ToString()
        };
    }

    private static string FormatRtNeatBias(float bias)
    {
        return bias.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture);
    }

    private static string Svg(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static void WriteBrainInputDiagnosticsSection(StreamWriter writer, BrainInputDiagnosticSummary summary)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Sensory Brain Wiring</h2>");
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
        WriteMetric(writer, "Direct small-creature sectors", FormatBrainWeight(summary.DirectSmallerCreatureSectorWeightMagnitude));
        WriteMetric(writer, "Direct similar-creature sectors", FormatBrainWeight(summary.DirectSimilarCreatureSectorWeightMagnitude));
        WriteMetric(writer, "Direct large-creature sectors", FormatBrainWeight(summary.DirectLargerCreatureSectorWeightMagnitude));
        WriteMetric(writer, "Direct approach sectors", FormatBrainWeight(summary.DirectCreatureApproachSectorWeightMagnitude));
        WriteMetric(writer, "Direct facing sectors", FormatBrainWeight(summary.DirectCreatureFacingSectorWeightMagnitude));
        WriteMetric(writer, "Hidden freshness magnitude", FormatBrainWeight(summary.HiddenFreshnessWeightMagnitude));
        WriteMetric(writer, "Hidden rot-scent magnitude", FormatBrainWeight(summary.HiddenRotScentWeightMagnitude));
        WriteMetric(writer, "Hidden small-creature sectors", FormatBrainWeight(summary.HiddenSmallerCreatureSectorWeightMagnitude));
        WriteMetric(writer, "Hidden similar-creature sectors", FormatBrainWeight(summary.HiddenSimilarCreatureSectorWeightMagnitude));
        WriteMetric(writer, "Hidden large-creature sectors", FormatBrainWeight(summary.HiddenLargerCreatureSectorWeightMagnitude));
        WriteMetric(writer, "Hidden approach sectors", FormatBrainWeight(summary.HiddenCreatureApproachSectorWeightMagnitude));
        WriteMetric(writer, "Hidden facing sectors", FormatBrainWeight(summary.HiddenCreatureFacingSectorWeightMagnitude));
        WriteMetric(writer, "Move from freshness", FormatSignedBrainWeight(summary.MoveFreshnessWeight));
        WriteMetric(writer, "Eat from freshness", FormatSignedBrainWeight(summary.EatFreshnessWeight));
        WriteMetric(writer, "Move from rot ahead", FormatSignedBrainWeight(summary.MoveRotScentForwardWeight));
        WriteMetric(writer, "Turn from rot right", FormatSignedBrainWeight(summary.TurnRotScentRightWeight));
        WriteMetric(writer, "Attack small-creature sectors", FormatSignedBrainWeight(summary.AttackSmallerCreatureSectorWeight));
        WriteMetric(writer, "Attack large-creature sectors", FormatSignedBrainWeight(summary.AttackLargerCreatureSectorWeight));
        WriteMetric(writer, "Attack approach sectors", FormatSignedBrainWeight(summary.AttackCreatureApproachSectorWeight));
        WriteMetric(writer, "Attack facing sectors", FormatSignedBrainWeight(summary.AttackCreatureFacingSectorWeight));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");
    }

    private static void WriteLineageBrainInputDiagnosticsSection(
        StreamWriter writer,
        IReadOnlyList<LineageBrainInputDiagnosticSummary> summaries)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Lineage Sensory Wiring</h2>");
        if (summaries.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living founder lineages were available for brain-input diagnostics.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Founder</th><th>Living</th><th>Share</th><th>Fresh Direct</th><th>Rot Direct</th><th>Small Direct</th><th>Similar Direct</th><th>Large Direct</th><th>Approach Direct</th><th>Facing Direct</th><th>Fresh Hidden</th><th>Rot Hidden</th><th>Small Hidden</th><th>Similar Hidden</th><th>Large Hidden</th><th>Approach Hidden</th><th>Facing Hidden</th><th>Move Fresh</th><th>Eat Fresh</th><th>Move Rot Ahead</th><th>Turn Rot Right</th><th>Attack Small</th><th>Attack Large</th><th>Attack Approach</th><th>Attack Facing</th></tr></thead>");
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
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectSmallerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectSimilarCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectLargerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectCreatureApproachSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectCreatureFacingSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenFreshnessWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenRotScentWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenSmallerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenSimilarCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenLargerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenCreatureApproachSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenCreatureFacingSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveFreshnessWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.EatFreshnessWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveRotScentForwardWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.TurnRotScentRightWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackSmallerCreatureSectorWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackLargerCreatureSectorWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackCreatureApproachSectorWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackCreatureFacingSectorWeight))}</td>" +
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
        writer.WriteLine("<h2>Species Sensory Wiring</h2>");
        if (summaries.Count == 0)
        {
            writer.WriteLine("<p class=\"empty\">No living neural species clusters were available for brain-input diagnostics.</p>");
            writer.WriteLine("</section>");
            return;
        }

        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Rank</th><th>Name</th><th>Living</th><th>Evaluated</th><th>Fresh Direct</th><th>Rot Direct</th><th>Small Direct</th><th>Similar Direct</th><th>Large Direct</th><th>Approach Direct</th><th>Facing Direct</th><th>Fresh Hidden</th><th>Rot Hidden</th><th>Small Hidden</th><th>Similar Hidden</th><th>Large Hidden</th><th>Approach Hidden</th><th>Facing Hidden</th><th>Move Fresh</th><th>Eat Fresh</th><th>Move Rot Density</th><th>Turn Rot Density</th><th>Move Rot Ahead</th><th>Turn Rot Right</th><th>Attack Small</th><th>Attack Large</th><th>Attack Approach</th><th>Attack Facing</th></tr></thead>");
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
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectSmallerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectSimilarCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectLargerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectCreatureApproachSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.DirectCreatureFacingSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenFreshnessWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenRotScentWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenSmallerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenSimilarCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenLargerCreatureSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenCreatureApproachSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatBrainWeight(diagnostics.HiddenCreatureFacingSectorWeightMagnitude))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveFreshnessWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.EatFreshnessWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveRotScentDensityWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.TurnRotScentDensityWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.MoveRotScentForwardWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.TurnRotScentRightWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackSmallerCreatureSectorWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackLargerCreatureSectorWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackCreatureApproachSectorWeight))}</td>" +
                $"<td>{Html(FormatSignedBrainWeight(diagnostics.AttackCreatureFacingSectorWeight))}</td>" +
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
            InitialBrainKind.SparseGraphForager => "Sparse graph forager",
            InitialBrainKind.SparseGraphScavenger => "Sparse graph scavenger",
            InitialBrainKind.SparseGraphPredator => "Sparse graph predator",
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
            BrainArchitectureKind.RtNeatGraph => "rtNEAT graph",
            BrainArchitectureKind.HybridDeep8x8Neural => "Hybrid deep 8x8 neural",
            BrainArchitectureKind.HiddenDeep8x8Neural => "Hidden deep 8x8 neural",
            _ => kind.ToString()
        };
    }

    private static void WriteScenarioSpeciesRosterSection(StreamWriter writer, SimulationScenario scenario)
    {
        var seeds = scenario.EnabledSpeciesSeeds().ToArray();
        if (seeds.Length == 0)
        {
            return;
        }

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Starting Roster</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Profile</th><th>Brain</th><th>Count</th><th>Spawn region</th><th>Energy</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var seed in seeds)
        {
            writer.WriteLine("<tr>");
            writer.WriteLine($"<td>{Html(FormatScenarioSpeciesSeedName(seed))}<br><small>{Html(seed.ProfilePath)}</small></td>");
            writer.WriteLine($"<td>{Html(FormatScenarioSpeciesSeedBrain(seed))}</td>");
            writer.WriteLine($"<td>{Html(seed.Count.ToString(CultureInfo.InvariantCulture))}</td>");
            writer.WriteLine($"<td>{Html(seed.SpawnRegion.ToString())}</td>");
            writer.WriteLine($"<td>{Html(FormatScenarioSpeciesSeedEnergy(seed))}</td>");
            writer.WriteLine("</tr>");
        }

        writer.WriteLine("</tbody>");
        writer.WriteLine("</table></div>");
        writer.WriteLine("</section>");
    }

    private static string FormatScenarioSpeciesSeeds(SimulationScenario scenario)
    {
        var seeds = scenario.EnabledSpeciesSeeds().ToArray();
        if (seeds.Length == 0)
        {
            return "None";
        }

        if (seeds.Length == 1)
        {
            var seed = seeds[0];
            return $"{seed.Count} x {FormatScenarioSpeciesSeedName(seed)} using {FormatScenarioSpeciesSeedBrain(seed)}";
        }

        var total = seeds.Sum(seed => seed.Count);
        return $"{total} creatures across {seeds.Length} roster entries";
    }

    private static string FormatSpeciesProfileName(string profilePath)
    {
        try
        {
            var profile = SpeciesProfileJson.Load(SimulationScenarioSpeciesSeeder.ResolveProfilePath(profilePath));
            return string.IsNullOrWhiteSpace(profile.Name)
                ? Path.GetFileName(profilePath)
                : profile.Name;
        }
        catch
        {
            return Path.GetFileName(profilePath);
        }
    }

    private static string FormatScenarioSpeciesSeedName(SpeciesScenarioSeed seed)
    {
        return string.IsNullOrWhiteSpace(seed.Label)
            ? FormatSpeciesProfileName(seed.ProfilePath)
            : seed.Label;
    }

    private static string FormatScenarioSpeciesSeedBrain(SpeciesScenarioSeed seed)
    {
        if (!string.IsNullOrWhiteSpace(seed.BrainProfilePath))
        {
            return $"{FormatBrainProfileName(seed.BrainProfilePath, seed.ProfilePath)} brain profile";
        }

        if (seed.BrainOverrideKind is not null)
        {
            return $"{FormatInitialBrainKind(seed.BrainOverrideKind.Value)} generated brain";
        }

        try
        {
            var speciesPath = SimulationScenarioSpeciesSeeder.ResolveProfilePath(seed.ProfilePath);
            var profile = SpeciesProfileJson.Load(speciesPath);
            if (!string.IsNullOrWhiteSpace(profile.DefaultBrainPath))
            {
                return $"{FormatBrainProfileName(profile.DefaultBrainPath, seed.ProfilePath)} default brain profile";
            }
        }
        catch
        {
            // Fall through to the embedded profile brain label.
        }

        return "embedded profile brain";
    }

    private static string FormatBrainProfileName(string brainProfilePath, string speciesProfilePath)
    {
        try
        {
            var resolvedSpeciesPath = SimulationScenarioSpeciesSeeder.ResolveProfilePath(speciesProfilePath);
            var resolvedBrainPath = SimulationScenarioSpeciesSeeder.ResolveBrainProfilePath(
                brainProfilePath,
                resolvedSpeciesPath);
            var profile = BrainProfileJson.Load(resolvedBrainPath);
            return string.IsNullOrWhiteSpace(profile.Name)
                ? Path.GetFileName(brainProfilePath)
                : profile.Name;
        }
        catch
        {
            return Path.GetFileName(brainProfilePath);
        }
    }

    private static string FormatScenarioSpeciesSeedEnergy(SpeciesScenarioSeed seed)
    {
        return seed.EnergyOverride is null
            ? "profile default"
            : seed.EnergyOverride.Value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatBiomePressureProfile(BiomePressureProfile profile)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Desert {profile.Desert:0.###}x, Scrubland {profile.Scrubland:0.###}x, Grassland {profile.Grassland:0.###}x, Fertile {profile.Fertile:0.###}x, Forest {profile.Forest:0.###}x, Wetland {profile.Wetland:0.###}x, Tundra {profile.Tundra:0.###}x, Highland {profile.Highland:0.###}x");
    }

    private static string FormatBiomeKind(BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome).ToString();
    }

    private static string BiomeColor(BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => "#c7b56f",
            BiomeKind.Scrubland => "#8d8d49",
            BiomeKind.Grassland => "#58ad57",
            BiomeKind.Fertile => "#2f8f43",
            BiomeKind.Forest => "#123d22",
            BiomeKind.Wetland => "#2e8a8a",
            BiomeKind.Tundra => "#b4c2c5",
            BiomeKind.Highland => "#887b68",
            _ => "#58ad57"
        };
    }

    private static string TemperatureColor(float temperature)
    {
        var value = Math.Clamp(temperature, 0f, 1f);
        if (value < 0.30f)
        {
            return InterpolateHexColor(0x2e, 0x57, 0xd3, 0x1b, 0x91, 0xa8, value / 0.30f);
        }

        if (value < 0.55f)
        {
            return InterpolateHexColor(0x1b, 0x91, 0xa8, 0x4b, 0x9b, 0x44, (value - 0.30f) / 0.25f);
        }

        if (value < 0.75f)
        {
            return InterpolateHexColor(0x4b, 0x9b, 0x44, 0xd6, 0x9b, 0x2f, (value - 0.55f) / 0.20f);
        }

        return InterpolateHexColor(0xd6, 0x9b, 0x2f, 0xc9, 0x49, 0x2e, (value - 0.75f) / 0.25f);
    }

    private static string InterpolateHexColor(
        int fromR,
        int fromG,
        int fromB,
        int toR,
        int toG,
        int toB,
        float amount)
    {
        var t = Math.Clamp(amount, 0f, 1f);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"#{LerpByte(fromR, toR, t):x2}{LerpByte(fromG, toG, t):x2}{LerpByte(fromB, toB, t):x2}");
    }

    private static int LerpByte(int from, int to, float amount)
    {
        return Math.Clamp((int)MathF.Round(from + (to - from) * amount), 0, 255);
    }

    private static string SvgNumber(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
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
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => snapshot.BarrenCreatureCount,
            BiomeKind.Scrubland => snapshot.SparseCreatureCount,
            BiomeKind.Grassland => snapshot.GrasslandCreatureCount,
            BiomeKind.Fertile => snapshot.RichCreatureCount,
            BiomeKind.Forest => snapshot.ForestCreatureCount,
            BiomeKind.Wetland => snapshot.WetlandCreatureCount,
            BiomeKind.Tundra => snapshot.TundraCreatureCount,
            BiomeKind.Highland => snapshot.HighlandCreatureCount,
            _ => 0
        };
    }

    private static void WriteBiomePreferenceSection(
        TextWriter writer,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        IReadOnlyList<BiomeSummary> biomeSummaries,
        float worldArea)
    {
        const int TailSnapshotCount = 100;

        var tailCount = Math.Min(TailSnapshotCount, snapshots.Count);
        var tailSnapshots = tailCount > 0
            ? snapshots.Skip(snapshots.Count - tailCount).ToArray()
            : Array.Empty<SimulationStatsSnapshot>();

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Biome Preference</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Biome</th><th>Area Share</th><th>Living Share</th><th>Preference</th><th>Plant kcal Share</th><th>Eaten Share</th><th>Death Share</th><th>Late Deaths</th></tr></thead>");
        writer.WriteLine("<tbody>");

        if (tailSnapshots.Length == 0)
        {
            writer.WriteLine("<tr><td colspan=\"8\" class=\"empty\">No stat snapshots were recorded.</td></tr>");
            writer.WriteLine("</tbody></table></div>");
            writer.WriteLine("</section>");
            return;
        }

        var first = tailSnapshots[0];
        var last = tailSnapshots[^1];
        var averageCreatures = Average(tailSnapshots, snapshot => snapshot.CreatureCount);
        var averagePlantCalories = Average(tailSnapshots, snapshot => snapshot.TotalPlantCalories);
        var averageCaloriesEaten = Average(tailSnapshots, snapshot => snapshot.TotalCaloriesEatenPerSecond);
        var lateDeaths = Math.Max(0, last.CreatureDeathCount - first.CreatureDeathCount);
        var activeBiomeSummaries = ActiveBiomeSummaries(biomeSummaries);

        foreach (var summary in activeBiomeSummaries)
        {
            var areaShare = summary.Area / worldArea;
            var averageLiving = Average(tailSnapshots, snapshot => CreatureCountForBiome(snapshot, summary.Kind));
            var livingShare = averageCreatures > 0f ? averageLiving / averageCreatures : 0f;
            var preference = areaShare > 0f ? livingShare / areaShare : 0f;
            var plantCaloriesShare = averagePlantCalories > 0f
                ? Average(tailSnapshots, snapshot => PlantCaloriesForBiome(snapshot, summary.Kind)) / averagePlantCalories
                : 0f;
            var eatenShare = averageCaloriesEaten > 0f
                ? Average(tailSnapshots, snapshot => CaloriesEatenForBiome(snapshot, summary.Kind)) / averageCaloriesEaten
                : 0f;
            var biomeLateDeaths = Math.Max(
                0,
                DeathCountForBiome(last, summary.Kind) - DeathCountForBiome(first, summary.Kind));
            var deathShare = lateDeaths > 0 ? Share(biomeLateDeaths, lateDeaths) : 0f;

            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(FormatBiomeKind(summary.Kind))}</td>" +
                $"<td>{Html(FormatPercent(areaShare))}</td>" +
                $"<td>{Html(FormatPercent(livingShare))}</td>" +
                $"<td>{Html(preference.ToString("0.##", CultureInfo.InvariantCulture))}x</td>" +
                $"<td>{Html(FormatPercent(plantCaloriesShare))}</td>" +
                $"<td>{Html(FormatPercent(eatenShare))}</td>" +
                $"<td>{Html(FormatPercent(deathShare))}</td>" +
                $"<td>{Html(biomeLateDeaths)}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteBiomeRiskRewardSection(
        TextWriter writer,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        IReadOnlyList<BiomeSummary> biomeSummaries,
        float worldArea)
    {
        const int TailSnapshotCount = 100;

        var tailCount = Math.Min(TailSnapshotCount, snapshots.Count);
        var tailSnapshots = tailCount > 0
            ? snapshots.Skip(snapshots.Count - tailCount).ToArray()
            : Array.Empty<SimulationStatsSnapshot>();

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Biome Risk and Reward</h2>");
        writer.WriteLine("<p class=\"biome-map-note\">Uses the last up to 100 stat snapshots. Reward index compares food-eaten share to living share; risk index compares death share to living share. Values above 1x are overrepresented for the creatures using that biome.</p>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Biome</th><th>Area Share</th><th>Living Share</th><th>Preference</th><th>Plant Availability</th><th>Food / Creature / s</th><th>Reward Index</th><th>Late Deaths</th><th>Deaths / Creature Hr</th><th>Risk Index</th></tr></thead>");
        writer.WriteLine("<tbody>");

        if (tailSnapshots.Length == 0)
        {
            writer.WriteLine("<tr><td colspan=\"10\" class=\"empty\">No stat snapshots were recorded.</td></tr>");
            writer.WriteLine("</tbody></table></div>");
            writer.WriteLine("</section>");
            return;
        }

        var first = tailSnapshots[0];
        var last = tailSnapshots[^1];
        var tailHours = Math.Max(0d, last.ElapsedSeconds - first.ElapsedSeconds) / 3600d;
        var averageCreatures = Average(tailSnapshots, snapshot => snapshot.CreatureCount);
        var averagePlantCalories = Average(tailSnapshots, snapshot => snapshot.TotalPlantCalories);
        var averageCaloriesEaten = Average(tailSnapshots, snapshot => snapshot.TotalCaloriesEatenPerSecond);
        var lateDeaths = Math.Max(0, last.CreatureDeathCount - first.CreatureDeathCount);
        var activeBiomeSummaries = ActiveBiomeSummaries(biomeSummaries);

        foreach (var summary in activeBiomeSummaries)
        {
            var areaShare = summary.Area / worldArea;
            var averageLiving = Average(tailSnapshots, snapshot => CreatureCountForBiome(snapshot, summary.Kind));
            var livingShare = averageCreatures > 0f ? averageLiving / averageCreatures : 0f;
            var preference = areaShare > 0f ? livingShare / areaShare : 0f;
            var plantCalories = Average(tailSnapshots, snapshot => PlantCaloriesForBiome(snapshot, summary.Kind));
            var plantCaloriesShare = averagePlantCalories > 0f ? plantCalories / averagePlantCalories : 0f;
            var plantAvailability = areaShare > 0f ? plantCaloriesShare / areaShare : 0f;
            var eatenPerSecond = Average(tailSnapshots, snapshot => CaloriesEatenForBiome(snapshot, summary.Kind));
            var eatenShare = averageCaloriesEaten > 0f ? eatenPerSecond / averageCaloriesEaten : 0f;
            var foodPerCreaturePerSecond = averageLiving > 0f ? eatenPerSecond / averageLiving : 0f;
            var rewardIndex = livingShare > 0f ? eatenShare / livingShare : float.NaN;
            var biomeLateDeaths = Math.Max(
                0,
                DeathCountForBiome(last, summary.Kind) - DeathCountForBiome(first, summary.Kind));
            var deathShare = lateDeaths > 0 ? Share(biomeLateDeaths, lateDeaths) : 0f;
            var deathsPerCreatureHour = averageLiving > 0f && tailHours > 0d
                ? biomeLateDeaths / (averageLiving * (float)tailHours)
                : 0f;
            var riskIndex = livingShare > 0f ? deathShare / livingShare : float.NaN;

            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(FormatBiomeKind(summary.Kind))}</td>" +
                $"<td>{Html(FormatPercent(areaShare))}</td>" +
                $"<td>{Html(FormatPercent(livingShare))}</td>" +
                $"<td>{Html(FormatIndex(preference))}</td>" +
                $"<td>{Html(FormatIndex(plantAvailability))}</td>" +
                $"<td>{Html(foodPerCreaturePerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatIndex(rewardIndex))}</td>" +
                $"<td>{Html(biomeLateDeaths)}</td>" +
                $"<td>{Html(deathsPerCreatureHour.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(FormatIndex(riskIndex))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WriteBiomePreferenceByGenerationSection(
        TextWriter writer,
        IReadOnlyList<CreatureState> creatures,
        BiomeMap biomes,
        IReadOnlyList<BiomeSummary> biomeSummaries,
        float worldArea)
    {
        var bands = GenerationBiomePreferenceBand.Defaults;
        var activeBiomeSummaries = ActiveBiomeSummaries(biomeSummaries);

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Biome Preference by Generation</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Generation Band</th><th>Living</th><th>Biome</th><th>Band Share</th><th>Area Share</th><th>Preference</th></tr></thead>");
        writer.WriteLine("<tbody>");

        if (creatures.Count == 0)
        {
            writer.WriteLine("<tr><td colspan=\"6\" class=\"empty\">No living creatures remain.</td></tr>");
            writer.WriteLine("</tbody></table></div>");
            writer.WriteLine("</section>");
            return;
        }

        foreach (var band in bands)
        {
            var bandCreatures = creatures
                .Where(creature => band.Contains(creature.Generation))
                .ToArray();

            if (bandCreatures.Length == 0)
            {
                writer.WriteLine(
                    "<tr>" +
                    $"<td>{Html(band.Label)}</td>" +
                    "<td>0</td>" +
                    "<td colspan=\"4\" class=\"empty\">No living creatures in this generation band.</td>" +
                    "</tr>");
                continue;
            }

            foreach (var summary in activeBiomeSummaries)
            {
                var canonicalBiome = BiomeKinds.Canonicalize(summary.Kind);
                var livingInBiome = bandCreatures.Count(creature =>
                    BiomeKinds.Canonicalize(biomes.GetKindAt(creature.Position)) == canonicalBiome);
                var bandShare = Share(livingInBiome, bandCreatures.Length);
                var areaShare = summary.Area / worldArea;
                var preference = areaShare > 0f ? bandShare / areaShare : 0f;

                writer.WriteLine(
                    "<tr>" +
                    $"<td>{Html(band.Label)}</td>" +
                    $"<td>{Html(livingInBiome)} / {Html(bandCreatures.Length)}</td>" +
                    $"<td>{Html(FormatBiomeKind(summary.Kind))}</td>" +
                    $"<td>{Html(FormatPercent(bandShare))}</td>" +
                    $"<td>{Html(FormatPercent(areaShare))}</td>" +
                    $"<td>{Html(preference.ToString("0.##", CultureInfo.InvariantCulture))}x</td>" +
                    "</tr>");
            }
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static BiomeSummary[] ActiveBiomeSummaries(IReadOnlyList<BiomeSummary> biomeSummaries)
    {
        var active = biomeSummaries
            .Where(summary => summary.Area > 0f)
            .ToArray();

        return active.Length > 0
            ? active
            : biomeSummaries.ToArray();
    }

    private static float Average(IReadOnlyList<SimulationStatsSnapshot> snapshots, Func<SimulationStatsSnapshot, float> selector)
    {
        if (snapshots.Count == 0)
        {
            return 0f;
        }

        var total = 0f;
        foreach (var snapshot in snapshots)
        {
            total += selector(snapshot);
        }

        return total / snapshots.Count;
    }

    private readonly record struct GenerationBiomePreferenceBand(string Label, int MinGeneration, int? MaxGeneration)
    {
        public static readonly GenerationBiomePreferenceBand[] Defaults =
        [
            new("0-5", 0, 5),
            new("6-15", 6, 15),
            new("16+", 16, null)
        ];

        public bool Contains(int generation)
        {
            return generation >= MinGeneration
                && (!MaxGeneration.HasValue || generation <= MaxGeneration.Value);
        }
    }

    private static float PlantCaloriesForBiome(SimulationStatsSnapshot snapshot, BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => snapshot.BarrenPlantCalories,
            BiomeKind.Scrubland => snapshot.SparsePlantCalories,
            BiomeKind.Grassland => snapshot.GrasslandPlantCalories,
            BiomeKind.Fertile => snapshot.RichPlantCalories,
            BiomeKind.Forest => snapshot.ForestPlantCalories,
            BiomeKind.Wetland => snapshot.WetlandPlantCalories,
            BiomeKind.Tundra => snapshot.TundraPlantCalories,
            BiomeKind.Highland => snapshot.HighlandPlantCalories,
            _ => 0f
        };
    }

    private static float CaloriesEatenForBiome(SimulationStatsSnapshot snapshot, BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => snapshot.BarrenCaloriesEatenPerSecond,
            BiomeKind.Scrubland => snapshot.SparseCaloriesEatenPerSecond,
            BiomeKind.Grassland => snapshot.GrasslandCaloriesEatenPerSecond,
            BiomeKind.Fertile => snapshot.RichCaloriesEatenPerSecond,
            BiomeKind.Forest => snapshot.ForestCaloriesEatenPerSecond,
            BiomeKind.Wetland => snapshot.WetlandCaloriesEatenPerSecond,
            BiomeKind.Tundra => snapshot.TundraCaloriesEatenPerSecond,
            BiomeKind.Highland => snapshot.HighlandCaloriesEatenPerSecond,
            _ => 0f
        };
    }

    private static int DeathCountForBiome(SimulationStatsSnapshot snapshot, BiomeKind biome)
    {
        return BiomeKinds.Canonicalize(biome) switch
        {
            BiomeKind.Desert => snapshot.BarrenDeathCount,
            BiomeKind.Scrubland => snapshot.SparseDeathCount,
            BiomeKind.Grassland => snapshot.GrasslandDeathCount,
            BiomeKind.Fertile => snapshot.RichDeathCount,
            BiomeKind.Forest => snapshot.ForestDeathCount,
            BiomeKind.Wetland => snapshot.WetlandDeathCount,
            BiomeKind.Tundra => snapshot.TundraDeathCount,
            BiomeKind.Highland => snapshot.HighlandDeathCount,
            _ => 0
        };
    }

    private static void WriteDeathCausesByBiomeSection(TextWriter writer, BiomeDeathCauseCounts counts)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Deaths by Biome and Cause</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Biome</th><th>Total</th><th>Share</th><th>Starvation</th><th>Injury</th><th>Rotten Meat</th><th>Unknown</th></tr></thead>");
        writer.WriteLine("<tbody>");

        foreach (var biome in BiomeKinds.All)
        {
            var row = counts.For(biome);
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(BiomeKinds.Canonicalize(biome))}</td>" +
                $"<td>{Html(row.Total)}</td>" +
                $"<td>{Html(FormatPercent(Share(row.Total, counts.Total)))}</td>" +
                $"<td>{Html(row.Starvation)}</td>" +
                $"<td>{Html(row.Injury)}</td>" +
                $"<td>{Html(row.RottenMeat)}</td>" +
                $"<td>{Html(row.Unknown)}</td>" +
                "</tr>");
        }

        writer.WriteLine(
            "<tr>" +
            "<td><strong>Total</strong></td>" +
            $"<td>{Html(counts.Total)}</td>" +
            $"<td>{Html(FormatPercent(counts.Total > 0 ? 1f : 0f))}</td>" +
            $"<td>{Html(SumDeathCause(counts, CreatureDeathReason.Starvation))}</td>" +
            $"<td>{Html(SumDeathCause(counts, CreatureDeathReason.Injury))}</td>" +
            $"<td>{Html(SumDeathCause(counts, CreatureDeathReason.RottenMeat))}</td>" +
            $"<td>{Html(SumUnknownDeaths(counts))}</td>" +
            "</tr>");

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static int SumDeathCause(BiomeDeathCauseCounts counts, CreatureDeathReason reason)
    {
        return BiomeKinds.All.Sum(biome => counts.For(biome).For(reason));
    }

    private static int SumUnknownDeaths(BiomeDeathCauseCounts counts)
    {
        return BiomeKinds.All.Sum(biome => counts.For(biome).Unknown);
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

    private static string FormatTemperatureIndex(float temperature)
    {
        return $"{Math.Clamp(temperature, 0f, 1f) * 100f:0.#}";
    }

    private static string FormatIndex(float value)
    {
        return float.IsFinite(value)
            ? $"{value.ToString("0.##", CultureInfo.InvariantCulture)}x"
            : "n/a";
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

    private static IReadOnlyList<SimulationStatsSnapshot> SelectReportSnapshots(
        IReadOnlyList<SimulationStatsSnapshot> snapshots)
    {
        if (snapshots.Count <= ReportTimelineSampleLimit)
        {
            return snapshots;
        }

        var selected = new List<SimulationStatsSnapshot>(ReportTimelineSampleLimit);
        var lastIndex = -1;
        for (var i = 0; i < ReportTimelineSampleLimit; i++)
        {
            var index = (int)Math.Round(i * (snapshots.Count - 1) / (double)(ReportTimelineSampleLimit - 1));
            if (index == lastIndex)
            {
                continue;
            }

            selected.Add(snapshots[index]);
            lastIndex = index;
        }

        return selected;
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

    private static string FormatThermalShares(ThermalLineageNicheSummary summary)
    {
        return $"{FormatPercent(summary.ColdTemperatureShare)} / {FormatPercent(summary.TemperateTemperatureShare)} / {FormatPercent(summary.HotTemperatureShare)}";
    }

    private static string FormatStressShares(ThermalLineageNicheSummary summary)
    {
        return $"{FormatPercent(summary.ColdThermalStressShare)} / {FormatPercent(summary.HotThermalStressShare)}";
    }

    private static string FormatPlantAdaptation(SpeciesClusterSummary summary)
    {
        return $"T {summary.AverageTenderPlantAdaptation:0.##} / R {summary.AverageRichPlantAdaptation:0.##} / Tough {summary.AverageToughPlantAdaptation:0.##}";
    }

    private static string FormatPlantRelocations(SimulationStats stats)
    {
        return $"local {stats.PlantLocalDispersalCount}, cluster {stats.PlantClusterRelocationCount}, global {stats.PlantGlobalRelocationCount}";
    }

    private static void WritePlantTypeDiagnosticsSection(StreamWriter writer, SimulationStatsSnapshot snapshot)
    {
        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Plant Type Diagnostics</h2>");
        writer.WriteLine("<div class=\"table-wrap\"><table>");
        writer.WriteLine("<thead><tr><th>Type</th><th>Resources</th><th>Plant kcal</th><th>Raw eaten/s</th><th>Intake share</th><th>Raw/resource</th><th>Digested energy/s</th><th>Adaptation</th><th>Payoff trace</th></tr></thead>");
        writer.WriteLine("<tbody>");
        WritePlantTypeDiagnosticsRow(
            writer,
            "Generic",
            GenericPlantTypeResourceCount(snapshot),
            GenericPlantTypeCalories(snapshot),
            GenericPlantCaloriesEatenPerSecond(snapshot),
            snapshot.TotalPlantCaloriesEatenPerSecond,
            GenericPlantDigestedEnergyPerSecond(snapshot),
            null,
            null);
        WritePlantTypeDiagnosticsRow(
            writer,
            "Tender",
            snapshot.TenderPlantTypeResourceCount,
            snapshot.TenderPlantTypeCalories,
            snapshot.TenderPlantCaloriesEatenPerSecond,
            snapshot.TotalPlantCaloriesEatenPerSecond,
            snapshot.TenderPlantDigestedEnergyPerSecond,
            snapshot.AverageTenderPlantAdaptation,
            snapshot.AverageTenderPlantPayoffTrace);
        WritePlantTypeDiagnosticsRow(
            writer,
            "Rich",
            snapshot.RichPlantTypeResourceCount,
            snapshot.RichPlantTypeCalories,
            snapshot.RichPlantCaloriesEatenPerSecond,
            snapshot.TotalPlantCaloriesEatenPerSecond,
            snapshot.RichPlantDigestedEnergyPerSecond,
            snapshot.AverageRichPlantAdaptation,
            snapshot.AverageRichPlantPayoffTrace);
        WritePlantTypeDiagnosticsRow(
            writer,
            "Tough",
            snapshot.ToughPlantTypeResourceCount,
            snapshot.ToughPlantTypeCalories,
            snapshot.ToughPlantCaloriesEatenPerSecond,
            snapshot.TotalPlantCaloriesEatenPerSecond,
            snapshot.ToughPlantDigestedEnergyPerSecond,
            snapshot.AverageToughPlantAdaptation,
            snapshot.AverageToughPlantPayoffTrace);
        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");
    }

    private static void WritePlantTypeDiagnosticsRow(
        StreamWriter writer,
        string label,
        int resources,
        float plantCalories,
        float rawEatenPerSecond,
        float totalPlantEatenPerSecond,
        float digestedEnergyPerSecond,
        float? adaptation,
        float? payoffTrace)
    {
        writer.WriteLine(
            "<tr>" +
            $"<td>{Html(label)}</td>" +
            $"<td>{Html(resources)}</td>" +
            $"<td>{Html(plantCalories.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(rawEatenPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(FormatPercent(PlantTypeShare(rawEatenPerSecond, totalPlantEatenPerSecond)))}</td>" +
            $"<td>{Html(FormatPlantTypeIntakePerResource(rawEatenPerSecond, resources))}</td>" +
            $"<td>{Html(digestedEnergyPerSecond.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
            $"<td>{Html(adaptation.HasValue ? adaptation.Value.ToString("0.###", CultureInfo.InvariantCulture) : "n/a")}</td>" +
            $"<td>{Html(payoffTrace.HasValue ? payoffTrace.Value.ToString("0.###", CultureInfo.InvariantCulture) : "n/a")}</td>" +
            "</tr>");
    }

    private static string FormatPlantTypeMix(SimulationScenario scenario)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"generic {scenario.GenericPlantWeight:0.###}, tender {scenario.TenderPlantWeight:0.###}, rich {scenario.RichPlantWeight:0.###}, tough {scenario.ToughPlantWeight:0.###}");
    }

    private static string FormatPlantTypeCalories(SimulationStatsSnapshot snapshot)
    {
        return FormatPlantTypeValues(
            GenericPlantTypeCalories(snapshot),
            snapshot.TenderPlantTypeCalories,
            snapshot.RichPlantTypeCalories,
            snapshot.ToughPlantTypeCalories,
            "0");
    }

    private static string FormatPlantTypeIntake(SimulationStatsSnapshot snapshot)
    {
        return FormatPlantTypeValues(
            GenericPlantCaloriesEatenPerSecond(snapshot),
            snapshot.TenderPlantCaloriesEatenPerSecond,
            snapshot.RichPlantCaloriesEatenPerSecond,
            snapshot.ToughPlantCaloriesEatenPerSecond,
            "0.###");
    }

    private static string FormatPlantTypeDigestion(SimulationStatsSnapshot snapshot)
    {
        return FormatPlantTypeValues(
            GenericPlantDigestedEnergyPerSecond(snapshot),
            snapshot.TenderPlantDigestedEnergyPerSecond,
            snapshot.RichPlantDigestedEnergyPerSecond,
            snapshot.ToughPlantDigestedEnergyPerSecond,
            "0.###");
    }

    private static string FormatPlantPayoffTraces(SimulationStatsSnapshot snapshot)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"tender {snapshot.AverageTenderPlantPayoffTrace:0.###}, rich {snapshot.AverageRichPlantPayoffTrace:0.###}, tough {snapshot.AverageToughPlantPayoffTrace:0.###}");
    }

    private static string FormatPlantTypeValues(float generic, float tender, float rich, float tough, string format)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"generic {generic.ToString(format, CultureInfo.InvariantCulture)}, tender {tender.ToString(format, CultureInfo.InvariantCulture)}, rich {rich.ToString(format, CultureInfo.InvariantCulture)}, tough {tough.ToString(format, CultureInfo.InvariantCulture)}");
    }

    private static int GenericPlantTypeResourceCount(SimulationStatsSnapshot snapshot)
    {
        return Math.Max(
            0,
            snapshot.PlantResourceCount
            - snapshot.TenderPlantTypeResourceCount
            - snapshot.RichPlantTypeResourceCount
            - snapshot.ToughPlantTypeResourceCount);
    }

    private static float GenericPlantTypeCalories(SimulationStatsSnapshot snapshot)
    {
        return Math.Max(
            0f,
            snapshot.TotalPlantCalories
            - snapshot.TenderPlantTypeCalories
            - snapshot.RichPlantTypeCalories
            - snapshot.ToughPlantTypeCalories);
    }

    private static float GenericPlantCaloriesEatenPerSecond(SimulationStatsSnapshot snapshot)
    {
        return Math.Max(
            0f,
            snapshot.TotalPlantCaloriesEatenPerSecond
            - snapshot.TenderPlantCaloriesEatenPerSecond
            - snapshot.RichPlantCaloriesEatenPerSecond
            - snapshot.ToughPlantCaloriesEatenPerSecond);
    }

    private static float GenericPlantDigestedEnergyPerSecond(SimulationStatsSnapshot snapshot)
    {
        return Math.Max(
            0f,
            snapshot.TotalPlantDigestedEnergyPerSecond
            - snapshot.TenderPlantDigestedEnergyPerSecond
            - snapshot.RichPlantDigestedEnergyPerSecond
            - snapshot.ToughPlantDigestedEnergyPerSecond);
    }

    private static float PlantTypeShare(float plantTypeCaloriesPerSecond, float totalPlantCaloriesPerSecond)
    {
        return totalPlantCaloriesPerSecond > 0f
            ? Math.Clamp(plantTypeCaloriesPerSecond / totalPlantCaloriesPerSecond, 0f, 1f)
            : 0f;
    }

    private static float PlantTypeIntakePerResource(float plantTypeCaloriesPerSecond, int resourceCount)
    {
        return resourceCount > 0
            ? plantTypeCaloriesPerSecond / resourceCount
            : 0f;
    }

    private static string FormatPlantTypeIntakePerResource(float plantTypeCaloriesPerSecond, int resourceCount)
    {
        return resourceCount > 0
            ? PlantTypeIntakePerResource(plantTypeCaloriesPerSecond, resourceCount).ToString("0.###", CultureInfo.InvariantCulture)
            : "n/a";
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

    private static float AverageSnapshotValue(
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        Func<SimulationStatsSnapshot, float> selector)
    {
        if (snapshots.Count == 0)
        {
            return 0f;
        }

        var total = 0f;
        foreach (var snapshot in snapshots)
        {
            total += selector(snapshot);
        }

        return total / snapshots.Count;
    }

    private static float ToDegrees(float radians)
    {
        return radians * 180f / MathF.PI;
    }

    private static string Html(object? value)
    {
        return WebUtility.HtmlEncode(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
    }

    private readonly record struct FounderSummary(
        EntityId FounderId,
        int TotalCreatures,
        int LivingCreatures,
        int DeadCreatures,
        int MaxGeneration,
        ThermalLineageNicheSummary ThermalNiche);

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
        public FloatAccumulator TenderPlantAdaptation;
        public FloatAccumulator RichPlantAdaptation;
        public FloatAccumulator ToughPlantAdaptation;
        public FloatAccumulator PlantDigestion;
        public FloatAccumulator MeatDigestion;
        public FloatAccumulator FreshMeatDigestion;
        public FloatAccumulator StaleMeatDigestion;
        public FloatAccumulator GutCapacityCalories;
        public FloatAccumulator DigestionCaloriesPerSecond;
        public FloatAccumulator BiteStrength;
        public FloatAccumulator DamageResistance;
        public FloatAccumulator ThermalOptimum;
        public FloatAccumulator ThermalTolerance;
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

    private static ChartSeries[] DownsampleChartSeries(IReadOnlyList<ChartSeries> series, int maxPoints)
    {
        if (maxPoints < 2)
        {
            return series.ToArray();
        }

        var result = new ChartSeries[series.Count];
        for (var i = 0; i < series.Count; i++)
        {
            var chartSeries = series[i];
            result[i] = chartSeries.Values.Length <= maxPoints
                ? chartSeries
                : chartSeries with { Values = DownsampleValues(chartSeries.Values, maxPoints) };
        }

        return result;
    }

    private static float[] DownsampleValues(float[] values, int maxPoints)
    {
        if (values.Length <= maxPoints)
        {
            return values;
        }

        var selected = new float[maxPoints];
        var lastIndex = -1;
        var selectedCount = 0;
        for (var i = 0; i < maxPoints; i++)
        {
            var index = (int)Math.Round(i * (values.Length - 1) / (double)(maxPoints - 1));
            if (index == lastIndex)
            {
                continue;
            }

            selected[selectedCount++] = values[index];
            lastIndex = index;
        }

        if (selectedCount == selected.Length)
        {
            return selected;
        }

        Array.Resize(ref selected, selectedCount);
        return selected;
    }
}
