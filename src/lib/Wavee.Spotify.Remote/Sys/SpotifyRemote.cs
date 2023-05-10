using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Channels;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Eum.Spotify.transfer;
using Google.Protobuf;
using LanguageExt.UnsafeValueAccess;
using Wavee.Common;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Player;
using Wavee.Spotify.Playback;
using Wavee.Spotify.Playback.Streams;
using Wavee.Spotify.Playback.Sys;
using Wavee.Spotify.Remote.Helpers;
using Wavee.Spotify.Remote.State;
using Wavee.Spotify.Remote.Sys.Connection;
using Wavee.Spotify.Sys;
using Wavee.Spotify.Sys.AudioKey;
using Wavee.Spotify.Sys.Common;

namespace Wavee.Spotify.Remote.Sys;

internal static class SpotifyRemote<RT> where RT : struct, HasWebsocket<RT>, HasHttp<RT>, HasAudioOutput<RT>
{
    private static readonly ChannelWriter<ISpotifyRemoteMessage> SpotifyRequests;

    static SpotifyRemote()
    {
        var spotifyRequests = Channel.CreateUnbounded<ISpotifyRemoteMessage>();
        SpotifyRequests = spotifyRequests.Writer;

        Task.Factory.StartNew(async () =>
        {
            await ProcessMessages(spotifyRequests.Reader.ReadAllAsync());
        }, TaskCreationOptions.LongRunning);
    }

    private static async Task ProcessMessages(IAsyncEnumerable<ISpotifyRemoteMessage> messages)
    {
        Option<string> activeConnectionId = None;
        Option<IDisposable> playerSession = None;

        await foreach (var message in messages)
        {
            (activeConnectionId, playerSession) = await HandleMessage(message, activeConnectionId, playerSession);
        }
    }

    private static async Task<(Option<string>, Option<IDisposable>)> HandleMessage(
        ISpotifyRemoteMessage message,
        Option<string> activeConnectionId,
        Option<IDisposable> playerSession)
    {
        if (!activeConnectionId.IsNone)
        {
            if (message.ConnectionId.IsNone || (message.ConnectionId != activeConnectionId.ValueUnsafe()))
            {
                // Clear state
                // TODO
                Debugger.Break();
            }
        }

        activeConnectionId = message.ConnectionId;

        return message switch
        {
            SpotifyTransferRequest transfer => await HandleTransferRequest(transfer, playerSession),
            _ => (activeConnectionId, playerSession)
        };
    }

    private static async Task<(Option<string>, Option<IDisposable>)> HandleTransferRequest(
        SpotifyTransferRequest transfer,
        Option<IDisposable> playerSession)
    {
        atomic(() =>
        {
            transfer.LocalDeviceState.Swap(k =>
            {
                var newOptions = new ContextPlayerOptions();
                var options = transfer.Data.Options;
                if (options.HasRepeatingContext)
                    newOptions.RepeatingContext = options.RepeatingContext;
                if (options.HasRepeatingTrack)
                    newOptions.RepeatingTrack = options.RepeatingTrack;
                if (options.HasShufflingContext)
                    newOptions.ShufflingContext = options.ShufflingContext;
                k.State.Options = newOptions;

                var newPlayOrigin = new PlayOrigin();
                var session = transfer.Data.CurrentSession;
                if (session.PlayOrigin.HasDeviceIdentifier)
                    newPlayOrigin.DeviceIdentifier = session.PlayOrigin.DeviceIdentifier;
                if (session.PlayOrigin.HasFeatureIdentifier)
                    newPlayOrigin.FeatureIdentifier = session.PlayOrigin.FeatureIdentifier;
                if (session.PlayOrigin.HasFeatureVersion)
                    newPlayOrigin.FeatureVersion = session.PlayOrigin.FeatureVersion;
                if (session.PlayOrigin.HasViewUri)
                    newPlayOrigin.ViewUri = session.PlayOrigin.ViewUri;
                if (session.PlayOrigin.HasReferrerIdentifier)
                    newPlayOrigin.ReferrerIdentifier = session.PlayOrigin.ReferrerIdentifier;
                foreach (var kv in session.PlayOrigin.FeatureClasses)
                    newPlayOrigin.FeatureClasses.Add(kv);

                k.State.PlayOrigin = newPlayOrigin;

                k.State.ContextUri = session.Context.Uri;

                if (session.Context.HasUrl)
                {
                    k.State.ContextUrl = session.Context.Url;
                }
                else
                {
                    k.State.ContextUrl = string.Empty;
                }

                k.State.ContextMetadata.Clear();
                foreach (var kv in session.Context.Metadata)
                    k.State.ContextMetadata[kv.Key] = kv.Value;

                static string GenerateSessionId()
                {
                    var bytes = new byte[16];
                    RandomNumberGenerator.Fill(bytes);
                    return Convert.ToBase64String(bytes);
                }

                var sessionId = GenerateSessionId();
                k.State.SessionId = sessionId;
                k.State.IsBuffering = true;
                return k with
                {
                    State = k.State,
                    LastMessageId = Some(transfer.MessageId),
                    LastCommandSentByDeviceId = transfer.SentByDeviceId,
                    StartedPlayingAt = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
            });
        });

        _ = Task.Run(async () => await Put(transfer.ConnectionId.ValueUnsafe(),
            transfer.LocalDeviceState,
            transfer.GetBearerFunc,
            transfer.Runtime));

        var track = transfer.Data.Playback.CurrentTrack;
        var itemId = SpotifyId.FromGid(track.Gid, AudioItemType.Track);
        var startAt = GetStartAt(transfer.Data);
        var isPaused = transfer.Data.Playback.IsPaused;

        var aff = from metadata in transfer.GetTrackFunc(itemId)
            from stream in SpotifyPlayback<RT>
                .Stream(metadata, track, transfer.AudioKeyFunc, transfer.GetBearerFunc,
                    PreferredQualityType.High, CancellationToken.None)
            from listener in WaveePlayer.Play(stream, startAt, isPaused, async t =>
            {
                StateChanged(t, transfer.LocalDeviceState);
                await Put(transfer.ConnectionId.ValueUnsafe(),
                    transfer.LocalDeviceState,
                    transfer.GetBearerFunc,
                    transfer.Runtime
                );
            }).ToAff()
            select listener;

        var tr = await aff.Run(transfer.Runtime);
        var newListener = tr.ThrowIfFail();

        return (transfer.ConnectionId, Some(newListener));
    }

    private static async Task Put(
        string connectionId,
        LanguageExt.Ref<LocalDeviceState> localDeviceState,
        Func<ValueTask<string>> getBearerFunc,
        RT runtime)
    {
        var putState = localDeviceState.Value.BuildPutState(
            PutStateReason.PlayerStateChanged, None,
            true);
        await PutState(connectionId,
            localDeviceState.Value.DeviceInfo.DeviceId,
            putState,
            getBearerFunc,
            CancellationToken.None
        ).Run(runtime);
    }

    private static void StateChanged(
        IWaveePlayerState state,
        LanguageExt.Ref<LocalDeviceState> localDeviceState)
    {
        switch (state)
        {
            case WaveePlayingState playing:
                atomic(() =>
                {
                    localDeviceState.Swap(state =>
                    {
                        if (playing.Stream is ISpotifyStream spotifyStream)
                        {
                            state.State.Timestamp =
                                (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            state.State.PositionAsOfTimestamp =
                                (long)playing.Position.TotalMilliseconds;
                            state.State.Position = 0;
                            state.State.IsBuffering = false;
                            state.State.IsPaused = false;
                            state.State.IsPlaying = true;

                            var tr = new ProvidedTrack();
                            var ctxTrack = spotifyStream.Track;
                            tr.Provider = "context";
                            if (ctxTrack.HasUid)
                                tr.Uid = ctxTrack.Uid;
                            if (ctxTrack.HasUri && !string.IsNullOrEmpty(ctxTrack.Uri))
                                tr.Uri = ctxTrack.Uri;
                            else if (ctxTrack.HasGid)
                            {
                                var id = SpotifyId.FromGid(ctxTrack.Gid, AudioItemType.Track);
                                tr.Uri = id.Uri;
                            }

                            foreach (var kv in ctxTrack.Metadata)
                                tr.Metadata[kv.Key] = kv.Value;

                            state.State.Duration = spotifyStream.Metadata.Duration;
                            state.State.Track = tr;
                            return state;
                        }
                        else
                        {
                            // Stop! We are not playing a Spotify stream
                            Debugger.Break();
                            // TODO
                            return state;
                        }
                    });
                });
                break;
        }
    }

    private static TimeSpan GetStartAt(TransferState transferState)
    {
        var isPaused = transferState.Playback.IsPaused;
        var positionAsOfTimestamp = transferState.Playback.PositionAsOfTimestamp;
        if (isPaused)
        {
            // If paused, we want to start at the current position
            return TimeSpan.FromMilliseconds(positionAsOfTimestamp);
        }

        var timestamp = transferState.Playback.Timestamp;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var diff = now - timestamp;
        var position = positionAsOfTimestamp + diff;
        return TimeSpan.FromMilliseconds(position);
    }

    public static Aff<RT, SpotifyRemoteInfo> Connect(
        SpotifyRemoteInfo remoteInfo,
        LanguageExt.Ref<Option<APWelcome>> userIdref,
        string deviceId,
        Func<ValueTask<string>> getBearerFunc,
        Func<SpotifyId, ByteString, CancellationToken, Aff<RT, Either<AesKeyError, ReadOnlyMemory<byte>>>>
            fetchAudioKeyFunc,
        Func<SpotifyId, Aff<RT, TrackOrEpisode>> getTrackFunc,
        SpotifyPlaybackConfig config,
        Action<Option<Error>, SpotifyRemoteInfo> onDisconnected,
        CancellationToken cancellationToken) =>
        from connectionId in SpotifyWebsocket<RT>.EstablishConnection(getBearerFunc, cancellationToken)
            .Map(x =>
            {
                atomic(() => remoteInfo.SpotifyConnectionId.Swap(k => x.ConnectionId));
                return x;
            })
        let localDeviceState = Ref(LocalDeviceState.New(deviceId, config.DeviceName, config.DeviceType,
            Math.Clamp(config.InitialVolume, 0, 1)))
        from cluster in PutState(
                connectionId.ConnectionId,
                deviceId,
                localDeviceState.Value.BuildPutState(PutStateReason.NewDevice, None), getBearerFunc, cancellationToken)
            .Map(x =>
            {
                atomic(() => remoteInfo.UpdateCluster(x));
                return x;
            })
        from _ in Eff<RT, Unit>((rt) =>
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    //ReadFromMessage is a blocking call, so we need to run it on a separate thread
                    var message = await SpotifyWebsocket<RT>.ReadNextMessage(connectionId.Socket)
                        .Run(rt);

                    if (message.IsFail)
                    {
                        atomic(() => remoteInfo.SpotifyConnectionId.Swap(k => None));
                        var error = message.Match(Succ: _ => throw new Exception("This should never happen"),
                            Fail: e => e);
                        onDisconnected(error, remoteInfo);
                        return;
                    }

                    var msg = message.Match(Succ: x => x, Fail: e => throw e);
                    switch (msg.Type)
                    {
                        case SpotifyWebsocketMessageType.Message:
                            var userId = userIdref.Value;
                            if (msg.Uri.StartsWith("hm://connect-state/v1/cluster"))
                            {
                                var clusterUpdate = ClusterUpdate.Parser.ParseFrom(msg.Payload.ValueUnsafe().Span);
                                atomic(() => remoteInfo.UpdateCluster(clusterUpdate.Cluster));
                            }
                            else if (msg.Uri.StartsWith(
                                         $"hm://collection/collection/{userId.ValueUnsafe().CanonicalUsername}/json"))
                            {
                                using var collectionUpdate = JsonDocument.Parse(msg.Payload.ValueUnsafe());
                                //  "["{"items":[{"type":"track","unheard":false,"addedAt":1683541347,"removed":false,"identifier":"7isOa0YWmngqAq7h3RcZj1"}]}"]"
                                var itemsString = collectionUpdate.RootElement.EnumerateArray().First().ToString();
                                using var itemsRoot = JsonDocument.Parse(itemsString);
                                var items = itemsRoot.RootElement.GetProperty("items").EnumerateArray()
                                    .Select(x => new SpotifyCollectionUpdateNotificationItem(
                                        x.GetProperty("type").GetString(),
                                        x.GetProperty("unheard").GetBoolean(),
                                        x.GetProperty("addedAt").GetInt64(),
                                        x.GetProperty("removed").GetBoolean(),
                                        x.GetProperty("identifier").GetString()
                                    )).ToSeq();

                                remoteInfo.Collection.OnNext(items);
                            }

                            break;
                        case SpotifyWebsocketMessageType.Request:
                        {
                            using var request = JsonDocument.Parse(msg.Payload.ValueUnsafe());
                            var messageId = request.RootElement.GetProperty("message_id").GetUInt32();
                            var sentByDeviceId = request.RootElement.GetProperty("sent_by_device_id").GetString();
                            var command = request.RootElement.GetProperty("command");
                            var commandEndpoint = command.GetProperty("endpoint").GetString();
                            ReadOnlyMemory<byte> data = command.GetProperty("data").GetBytesFromBase64();
                            switch (commandEndpoint)
                            {
                                case "transfer":
                                {
                                    var spotifyTransferMessage =
                                        new SpotifyTransferRequest
                                        {
                                            ConnectionId = connectionId.ConnectionId,
                                            MessageId = messageId,
                                            SentByDeviceId = sentByDeviceId,
                                            Endpoint = SpotifyRequestEndpointType.Transfer,
                                            Data = TransferState.Parser.ParseFrom(data.Span),
                                            LocalDeviceState = localDeviceState,
                                            GetBearerFunc = getBearerFunc,
                                            GetTrackFunc = getTrackFunc,
                                            AudioKeyFunc = fetchAudioKeyFunc,
                                            Runtime = rt,
                                        };
                                    await SpotifyRequests.WriteAsync(spotifyTransferMessage, cancellationToken)
                                        .ConfigureAwait(false);

                                    var reply = new
                                    {
                                        type = "reply",
                                        key = msg.Uri,
                                        payload = new
                                        {
                                            success = true.ToString().ToLower()
                                        }
                                    };
                                    ReadOnlyMemory<byte> replyJson = JsonSerializer.SerializeToUtf8Bytes(reply);
                                    var sendAff = await Ws<RT>.Write(connectionId.Socket, replyJson)
                                        .Run(rt);
                                    break;
                                }
                            }

                            break;
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning);
            return unit;
        })
        select remoteInfo;


    public static Aff<RT, Cluster> PutState(
        string connectionId,
        string deviceId,
        PutStateRequest putState,
        Func<ValueTask<string>> getBearerFunc,
        CancellationToken cancellationToken) =>
        from baseUrl in AP<RT>.FetchSpClient()
            .Map(x => $"https://{x.Host}:{x.Port}/connect-state/v1/devices/{deviceId}")
        from bearer in getBearerFunc().ToAff()
            .Map(f => new AuthenticationHeaderValue("Bearer", f))
        from headers in SuccessEff(new HashMap<string, string>()
            .Add("X-Spotify-Connection-Id", connectionId)
            .Add("accept", "gzip"))
        from body in GzipHelpers.GzipCompress(putState.ToByteArray().AsMemory())
        from response in Http<RT>.Put(baseUrl, bearer, headers, body, cancellationToken)
            .MapAsync(async c =>
            {
                c.EnsureSuccessStatusCode();
                await using var stream = await c.Content.ReadAsStreamAsync(cancellationToken);
                var l = stream.Length;
                await using var decompressedStream = GzipHelpers.GzipDecompress(stream);
                var newL = decompressedStream.Length;

                return Cluster.Parser.ParseFrom(decompressedStream);
            })
        select response;

    private interface ISpotifyRemoteMessage
    {
        Option<string> ConnectionId { get; }
        uint MessageId { get; }
        string SentByDeviceId { get; }
        SpotifyRequestEndpointType Endpoint { get; }
    }

    private record SpotifyTransferRequest : ISpotifyRemoteMessage
    {
        public required Option<string> ConnectionId { get; init; }
        public required uint MessageId { get; init; }
        public required string SentByDeviceId { get; init; }
        public required SpotifyRequestEndpointType Endpoint { get; init; }
        public required TransferState Data { get; init; }
        public required LanguageExt.Ref<LocalDeviceState> LocalDeviceState { get; init; }

        public required RT Runtime { get; init; }
        public required Func<ValueTask<string>> GetBearerFunc { get; init; }
        public required Func<SpotifyId, Aff<RT, TrackOrEpisode>> GetTrackFunc { get; init; }

        public required
            Func<SpotifyId, ByteString, CancellationToken, Aff<RT, Either<AesKeyError, ReadOnlyMemory<byte>>>>
            AudioKeyFunc { get; init; }
    }

    private enum SpotifyRequestEndpointType
    {
        Transfer
    }
}