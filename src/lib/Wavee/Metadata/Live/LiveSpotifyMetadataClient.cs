using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Eum.Spotify;
using Eum.Spotify.canvaz;
using Eum.Spotify.extendedmetadata;
using Eum.Spotify.playlist4;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Spotify.Metadata;
using Wavee.Cache;
using Wavee.Id;
using Wavee.Infrastructure;
using Wavee.Infrastructure.Mercury;
using Wavee.Metadata.Album;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Common;
using Wavee.Metadata.Home;
using Wavee.Metadata.Me;
using Wavee.Sqlite.Repository;
using Wavee.Token.Live;

namespace Wavee.Metadata.Live;

internal readonly struct LiveSpotifyMetadataClient : ISpotifyMetadataClient
{
    private readonly ISpotifyCache _cache;
    private readonly Func<IGraphQLQuery, CultureInfo, Task<HttpResponseMessage>> _query;
    private readonly Func<IMercuryClient> _mercuryFactory;
    private readonly Func<CancellationToken, ValueTask<string>> _tokenFactory;
    private readonly ValueTask<string> _country;
    private readonly CultureInfo _defaultLang;
    private readonly string _userId;

    public LiveSpotifyMetadataClient(Func<IMercuryClient> mercuryFactory, Task<string> country,
        Func<IGraphQLQuery, CultureInfo, Task<HttpResponseMessage>> query, ISpotifyCache cache,
        Func<CancellationToken, ValueTask<string>> tokenFactory, CultureInfo defaultLang, string userId)
    {
        _mercuryFactory = mercuryFactory;
        _query = query;
        _cache = cache;
        _tokenFactory = tokenFactory;
        _defaultLang = defaultLang;
        _userId = userId;
        _country = new ValueTask<string>(country);
    }

    public async Task<Track> GetTrack(SpotifyId id, CancellationToken cancellationToken = default)
    {
        var cache = _cache;
        var track = await cache.GetTrack(id);
        if (track.IsSome)
        {
            return track.ValueUnsafe();
        }


        const string query = "hm://metadata/4/track/{0}?country={1}";
        var finalUri = string.Format(query, id.ToBase16(), await _country);
        var mercury = _mercuryFactory();
        var response = await mercury.Get(finalUri, cancellationToken);
        if (response.Header.StatusCode == 200)
        {
            var result = Track.Parser.ParseFrom(response.Payload.Span);
            await cache.SetTrack(id, result);
            return result;
        }

        throw new MercuryException(response);
    }

    public async Task<SpotifyHomeGroupSection> GetRecentlyPlayed(Option<AudioItemType> typeFilterType,
        CancellationToken cancellationToken = default)
    {
        //spclient -> 	/recently-played/v3/user/7ucghdgquf6byqusqkliltwc2/recently-played
        var userId = _userId;
        const string url = "hm://recently-played/v2/user/{0}/recently-played?format=json&limit=10";
        var mercury = _mercuryFactory();
        var response = await mercury.Get(string.Format(url, userId), cancellationToken);
        using var jsonDocument = JsonDocument.Parse(response.Payload);
        var contexts = jsonDocument.RootElement.GetProperty("playContexts");
        var output = new string[contexts.GetArrayLength()];
        int i = 0;
        using var arr = contexts.EnumerateArray();

        //regex for spotify:user:spotify:playlist:37i9dQZF1E8H2iOSY4VSjq , which shouldbe just spotify:playlist:...
        //so spotify:{userId}:playlist:{playlistId}
        var playlistRegex = new Regex(@"spotify:user:(?<userId>.+):playlist:(?<playlistId>.+)");
        //regex for spotify:user:7ucghdgquf6byqusqkliltwc2:collection
        //so spotify:{userId}:collection
        var collectionRegex = new Regex(@"spotify:user:(?<userId>.+):collection");

        while (arr.MoveNext())
        {
            var current = arr.Current;
            var uri = current.GetProperty("uri").GetString();

            //check if we match the playlist regex first
            var playlistMatch = playlistRegex.Match(uri);
            if (playlistMatch.Success)
            {
                _ = playlistMatch.Groups["userId"].Value;
                var playlistId = playlistMatch.Groups["playlistId"].Value;
                uri = $"spotify:playlist:{playlistId}";
            }
            else
            {
                var collectionMatch = collectionRegex.Match(uri);
                if (collectionMatch.Success)
                {
                    //anonymized
                    uri = "spotify:user:anonymized:collection";
                }
            }

            output[i++] = uri;
        }

        using var entities = await _query(new FetchRecentlyPlayedQuery(output), _defaultLang);
        using var stream = await entities.Content.ReadAsStreamAsync();
        using var entitiesJson = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var entitiesArr = entitiesJson.RootElement.GetProperty("data").GetProperty("lookup");
        var itemsOutput = new ISpotifyHomeItem[entitiesArr.GetArrayLength()];

        int j = -1;
        using var itemsArr = entitiesArr.EnumerateArray();
        while (itemsArr.MoveNext())
        {
            j++;
            var rootItem = itemsArr.Current;
            var typeName = rootItem.GetProperty("__typename").GetString();
            if (typeName is "UnknownTypeWrapper")
            {
                var uri = rootItem.GetProperty("_uri").GetString();
                if (uri is "spotify:user:anonymized:collection")
                {
                    itemsOutput[j] = new SpotifyCollectionItem();
                    continue;
                }

                continue;
            }

            var homeitem = SpotifyItemParser.ParseFrom(rootItem.GetProperty("data"));
            if (homeitem.IsSome)
            {
                itemsOutput[j] = homeitem.ValueUnsafe();
            }
        }

        return new SpotifyHomeGroupSection
        {
            Title = null,
            SectionId = default,
            TotalCount = (uint)output.Length,
            Items = itemsOutput.Where(x =>
                x is not null && (!typeFilterType.IsSome || typeFilterType.ValueUnsafe().HasFlag(x.Id.Type))),
        };
    }

    public async Task<SpotifyHomeView> GetHomeView(Option<AudioItemType> typeFilterType, TimeZoneInfo timezone,
        Option<CultureInfo> languageOverride, CancellationToken cancellationToken = default)
    {
        var query = new HomeQuery(timezone);
        var recentlyPlayedTask = GetRecentlyPlayed(typeFilterType, cancellationToken);
        var responseTask = _query(query, languageOverride.IfNone(_defaultLang));
        await Task.WhenAll(recentlyPlayedTask, responseTask);
        if (responseTask.Result.IsSuccessStatusCode)
        {
            ReadOnlyMemory<byte> stream = await responseTask.Result.Content.ReadAsByteArrayAsync();
            var home = SpotifyHomeView.ParseFrom(stream, recentlyPlayedTask.Result, typeFilterType);
            responseTask.Result.Dispose();
            return home;
        }

        throw new MercuryException(new MercuryResponse(
            Header: new Header
            {
                StatusCode = (int)responseTask.Result.StatusCode
            }, ReadOnlyMemory<byte>.Empty
        ));
    }

    public Task<ArtistOverview> GetArtistOverview(SpotifyId artistId, bool destroyCache,
        Option<CultureInfo> languageOverride,
        bool includePrerelease,
        CancellationToken ct = default)
    {
        LiveSpotifyMetadataClient tmpThis = this;
        var result = tmpThis._cache
            .GetRawEntity(artistId.ToString())
            .BindAsync(f => destroyCache ? Option<ReadOnlyMemory<byte>>.None :
                (f.Length > 0 ? Option<ReadOnlyMemory<byte>>.Some(f) : Option<ReadOnlyMemory<byte>>.None))
            .Match(
                Some: data =>
                {
                    var res = new ValueTask<ArtistOverview>(ArtistOverview.ParseFrom(data));
                    return res;
                },
                None: () =>
                {
                    static async Task<ArtistOverview> Fetch(SpotifyId artistid, LiveSpotifyMetadataClient tmpthis,
                        Option<CultureInfo> languageoverride, bool preRelease)
                    {
                        var query = new QueryArtistOverviewQuery(artistid, preRelease);
                        var response = await tmpthis._query(query, languageoverride.IfNone(tmpthis._defaultLang));
                        if (response.IsSuccessStatusCode)
                        {
                            var stream = await response.Content.ReadAsByteArrayAsync();
                            var artistOverview = ArtistOverview.ParseFrom(stream);
                            await tmpthis._cache.SaveRawEntity(artistid.ToString(), stream,
                                DateTimeOffset.Now.AddMinutes(30));
                            return artistOverview;
                        }

                        throw new MercuryException(new MercuryResponse(
                            Header: new Header
                            {
                                StatusCode = (int)response.StatusCode
                            }, ReadOnlyMemory<byte>.Empty
                        ));
                    }

                    return new ValueTask<ArtistOverview>(Fetch(artistId, tmpThis, languageOverride, includePrerelease));
                }
            )
            .MapAsync(async x => await x);

        return result;
    }

    public async ValueTask<IArtistDiscographyRelease[]> GetArtistDiscography(SpotifyId artistId, ReleaseType type,
        int offset,
        int limit,
        CancellationToken ct = default)
    {
        //GET
        //https://api-partner.spotify.com/pathfinder/v1/query?operationName=queryArtistDiscographyAlbums&variables={"uri":"spotify:artist:4XQhU3S4TyPkiPIsSu2hmA","offset":0,"limit":9}&extensions={"persistedQuery":{"version":1,"sha256Hash":"983072ae655f5de212747a41be0d7c4cc49559aa9fe8e32836369b6f130cac33"}}
        const string albumQuery = "queryArtistDiscographyAlbums";
        const string singleQuery = "queryArtistDiscographySingles";
        const string compilationQuery = "queryArtistDiscographyCompilations";

        const string albumQueryHash = "983072ae655f5de212747a41be0d7c4cc49559aa9fe8e32836369b6f130cac33";
        const string singleQueryHash = "e02547d028482cec098d8d31899afcf488026d5dbdc2fcb973f05657c9cd6797";
        const string compilationQueryHash = "6702dd8b0d793fdb981e1ce508bce717e06e81b60d2a6fb7c1b79843ca55e901";

        //const string url = "https://api-partner.spotify.com/pathfinder/v1/query?operationName={0}&variables={{\"uri\":\"{1}\",\"offset\":{2},\"limit\":{3}}}&extensions={{\"persistedQuery\":{{\"version\":1,\"sha256Hash\":\"{4}\"}}}}";
        var query = type switch
        {
            ReleaseType.Album => new AlbumDiscographyQuery(albumQuery, artistId, offset, limit, albumQueryHash),
            ReleaseType.Single => new AlbumDiscographyQuery(singleQuery, artistId, offset, limit, singleQueryHash),
            ReleaseType.Compilation => new AlbumDiscographyQuery(compilationQuery, artistId, offset, limit,
                compilationQueryHash),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        using var response = await _query(query, _defaultLang);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream, default, ct);
        var data = json.RootElement.GetProperty("data").GetProperty("artistUnion").GetProperty("discography");
        var nextKey = type switch
        {
            ReleaseType.Album => data.GetProperty("albums"),
            ReleaseType.Single => data.GetProperty("singles"),
            ReleaseType.Compilation => data.GetProperty("compilations"),
        };
        var items = nextKey.GetProperty("items");
        var output = new IArtistDiscographyRelease[items.GetArrayLength()];
        using var array = items.EnumerateArray();
        var i = 0;
        while (array.MoveNext())
        {
            var item = array.Current;
            //Nested releases, just get the first one
            var firstRelease = item.GetProperty("releases").GetProperty("items").EnumerateArray().First();
            var parsed = ArtistOverview.ParseRelease(firstRelease);
            output[i++] = parsed;
        }

        return output;
    }

    public async Task<MeUser> GetMe(CancellationToken ct = default)
    {
        const string endpoint = "https://api.spotify.com/v1/me";
        var token = await _tokenFactory(ct);
        var header = new AuthenticationHeaderValue("Bearer", token);
        using var response = await HttpIO.Get(endpoint, new Dictionary<string, string>(), header, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream, default, ct);
        var id = json.RootElement.GetProperty("id").GetString()!;
        var displayName = json.RootElement.GetProperty("display_name");
        CoverImage[] images = Array.Empty<CoverImage>();
        if (json.RootElement.TryGetProperty("images", out var imgs) && imgs.ValueKind is not JsonValueKind.Null)
        {
            images = new CoverImage[imgs.GetArrayLength()];
            using var array = imgs.EnumerateArray();
            int i = 0;
            while (array.MoveNext())
            {
                var img = array.Current;
                var url = img.GetProperty("url").GetString()!;
                var potentialWidth = img.TryGetProperty("width", out var wd)
                                     && wd.ValueKind is JsonValueKind.Number
                    ? wd.GetUInt16()
                    : Option<ushort>.None;
                var potentialHeight = img.TryGetProperty("height", out var ht)
                                      && ht.ValueKind is JsonValueKind.Number
                    ? ht.GetUInt16()
                    : Option<ushort>.None;
                images[i++] = new CoverImage(
                    Url: url,
                    Width: potentialWidth,
                    Height: potentialHeight
                );
            }
        }

        return new MeUser
        {
            DisplayName = displayName.ValueKind is JsonValueKind.Null ? id : displayName.GetString()!,
            Images = images
        };
    }

    public async Task<SpotifyAlbum> GetAlbum(SpotifyId id, CancellationToken ct = default)
    {
        var query = new QueryAlbum(id, 0, 300);
        using var response = await _query(query, _defaultLang);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream, default, ct);
        var root = json.RootElement.GetProperty("data").GetProperty("albumUnion");

        return SpotifyAlbum.ParseFrom(root);
    }

    public Task<SpotifyAlbumDisc[]> GetAlbumTracks(SpotifyId id, CancellationToken ct = default)
    {
        static SpotifyAlbumDisc[] Parse(ReadOnlyMemory<byte> data)
        {
            using var json = JsonDocument.Parse(data);
            var root = json.RootElement.GetProperty("data").GetProperty("albumUnion").GetProperty("tracks");
            var totalCount = root.GetProperty("totalCount").GetInt32();
            var output = new SpotifyAlbumTrack[totalCount];
            var items = root.GetProperty("items");
            using var array = items.EnumerateArray();
            int i = 0;
            while (array.MoveNext())
            {
                var item = array.Current;
                var parsed = SpotifyAlbum.ParseTrack(item);
                output[i++] = parsed;
            }

            return output.GroupBy(x => x.DiscNumber)
                .Select(f => new SpotifyAlbumDisc(
                    number: f.Key,
                    tracks: f.ToArray()
                )).ToArray();
        }

        var key = $"{id.ToString()}-tracks";
        LiveSpotifyMetadataClient tmpThis = this;
        var result = tmpThis._cache
            .GetRawEntity(key)
            .Match(
                Some: data =>
                {
                    var res = new ValueTask<SpotifyAlbumDisc[]>(Parse(data));
                    return res;
                },
                None: () =>
                {
                    static async Task<SpotifyAlbumDisc[]> Fetch(SpotifyId albumId, LiveSpotifyMetadataClient tmpthis,
                        string key, CancellationToken ct)
                    {
                        var query = new QueryAlbumTracks(id: albumId);
                        var response = await tmpthis._query(query, tmpthis._defaultLang);
                        response.EnsureSuccessStatusCode();
                        var stream = await response.Content.ReadAsByteArrayAsync(ct);
                        if (response.IsSuccessStatusCode)
                        {
                            var data = Parse(stream);
                            await tmpthis._cache.SaveRawEntity(key, stream, DateTimeOffset.Now.AddDays(1));
                            return data;
                        }

                        throw new MercuryException(new MercuryResponse(
                            Header: new Header
                            {
                                StatusCode = (int)response.StatusCode
                            }, ReadOnlyMemory<byte>.Empty
                        ));
                    }

                    return new ValueTask<SpotifyAlbumDisc[]>(Fetch(id, tmpThis, key, ct));
                }
            )
            .MapAsync(async x => await x);

        return result;
    }

    public Task<LyricsLine[]> GetLyrics(SpotifyId trackId, CancellationToken ct = default)
    {
        //https://spclient.wg.spotify.com/color-lyrics/v2/track/0mvkwaZMP2gAy2ApQLtZRv/image/https%3A%2F%2Fi.scdn.co%2Fimage%2Fab67616d0000b273051ae642ad4a0c1329b41d99?format=json&vocalRemoval=false&market=from_token
        const string spclient = "gae2-spclient.spotify.com:443";
        const string url = "/color-lyrics/v2/track/{0}?format=json&vocalRemoval=false&market=from_token";
        var key = $"{trackId}-lyrics";
        LiveSpotifyMetadataClient tmpThis = this;
        var result = tmpThis._cache
            .GetRawEntity(key)
            .Match(
                Some: data =>
                {
                    var res = new ValueTask<LyricsLine[]>(Parse(data));
                    return res;
                },
                None: () =>
                {
                    static async Task<LyricsLine[]> Fetch(SpotifyId trackId,
                        LiveSpotifyMetadataClient tmpthis, string key, CancellationToken ct)
                    {
                        var token = await tmpthis._tokenFactory(ct);
                        var tokenHeader = new AuthenticationHeaderValue("Bearer", token);
                        var finalUrl = $"https://{spclient}{string.Format(url, trackId.ToBase62())}";

                        var header = new Dictionary<string, string> { { "App-Platform", "WebPlayer" } };

                        var response = await HttpIO.Get(finalUrl, header, tokenHeader, ct);
                        response.EnsureSuccessStatusCode();
                        var stream = await response.Content.ReadAsByteArrayAsync();
                        if (response.IsSuccessStatusCode)
                        {
                            var data = Parse(stream);
                            await tmpthis._cache.SaveRawEntity(key, stream, DateTimeOffset.Now.AddDays(1));
                            return data;
                        }

                        throw new MercuryException(new MercuryResponse(
                            Header: new Header
                            {
                                StatusCode = (int)response.StatusCode
                            }, ReadOnlyMemory<byte>.Empty
                        ));
                    }

                    return new ValueTask<LyricsLine[]>(Fetch(trackId, tmpThis, key, ct));
                }
            )
            .MapAsync(async x => await x);
        return result;
    }

    public async Task<SelectedListContent?> GetPlaylist(SpotifyId spotifyId, CancellationToken ct = default)
    {
        const string spclient = "gae2-spclient.spotify.com:443";
        const string url = "/playlist/v2/playlist/{0}";
        var finalUrl = string.Format($"https://{spclient}{url}", spotifyId.ToBase62());

        var token = await _tokenFactory(ct);
        var tokenHeader = new AuthenticationHeaderValue("Bearer", token);
        var response = await HttpIO.Get(finalUrl, new Dictionary<string, string>(), tokenHeader, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        if (response.IsSuccessStatusCode)
        {
            return SelectedListContent.Parser.ParseFrom(stream);
        }

        return null;
    }

    public async Task<Dictionary<string, Option<Either<TrackWithExpiration, EpisodeWithExpiration>>>> GetExtendedMetadataForItems(IGrouping<AudioItemType, string>[] items, CancellationToken ct)
    {
        const string spclient = "gae2-spclient.spotify.com:443";
        const string url = "/extended-metadata/v0/extended-metadata";

        var finalUrl = $"https://{spclient}{url}";
        var token = await _tokenFactory(ct);
        var tokenHeader = new AuthenticationHeaderValue("Bearer", token);
        var request = new BatchedEntityRequest();
        foreach (var group in items)
        {
            switch (group.Key)
            {
                case AudioItemType.Track:
                    request.EntityRequest.AddRange(group.Select(a => new EntityRequest
                    {
                        EntityUri = a,
                        Query =
                        {
                            new ExtensionQuery
                            {
                                ExtensionKind = ExtensionKind.TrackV4
                            }
                        }
                    }));
                    break;
                case AudioItemType.PodcastEpisode:
                    request.EntityRequest.AddRange(group.Select(a => new EntityRequest
                    {
                        EntityUri = a,
                        Query =
                        {
                            new ExtensionQuery
                            {
                                ExtensionKind = ExtensionKind.EpisodeV4
                            }
                        }
                    }));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        request.Header = new BatchedEntityRequestHeader
        {
            Catalogue = "premium",
            Country = _country.Result
        };

        using var stream = new MemoryStream();
        request.WriteTo(stream);
        stream.Position = 0;
        using var streamContent = new StreamContent(stream);
        using var response = await HttpIO.Post(finalUrl, new Dictionary<string, string>(), streamContent, tokenHeader, ct);
        response.EnsureSuccessStatusCode();
        await using var responseStream = await response.Content.ReadAsStreamAsync(ct);
        var data = BatchedExtensionResponse.Parser.ParseFrom(responseStream);
        var result = new Dictionary<string, Option<Either<TrackWithExpiration, EpisodeWithExpiration>>>();
        foreach (var metadata in data.ExtendedMetadata)
        {
            switch (metadata.ExtensionKind)
            {
                case ExtensionKind.EpisodeV4:
                    {
                        foreach (var item in metadata.ExtensionData)
                        {
                            if (item.Header.StatusCode is 200)
                            {
                                var episode = Episode.Parser.ParseFrom(item.ExtensionData.Value);
                                result[item.EntityUri] =
                                    Option<Either<TrackWithExpiration, EpisodeWithExpiration>>.Some(
                                        Either<TrackWithExpiration, EpisodeWithExpiration>.Right(
                                            new EpisodeWithExpiration(
                                                Track: episode,
                                                Expiration: DateTimeOffset.UtcNow.AddSeconds(item.Header
                                                    .OfflineTtlInSeconds)
                                            )));
                            }
                            else
                            {
                                result[item.EntityUri] =
                                    Option<Either<TrackWithExpiration, EpisodeWithExpiration>>.None;
                            }
                        }

                        break;
                    }
                case ExtensionKind.TrackV4:
                    {
                        foreach (var item in metadata.ExtensionData)
                        {
                            if (item.Header.StatusCode is 200)
                            {
                                var track = Track.Parser.ParseFrom(item.ExtensionData.Value);
                                //result.Add(Option<Either<Track, Episode>>.Some(Either<Track, Episode>.Left(track)));
                                result[item.EntityUri] = Option<Either<TrackWithExpiration, EpisodeWithExpiration>>.Some(Either<TrackWithExpiration, EpisodeWithExpiration>.Left(new TrackWithExpiration(
                                    Track: track,
                                    Expiration: DateTimeOffset.UtcNow.AddSeconds(item.Header.OfflineTtlInSeconds)
                                    )));
                            }
                            else
                            {
                                result[item.EntityUri] = Option<Either<TrackWithExpiration, EpisodeWithExpiration>>.None;
                                //   result.Add(Option<Either<Track, Episode>>.None);
                            }
                        }
                        break;
                    }
            }
        }

        return result;
    }

    private static LyricsLine[] Parse(ReadOnlyMemory<byte> data)
    {
        using var json = JsonDocument.Parse(data);
        var lyrics = json.RootElement.GetProperty("lyrics");
        var syncType = lyrics.GetProperty("syncType").GetString()!;
        if (syncType is not "LINE_SYNCED")
            return Array.Empty<LyricsLine>();

        var lines = lyrics.GetProperty("lines");
        var output = new LyricsLine[lines.GetArrayLength()];
        using var array = lines.EnumerateArray();
        int i = 0;

        while (array.MoveNext())
        {
            var item = array.Current;
            var parsed = new LyricsLine(
                Words: item.GetProperty("words").GetString()!,
                StartTimeMs: double.Parse(item.GetProperty("startTimeMs").GetString()!)
            );
            output[i++] = parsed;
        }

        return output;
    }
}