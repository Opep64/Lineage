using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lineage.Core;

public sealed record SimulationScenarioFieldMetadata(
    string Name,
    string JsonName,
    string Label,
    string Group,
    string Type,
    IReadOnlyList<string> EnumValues,
    bool Advanced,
    double? Minimum,
    double? Maximum,
    double? Step,
    string? Units,
    string? Description);

public static class SimulationScenarioMetadata
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IReadOnlyList<SimulationScenarioFieldMetadata> Fields { get; } = BuildFields();

    public static SimulationScenarioFieldMetadata? FindByJsonName(string jsonName)
    {
        return Fields.FirstOrDefault(field =>
            string.Equals(field.JsonName, jsonName, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<SimulationScenarioFieldMetadata> BuildFields()
    {
        return typeof(SimulationScenario)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            .Where(property => property.GetMethod is not null)
            .Select(BuildField)
            .ToArray();
    }

    private static SimulationScenarioFieldMetadata BuildField(PropertyInfo property)
    {
        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        var type = ScenarioEditorType(propertyType);
        var enumValues = propertyType.IsEnum
            ? Enum.GetNames(propertyType).Select(JsonOptions.PropertyNamingPolicy!.ConvertName).ToArray()
            : Array.Empty<string>();
        var hints = ScenarioFieldHints(property.Name, propertyType, type);

        return new SimulationScenarioFieldMetadata(
            property.Name,
            JsonName(property),
            ToLabel(property.Name),
            ScenarioFieldGroup(property.Name),
            type,
            enumValues,
            IsAdvancedScenarioField(property.Name),
            hints.Minimum,
            hints.Maximum,
            hints.Step,
            hints.Units,
            hints.Description);
    }

    private static (double? Minimum, double? Maximum, double? Step, string? Units, string? Description) ScenarioFieldHints(
        string name,
        Type propertyType,
        string type)
    {
        var step = ScenarioFieldStep(name, propertyType, type);
        var units = ScenarioFieldUnits(name);
        var description = ScenarioFieldDescription(name);

        if (type != "number")
        {
            return (null, null, null, units, description);
        }

        if (name == "Seed")
        {
            return (0d, null, 1d, units, description);
        }

        if (name.Contains("Chance", StringComparison.Ordinal)
            || name.Contains("MutationRate", StringComparison.Ordinal)
            || name.Contains("Adaptation", StringComparison.Ordinal)
            || name is "DeathMeatEnergyFraction"
                or "CloseSenseRefreshProximity" or "LocalFertilityNeighborDepletionShare")
        {
            return (0d, 1d, step, units, description);
        }

        if (name == "VisionAngleRadians")
        {
            return (Math.PI / 12d, Math.Tau, 0.01d, units, description);
        }

        if (name == "StaleMeatDecayMultiplier")
        {
            return (1d, null, 0.1d, units, description);
        }

        if (name == "BrainHiddenNodeCount")
        {
            return (0d, NeuralBrainSchema.MaxHiddenNodeCount, 1d, units, description);
        }

        if (name.EndsWith("Count", StringComparison.Ordinal)
            || name.EndsWith("Ticks", StringComparison.Ordinal))
        {
            return (name == "InitialCreatureCount" ? 0d : 1d, null, 1d, units, description);
        }

        if (name.Contains("Seconds", StringComparison.Ordinal)
            || name.Contains("Calories", StringComparison.Ordinal)
            || name.Contains("Energy", StringComparison.Ordinal)
            || name.Contains("Radius", StringComparison.Ordinal)
            || name.Contains("Width", StringComparison.Ordinal)
            || name.Contains("Height", StringComparison.Ordinal)
            || name.Contains("Cost", StringComparison.Ordinal)
            || name.Contains("Multiplier", StringComparison.Ordinal)
            || name.Contains("Strength", StringComparison.Ordinal)
            || name.Contains("Weight", StringComparison.Ordinal)
            || name.Contains("Resistance", StringComparison.Ordinal)
            || name.Contains("Damage", StringComparison.Ordinal)
            || name.Contains("Health", StringComparison.Ordinal)
            || name.Contains("Padding", StringComparison.Ordinal)
            || name.EndsWith("PerMillionArea", StringComparison.Ordinal))
        {
            return (0d, null, step, units, description);
        }

        return (null, null, step, units, description);
    }

    private static string? ScenarioFieldUnits(string name)
    {
        if (name.EndsWith("Ticks", StringComparison.Ordinal))
        {
            return "ticks";
        }

        if (name.EndsWith("ThreadCount", StringComparison.Ordinal))
        {
            return "threads";
        }

        if (name.Contains("Seconds", StringComparison.Ordinal)
            || name is "FixedDeltaSeconds")
        {
            return "seconds";
        }

        if (name.Contains("Radians", StringComparison.Ordinal))
        {
            return "radians";
        }

        if (name == "HealingHealthFractionPerSecond")
        {
            return "max health fraction/s";
        }

        if (name == "HealingEnergyCostPerHealth")
        {
            return "energy per health";
        }

        if (name == "StaleMeatDecayMultiplier")
        {
            return "x";
        }

        if (name.Contains("Calories", StringComparison.Ordinal)
            || name.Contains("Kcal", StringComparison.Ordinal))
        {
            return "kcal";
        }

        if (name.EndsWith("PerSecond", StringComparison.Ordinal))
        {
            return "per second";
        }

        if (name.EndsWith("PerMillionArea", StringComparison.Ordinal))
        {
            return "per 1M area";
        }

        if (name.EndsWith("Rate", StringComparison.Ordinal)
            || name.EndsWith("Chance", StringComparison.Ordinal)
            || name.EndsWith("Fraction", StringComparison.Ordinal)
            || name.EndsWith("Share", StringComparison.Ordinal))
        {
            return "0-1";
        }

        return null;
    }

    private static double? ScenarioFieldStep(string name, Type propertyType, string type)
    {
        if (IsIntegerType(propertyType))
        {
            return 1d;
        }

        if (type != "number")
        {
            return null;
        }

        return name switch
        {
            "FixedDeltaSeconds" => 0.00001d,
            "LocalFertilityRecoveryPerSecond" => 0.00001d,
            "RtNeatEnabledConnectionEnergyCostPerSecond" => 0.00001d,
            "SenseRadiusEnergyCostPerSecond" => 0.0001d,
            "GutCapacityEnergyCostPerSecond" => 0.0001d,
            "RottenMeatDamagePerRawKcal" => 0.001d,
            _ when name.EndsWith("EnergyCostPerSecond", StringComparison.Ordinal) => 0.001d,
            _ => 0.01d
        };
    }

    private static string? ScenarioFieldDescription(string name)
    {
        return name switch
        {
            "Seed" => "Deterministic seed used to build the starting world.",
            "BrainArchitectureKind" => "Neural brain architecture used for founders and descendants.",
            "InitialBrainKind" => "Starter brain preset assigned to founders unless species seeds override it.",
            "BrainHiddenNodeCount" => "Hidden-node count requested for neural brain genomes.",
            "InitialCreatureCount" => "Number of founders spawned when no species roster is enabled.",
            "InitialResourcesPerMillionArea" => "Plant density scaled by world area.",
            "EnableSectorVision" => "Collects grouped raw sector vision samples for overlays and diagnostics; brain inputs use semantic visual summaries.",
            "ReuseNeuralActionsOnSkippedWorldSenses" => "Reuses the previous neural action on ticks where expensive world senses were not refreshed, unless contact or internal-state changes force a new decision.",
            "SensingThreadCount" => "Maximum worker threads used for creature sensing. Use 1 for the fully detailed single-threaded path.",
            "NeuralControllerThreadCount" => "Maximum worker threads used for neural controller evaluation. Defaults to 8; use 1 for the single-threaded deterministic path.",
            "CloseSenseRefreshMinimumTicks" => "Minimum age of stale world senses before proximity-only close cues can force an extra world refresh.",
            "PlantPayoffTraceHalfLifeSeconds" => "Controls how long recent typed plant payoff signals remain available to the brain.",
            "VisionAngleEnergyCostPerSecond" => "Baseline-scaled cubic upkeep for field-of-view width. Starter vision pays the configured baseline rate; panoramic vision rises steeply.",
            "SoundRangeMultiplier" => "Intentional communication sound range as a multiplier of sense radius.",
            "SoundDensitySaturation" => "Total nearby sound strength needed to saturate the sound-density input.",
            "EnableExtinctPayloadPruning" => "Drops genome and brain payloads that are not referenced by living creatures, eggs, or the current survivor ancestry chain.",
            "ExtinctPayloadPruneIntervalTicks" => "How often survivor-ancestry-aware genome and brain payload compaction runs.",
            "RtNeatHiddenNodeEnergyCostPerSecond" => "Optional metabolic upkeep charged for each hidden node in an rtNEAT graph brain. Defaults to zero so rtNEAT is not uniquely taxed.",
            "RtNeatEnabledConnectionEnergyCostPerSecond" => "Optional metabolic upkeep charged for each enabled connection in an rtNEAT graph brain. Defaults to zero so rtNEAT is not uniquely taxed.",
            "MetabolicPace" => "Starting life-history pace. Higher values burn more basal energy while speeding digestion, maturity, reproduction, healing, fertility aging, locomotion, and biological aging.",
            "MaxLifeExpectancySeconds" => "Baseline adult life expectancy before old-age mortality risk, adjusted by body size and metabolic pace.",
            "StaleMeatDecayMultiplier" => "Multiplier applied to meat calorie decay after carcasses become stale. Use 1x for carrion-heavy recipes.",
            "HealingDelaySeconds" => "Time after taking damage before passive healing can begin.",
            "HealingHealthFractionPerSecond" => "Fraction of maximum health restored per second once passive healing is active.",
            "HealingEnergyCostPerHealth" => "Energy spent for each point of health restored by passive healing.",
            "HealingMinimumEnergy" => "Passive healing stops at or below this energy reserve.",
            "EnableTemperature" => "Builds a cached temperature map for climate visualization, telemetry, thermal sensing, and optional thermal stress costs.",
            "ThermalMismatchBasalCostMultiplier" => "Extra basal upkeep applied at full thermal mismatch. Zero keeps temperature sensory and observational only.",
            "EcologicalEvents" => "Optional scheduled regional shocks. Fertility pulse/crash events multiply plant fertility; heat waves and cold snaps temporarily shift effective temperature.",
            "ObstacleEvents" => "Optional scheduled wall-group changes. Off opens the group; on closes it again at the requested tick.",
            "ThermalOptimum" => "Starting creature thermal optimum on the normalized 0 cold, 0.5 temperate, 1 hot scale.",
            "ThermalTolerance" => "Starting creature thermal tolerance. Larger values make temperature mismatch cues ramp up more slowly.",
            "EnableCreatureCollision" => "Prevents living creatures from passing through one another and records creature body collisions.",
            "CreatureCollisionSafeImpactSpeed" => "Relative closing speed below which creature body contact separates without impact damage.",
            "CreatureCollisionDamageScale" => "Damage multiplier for creature body impacts above the safe speed. Set to 0 for blocking without impact injury.",
            "CreatureCollisionSeparationIterations" => "Number of deterministic overlap-resolution passes after movement.",
            "EnableInjuryMemory" => "Gives creatures a decaying directional memory of recent attack or collision injury sources.",
            "InjuryMemoryHalfLifeSeconds" => "Seconds for recent injury-source memory to decay to half strength.",
            "InjuryMemoryDamageSignalScale" => "How strongly health damage stamps injury-source memory. Set to 0 to decay existing traces without adding new ones.",
            "WorldMapPath" => "Reusable world map artifact used when biomeMapKind and/or obstacleMapKind are manual.",
            "ManualBiomeMapPath" => "Manual biome map JSON used when biomeMapKind is manual.",
            "ManualObstacleMapPath" => "Manual obstacle map JSON used when obstacleMapKind is manual.",
            "SpeciesSeeds" => "Optional authored founder roster stored as scenario JSON.",
            _ => null
        };
    }

    private static string ScenarioEditorType(Type type)
    {
        if (type == typeof(bool))
        {
            return "boolean";
        }

        if (type.IsEnum)
        {
            return "enum";
        }

        if (type == typeof(string))
        {
            return "text";
        }

        return IsNumberType(type) ? "number" : "json";
    }

    private static bool IsNumberType(Type type)
    {
        return IsIntegerType(type)
            || type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal);
    }

    private static bool IsIntegerType(Type type)
    {
        return type == typeof(byte)
            || type == typeof(short)
            || type == typeof(int)
            || type == typeof(long)
            || type == typeof(ushort)
            || type == typeof(uint)
            || type == typeof(ulong);
    }

    private static string JsonName(PropertyInfo property)
    {
        return property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
            ?? JsonOptions.PropertyNamingPolicy!.ConvertName(property.Name);
    }

    private static string ToLabel(string name)
    {
        var builder = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];
            if (i > 0
                && char.IsUpper(current)
                && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        return builder.ToString();
    }

    private static string ScenarioFieldGroup(string name)
    {
        if (name is "Name" or "Seed" or "PipelineKind" or "InitialCreatureCount" or "InitialCreatureSpawnRegion" or "StatsSnapshotIntervalTicks")
        {
            return "Basics";
        }

        if (name.Contains("Mutation", StringComparison.Ordinal))
        {
            return "Mutation";
        }

        if (name is "ReuseNeuralActionsOnSkippedWorldSenses"
                or "SensingThreadCount"
                or "NeuralControllerThreadCount"
            || name.Contains("Prune", StringComparison.Ordinal)
            || name.Contains("Pruning", StringComparison.Ordinal)
            || name.Contains("Payload", StringComparison.Ordinal))
        {
            return "Performance";
        }

        if (name.Contains("Brain", StringComparison.Ordinal)
            || name.Contains("Vision", StringComparison.Ordinal)
            || name.Contains("Sense", StringComparison.Ordinal)
            || name.Contains("Sound", StringComparison.Ordinal)
            || name.Contains("Memory", StringComparison.Ordinal))
        {
            return "Brain & Vision";
        }

        if (name.Contains("Prey", StringComparison.Ordinal))
        {
            return "Diet & Combat";
        }

        if (name.Contains("World", StringComparison.Ordinal)
            || name.Contains("Biome", StringComparison.Ordinal)
            || name.Contains("Obstacle", StringComparison.Ordinal)
            || name.Contains("Tree", StringComparison.Ordinal)
            || name.Contains("Terrain", StringComparison.Ordinal)
            || name.Contains("Ecological", StringComparison.Ordinal)
            || name.Contains("Temperature", StringComparison.Ordinal)
            || name.Contains("Spatial", StringComparison.Ordinal)
            || name is "FixedDeltaSeconds")
        {
            return "World & Terrain";
        }

        if (name.Contains("Resource", StringComparison.Ordinal)
            || name.Contains("Plant", StringComparison.Ordinal)
            || name.Contains("Fertility", StringComparison.Ordinal))
        {
            return "Plants";
        }

        if (name.Contains("Season", StringComparison.Ordinal))
        {
            return "Seasons";
        }

        if (name.Contains("Reproduction", StringComparison.Ordinal)
            || name.Contains("Reproductive", StringComparison.Ordinal)
            || name.Contains("Offspring", StringComparison.Ordinal)
            || name.Contains("Egg", StringComparison.Ordinal)
            || name.Contains("Maturity", StringComparison.Ordinal)
            || name.Contains("Senescent", StringComparison.Ordinal)
            || name.Contains("Crowding", StringComparison.Ordinal)
            || name.Contains("InitialCreatureEnergy", StringComparison.Ordinal))
        {
            return "Reproduction";
        }

        if (name.Contains("Diet", StringComparison.Ordinal)
            || name.Contains("Carrion", StringComparison.Ordinal)
            || name.Contains("Meat", StringComparison.Ordinal)
            || name.Contains("Prey", StringComparison.Ordinal)
            || name.Contains("Bite", StringComparison.Ordinal)
            || name.Contains("Damage", StringComparison.Ordinal)
            || name.Contains("Healing", StringComparison.Ordinal)
            || name.Contains("Eat", StringComparison.Ordinal)
            || name.Contains("Gut", StringComparison.Ordinal)
            || name.Contains("Digestion", StringComparison.Ordinal))
        {
            return "Diet & Combat";
        }

        if (name.Contains("Energy", StringComparison.Ordinal)
            || name.Contains("Fat", StringComparison.Ordinal)
            || name.Contains("Movement", StringComparison.Ordinal)
            || name.Contains("Speed", StringComparison.Ordinal)
            || name.Contains("Turn", StringComparison.Ordinal)
            || name.Contains("BodyRadius", StringComparison.Ordinal)
            || name.Contains("Basal", StringComparison.Ordinal)
            || name.Contains("Metabolic", StringComparison.Ordinal)
            || name.Contains("Pace", StringComparison.Ordinal))
        {
            return "Energy & Movement";
        }

        if (name.Contains("Species", StringComparison.Ordinal))
        {
            return "Species";
        }

        return "Advanced";
    }

    private static bool IsAdvancedScenarioField(string name)
    {
        return name.Contains("Multiplier", StringComparison.Ordinal)
            || name.Contains("EnergyCost", StringComparison.Ordinal)
            || name.Contains("LocalFertility", StringComparison.Ordinal)
            || name.Contains("Species", StringComparison.Ordinal)
            || name.Contains("Phase", StringComparison.Ordinal)
            || name is "FixedDeltaSeconds"
                or "CloseSenseRefreshProximity"
                or "CloseSenseRefreshMinimumTicks"
                or "ReuseNeuralActionsOnSkippedWorldSenses"
                or "SensingThreadCount"
                or "NeuralControllerThreadCount";
    }
}
