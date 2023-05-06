using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Effects.Traits;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Remote.Infrastructure.State;
using Wavee.Spotify.Remote.Infrastructure.State.Messages;

namespace Wavee.Spotify.Remote.Infrastructure.Sys;

public static class SpotifyRemoteRuntime<RT> where RT : struct, HasCancel<RT>,
    HasWebsocket<RT>,
    HasHttp<RT>
{
    public static Aff<RT, ISpotifyRemoteClient> Connect(
        string deviceId,
        string deviceName,
        DeviceType deviceType,
        string spClientUrl,
        Func<Task<string>> getBearer) =>
        from ws in ConnectToWebsocket(getBearer)
        from connectionId in Ws<RT>.Read(ws)
            .Map(c =>
            {
                using var reader = JsonDocument.Parse(c);
                var connId = reader.RootElement.GetProperty("headers")
                    .GetProperty("Spotify-Connection-Id")
                    .GetString();
                return connId;
            })
        from newRemoteState in Eff(() => SpotifyRemoteState.CreateNew(
            deviceId,
            deviceName,
            deviceType,
            connectionId))
        from cluster in PutState(spClientUrl, newRemoteState, getBearer)
        from cl in Eff<RT, SpotifyRemoteClient<RT>>(rt => new SpotifyRemoteClient<RT>(newRemoteState,
            cluster, getBearer, rt))
        from _ in SetupListeners(ws, cl)
        select (ISpotifyRemoteClient)cl;

    private static Eff<RT, Unit> SetupListeners(WebSocket ws,
        SpotifyRemoteClient<RT> client) => Eff<RT, Unit>((rt) =>
    {
        Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    var msgResult =
                        from msg in Ws<RT>.Read(ws)
                        from parsedMessage in Eff(() => SpotifyWebsocketMessage.ParseFrom(msg))
                        select parsedMessage;

                    var result = await msgResult.Run(rt);

                    var spotifyMessage = result.Match(
                        Fail: error => throw error,
                        Succ: m => m
                    );


                    switch (spotifyMessage.Type)
                    {
                        case SpotifyWebsocketMessageType.Message:
                            client.OnCluster(ClusterUpdate.Parser.ParseFrom(spotifyMessage.Payload.Span));
                            break;
                        case SpotifyWebsocketMessageType.Request:
                            var spotifyRequestCommand = SpotifyWebsocketMessage.ParseRequest(spotifyMessage.Payload);
                            var key = spotifyMessage.Uri;

                            var requestResult =
                                await client.OnRequest(key, spotifyRequestCommand)
                                    .Run(rt);

                            var responseAff = requestResult.Match(
                                Succ: didSomething => SendWebsocketResponse(ws, key, didSomething),
                                Fail: err =>
                                {
                                    Debug.WriteLine(err);
                                    return SendWebsocketResponse(ws, key, false);
                                }
                            );

                            var responseToResponse = await responseAff.Run(rt);
                            responseToResponse.Match(
                                Succ: _ =>
                                {
                                    //Done!
                                },
                                Fail: err =>
                                {
                                    Debug.WriteLine(err);
                                    throw err;
                                }
                            );
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
        });
        return unit;
    });

    private static Aff<RT, Unit> SendWebsocketResponse(
        WebSocket socket,
        string key, bool success)
    {
        var message = new
        {
            type = "reply",
            key = key,
            payload = new
            {
                success = success.ToString().ToLower()
            }
        };

        ReadOnlyMemory<byte> json = JsonSerializer.SerializeToUtf8Bytes(message);

        return Ws<RT>.Write(socket, json);
    }

    private static Aff<RT, Cluster> PutState(
        string spClientUrl,
        SpotifyRemoteState remoteState,
        Func<Task<string>> getBearer) =>
        from putStateRequest in Eff(() => remoteState.BuildPutState(PutStateReason.NewDevice, false))
        let connectionId = remoteState.ConnectionId
        let deviceId = remoteState.DeviceId
        from bearer in getBearer().ToAff()
        from gzipContent in EncodeToGzip(putStateRequest.ToByteArray().AsMemory())
        from cluster in Http<RT>.Put(
                $"{spClientUrl}/connect-state/v1/devices/{deviceId}",
                Option<AuthenticationHeaderValue>.Some(new AuthenticationHeaderValue("Bearer", bearer)),
                BuildSpotifyHeader(connectionId),
                gzipContent)
            .MapAsync(async c =>
            {
                c.EnsureSuccessStatusCode();
                await using var str = await c.Content.ReadAsStreamAsync();
                return Cluster.Parser.ParseFrom(str);
            })
        select cluster;

    private static Option<HashMap<string, string>> BuildSpotifyHeader(string connectionId)
    {
        var spotifyHeader = new HashMap<string, string>();
        return spotifyHeader.Add("X-Spotify-Connection-Id", connectionId);
    }

    private static Aff<HttpContent> EncodeToGzip(ReadOnlyMemory<byte> data) => Aff(async () =>
    {
        var compressedGzip = Compression(data);
        var gzipContent = new ByteArrayContent(compressedGzip);
        gzipContent.Headers.ContentType = new MediaTypeHeaderValue("application/protobuf");
        gzipContent.Headers.ContentEncoding.Add("gzip");
        return (HttpContent)gzipContent;
    });

    /// <summary>
    /// 중복된 많은 문자열을 용량을 줄일 때 사용
    /// , 메모리가 많이 사용되기 때문에 충분히 테스트가 되어야 하며 사전에 사용할 수 있는 최대값을 결정하고 허용된 범위 안에서만 되도록 한다.
    /// </summary>
    /// <param name="str">압축할 문자열</param>
    /// <returns>압축된 문자열</returns>
    /// <remarks>
    /// UTF-8기반 문자열 압축 - 최대 4Gb 이하만 사용
    /// , 메모리가 많이 사용되기 때문에 충분히 테스트가 되어야 하며 사전에 사용할 수 있는 최대값을 결정하고 허용된 범위 안에서만 되도록 한다.
    /// </remarks>
    /// <example> 문자열 압축
    /// <code>
    /// string str = "compress string";
    /// string compressed = Compression(str);
    /// </code>
    /// </example>
    /// <exception cref="OutOfMemoryException"></exception>
    /// <exception cref="InsufficientMemoryException">사용 가능한 메모리가 없을때 발생</exception>
    public static byte[] Compression(ReadOnlyMemory<byte> ata)
    {
        using var output = new MemoryStream();
        using (var compressor = new GZipStream(output, CompressionMode.Compress))
        {
            compressor.Write(ata.Span);
        }

        return output.ToArray();
    }

    private static Aff<RT, WebSocket> ConnectToWebsocket(
        Func<Task<string>> fetchBearer) =>
        from token in cancelToken<RT>()
        from bearer in fetchBearer().ToAff()
        from wssUrl in FetchDealer()
            .Map(f => $"wss://{f.Host}:{f.Port}?access_token={bearer}")
        from websocketClient in Ws<RT>.Connect(wssUrl)
        select websocketClient;

    const string DEALER_URL = "https://apresolve.spotify.com/?type=dealer";

    private static Aff<RT, (string Host, ushort Port)> FetchDealer() =>
        from httpResponse in Http<RT>.Get(DEALER_URL, Option<AuthenticationHeaderValue>.None,
            Option<HashMap<string, string>>.None)
        from _ in Eff((() =>
        {
            httpResponse.EnsureSuccessStatusCode();
            return unit;
        }))
        from jsonContent in httpResponse.Content.ReadFromJsonAsync<ApResolveData>().ToAff()
            .Map(x => x.Dealer.First())
        from splitted in Eff(() =>
        {
            var split = jsonContent.Split(":", 2);
            return (split[0], ushort.Parse(split[1]));
        })
        select splitted;

    private readonly record struct ApResolveData(
        [property: JsonPropertyName("dealer")] string[] Dealer);
}