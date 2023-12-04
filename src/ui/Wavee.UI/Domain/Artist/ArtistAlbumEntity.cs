using Wavee.Spotify.Domain.Common;

namespace Wavee.UI.Domain.Artist;

public sealed class ArtistAlbumEntity
{
    public required IReadOnlyCollection<SpotifyImage> Images { get; init; }
    public required string Name { get; set; }
    public required string Id { get; init; }
    public required ArtistAlbumTrackEntity[] Tracks { get; set; }
    public required ushort Year { get; init; }
    public required string Type { get; init; }
}

public sealed class ArtistAlbumTrackEntity
{
    public required TimeSpan Duration { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ulong? PlayCount { get; init; }
}