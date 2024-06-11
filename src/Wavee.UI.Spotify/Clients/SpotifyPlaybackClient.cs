using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Clients;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.Contracts.Interfaces.Playback;
using Wavee.UI.Spotify.Common;
using Wavee.UI.Spotify.Exceptions;
using Wavee.UI.Spotify.Interfaces;
using Wavee.UI.Spotify.Interfaces.Api;
using Wavee.UI.Spotify.Playback;

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

    private readonly IWaveePlayer _player;

    public SpotifyPlaybackClient(ISpClient spClient,
        SpotifyTokenClient tokensClient,
        string deviceId,
        SpotifyClient parentClient,
        Action<(SpotifyPlaybackDevice? Old, SpotifyPlaybackDevice? New)> deviceConnected,
        ISpotifyWebsocketConnectionFactory connectionFactory,
        IWaveePlayer player)
    {
        _spClient = spClient;
        _tokensClient = tokensClient;
        _deviceId = deviceId;
        _parentClient = parentClient;
        _deviceConnected = deviceConnected;
        _connectionFactory = connectionFactory;
        _player = player;
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
        _device = new SpotifyPlaybackDevice(_deviceId, name, type, _spClient, _tokensClient, _parentClient,
            _connectionFactory, _player);
        var conn = await _device.Connect(cancellationToken);
        if (conn)
        {
            _deviceConnected((old_device, _device));
        }

        return _device;
    }

    public async Task<IMediaSource> CreateMediaSource(RegularSpotifyId itemId, bool preload,
        CancellationToken cancellationToken)
    {
        // 3 Steps:
        // 1. Get the file
        // 2a. Get the audio key
        // 2b. Get the audio file URL
        // 3. Create the media source

        var item = await _parentClient.Tracks.GetTrack(itemId, cancellationToken);
        var file = item.AudioFiles
            .Where(x => x.Type is AudioFileType.Vorbis)
            .FirstOrDefault(x => x.Quality is AudioFileQuality.High);

        if (file is null)
        {
            throw new SpotifyException(SpotifyFailureReason.CannotPlayTrack, "No high quality vorbis file found");
        }

        var keyTask = _tokensClient.GetAudioKey(itemId, file.Id, cancellationToken).AsTask();
        var urlTask = GetAudioUrl(file.Id, preload, cancellationToken);
        await Task.WhenAll(keyTask, urlTask);
        var key = keyTask.Result;
        var url = urlTask.Result;
        
        return new SpotifyMediaSource(item, file, key, url);
    }

    private async Task<string> GetAudioUrl(string fileId, bool preload, CancellationToken cancellationToken)
    {
       var fileIdBase16 = ConvertToBase16(fileId);
        
        var storageResolve = preload
            ? await _spClient.InteractivePrefetch(fileIdBase16, cancellationToken)
            : await _spClient.Interactive(fileIdBase16, cancellationToken);

        var item = storageResolve.Cdnurl.FirstOrDefault();
        if (item is null)
        {
            throw new SpotifyException(SpotifyFailureReason.CannotPlayTrack, "No CDN URL found");
        }

        return item;
    }

    private string ConvertToBase16(string fileId)
    {
        ReadOnlySpan<byte> fileIdBase62 = ByteString.FromBase64(fileId).Span;
        var sb = new System.Text.StringBuilder();
        foreach (var b in fileIdBase62)
        {
            sb.Append(b.ToString("x2"));
        }
        
        return sb.ToString();
    }
}