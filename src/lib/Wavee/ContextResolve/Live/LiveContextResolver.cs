using System.Text.Json;
using Eum.Spotify.context;
using LanguageExt;
using Wavee.Token.Live;
using static LanguageExt.Prelude;

namespace Wavee.ContextResolve.Live;

internal readonly struct LiveContextResolver : IContextResolver
{
    private readonly Func<IMercuryClient> _mercuryFactory;

    public LiveContextResolver(Func<IMercuryClient> mercuryFactory)
    {
        _mercuryFactory = mercuryFactory;
    }

    public async Task<SpotifyContext> Resolve(string uri, CancellationToken cancellationToken)
    {
        const string query = "hm://context-resolve/v1/{0}";
        var url = string.Format(query, uri);
        var finalData = await _mercuryFactory().Get(url, cancellationToken);

        using var jsonDocument = JsonDocument.Parse(finalData.Payload);
        var parsed = Parse(jsonDocument);
        return parsed;
    }

    public async Task<SpotifyContext> ResolveRaw(string pageUrl, CancellationToken ct = default)
    {
        var finalData = await _mercuryFactory().Get(pageUrl, ct);
        
        using var jsonDocument = JsonDocument.Parse(finalData.Payload);
        var parsed = Parse(jsonDocument);
        return parsed;
    }

    private static SpotifyContext Parse(JsonDocument jsonDocument)
    {
        var metadata = jsonDocument.RootElement.TryGetProperty("metadata", out var metadataElement)
            ? metadataElement.EnumerateObject().Fold(new HashMap<string, string>(),
                (acc, x) => acc.Add(x.Name, x.Value.GetString()!))
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
        return new SpotifyContext(url!, metadata, pages, restrictions);
    }
}