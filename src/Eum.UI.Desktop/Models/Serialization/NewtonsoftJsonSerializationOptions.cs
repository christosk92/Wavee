using Newtonsoft.Json;

namespace Eum.UI.Models.Serialization;

public class NewtonsoftJsonSerializationOptions
{
    private static readonly JsonSerializerSettings CurrentSettings = new()
    {
        Converters = new List<JsonConverter>()
    };
    public static readonly NewtonsoftJsonSerializationOptions Default = new();

    private NewtonsoftJsonSerializationOptions()
    {
    }

    public JsonSerializerSettings Settings => CurrentSettings;
}
