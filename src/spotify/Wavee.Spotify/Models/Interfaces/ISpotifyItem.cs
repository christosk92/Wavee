using Wavee.Spotify.Models.Common;

namespace Wavee.Spotify.Models.Interfaces;

public interface ISpotifyItem
{
    SpotifyId Uri { get; }
}