using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wavee.UI.Spotify.Responses;

namespace Wavee.UI.Spotify.Json;

internal sealed class HomeSectionSerializer : JsonConverter<ISpotifyHomeSectionData>
{
    public override ISpotifyHomeSectionData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Implement your custom deserialization logic here
        // Example:
        using var doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        var typename = root.GetProperty("__typename").GetString();
        return typename switch
        {
            "HomeShortsSectionData" => root.Deserialize<HomeShortsSectionData>(options),
            "HomeRecentlyPlayedSectionData" => root.Deserialize<HomeRecentlyPlayedSectionData>(options),
            "HomeSpotlightSectionData" => root.Deserialize<HomeSpotlightSectionData>(options),
            _ => root.Deserialize<HomeGenericSectionData>(options)
        };

    }

    public override void Write(Utf8JsonWriter writer, ISpotifyHomeSectionData value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}