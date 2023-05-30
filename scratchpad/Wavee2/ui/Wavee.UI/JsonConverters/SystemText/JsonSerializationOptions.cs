
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wavee.UI.JsonConverters.SystemText;

internal class JsonSerializationOptions
{
    private static readonly JsonSerializerOptions CurrentSettings = new JsonSerializerOptions
    {
        Converters =
        {
            new JsonHashmapConverter(),
            new JsonOptionStringConverter()
        },
        PropertyNameCaseInsensitive = true
    };

    public static readonly JsonSerializationOptions Default = new();

    private JsonSerializationOptions()
    {
    }

    public JsonSerializerOptions Settings => CurrentSettings;
}
