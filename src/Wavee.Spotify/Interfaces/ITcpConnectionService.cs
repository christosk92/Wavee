using Eum.Spotify;
using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Core.Models.Connection;
using Wavee.Spotify.Interfaces.Clients.Playback;

namespace Wavee.Spotify.Interfaces;

internal interface ITcpConnectionService : IDisposable
{
    Task<APWelcome> ConnectAsync(CancellationToken cancellationToken);
    Task<SpotifyAudioKey> RequestAudioKeyAsync(SpotifyId itemId, string fileId, CancellationToken cancellationToken);
    APWelcome? WelcomeMessage { get; }
}