using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Common;

namespace Wavee.Spotify.Domain.Podcasts;

public sealed class SpotifySimplePodcast : ISpotifyItem
{
    public required SpotifyId Uri { get; init; }
}