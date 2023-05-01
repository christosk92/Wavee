using System.Text.Json;

namespace Wavee.Spotify.Constants;

internal static class SystemTextJsonOptions
{
    static SystemTextJsonOptions()
    {
        Options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public static JsonSerializerOptions Options { get; }
}