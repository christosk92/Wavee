using System.Reactive.Linq;
using Eum.Spotify.connectstate;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
using Wavee.Core.Player;
using Wavee.Core.Player.PlaybackStates;
using Wavee.Spotify.Infrastructure.ApResolver;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Mercury.Token;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.Spotify.Infrastructure.Remote.Messaging;

namespace Wavee.Spotify.Infrastructure.Remote;

public class SpotifyRemoteClient
{
    private readonly SpotifyRemoteConnection _connection;
    private readonly MercuryClient _mercuryClient;
    private readonly TokenClient _tokenClient;
    private readonly SpotifyRemoteConfig _config;
    private readonly Option<uint> _lastCommandId = Option<uint>.None;
    private readonly Option<string> _lastCommandSentBy = Option<string>.None;

    internal SpotifyRemoteClient(MercuryClient mercuryClient, TokenClient tokenClient, SpotifyRemoteConfig config,
        string deviceId)
    {
        _mercuryClient = mercuryClient;
        _tokenClient = tokenClient;
        _config = config;
        _connection = new SpotifyRemoteConnection();

        var mn = new ManualResetEvent(false);
        Task.Run(async () =>
        {
            await SpotifyRemoteRuntime.Start(_connection, _mercuryClient, _tokenClient, _config, deviceId,
                onLost: (e) => { mn.Reset(); });
            mn.Set();
        });

        SpotifyLocalDeviceState localState = SpotifyLocalDeviceState.New(
            deviceId: deviceId,
            deviceName: config.DeviceName,
            deviceType: config.DeviceType
        );

        bool wasActive = false;
        //setup a player listener
        WaveePlayer.StateChanged
            .Select(cm =>
            {
                //wait for connection
                mn.WaitOne();
                return cm;
            })
            .Select(c => MutateFromPlayer(c, localState, wasActive))
            .Select(async localDeviceState =>
            {
                localState = localDeviceState;
                wasActive = localDeviceState.IsActive;
                //send the state to spotify
                var putState = localDeviceState.BuildPutState(
                    PutStateReason.PlayerStateChanged,
                    playerTime: WaveePlayer.Position
                );

                var spClient = await ApResolve.GetSpClient(CancellationToken.None);
                var spClientUrl = $"https://{spClient.host}:{spClient.port}";
                var cluster = await SpotifyRemoteRuntime.PutState(
                    deviceId, spClientUrl,
                    putState,
                    _connection.ConnectionId.ValueUnsafe(),
                    _tokenClient,
                    CancellationToken.None);
            })
            .Subscribe();
    }

    private SpotifyLocalDeviceState MutateFromPlayer(
        WaveePlayerState state,
        SpotifyLocalDeviceState spotifyLocalDeviceState,
        bool wasActive)
    {
        bool isActiveNow = state.PlaybackState is { IsPlaying: true, TrackId.Service: ServiceType.Spotify };
        if (!wasActive && isActiveNow)
        {
            spotifyLocalDeviceState = spotifyLocalDeviceState with
            {
                IsActive = true,
                PlayingSince = DateTimeOffset.UtcNow
            };

            spotifyLocalDeviceState = spotifyLocalDeviceState with
            {
                LastCommandId = _lastCommandId,
                LastCommandSentBy = _lastCommandSentBy
            };
        }
        else if (!isActiveNow && wasActive)
        {
            return SpotifyLocalDeviceState.New(
                deviceId: spotifyLocalDeviceState.DeviceId,
                deviceName: spotifyLocalDeviceState.DeviceName,
                deviceType: spotifyLocalDeviceState.DeviceType
            );
        }

        return spotifyLocalDeviceState.SetStateFrom(state);
    }

    public IObservable<SpotifyRemoteState> StateChanged =>
        _connection
            .OnClusterChange()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Select(SpotifyRemoteState.From);
}