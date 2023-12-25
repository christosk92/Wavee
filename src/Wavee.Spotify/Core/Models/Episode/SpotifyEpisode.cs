using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Interfaces.Models;

namespace Wavee.Spotify.Core.Models.Episode;

public readonly struct SpotifyEpisode : ISpotifyPlayableItem
{
    public TimeSpan Duration { get; }
    public string? Id => Uri.ToString();
    public SpotifyId Uri { get; }
}