using System.Globalization;
using Lineage.Core;

namespace Lineage.Viewer;

/// <summary>
/// Writes the analysis bundle for a live Godot simulation.
/// </summary>
///
/// <remarks>
/// The CLI already writes these files after a headless run. This writer gives the
/// viewer the same analysis surface for a run that was steered or inspected in Godot.
/// </remarks>
public static class GodotRunExportWriter
{
    private const int ExportSnapshotStatsSampleLimit = 4096;

    public static GodotRunExportResult Write(
        string statsPath,
        string reportPath,
        string snapshotPath,
        SimulationScenario scenario,
        Simulation simulation,
        IReadOnlyList<SpeciesInjectionResult> speciesInjections)
    {
        var paths = GodotRunExportPaths.From(statsPath, reportPath, snapshotPath);
        var state = simulation.State;

        GodotStatsCsvWriter.Write(paths.StatsPath, state.Stats.Snapshots);
        GodotLineageCsvWriter.Write(paths.LineagePath, state.LineageRecords);
        GodotTraitSummaryCsvWriter.Write(paths.TraitSummaryPath, state);
        GodotSpeciesClusterCsvWriter.Write(paths.SpeciesSummaryPath, state);
        GodotSpeciesClusterTrendCsvWriter.Write(paths.SpeciesTrendPath, state.Stats.Snapshots, state);
        GodotFounderSummaryCsvWriter.Write(paths.FounderSummaryPath, state.LineageRecords);
        GodotGenerationSummaryCsvWriter.Write(paths.GenerationSummaryPath, state.LineageRecords);
        GodotLineageTrendCsvWriter.Write(paths.LineageTrendPath, state.Stats.Snapshots, state.LineageRecords);
        GodotRosterLineageSummaryCsvWriter.Write(paths.RosterSummaryPath, state.LineageRecords, speciesInjections);
        SimulationScenarioJson.Save(paths.ScenarioPath, scenario);
        ViewerReportWriter.Write(paths.ReportPath, scenario, simulation, speciesInjections);
        SimulationSnapshotJson.Save(
            paths.SnapshotPath,
            SimulationSnapshot.Capture(scenario, simulation, maxStatsSnapshots: ExportSnapshotStatsSampleLimit));

        return new GodotRunExportResult(
            paths.StatsPath,
            paths.LineagePath,
            paths.TraitSummaryPath,
            paths.SpeciesSummaryPath,
            paths.SpeciesTrendPath,
            paths.FounderSummaryPath,
            paths.GenerationSummaryPath,
            paths.LineageTrendPath,
            paths.RosterSummaryPath,
            paths.ScenarioPath,
            paths.ReportPath,
            paths.SnapshotPath);
    }
}

public sealed record GodotRunExportResult(
    string StatsPath,
    string LineagePath,
    string TraitSummaryPath,
    string SpeciesSummaryPath,
    string SpeciesTrendPath,
    string FounderSummaryPath,
    string GenerationSummaryPath,
    string LineageTrendPath,
    string RosterSummaryPath,
    string ScenarioPath,
    string ReportPath,
    string SnapshotPath)
{
    public int FileCount => 12;
}

internal sealed record GodotRunExportPaths(
    string StatsPath,
    string LineagePath,
    string TraitSummaryPath,
    string SpeciesSummaryPath,
    string SpeciesTrendPath,
    string FounderSummaryPath,
    string GenerationSummaryPath,
    string LineageTrendPath,
    string RosterSummaryPath,
    string ScenarioPath,
    string ReportPath,
    string SnapshotPath)
{
    public static GodotRunExportPaths From(string statsPath, string reportPath, string snapshotPath)
    {
        return new GodotRunExportPaths(
            statsPath,
            AddSuffix(statsPath, "lineage"),
            AddSuffix(statsPath, "traits"),
            AddSuffix(statsPath, "species"),
            AddSuffix(statsPath, "species_trends"),
            AddSuffix(statsPath, "founders"),
            AddSuffix(statsPath, "generations"),
            AddSuffix(statsPath, "lineage_trends"),
            AddSuffix(statsPath, "roster"),
            Path.ChangeExtension(AddSuffix(statsPath, "scenario"), ".json"),
            reportPath,
            snapshotPath);
    }

    private static string AddSuffix(string path, string suffix)
    {
        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        var suffixed = $"{fileName}_{suffix}{extension}";
        return string.IsNullOrWhiteSpace(directory)
            ? suffixed
            : Path.Combine(directory, suffixed);
    }
}

internal static class GodotStatsCsvWriter
{
    public static void Write(string path, IReadOnlyList<SimulationStatsSnapshot> snapshots)
    {
        using var writer = CreateWriter(path);
        writer.WriteLine("tick,elapsed_seconds,season_phase,season_fertility_multiplier,creatures,eggs,resources,plant_resources,meat_resources,dormant_plant_resources,total_dormant_plant_seconds_remaining,avg_dormant_plant_seconds_remaining,plant_patch_occupied_cell_share,plant_patch_top_decile_calories_share,plant_patchiness,local_fertility_cells,avg_local_fertility_multiplier,min_local_fertility_multiplier,depleted_local_fertility_cell_share,genomes,brains,avg_brain_hidden_nodes,max_brain_hidden_nodes,avg_hidden_input_weight_magnitude,avg_hidden_output_weight_magnitude,active_hidden_output_share,max_generation,total_creature_energy,total_egg_energy,total_egg_health,total_resource_calories,total_plant_calories,tender_plant_type_resources,rich_plant_type_resources,tough_plant_type_resources,tender_plant_type_calories,rich_plant_type_calories,tough_plant_type_calories,total_meat_calories,barren_creatures,barren_creature_share,sparse_creatures,sparse_creature_share,grassland_creatures,grassland_creature_share,rich_creatures,rich_creature_share,avg_biome_movement_cost,avg_biome_basal_cost,avg_biome_speed,obstacle_blocked_creatures,obstacle_blocked_share,obstacle_sensed_creatures,obstacle_sensed_share,avg_forward_obstacle,avg_left_obstacle,avg_right_obstacle,food_detected_creatures,food_detected_share,plant_detected_creatures,plant_detected_share,meat_detected_creatures,meat_detected_share,meat_scent_detected_creatures,meat_scent_detected_share,creature_detected_creatures,creature_detected_share,food_contact_creatures,food_contact_share,eating_creatures,eating_share,attacking_creatures,attacking_share,avg_visible_food_density,avg_visible_plant_density,avg_visible_meat_density,fresh_meat_detected_creatures,fresh_meat_detected_share,stale_meat_detected_creatures,stale_meat_detected_share,stale_meat_avoided_creatures,stale_meat_avoided_share,avg_visible_meat_freshness,avg_meat_scent_density,rotten_meat_scent_detected_creatures,rotten_meat_scent_detected_share,avg_rotten_meat_scent_density,avg_visible_creature_density,total_calories_eaten_per_second,plant_calories_eaten_per_second,tender_plant_calories_eaten_per_second,rich_plant_calories_eaten_per_second,tough_plant_calories_eaten_per_second,carcass_calories_eaten_per_second,egg_calories_eaten_per_second,live_prey_calories_eaten_per_second,meat_calories_eaten_share,fresh_kill_calories_eaten_share,total_calories_digested_per_second,plant_digested_energy_per_second,tender_plant_digested_energy_per_second,rich_plant_digested_energy_per_second,tough_plant_digested_energy_per_second,meat_digested_energy_per_second,meat_digested_energy_share,avg_gut_fill_ratio,avg_gut_plant_share,avg_gut_meat_share,avg_dietary_adaptation,avg_carrion_adaptation,avg_bite_strength,avg_damage_resistance,attacker_avg_dietary_adaptation,attacker_avg_bite_strength,attacker_avg_damage_resistance,non_attacker_avg_dietary_adaptation,non_attacker_avg_bite_strength,non_attacker_avg_damage_resistance,total_attack_damage_per_second,avg_seconds_since_last_meal,total_distance_traveled_per_second,avg_distance_since_last_meal,calories_eaten_per_distance,calories_digested_per_distance,calories_eaten_per_food_vision_event,avg_birth_investment_ratio,avg_egg_health_ratio,avg_vision_range,avg_vision_angle_degrees,births,eggs_laid,reproduction_attempts,eggs_hatched,egg_deaths,egg_predation_deaths,deaths,starvation_deaths,injury_deaths,rotten_meat_deaths,plant_depletions,plant_local_dispersals,plant_cluster_relocations,plant_global_relocations,plant_dormancy_started,plant_dormancy_completed,avg_plant_dormancy_scheduled_seconds,avg_plant_dormancy_completed_seconds,avg_meat_freshness,fresh_meat_calories_eaten_per_second,stale_meat_calories_eaten_per_second,fresh_meat_calories_eaten_share,stale_meat_calories_eaten_share,rotten_meat_damage_per_second,rotten_meat_damaged_creatures,rotten_meat_damaged_share,avg_lifespan_seconds,median_lifespan_seconds,reproduction_ready_creatures,reproduction_ready_share,reproduction_intent_creatures,reproduction_intent_share,avg_egg_reserve_ratio,avg_energy_surplus_ratio,avg_recent_food_success,active_memory_creatures,active_memory_share,avg_memory_strength,memory_food_contact_share,non_memory_food_contact_share,memory_eating_share,non_memory_eating_share,memory_calories_eaten_per_distance,non_memory_calories_eaten_per_distance,memory_avg_seconds_since_last_meal,non_memory_avg_seconds_since_last_meal,memory_avg_distance_since_last_meal,non_memory_avg_distance_since_last_meal,memory_avg_recent_food_success,non_memory_avg_recent_food_success,memory_avg_generation,non_memory_avg_generation,memory_avg_max_x_progress_share,non_memory_avg_max_x_progress_share,memory_right_region_share,non_memory_right_region_share,left_region_creatures,left_region_creature_share,middle_region_creatures,middle_region_creature_share,right_region_creatures,right_region_creature_share,left_region_eggs,middle_region_eggs,right_region_eggs,left_region_plant_calories,middle_region_plant_calories,right_region_plant_calories,left_region_meat_calories,middle_region_meat_calories,right_region_meat_calories,left_region_avg_generation,middle_region_avg_generation,right_region_avg_generation,left_region_season_fertility,middle_region_season_fertility,right_region_season_fertility,creature_contact_creatures,creature_contact_share,attack_intent_creatures,attack_intent_share,attack_intent_touching_creatures,attack_intent_touching_share,attack_no_intent_contact_creatures,attack_no_intent_contact_share,raw_attack_positive_creatures,raw_attack_positive_share,raw_attack_near_gate_creatures,raw_attack_near_gate_share,raw_attack_near_gate_touching_creatures,raw_attack_near_gate_touching_share,avg_attack_output,avg_touching_attack_output");

        foreach (var snapshot in snapshots)
        {
            writer.WriteLine(string.Join(
                ',',
                snapshot.Tick.ToString(CultureInfo.InvariantCulture),
                snapshot.ElapsedSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.SeasonPhase.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.SeasonFertilityMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CreatureCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggCount.ToString(CultureInfo.InvariantCulture),
                snapshot.ResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.MeatResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.DormantPlantResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.TotalDormantPlantSecondsRemaining.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageDormantPlantSecondsRemaining.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.PlantPatchOccupiedCellShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.PlantPatchTopDecileCaloriesShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.PlantPatchiness.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.LocalFertilityCellCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageLocalFertilityMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MinimumLocalFertilityMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.DepletedLocalFertilityCellShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.GenomeCount.ToString(CultureInfo.InvariantCulture),
                snapshot.BrainCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageBrainHiddenNodeCount.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxBrainHiddenNodeCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AverageBrainHiddenInputWeightMagnitude.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageBrainHiddenOutputWeightMagnitude.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ActiveBrainHiddenOutputShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MaxGeneration.ToString(CultureInfo.InvariantCulture),
                snapshot.TotalCreatureEnergy.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalEggEnergy.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalEggHealth.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalResourceCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TenderPlantTypeResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.RichPlantTypeResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.ToughPlantTypeResourceCount.ToString(CultureInfo.InvariantCulture),
                snapshot.TenderPlantTypeCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RichPlantTypeCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ToughPlantTypeCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.BarrenCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.BarrenCreatureCount, snapshot.CreatureCount),
                snapshot.SparseCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.SparseCreatureCount, snapshot.CreatureCount),
                snapshot.GrasslandCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.GrasslandCreatureCount, snapshot.CreatureCount),
                snapshot.RichCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RichCreatureCount, snapshot.CreatureCount),
                snapshot.AverageBiomeMovementCostMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageBiomeBasalCostMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageBiomeSpeedMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ObstacleBlockedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.ObstacleBlockedCreatureCount, snapshot.CreatureCount),
                snapshot.ObstacleSensedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.ObstacleSensedCreatureCount, snapshot.CreatureCount),
                snapshot.AverageForwardObstacle.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageLeftObstacle.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageRightObstacle.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.FoodDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.FoodDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.PlantDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.PlantDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.MeatDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.MeatDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.MeatScentDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.MeatScentDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.CreatureDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.CreatureDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.FoodContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.FoodContactCreatureCount, snapshot.CreatureCount),
                snapshot.EatingCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.EatingCreatureCount, snapshot.CreatureCount),
                snapshot.AttackingCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.AttackingCreatureCount, snapshot.CreatureCount),
                snapshot.AverageVisibleFoodDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageVisiblePlantDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageVisibleMeatDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.FreshMeatDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.FreshMeatDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.StaleMeatDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.StaleMeatDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.StaleMeatAvoidedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.StaleMeatAvoidedCreatureCount, snapshot.CreatureCount),
                snapshot.AverageVisibleMeatFreshness.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageMeatScentDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RottenMeatScentDetectedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RottenMeatScentDetectedCreatureCount, snapshot.CreatureCount),
                snapshot.AverageRottenMeatScentDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageVisibleCreatureDensity.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalPlantCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TenderPlantCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RichPlantCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ToughPlantCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalCarcassCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalEggCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalLivePreyCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MeatCaloriesEatenShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.FreshKillCaloriesEatenShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalCaloriesDigestedPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalPlantDigestedEnergyPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TenderPlantDigestedEnergyPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RichPlantDigestedEnergyPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ToughPlantDigestedEnergyPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalMeatDigestedEnergyPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MeatDigestedEnergyShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageGutFillRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageGutPlantShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageGutMeatShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageDietaryAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageCarrionAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageBiteStrength.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageDamageResistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AttackerAverageDietaryAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AttackerAverageBiteStrength.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AttackerAverageDamageResistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonAttackerAverageDietaryAdaptation.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonAttackerAverageBiteStrength.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonAttackerAverageDamageResistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalAttackDamagePerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageSecondsSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalDistanceTraveledPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageDistanceSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CaloriesEatenPerDistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CaloriesDigestedPerDistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CaloriesEatenPerFoodVisionEvent.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageBirthInvestmentRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageEggHealthRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageVisionRange.ToString("0.######", CultureInfo.InvariantCulture),
                ToDegrees(snapshot.AverageVisionAngleRadians).ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CreatureBirthCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggLaidCount.ToString(CultureInfo.InvariantCulture),
                snapshot.ReproductionAttemptCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggHatchedCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.EggPredationDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.CreatureDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.StarvationDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.InjuryDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.RottenMeatDeathCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantDepletionCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantLocalDispersalCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantClusterRelocationCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantGlobalRelocationCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantDormancyStartedCount.ToString(CultureInfo.InvariantCulture),
                snapshot.PlantDormancyCompletedCount.ToString(CultureInfo.InvariantCulture),
                snapshot.AveragePlantDormancyScheduledSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AveragePlantDormancyCompletedSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageMeatFreshness.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalFreshMeatCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalStaleMeatCaloriesEatenPerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.FreshMeatCaloriesEatenShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.StaleMeatCaloriesEatenShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.TotalRottenMeatDamagePerSecond.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RottenMeatDamagedCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RottenMeatDamagedCreatureCount, snapshot.CreatureCount),
                snapshot.AverageLifespanSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MedianLifespanSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ReproductionReadyCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.ReproductionReadyCreatureCount, snapshot.CreatureCount),
                snapshot.ReproductionIntentCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.ReproductionIntentCreatureCount, snapshot.CreatureCount),
                snapshot.AverageEggReserveRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageEnergySurplusRatio.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageRecentFoodSuccess.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.ActiveMemoryCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.ActiveMemoryCreatureCount, snapshot.CreatureCount),
                snapshot.AverageMemoryStrength.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserFoodContactShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserFoodContactShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserEatingShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserEatingShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserCaloriesEatenPerDistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserCaloriesEatenPerDistance.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserAverageSecondsSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserAverageSecondsSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserAverageDistanceSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserAverageDistanceSinceLastMeal.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserAverageRecentFoodSuccess.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserAverageRecentFoodSuccess.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserAverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserAverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserAverageMaxXProgressShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserAverageMaxXProgressShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MemoryUserRightRegionShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.NonMemoryUserRightRegionShare.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.LeftRegionCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.LeftRegionCreatureCount, snapshot.CreatureCount),
                snapshot.MiddleRegionCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.MiddleRegionCreatureCount, snapshot.CreatureCount),
                snapshot.RightRegionCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RightRegionCreatureCount, snapshot.CreatureCount),
                snapshot.LeftRegionEggCount.ToString(CultureInfo.InvariantCulture),
                snapshot.MiddleRegionEggCount.ToString(CultureInfo.InvariantCulture),
                snapshot.RightRegionEggCount.ToString(CultureInfo.InvariantCulture),
                snapshot.LeftRegionPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MiddleRegionPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RightRegionPlantCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.LeftRegionMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MiddleRegionMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RightRegionMeatCalories.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.LeftRegionAverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MiddleRegionAverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RightRegionAverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.LeftRegionSeasonFertilityMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.MiddleRegionSeasonFertilityMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.RightRegionSeasonFertilityMultiplier.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.CreatureContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.CreatureContactCreatureCount, snapshot.CreatureCount),
                snapshot.AttackIntentCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.AttackIntentCreatureCount, snapshot.CreatureCount),
                snapshot.AttackIntentWhileTouchingCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.AttackIntentWhileTouchingCreatureCount, snapshot.CreatureCount),
                snapshot.AttackNoIntentContactCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.AttackNoIntentContactCreatureCount, snapshot.CreatureCount),
                snapshot.RawAttackPositiveCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RawAttackPositiveCreatureCount, snapshot.CreatureCount),
                snapshot.RawAttackNearGateCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RawAttackNearGateCreatureCount, snapshot.CreatureCount),
                snapshot.RawAttackNearGateWhileTouchingCreatureCount.ToString(CultureInfo.InvariantCulture),
                FormatShare(snapshot.RawAttackNearGateWhileTouchingCreatureCount, snapshot.CreatureCount),
                snapshot.AverageAttackOutput.ToString("0.######", CultureInfo.InvariantCulture),
                snapshot.AverageTouchingAttackOutput.ToString("0.######", CultureInfo.InvariantCulture)));
        }
    }

    internal static StreamWriter CreateWriter(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new StreamWriter(path);
    }

    private static string FormatShare(int count, int total)
    {
        return (total > 0 ? count / (float)total : 0f).ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static float ToDegrees(float radians)
    {
        return radians * 180f / MathF.PI;
    }
}

internal static class GodotLineageCsvWriter
{
    public static void Write(string path, IReadOnlyList<CreatureLineageRecord> records)
    {
        using var writer = GodotStatsCsvWriter.CreateWriter(path);
        writer.WriteLine("id,parent_id,birth_tick,birth_elapsed_seconds,generation,genome_id,brain_id,birth_energy,death_tick,death_elapsed_seconds,death_reason,is_founder,is_alive");

        foreach (var record in records)
        {
            writer.WriteLine(string.Join(
                ',',
                record.Id.Value.ToString(CultureInfo.InvariantCulture),
                record.ParentId.Value.ToString(CultureInfo.InvariantCulture),
                record.BirthTick.ToString(CultureInfo.InvariantCulture),
                record.BirthElapsedSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                record.Generation.ToString(CultureInfo.InvariantCulture),
                record.GenomeId.ToString(CultureInfo.InvariantCulture),
                record.BrainId.ToString(CultureInfo.InvariantCulture),
                record.BirthEnergy.ToString("0.######", CultureInfo.InvariantCulture),
                record.DeathTick?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                record.DeathElapsedSeconds?.ToString("0.######", CultureInfo.InvariantCulture) ?? string.Empty,
                record.DeathReason?.ToString() ?? string.Empty,
                record.IsFounder.ToString(CultureInfo.InvariantCulture),
                record.IsAlive.ToString(CultureInfo.InvariantCulture)));
        }
    }
}

internal static class GodotTraitSummaryCsvWriter
{
    public static void Write(string path, WorldState state)
    {
        using var writer = GodotStatsCsvWriter.CreateWriter(path);
        writer.WriteLine("scope,count,avg_body_radius,min_body_radius,max_body_radius,avg_max_speed,min_max_speed,max_max_speed,avg_vision_range,min_vision_range,max_vision_range,avg_vision_angle_degrees,min_vision_angle_degrees,max_vision_angle_degrees,avg_reproduction_threshold,min_reproduction_threshold,max_reproduction_threshold,avg_offspring_investment,min_offspring_investment,max_offspring_investment,avg_egg_production_per_second,min_egg_production_per_second,max_egg_production_per_second,avg_egg_incubation_seconds,min_egg_incubation_seconds,max_egg_incubation_seconds,avg_maturity_age_seconds,min_maturity_age_seconds,max_maturity_age_seconds,avg_dietary_adaptation,min_dietary_adaptation,max_dietary_adaptation,avg_carrion_adaptation,min_carrion_adaptation,max_carrion_adaptation,avg_plant_digestion,min_plant_digestion,max_plant_digestion,avg_meat_digestion,min_meat_digestion,max_meat_digestion,avg_fresh_meat_digestion,min_fresh_meat_digestion,max_fresh_meat_digestion,avg_stale_meat_digestion,min_stale_meat_digestion,max_stale_meat_digestion,avg_gut_capacity,min_gut_capacity,max_gut_capacity,avg_digestion_rate,min_digestion_rate,max_digestion_rate,avg_bite_strength,min_bite_strength,max_bite_strength,avg_damage_resistance,min_damage_resistance,max_damage_resistance,avg_mutation_strength,min_mutation_strength,max_mutation_strength,avg_trait_mutation_rate,min_trait_mutation_rate,max_trait_mutation_rate,avg_brain_mutation_rate,min_brain_mutation_rate,max_brain_mutation_rate");

        if (state.Creatures.Count == 0)
        {
            writer.WriteLine("living_creatures,0" + new string(',', 66));
            return;
        }

        var summary = ViewerTraitAccumulator.FromLivingCreatures(state);
        writer.WriteLine(string.Join(
            ',',
            "living_creatures",
            summary.Count.ToString(CultureInfo.InvariantCulture),
            Format(summary.BodyRadius.Average),
            Format(summary.BodyRadius.Min),
            Format(summary.BodyRadius.Max),
            Format(summary.MaxSpeed.Average),
            Format(summary.MaxSpeed.Min),
            Format(summary.MaxSpeed.Max),
            Format(summary.SenseRadius.Average),
            Format(summary.SenseRadius.Min),
            Format(summary.SenseRadius.Max),
            Format(ToDegrees(summary.VisionAngleRadians.Average)),
            Format(ToDegrees(summary.VisionAngleRadians.Min)),
            Format(ToDegrees(summary.VisionAngleRadians.Max)),
            Format(summary.ReproductionThreshold.Average),
            Format(summary.ReproductionThreshold.Min),
            Format(summary.ReproductionThreshold.Max),
            Format(summary.OffspringInvestment.Average),
            Format(summary.OffspringInvestment.Min),
            Format(summary.OffspringInvestment.Max),
            Format(summary.EggProductionEnergyPerSecond.Average),
            Format(summary.EggProductionEnergyPerSecond.Min),
            Format(summary.EggProductionEnergyPerSecond.Max),
            Format(summary.EggIncubationSeconds.Average),
            Format(summary.EggIncubationSeconds.Min),
            Format(summary.EggIncubationSeconds.Max),
            Format(summary.MaturityAgeSeconds.Average),
            Format(summary.MaturityAgeSeconds.Min),
            Format(summary.MaturityAgeSeconds.Max),
            Format(summary.DietaryAdaptation.Average),
            Format(summary.DietaryAdaptation.Min),
            Format(summary.DietaryAdaptation.Max),
            Format(summary.CarrionAdaptation.Average),
            Format(summary.CarrionAdaptation.Min),
            Format(summary.CarrionAdaptation.Max),
            Format(summary.PlantDigestion.Average),
            Format(summary.PlantDigestion.Min),
            Format(summary.PlantDigestion.Max),
            Format(summary.MeatDigestion.Average),
            Format(summary.MeatDigestion.Min),
            Format(summary.MeatDigestion.Max),
            Format(summary.FreshMeatDigestion.Average),
            Format(summary.FreshMeatDigestion.Min),
            Format(summary.FreshMeatDigestion.Max),
            Format(summary.StaleMeatDigestion.Average),
            Format(summary.StaleMeatDigestion.Min),
            Format(summary.StaleMeatDigestion.Max),
            Format(summary.GutCapacityCalories.Average),
            Format(summary.GutCapacityCalories.Min),
            Format(summary.GutCapacityCalories.Max),
            Format(summary.DigestionCaloriesPerSecond.Average),
            Format(summary.DigestionCaloriesPerSecond.Min),
            Format(summary.DigestionCaloriesPerSecond.Max),
            Format(summary.BiteStrength.Average),
            Format(summary.BiteStrength.Min),
            Format(summary.BiteStrength.Max),
            Format(summary.DamageResistance.Average),
            Format(summary.DamageResistance.Min),
            Format(summary.DamageResistance.Max),
            Format(summary.MutationStrength.Average),
            Format(summary.MutationStrength.Min),
            Format(summary.MutationStrength.Max),
            Format(summary.TraitMutationRate.Average),
            Format(summary.TraitMutationRate.Min),
            Format(summary.TraitMutationRate.Max),
            Format(summary.BrainMutationRate.Average),
            Format(summary.BrainMutationRate.Min),
            Format(summary.BrainMutationRate.Max)));
    }

    private static string Format(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static float ToDegrees(float radians)
    {
        return radians * 180f / MathF.PI;
    }
}

internal static class GodotSpeciesClusterCsvWriter
{
    public static void Write(string path, WorldState state)
    {
        using var writer = GodotStatsCsvWriter.CreateWriter(path);
        writer.WriteLine("rank,species_id,name,living_creatures,living_share,founder_count,dominant_founder_id,dominant_founder_living,min_generation,avg_generation,max_generation,avg_energy,avg_age_seconds,avg_genome_distance,avg_brain_distance,avg_body_radius,avg_max_speed,avg_vision_range,avg_dietary_adaptation,avg_carrion_adaptation,avg_plant_digestion,avg_meat_digestion,avg_fresh_meat_digestion,avg_stale_meat_digestion,avg_bite_strength,avg_damage_resistance,recent_plant_kcal,recent_meat_kcal,eating_share,attack_share,current_east_progress_share,right_region_share,diet_label,tactic_label,region_label");

        foreach (var summary in SpeciesClusterAnalyzer.Analyze(state))
        {
            writer.WriteLine(string.Join(
                ',',
                summary.Rank.ToString(CultureInfo.InvariantCulture),
                summary.SpeciesId.ToString(CultureInfo.InvariantCulture),
                Escape(summary.Name),
                summary.LivingCreatures.ToString(CultureInfo.InvariantCulture),
                Format(summary.LivingShare),
                summary.FounderCount.ToString(CultureInfo.InvariantCulture),
                summary.DominantFounderId.Value.ToString(CultureInfo.InvariantCulture),
                summary.DominantFounderLivingCreatures.ToString(CultureInfo.InvariantCulture),
                summary.MinGeneration.ToString(CultureInfo.InvariantCulture),
                Format(summary.AverageGeneration),
                summary.MaxGeneration.ToString(CultureInfo.InvariantCulture),
                Format(summary.AverageEnergy),
                Format(summary.AverageAgeSeconds),
                Format(summary.AverageGenomeDistance),
                Format(summary.AverageBrainDistance),
                Format(summary.AverageBodyRadius),
                Format(summary.AverageMaxSpeed),
                Format(summary.AverageSenseRadius),
                Format(summary.AverageDietaryAdaptation),
                Format(summary.AverageCarrionAdaptation),
                Format(summary.AveragePlantDigestion),
                Format(summary.AverageMeatDigestion),
                Format(summary.AverageFreshMeatDigestion),
                Format(summary.AverageStaleMeatDigestion),
                Format(summary.AverageBiteStrength),
                Format(summary.AverageDamageResistance),
                Format(summary.RecentPlantCaloriesEaten),
                Format(summary.RecentMeatCaloriesEaten),
                Format(summary.EatingShare),
                Format(summary.AttackShare),
                Format(summary.CurrentEastProgressShare),
                Format(summary.RightRegionShare),
                Escape(summary.DietLabel),
                Escape(summary.TacticLabel),
                Escape(summary.RegionLabel)));
        }
    }

    private static string Format(float value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}

internal static class GodotSpeciesClusterTrendCsvWriter
{
    public static void Write(
        string path,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        WorldState state)
    {
        using var writer = GodotStatsCsvWriter.CreateWriter(path);
        writer.WriteLine("tick,elapsed_seconds,rank,species_id,name,living_creatures,total_living,living_share,min_generation,avg_generation,max_generation");

        foreach (var row in SpeciesClusterAnalyzer.AnalyzeHistory(state, snapshots).Rows)
        {
            writer.WriteLine(string.Join(
                ',',
                row.Tick.ToString(CultureInfo.InvariantCulture),
                row.ElapsedSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                row.Rank.ToString(CultureInfo.InvariantCulture),
                row.SpeciesId.ToString(CultureInfo.InvariantCulture),
                Escape(row.Name),
                row.LivingCreatures.ToString(CultureInfo.InvariantCulture),
                row.TotalLiving.ToString(CultureInfo.InvariantCulture),
                row.LivingShare.ToString("0.######", CultureInfo.InvariantCulture),
                row.MinGeneration.ToString(CultureInfo.InvariantCulture),
                row.AverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                row.MaxGeneration.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static string Escape(string value)
    {
        return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}

internal static class GodotFounderSummaryCsvWriter
{
    public static void Write(string path, IReadOnlyList<CreatureLineageRecord> records)
    {
        using var writer = GodotStatsCsvWriter.CreateWriter(path);
        writer.WriteLine("founder_id,total_creatures,descendant_count,living_creatures,dead_creatures,max_generation");

        foreach (var summary in Summarize(records).OrderBy(summary => summary.FounderId.Value))
        {
            writer.WriteLine(string.Join(
                ',',
                summary.FounderId.Value.ToString(CultureInfo.InvariantCulture),
                summary.TotalCreatures.ToString(CultureInfo.InvariantCulture),
                summary.DescendantCount.ToString(CultureInfo.InvariantCulture),
                summary.LivingCreatures.ToString(CultureInfo.InvariantCulture),
                summary.DeadCreatures.ToString(CultureInfo.InvariantCulture),
                summary.MaxGeneration.ToString(CultureInfo.InvariantCulture)));
        }
    }

    public static IReadOnlyList<GodotFounderSummary> Summarize(IReadOnlyList<CreatureLineageRecord> records)
    {
        var byId = records.ToDictionary(record => record.Id);
        var summaries = new Dictionary<EntityId, FounderAccumulator>();

        foreach (var record in records)
        {
            var founderId = FindFounderId(record, byId);
            summaries.TryGetValue(founderId, out var summary);
            summary.TotalCreatures++;
            summary.LivingCreatures += record.IsAlive ? 1 : 0;
            summary.DeadCreatures += record.IsAlive ? 0 : 1;
            summary.MaxGeneration = Math.Max(summary.MaxGeneration, record.Generation);
            summaries[founderId] = summary;
        }

        return summaries
            .Select(pair => new GodotFounderSummary(
                pair.Key,
                pair.Value.TotalCreatures,
                Math.Max(0, pair.Value.TotalCreatures - 1),
                pair.Value.LivingCreatures,
                pair.Value.DeadCreatures,
                pair.Value.MaxGeneration))
            .ToArray();
    }

    public static EntityId FindFounderId(
        CreatureLineageRecord record,
        IReadOnlyDictionary<EntityId, CreatureLineageRecord> byId)
    {
        var current = record;
        while (!current.IsFounder && byId.TryGetValue(current.ParentId, out var parent))
        {
            current = parent;
        }

        return current.Id;
    }

    private struct FounderAccumulator
    {
        public int TotalCreatures;
        public int LivingCreatures;
        public int DeadCreatures;
        public int MaxGeneration;
    }
}

internal readonly record struct GodotFounderSummary(
    EntityId FounderId,
    int TotalCreatures,
    int DescendantCount,
    int LivingCreatures,
    int DeadCreatures,
    int MaxGeneration);

internal static class GodotRosterLineageSummaryCsvWriter
{
    public static void Write(
        string path,
        IReadOnlyList<CreatureLineageRecord> records,
        IReadOnlyList<SpeciesInjectionResult> injections)
    {
        using var writer = GodotStatsCsvWriter.CreateWriter(path);
        writer.WriteLine("profile_name,founder_count,total_creatures,descendant_count,living_creatures,dead_creatures,max_generation,starvation_deaths,injury_deaths,rotten_meat_deaths,unknown_deaths,genome_ids,brain_ids");

        foreach (var summary in RosterLineageAnalyzer.Analyze(records, injections))
        {
            writer.WriteLine(string.Join(
                ',',
                EscapeCsv(summary.ProfileName),
                summary.FounderCount.ToString(CultureInfo.InvariantCulture),
                summary.TotalCreatures.ToString(CultureInfo.InvariantCulture),
                summary.DescendantCount.ToString(CultureInfo.InvariantCulture),
                summary.LivingCreatures.ToString(CultureInfo.InvariantCulture),
                summary.DeadCreatures.ToString(CultureInfo.InvariantCulture),
                summary.MaxGeneration.ToString(CultureInfo.InvariantCulture),
                summary.StarvationDeaths.ToString(CultureInfo.InvariantCulture),
                summary.InjuryDeaths.ToString(CultureInfo.InvariantCulture),
                summary.RottenMeatDeaths.ToString(CultureInfo.InvariantCulture),
                summary.UnknownDeaths.ToString(CultureInfo.InvariantCulture),
                EscapeCsv(string.Join("|", summary.GenomeIds)),
                EscapeCsv(string.Join("|", summary.BrainIds))));
        }
    }

    private static string EscapeCsv(string value)
    {
        return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}

internal static class GodotGenerationSummaryCsvWriter
{
    public static void Write(string path, IReadOnlyList<CreatureLineageRecord> records)
    {
        using var writer = GodotStatsCsvWriter.CreateWriter(path);
        writer.WriteLine("generation,births,living,dead,starvation_deaths,injury_deaths,rotten_meat_deaths,survival_rate");

        foreach (var summary in Summarize(records))
        {
            writer.WriteLine(string.Join(
                ',',
                summary.Generation.ToString(CultureInfo.InvariantCulture),
                summary.Births.ToString(CultureInfo.InvariantCulture),
                summary.Living.ToString(CultureInfo.InvariantCulture),
                summary.Dead.ToString(CultureInfo.InvariantCulture),
                summary.StarvationDeaths.ToString(CultureInfo.InvariantCulture),
                summary.InjuryDeaths.ToString(CultureInfo.InvariantCulture),
                summary.RottenMeatDeaths.ToString(CultureInfo.InvariantCulture),
                summary.SurvivalRate.ToString("0.######", CultureInfo.InvariantCulture)));
        }
    }

    public static IReadOnlyList<GodotGenerationSummary> Summarize(IReadOnlyList<CreatureLineageRecord> records)
    {
        return records
            .GroupBy(record => record.Generation)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var births = group.Count();
                var living = group.Count(record => record.IsAlive);
                var starvationDeaths = group.Count(record => record.DeathReason == CreatureDeathReason.Starvation);
                var injuryDeaths = group.Count(record => record.DeathReason == CreatureDeathReason.Injury);
                var rottenMeatDeaths = group.Count(record => record.DeathReason == CreatureDeathReason.RottenMeat);

                return new GodotGenerationSummary(
                    group.Key,
                    births,
                    living,
                    births - living,
                    starvationDeaths,
                    injuryDeaths,
                    rottenMeatDeaths);
            })
            .ToArray();
    }
}

internal readonly record struct GodotGenerationSummary(
    int Generation,
    int Births,
    int Living,
    int Dead,
    int StarvationDeaths,
    int InjuryDeaths,
    int RottenMeatDeaths)
{
    public float SurvivalRate => Births > 0 ? Living / (float)Births : 0f;
}

internal static class GodotLineageTrendCsvWriter
{
    private const int DefaultMaxRowsPerSnapshot = 10;

    public static void Write(
        string path,
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        IReadOnlyList<CreatureLineageRecord> records)
    {
        using var writer = GodotStatsCsvWriter.CreateWriter(path);
        writer.WriteLine("tick,elapsed_seconds,rank,founder_id,living_creatures,total_living,living_share,founder_min_generation,founder_avg_generation,founder_max_generation,overall_min_generation,overall_avg_generation,overall_max_generation");

        foreach (var row in Summarize(snapshots, records))
        {
            writer.WriteLine(string.Join(
                ',',
                row.Tick.ToString(CultureInfo.InvariantCulture),
                row.ElapsedSeconds.ToString("0.######", CultureInfo.InvariantCulture),
                row.Rank.ToString(CultureInfo.InvariantCulture),
                row.FounderId.Value.ToString(CultureInfo.InvariantCulture),
                row.LivingCreatures.ToString(CultureInfo.InvariantCulture),
                row.TotalLiving.ToString(CultureInfo.InvariantCulture),
                row.LivingShare.ToString("0.######", CultureInfo.InvariantCulture),
                row.FounderMinGeneration.ToString(CultureInfo.InvariantCulture),
                row.FounderAverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                row.FounderMaxGeneration.ToString(CultureInfo.InvariantCulture),
                row.OverallMinGeneration.ToString(CultureInfo.InvariantCulture),
                row.OverallAverageGeneration.ToString("0.######", CultureInfo.InvariantCulture),
                row.OverallMaxGeneration.ToString(CultureInfo.InvariantCulture)));
        }
    }

    public static IReadOnlyList<GodotLineageTrendRow> Summarize(
        IReadOnlyList<SimulationStatsSnapshot> snapshots,
        IReadOnlyList<CreatureLineageRecord> records,
        int maxRowsPerSnapshot = DefaultMaxRowsPerSnapshot)
    {
        if (snapshots.Count == 0 || records.Count == 0 || maxRowsPerSnapshot <= 0)
        {
            return Array.Empty<GodotLineageTrendRow>();
        }

        var byId = records.ToDictionary(record => record.Id);
        var founderByCreature = records.ToDictionary(
            record => record.Id,
            record => GodotFounderSummaryCsvWriter.FindFounderId(record, byId));
        var births = records
            .OrderBy(record => record.BirthTick)
            .ThenBy(record => record.Id.Value)
            .ToArray();
        var deaths = records
            .Where(record => record.DeathTick is not null)
            .OrderBy(record => record.DeathTick!.Value)
            .ThenBy(record => record.Id.Value)
            .ToArray();
        var orderedSnapshots = snapshots
            .OrderBy(snapshot => snapshot.Tick)
            .ToArray();

        var rows = new List<GodotLineageTrendRow>();
        var activeFounders = new Dictionary<EntityId, GenerationAccumulator>();
        var overallGenerations = new GenerationAccumulator();
        var birthIndex = 0;
        var deathIndex = 0;

        foreach (var snapshot in orderedSnapshots)
        {
            while (birthIndex < births.Length && births[birthIndex].BirthTick <= snapshot.Tick)
            {
                AddActiveCreature(activeFounders, overallGenerations, births[birthIndex], founderByCreature);
                birthIndex++;
            }

            while (deathIndex < deaths.Length && deaths[deathIndex].DeathTick!.Value <= snapshot.Tick)
            {
                RemoveActiveCreature(activeFounders, overallGenerations, deaths[deathIndex], founderByCreature);
                deathIndex++;
            }

            if (overallGenerations.Count == 0)
            {
                continue;
            }

            var rank = 1;
            foreach (var pair in activeFounders
                .Where(pair => pair.Value.Count > 0)
                .OrderByDescending(pair => pair.Value.Count)
                .ThenBy(pair => pair.Key.Value)
                .Take(maxRowsPerSnapshot))
            {
                rows.Add(new GodotLineageTrendRow(
                    snapshot.Tick,
                    snapshot.ElapsedSeconds,
                    rank,
                    pair.Key,
                    pair.Value.Count,
                    overallGenerations.Count,
                    pair.Value.Count / (float)overallGenerations.Count,
                    pair.Value.MinGeneration,
                    pair.Value.AverageGeneration,
                    pair.Value.MaxGeneration,
                    overallGenerations.MinGeneration,
                    overallGenerations.AverageGeneration,
                    overallGenerations.MaxGeneration));
                rank++;
            }
        }

        return rows;
    }

    private static void AddActiveCreature(
        IDictionary<EntityId, GenerationAccumulator> activeFounders,
        GenerationAccumulator overallGenerations,
        CreatureLineageRecord record,
        IReadOnlyDictionary<EntityId, EntityId> founderByCreature)
    {
        var founderId = founderByCreature[record.Id];
        if (!activeFounders.TryGetValue(founderId, out var accumulator))
        {
            accumulator = new GenerationAccumulator();
            activeFounders.Add(founderId, accumulator);
        }

        accumulator.Add(record.Generation);
        overallGenerations.Add(record.Generation);
    }

    private static void RemoveActiveCreature(
        IReadOnlyDictionary<EntityId, GenerationAccumulator> activeFounders,
        GenerationAccumulator overallGenerations,
        CreatureLineageRecord record,
        IReadOnlyDictionary<EntityId, EntityId> founderByCreature)
    {
        var founderId = founderByCreature[record.Id];
        if (activeFounders.TryGetValue(founderId, out var accumulator))
        {
            accumulator.Remove(record.Generation);
        }

        overallGenerations.Remove(record.Generation);
    }

    private sealed class GenerationAccumulator
    {
        private readonly SortedDictionary<int, int> _generationCounts = new();
        private long _generationSum;

        public int Count { get; private set; }

        public int MinGeneration => Count > 0 ? _generationCounts.First().Key : 0;

        public int MaxGeneration => Count > 0 ? _generationCounts.Last().Key : 0;

        public float AverageGeneration => Count > 0 ? _generationSum / (float)Count : 0f;

        public void Add(int generation)
        {
            _generationCounts.TryGetValue(generation, out var count);
            _generationCounts[generation] = count + 1;
            _generationSum += generation;
            Count++;
        }

        public void Remove(int generation)
        {
            if (!_generationCounts.TryGetValue(generation, out var count) || count == 0)
            {
                return;
            }

            if (count == 1)
            {
                _generationCounts.Remove(generation);
            }
            else
            {
                _generationCounts[generation] = count - 1;
            }

            _generationSum -= generation;
            Count--;
        }
    }
}

internal readonly record struct GodotLineageTrendRow(
    long Tick,
    double ElapsedSeconds,
    int Rank,
    EntityId FounderId,
    int LivingCreatures,
    int TotalLiving,
    float LivingShare,
    int FounderMinGeneration,
    float FounderAverageGeneration,
    int FounderMaxGeneration,
    int OverallMinGeneration,
    float OverallAverageGeneration,
    int OverallMaxGeneration);

internal readonly record struct ViewerTraitAccumulator(
    int Count,
    ViewerFloatSummary BodyRadius,
    ViewerFloatSummary MaxSpeed,
    ViewerFloatSummary SenseRadius,
    ViewerFloatSummary VisionAngleRadians,
    ViewerFloatSummary ReproductionThreshold,
    ViewerFloatSummary OffspringInvestment,
    ViewerFloatSummary EggProductionEnergyPerSecond,
    ViewerFloatSummary EggIncubationSeconds,
    ViewerFloatSummary MaturityAgeSeconds,
    ViewerFloatSummary DietaryAdaptation,
    ViewerFloatSummary CarrionAdaptation,
    ViewerFloatSummary PlantDigestion,
    ViewerFloatSummary MeatDigestion,
    ViewerFloatSummary FreshMeatDigestion,
    ViewerFloatSummary StaleMeatDigestion,
    ViewerFloatSummary GutCapacityCalories,
    ViewerFloatSummary DigestionCaloriesPerSecond,
    ViewerFloatSummary BiteStrength,
    ViewerFloatSummary DamageResistance,
    ViewerFloatSummary MutationStrength,
    ViewerFloatSummary TraitMutationRate,
    ViewerFloatSummary BrainMutationRate)
{
    public static ViewerTraitAccumulator FromLivingCreatures(WorldState state)
    {
        var bodyRadius = new ViewerFloatAccumulator();
        var maxSpeed = new ViewerFloatAccumulator();
        var senseRadius = new ViewerFloatAccumulator();
        var visionAngleRadians = new ViewerFloatAccumulator();
        var reproductionThreshold = new ViewerFloatAccumulator();
        var offspringInvestment = new ViewerFloatAccumulator();
        var eggProductionEnergyPerSecond = new ViewerFloatAccumulator();
        var eggIncubationSeconds = new ViewerFloatAccumulator();
        var maturityAgeSeconds = new ViewerFloatAccumulator();
        var dietaryAdaptation = new ViewerFloatAccumulator();
        var carrionAdaptation = new ViewerFloatAccumulator();
        var plantDigestion = new ViewerFloatAccumulator();
        var meatDigestion = new ViewerFloatAccumulator();
        var freshMeatDigestion = new ViewerFloatAccumulator();
        var staleMeatDigestion = new ViewerFloatAccumulator();
        var gutCapacityCalories = new ViewerFloatAccumulator();
        var digestionCaloriesPerSecond = new ViewerFloatAccumulator();
        var biteStrength = new ViewerFloatAccumulator();
        var damageResistance = new ViewerFloatAccumulator();
        var mutationStrength = new ViewerFloatAccumulator();
        var traitMutationRate = new ViewerFloatAccumulator();
        var brainMutationRate = new ViewerFloatAccumulator();

        foreach (var creature in state.Creatures)
        {
            var genome = state.GetGenome(creature.GenomeId);
            bodyRadius.Add(genome.BodyRadius);
            maxSpeed.Add(genome.MaxSpeed);
            senseRadius.Add(genome.SenseRadius);
            visionAngleRadians.Add(genome.VisionAngleRadians);
            reproductionThreshold.Add(genome.ReproductionEnergyThreshold);
            offspringInvestment.Add(genome.OffspringEnergyInvestment);
            eggProductionEnergyPerSecond.Add(genome.EggProductionEnergyPerSecond);
            eggIncubationSeconds.Add(genome.EggIncubationSeconds);
            maturityAgeSeconds.Add(genome.MaturityAgeSeconds);
            dietaryAdaptation.Add(genome.DietaryAdaptation);
            carrionAdaptation.Add(genome.CarrionAdaptation);
            plantDigestion.Add(CreatureDigestion.PlantEfficiency(genome));
            meatDigestion.Add(CreatureDigestion.MeatEfficiency(genome));
            freshMeatDigestion.Add(CreatureDigestion.FreshMeatEnergyEfficiency(genome));
            staleMeatDigestion.Add(CreatureDigestion.StaleMeatEnergyEfficiency(genome));
            gutCapacityCalories.Add(genome.GutCapacityCalories);
            digestionCaloriesPerSecond.Add(genome.DigestionCaloriesPerSecond);
            biteStrength.Add(genome.BiteStrength);
            damageResistance.Add(genome.DamageResistance);
            mutationStrength.Add(genome.MutationStrength);
            traitMutationRate.Add(genome.TraitMutationRate);
            brainMutationRate.Add(genome.BrainMutationRate);
        }

        return new ViewerTraitAccumulator(
            state.Creatures.Count,
            bodyRadius.ToSummary(),
            maxSpeed.ToSummary(),
            senseRadius.ToSummary(),
            visionAngleRadians.ToSummary(),
            reproductionThreshold.ToSummary(),
            offspringInvestment.ToSummary(),
            eggProductionEnergyPerSecond.ToSummary(),
            eggIncubationSeconds.ToSummary(),
            maturityAgeSeconds.ToSummary(),
            dietaryAdaptation.ToSummary(),
            carrionAdaptation.ToSummary(),
            plantDigestion.ToSummary(),
            meatDigestion.ToSummary(),
            freshMeatDigestion.ToSummary(),
            staleMeatDigestion.ToSummary(),
            gutCapacityCalories.ToSummary(),
            digestionCaloriesPerSecond.ToSummary(),
            biteStrength.ToSummary(),
            damageResistance.ToSummary(),
            mutationStrength.ToSummary(),
            traitMutationRate.ToSummary(),
            brainMutationRate.ToSummary());
    }
}

internal readonly record struct ViewerFloatSummary(float Average, float Min, float Max);

internal struct ViewerFloatAccumulator
{
    private float _sum;
    private float _min;
    private float _max;

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
    }

    public ViewerFloatSummary ToSummary()
    {
        return Count == 0
            ? new ViewerFloatSummary(0f, 0f, 0f)
            : new ViewerFloatSummary(_sum / Count, _min, _max);
    }
}
