using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lineage.Core;

/// <summary>
/// JSON helpers for portable brain profile files.
/// </summary>
public static class BrainProfileJson
{
    public const string FileExtension = ".brain.json";

    public const string FilePattern = "*.brain.json";

    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    public static string ToJson(BrainProfile profile)
    {
        return JsonSerializer.Serialize(profile.Validated(), JsonOptions);
    }

    public static BrainProfile FromJson(string json)
    {
        var profile = JsonSerializer.Deserialize<BrainProfile>(json, JsonOptions)
            ?? throw new InvalidOperationException("Brain profile JSON did not contain a profile object.");
        return profile.Validated();
    }

    public static BrainProfile Load(string path)
    {
        return FromJson(File.ReadAllText(path));
    }

    public static void Save(string path, BrainProfile profile)
    {
        path = WithFileExtension(path);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, ToJson(profile));
    }

    public static string WithFileExtension(string path)
    {
        if (path.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        return Path.ChangeExtension(path, FileExtension);
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
