using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Common;

namespace Wavee.Spotify.Domain.Tracks;

public sealed class SpotifySimpleTrack : ISpotifyItem
{
    public required SpotifyId Uri { get; init; }
    public required string Name { get; init; }
}