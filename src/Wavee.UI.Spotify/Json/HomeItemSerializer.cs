using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.UI.Spotify.Responses.Parsers;

namespace Wavee.UI.Spotify.Json;

internal sealed class HomeItemSerializer : JsonConverter<IHomeItem>
{
    public override IHomeItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Implement your custom deserialization logic here
        // Example:
        using var doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        var typename = root.GetProperty("content").GetProperty("__typename").GetString();
        if (typename is "UnknownType")
            return null;
        
        var content = root.GetProperty("content").GetProperty("data");
        var item = content.ParseHomeItem();
        
        return item;
    }

    public override void Write(Utf8JsonWriter writer, IHomeItem value, JsonSerializerOptions options)
    {
        // Implement your custom serialization logic here if needed
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}