using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Eum.Spotify;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Sys;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Configs;
using Wavee.Spotify.Infrastructure;
using Wavee.Spotify.Infrastructure.Authentication;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Crypto;
using Wavee.Spotify.Infrastructure.Sys;

namespace Wavee.Spotify;

public static class SpotifyClient
{
    public static async Task<ISpotifyConnection> Create(
        LoginCredentials credentials,
        SpotifyConfig config,
        Option<ILogger> logger,
        CancellationToken ct = default)
    {
        _ = WaveeCore.Runtime;
        atomic(() => WaveeCore.Logger.Swap(_ => logger));
        //these are not related

        //this one is used from the tcp connection and dispatches them to ConnectionConsumer
        //so tcp -> consumer
        var coreChannel = Channel.CreateUnbounded<SpotifyPacket>();

        //this one is used to write to the tcp connection, and are read from ConnectionProducer
        //so producer -> tcp
        var coreWriteChannel = Channel.CreateUnbounded<SpotifyPacket>();
        var connectionId = Guid.NewGuid();

        var deviceId = Guid.NewGuid().ToString();
        var newInfo = new InternalSpotifyConnectionInfo
        {
            ConnectionId = connectionId,
            Deviceid = deviceId,
            Config = config,
        };

        var aff = from info in SpotifyClientRuntime<WaveeRuntime>.AuthenticateWithoutConnectionId(
                connectionInfo: newInfo,
                writer: coreChannel.Writer,
                reader: coreWriteChannel.Reader,
                credentials: credentials,
                deviceId: deviceId,
                ct: ct)
            let connection = new SpotifyConnection<WaveeRuntime>(info,
                coreChannel.Reader,
                coreWriteChannel.Writer,
                WaveeCore.Runtime)
            select connection;

        var result = await aff.Run(WaveeCore.Runtime);

        return result.ThrowIfFail();
    }
}

internal static class SpotifyClientRuntime<RT> where RT : struct, HasTCP<RT>, HasHttp<RT>, HasLog<RT>
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<RT, InternalSpotifyConnectionInfo> AuthenticateWithoutConnectionId(
        InternalSpotifyConnectionInfo connectionInfo,
        ChannelWriter<SpotifyPacket> writer,
        ChannelReader<SpotifyPacket> reader,
        LoginCredentials credentials,
        string deviceId,
        CancellationToken ct)
    {
        return
            from l in Log<RT>.logInfo("Authenticating...")
            from connectionResult in ConnectAndAuthenticate(credentials, deviceId, ct)
            from l2 in Log<RT>.logInfo($"Authenticated as {connectionResult.Item1.CanonicalUsername} !")
            let newConnectionInfo = connectionInfo.With(connectionResult.Item1)
            from _ in StartMessageReader(
                connectionId: newConnectionInfo.ConnectionId,
                writer: writer,
                reader: reader,
                welcomeMessage: connectionResult.Item1,
                networkStream: connectionResult.Item2,
                spotifyEncryptionRecord: connectionResult.Item3,
                onDisconnection: async (rt, errorMaybe) =>
                {
                    if (errorMaybe.IsNone)
                    {
                        //do nothing
                        return;
                    }

                    var msg = errorMaybe.MatchUnsafe(
                        Some: e => e.Message,
                        None: () => throw new Exception("Impossible")
                    );
                    Debug.WriteLine(msg);
                    bool connected = false;
                    while (!connected)
                    {
                        var run = await OnDisconnection(newConnectionInfo, writer, reader, credentials, deviceId, ct)
                            .Run(rt);
                        connected = run.IsSucc;
                        await Task.Delay(3000, ct);
                    }
                })
            select newConnectionInfo.With(connectionResult.Item1);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, (APWelcome, NetworkStream, SpotifyEncryptionRecord)> ConnectAndAuthenticate(
        LoginCredentials credentials, string deviceId, CancellationToken ct) =>
        from hostPortResponse in AP<RT>.FetchAP()
        from tcpClient in Tcp<RT>.Connect(hostPortResponse.Host, hostPortResponse.Port)
        let stream = tcpClient.GetStream()
        from clientHelloResult in Handshake<RT>.PerformClientHello(stream, ct)
        from nonceAfterAuthAndApWelcome in Authentication<RT>.Authenticate(stream, clientHelloResult, credentials,
            deviceId)
        select (nonceAfterAuthAndApWelcome.ApWelcome, stream, nonceAfterAuthAndApWelcome.EncryptionRecord);


    internal static Aff<RT, Unit> StartMessageReader(Guid connectionId,
        ChannelWriter<SpotifyPacket> writer,
        ChannelReader<SpotifyPacket> reader,
        APWelcome welcomeMessage,
        NetworkStream networkStream,
        SpotifyEncryptionRecord spotifyEncryptionRecord,
        Action<RT, Option<Error>> onDisconnection)
    {
        return Aff<RT, Unit>(async rt =>
        {
            await Task.Factory.StartNew(async () =>
            {
                await ReadMessageRecursive(
                    runtime: rt,
                    writer: writer,
                    networkStream: networkStream,
                    spotifyEncryptionRecord: spotifyEncryptionRecord,
                    onDisconnection: onDisconnection
                );
            }, TaskCreationOptions.LongRunning);

            await Task.Factory.StartNew(async () =>
            {
                await WriteMessageRecursive(
                    runtime: rt,
                    reader: reader,
                    networkStream: networkStream,
                    spotifyEncryptionRecord: spotifyEncryptionRecord,
                    onDisconnection: onDisconnection
                );
            }, TaskCreationOptions.LongRunning);
            return unit;
        });
    }

    private static async Task<Unit> WriteMessageRecursive(
        RT runtime,
        ChannelReader<SpotifyPacket> reader,
        NetworkStream networkStream,
        SpotifyEncryptionRecord spotifyEncryptionRecord,
        Action<RT, Option<Error>> onDisconnection)
    {
        var run =
            await WriteMessage(
                reader,
                networkStream,
                spotifyEncryptionRecord).Run(runtime);

        if (run.IsFail)
        {
            var error = run.Match(Succ: _ => throw new Exception("Impossible"), Fail: e => e);
            onDisconnection(runtime, error);
            return unit;
        }

        var newRecord = run.Match(Succ: e => e, Fail: _ => throw new Exception("Impossible"));

        //call self
        return await WriteMessageRecursive(
            runtime,
            reader,
            networkStream,
            newRecord,
            onDisconnection);
    }

    private static async Task<Unit> ReadMessageRecursive(
        RT runtime,
        ChannelWriter<SpotifyPacket> writer,
        NetworkStream networkStream,
        SpotifyEncryptionRecord spotifyEncryptionRecord,
        Action<RT, Option<Error>> onDisconnection)
    {
        var run =
            await ReadMessage(
                writer,
                networkStream,
                spotifyEncryptionRecord).Run(runtime);

        if (run.IsFail)
        {
            var error = run.Match(Succ: _ => throw new Exception("Impossible"), Fail: e => e);
            onDisconnection(runtime, error);
            return unit;
        }

        var newRecord = run.Match(Succ: e => e, Fail: _ => throw new Exception("Impossible"));

        //call self
        return await ReadMessageRecursive(
            runtime,
            writer,
            networkStream,
            newRecord,
            onDisconnection);
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, SpotifyEncryptionRecord> WriteMessage(
        ChannelReader<SpotifyPacket> writer,
        NetworkStream networkStream,
        SpotifyEncryptionRecord spotifyEncryptionRecord) =>
        from _ in writer.WaitToReadAsync().ToAff()
        from message in writer.ReadAsync().ToAff()
        from sent in Authentication<RT>
            .SendEncryptedMessage(networkStream, message, spotifyEncryptionRecord)
        select sent;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, SpotifyEncryptionRecord> ReadMessage(
        ChannelWriter<SpotifyPacket> writer,
        NetworkStream networkStream,
        SpotifyEncryptionRecord spotifyEncryptionRecord) =>
        from message in Authentication<RT>
            .ReadDecryptedMessage(networkStream, spotifyEncryptionRecord)
        from dispatch in Eff(() =>
        {
            if (writer.TryWrite(message.Packet))
                return unit;
            throw new Exception("Failed to write to channel. Probably closed.");
        })
        select message.NewEncryptionRecord;


    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Aff<RT, Unit> OnDisconnection(InternalSpotifyConnectionInfo connectionInfo,
        ChannelWriter<SpotifyPacket> writer,
        ChannelReader<SpotifyPacket> reader,
        LoginCredentials credentials, string deviceId, CancellationToken ct) =>
        from _ in Log<RT>.logInfo("Disconnected from Spotify")
        from __ in SuccessEff(atomic(() => connectionInfo.With(None)))
        from ___ in AuthenticateWithoutConnectionId(connectionInfo, writer, reader, credentials, deviceId, ct)
        select unit;
}

internal class InternalSpotifyConnectionInfo
{
    internal readonly Ref<Option<APWelcome>> WelcomeMessage = Ref(Option<APWelcome>.None);
    public required Guid ConnectionId { get; init; }
    public required string Deviceid { get; init; }
    public required SpotifyConfig Config { get; init; }
    public Option<APWelcome> Welcome => WelcomeMessage.Value;
    public IObservable<Option<APWelcome>> WelcomeChanged => WelcomeMessage.OnChange();

    internal InternalSpotifyConnectionInfo With(Option<APWelcome> w)
    {
        atomic(() => WelcomeMessage.Swap(_ => w));
        return this;
    }
}