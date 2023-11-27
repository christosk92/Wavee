using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Eum.Spotify;
using Google.Protobuf;
using Mediator;
using Nito.AsyncEx;
using Wavee.Spotify.Application.Common.Queries;
using Wavee.Spotify.Common;
using Wavee.Spotify.Infrastructure.LegacyAuth.Functions;

namespace Wavee.Spotify.Infrastructure.LegacyAuth;

internal readonly record struct BoxedSpotifyPackage(
    SpotifyPacketType Type,
    ReadOnlyMemory<byte> Data);

public sealed class SpotifyTcpHolder : IDisposable
{
    private readonly record struct SendSpotifyPackage(
        BoxedSpotifyPackage Send,
        Action<BoxedSpotifyPackage> Response);

    private readonly AsyncLock _connectLock = new();
    private bool _disposed;
    private readonly IMediator _mediator;
    private ActiveSpotifyConnection? _connection;
    private readonly AsyncManualResetEvent _connected = new();
    private readonly BlockingCollection<BoxedSpotifyPackage> _sendQueue = new();


    public SpotifyTcpHolder(IMediator mediator)
    {
        _mediator = mediator;

        WelcomeMessage = WaitForMessage();
    }

    internal async Task Connect(
        LoginCredentials credentials,
        string deviceId,
        CancellationToken cancellationToken)
    {
        using (await _connectLock.LockAsync())
        {
            if (_connected.IsSet)
                return;
            _connection = new ActiveSpotifyConnection(
                credentials,
                deviceId: deviceId,
                mediator: _mediator
            );
            await _connection.Connect(cancellationToken);
            _connected.Set();

            await StartListening(_connection, ConnectionOnLost);
        }
    }

    private async Task StartListening(ActiveSpotifyConnection connection,
        Action<ActiveSpotifyConnection, Exception> connectionOnLost)
    {
        //Listen 
        _ = Task.Factory.StartNew(() =>
        {
            int nonce = 1;
            var keys = connection.Keys;
            while (true)
            {
                try
                {
                    var stream = connection.TcpClient.GetStream();
                    var receive = Auth.Receive(stream, keys.ReceiveKey.Span, nonce);
                    switch (receive.Type)
                    {
                        case SpotifyPacketType.Ping:
                        {
                            _sendQueue.Add(
                                new BoxedSpotifyPackage(SpotifyPacketType.Pong, new byte[4]));
                            break;
                        }
                        case SpotifyPacketType.PongAck:
                        {
                            break;
                        }
                        case SpotifyPacketType.AesKey:
                        {
                            var key = new byte[16];
                            var seq = BinaryPrimitives.ReadUInt32BigEndian(receive.Payload.Slice(0, 4));

                            receive.Payload.Slice(4, key.Length).CopyTo(key);
                            if (_connection.AudioWaiters.TryGetValue(seq, out var waiter))
                                waiter.SetResult(key);
                            break;
                        }
                    }

                    nonce++;
                }
                catch (Exception e)
                {
                    connectionOnLost(connection, e);
                }
            }
        });

        //Send
        Task.Factory.StartNew(() =>
        {
            int nonce = 1;
            var keys = connection.Keys;
            var stream = connection.TcpClient.GetStream();
            var empty4bytearray = new byte[4];
            while (true)
            {
                //get _sendQueue
                var send = _sendQueue.TryTake(out var item, -1);

                //Send
                if (!send) continue;

                Auth.Send(stream,
                    item.Type,
                    item.Data.Span,
                    keys.SendKey.Span,
                    nonce);


                nonce++;
            }
        });
    }

    public async Task<byte[]> RequestAudioKey(SpotifyId itemId, ByteString fileId, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<byte[]>();
        SendKeyRequest(_connection!.GetNextAudioKeySeq(tcs), itemId, fileId);
        var result = await tcs.Task;
        return result;
    }

    private async void ConnectionOnLost(ActiveSpotifyConnection sender, Exception e)
    {
        if (_disposed) return;
        sender.Dispose();

        await Task.Delay(1000);
        var credentials = sender.Credentials;
        var deviceId = sender.DeviceId;

        await Connect(credentials,
            deviceId,
            CancellationToken.None);
    }

    private async Task<APWelcome>? WaitForMessage()
    {
        await _connected.WaitAsync();
        return _connection!.WelcomeMessage;
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _disposed = true;
    }

    public Task<APWelcome> WelcomeMessage { get; }


    private void SendKeyRequest(uint seq, SpotifyId track, ByteString fileId)
    {
        var raw = track.ToRaw();
        ReadOnlySpan<byte> fileMemory = fileId.ToByteArray();

        Memory<byte> data = new byte[raw.Length + fileMemory.Length + 2 + sizeof(uint)];

        fileMemory.CopyTo(data.Span);
        raw.CopyTo(data.Span.Slice(fileMemory.Length));
        BinaryPrimitives.WriteUInt32BigEndian(data.Span.Slice(fileMemory.Length + raw.Length), seq);
        BinaryPrimitives.WriteUInt16BigEndian(data.Span.Slice(fileMemory.Length + raw.Length + sizeof(uint)), 0x0000);


        _sendQueue.Add(
            new BoxedSpotifyPackage(SpotifyPacketType.RequestKey, data)
        );
    }
}

internal sealed class ActiveSpotifyConnection : IDisposable
{
    private readonly IMediator _mediator;
    private uint _audioKeySeq;
    private AsyncLock _audioKeySeqLock = new();
    private TcpClient? _tcpClient;

    public ActiveSpotifyConnection(LoginCredentials credentials, string deviceId, IMediator mediator)
    {
        Credentials = credentials;
        DeviceId = deviceId;
        _mediator = mediator;
    }

    public uint GetNextAudioKeySeq(TaskCompletionSource<byte[]> taskCompletionSource)
    {
        using (_audioKeySeqLock.Lock())
        {
            var current = _audioKeySeq;
            _audioKeySeq++;

            AudioWaiters.Add(current, taskCompletionSource);
            return current;
        }
    }

    public async Task Connect(CancellationToken cancellationToken)
    {
        var url = await _mediator.Send(new SpotifyGetAdaptiveApiUrlQuery
        {
            Type = SpotifyApiUrlType.AccessPoint,
            DontReturnThese = null
        }, cancellationToken);
        var host = url.Host;
        var port = url.Port;
        var (apwelcome, tcpClient, keys) = SpotifyLegacyAuth.Create(
            host: host,
            port: port,
            credentials: Credentials, DeviceId);
        WelcomeMessage = apwelcome;
        Credentials = new LoginCredentials
        {
            Username = WelcomeMessage.CanonicalUsername,
            AuthData = WelcomeMessage.ReusableAuthCredentials,
            Typ = WelcomeMessage.ReusableAuthCredentialsType
        };
        _tcpClient = tcpClient;
        Keys = keys;
    }

    public APWelcome? WelcomeMessage { get; private set; }
    public LoginCredentials Credentials { get; private set; }
    public string DeviceId { get; }
    public SpotifyEncryptionKeys Keys { get; private set; }
    public TcpClient? TcpClient => _tcpClient;

    public Dictionary<uint, TaskCompletionSource<byte[]>> AudioWaiters { get; } =
        new Dictionary<uint, TaskCompletionSource<byte[]>>();

    public void Dispose()
    {
        _tcpClient?.Dispose();
    }
}