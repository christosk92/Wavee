using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using Eum.Spotify.extendedmetadata;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Logging;
using Spotify.ContextTrackColor;
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
            uri = x.Id.ToString()
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

    public async Task<IReadOnlyDictionary<string, (string Dark, string Light)>> FetchExtractedColors(Seq<string> urls)
    {
        const string operationName = "fetchExtractedColors";
        const string operationHash = "d7696dd106f3c84a1f3ca37225a1de292e66a2d5aced37a66632585eeb3bbbfa";
        var variables = new Dictionary<string, object>
        {
            ["uris"] = urls.ToArray()
        };
        var token = await _tokenFactory();
        using var response = await _httpClient.GetGraphQL(token, operationName, operationHash, variables);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var jsondoc = await JsonDocument.ParseAsync(stream);
        var colors = jsondoc.RootElement.GetProperty("data").GetProperty("extractedColors");
        var output = new Dictionary<string, (string Dark, string Light)>();
        using var enumerator = colors.EnumerateArray();
        int idx = 0;
        while (enumerator.MoveNext())
        {
            var curr = enumerator.Current;
            var colorDark = curr.GetProperty("colorDark").GetProperty("hex").GetString();
            var colorLight = curr.GetProperty("colorLight").GetProperty("hex").GetString();
            var url = urls.At(idx).ValueUnsafe();
            output[url] = (colorDark, colorLight);
            idx++;
        }

        return output;
    }

    public async Task<IReadOnlyCollection<LyricsLine>> GetLyricsFor(SpotifyId id)
    {
        if (id.Type is not AudioItemType.Track) throw new NotSupportedException("Only tracks have lyrics !");
        var accessToken = await _tokenFactory();
        var spclient = await ApResolve.GetSpClient(_httpClient);
        var finalEndpoint = $"https://{spclient}/color-lyrics/v2/track/{id.ToBase62()}?format=json&vocalRemoval=false&market=from_token";
        using var request = new HttpRequestMessage(HttpMethod.Get, finalEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("App-Platform", "WebPlayer");
        using var resp = await _httpClient.Send(request, CancellationToken.None);
        await using var stream = await resp.Content.ReadAsStreamAsync(CancellationToken.None);
        using var jsondoc = await JsonDocument.ParseAsync(stream);
        using var lines = jsondoc.RootElement.GetProperty("lyrics").GetProperty("lines").EnumerateArray();
        var output = new List<LyricsLine>();
        while (lines.MoveNext())
        {
            var curr = lines.Current;
            var startTime = TimeSpan.FromMilliseconds(long.Parse(curr.GetProperty("startTimeMs").GetString()!));
            var words = curr.GetProperty("words").GetString()!;

            output.Add(new LyricsLine(startTime, words));
        }
        return output;
    }

    public async Task<SpotifyFullAlbum> GetAlbum(SpotifyId fromUri)
    {
        const string operationName = "getAlbum";
        const string operationHash = "01c6295923a9603d5a97eb945fc7e54d6fb5129ea801b54321647abe0d423c25";
        var variables = new Dictionary<string, object>
        {
            ["uri"] = fromUri.ToString(),
            ["locale"] = string.Empty,
            ["offset"] = 0,
            ["limit"] = 300
        };
        var token = await _tokenFactory();
        using var response = await _httpClient.GetGraphQL(token, operationName, operationHash, variables);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var jsondoc = await JsonDocument.ParseAsync(stream);

        var album = jsondoc.RootElement.GetProperty("data").GetProperty("albumUnion");



        var name = album.GetProperty("name").GetString();
        var label = album.GetProperty("label").GetString();
        var releaseDate = DateTime.Parse(album.GetProperty("date").GetProperty("isoString").GetString()!);
        var type = album.GetProperty("type").GetString()!;

        Seq<UrlImage> images = LanguageExt.Seq<UrlImage>.Empty;
        using var imagesEnumerator = album.GetProperty("coverArt").GetProperty("sources").EnumerateArray();
        foreach (var imageElement in imagesEnumerator)
        {
            images = images.Add(new UrlImage
            {
                Url = imageElement.GetProperty("url").GetString(),
                Width = imageElement.GetProperty("width").GetUInt32(),
                Height = imageElement.GetProperty("height").GetUInt32(),
                CommonSize = UrlImageSizeType.Default
            });
            // album.CoverArtImages.Add(new Image
            // {
            //     Url = imageElement.GetProperty("url").GetString(),
            //     Width = imageElement.GetProperty("width").GetInt32(),
            //     Height = imageElement.GetProperty("height").GetInt32()
            // });
        }


        Seq<IWaveeAlbumArtist> artists = LanguageExt.Seq<IWaveeAlbumArtist>.Empty;
        using var artistsItems = album.GetProperty("artists").GetProperty("items").EnumerateArray();
        foreach (var artist in artistsItems)
        {
            artists = artists.Add(new WaveePlayableItemDescription
            {
                Id = artist.GetProperty("id").GetString()!,
                Name = artist.GetProperty("profile").GetProperty("name").GetString()!
            });
        }

        Seq<IWaveeTrackAlbum> tracks = LanguageExt.Seq<IWaveeTrackAlbum>.Empty;
        using var tracksItems = album.GetProperty("tracks").GetProperty("items").EnumerateArray();
        foreach (var trackItem in tracksItems)
        {
            var track = trackItem.GetProperty("track");
            tracks = tracks.Add(new SpotifyTrackAlbum
            {
                Uri = SpotifyId.FromUri(track.GetProperty("uri")
                    .GetString()),
                Name = track.GetProperty("name")
                    .GetString()!,
                Images = images,
                Artists = default,
                Year = releaseDate.Year,
                Playcount = long.Parse(track.GetProperty("playcount")
                    .GetString()),
                Number = track.GetProperty("trackNumber")
                    .GetInt32(),
                Duration = TimeSpan.FromMilliseconds(track.GetProperty("duration").GetProperty("totalMilliseconds").GetUInt64()),
            });
        }

        return new SpotifyFullAlbum
        {
            Name = name,
            Artists = artists,
            Year = releaseDate.Year,
            Type = type,
            TotalTracks = tracks.Length,
            Uri = fromUri,
            Images = images,
            Tracks = tracks
        };
    }
}