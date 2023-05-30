using System.Text.Json.Serialization;
using System.Text.Json;
using LanguageExt.UnsafeValueAccess;

namespace Wavee.UI.JsonConverters.SystemText;

internal sealed class JsonOptionStringConverter : JsonConverter<Option<string>>
{
    public override Option<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return str == null ? Option<string>.None : Option<string>.Some(str);
    }

    public override void Write(Utf8JsonWriter writer, Option<string> value, JsonSerializerOptions options)
    {
        if (value.IsSome)
        {
            writer.WriteStringValue(value.ValueUnsafe());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}