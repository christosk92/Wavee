using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Common;

namespace Wavee.Spotify.Domain.Podcasts;

public sealed class SpotifySimplePodcastEpisode : ISpotifyItem
{
    public required SpotifyId Uri { get; init; }
}