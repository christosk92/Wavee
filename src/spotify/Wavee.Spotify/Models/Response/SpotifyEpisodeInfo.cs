using Wavee.Spotify.Models.Common;
using Wavee.Spotify.Models.Interfaces;

namespace Wavee.Spotify.Models.Response;

public sealed class SpotifyEpisodeInfo : ISpotifyPlayableItem
{
    public required TimeSpan Duration { get; init; }
    public required SpotifyId Uri { get; init; }
}