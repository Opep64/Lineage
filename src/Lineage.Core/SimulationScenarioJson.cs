using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lineage.Core;

/// <summary>
/// JSON serialization helpers for reproducible scenario files.
/// </summary>
public static class SimulationScenarioJson
{
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    public static string ToJson(SimulationScenario scenario)
    {
        return JsonSerializer.Serialize(scenario.Validated(), JsonOptions);
    }

    public static SimulationScenario FromJson(string json)
    {
        var scenario = JsonSerializer.Deserialize<SimulationScenario>(json, JsonOptions)
            ?? throw new InvalidOperationException("Scenario JSON did not contain a scenario object.");
        return ApplyMigrations(json, scenario).Validated();
    }

    public static SimulationScenario Load(string path)
    {
        return FromJson(File.ReadAllText(path));
    }

    public static void Save(string path, SimulationScenario scenario)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, ToJson(scenario));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private static SimulationScenario ApplyMigrations(string json, SimulationScenario scenario)
    {
        using var document = JsonDocument.Parse(json);
        var migrated = scenario;

        if (!TryGetProperty(document.RootElement, "initialBrainKind", out _)
            && TryGetProperty(document.RootElement, "randomizeInitialBrainWeights", out var legacyRandomBrainsElement)
            && legacyRandomBrainsElement.ValueKind is JsonValueKind.True or JsonValueKind.False
            && legacyRandomBrainsElement.GetBoolean())
        {
            migrated = migrated with { InitialBrainKind = InitialBrainKind.RandomPerFounder };
        }

        if (!TryGetProperty(document.RootElement, "initialResourcesPerMillionArea", out _)
            && TryGetProperty(document.RootElement, "initialResourceCount", out var legacyResourceCountElement))
        {
            var legacyResourceCount = legacyResourceCountElement.GetInt32();
            migrated = migrated with
            {
                InitialResourcesPerMillionArea = SimulationScenario.CalculateResourcesPerMillionArea(
                    legacyResourceCount,
                    migrated.WorldWidth,
                    migrated.WorldHeight)
            };
        }

        return migrated;
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
