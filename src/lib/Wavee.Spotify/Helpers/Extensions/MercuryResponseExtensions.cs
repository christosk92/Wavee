using System.Text.Json;
using Wavee.Spotify.Constants;
using Wavee.Spotify.Infrastructure.Common.Mercury;

namespace Wavee.Spotify.Helpers.Extensions;

public static class MercuryResponseExtensions
{
    public static Option<T> DeserializeFromJson<T>(this MercuryResponse response)
    {
        Span<byte> data = new byte[response.TotalLength];
        var copied = 0;
        foreach (var part in response.Payload)
        {
            part.Span.CopyTo(data.Slice(copied));
            copied += part.Length;
        }

        return JsonSerializer.Deserialize<T>(data, SystemTextJsonOptions.Options);
    }
}