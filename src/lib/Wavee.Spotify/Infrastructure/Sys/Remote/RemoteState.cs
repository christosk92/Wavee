using System.Net.Http.Headers;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Clients.Remote;
using Wavee.Spotify.Helpers;

namespace Wavee.Spotify.Infrastructure.Sys.Remote;

public static class RemoteState<RT> where RT : struct, HasWebsocket<RT>, HasHttp<RT>, HasAudioOutput<RT>
{
    public static Aff<RT, (Ref<LocalDeviceState> LocalState, Cluster RemoteState)> Hello(
        Option<LocalDeviceState> localDeviceState, string connectionId,
        string deviceId,
        string deviceName,
        DeviceType deviceType,
        Func<ValueTask<string>> getBearer,
        CancellationToken ct = default)
    {
        var newState =
            Ref(localDeviceState.Match(
                Some: r => r with
                {
                    DeviceId = deviceId,
                    DeviceName = deviceName,
                    DeviceType = deviceType,
                    ConnectionId = connectionId
                }, None: () => LocalDeviceState.New(connectionId, deviceId, deviceName, deviceType)
            ));

        return
            from vol in AudioOutput<RT>.Volume()
            from putState in Eff(() => newState.Value.BuildPutState(PutStateReason.NewDevice,
                vol,
                None))
            from putStateResult in Put(putState,
                newState.Value.DeviceId,
                newState.Value.ConnectionId,
                getBearer, ct)
            select (newState, putStateResult);
    }

    public static Aff<RT, Cluster> Put(
        PutStateRequest putState,
        string deviceId,
        string connectionId,
        Func<ValueTask<string>> getBearer,
        CancellationToken ct) =>
        from baseUrl in AP<RT>.FetchSpClient()
            .Map(x => $"https://{x.Host}:{x.Port}/connect-state/v1/devices/{deviceId}")
        from bearer in getBearer().ToAff()
            .Map(f => new AuthenticationHeaderValue("Bearer", f))
        from headers in SuccessEff(new HashMap<string, string>()
            .Add("X-Spotify-Connection-Id", connectionId)
            .Add("accept", "gzip"))
        from body in GzipHelpers.GzipCompress(putState.ToByteArray().AsMemory())
        from response in Http<RT>.Put(baseUrl, bearer, headers, body, ct)
            .MapAsync(async c =>
            {
                c.EnsureSuccessStatusCode();
                await using var stream = await c.Content.ReadAsStreamAsync(ct);
                var l = stream.Length;
                await using var decompressedStream = GzipHelpers.GzipDecompress(stream);
                var newL = decompressedStream.Length;

                return Cluster.Parser.ParseFrom(decompressedStream);
            })
        select response;
}