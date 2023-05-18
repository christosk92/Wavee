using System.Text;
using System.Text.Json;
using Eum.Spotify.context;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Core.Ids;
using Wavee.Spotify.Helpers;
using Wavee.Spotify.Infrastructure.Playback;

namespace Wavee.Spotify.Infrastructure.Mercury;

public readonly record struct SpotifyContext(string Url, HashMap<string, string> Metadata, Seq<ContextPage> Pages,
    HashMap<string, Seq<string>> Restrictions);

public static class MercuryClientExtensions
{
    public static async Task<string> AutoplayQuery(this MercuryClient mercuryClient, string uri,
        CancellationToken ct = default)
    {
        const string query = "hm://autoplay-enabled/query?uri={0}";
        var response = await mercuryClient.Get(string.Format(query, uri), ct);
        var context = Encoding.UTF8.GetString(response.Payload.Span);
        return context;
    }

    public static async Task<TrackOrEpisode> GetMetadata(this MercuryClient mercuryClient, AudioId itemId,
        string country,
        CancellationToken ct = default)
    {
        const string episode = "hm://metadata/4/episode/{0}?country={1}";
        const string track = "hm://metadata/4/track/{0}?country={1}";

        var url = itemId.Type switch
        {
            AudioItemType.Track => string.Format(track, itemId.ToBase16(), country),
            AudioItemType.PodcastEpisode => string.Format(episode, itemId.ToBase16(), country),
            _ => throw new ArgumentOutOfRangeException(nameof(itemId), itemId, null)
        };

        var response = await mercuryClient.Get(url, ct);

        return itemId.Type switch
        {
            AudioItemType.PodcastEpisode => new TrackOrEpisode(Episode.Parser.ParseFrom(response.Payload.Span)),
            AudioItemType.Track => new TrackOrEpisode(Track.Parser.ParseFrom(response.Payload.Span)),
        };
    }

    public static async Task<SpotifyContext> ContextResolve(this MercuryClient mercury, string contextUri,
        CancellationToken ct = default)
    {
        const string query = "hm://context-resolve/v1/{0}";
        var url = string.Format(query, contextUri);
        var response = await mercury.Get(url, ct);
        using var jsonDocument = JsonDocument.Parse(response.Payload);
        var parsed = Parse(jsonDocument);
        return parsed;
    }

    public static async Task<SpotifyContext> ContextResolveRaw(this MercuryClient mercury, string contextUrl,
        CancellationToken ct = default)
    {
        var response = await mercury.Get(contextUrl, ct);
        using var jsonDocument = JsonDocument.Parse(response.Payload);
        var parsed = Parse(jsonDocument);
        return parsed;
    }

    private static SpotifyContext Parse(JsonDocument jsonDocument)
    {
        var metadata = jsonDocument.RootElement.TryGetProperty("metadata", out var metadataElement)
            ? metadataElement.EnumerateObject().Fold(new HashMap<string, string>(),
                (acc, x) => acc.Add(x.Name, x.Value.GetString()))
            : Empty;


        var pages = jsonDocument.RootElement.TryGetProperty("pages", out var pagesElement)
            ? pagesElement.Clone().EnumerateArray().Select(ContextHelper.ParsePage).ToSeq()
            : Empty;
        var url = jsonDocument.RootElement.TryGetProperty("url", out var urlElement)
            ? urlElement.GetString()
            : null;

        var tracks = jsonDocument.RootElement.TryGetProperty("tracks", out var tracksElement)
            ? tracksElement.Clone().EnumerateArray().Select(ContextHelper.ParseTrack).ToSeq()
            : Empty;
        //if(pages is empty, add tracks to pages)
        if (pages.IsEmpty && !tracks.IsEmpty)
        {
            pages = Seq1(new ContextPage
            {
                Tracks = { tracks }
            });
        }

        var restrictions = jsonDocument.RootElement.TryGetProperty("restrictions", out var restrictionsElement)
            ? restrictionsElement.EnumerateObject().Fold(new HashMap<string, Seq<string>>(),
                (acc, x) => acc.Add(x.Name, x.Value.Clone().EnumerateArray().Select(y => y.GetString()).ToSeq()!))
            : Empty;
        return new SpotifyContext(url, metadata, pages, restrictions);
    }
}