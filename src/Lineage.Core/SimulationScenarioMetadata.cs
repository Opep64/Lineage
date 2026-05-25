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
        var step = IsIntegerType(propertyType) ? 1d : type == "number" ? 0.01d : (double?)null;
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
            || name is "DietaryAdaptation" or "CarrionAdaptation" or "DeathMeatEnergyFraction"
                or "CloseSenseRefreshProximity" or "LocalFertilityNeighborDepletionShare")
        {
            return (0d, 1d, step, units, description);
        }

        if (name == "VisionAngleRadians")
        {
            return (Math.PI / 12d, Math.Tau, 0.01d, units, description);
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
            || name.Contains("Resistance", StringComparison.Ordinal)
            || name.Contains("Damage", StringComparison.Ordinal)
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

        if (name.Contains("Seconds", StringComparison.Ordinal)
            || name is "FixedDeltaSeconds")
        {
            return "seconds";
        }

        if (name.Contains("Radians", StringComparison.Ordinal))
        {
            return "radians";
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
            "EnableSectorVision" => "Enables grouped sector vision inputs for the neural controller.",
            "EnableLegacyNearestFoodVisionInputs" => "Keeps legacy nearest-food inputs available beside sector vision.",
            "EnableLegacyNearestCreatureVisionInputs" => "Keeps legacy nearest-creature inputs available beside sector vision.",
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

        if (name.Contains("Brain", StringComparison.Ordinal)
            || name.Contains("Vision", StringComparison.Ordinal)
            || name.Contains("Sense", StringComparison.Ordinal)
            || name.Contains("Memory", StringComparison.Ordinal))
        {
            return "Brain & Vision";
        }

        if (name.Contains("World", StringComparison.Ordinal)
            || name.Contains("Biome", StringComparison.Ordinal)
            || name.Contains("Obstacle", StringComparison.Ordinal)
            || name.Contains("Terrain", StringComparison.Ordinal)
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
            || name.Contains("Bite", StringComparison.Ordinal)
            || name.Contains("Damage", StringComparison.Ordinal)
            || name.Contains("Eat", StringComparison.Ordinal)
            || name.Contains("Gut", StringComparison.Ordinal)
            || name.Contains("Digestion", StringComparison.Ordinal))
        {
            return "Diet & Combat";
        }

        if (name.Contains("Energy", StringComparison.Ordinal)
            || name.Contains("Movement", StringComparison.Ordinal)
            || name.Contains("Speed", StringComparison.Ordinal)
            || name.Contains("Turn", StringComparison.Ordinal)
            || name.Contains("BodyRadius", StringComparison.Ordinal)
            || name.Contains("Basal", StringComparison.Ordinal))
        {
            return "Energy & Movement";
        }

        if (name.Contains("Mutation", StringComparison.Ordinal))
        {
            return "Mutation";
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
            || name is "FixedDeltaSeconds" or "CloseSenseRefreshProximity";
    }
}
