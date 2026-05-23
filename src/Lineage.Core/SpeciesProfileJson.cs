using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lineage.Core;

/// <summary>
/// JSON helpers for portable species profile files.
/// </summary>
public static class SpeciesProfileJson
{
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    public static string ToJson(SpeciesProfile profile)
    {
        return JsonSerializer.Serialize(profile.Validated(), JsonOptions);
    }

    public static SpeciesProfile FromJson(string json)
    {
        var profile = JsonSerializer.Deserialize<SpeciesProfile>(json, JsonOptions)
            ?? throw new InvalidOperationException("Species profile JSON did not contain a profile object.");
        return profile.Validated();
    }

    public static SpeciesProfile Load(string path)
    {
        return FromJson(File.ReadAllText(path));
    }

    public static void Save(string path, SpeciesProfile profile)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, ToJson(profile));
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
}
