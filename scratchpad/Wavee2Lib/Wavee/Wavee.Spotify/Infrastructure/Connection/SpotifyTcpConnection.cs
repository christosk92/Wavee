using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using Eum.Spotify;
using Google.Protobuf;
using LanguageExt;
using Wavee.Spotify.Infrastructure.Connection.Specifics;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Infrastructure.Connection;

internal readonly record struct BoxedSpotifyPacket(SpotifyPacketType Type, ReadOnlyMemory<byte> Data);

internal sealed class SpotifyTcpConnection : IDisposable
{
    private readonly List<(PackageReceiveCondition condition, ChannelWriter<BoxedSpotifyPacket> Writer)> _callbacks =
        new();

    private readonly string _deviceId;
    private readonly string _host;
    private readonly ushort _port;
    private TcpClient _client;
    private NetworkStream _stream;
    private readonly LoginCredentials _credentials;
    private readonly CancellationTokenSource _cts = new();

    public SpotifyTcpConnection(string host, ushort port, LoginCredentials credentials, string deviceId)
    {
        _callbacks = new();

        _credentials = credentials;
        _deviceId = deviceId;
        var (client, sendKey, receiveKey, welcomeMessage) = ConnectAndHandshake(host, port, credentials, deviceId);
        _client = client;
        _stream = _client.GetStream();
        _host = host;
        _port = port;
        _sendKey = sendKey;
        _receiveKey = receiveKey;
        _sendSequence = 1; // start at 1 because 0 is reserved for the handshake
        _receiveSequence = 1; // start at 1 because 0 is reserved for the handshake

        LastCountryCode = Ref(Option<string>.None);
        LastWelcomeMessage = Ref(welcomeMessage);

        Task.Run(() =>
        {
            var empty4Bytes = new byte[4];
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    ReceiveInternal(out var package);
                    switch (package.Type)
                    {
                        case SpotifyPacketType.Ping:
                            var response = new SpotifyUnencryptedPackage(SpotifyPacketType.Pong, empty4Bytes);
                            Send(response);
                            break;
                        case SpotifyPacketType.PongAck:
                            Debug.WriteLine("PongAck");
                            break;
                        case SpotifyPacketType.CountryCode:
                            var countryCode = Encoding.UTF8.GetString(package.Payload);
                            atomic(() => LastCountryCode.Value = countryCode);
                            break;
                        case SpotifyPacketType.ProductInfo:
                            break;
                        default:
                            bool wasInteresting = false;
                            foreach (var callback in _callbacks)
                            {
                                if (callback.condition(ref package))
                                {
                                    wasInteresting = true;
                                    callback.Writer.TryWrite(new BoxedSpotifyPacket(
                                        Type: package.Type,
                                        Data: package.Payload.ToArray()
                                    ));
                                }
                            }
                            if (!wasInteresting)
                            {
                                Debug.WriteLine($"Received unhandled package: {package.Type}");
                            }

                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    AttemptReconnect();
                }
            }
        });
    }

    public Ref<APWelcome> LastWelcomeMessage { get; }
    public Ref<Option<string>> LastCountryCode { get; }

    //Thread safe
    public Unit Send(SpotifyUnencryptedPackage package)
    {
        int seq;
        lock (_receiveSequenceLock)
        {
            seq = _sendSequence;
            _sendSequence++;
        }

        try
        {
            _stream.Send(package, _sendKey.Span, seq);
            return default;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);

            AttemptReconnect();
            Send(package);

            return default;
        }
    }


    //Thread safe
    public Channel<BoxedSpotifyPacket> CreateListener(PackageReceiveCondition condition)
    {
        var result = Channel.CreateUnbounded<BoxedSpotifyPacket>();
        _callbacks.Add((condition, result.Writer));
        return result;
    }

    // public Task<T> Receive<T>(
    //     PackageReceiveCondition condition,
    //     MutatePackageFuncDelegate<T> projection)
    // {
    //     var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
    //     var callback = new PackageCallbackInt<T>(condition, projection, tcs);
    //     _callbacks.Add(callback);
    //     return tcs.Task.ContinueWith(x =>
    //     {
    //         _callbacks.Remove(callback);
    //         return x.Result;
    //     });
    // }

    //not thread safe
    //only call once (in a main loop)
    private Unit ReceiveInternal(out SpotifyUnencryptedPackage package)
    {
        //var dummyPackage = new SpotifyUnencryptedPackage();
        try
        {
            _stream.Receive(_receiveKey.Span, _receiveSequence, out var pckg);
            _receiveSequence++;
            package = pckg;
            return default;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);

            AttemptReconnect();
            ReceiveInternal(out package);

            return default;
        }
    }


    private static (TcpClient Client, ReadOnlyMemory<byte> SendKey, ReadOnlyMemory<byte> ReceiveKey, APWelcome
        WelcomeMessage)
        ConnectAndHandshake(string host, ushort port, LoginCredentials credentials, string deviceId)
    {
        var client = new TcpClient();
        client.Connect(host, port);
        var stream = client.GetStream();
        var (sendKey, receiveKey) = HandshakeIO.Handshake(stream);
        var welcomeMessage = Authenticate(stream,
            sendKey,
            receiveKey,
            credentials,
            deviceId);
        return (client, sendKey, receiveKey, welcomeMessage);
    }

    private static APWelcome Authenticate(NetworkStream stream,
        ReadOnlyMemory<byte> sendKey,
        ReadOnlyMemory<byte> receiveKey,
        LoginCredentials credentials,
        string deviceId)
    {
        var arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
        var cpu = arch switch
        {
            "blackfin" => CpuFamily.CpuBlackfin,
            "arm" or "arm64" => CpuFamily.CpuArm,
            "ia64" => CpuFamily.CpuIa64,
            "mips" => CpuFamily.CpuMips,
            "ppc" => CpuFamily.CpuPpc,
            "ppc64" => CpuFamily.CpuPpc64,
            "sh" => CpuFamily.CpuSh,
            "x86" => CpuFamily.CpuX86,
            "x86_64" or "AMD64" => CpuFamily.CpuX8664,
            _ => CpuFamily.CpuUnknown
        };

        var osString = Environment.OSVersion.Platform.ToString().ToLower();
        var os = osString switch
        {
            "android" => Os.Android,
            "freebsd" or "netbsd" or "openbsd" => Os.Freebsd,
            "io" => Os.Iphone,
            "linux" => Os.Linux,
            "macoS" => Os.Osx,
            "windows" => Os.Windows,
            _ => Os.Unknown
        };

        Span<byte> packet = new ClientResponseEncrypted
        {
            LoginCredentials = credentials,
            SystemInfo = new SystemInfo
            {
                CpuFamily = cpu,
                Os = os,
                SystemInformationString = "librespot 0.5.0-dev a211ff9",
                DeviceId = deviceId
            },
            VersionString = "librespot 0.5.0-dev a211ff9",
        }.ToByteArray();

        var package = new SpotifyUnencryptedPackage(SpotifyPacketType.Login, packet);
        stream.Send(package, sendKey.Span, 0);
        var response = stream.Receive(receiveKey.Span, 0, out var pckg);

        return pckg.Type switch
        {
            SpotifyPacketType.APWelcome => APWelcome.Parser.ParseFrom(pckg.Payload),
            SpotifyPacketType.AuthFailure => throw new SpotifyAuthenticationException(
                APLoginFailed.Parser.ParseFrom(pckg.Payload)),
        };
    }

    private readonly object _reconnectLock = new();

    private void AttemptReconnect()
    {
        lock (_reconnectLock)
        {
            if (_client.Connected)
            {
                return;
            }

            _client.Dispose();
            _stream.Dispose();

            var (client, sendKey, receiveKey, welcomeMessage) =
                ConnectAndHandshake(_host, _port, _credentials, _deviceId);
            _client = client;
            _stream = _client.GetStream();

            atomic(() => LastWelcomeMessage.Swap(_ => welcomeMessage));
            _sendKey = sendKey;
            _receiveKey = receiveKey;
            _sendSequence = 1; //send credentials
            _receiveSequence = 1; //receive welcome
        }
    }

    private ReadOnlyMemory<byte> _sendKey;
    private ReadOnlyMemory<byte> _receiveKey;

    private int _sendSequence;
    private int _receiveSequence;
    private readonly object _receiveSequenceLock = new();
    private readonly object _sendSequenceLock = new();
    private readonly object _receiving = new();

    public void Dispose()
    {
        _client.Dispose();
        _stream.Dispose();
    }
}

internal delegate bool PackageReceiveCondition(ref SpotifyUnencryptedPackage packageToCheck);

internal delegate Task<T> ReceivePackageDelegate<T>(PackageReceiveCondition condition,
    MutatePackageFuncDelegate<T> projection);

internal delegate T MutatePackageFuncDelegate<out T>(ref SpotifyUnencryptedPackage from);