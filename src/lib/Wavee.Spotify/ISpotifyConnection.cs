using Eum.Spotify;
using Wavee.Spotify.Clients.Info;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.Playback;
using Wavee.Spotify.Clients.Remote;
using Wavee.Spotify.Clients.Token;

namespace Wavee.Spotify;

public interface ISpotifyConnection
{
    ISpotifyConnectionInfo Info { get; }
    IMercuryClient Mercury { get; }
    ITokenClient Token { get; }
    IRemoteClient Remote { get; }
    IPlaybackClient Playback { get; }
}
