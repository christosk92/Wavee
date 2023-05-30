using Newtonsoft.Json;

namespace Wavee.UI.JsonConverters.Newtonsoft;

internal class JsonSerializationOptions
{
    private static readonly JsonSerializerSettings CurrentSettings = new()
    {
        Converters = new List<JsonConverter>()
        {
            new JsonHashmapConverter(),
            new JsonOptionStringConverter(),
        }
    };

    public static readonly JsonSerializationOptions Default = new();

    private JsonSerializationOptions()
    {
    }

    public JsonSerializerSettings Settings => CurrentSettings;
}
