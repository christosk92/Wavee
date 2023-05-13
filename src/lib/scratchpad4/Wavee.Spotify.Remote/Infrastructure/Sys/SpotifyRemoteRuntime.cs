using System.Net.Http.Headers;
using System.Net.WebSockets;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.Common;
using Wavee.Core.Infrastructure.Sys.IO;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Spotify.Remote.Helpers;
using Wavee.Spotify.Remote.Models;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Remote.Infrastructure.Sys;

public static class SpotifyRemoteRuntime<R> where R : struct, HasWebsocket<R>, HasHttp<R>
{
    public static Aff<R, SpotifyRemoteConnection<R>> Create(
        string spClientUrl,
        string dealerHostUrl,
        ushort dealerHostPort,
        string deviceId,
        string deviceName,
        DeviceType deviceType,
        Func<ValueTask<string>> getAccessToken,
        CancellationToken ct = default)
    {
        var connection = new SpotifyRemoteConnection<R>();
        return
            from deviceState in ConnectWithDisconnectionLogic(
                spClientUrl,
                dealerHostUrl,
                dealerHostPort,
                deviceId,
                deviceName,
                deviceType,
                getAccessToken,
                connection,
                ct)
            select connection;
    }

    private static Aff<R, SpotifyLocalDeviceState> ConnectWithDisconnectionLogic(
        string spClientUrl,
        string dealerHostUrl,
        ushort dealerHostPort,
        string deviceId,
        string deviceName,
        DeviceType deviceType,
        Func<ValueTask<string>> getAccessToken,
        SpotifyRemoteConnection<R> connection,
        CancellationToken ct) =>
        from accessToken in getAccessToken().ToAff()
        let wsUrl = $"wss://{dealerHostUrl}:{dealerHostPort}?access_token={accessToken}"
        from ws in Ws<R>.Connect(wsUrl, ct)
        from connId in ReadConnectionId(ws, ct)
        from _ in Eff(() => connection.SwapConnectionId(connId))
        let emptyState = SpotifyLocalDeviceState.New(deviceId, deviceName, deviceType)
        from initialCluster in PutState(
            spClientUrl,
            emptyState.BuildPutState(
                PutStateReason.NewDevice, None),
            connId,
            getAccessToken,
            ct
        )
        from __ in Eff(() =>
        {
            connection.SwapLatestCluster(initialCluster);
            return unit;
        })
        from ___ in Eff(() => connection.SwapDeviceState(emptyState))
        from ____ in Aff<R, Unit>(async (r) =>
        {
            await Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    var aff =
                        from msg in ReadNextMessage(ws, ct)
                        from _ in Eff(() => connection.DispatchMessage(msg))
                        select unit;

                    var result = await aff.Run(r);
                    if (result.IsFail)
                    {
                        break;
                    }
                }
                
                //TODO: Attempt to reconnect
            }, ct);
            return unit;
        })
        select emptyState;

    private static Aff<R, Cluster>
        PutState(
            string baseUrl,
            PutStateRequest putState,
            string connId,
            Func<ValueTask<string>> getAccessToken,
            CancellationToken ct) =>
        from bearer in getAccessToken().ToAff()
            .Map(f => new AuthenticationHeaderValue("Bearer", f))
        from headers in SuccessEff(new HashMap<string, string>()
            .Add("X-Spotify-Connection-Id", connId)
            .Add("accept", "gzip"))
        let finalUrl = $"{baseUrl}/connect-state/v1/devices/{putState.Device.DeviceInfo.DeviceId}"
        from body in GzipHelpers.GzipCompress(putState.ToByteArray().AsMemory())
        from response in Http<R>.Put(finalUrl, bearer, headers, body, ct)
            .MapAsync(async c =>
            {
                c.EnsureSuccessStatusCode();
                await using var stream = await c.Content.ReadAsStreamAsync(ct);
                await using var decompressedStream = GzipHelpers.GzipDecompress(stream);
                return Cluster.Parser.ParseFrom(decompressedStream);
            })
        select response;

    private static Aff<R, string> ReadConnectionId(WebSocket ws, CancellationToken ct) =>
        from connectionId in ReadNextMessage(ws, ct)
            .Map(msg =>
            {
                return msg.Headers
                    .Find("Spotify-Connection-Id")
                    .Match(
                        Some: x => x,
                        None: () => throw new Exception("Connection id not found in websocket message"));
            })
        select connectionId;

    private static Aff<R, SpotifyWebsocketMessage>
        ReadNextMessage(WebSocket websocket, CancellationToken ct = default) =>
        from message in Ws<R>.Read(websocket, ct)
        select SpotifyWebsocketMessage.ParseFrom(message);
}