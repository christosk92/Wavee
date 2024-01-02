using Spotify.Metadata;
using Wavee.Core.Enums;
using Wavee.Interfaces;
using Wavee.Spotify.Core.Mappings;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Exceptions;
using Wavee.Spotify.Core.Models.Metadata;
using Wavee.Spotify.Infrastructure.HttpClients;
using Wavee.Spotify.Infrastructure.Services;
using Wavee.Spotify.Interfaces.Clients;
using Wavee.Spotify.Interfaces.Models;

namespace Wavee.Spotify.Core.Clients;

internal sealed class SpotifyMetadataClient : ISpotifyMetadataClient
{
    private readonly SpotifyInternalHttpClient _httpClient;
    private readonly IWaveeCachingProvider _cachingProvider;

    public SpotifyMetadataClient(SpotifyInternalHttpClient httpClient, IWaveeCachingProvider cachingProvider)
    {
        _httpClient = httpClient;
        _cachingProvider = cachingProvider;
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

    public async ValueTask<SpotifySimpleTrack> GetTrack(SpotifyId id, CancellationToken cancellationToken = default)
    {
        var response = await FetchTrack(id, true, cancellationToken);
        return response is SpotifySimpleTrack track
            ? track
            : throw new SpotifyItemNotSupportedException(id);
    }

    private ValueTask<ISpotifyItem> FetchTrack(SpotifyId id,
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

        if (allowCache)
        {
            if (_cachingProvider.TryGet(cacheKey, out var cachedItem))
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
        using var resp = await _httpClient.Get(endpointWithId, null, cancellationToken);
        var bytes = await resp.Content.ReadAsByteArrayAsync(cancellationToken);
        var item = parser(bytes);
        _cachingProvider.Set(cacheKey, bytes);
        return item;
    }
}