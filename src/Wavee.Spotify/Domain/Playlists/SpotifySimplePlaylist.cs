using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Common;

namespace Wavee.Spotify.Domain.Playlists;

public sealed class SpotifySimplePlaylist : ISpotifyItem
{
    public required SpotifyId Uri { get; init; }
}