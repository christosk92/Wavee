using System.Text;
using System.Text.Json;
using System.Web;
using LanguageExt.Effects.Traits;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Common;
using Wavee.Spotify.Clients.SpApi;
using Wavee.Spotify.Common;
using Wavee.Spotify.Infrastructure.Sys;

namespace Wavee.Spotify.Clients.Mercury;

public interface IMercuryClient
{
    ValueTask<MercuryResponse> Send(MercuryMethod method, string uri, Option<string> contentType);

    ValueTask<SearchResponse> Search(string query,
        string types,
        int offset = 0,
        int limit = 10,
        CancellationToken ct = default);

    ValueTask<Track> GetTrack(string id, CancellationToken cancellationToken = default);
    ValueTask<Episode> GetEpisode(string id, CancellationToken cancellationToken = default);

    ValueTask<string> FetchBearer(CancellationToken ct = default);
}

public readonly record struct SearchResponse(Seq<SearchCategory> Categories)
{
    internal static SearchResponse ParseFrom(ReadOnlyMemory<byte> span)
    {
        using var jsonDocument = JsonDocument.Parse(span);
        var root = jsonDocument.RootElement;
        var categoriesOrder = root.GetProperty("categoriesOrder").EnumerateArray().Select(x => x.GetString());
        var results = root.GetProperty("results").Clone();

        var searchResponse = new SearchResponse();
        foreach (var categoryOrder in categoriesOrder)
        {
            var category = ParseCategory(results, categoryOrder);

            if (category.IsSome)
            {
                searchResponse = searchResponse with { Categories = searchResponse.Categories.Add(category.ValueUnsafe()) };
            }
        }

        return searchResponse;
    }

    private static Option<SearchCategory> ParseCategory(JsonElement root, string categoryOrder)
    {
        try
        {
            if (!root.TryGetProperty(categoryOrder, out var category))
                return Option<SearchCategory>.None; ;
            var total = category.TryGetProperty("total", out var totalElem) ? totalElem.GetInt32() : 0;
            var hits = category.GetProperty("hits").EnumerateArray();

            var hitsMapped = hits.Select(ParseFrom).ToSeq();

            return new SearchCategory(categoryOrder, total, hitsMapped);
        }
        catch (KeyNotFoundException)
        {
            return Option<SearchCategory>.None;
        }
    }

    private static ISearchHit ParseFrom(
        JsonElement jsonElement)
    {
        var uri = jsonElement.GetProperty("uri").GetString();
        var id = new SpotifyId(uri);

        return id.Type switch
        {
            AudioItemType.Track => new TrackSearchHit(id,
                jsonElement.GetProperty("name").GetString(),
                Artists: jsonElement.GetProperty("artists").EnumerateArray().Select(x =>
                    new NameUriCombo(new SpotifyId(x.GetProperty("uri").GetString()),
                        x.GetProperty("name").GetString())).ToSeq(),
                Album: new NameUriCombo(new SpotifyId(jsonElement.GetProperty("album").GetProperty("uri").GetString()),
                    jsonElement.GetProperty("album").GetProperty("name").GetString()),
                Duration: jsonElement.GetProperty("duration").GetUInt32(),
                Image: jsonElement.GetProperty("image").GetString()),
            _ => default
        };
    }
}

public readonly record struct SearchCategory(string Category, int Total, Seq<ISearchHit> Hits);

public interface ISearchHit
{
    SpotifyId Id { get; }
}

public readonly record struct TrackSearchHit(SpotifyId Id, string Name, Seq<NameUriCombo> Artists, NameUriCombo Album,
    uint Duration, string Image) : ISearchHit;

public readonly record struct NameUriCombo(SpotifyId Id, string Name);

internal readonly struct MercuryClientImpl<RT> : IMercuryClient where RT : struct, HasCancel<RT>
{
    private readonly Guid _connectionId;
    private readonly Ref<Option<ulong>> _nextMercurySequence;
    private readonly Option<string> _countryCodeRef;
    private static Ref<HashMap<string, BearerToken>> _bearerCache = Ref(LanguageExt.HashMap<string, BearerToken>.Empty);
    private readonly RT _rt;
    private readonly Option<string> _userId;

    public MercuryClientImpl(Guid connectionId,
        Ref<Option<ulong>> nextMercurySequence,
        Option<string> countryCodeRef, Option<string> userId, RT rt)
    {
        _connectionId = connectionId;
        _nextMercurySequence = nextMercurySequence;
        _countryCodeRef = countryCodeRef;
        _userId = userId;
        _rt = rt;
    }

    public async ValueTask<MercuryResponse> Send(MercuryMethod method, string uri, Option<string> contentType)
    {
        var listenerResult = SpotifyRuntime.GetChannelReader(_connectionId);
        var getWriter = SpotifyRuntime.GetSender(_connectionId);

        var response =
            await MercuryRuntime.Send(method, uri, contentType, _nextMercurySequence, getWriter, listenerResult);

        var run = SpotifyRuntime.RemoveListener(_connectionId, listenerResult);

        return response;
    }

    public async ValueTask<SearchResponse> Search(string query,
        string types,
        int offset = 0,
        int limit = 10,
        CancellationToken ct = default)
    {
        const string uri =
            "hm://searchview/km/v4/search/{0}?offset={1}&limit={2}&entityVersion={3}&catalogue={4}&country={5}&locale={6}&categories={7}";
        var finalUrl = string.Format(uri, HttpUtility.UrlEncode(query, Encoding.UTF8), offset, limit, 2, "premium",
            _countryCodeRef.IfNone("US"), "en", types);

        var response = await Send(MercuryMethod.Get, finalUrl, Option<string>.None);
        return response.Header.StatusCode switch
        {
            200 => SearchResponse.ParseFrom(response.Body),
            _ => throw new InvalidOperationException()
        };
    }

    public async ValueTask<Track> GetTrack(string id, CancellationToken cancellationToken = default)
    {
        const string uri = "hm://metadata/4/track";

        var finalUri = $"{uri}/{id}";

        var response = await Send(MercuryMethod.Get, finalUri, Option<string>.None);
        return response.Header.StatusCode switch
        {
            200 => Track.Parser.ParseFrom(response.Body.Span),
            _ => throw new InvalidOperationException()
        };
    }

    public async ValueTask<Episode> GetEpisode(string id, CancellationToken cancellationToken = default)
    {
        const string uri = "hm://metadata/4/episode";

        var finalUri = $"{uri}/{id}";

        var response = await Send(MercuryMethod.Get, finalUri, Option<string>.None);
        return response.Header.StatusCode switch
        {
            200 => Episode.Parser.ParseFrom(response.Body.Span),
            _ => throw new InvalidOperationException()
        };
    }

    public async ValueTask<string> FetchBearer(CancellationToken ct = default)
    {
        if (_userId.IsNone)
            throw new InvalidOperationException("No user id set");
        var bearer = await FetchBearer(this, _userId.ValueUnsafe(), _bearerCache)
            .Run(_rt);
        return bearer.Match(
            Succ: r => r,
            Fail: ex => throw ex
        );
    }

    internal static Aff<RT, string> FetchBearer(IMercuryClient mercuryClient, string userId,
        Ref<HashMap<string, BearerToken>> cache)
    {
        var cacheMaybe = cache.Value.Find(userId).Bind(x => !x.Expired ? Some(x) : None);
        if (cacheMaybe.IsSome)
            return SuccessAff(cacheMaybe.ValueUnsafe().AccessToken);

        return
            from token in FetchBearerToken(mercuryClient)
            from newCache in Eff<HashMap<string, BearerToken>>(() => atomic(() => cache.Swap(f =>
            {
                var k = f.AddOrUpdate(userId, token);
                return k;
            })))
            select newCache.Find(userId).ValueUnsafe().AccessToken;
    }

    private static Aff<RT, BearerToken> FetchBearerToken(IMercuryClient mercuryClient)
    {
        const string keymasterurl = "hm://keymaster/token/authenticated?scope={0}&client_id={1}&device_id=";
        const string scopes =
            "app-remote-control,playlist-modify,playlist-modify-private,playlist-modify-public,playlist-read,playlist-read-collaborative,playlist-read-private,streaming,ugc-image-upload,user-follow-modify,user-follow-read,user-library-modify,user-library-read,user-modify,user-modify-playback-state,user-modify-private,user-personalized,user-read-birthdate,user-read-currently-playing,user-read-email,user-read-play-history,user-read-playback-position,user-read-playback-state,user-read-private,user-read-recently-played,user-top-read";
        const string clientId = SpotifyConstants.KEYMASTER_CLIENT_ID;
        var url = string.Format(keymasterurl, scopes, clientId);

        return
            from response in mercuryClient.Send(MercuryMethod.Get, url, None).ToAff()
            from bearerToken in Eff(() => BearerToken.ParseFrom(response.Body))
            select bearerToken;
    }

}