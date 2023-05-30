using System.Text.Json.Serialization;
using System.Text.Json;

namespace Wavee.UI.JsonConverters.SystemText;
internal sealed class JsonHashmapConverter : JsonConverter<HashMap<string, string>>
{
    public override HashMap<string, string> Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var result = new HashMap<string, string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var key = reader.GetString();
                reader.Read();
                var value = reader.GetString();
                result = result.AddOrUpdate(key, value);
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, HashMap<string, string> value, JsonSerializerOptions options)
    {
        //just a dictionary
        //write key value pairs
        writer.WriteStartObject();
        foreach (var (key, val) in value)
        {
            writer.WriteString(key, val);
        }

        writer.WriteEndObject();
    }
}