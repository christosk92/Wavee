using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Common;

namespace Wavee.Spotify.Domain.Genres;

public sealed class SpotifySimpleGenre : ISpotifyItem
{
    public required SpotifyId Uri { get; init; }
}