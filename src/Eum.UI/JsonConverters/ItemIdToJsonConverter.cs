using System.Text.Json;
using Eum.UI.Items;
using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Eum.UI.JsonConverters;

public class ItemIdToJsonConverter : System.Text.Json.Serialization.JsonConverter<ItemId>
{
    public override ItemId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;
        return new ItemId(reader.GetString() ?? throw new InvalidOperationException());
    }

    public override void Write(Utf8JsonWriter writer, ItemId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Uri);
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(ItemId);
    }
}
public class ItemIdToJsonConverterNewtonsoft : Newtonsoft.Json.JsonConverter<ItemId>
{
    public override void WriteJson(JsonWriter writer, ItemId value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Uri);
    }

    public override ItemId ReadJson(JsonReader reader, Type objectType, ItemId existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        return new ItemId(reader.ReadAsString() ?? throw new InvalidOperationException());
    }
}