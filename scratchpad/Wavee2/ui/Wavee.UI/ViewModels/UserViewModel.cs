using System.Text.Json.Serialization;
using Wavee.UI.JsonConverters.SystemText;

namespace Wavee.UI.ViewModels;

public readonly record struct User(string Id,
    bool IsDefault,
    [property: JsonConverter(typeof(JsonOptionStringConverter))]
    Option<string> DisplayName,
    [property: JsonConverter(typeof(JsonOptionStringConverter))]
    Option<string> ImageId,
    [property: JsonConverter(typeof(JsonHashmapConverter))]
    HashMap<string, string> Metadata)
{
    public User SetDisplayName(Option<string> displayName)
    {
        return this with
        {
            DisplayName = displayName
        };
    }
}