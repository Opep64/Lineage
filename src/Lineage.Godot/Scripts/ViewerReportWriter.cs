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
    public static void Write(string path, SimulationScenario scenario, Simulation simulation)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(path);
        var state = simulation.State;
        var snapshot = state.Stats.Snapshots.Count > 0 ? state.Stats.Snapshots[^1] : default;
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
        var founderSummaries = SummarizeFounders(state.LineageRecords)
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenByDescending(summary => summary.TotalCreatures)
            .ThenBy(summary => summary.FounderId.Value)
            .Take(10)
            .ToArray();
        var behaviorSummary = BehaviorAssay.Analyze(state);
        var lineageBehaviorSummaries = BehaviorAssay.AnalyzeTopFounderLineages(state, 10);

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
        WriteMetric(writer, "Initial brain", FormatInitialBrainKind(scenario.InitialBrainKind));
        WriteMetric(writer, "Seed", scenario.Seed.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "World size", $"{state.Bounds.Width:0} x {state.Bounds.Height:0}");
        WriteMetric(writer, "Resources per 1M area", resourceDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Tick", state.Tick.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Simulated seconds", state.ElapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Snapshot interval", $"{scenario.StatsSnapshotIntervalTicks} ticks");
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
        WriteMetric(writer, "Deaths", state.Stats.CreatureDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Starvation deaths", state.Stats.StarvationDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average lifespan", $"{snapshot.AverageLifespanSeconds:0.###} seconds");
        WriteMetric(writer, "Median lifespan", $"{snapshot.MedianLifespanSeconds:0.###} seconds");
        WriteMetric(writer, "Max generation", snapshot.MaxGeneration.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Avg movement biome cost", $"{snapshot.AverageBiomeMovementCostMultiplier:0.###}x");
        WriteMetric(writer, "Avg basal biome cost", $"{snapshot.AverageBiomeBasalCostMultiplier:0.###}x");
        WriteMetric(writer, "Avg biome speed", $"{snapshot.AverageBiomeSpeedMultiplier:0.###}x");
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Foraging Diagnostics</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Seeing food", FormatPercent(Share(snapshot.FoodDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Seeing plants", FormatPercent(Share(snapshot.PlantDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Seeing meat", FormatPercent(Share(snapshot.MeatDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Smelling meat", FormatPercent(Share(snapshot.MeatScentDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Seeing creatures", FormatPercent(Share(snapshot.CreatureDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Touching food", FormatPercent(Share(snapshot.FoodContactCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Eating this tick", FormatPercent(Share(snapshot.EatingCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Visible food density", snapshot.AverageVisibleFoodDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible plant density", snapshot.AverageVisiblePlantDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible meat density", snapshot.AverageVisibleMeatDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Meat scent density", snapshot.AverageMeatScentDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible creature density", snapshot.AverageVisibleCreatureDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Calories eaten", $"{snapshot.TotalCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Plant eaten", $"{snapshot.TotalPlantCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Carcass eaten", $"{snapshot.TotalCarcassCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Fresh meat eaten", $"{snapshot.TotalFreshMeatCaloriesEatenPerSecond:0.###} raw kcal/s");
        WriteMetric(writer, "Stale meat eaten", $"{snapshot.TotalStaleMeatCaloriesEatenPerSecond:0.###} raw kcal/s");
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
        WriteMetric(writer, "Fresh kill share", FormatPercent(snapshot.FreshKillCaloriesEatenShare));
        WriteMetric(writer, "Meat raw share", FormatPercent(snapshot.MeatCaloriesEatenShare));
        WriteMetric(writer, "Fresh meat share", FormatPercent(snapshot.FreshMeatCaloriesEatenShare));
        WriteMetric(writer, "Stale meat share", FormatPercent(snapshot.StaleMeatCaloriesEatenShare));
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
        WriteMetric(writer, "Initial resource density", $"{scenario.InitialResourcesPerMillionArea:0.###} per 1M area");
        WriteMetric(writer, "Initial resource patches", scenario.CalculateInitialResourceCount().ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Biomes", scenario.EnableBiomes ? "Enabled" : "Disabled");
        WriteMetric(writer, "Biome cell size", scenario.BiomeCellSize.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Resource void border", $"{scenario.ResourceVoidBorderWidth:0.###} world units");
        WriteMetric(writer, "Resource calories", FormatRange(scenario.ResourceCaloriesMin, scenario.ResourceCaloriesMax));
        WriteMetric(writer, "Resource regrowth", $"{FormatRange(scenario.ResourceRegrowthMin, scenario.ResourceRegrowthMax)} kcal/s");
        WriteMetric(writer, "Depleted resources relocate", scenario.RelocateDepletedResources ? "Yes" : "No");
        WriteMetric(writer, "Plant respawn delay", $"{FormatRange(scenario.PlantRespawnDelaySecondsMin, scenario.PlantRespawnDelaySecondsMax)} seconds");
        WriteMetric(writer, "Resource clustering", FormatPercent(scenario.ResourceClusterStrength));
        WriteMetric(writer, "Resource cluster radius", $"{scenario.ResourceClusterRadius:0.###} world units");
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
        writer.WriteLine("<thead><tr><th>Biome</th><th>Area Share</th><th>Density Mult</th><th>Regrowth Mult</th><th>Move Cost</th><th>Basal Cost</th><th>Speed</th><th>Resources</th><th>Resources/M</th><th>Calories</th><th>Living</th><th>Living Share</th></tr></thead>");
        writer.WriteLine("<tbody>");
        var movementCostProfile = scenario.CreateBiomeMovementCostProfile();
        var basalCostProfile = scenario.CreateBiomeBasalCostProfile();
        var speedProfile = scenario.CreateBiomeSpeedProfile();
        foreach (var summary in biomeSummaries)
        {
            var resourcesPerMillion = summary.Area > 0f
                ? summary.ResourceCount / summary.Area * SimulationScenario.ResourceDensityAreaUnits
                : 0f;
            var livingCreatureCount = CreatureCountForBiome(snapshot, summary.Kind);
            var movementCost = movementCostProfile.For(summary.Kind).ToString("0.###", CultureInfo.InvariantCulture);
            var basalCost = basalCostProfile.For(summary.Kind).ToString("0.###", CultureInfo.InvariantCulture);
            var speed = speedProfile.For(summary.Kind).ToString("0.###", CultureInfo.InvariantCulture);
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.Kind)}</td>" +
                $"<td>{Html(FormatPercent(summary.Area / worldArea))}</td>" +
                $"<td>{Html(summary.ResourceDensityMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ResourceRegrowthMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
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
        WriteBehaviorAssaySection(writer, behaviorSummary);
        WriteLineageBehaviorAssaySection(writer, lineageBehaviorSummaries);

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
            new ChartSeries("Touching food", "#6a8fce", snapshots.Select(snapshot => Share(snapshot.FoodContactCreatureCount, snapshot.CreatureCount) * 100f).ToArray()),
            new ChartSeries("Eating", "#d69d2f", snapshots.Select(snapshot => Share(snapshot.EatingCreatureCount, snapshot.CreatureCount) * 100f).ToArray()));
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
            new ChartSeries("Gut fullness %", "#6a8fce", snapshots.Select(snapshot => snapshot.AverageGutFillRatio * 100f).ToArray()));
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
            new ChartSeries("Fresh eaten share", "#35a862", snapshots.Select(snapshot => snapshot.FreshMeatCaloriesEatenShare * 100f).ToArray()),
            new ChartSeries("Stale eaten share", "#b84a4a", snapshots.Select(snapshot => snapshot.StaleMeatCaloriesEatenShare * 100f).ToArray()));
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
        WriteMetric(writer, "Population ecotype", summary.Ecotype);
        WriteMetric(writer, "Food response", summary.ForagingBias);
        WriteMetric(writer, "Creature attack response", summary.PredatorTendency);
        WriteMetric(writer, "Risk response", summary.RiskResponse);
        WriteMetric(writer, "Terrain response", summary.TerrainResponse);
        WriteMetric(writer, "Egg laying", summary.ReproductionTendency);
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
        writer.WriteLine("<thead><tr><th>Founder</th><th>Living</th><th>Share</th><th>Ecotype</th><th>Food</th><th>Risk</th><th>Terrain</th><th>Attack</th><th>Movement</th><th>Egg Laying</th><th>Small Attack</th><th>Large Approach Attack</th></tr></thead>");
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
            InitialBrainKind.ForagerPredator => "Forager predator",
            InitialBrainKind.RandomPerFounder => "Per-founder random weights",
            _ => kind.ToString()
        };
    }

    private static string FormatBiomePressureProfile(BiomePressureProfile profile)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Barren {profile.Barren:0.###}x, Sparse {profile.Sparse:0.###}x, Grassland {profile.Grassland:0.###}x, Rich {profile.Rich:0.###}x");
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
