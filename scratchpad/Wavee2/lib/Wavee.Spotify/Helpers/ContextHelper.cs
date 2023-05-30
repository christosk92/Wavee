using System.Text.Json;
using Eum.Spotify.context;

namespace Wavee.Spotify.Helpers;

internal static class ContextHelper
{
    public static ContextTrack ParseTrack(JsonElement track)
    {
        var ctxTrack = new ContextTrack
        {
            Uri = track.GetProperty("uri").GetString(),
        };
        if (track.TryGetProperty("uid", out var uid))
            ctxTrack.Uid = uid.GetString();

        if (track.TryGetProperty("metadata", out var metadata))
        {
            ctxTrack = metadata.EnumerateObject().Fold(
                ctxTrack,
                (acc, x) =>
                {
                    acc.Metadata[x.Name] = x.Value.GetString();
                    return acc;
                });
        }

        return ctxTrack;
    }

    public static ContextPage ParsePage(JsonElement page)
    {
        var pg = new ContextPage();
        if (page.TryGetProperty("next_page_url", out var nextPageUrl))
        {
            pg.NextPageUrl = nextPageUrl.GetString();
        }

        if (page.TryGetProperty("page_url", out var pageUrl))
            pg.PageUrl = pageUrl.GetString();

        if (page.TryGetProperty("tracks", out var tracks))
        {
            using var tracksenum = tracks.EnumerateArray();
            foreach (var track in tracksenum)
            {
                //uri, uid, metadata
                var ctxTrack = ParseTrack(track);

                pg.Tracks.Add(ctxTrack);
            }
        }

        return pg;
    }
}