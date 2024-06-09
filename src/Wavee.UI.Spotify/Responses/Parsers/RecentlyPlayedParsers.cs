using System;
using System.Text.Json;

namespace Wavee.UI.Spotify.Responses.Parsers;

public static class RecentlyPlayedParsers
{
    public static SpotifyPlayContext[] ParseSpotifyPlayContexts(this JsonElement element, int expectedCount)
    {
        using var enumerator = element.EnumerateArray();
        Span<SpotifyPlayContext> playContexts = new SpotifyPlayContext[expectedCount];
        int index = 0;
        while (enumerator.MoveNext())
        {
            var item = enumerator.Current;
            var uri = item.GetProperty("uri").GetString();
            var playedAt = item.GetProperty("lastPlayedTime").GetInt64();
            var trackUri = item.GetProperty("lastPlayedTrackUri").GetString();
            playContexts[index++] =
                new SpotifyPlayContext(uri, DateTimeOffset.FromUnixTimeMilliseconds(playedAt), trackUri);
        }

        return playContexts[..index].ToArray();
    }
}