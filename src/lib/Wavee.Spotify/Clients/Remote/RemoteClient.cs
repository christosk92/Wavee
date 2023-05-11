using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text.Json;
using Eum.Spotify.connectstate;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Sys;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.Mercury.Metadata;
using Wavee.Spotify.Clients.Playback;
using Wavee.Spotify.Infrastructure.Sys;
using Wavee.Spotify.Infrastructure.Sys.Remote;

namespace Wavee.Spotify.Clients.Remote;

internal readonly struct RemoteClient<RT> : IRemoteClient
    where RT : struct, HasWebsocket<RT>, HasHttp<RT>, HasLog<RT>, HasAudioOutput<RT>
{
    private readonly string _deviceId;
    private readonly string _deviceName;
    private readonly DeviceType _deviceType;
    private readonly IPlaybackClient _playbackClient;
    private readonly Guid _mainConnectionId;
    private readonly Func<ValueTask<string>> _getBearer;
    private readonly RT _runtime;

    private static readonly AtomHashMap<Guid, Seq<Action<SpotifyPlaybackInfo>>> OnPlaybackInfo =
        LanguageExt.AtomHashMap<Guid, Seq<Action<SpotifyPlaybackInfo>>>.Empty;

    public RemoteClient(Guid mainConnectionId,
        string deviceId,
        string deviceName,
        DeviceType deviceType,
        Func<ValueTask<string>> getBearer,
        IPlaybackClient playbackClient,
        RT runtime)
    {
        _mainConnectionId = mainConnectionId;
        _getBearer = getBearer;
        _runtime = runtime;
        _playbackClient = playbackClient;
        _deviceName = deviceName;
        _deviceType = deviceType;
        _deviceId = deviceId;
    }

    public static void OnPlaybackChanged(Guid infoConnectionId, SpotifyPlaybackInfo playbackInfo)
    {
        var g = OnPlaybackInfo.Find(infoConnectionId);
        g.IfSome(k => k.Iter(f => f(playbackInfo)));
    }

    public async Task<IObservable<SpotifyPlaybackState>> Connect(CancellationToken ct = default)
    {
        var remoteClusterRef = Ref(Option<Cluster>.None);
        RemoteClient<RT> tmpThis = this;

        Ref<SpotifyPlaybackInfo> playbackInfoRef = Ref(new SpotifyPlaybackInfo());

        void OnPlaybackInfo(SpotifyPlaybackInfo playbackInfo)
        {
            atomic(() => playbackInfoRef.Swap(_ => playbackInfo));
        }

        RemoteClient<RT>.OnPlaybackInfo.AddOrUpdate(_mainConnectionId, r => r.Add(OnPlaybackInfo),
            () => Seq1(OnPlaybackInfo));

        var aff = ConnectWithDisconnection(
            remoteClusterRef,
            Option<LocalDeviceState>.None,
            playbackInfoRef.OnChange(),
            _deviceId,
            _deviceName,
            _deviceType,
            _getBearer,
            ((message, state) => OnRequest(tmpThis._playbackClient, message, state)),
            ct);

        var remoteState = (await aff.Run(_runtime)).ThrowIfFail();
        atomic(() => remoteClusterRef.Swap(_ => remoteState));

        return remoteClusterRef
            .OnChange()
            .Select(c => c.Match(
                Some: x => new SpotifyPlaybackState
                {
                    IsPlayingOnRemote = true,
                    RemoteState = Some(x)
                },
                None: () => new SpotifyPlaybackState
                {
                    IsPlayingOnRemote = false,
                    RemoteState = Option<Cluster>.None
                }));
    }

    private static Task<LocalDeviceState> OnRequest(
        IPlaybackClient playbackClient,
        SpotifyWebsocketMessage arg,
        LocalDeviceState localDeviceState)
    {
        using var jsonDocument = JsonDocument.Parse(arg.Payload.ValueUnsafe());
        var messageId = jsonDocument.RootElement.GetProperty("message_id").GetUInt32();
        var sentByDeviceId = jsonDocument.RootElement.GetProperty("sent_by_device_id").GetString()!;
        var command = jsonDocument.RootElement.GetProperty("command");

        var endpoint = command.GetProperty("endpoint").GetString();

        switch (endpoint)
        {
            case "transfer":
                localDeviceState = HandleTransferCommand(command, localDeviceState);
                break;
        }

        return Task.FromResult(localDeviceState.SetActive(true) with
        {
            LastMessageId = Some(messageId),
            LastCommandSentByDeviceId = Some(sentByDeviceId),
        });
    }

    private static LocalDeviceState HandleTransferCommand(JsonElement command, LocalDeviceState localDeviceState)
    {
        return localDeviceState;
    }


    private static Aff<RT, Cluster>
        ConnectWithDisconnection(
            Ref<Option<Cluster>> remoteClusterRef,
            Option<LocalDeviceState> maybeLocalState,
            IObservable<SpotifyPlaybackInfo> playbackInfoChanged,
            string deviceId,
            string deviceName,
            DeviceType deviceType,
            Func<ValueTask<string>> getBearer,
            Func<SpotifyWebsocketMessage, LocalDeviceState, Task<LocalDeviceState>> onRequest,
            CancellationToken ct = default) =>
        from websocket in Remote<RT>.Connect(getBearer, ct)
        from hello in Remote<RT>.Hello(
            maybeLocalState,
            websocket,
            deviceId,
            deviceName,
            deviceType,
            getBearer,
            ct)
        from _ in Aff<RT, Unit>(async (rt) =>
        {
            await Task.Factory.StartNew(async () =>
            {
                using var disposable = playbackInfoChanged.Subscribe(async info =>
                {
                    var build = atomic(() => hello.LocalState
                        .Swap(f =>
                        {
                            var k = f
                                .SetActive(true);
                            if (info.Buffering)
                                k = k.Buffering(None);
                            k = info.Paused ? k.Paused() : k.Playing();
                            
                            return k.
                                    SetDuration(info.Duration).
                                    SetContextUri(info.ContextUri, info.ContextUrl, info.ContextMetadata, info.ContextRestrictions).
                                    SetTrack(info.Track).
                                    SetIndex(info.Index).
                                    SetPosition((long)info.Position.IfNone(TimeSpan.Zero).TotalMilliseconds);
                        }));
                    var vol = AudioOutput<RT>.Volume().Run(rt).ThrowIfFail();
                    var putState = build.BuildPutState(PutStateReason.PlayerStateChanged,
                        vol,
                        info.Position);
                    var aff = await RemoteState<RT>.Put(putState, build.DeviceId,
                        build.ConnectionId,
                        getBearer, ct).Run(rt);
                });

                bool breakOut = false;
                while (!breakOut)
                {
                    var run = await Remote<RT>
                        .ListenForMessages(websocket, remoteClusterRef,
                            getBearer,
                            hello.LocalState,
                            onRequest,
                            CancellationToken.None)
                        .Run(rt);

                    if (run.IsFail)
                    {
                        atomic(() => remoteClusterRef.Swap(_ => None));
                        //reconnect
                        var isConnected = false;
                        while (!isConnected)
                        {
                            await Task.Delay(3000);
                            var connResult = await ConnectWithDisconnection(
                                remoteClusterRef,
                                hello.LocalState.Value,
                                playbackInfoChanged,
                                deviceId,
                                deviceName,
                                deviceType,
                                getBearer,
                                onRequest,
                                CancellationToken.None
                            ).Run(rt);

                            if (connResult.IsFail)
                            {
                                Log<RT>.logInfo("Failed to reconnect to remote... Trying again in 3 seconds");
                                await Task.Delay(3000);
                            }
                            else
                            {
                                isConnected = true;
                                var cluster = connResult.ThrowIfFail();
                                atomic(() => remoteClusterRef.Swap(_ => Some(cluster)));
                                breakOut = true;
                            }
                        }
                    }
                    else
                    {
                        var newState = run.ThrowIfFail();
                    }
                }
            }, TaskCreationOptions.LongRunning);
            return unit;
        })
        select hello.RemoteState;
}