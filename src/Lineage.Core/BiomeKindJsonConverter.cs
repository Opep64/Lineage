using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lineage.Core;

public sealed class BiomeKindJsonConverter : JsonConverter<BiomeKind>
{
    public override BiomeKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var numericValue))
        {
            return BiomeKinds.Canonicalize((BiomeKind)numericValue);
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Biome kind must be a string or integer.");
        }

        var value = reader.GetString();
        return value?.Trim().ToLowerInvariant() switch
        {
            "desert" or "barren" => BiomeKind.Desert,
            "scrubland" or "scrub" or "sparse" => BiomeKind.Scrubland,
            "grassland" => BiomeKind.Grassland,
            "fertile" or "rich" => BiomeKind.Fertile,
            "forest" => BiomeKind.Forest,
            "wetland" or "wetlands" => BiomeKind.Wetland,
            "tundra" => BiomeKind.Tundra,
            "highland" or "highlands" => BiomeKind.Highland,
            _ => throw new JsonException($"Unknown biome kind '{value}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, BiomeKind value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(BiomeKinds.Canonicalize(value) switch
        {
            BiomeKind.Desert => "desert",
            BiomeKind.Scrubland => "scrubland",
            BiomeKind.Grassland => "grassland",
            BiomeKind.Fertile => "fertile",
            BiomeKind.Forest => "forest",
            BiomeKind.Wetland => "wetland",
            BiomeKind.Tundra => "tundra",
            BiomeKind.Highland => "highland",
            _ => "grassland"
        });
    }
}
