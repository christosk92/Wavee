
using Newtonsoft.Json;

namespace Wavee.UI.JsonConverters.Newtonsoft;
internal sealed class JsonHashmapConverter : JsonConverter<HashMap<string, string>>
{
    public override void WriteJson(JsonWriter writer, HashMap<string, string> value, JsonSerializer serializer)
    {
        //just a dictionary
        //write key value pairs

        writer.WriteStartObject();
        foreach (var (key, val) in value)
        {
            writer.WritePropertyName(key);
            writer.WriteValue(val);
        }

        writer.WriteEndObject();
    }

    public override HashMap<string, string> ReadJson(JsonReader reader, Type objectType, HashMap<string, string> existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var result = new HashMap<string, string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonToken.PropertyName)
            {
                var key = reader.Value.ToString();
                reader.Read();
                var value = reader.Value.ToString();
                result = result.AddOrUpdate(key, value);
            }
        }
        return result;
    }
}