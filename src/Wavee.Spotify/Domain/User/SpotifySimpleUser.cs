using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Common;

namespace Wavee.Spotify.Domain.User;

public sealed class SpotifySimpleUser : ISpotifyItem
{
    public required SpotifyId Uri { get; init; }
}