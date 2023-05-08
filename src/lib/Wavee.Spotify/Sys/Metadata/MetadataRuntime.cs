using Spotify.Metadata;
using Wavee.Spotify.Sys.Common;
using Wavee.Spotify.Sys.Connection;
using Wavee.Spotify.Sys.Mercury;

namespace Wavee.Spotify.Sys.Metadata;

public static class MetadataRuntime
{
    public static async ValueTask<Track> GetTrack(this SpotifyConnectionInfo connectionInfo, string hexId,
        CancellationToken ct = default)
    {
        const string uri = "hm://metadata/4/track";

        var finalUri = $"{uri}/{hexId}";

        var response = await connectionInfo.Get(finalUri, Option<string>.None, ct: ct);
        return response.Header.StatusCode switch
        {
            200 => Track.Parser.ParseFrom(response.Body.Span),
            _ => throw new InvalidOperationException()
        };
    }

    public static async ValueTask<Episode> GetEpisode(this SpotifyConnectionInfo connectionInfo, string hexId,
        CancellationToken ct = default)
    {
        const string uri = "hm://metadata/4/episode";

        var finalUri = $"{uri}/{hexId}";

        var response = await connectionInfo.Get(finalUri, Option<string>.None, ct: ct);
        return response.Header.StatusCode switch
        {
            200 => Episode.Parser.ParseFrom(response.Body.Span),
            _ => throw new InvalidOperationException()
        };
    }
}