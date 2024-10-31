using System.Net.Sockets;
using System.Security.Authentication;
using Eum.Spotify;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Wavee.Enums;
using Wavee.Exceptions;
using Wavee.Interfaces;

namespace Wavee.Services.Session;

internal sealed class WaveeTcpClient : ITcpClient
{
    private bool _didEverything = false;
    private readonly TcpClient _tcpClient;
    private readonly ILogger<WaveeTcpClient> _logger;
    private ApCodec? _codec;
    private Stream? _stream;

    public WaveeTcpClient(ILogger<WaveeTcpClient> logger)
    {
        _logger = logger;
        _tcpClient = new TcpClient();
    }

    public bool Connected
    {
        get
        {
            try
            {
                return
                    _didEverything
                    && _tcpClient.Connected
                    && _tcpClient.GetStream().ReadTimeout > -2;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public ValueTask ConnectAsync(string host, int port, CancellationToken cancellationToken)
    {
        var res = _tcpClient.ConnectAsync(host, port, cancellationToken);
        return res;
    }

    public async Task<LoginCredentials> Initialize(LoginCredentials storedCredentials, string playbackDeviceId,
        CancellationToken cancellationToken)
    {
        if (_didEverything)
        {
            throw new InvalidOperationException("Already initialized");
        }

        var (receiveKey, sendKey, stream) = Handshake.Do(this, _logger);
        var codec = new ApCodec(sendKey, receiveKey);
        var credentials =
            await AuthenticateAsync(stream, codec, storedCredentials, playbackDeviceId, _logger, cancellationToken);
        _didEverything = true;
        return credentials;
    }

    public Stream GetStream()
    {
        return _tcpClient.GetStream();
    }

    public async Task<SpotifyTcpMessage> ReadMessageAsync(CancellationToken token)
    {
        if (_codec is null || _stream is null)
        {
            throw new WaveeUnknownException("TCP client is not initialized");
        }
        var response = await _codec.ReceiveAsync(_stream, token);
        if (!response.HasValue)
        {
            throw new WaveeNetworkException("No data received");
        }
        
        var (cmdByte, data) = response.Value;
        return new SpotifyTcpMessage((PacketType)cmdByte, data);
    }

    public Task SendPacketAsync(SpotifyTcpMessage message, CancellationToken cancellationToken)
    {
        if (_codec is null || _stream is null)
        {
            throw new WaveeUnknownException("TCP client is not initialized");
        }
        return _codec.SendAsync(_stream, (byte)message.Type, message.Payload, cancellationToken);
    }

    private async Task<LoginCredentials> AuthenticateAsync(
        Stream stream,
        ApCodec codec,
        LoginCredentials credentials,
        string deviceId,
        ILogger<WaveeTcpClient> logger,
        CancellationToken cancellationToken)
    {
        var cpuFamily = GetCpuFamily();
        var os = GetOs();

        var packet = new ClientResponseEncrypted
        {
            LoginCredentials = new LoginCredentials
            {
                Username = credentials.Username,
                Typ = credentials.Typ,
                AuthData = credentials.AuthData
            },
            SystemInfo = new SystemInfo
            {
                CpuFamily = cpuFamily,
                Os = os,
                SystemInformationString = "librespot 0.5.0-dev a211ff9",
                DeviceId = deviceId
            },
            VersionString = "librespot 0.5.0-dev a211ff9",
        };

        var cmd = PacketType.Login;
        var data = packet.ToByteArray();

        logger.LogTrace("Sending login packet.");
        await codec.SendAsync(stream, (byte)cmd, data, cancellationToken);

        logger.LogTrace("Receiving login response.");
        var response = await codec.ReceiveAsync(stream, cancellationToken);

        if (!response.HasValue)
        {
            logger.LogError("Transport returned no data.");
            throw new AuthenticationException("Transport returned no data");
        }

        var (cmdByte, responseData) = response.Value;
        var packetType = (PacketType)cmdByte;
        logger.LogTrace("Received packet: {PacketType}", packetType);
        _codec = codec;
        _stream = stream;
        return packetType switch
        {
            PacketType.APWelcome => ParseWelcome(responseData),
            PacketType.AuthFailure => throw new WaveeCouldNotAuthenticateException("Authentication failed: " +
                ParseError(responseData)),
            _ => throw new WaveeCouldNotAuthenticateException($"Unexpected packet: {cmdByte}")
        };
    }


    private static CpuFamily GetCpuFamily()
    {
        return Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") switch
        {
            "x86" => CpuFamily.CpuX86,
            "x64" => CpuFamily.CpuX8664,
            "arm" => CpuFamily.CpuArm,
            "arm64" => CpuFamily.CpuArm,
            "ia64" => CpuFamily.CpuIa64,
            _ => CpuFamily.CpuUnknown
        };
    }

    private static Os GetOs()
    {
        return Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => Os.Windows,
            PlatformID.MacOSX => Os.Osx,
            PlatformID.Unix => Os.Linux,
            _ => Os.Unknown
        };
    }

    private static LoginCredentials ParseWelcome(byte[] data)
    {
        var welcomeData = APWelcome.Parser.ParseFrom(data);
        return new LoginCredentials
        {
            Username = welcomeData.CanonicalUsername,
            Typ = welcomeData.ReusableAuthCredentialsType,
            AuthData = welcomeData.ReusableAuthCredentials
        };
    }

    private static ErrorCode ParseError(byte[] data)
    {
        var errorData = APLoginFailed.Parser.ParseFrom(data);
        return errorData.ErrorCode;
    }

    public void Dispose()
    {
        _tcpClient.Dispose();
    }
}