using Mediator;
using Spotify.Metadata;
using Wavee.Spotify.Domain.Album;
using Wavee.UI.Domain.Album;
using Wavee.UI.Domain.Artist;

namespace Wavee.UI.Features.Album.Queries;

public sealed class GetAlbumViewQuery : IQuery<AlbumViewResult>
{
    public required string Id { get; init; }
}

public sealed class AlbumViewResult
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required DateOnly ReleaseDate { get; init; }
    public required ReleaseDatePrecision ReleaseDatePrecision { get; init; }
    public required IReadOnlyCollection<SimpleArtistEntity> Artists { get; init; }
    public required string LargeImageUrl { get; init; }
    public required string MediumImageUrl { get; init; }
    public required IReadOnlyCollection<AlbumDiscEntity> Discs { get; init; }
    public required IReadOnlyCollection<SimpleAlbumEntity> MoreAlbumsByArtist { get; init; }
    public required string Label { get; init; }
    public required IReadOnlyCollection<Copyright> Copyrights { get; init; }
}