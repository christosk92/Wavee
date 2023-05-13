using Wavee.Spotify.Clients.Info;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.Token;
using Wavee.Spotify.Remote;

namespace Wavee.Spotify;

public interface ISpotifyConnection
{
    ISpotifyConnectionInfo Info { get; }
    IMercuryClient Mercury { get; }
    ITokenClient Token { get; }
    IRemoteClient Remote { get; }
}
