using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using Eum.Spotify;
using LanguageExt.Effects.Traits;
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.ApResolver;
using Wavee.Spotify.Connection;
using Wavee.Spotify.Crypto;

namespace Wavee.Spotify;

public static class SpotifyRuntime
{
    public static async ValueTask<APWelcome> Authenticate(
        LoginCredentials credentials,
        Option<ISpotifyListener> listener)
    {
        const string apResolve = "https://apresolve.spotify.com/?type=accesspoint&type=dealer&type=spclient";

        var result = await Authenticate<WaveeRuntime>(apResolve,
            credentials, listener).Run(WaveeCore.Runtime);

        return result.Match(
            Succ: t => t,
            Fail: e => throw e);
    }

    private static Aff<RT, APWelcome> Authenticate<RT>(string apResolve,
        LoginCredentials credentials, Option<ISpotifyListener> listener)
        where RT : struct, HasHttp<RT>, HasTCP<RT>
    {
        var deviceId = Guid.NewGuid().ToString();
        var channel = Channel.CreateUnbounded<SpotifyPacket>();
        return AuthenticateWithTcp<RT>(apResolve, credentials, listener, channel, deviceId);
    }

    private static Aff<RT, Unit> SendMessage<RT>(ChannelWriter<byte[]> writer, byte[] message)
        where RT : struct, HasCancel<RT>, HasTCP<RT>
        => Aff<RT, Unit>(async (_) =>
        {
            await writer.WriteAsync(message);
            return unit;
        });

    private static Aff<RT, APWelcome> AuthenticateWithTcp<RT>(
        string apResolve,
        LoginCredentials credentials,
        Option<ISpotifyListener> listener,
        Channel<SpotifyPacket> channel,
        string deviceId)
        where RT : struct, HasHttp<RT>, HasTCP<RT>
    {
        return
            from hostPortResponse in AP<RT>.FetchHostAndPort(apResolve)
            from tcpClient in Tcp<RT>.Connect(hostPortResponse.Host, hostPortResponse.Port)
            let stream = tcpClient.GetStream()
            from clientHelloResult in Handshake<RT>.PerformClientHello(stream)
            from nonceAfterAuthAndApWelcome in Authentication<RT>.Authenticate(stream, clientHelloResult, credentials,
                deviceId)
            from _ in Eff<RT, Unit>((r) =>
            {
                Task.Run(() =>
                    ProcessMessages<RT>(channel, stream, Ref(nonceAfterAuthAndApWelcome.EncryptionRecord), listener)
                        .Run(r));
                return unit;
            })
            select nonceAfterAuthAndApWelcome.ApWelcome;
    }

    internal static Aff<RT, Unit> ProcessMessages<RT>(Channel<SpotifyPacket> channel, NetworkStream stream,
        Ref<SpotifyEncryptionRecord> encryptionRecord, Option<ISpotifyListener> spotifyListener)
        where RT : struct, HasCancel<RT>, HasTCP<RT>
    {
        return Aff<RT, Unit>(async env =>
        {
            // Continuously read messages from the channel, encrypt them, and send them over the TCP connection
            var sendingTask = Task.Run(async () =>
            {
                await foreach (var message in channel.Reader.ReadAllAsync(env.CancellationToken))
                {
                    // Perform the encryption
                    var encryptedMessage = await Authentication<RT>
                        .SendEncryptedMessage(stream, message, encryptionRecord)
                        .Run(env);
                    encryptedMessage.Match(
                        Succ: r => { atomic(() => encryptionRecord.Swap(k => r)); },
                        Fail: e =>
                        {
                            Debug.WriteLine(e);
                            throw e;
                        }
                    );
                }
            });

            // Continuously listen for messages and decrypt them
            var listeningTask = ReadAndProcessMessage<RT>(stream, encryptionRecord).Run(env).AsTask();

            await Task.WhenAll(sendingTask, listeningTask);
            return unit;
        });
    }

    private static Aff<RT, Unit> ReadAndProcessMessage<RT>(NetworkStream stream,
        SpotifyEncryptionRecord encryptionRecord)
        where RT : struct, HasCancel<RT>, HasTCP<RT>
    {
        return Aff<RT, Unit>(async env =>
        {
            while (true)
            {
                var messageResult = await Authentication<RT>
                    .ReadDecryptedMessage(stream, encryptionRecord)
                    .Run(env);
                if (messageResult.IsFail)
                {
                    var err = messageResult.Match(Succ: _ => throw new Exception("Impossible"), Fail: identity);
                    return unit;
                }
            }
        });
    }
}

public interface ISpotifyListener
{
    Unit OnDisconnected(Option<Error> error);
    Unit CountryCodeReceived(string countryCode);
}