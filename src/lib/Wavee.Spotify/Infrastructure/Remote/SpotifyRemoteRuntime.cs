using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text.Json;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using Wavee.Core.Infrastructure.IO;
using Wavee.Spotify.Helpers;
using Wavee.Spotify.Infrastructure.ApResolver;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Mercury.Token;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.Spotify.Infrastructure.Remote.Messaging;

namespace Wavee.Spotify.Infrastructure.Remote;

internal static class SpotifyRemoteRuntime
{
    public static async Task Start(SpotifyRemoteConnection connection,
        MercuryClient mercuryClient,
        TokenClient tokenClient,
        SpotifyRemoteConfig config,
        string deviceId,
        Action<Exception> onLost)
    {
        try
        {
            var dealer = await ApResolve.GetDealer(CancellationToken.None);
            var token = await tokenClient.GetToken();
            
            var wsUrl = $"wss://{dealer.host}:{dealer.port}?access_token={token}";
            var ws = await WebsocketIO.Connect(wsUrl, CancellationToken.None);
            var connId = await ReadConnectionId(ws);
            connection.SwapConnectionId(connId);

            var spClient = await ApResolve.GetSpClient(CancellationToken.None);
            var spClientUrl = $"https://{spClient.host}:{spClient.port}";
            var emptyState = SpotifyLocalDeviceState.New(deviceId, config.DeviceName, config.DeviceType);
            var initialCluster = await PutState(deviceId,
                spClientUrl,
                emptyState.BuildPutState(PutStateReason.NewDevice, Option<TimeSpan>.None),
                connId,
                tokenClient,
                CancellationToken.None);
            connection.SwapLatestCluster(initialCluster);

            await Task.Factory.StartNew(async () =>
            {
                await StartMessageReader(ws, connection, mercuryClient, tokenClient, config, deviceId,
                    onLost,
                    CancellationToken.None);
            }, TaskCreationOptions.LongRunning);

            await Task.Factory.StartNew(async () => { await StartPingPong(ws); }, TaskCreationOptions.LongRunning);
        }
        catch (Exception e)
        {
            onLost(e);
            Console.WriteLine(e);
            //try again
            await Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                await Start(connection, mercuryClient, tokenClient, config, deviceId, onLost);
            });
        }
    }

    private static async Task StartPingPong(ClientWebSocket ws)
    {
        while (true)
        {
            try
            {
                var pingMessage = new
                {
                    type = "ping"
                };
                ReadOnlyMemory<byte> json = JsonSerializer.SerializeToUtf8Bytes(pingMessage);
                await ws.SendAsync(json, WebSocketMessageType.Text, true, CancellationToken.None);
                await Task.Delay(TimeSpan.FromSeconds(25));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    private static async Task StartMessageReader(
        ClientWebSocket ws,
        SpotifyRemoteConnection connection,
        MercuryClient mercuryClient,
        TokenClient tokenClient,
        SpotifyRemoteConfig config,
        string deviceId,
        Action<Exception> onLost,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            SpotifyWebsocketMessage message = default;
            try
            {
                message = await ReadNextMessage(ws, ct);
            }
            catch (Exception e)
            {
                if (e is not JsonException)
                {
                    ws.Dispose();

                    Console.WriteLine(e);
                    //try again
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(3), ct);
                        await Start(connection, mercuryClient, tokenClient, config, deviceId, onLost);
                    });
                    break;
                }
                else
                {
                    Debug.WriteLine(e);
                }
            }

            connection.DispatchMessage(message);
            if (message.Type is SpotifyWebsocketMessageType.Request)
            {
                var datareply = new
                {
                    type = "reply",
                    key = message.Uri,
                    payload = new
                    {
                        success = true.ToString().ToLower()
                    }
                };
                ReadOnlyMemory<byte> payload = JsonSerializer.SerializeToUtf8Bytes(datareply);
                await ws.SendAsync(payload, WebSocketMessageType.Text, true, ct);
            }
        }
    }

    public static async Task<Cluster> PutState(
        string deviceId,
        string spClientUrl,
        PutStateRequest putState,
        string connId,
        TokenClient tokenClient, CancellationToken ct)
    {
        var token = await tokenClient.GetToken(ct);
        var bearerHeader = new AuthenticationHeaderValue("Bearer", token);
        var headers = new HashMap<string, string>()
            .Add("X-Spotify-Connection-Id", connId)
            .Add("accept", "gzip");
        var finalUrl = $"{spClientUrl}/connect-state/v1/devices/{deviceId}";
        using var body = GzipHelpers.GzipCompress(putState.ToByteArray().AsMemory());
        using var response = await HttpIO.Put(finalUrl, bearerHeader, headers, body, ct);
        response.EnsureSuccessStatusCode();
        await using var responseStream = await response.Content.ReadAsStreamAsync(ct);
        using var gzip = GzipHelpers.GzipDecompress(responseStream);
        gzip.Position = 0;
        var cluster = Cluster.Parser.ParseFrom(gzip);
        return cluster;
    }

    private static async Task<SpotifyWebsocketMessage> ReadNextMessage(ClientWebSocket ws, CancellationToken ct)
    {
        var message = await WebsocketIO.Receive(ws, ct);
        return SpotifyWebsocketMessage.ParseFrom(message);
    }

    private static async Task<string> ReadConnectionId(ClientWebSocket ws)
    {
        var message = await ReadNextMessage(ws, CancellationToken.None);
        return message
            .Headers
            .Find("Spotify-Connection-Id")
            .IfNone(string.Empty);
    }

    public static async Task InvokeCommandOnRemoteDevice(object command,
        string sp,
        string toDeviceId,
        string fromDeviceId,
        TokenClient tokenClient, CancellationToken ct)
    {
        // https://gae2-spclient.spotify.com/connect-state/v1/player/command/from/1b5327f43e39a20de0ec1dcafa3466f082e28348/to/342d539fa2bc06a1cfa4d03d67c3d90513b75879
        var url = $"{sp}/connect-state/v1/player/command/from/{fromDeviceId}/to/{toDeviceId}";
        var token = await tokenClient.GetToken(ct);
        var bearerHeader = new AuthenticationHeaderValue("Bearer", token);
        var json = JsonSerializer.SerializeToUtf8Bytes(command);
        using var content = new ByteArrayContent(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var response = await HttpIO.Post(url, bearerHeader, content, ct);
    }

    public static async Task SetVolume(object command, string sp,
        string deviceId,
        string to,
        TokenClient tokenClient, CancellationToken none)
    {
        // /connect-state/v1/connect/volume/from/132709aee590df96ee47ff46704b1926e68b1362/to/1113456521558a98ef06139acd67b36610a8edf4
        var url = $"{sp}/connect-state/v1/connect/volume/from/{deviceId}/to/{to}";
        var token = await tokenClient.GetToken(none);
        var bearerHeader = new AuthenticationHeaderValue("Bearer", token);
        var json = JsonSerializer.SerializeToUtf8Bytes(command);
        using var content = new ByteArrayContent(json);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var response = await HttpIO.Put(url, bearerHeader, LanguageExt.HashMap<string, string>.Empty, content, CancellationToken.None);
    }
}