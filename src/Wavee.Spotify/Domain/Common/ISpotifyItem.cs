using Wavee.Spotify.Common;

namespace Wavee.Spotify.Domain.Common;

public interface ISpotifyItem
{
    SpotifyId Uri { get; }
}