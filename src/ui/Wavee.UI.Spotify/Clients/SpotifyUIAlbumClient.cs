using LanguageExt;
using Wavee.Id;
using Wavee.Metadata.Album;
using Wavee.UI.Client.Album;
using Wavee.UI.Common;

namespace Wavee.UI.Spotify.Clients;

internal sealed class SpotifyUIAlbumClient : IWaveeUIAlbumClient
{
    private readonly WeakReference<SpotifyClient> _spotifyClient;

    public SpotifyUIAlbumClient(SpotifyClient spotifyClient)
    {
        _spotifyClient = new WeakReference<SpotifyClient>(spotifyClient);
    }

    public async Task<WaveeUIAlbumView> GetAlbum(string id, CancellationToken cancellationToken)
    {
        if (!_spotifyClient.TryGetTarget(out var spotifyClient))
        {
            throw new InvalidOperationException("SpotifyClient is not available");
        }

        var album = await spotifyClient.Metadata.GetAlbum(SpotifyId.FromUri(id), cancellationToken);
        return ParseFrom(album);
    }

    public Task<WaveeUIAlbumDisc[]> GetAlbumTracks(string id, CancellationToken ct = default)
    {
        if (!_spotifyClient.TryGetTarget(out var spotifyClient))
        {
            throw new InvalidOperationException("SpotifyClient is not available");
        }
        return spotifyClient.Metadata.GetAlbumTracks(SpotifyId.FromUri(id), ct)
            .Map(f => f.Select(ParseToDisc).ToArray());
    }

    private static WaveeUIAlbumView ParseFrom(SpotifyAlbum album)
    {
        return new WaveeUIAlbumView
        {
            Source = album.Id.Service,
            Id = album.Id.ToString(),
            Name = album.Name,
            Artists = album.Artists,
            ReleaseDate = album.ReleaseDate,
            ReleaseDatePrecision = album.ReleaseDatePrecision,
            LargeImage = album.Images.OrderByDescending(x => x.Height.IfNone(0)).HeadOrNone().Map(f => f.Url).IfNone(string.Empty),
            DarkColor = album.Colors.Map(x => x.ColorDark),
            LightColor = album.Colors.Map(f => f.ColorLight),
            MoreAlbums = album.MoreAlbums.Select(x => new CardViewModel
            {
                Title = x.Name,
                Id = x.Id.ToString(),
                Type = x.Id.Type,
                Image = x.Images.OrderByDescending(x => x.Height.IfNone(0)).HeadOrNone().Map(f => f.Url).IfNone(string.Empty),
                Subtitle = x.ReleaseDate.Year.ToString(),
            }).ToArray(),
            Copyrights = album.Copyrights,
            Discs = album.Discs.Select(x => ParseToDisc(x)).ToArray()
        };
    }

    private static WaveeUIAlbumDisc ParseToDisc(SpotifyAlbumDisc spotifyAlbumDisc)
    {
        return new WaveeUIAlbumDisc
        {
            Tracks = spotifyAlbumDisc.Tracks.Select(f => new WaveeUIAlbumTrack
            {
                Uid = f.Uid,
                Source = f.Id.Service,
                Id = f.Id.ToString(),
                Name = f.Name,
                ContentRating = f.ContentRating,
                Artists = f.Artists,
                Duration = f.Duration,
                Playcount = f.Playcount,
                InLibrary = f.Saved,
                TrackNumber = f.TrackNumber
            }).ToArray(),
            DiscNumber = spotifyAlbumDisc.Number
        };
    }
}