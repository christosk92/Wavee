using Spotify.Metadata;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Artist;
using Wavee.Spotify.Domain.Common;

namespace Wavee.Spotify.Domain.Album;

public sealed class SpotifyAlbumView
{
    public required SpotifyId Id { get; init; }
    public required string Name { get; init; } = null!;
    public required IReadOnlyCollection<SpotifySimpleArtist> Artists { get; init; } = null!;
    public required IReadOnlyCollection<SpotifyImage> Images { get; init; } = null!;
    public required DateOnly ReleaseDate { get; init; }
    public required ReleaseDatePrecision ReleaseDatePrecision { get; init; }
    public required IReadOnlyCollection<SpotifyAlbumDisc> Discs { get; init; }
    public required string Label { get; init; }
    public required IReadOnlyCollection<SpotifySimpleAlbum> MoreAlbumsByArtist { get; init; }
    public required IReadOnlyCollection<Copyright> Copyright { get; init; }
    public required string Type { get; init; }
}

public enum ReleaseDatePrecision
{
    Year,
    Month,
    Day
}