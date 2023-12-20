using System.Collections.Immutable;
using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;
using Wavee.Spotify.Domain.Album;
using Wavee.Spotify.Domain.Tracks;
using Wavee.UI.Domain.Album;
using Wavee.UI.Domain.Track;
using Wavee.UI.Features.Album.Queries;

namespace Wavee.UI.Features.Album.QueryHandlers;

public sealed class GetAlbumViewQueryHandler : IQueryHandler<GetAlbumViewQuery, AlbumViewResult>
{
    private readonly ISpotifyClient _spotifyClient;

    public GetAlbumViewQueryHandler(ISpotifyClient spotifyClient)
    {
        _spotifyClient = spotifyClient;
    }

    public async ValueTask<AlbumViewResult> Handle(GetAlbumViewQuery query, CancellationToken cancellationToken)
    {
        if (SpotifyId.TryParse(query.Id, out var spotifyId))
        {
            var spotifyAlbum = await _spotifyClient.Album.GetAlbum(spotifyId, cancellationToken);
            return ToSharedAlbumResult(spotifyAlbum);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private AlbumViewResult ToSharedAlbumResult(SpotifyAlbumView spotifyAlbum)
    {
        return new AlbumViewResult
        {
            Id = spotifyAlbum.Id.ToString(),
            Name = spotifyAlbum.Name,
            ReleaseDate = spotifyAlbum.ReleaseDate,
            ReleaseDatePrecision = spotifyAlbum.ReleaseDatePrecision,
            Artists = spotifyAlbum.Artists.Select(x => x.ToSimpleArtist()).ToImmutableArray(),
            LargeImageUrl = spotifyAlbum.Images.OrderByDescending(x => x.Width ?? 0).FirstOrDefault().Url,
            MediumImageUrl = spotifyAlbum.Images.OrderByDescending(x => x.Width ??0).Skip(1).FirstOrDefault().Url,
            Discs = CreateDiscs(spotifyAlbum.Discs),
            MoreAlbumsByArtist = spotifyAlbum.MoreAlbumsByArtist.Select(x=> x.ToSimpleAlbum()).ToImmutableArray(),
            Label = spotifyAlbum.Label,
            Copyrights = spotifyAlbum.Copyright
        };
    }

    private static IReadOnlyCollection<AlbumDiscEntity> CreateDiscs(IReadOnlyCollection<SpotifyAlbumDisc> spotifyAlbumDiscs)
    {
        Span<AlbumDiscEntity> discs = new AlbumDiscEntity[spotifyAlbumDiscs.Count];
        for (var i = 0; i < spotifyAlbumDiscs.Count; i++)
        {
            var spotifyAlbumDisc = spotifyAlbumDiscs.ElementAt(i);
            discs[i] = new AlbumDiscEntity
            {
                Number = spotifyAlbumDisc.Number,
                Tracks = CreateTracks(spotifyAlbumDisc.Tracks)
            };
        }

        return ImmutableArray.Create(discs);
    }

    private static IReadOnlyCollection<AlbumTrackEntity> CreateTracks(IReadOnlyCollection<SpotifyAlbumTrack> tracks)
    {
        Span<AlbumTrackEntity> albumTracks = new AlbumTrackEntity[tracks.Count];
        for (var i = 0; i < tracks.Count; i++)
        {
            var spotifyAlbumTrack = tracks.ElementAt(i);
            albumTracks[i] = new AlbumTrackEntity
            {
                Id = spotifyAlbumTrack.Uri.ToString(),
                Name = spotifyAlbumTrack.Name,
                Duration = spotifyAlbumTrack.Duration,
                PlayCount = spotifyAlbumTrack.PlayCount,
                UniqueItemId = spotifyAlbumTrack.UniqueItemId
            };
        }

        return ImmutableArray.Create(albumTracks);
    }
}