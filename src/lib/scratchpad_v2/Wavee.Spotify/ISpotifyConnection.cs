using Eum.Spotify;
using Wavee.Spotify.Clients.Info;

namespace Wavee.Spotify;

public interface ISpotifyConnection
{
    ISpotifyConnectionInfo Info { get; }
}