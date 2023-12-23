using Wavee.Spotify.Common;

namespace Wavee.Spotify.Core.Models.User;

public sealed class Me
{
    public required SpotifyId Id { get; init; }
}