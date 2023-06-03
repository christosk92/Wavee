using System.Text.Json;
using System.Text.Json.Serialization;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;

namespace Wavee;

public readonly record struct Artwork(
    [property: JsonPropertyName("url")]
    string Url,
    [property: JsonConverter(typeof(JsonOptionNullableConverterAttribute<int>))]
    Option<int> Width,
    [property: JsonConverter(typeof(JsonOptionNullableConverterAttribute<int>))]
    Option<int> Height,
    [property: JsonIgnore]
    Option<ArtworkSizeType> Size);

public sealed class JsonOptionNullableConverterAttribute<T> : JsonConverter<Option<T>>
{
    public override Option<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Option<T>.None;
        }
        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, Option<T> value, JsonSerializerOptions options)
    {
        if (value.IsNone)
        {
            writer.WriteNullValue();
            return;
        }
        JsonSerializer.Serialize(writer, value.ValueUnsafe(), options);
    }
}