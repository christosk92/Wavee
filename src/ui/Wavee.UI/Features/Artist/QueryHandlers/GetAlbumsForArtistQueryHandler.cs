using System.Diagnostics;
using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Common;
using Wavee.Spotify.Domain.Tracks;
using Wavee.UI.Domain.Album;
using Wavee.UI.Domain.Artist;
using Wavee.UI.Features.Artist.Queries;

namespace Wavee.UI.Features.Artist.QueryHandlers;

public sealed class GetAlbumsForArtistQueryHandler : IQueryHandler<GetAlbumsForArtistQuery, ArtistAlbumsResult>
{
    private readonly record struct CachedAlbumsKey(SpotifyId ArtistId,
        int Offset,
        int Limit,
        DiscographyGroupType? Group);
    private record CachedAlbums(
        IReadOnlyCollection<SimpleAlbumEntity> Albums,
        uint Total,
        DateTimeOffset CachedAt)
    {
        private static TimeSpan _cacheDuration = TimeSpan.FromHours(1);

        public bool Expired => DateTimeOffset.UtcNow - CachedAt > _cacheDuration;
    }

    private static Dictionary<CachedAlbumsKey, CachedAlbums> _cache = new();
    private readonly ISpotifyClient _spotifyClient;

    public GetAlbumsForArtistQueryHandler(ISpotifyClient spotifyClient)
    {
        _spotifyClient = spotifyClient;
    }


    public async ValueTask<ArtistAlbumsResult> Handle(GetAlbumsForArtistQuery query, CancellationToken cancellationToken)
    {
        // Step 1. Fetch albums
        var key = new CachedAlbumsKey(SpotifyId.FromUri(query.Id),
            query.Offset,
            query.Limit,
            query.Group);

        if (_cache.TryGetValue(key, out var cached) && !cached.Expired)
        {
            return new ArtistAlbumsResult
            {
                Total = (uint)cached.Total,
                Albums = cached.Albums
            };
        }

        var (albums, total) = await (query.Group switch
        {
            DiscographyGroupType.Album => _spotifyClient.Artist.GetDiscographyAlbumsAsync(SpotifyId.FromUri(query.Id),
                offset: (uint)query.Offset,
                limit: (uint)query.Limit,
                cancellationToken),
            DiscographyGroupType.Single => _spotifyClient.Artist.GetDiscographySinglesAsync(SpotifyId.FromUri(query.Id),
                offset: (uint)query.Offset,
                limit: (uint)query.Limit,
                cancellationToken),
            DiscographyGroupType.Compilation => _spotifyClient.Artist.GetDiscographyCompilationsAsync(SpotifyId.FromUri(query.Id),
                offset: (uint)query.Offset,
                limit: (uint)query.Limit,
                cancellationToken),
            null => _spotifyClient.Artist.GetDiscographyAllAsync(SpotifyId.FromUri(query.Id),
                offset: (uint)query.Offset,
                limit: (uint)query.Limit,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException()
        });

        IReadOnlyDictionary<SpotifyId, IReadOnlyCollection<SpotifyAlbumTrack>> tracks = new Dictionary<SpotifyId, IReadOnlyCollection<SpotifyAlbumTrack>>();
        if (query.FetchTracks)
        {
            // Step 2. Fetch tracks
            tracks = await GetTracksForAlbums(albums.Select(f => f.Uri), cancellationToken);
        }

        var adapted = albums.Select(f => new SimpleAlbumEntity()
        {
            Images = f.Images,
            Name = f.Name,
            Id = f.Uri.ToString(),
            Tracks = tracks.TryGetValue(f.Uri, out var tr)
                ? tr
                    .Select(x => new AlbumTrackEntity()
                    {
                        Duration = x.Duration,
                        Id = x.Uri.ToString(),
                        Name = x.Name,
                        PlayCount = x.PlayCount
                    })
                    .ToArray()
                : null,
            Year = (ushort)f.ReleaseDate.Year,
            Type = f.Type
        }).ToArray();
        _cache[key] = new CachedAlbums(adapted, total, DateTimeOffset.UtcNow);

        return new ArtistAlbumsResult
        {
            Total = total,
            Albums = adapted
        };
    }

    private async Task<IReadOnlyDictionary<SpotifyId, IReadOnlyCollection<SpotifyAlbumTrack>>> GetTracksForAlbums(IEnumerable<SpotifyId> select, CancellationToken cancellationToken)
    {
        //var tracks = new Dictionary<SpotifyId, IReadOnlyCollection<SpotifyAlbumTrack>>();
        var tracks = select.ToDictionary(x => x,
            x => Task.Run(async () => await _spotifyClient.Album.GetTracks(x, cancellationToken), cancellationToken));
        await Task.WhenAll(tracks.Values);
        return tracks.ToDictionary(x => x.Key, x => x.Value.Result);
    }
}