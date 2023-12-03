using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Common;

namespace Wavee.Spotify.Domain.Album;

public sealed class SpotifySimpleAlbum : ISpotifyItem
{
    public required SpotifyId Uri { get; init; }
    public required string Name { get; init; }
    public required SpotifyImage[] Images { get; init; }
    public required DateOnly ReleaseDate { get; init; }
    public required string Type { get; init; }
}