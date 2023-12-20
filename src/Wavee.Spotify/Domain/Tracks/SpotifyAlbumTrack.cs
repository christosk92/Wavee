using Wavee.Spotify.Common;

namespace Wavee.Spotify.Domain.Tracks;

public sealed class SpotifyAlbumTrack
{
    public required SpotifyId Uri { get; init; }
    public required string Name { get; init; } = string.Empty;
    public required TimeSpan Duration { get; init; }
    public required ulong? PlayCount { get; init; }
    public required string UniqueItemId { get; init; }
}