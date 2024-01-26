using System.Net.Http.Headers;
using System.Text.Json;
using Eum.Spotify.extendedmetadata;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using Spotify.Metadata;
using Wavee.Spfy.Exceptions;
using Wavee.Spfy.Items;
using Wavee.Spfy.Mapping;
using static LanguageExt.Prelude;

namespace Wavee.Spfy;

public sealed class WaveeSpotifyMetadataClient
{
    private readonly IGzipHttpClient _gzipHttpClient;
    private readonly IHttpClient _httpClient;
    private readonly Func<ValueTask<string>> _tokenFactory;
    private readonly ILogger _logger;
    private readonly Option<ICachingProvider> _cachingProvider;

    internal WaveeSpotifyMetadataClient(IGzipHttpClient gzipHttpClient,
        IHttpClient httpClient,
        Func<ValueTask<string>> tokenFactory,
        ILogger logger,
        Option<ICachingProvider> cachingProvider)
    {
        _gzipHttpClient = gzipHttpClient;
        _httpClient = httpClient;
        _tokenFactory = tokenFactory;
        _logger = logger;
        _cachingProvider = cachingProvider;
    }

    public async ValueTask<LocalTrackMetadata> PopulateLocalTrackWithSpotifyMetadata(
        string fileToPopulate,
        SpotifyId withEntity,
        CancellationToken cancellationToken = default)
    {
        var item = await GetItem(withEntity, false, cancellationToken);
        switch (item)
        {
            case SpotifySimpleTrack track:
                {
                    var fill = new LocalTrackMetadata(
                        filePath: fileToPopulate,
                        title: track.Name,
                        composers: track.Descriptions.Select(x => x.Name).ToArray(),
                        albumArtists: Option<string[]>.None,
                        album: track.Group.Name,
                        genres: Option<string[]>.None, // TODO
                        comment: BuildComment(track),
                        trackNumber: Some((int)track.TrackNumber),
                        year: track.Group.Year,
                        discNumber: Some((int)track.DiscNumber),
                        getImage: Option<Func<Option<string>>>.None,
                        getImageBytes: Some(() => DownloadImage(track.Group.Images, _httpClient)));

                    LocalMetadata.ReplaceMetadata(fileToPopulate, fill);
                    return fill;
                }
            case SpotifySimpleEpisode episode:
                {
                    throw new NotImplementedException();
                    break;
                }
            default:
                throw new SpotifyItemNotSupportedException(withEntity);
        }
    }

    public ValueTask<ISpotifyItem> GetItem(SpotifyId id,
        bool allowCache,
        CancellationToken cancellationToken = default)
    {
        return id.Type switch
        {
            AudioItemType.Track => FetchTrack(id, allowCache, cancellationToken),
            AudioItemType.Album => FetchAlbum(id, allowCache, cancellationToken),
            AudioItemType.Artist => FetchArtist(id, allowCache, cancellationToken),
            AudioItemType.PodcastEpisode => FetchPodcastEpisode(id, allowCache, cancellationToken),
            AudioItemType.PodcastShow => FetchPodcastShow(id, allowCache, cancellationToken),
            _ => throw new SpotifyItemNotSupportedException(id)
        };
    }

    internal ValueTask<ISpotifyItem> FetchTrack(SpotifyId id,
        bool allowCache,
        CancellationToken cancellationToken)
    {
        const string metadataKey = "track";
        var res = GetBasicMetadataItem(metadataKey, id,
            allowCache,
            x => Track.Parser.ParseFrom(x).MapToDto(), cancellationToken);
        return res;
    }

    private ValueTask<ISpotifyItem> FetchAlbum(SpotifyId id,
        bool allowCache,
        CancellationToken cancellationToken)
    {
        const string metadataKey = "album";
        var res = GetBasicMetadataItem(metadataKey, id,
            allowCache,
            x => Album.Parser.ParseFrom(x).MapToDto(), cancellationToken);
        return res;
    }

    private ValueTask<ISpotifyItem> FetchArtist(SpotifyId id,
        bool allowCache,
        CancellationToken cancellationToken)
    {
        const string metadataKey = "artist";
        var res = GetBasicMetadataItem(metadataKey, id,
            allowCache,
            x => Artist.Parser.ParseFrom(x).MapToDto(), cancellationToken);
        return res;
    }

    private ValueTask<ISpotifyItem> FetchPodcastEpisode(SpotifyId id,
        bool allowCache,
        CancellationToken cancellationToken)
    {
        const string metadataKey = "episode";
        var res = GetBasicMetadataItem(metadataKey, id,
            allowCache,
            x => Episode.Parser.ParseFrom(x).MapToDto(), cancellationToken);
        return res;
    }

    private ValueTask<ISpotifyItem> FetchPodcastShow(SpotifyId id,
        bool allowCache,
        CancellationToken cancellationToken)
    {
        const string metadataKey = "show";
        var res = GetBasicMetadataItem(metadataKey, id,
            allowCache,
            x => Show.Parser.ParseFrom(x).MapToDto(), cancellationToken);
        return res;
    }

    private ValueTask<ISpotifyItem> GetBasicMetadataItem<T>(string metadatakey,
        SpotifyId id,
        bool allowCache,
        Func<byte[], T> parser,
        CancellationToken cancellationToken) where T : ISpotifyItem
    {
        const string endpoint = "/metadata/4/{0}/{1}?market=from_token";
        var endpointWithId = string.Format(endpoint, metadatakey, id.ToBase16());
        var cacheKey = Constants.Caching.SimpleItem(id);

        if (allowCache && _cachingProvider.IsSome)
        {
            var cachingProviderValue = _cachingProvider.ValueUnsafe();
            if (cachingProviderValue.TryGet(cacheKey, out var cachedItem))
            {
                return new ValueTask<ISpotifyItem>(parser(cachedItem));
            }
        }

        return new ValueTask<ISpotifyItem>(GetBasicMetadataItemAsync(endpointWithId, parser, cacheKey,
            cancellationToken));
    }

    private async Task<ISpotifyItem> GetBasicMetadataItemAsync<T>(string endpointWithId,
        Func<byte[], T> parser,
        string cacheKey,
        CancellationToken cancellationToken) where T : ISpotifyItem
    {
        var accessToken = await _tokenFactory();
        var spclient = await ApResolve.GetSpClient(_httpClient);
        endpointWithId = $"https://{spclient}{endpointWithId}";

        using var resp = await _httpClient.Get(endpointWithId, accessToken, cancellationToken);
        var bytes = await resp.Content.ReadAsByteArrayAsync(cancellationToken);
        var item = parser(bytes);

        _cachingProvider.IfSome(x => x.Set(cacheKey, bytes));
        return item;
    }

    private static IReadOnlyDictionary<AudioItemType, ExtensionQuery> ExtensionQueries { get; } =
        new Dictionary<AudioItemType, ExtensionQuery>
        {
            {
                AudioItemType.Track, new ExtensionQuery
                {
                    ExtensionKind = ExtensionKind.TrackV4
                }
            },
            {
                AudioItemType.Album, new ExtensionQuery
                {
                    ExtensionKind = ExtensionKind.AlbumV4
                }
            },
            {
                AudioItemType.Artist, new ExtensionQuery
                {
                    ExtensionKind = ExtensionKind.ArtistV4
                }
            }
        };

    private static Option<string> BuildComment(SpotifySimpleTrack track)
    {
        // SpotifyArtist references
        // SpotifyAlbum references
        // { "artists": [artistIdx: spotifyUri..], "album": spotifyUri.. }
        var artistsArr = track.Descriptions.Select((x, i) => new
        {
            idx = i,
            uri = x.Uri.ToString()
        });
        var album = new
        {
            uri = track.Group.Uri.ToString()
        };

        var obj = new
        {
            spotify_artists = artistsArr,
            spotify_album = album
        };
        var json = JsonSerializer.Serialize(obj);
        return json;
    }

    private static Option<byte[]> DownloadImage(Seq<UrlImage> groupImages, IHttpClient httpClient)
    {
        var image = groupImages
            .OrderByDescending(x => x.CommonSize)
            .FirstOrDefault();
        if (image.Url == null) return None;

        var url = image.Url;
        var res = Task.Run(async () =>
        {
            using var resp = await httpClient.Get(url);
            var bytes = await resp.Content.ReadAsByteArrayAsync(CancellationToken.None);
            return bytes;
        }).Result;

        return Some(res);
    }

    public async Task FillBatched<TKey>(Dictionary<TKey, ISpotifyItem?> idOutput,
        string country,
        Func<TKey, SpotifyId> idFunc) where TKey : notnull
    {
        //https://gae2-spclient.spotify.com/extended-metadata/v0/extended-metadata
        var accessToken = await _tokenFactory();
        var spclient = await ApResolve.GetSpClient(_httpClient);
        var url = $"https://{spclient}/extended-metadata/v0/extended-metadata";

        var batchedEntityRequest = new BatchedEntityRequest
        {
            Header = new BatchedEntityRequestHeader
            {
                Catalogue = "premium",
                Country = country
            }
        };
        var reverseId = new Dictionary<string, TKey>();
        foreach (var item in idOutput)
        {
            var id = idFunc(item.Key);
            var req = new EntityRequest
            {
                EntityUri = id.ToString(),
                Query = { ExtensionQueries[id.Type] }
            };
            batchedEntityRequest.EntityRequest.Add(req);
            reverseId[req.EntityUri] = item.Key;
        }

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, url);
        httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/protobuf"));
        httpReq.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        httpReq.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        httpReq.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));


        httpReq.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));

        using var body = new ByteArrayContent(batchedEntityRequest.ToByteArray());
        body.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/protobuf");
        httpReq.Content = body;
        using var httpResponse = await _gzipHttpClient.SendAsync(httpReq, CancellationToken.None);
        await using var stream = await httpResponse.Content.ReadAsStreamAsync();
        var response = BatchedExtensionResponse.Parser.ParseFrom(stream);
        foreach (var group in response.ExtendedMetadata)
        {
            switch (group.ExtensionKind)
            {
                case ExtensionKind.ArtistV4:
                    {
                        foreach (var item in group.ExtensionData)
                        {
                            var id = reverseId[item.EntityUri];
                            var artist = Artist.Parser.ParseFrom(item.ExtensionData.Value).MapToDto();
                            idOutput[id] = artist;
                        }
                        break;
                    }
                case ExtensionKind.AlbumV4:
                    {
                        foreach (var item in group.ExtensionData)
                        {
                            var id = reverseId[item.EntityUri];
                            var artist = Album.Parser.ParseFrom(item.ExtensionData.Value).MapToDto();
                            idOutput[id] = artist;
                        }
                        break;
                    }
                case ExtensionKind.TrackV4:
                    {
                        foreach (var item in group.ExtensionData)
                        {
                            var id = reverseId[item.EntityUri];
                            var artist = Track.Parser.ParseFrom(item.ExtensionData.Value).MapToDto();
                            idOutput[id] = artist;
                        }
                        break;
                    }
                case ExtensionKind.ShowV4:
                    {
                        break;
                    }
                case ExtensionKind.EpisodeV4:
                    {
                        break;
                    }
            }
        }
    }
}