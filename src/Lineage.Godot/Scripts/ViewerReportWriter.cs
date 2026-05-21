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
        var totalResourceCalories = state.Resources.Sum(resource => resource.Calories);
        var resourceCapacity = state.Resources.Sum(resource => resource.MaxCalories);
        var worldArea = MathF.Max(1f, state.Bounds.Width * state.Bounds.Height);
        var resourceDensity = state.Resources.Count / worldArea * 1_000_000f;
        var traitSummary = SummarizeTraits(state);
        var biomeSummaries = state.Biomes.SummarizeResources(state.Resources);
        var founderSummaries = SummarizeFounders(state.LineageRecords)
            .OrderByDescending(summary => summary.LivingCreatures)
            .ThenByDescending(summary => summary.TotalCreatures)
            .ThenBy(summary => summary.FounderId.Value)
            .Take(10)
            .ToArray();

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
        WriteMetric(writer, "Initial brain", scenario.RandomizeInitialBrainWeights ? "Per-founder random weights" : "Seed forager");
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
        WriteMetric(writer, "Resource patches", state.Resources.Count.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plants", snapshot.PlantResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Meat", snapshot.MeatResourceCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Resource calories", totalResourceCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Plant calories", snapshot.TotalPlantCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Meat calories", snapshot.TotalMeatCalories.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Resource fullness", resourceCapacity > 0f ? $"{totalResourceCalories / resourceCapacity * 100f:0.0}%" : "n/a");
        WriteMetric(writer, "Births", state.Stats.CreatureBirthCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Eggs laid", state.Stats.EggLaidCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Eggs hatched", state.Stats.EggHatchedCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Egg deaths", state.Stats.EggDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Egg predation deaths", state.Stats.EggPredationDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Egg survival", FormatPercent(Share(state.Stats.EggHatchedCount, state.Stats.EggLaidCount)));
        WriteMetric(writer, "Offspring alive", FormatPercent(Share(state.Creatures.Count(creature => creature.Generation > 0), state.Stats.EggHatchedCount)));
        WriteMetric(writer, "Egg health", $"{snapshot.AverageEggHealthRatio * 100f:0.0}%");
        WriteMetric(writer, "Birth investment", $"{snapshot.AverageBirthInvestmentRatio:0.###}x");
        WriteMetric(writer, "Deaths", state.Stats.CreatureDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Starvation deaths", state.Stats.StarvationDeathCount.ToString(CultureInfo.InvariantCulture));
        WriteMetric(writer, "Max generation", snapshot.MaxGeneration.ToString(CultureInfo.InvariantCulture));
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

        writer.WriteLine("<section>");
        writer.WriteLine("<h2>Foraging Diagnostics</h2>");
        writer.WriteLine("<div class=\"metric-grid\">");
        WriteMetric(writer, "Seeing food", FormatPercent(Share(snapshot.FoodDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Seeing plants", FormatPercent(Share(snapshot.PlantDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Seeing meat", FormatPercent(Share(snapshot.MeatDetectedCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Touching food", FormatPercent(Share(snapshot.FoodContactCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Eating this tick", FormatPercent(Share(snapshot.EatingCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Attacking this tick", FormatPercent(Share(snapshot.AttackingCreatureCount, snapshot.CreatureCount)));
        WriteMetric(writer, "Visible food density", snapshot.AverageVisibleFoodDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible plant density", snapshot.AverageVisiblePlantDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible meat density", snapshot.AverageVisibleMeatDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Visible prey density", snapshot.AverageVisiblePreyDensity.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Calories eaten", $"{snapshot.TotalCaloriesEatenPerSecond:0.###} kcal/s");
        WriteMetric(writer, "Attack damage", $"{snapshot.TotalAttackDamagePerSecond:0.###} health/s");
        WriteMetric(writer, "Time since meal", $"{snapshot.AverageSecondsSinceLastMeal:0.###} s avg");
        WriteMetric(writer, "Average vision range", snapshot.AverageVisionRange.ToString("0.###", CultureInfo.InvariantCulture));
        WriteMetric(writer, "Average vision angle", $"{ToDegrees(snapshot.AverageVisionAngleRadians):0.###} degrees");
        writer.WriteLine("</div>");
        writer.WriteLine("</section>");

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
        WriteMetric(writer, "Basal upkeep", $"{scenario.BasalEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Body radius upkeep", $"{scenario.BodyRadiusEnergyCostPerSecond:0.###} energy/radius/s");
        WriteMetric(writer, "Max speed upkeep", $"{scenario.MaxSpeedEnergyCostPerSecond:0.######} energy/speed/s");
        WriteMetric(writer, "Turn rate upkeep", $"{scenario.TurnRateEnergyCostPerSecond:0.######} energy/rad/s/s");
        WriteMetric(writer, "Sense radius upkeep", $"{scenario.SenseRadiusEnergyCostPerSecond:0.######} energy/radius/s");
        WriteMetric(writer, "Vision angle", $"{ToDegrees(scenario.VisionAngleRadians):0.###} degrees");
        WriteMetric(writer, "Vision angle upkeep", $"{scenario.VisionAngleEnergyCostPerSecond:0.######} energy/radian/s");
        WriteMetric(writer, "Eat rate upkeep", $"{scenario.EatRateEnergyCostPerSecond:0.######} energy/rate/s");
        WriteMetric(writer, "Egg upkeep", $"{scenario.EggEnergyCostPerSecond:0.######} energy/egg/s");
        WriteMetric(writer, "Egg exposure damage", $"{scenario.EggEnvironmentalDamagePerSecond:0.######} health/s");
        WriteMetric(writer, "Movement upkeep", $"{scenario.MovementEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Eat rate", $"{scenario.EatCaloriesPerSecond:0.###} kcal/s");
        WriteMetric(writer, "Egg production", $"{scenario.EggProductionEnergyPerSecond:0.###} energy/s");
        WriteMetric(writer, "Egg incubation", $"{scenario.EggIncubationSeconds:0.###} seconds");
        WriteMetric(writer, "Maturity age", $"{scenario.MaturityAgeSeconds:0.###} seconds");
        WriteMetric(writer, "Starting diet", $"{scenario.DietaryAdaptation:0.###} meat bias");
        WriteMetric(writer, "Starting plant digestion", FormatPercent(CreatureDigestion.PlantEfficiency(CreatureGenome.Baseline with { DietaryAdaptation = scenario.DietaryAdaptation })));
        WriteMetric(writer, "Starting meat digestion", FormatPercent(CreatureDigestion.MeatEfficiency(CreatureGenome.Baseline with { DietaryAdaptation = scenario.DietaryAdaptation })));
        WriteMetric(writer, "Death meat body calories", $"{scenario.DeathMeatCaloriesPerBodyRadius:0.###} kcal/radius");
        WriteMetric(writer, "Death meat energy fraction", FormatPercent(scenario.DeathMeatEnergyFraction));
        WriteMetric(writer, "Meat decay", $"{scenario.MeatDecayCaloriesPerSecond:0.###} kcal/s");
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
        writer.WriteLine("<thead><tr><th>Biome</th><th>Area Share</th><th>Density Mult</th><th>Regrowth Mult</th><th>Resources</th><th>Resources/M</th><th>Calories</th></tr></thead>");
        writer.WriteLine("<tbody>");
        foreach (var summary in biomeSummaries)
        {
            var resourcesPerMillion = summary.Area > 0f
                ? summary.ResourceCount / summary.Area * SimulationScenario.ResourceDensityAreaUnits
                : 0f;
            writer.WriteLine(
                "<tr>" +
                $"<td>{Html(summary.Kind)}</td>" +
                $"<td>{Html(FormatPercent(summary.Area / worldArea))}</td>" +
                $"<td>{Html(summary.ResourceDensityMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ResourceRegrowthMultiplier.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ResourceCount)}</td>" +
                $"<td>{Html(resourcesPerMillion.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                $"<td>{Html(summary.ResourceCalories.ToString("0.###", CultureInfo.InvariantCulture))}</td>" +
                "</tr>");
        }

        writer.WriteLine("</tbody></table></div>");
        writer.WriteLine("</section>");

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
            WriteTraitRow(writer, "Plant digestion efficiency", traitSummary.PlantDigestion);
            WriteTraitRow(writer, "Meat digestion efficiency", traitSummary.MeatDigestion);
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
            summary.PlantDigestion.Add(CreatureDigestion.PlantEfficiency(genome));
            summary.MeatDigestion.Add(CreatureDigestion.MeatEfficiency(genome));
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
        public FloatAccumulator PlantDigestion;
        public FloatAccumulator MeatDigestion;
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
