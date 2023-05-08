using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Channels;
using CommunityToolkit.HighPerformance;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Eum.Spotify.transfer;
using Google.Protobuf;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Common;
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Player;
using Wavee.Spotify.Playback;
using Wavee.Spotify.Playback.Streams;
using Wavee.Spotify.Playback.Sys;
using Wavee.Spotify.Remote.Sys.Connection;
using Wavee.Spotify.Sys;
using Wavee.Spotify.Sys.AudioKey;
using Wavee.Spotify.Sys.Common;
using Wavee.Spotify.Sys.Connection;
using Wavee.Spotify.Sys.Metadata;
using Wavee.Spotify.Sys.Tokens;

namespace Wavee.Spotify.Remote;

public static class SpotifyRemoteClient
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="connectionInfo"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="???"></exception>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<SpotifyRemoteInfo> ConnectRemote(
        this SpotifyConnectionInfo connectionInfo,
        SpotifyPlaybackConfig config,
        CancellationToken cancellationToken = default)
    {
        var deviceId = connectionInfo.Deviceid;
        var getBearerFunc = () => connectionInfo.FetchAccessToken();
        var usernameRef = connectionInfo.WelcomeMessage;
        SpotifyRemoteInfo remoteInfo = new SpotifyRemoteInfo();


        Aff<WaveeRuntime, TrackOrEpisode> GetTrackFunc(SpotifyId id)
        {
            return GetTrackOrEpisode(connectionInfo, id);
        }

        var connectionId = await SpotifyRemote<WaveeRuntime>.Connect(remoteInfo,
            usernameRef,
            deviceId,
            getBearerFunc,
            connectionInfo.GetAudioKey<WaveeRuntime>,
            GetTrackFunc,
            config,
            (err, info) => OnDisconnected(deviceId, usernameRef,
                GetTrackFunc,
                connectionInfo.GetAudioKey<WaveeRuntime>,
                getBearerFunc, config, err, info),
            cancellationToken).Run(WaveeCore.Runtime);

        return connectionId
            .Match(
                Succ: g =>
                {
                    remoteInfo = g;
                    return g;
                },
                Fail: e => throw e
            );
    }

    private static Aff<WaveeRuntime, TrackOrEpisode> GetTrackOrEpisode(SpotifyConnectionInfo connectionInfo,
        SpotifyId id)
    {
        return id.Type switch
        {
            AudioItemType.Track => connectionInfo.GetTrack(id.ToHex(), CancellationToken.None)
                .Map(x => new TrackOrEpisode(Right(x)))
                .ToAff(),
            AudioItemType.Episode => connectionInfo.GetEpisode(id.ToHex(), CancellationToken.None)
                .Map(x => new TrackOrEpisode(Left(x)))
                .ToAff(),
            _ => FailEff<TrackOrEpisode>(new Exception("Invalid id type"))
        };
    }

    private static async void OnDisconnected(
        string deviceId,
        LanguageExt.Ref<Option<APWelcome>> userIdref,
        Func<SpotifyId, Aff<WaveeRuntime, TrackOrEpisode>> getTrackFunc,
        Func<SpotifyId, ByteString, CancellationToken, Aff<WaveeRuntime, Either<AesKeyError, ReadOnlyMemory<byte>>>>
            getAudioKeyFunc,
        Func<ValueTask<string>> getBearerFunc,
        SpotifyPlaybackConfig config,
        Option<Error> error,
        SpotifyRemoteInfo remoteInfo)
    {
        if (error.IsSome)
        {
            //probably need to reconnect
            bool connected = false;
            while (!connected)
            {
                await Task.Delay(3000);
                var newconnId = await SpotifyRemote<WaveeRuntime>.Connect(
                        remoteInfo,
                        userIdref,
                        deviceId,
                        getBearerFunc,
                        getAudioKeyFunc,
                        getTrackFunc,
                        config,
                        (err, info) => OnDisconnected(deviceId, userIdref,
                            getTrackFunc,
                            getAudioKeyFunc,
                            getBearerFunc,
                            config, err, info),
                        CancellationToken.None)
                    .Run(WaveeCore.Runtime);
                if (newconnId.IsSucc)
                {
                    var newconn = newconnId.Match(Succ: x => x, Fail: e => throw e);
                    connected = true;
                    atomic(() => remoteInfo.SpotifyConnectionId.Swap(k => newconn.ConnectionId));
                }
                else
                {
                    var err = newconnId.Match(Succ: x => throw new Exception(), Fail: e => e);
                    Debug.WriteLine($"Failed to reconnect: {err}");
                }
            }
        }
    }
}

internal static class SpotifyRemote<RT> where RT : struct, HasWebsocket<RT>, HasHttp<RT>, HasAudioOutput<RT>
{
    // internal readonly record struct SpotifyRequestMessage<RT>(
//     Option<string> ConnectionId,
//     uint MessageId,
//     string SentByDeviceId,
//     SpotifyRequestEndpointType Endpoint,
//     ReadOnlyMemory<byte> Data, 
//     LanguageExt.Ref<LocalDeviceState> LocalDeviceState);
    private interface ISpotifyRemoteMessage
    {
        Option<string> ConnectionId { get; }
        uint MessageId { get; }
        string SentByDeviceId { get; }
        SpotifyRequestEndpointType Endpoint { get; }
    }

    internal record SpotifyTransferRequest : ISpotifyRemoteMessage
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

    private static readonly ChannelWriter<ISpotifyRemoteMessage> SpotifyRequests;

    static SpotifyRemote()
    {
        var spotifyRequests = Channel.CreateUnbounded<ISpotifyRemoteMessage>();
        SpotifyRequests = spotifyRequests.Writer;

        Task.Factory.StartNew(async () =>
        {
            Option<string> activeConnectionId = None;
            Option<IDisposable> playerSession = None;
            await foreach (var message in spotifyRequests.Reader.ReadAllAsync())
            {
                if (!activeConnectionId.IsNone)
                {
                    if (message.ConnectionId.IsNone || (message.ConnectionId != activeConnectionId.ValueUnsafe()))
                    {
                        //clear state
                        //todo
                        Debugger.Break();
                    }
                }

                activeConnectionId = message.ConnectionId;
                switch (message)
                {
                    case SpotifyTransferRequest transfer:
                    {
                        var transferState = transfer.Data;
                        var playback = transferState.Playback;
                        var options = transferState.Options;
                        var session = transferState.CurrentSession;
                        var isPlayingQueue = transferState.Queue.IsPlayingQueue;

                        atomic(() =>
                        {
                            //base swap
                            //all track info will be handled by the player events
                            transfer.LocalDeviceState.Swap(k =>
                            {
                                var newOptions = new ContextPlayerOptions();
                                if (options.HasRepeatingContext)
                                    newOptions.RepeatingContext = options.RepeatingContext;
                                if (options.HasRepeatingTrack)
                                    newOptions.RepeatingTrack = options.RepeatingTrack;
                                if (options.HasShufflingContext)
                                    newOptions.ShufflingContext = options.ShufflingContext;
                                k.State.Options = newOptions;

                                var newPlayOrigin = new PlayOrigin();
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
                                    //no padding
                                    return Convert.ToBase64String(bytes).TrimEnd('=');
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

                        async Task Put()
                        {
                            var putState = transfer.LocalDeviceState.Value.BuildPutState(
                                PutStateReason.PlayerStateChanged, None,
                                true);
                            await PutState(transfer.ConnectionId.ValueUnsafe(),
                                transfer.LocalDeviceState.Value.DeviceInfo.DeviceId,
                                putState,
                                transfer.GetBearerFunc,
                                CancellationToken.None
                            ).Run(transfer.Runtime);
                        }
                        _ = Put();

                        var track = playback.CurrentTrack;
                        var itemId = SpotifyId.FromGid(track.Gid, AudioItemType.Track);


                        static TimeSpan GetStartAt(TransferState transferState)
                        {
                            var isPaused = transferState.Playback.IsPaused;
                            var positionAsOfTimestamp = transferState.Playback.PositionAsOfTimestamp;
                            if (isPaused)
                            {
                                //if paused, we want to start at the current position
                                return TimeSpan.FromMilliseconds(positionAsOfTimestamp);
                            }

                            var timestamp = transferState.Playback.Timestamp;
                            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            var diff = now - timestamp;
                            var position = positionAsOfTimestamp + diff;
                            return TimeSpan.FromMilliseconds(position);
                        }

                        var startAt = GetStartAt(transferState);
                        var isPaused = playback.IsPaused;

                        async void StateChanged(IWaveePlayerState obj)
                        {
                            switch (obj)
                            {
                                case WaveePlayingState playing:
                                    atomic(() =>
                                    {
                                        transfer.LocalDeviceState.Swap(state =>
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
                                                if(ctxTrack.HasUid)
                                                    tr.Uid = ctxTrack.Uid;
                                                if(ctxTrack.HasUri && !string.IsNullOrEmpty(ctxTrack.Uri))
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
                                                //stop !!! we are not playing a spotify stream
                                                Debugger.Break();
                                                //todo
                                                return state;
                                            }
                                        });
                                    });
                                    await Put();
                                    break;
                            }
                        }

                        var aff =
                            from metadata in transfer.GetTrackFunc(itemId)
                            from stream in SpotifyPlayback<RT>
                                .Stream(metadata, track, transfer.AudioKeyFunc, transfer.GetBearerFunc,
                                    PreferredQualityType.High, CancellationToken.None)
                            from listener in WaveePlayer.Play(stream, startAt, isPaused, StateChanged).ToAff()
                            select listener;
                        var tr = await aff.Run(transfer.Runtime);
                        var newListener = tr.ThrowIfFail();
                        playerSession = Some(newListener);
                        break;
                    }
                }
            }
        }, TaskCreationOptions.LongRunning);
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
                                    //echo back
                                    //var reply =
                                    //$"{{\"type\":\"reply\", \"key\": \"{key.ToLower()}\", \"payload\": {{\"success\": {success.ToString().ToLowerInvariant()}}}}}";
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
}

internal enum SpotifyRequestEndpointType
{
    Transfer
}

internal static class GzipHelpers
{
    internal static Eff<StreamContent> GzipCompress(ReadOnlyMemory<byte> data)
    {
        return Eff(() =>
        {
            using var inputStream = data.AsStream();
            if (inputStream.Position == inputStream.Length)
            {
                inputStream.Seek(0, SeekOrigin.Begin);
            }

            var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.SmallestSize, true))
            {
                inputStream.CopyTo(gzipStream);
            }

            inputStream.Close();

            compressedStream.Seek(0, SeekOrigin.Begin);
            var strContent = new StreamContent(compressedStream);
            strContent.Headers.ContentType = new MediaTypeHeaderValue("application/protobuf");
            strContent.Headers.ContentEncoding.Add("gzip");
            strContent.Headers.ContentLength = compressedStream.Length;
            return strContent;
        });
    }

    internal static MemoryStream GzipDecompress(Stream compressedStream)
    {
        if (compressedStream.Position == compressedStream.Length)
        {
            compressedStream.Seek(0, SeekOrigin.Begin);
        }

        var uncompressedStream = new MemoryStream(GetGzipUncompressedLength(compressedStream));
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false);
        gzipStream.CopyTo(uncompressedStream);

        uncompressedStream.Seek(0, SeekOrigin.Begin);
        return uncompressedStream;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetGzipUncompressedLength(ReadOnlyMemory<byte> compressedData)
    {
        return BitConverter.ToInt32(compressedData.Slice(compressedData.Length - 4, 4).Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetGzipUncompressedLength(Stream stream)
    {
        Span<byte> uncompressedLength = stackalloc byte[4];
        stream.Position = stream.Length - 4;
        stream.Read(uncompressedLength);
        stream.Seek(0, SeekOrigin.Begin);
        return BitConverter.ToInt32(uncompressedLength);
    }
}

public readonly record struct SpotifyPlaybackConfig(string DeviceName, DeviceType DeviceType, float InitialVolume,
    PreferredQualityType PreferredQuality);

public sealed class SpotifyRemoteInfo
{
    internal LanguageExt.Ref<Option<string>> SpotifyConnectionId = Ref<Option<string>>(None);
    internal LanguageExt.Ref<Cluster> Cluster = Ref<Cluster>(new Cluster());
    internal Subject<Seq<SpotifyCollectionUpdateNotificationItem>> Collection = new();

    public Option<Cluster> ClusterInfo => Cluster.Value;
    public Option<string> ConnectionId => SpotifyConnectionId.Value;

    public IObservable<Cluster> ClusterChanged => Cluster.OnChange();
    public IObservable<Seq<SpotifyCollectionUpdateNotificationItem>> CollectionChanged => Collection;
    internal void UpdateCluster(Cluster cluster) => atomic(() => Cluster.Swap(_ => cluster));
}

public readonly record struct SpotifyCollectionUpdateNotificationItem(string Type, bool Unheard, long AddedAt,
    bool Removed, string Identifier);