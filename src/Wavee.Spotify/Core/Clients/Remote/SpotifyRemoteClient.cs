using Eum.Spotify.connectstate;
using Wavee.Core.Models;
using Wavee.Interfaces;
using Wavee.Spotify.Core.Mappings;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.Spotify.Interfaces;
using Wavee.Spotify.Interfaces.Clients;

namespace Wavee.Spotify.Core.Clients.Remote;

internal sealed class SpotifyRemoteClient : ISpotifyRemoteClient
{
    private string? _activeConnectionId;
    private readonly IWebSocketService _webSocketService;
    private readonly IWaveePlayer _player;
    private readonly WaveeSpotifyConfig _config;
    private DateTimeOffset? _playingSince = null;
    private string? _sessionId;

    public SpotifyRemoteClient(IWebSocketService webSocketService, IWaveePlayer player, WaveeSpotifyConfig config)
    {
        _webSocketService = webSocketService;
        _player = player;
        _config = config;

        _player.PlaybackChanged += PlayerOnPlaybackChanged;
    }

    public async ValueTask<bool> Connect(CancellationToken cancellationToken = default)
    {
        var connectionId = await _webSocketService.ConnectAsync(cancellationToken: cancellationToken);
        if (_activeConnectionId != connectionId)
        {
            _activeConnectionId = connectionId;
            return true;
        }

        return false;
    }

    private async void PlayerOnPlaybackChanged(object? sender, WaveePlaybackState e)
    {
        if (e.Track is SpotifyAudioStream spotify)
        {
            // update remote device
            await UpdateSpotifyRemote(e, spotify);
        }
        else
        {
            // We are not playing a Spotify track. Disconnect from remote device if connected.
            await NotifyInactivity();
        }
    }

    private async Task UpdateSpotifyRemote(WaveePlaybackState waveePlaybackState, SpotifyAudioStream spotify)
    {
        if (_playingSince is null)
        {
            _playingSince = DateTimeOffset.UtcNow;
            _sessionId = Guid.NewGuid().ToString();
        }

        // This mainly means creating a PutStateRequest with the active device
        var spotifyRemoteState = waveePlaybackState.ToPlayerState(_player.Position,
            sessionId: _sessionId,
            spotify);
        
        var request = spotifyRemoteState.ToPutState(
            PutStateReason.PlayerStateChanged,
            volume: _player.Volume,
            playerPosition: _player.Position,
            hasBeenPlayingSince: _playingSince,
            now: DateTimeOffset.UtcNow,
            lastCommandSentBy: null,
            lastCommandId: null,
            _config.Remote);

        await _webSocketService.PutState(request, CancellationToken.None);
    }

    private async Task NotifyInactivity()
    {
        _playingSince = null;
        _sessionId = null;
        // TODO
    }
}