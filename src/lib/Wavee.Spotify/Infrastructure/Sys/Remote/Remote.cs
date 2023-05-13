using System.Net.WebSockets;
using System.Text.Json;
using Eum.Spotify.connectstate;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.Remote;

namespace Wavee.Spotify.Infrastructure.Sys.Remote;

internal static class Remote<RT> where RT : struct, HasWebsocket<RT>, HasHttp<RT>, HasAudioOutput<RT>
{
    public static Aff<RT, WebSocket> Connect(Func<ValueTask<string>> getBearer, CancellationToken ct) =>
        from bearer in getBearer().ToAff()
        from dealer in AP<RT>.FetchDealer().Map(x => $"wss://{x.Host}:{x.Port}?access_token={bearer}")
        from websocket in Ws<RT>.Connect(dealer, ct)
        select websocket;

    public static Aff<RT, (Ref<LocalDeviceState> LocalState, Cluster RemoteState)> Hello(
        Option<LocalDeviceState> localState,
        WebSocket websocket,
        string deviceId,
        string deviceName,
        DeviceType deviceType,
        Func<ValueTask<string>> getBearer,
        CancellationToken ct) =>
        from connectionId in ReadNextMessage(websocket, ct)
            .Map(msg =>
            {
                return msg.Headers
                    .Find("Spotify-Connection-Id")
                    .Match(
                        Some: x => x,
                        None: () => throw new Exception("Connection id not found in websocket message"));
            })
        from hello in RemoteState<RT>.Hello(localState, connectionId, deviceId,
            deviceName,
            deviceType,
            getBearer, ct)
        select hello;

    /// <summary>
    /// Reads the next message from the websocket.
    /// </summary>
    /// <param name="websocket">
    /// The websocket to read from.
    /// </param>
    /// <param name="ct">
    /// A cancellation token that can be used to cancel the read operation.
    /// </param>
    /// <returns>
    /// A message from the websocket.
    /// </returns>
    public static Aff<RT, SpotifyWebsocketMessage>
        ReadNextMessage(WebSocket websocket, CancellationToken ct = default) =>
        from message in Ws<RT>.Read(websocket, ct)
        select SpotifyWebsocketMessage.ParseFrom(message);

    public static Aff<RT, Unit> ListenForMessages(WebSocket websocket,
        Ref<Option<Cluster>> remoteClusterRef,
        Func<ValueTask<string>> getBearerFunc,
        Ref<LocalDeviceState> localDeviceState,
        Func<SpotifyWebsocketMessage, LocalDeviceState, Task<LocalDeviceState>> onRequest,
        CancellationToken ct) =>
        from message in ReadNextMessage(websocket, ct)
        from newState in message.Type switch
        {
            SpotifyWebsocketMessageType.Message => HandleMessage(message, remoteClusterRef,
                getBearerFunc,
                localDeviceState),
            SpotifyWebsocketMessageType.Request =>
                from deviceState in HandleRequest(message, remoteClusterRef, getBearerFunc, localDeviceState, onRequest,
                    ct)
                let websocketResponse = BuildRequestResponse(message.Uri)
                from _ in Ws<RT>.Write(websocket, websocketResponse)
                select deviceState,
        }
        select newState;

    private static ReadOnlyMemory<byte> BuildRequestResponse(string key)
    {
        var reply = new
        {
            type = "reply",
            key = key,
            payload = new
            {
                success = true.ToString().ToLower()
            }
        };
        return JsonSerializer.SerializeToUtf8Bytes(reply);
    }

    private static Aff<RT, Unit> HandleRequest(SpotifyWebsocketMessage message,
        Ref<Option<Cluster>> remoteClusterRef,
        Func<ValueTask<string>> getBearerFunc,
        Ref<LocalDeviceState> localDeviceState,
        Func<SpotifyWebsocketMessage, LocalDeviceState, Task<LocalDeviceState>> onRequest,
        CancellationToken ct)
    {
        atomic(() => remoteClusterRef.Swap(_ => Option<Cluster>.None));

        return
            from newLocalState in onRequest(message, localDeviceState)
                .Map(f => atomic(() => localDeviceState.Swap(_ => f)))
                .ToAff()
            let putState = newLocalState.BuildPutState(PutStateReason.NewDevice,
                0.5,
                Option<TimeSpan>.None)
            from _ in RemoteState<RT>.Put(putState, newLocalState.DeviceId, newLocalState.ConnectionId,
                getBearerFunc, ct)
            select unit;
    }

    private static Aff<RT, Unit> HandleMessage(SpotifyWebsocketMessage message,
        Ref<Option<Cluster>> remoteClusterRef,
        Func<ValueTask<string>> getBearerFunc,
        Ref<LocalDeviceState> localDeviceState)
    {
        if (message.Uri.StartsWith("hm://connect-state/v1/cluster"))
        {
            var clusterUpdate = ClusterUpdate.Parser.ParseFrom(message.Payload.ValueUnsafe().Span);
            atomic(() => remoteClusterRef.Swap(_ => clusterUpdate.Cluster));
        }
        else if (message.Uri.Equals("hm://connect-state/v1/connect/volume"))
        {
            var vl = SetVolumeCommand.Parser.ParseFrom(message.Payload.ValueUnsafe().Span);
            var frac = (double)vl.Volume / ushort.MaxValue;

            return
                from _ in AudioOutput<RT>.SetVolume(frac)
                from d in SuccessEff(
                    atomic(() => localDeviceState.Swap(f => f.FromVolume(vl.CommandOptions.MessageId))))
                from pos in AudioOutput<RT>.Position()
                from ___ in RemoteState<RT>.Put(d.BuildPutState(PutStateReason.PlayerStateChanged,
                        frac,
                        pos),
                    d.DeviceId,
                    d.ConnectionId,
                    getBearerFunc,
                    CancellationToken.None)
                select unit;
        }

        return SuccessEff(unit);
    }
}