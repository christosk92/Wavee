using Wavee.Spotify.Domain.Tracks;

namespace Wavee.Spotify.Domain.Album;

public sealed class SpotifyAlbumDisc
{
    public required ushort Number { get; init; }
    public required IReadOnlyCollection<SpotifyAlbumTrack> Tracks { get; init; }
}