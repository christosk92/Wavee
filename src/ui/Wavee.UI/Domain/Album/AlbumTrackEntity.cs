using Wavee.Spotify.Domain.Tracks;

namespace Wavee.UI.Domain.Album;

public sealed class AlbumTrackEntity
{
    public required TimeSpan Duration { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ulong? PlayCount { get; init; }
}