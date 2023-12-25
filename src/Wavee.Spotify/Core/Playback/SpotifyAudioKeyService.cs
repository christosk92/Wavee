using Wavee.Spotify.Core.Models.Common;
using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.Clients.Playback;

namespace Wavee.Spotify.Core.Clients.Playback;

internal sealed class SpotifyAudioKeyService : ISpotifyAudioKeyService
{
    private readonly ITcpConnectionService _tcpConnectionService;

    public SpotifyAudioKeyService(ITcpConnectionService tcpConnectionService)
    {
        _tcpConnectionService = tcpConnectionService;
    }

    public Task<SpotifyAudioKey> GetAudioKey(SpotifyId trackId, string fileFileIdBase16,
        CancellationToken cancellationToken)
    {
        return _tcpConnectionService.RequestAudioKeyAsync(trackId, fileFileIdBase16, cancellationToken);
    }
}