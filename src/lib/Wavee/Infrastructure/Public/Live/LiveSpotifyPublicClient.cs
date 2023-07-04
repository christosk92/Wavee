using System.Net.Http.Headers;
using System.Text.Json;
using LanguageExt;
using Wavee.Id;
using Wavee.Infrastructure.Public.Response;

namespace Wavee.Infrastructure.Public.Live;

internal readonly struct LiveSpotifyPublicClient : ISpotifyPublicClient
{
    private readonly Func<CancellationToken, ValueTask<string>> _tokenFactory;

    public LiveSpotifyPublicClient(Func<CancellationToken, ValueTask<string>> tokenFactory)
    {
        _tokenFactory = tokenFactory;
    }

    public Task<PagedResponse<SpotifyPublicTrack>> GetMyTracks(int offset, int limit, CancellationToken ct = default)
    {
        const string url = "https://api.spotify.com/v1/me/tracks?offset={0}&limit={1}&market=from_token";
        var finalUrl = string.Format(url, offset, limit);
        return Get(finalUrl, ct)
            .MapAsync(response => DeserializePagedResponse(response, ParseCollectionTrack, ct));
    }

    public Task<PagedResponse<SpotifyPublicTrack>> GetAlbumTracks(SpotifyId albumId, int offset, int limit,
        CancellationToken ct = default)
    {
        const string url = "https://api.spotify.com/v1/albums/{0}/tracks?offset={1}&limit={2}&market=from_token";
        var finalUrl = string.Format(url, albumId.ToBase62(), offset, limit);

        return Get(finalUrl, ct)
            .MapAsync(response => DeserializePagedResponse(response, ParsePublicTrack, ct));
    }


    public Task<PagedResponse<ISpotifyPlaylistItem>> GetPlaylistTracks(SpotifyId spotifyId, int offset, int limit, Option<AudioItemType[]> types, CancellationToken ct = default)
    {
        const string url = "https://api.spotify.com/v1/playlists/{0}/tracks?offset={1}&limit={2}&fields=total,limit,next,offset,items(added_at,track(uri,preview_url))&additional_types={3}&market=from_token";
        //items(added_by.id,track(name,href,album(name,href)))
        var additionalTypes = types.Map(f => string.Join(",", f.Select(y => y switch
        {
            AudioItemType.PodcastEpisode => "episode",
            AudioItemType.Track => "track",
            _ => throw new ArgumentOutOfRangeException(nameof(y), y, null)
        }))).IfNone("track,episode");
        var finalUrl = string.Format(url, spotifyId.ToBase62(), offset, limit, additionalTypes);

        return Get(finalUrl, ct)
            .MapAsync(response =>
            {
                return DeserializePagedResponse(response, static elem =>
                {
                    var track = elem.GetProperty("track");
                    var uri = SpotifyId.FromUri(track.GetProperty("uri").GetString().AsSpan());
                    return uri.Type switch
                    {
                        AudioItemType.Track => ParsePublicPlaylistTrack(elem, track),
                        AudioItemType.PodcastEpisode => ParsePublicPlaylistPodcastEpisode(elem, track),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }, ct);
            });
    }

    public Task<SpotifyPublicTrack> GetTrack(SpotifyId spotifyId, CancellationToken ct = default)
    {
        const string url = "https://api.spotify.com/v1/tracks/{0}?market=from_token";
        var finalUrl = string.Format(url, spotifyId.ToBase62());
        return Get(finalUrl, ct)
            .MapAsync(async response =>
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var jsondoc = await JsonDocument.ParseAsync(stream, default, ct);
                return ParsePublicTrack(jsondoc.RootElement);
            });
    }

    public Task<IReadOnlyCollection<SpotifyPublicTrack>> GetArtistTopTracks(SpotifyId spotifyId, string us,
        CancellationToken ct = default)
    {
        const string url = "https://api.spotify.com/v1/artists/{0}/top-tracks?market={1}";
        var finalUrl = string.Format(url, spotifyId.ToBase62(), us);
        return Get(finalUrl, ct)
            .MapAsync(async response =>
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var jsondoc = await JsonDocument.ParseAsync(stream, default, ct);
                var tracks = jsondoc.RootElement.GetProperty("tracks");
                var tracksOutput = new SpotifyPublicTrack[tracks.GetArrayLength()];
                int i = 0;
                using var arr = tracks.EnumerateArray();
                while (arr.MoveNext())
                {
                    tracksOutput[i++] = ParsePublicTrack(arr.Current);
                }

                return (IReadOnlyCollection<SpotifyPublicTrack>)tracksOutput;
            });
    }

    private async Task<HttpResponseMessage> Get(string finalUrl, CancellationToken ct)
    {
        var token = await _tokenFactory(ct);
        var tokenHeader = new AuthenticationHeaderValue("Bearer", token);
        return await HttpIO.Get(finalUrl, new Dictionary<string, string>(0), tokenHeader, ct);
    }
    private static ISpotifyPlaylistItem ParsePublicPlaylistPodcastEpisode(JsonElement root, JsonElement track)
    {
        var addedAt = root.GetProperty("added_at");
        Option<DateTimeOffset> addedAtVal = Option<DateTimeOffset>.None;
        if (addedAt.ValueKind is not JsonValueKind.Null)
        {
            addedAtVal = DateTimeOffset.Parse(addedAt.GetString()!);
        }

        var publicTrack = ParsePublicTrack(track);
        return new SpotifyTrackPlaylistItem
        {
            Track = publicTrack,
            AddedAt = addedAtVal,
            Id = publicTrack.TrackUri
        };
    }

    private static ISpotifyPlaylistItem ParsePublicPlaylistTrack(JsonElement root, JsonElement track)
    {
        var addedAt = root.GetProperty("added_at");
        Option<DateTimeOffset> addedAtVal = Option<DateTimeOffset>.None;
        if (addedAt.ValueKind is not JsonValueKind.Null)
        {
            addedAtVal = DateTimeOffset.Parse(addedAt.GetString()!);
        }

        var publicTrack = ParsePublicEpisode(track);
        return new SpotifyEpisodePlaylistItem()
        {
            Episode = publicTrack,
            AddedAt = addedAtVal,
            Id = publicTrack.TrackUri
        };
    }

    private static SpotifyPublicEpisode ParsePublicEpisode(JsonElement elem)
    {
        var previewUrl = elem.GetProperty("preview_url").GetString()!;
        var trackUri = SpotifyId.FromUri(elem.GetProperty("uri").GetString().AsSpan());

        return new SpotifyPublicEpisode
        {
            PreviewUrl = previewUrl,
            TrackUri = trackUri
        };
    }

    private static SpotifyPublicTrack ParseCollectionTrack(JsonElement root)
    {
        var track = root.GetProperty("track");
        return ParsePublicTrack(track);
    }
    private static SpotifyPublicTrack ParsePublicTrack(JsonElement elem)
    {
        var previewUrl = elem.GetProperty("preview_url").GetString()!;
        var trackUri = SpotifyId.FromUri(elem.GetProperty("uri").GetString().AsSpan());

        return new SpotifyPublicTrack
        {
            PreviewUrl = previewUrl,
            TrackUri = trackUri
        };
    }

    private static async Task<PagedResponse<T>> DeserializePagedResponse<T>(HttpResponseMessage response, Func<JsonElement, T> mapper, CancellationToken ct)
    {
        using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream, default, ct);
        var limit = json.RootElement.GetProperty("limit").GetInt32();
        var offset = json.RootElement.GetProperty("offset").GetInt32();
        var total = json.RootElement.GetProperty("total").GetInt32();
        //nexturl may be NULL
        var nextUrl = json.RootElement.GetProperty("next");
        var hasNextPage = !nextUrl.ValueKind.Equals(JsonValueKind.Null);

        var items = json.RootElement.GetProperty("items");
        var itemsOutput = new T[items.GetArrayLength()];
        using var arr = items.EnumerateArray();
        int i = 0;
        while (arr.MoveNext())
        {
            var curr = arr.Current;
            var itemOutput = mapper(curr);
            itemsOutput[i] = itemOutput;
            i++;
        }


        return new PagedResponse<T>
        {
            Items = itemsOutput,
            Total = total,
            Offset = offset,
            Limit = limit,
            HasNextPage = hasNextPage
        };
    }
}