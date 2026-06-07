using System.Globalization;

namespace Lineage.Core;

public readonly record struct GenomeTraitAverages(
    float BodyRadius,
    float MaxSpeed,
    float MaxTurnRadiansPerSecond,
    float SenseRadius,
    float VisionAngleRadians,
    float MetabolicPace,
    float BasalEnergyPerSecond,
    float MovementEnergyPerSecond,
    float EatCaloriesPerSecond,
    float ReproductionEnergyThreshold,
    float OffspringEnergyInvestment,
    float EggProductionEnergyPerSecond,
    float EggIncubationSeconds,
    float MaturityAgeSeconds,
    float ReproductionCooldownSeconds,
    float MaxLifeExpectancySeconds,
    float DietaryAdaptation,
    float CarrionAdaptation,
    float TenderPlantAdaptation,
    float RichPlantAdaptation,
    float ToughPlantAdaptation,
    float GutCapacityCalories,
    float DigestionCaloriesPerSecond,
    float FatStorageCapacityCalories,
    float FatStorageEfficiency,
    float ThermalOptimum,
    float ThermalTolerance,
    float ScentSignatureA,
    float ScentSignatureB,
    float ScentSignatureC,
    float BiteStrength,
    float DamageResistance,
    float MutationStrength,
    float TraitMutationRate,
    float BrainMutationRate);

public static class GenomeTraitAveragesCsv
{
    public const string Header =
        "avg_genome_body_radius,avg_genome_max_speed,avg_genome_max_turn_radians_per_second,avg_genome_sense_radius,avg_genome_vision_angle_degrees,avg_genome_metabolic_pace,avg_genome_basal_energy_per_second,avg_genome_movement_energy_per_second,avg_genome_eat_calories_per_second,avg_genome_reproduction_energy_threshold,avg_genome_offspring_energy_investment,avg_genome_egg_production_energy_per_second,avg_genome_egg_incubation_seconds,avg_genome_maturity_age_seconds,avg_genome_reproduction_cooldown_seconds,avg_genome_max_life_expectancy_seconds,avg_genome_dietary_adaptation,avg_genome_carrion_adaptation,avg_genome_tender_plant_adaptation,avg_genome_rich_plant_adaptation,avg_genome_tough_plant_adaptation,avg_genome_gut_capacity,avg_genome_digestion_rate,avg_genome_fat_storage_capacity,avg_genome_fat_storage_efficiency,avg_genome_thermal_optimum,avg_genome_thermal_tolerance,avg_genome_scent_signature_a,avg_genome_scent_signature_b,avg_genome_scent_signature_c,avg_genome_bite_strength,avg_genome_damage_resistance,avg_genome_mutation_strength,avg_genome_trait_mutation_rate,avg_genome_brain_mutation_rate";

    public static string Values(GenomeTraitAverages traits)
    {
        return string.Join(
            ',',
            Format(traits.BodyRadius),
            Format(traits.MaxSpeed),
            Format(traits.MaxTurnRadiansPerSecond),
            Format(traits.SenseRadius),
            Format(ToDegrees(traits.VisionAngleRadians)),
            Format(traits.MetabolicPace),
            Format(traits.BasalEnergyPerSecond),
            Format(traits.MovementEnergyPerSecond),
            Format(traits.EatCaloriesPerSecond),
            Format(traits.ReproductionEnergyThreshold),
            Format(traits.OffspringEnergyInvestment),
            Format(traits.EggProductionEnergyPerSecond),
            Format(traits.EggIncubationSeconds),
            Format(traits.MaturityAgeSeconds),
            Format(traits.ReproductionCooldownSeconds),
            Format(traits.MaxLifeExpectancySeconds),
            Format(traits.DietaryAdaptation),
            Format(traits.CarrionAdaptation),
            Format(traits.TenderPlantAdaptation),
            Format(traits.RichPlantAdaptation),
            Format(traits.ToughPlantAdaptation),
            Format(traits.GutCapacityCalories),
            Format(traits.DigestionCaloriesPerSecond),
            Format(traits.FatStorageCapacityCalories),
            Format(traits.FatStorageEfficiency),
            Format(traits.ThermalOptimum),
            Format(traits.ThermalTolerance),
            Format(traits.ScentSignatureA),
            Format(traits.ScentSignatureB),
            Format(traits.ScentSignatureC),
            Format(traits.BiteStrength),
            Format(traits.DamageResistance),
            Format(traits.MutationStrength),
            Format(traits.TraitMutationRate),
            Format(traits.BrainMutationRate));
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

internal struct GenomeTraitAccumulator
{
    private double _bodyRadius;
    private double _maxSpeed;
    private double _maxTurnRadiansPerSecond;
    private double _senseRadius;
    private double _visionAngleRadians;
    private double _metabolicPace;
    private double _basalEnergyPerSecond;
    private double _movementEnergyPerSecond;
    private double _eatCaloriesPerSecond;
    private double _reproductionEnergyThreshold;
    private double _offspringEnergyInvestment;
    private double _eggProductionEnergyPerSecond;
    private double _eggIncubationSeconds;
    private double _maturityAgeSeconds;
    private double _reproductionCooldownSeconds;
    private double _maxLifeExpectancySeconds;
    private double _dietaryAdaptation;
    private double _carrionAdaptation;
    private double _tenderPlantAdaptation;
    private double _richPlantAdaptation;
    private double _toughPlantAdaptation;
    private double _gutCapacityCalories;
    private double _digestionCaloriesPerSecond;
    private double _fatStorageCapacityCalories;
    private double _fatStorageEfficiency;
    private double _thermalOptimum;
    private double _thermalTolerance;
    private double _scentSignatureA;
    private double _scentSignatureB;
    private double _scentSignatureC;
    private double _biteStrength;
    private double _damageResistance;
    private double _mutationStrength;
    private double _traitMutationRate;
    private double _brainMutationRate;

    public void Add(CreatureGenome genome)
    {
        _bodyRadius += genome.BodyRadius;
        _maxSpeed += genome.MaxSpeed;
        _maxTurnRadiansPerSecond += genome.MaxTurnRadiansPerSecond;
        _senseRadius += genome.SenseRadius;
        _visionAngleRadians += genome.VisionAngleRadians;
        _metabolicPace += CreatureMetabolism.NormalizePace(genome.MetabolicPace);
        _basalEnergyPerSecond += genome.BasalEnergyPerSecond;
        _movementEnergyPerSecond += genome.MovementEnergyPerSecond;
        _eatCaloriesPerSecond += genome.EatCaloriesPerSecond;
        _reproductionEnergyThreshold += genome.ReproductionEnergyThreshold;
        _offspringEnergyInvestment += genome.OffspringEnergyInvestment;
        _eggProductionEnergyPerSecond += genome.EggProductionEnergyPerSecond;
        _eggIncubationSeconds += genome.EggIncubationSeconds;
        _maturityAgeSeconds += genome.MaturityAgeSeconds;
        _reproductionCooldownSeconds += genome.ReproductionCooldownSeconds;
        _maxLifeExpectancySeconds += genome.MaxLifeExpectancySeconds;
        _dietaryAdaptation += genome.DietaryAdaptation;
        _carrionAdaptation += genome.CarrionAdaptation;
        _tenderPlantAdaptation += genome.TenderPlantAdaptation;
        _richPlantAdaptation += genome.RichPlantAdaptation;
        _toughPlantAdaptation += genome.ToughPlantAdaptation;
        _gutCapacityCalories += genome.GutCapacityCalories;
        _digestionCaloriesPerSecond += genome.DigestionCaloriesPerSecond;
        _fatStorageCapacityCalories += genome.FatStorageCapacityCalories;
        _fatStorageEfficiency += genome.FatStorageEfficiency;
        _thermalOptimum += CreatureThermal.NormalizeOptimum(genome.ThermalOptimum);
        _thermalTolerance += CreatureThermal.NormalizeTolerance(genome.ThermalTolerance);
        _scentSignatureA += genome.ScentSignatureA;
        _scentSignatureB += genome.ScentSignatureB;
        _scentSignatureC += genome.ScentSignatureC;
        _biteStrength += genome.BiteStrength;
        _damageResistance += genome.DamageResistance;
        _mutationStrength += genome.MutationStrength;
        _traitMutationRate += genome.TraitMutationRate;
        _brainMutationRate += genome.BrainMutationRate;
    }

    public GenomeTraitAverages Average(float divisor)
    {
        return new GenomeTraitAverages(
            Average(_bodyRadius, divisor),
            Average(_maxSpeed, divisor),
            Average(_maxTurnRadiansPerSecond, divisor),
            Average(_senseRadius, divisor),
            Average(_visionAngleRadians, divisor),
            Average(_metabolicPace, divisor),
            Average(_basalEnergyPerSecond, divisor),
            Average(_movementEnergyPerSecond, divisor),
            Average(_eatCaloriesPerSecond, divisor),
            Average(_reproductionEnergyThreshold, divisor),
            Average(_offspringEnergyInvestment, divisor),
            Average(_eggProductionEnergyPerSecond, divisor),
            Average(_eggIncubationSeconds, divisor),
            Average(_maturityAgeSeconds, divisor),
            Average(_reproductionCooldownSeconds, divisor),
            Average(_maxLifeExpectancySeconds, divisor),
            Average(_dietaryAdaptation, divisor),
            Average(_carrionAdaptation, divisor),
            Average(_tenderPlantAdaptation, divisor),
            Average(_richPlantAdaptation, divisor),
            Average(_toughPlantAdaptation, divisor),
            Average(_gutCapacityCalories, divisor),
            Average(_digestionCaloriesPerSecond, divisor),
            Average(_fatStorageCapacityCalories, divisor),
            Average(_fatStorageEfficiency, divisor),
            Average(_thermalOptimum, divisor),
            Average(_thermalTolerance, divisor),
            Average(_scentSignatureA, divisor),
            Average(_scentSignatureB, divisor),
            Average(_scentSignatureC, divisor),
            Average(_biteStrength, divisor),
            Average(_damageResistance, divisor),
            Average(_mutationStrength, divisor),
            Average(_traitMutationRate, divisor),
            Average(_brainMutationRate, divisor));
    }

    private static float Average(double total, float divisor)
    {
        return divisor > 0f ? (float)(total / divisor) : 0f;
    }
}
