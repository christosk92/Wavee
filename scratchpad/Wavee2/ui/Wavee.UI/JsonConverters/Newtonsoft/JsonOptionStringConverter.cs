using LanguageExt.UnsafeValueAccess;
using Newtonsoft.Json;

namespace Wavee.UI.JsonConverters.Newtonsoft;

internal sealed class JsonOptionStringConverter : JsonConverter<Option<string>>
{
    public override void WriteJson(JsonWriter writer, Option<string> value, JsonSerializer serializer)
    {
        if (value.IsSome)
        {
            writer.WriteValue(value.ValueUnsafe());
        }
        else
        {
            writer.WriteNull();
        }
    }

    public override Option<string> ReadJson(JsonReader reader, Type objectType, Option<string> existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var str = reader.Value.ToString();
        return str == null ? Option<string>.None : Option<string>.Some(str);
    }
}