using Eum.Spotify;
using Wavee.Spotify.Core.Models.Connection;

namespace Wavee.Spotify.Core.Interfaces.Connection;

internal interface ISpotifyTcpClient : IDisposable
{
    Task<APWelcome> ConnectAsync(string host, int port, LoginCredentials credentiasl, string deviceId);
    bool Connected { get; }

    SpotifyRefPackage Receive(int seq);
    void Send(SpotifyRefPackage package, int seq);
}