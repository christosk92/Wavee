using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Eum.Spotify.connectstate;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Clients;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.UI.Spotify.Interfaces;
using Wavee.UI.Spotify.Interfaces.Api;
using Wavee.UI.Spotify.Playback;
using Wavee.UI.Spotify.Remote;

namespace Wavee.UI.Spotify.Clients;

internal sealed class SpotifyPlaybackClient : IPlaybackClient
{
    private readonly ISpClient _spClient;
    private readonly SpotifyTokenClient _tokensClient;
    private readonly string _deviceId;
    private SpotifyPlaybackDevice? _device;
    private readonly SpotifyClient _parentClient;

    private readonly Action<(SpotifyPlaybackDevice? Old, SpotifyPlaybackDevice? New)> _deviceConnected;
    private readonly ISpotifyWebsocketConnectionFactory _connectionFactory;

    public SpotifyPlaybackClient(ISpClient spClient,
        SpotifyTokenClient tokensClient,
        string deviceId,
        SpotifyClient parentClient,
        Action<(SpotifyPlaybackDevice? Old, SpotifyPlaybackDevice? New)> deviceConnected, 
        ISpotifyWebsocketConnectionFactory connectionFactory)
    {
        _spClient = spClient;
        _tokensClient = tokensClient;
        _deviceId = deviceId;
        _parentClient = parentClient;
        _deviceConnected = deviceConnected;
        _connectionFactory = connectionFactory;
        _deviceConnected((null, null));
    }

    public async Task<IPlaybackDevice> Connect(string name, DeviceType type, CancellationToken cancellationToken)
    {
        if (_device is not null)
        {
            var connected = await _device.Connect(cancellationToken);
            if (connected)
            {
                _deviceConnected((_device, _device));
            }

            return _device;
        }

        var old_device = _device;
        _device = new SpotifyPlaybackDevice(_deviceId, name, type, _spClient, _tokensClient, _parentClient, _connectionFactory);
        var conn = await _device.Connect(cancellationToken);
        if (conn)
        {
            _deviceConnected((old_device, _device));
        }

        return _device;
    }
}