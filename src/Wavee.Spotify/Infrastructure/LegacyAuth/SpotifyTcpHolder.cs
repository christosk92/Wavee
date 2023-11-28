using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using CommunityToolkit.HighPerformance;
using Eum.Spotify;
using Google.Protobuf;
using Mediator;
using Nito.AsyncEx;
using Wavee.Spotify.Application.Common.Queries;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Exceptions;
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
    private ulong _seq;
    private readonly AsyncLock _seqLock = new();

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
            var partials = new Dictionary<ulong, List<ReadOnlyMemory<byte>>>();
            while (true)
            {
                try
                {
                    var stream = connection.TcpClient.GetStream();
                    var receive = Auth.Receive(stream, keys.ReceiveKey.Span, nonce);
                    switch (receive.Type)
                    {
                        case SpotifyPacketType.MercuryReq:
                            {
                                var data = receive.Payload;
                                var seqLen = SeqLenRef(ref data);
                                var foundSeq = SeqRef(ref data, seqLen);
                                var flags = Flag(ref data);
                                var count = Count(ref data);
                                for (int i = 0; i < count; i++)
                                {
                                    var part = ParsePart(ref data);
                                    if (!partials.TryGetValue(foundSeq, out var list))
                                    {
                                        list = new List<byte[]>();
                                        partials.Add(foundSeq, list);
                                    }
                                    list.Add(part.ToArray());

                                    if (flags != 1)
                                        continue;
                                    var appropriatePartials = partials[foundSeq];
                                    var header = Header.Parser.ParseFrom(appropriatePartials[0]);
                                    var bodyLength = appropriatePartials.Skip(1).Sum(x => x.Length);
                                    Memory<byte> body = new byte[bodyLength];
                                    var offset = 0;
                                    foreach (var partpart in appropriatePartials.Skip(1))
                                    {
                                        partpart.CopyTo(body.Slice(offset));
                                        offset += partpart.Length;
                                    }

                                    if (connection.MercuryWaiters.TryGetValue(foundSeq, out var waiter))
                                    {
                                        waiter.SetResult(body.ToArray());
                                    }
                                }
                                break;
                            }
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
        if (_connection is null)
        {
            //wait for 5 seconds
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _connected.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new SpotifyNotAuthenticatedException(
                    "Not connected to Spotify. Did you forget to call Connect()?");
            }
        }

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

    public Task<GetMercuryResponse> GetMercury(string uri)
    {
        var tcs = new TaskCompletionSource<GetMercuryResponse>();

        var (packet, seq)= BuildPacket(uri);
        _connection!.MercuryWaiters.Add(seq, tcs);
        _sendQueue.Add(packet);

        return tcs.Task;
    }

    private ulong GetNextSequenceNumber()
    {
        using (_seqLock.Lock())
        {
            return _seq++;
        }
    }

    private (BoxedSpotifyPackage, ulong) BuildPacket(string uri)
    {
        Span<byte> seq = stackalloc byte[sizeof(ulong)];
        var sequenceNumber = GetNextSequenceNumber();
        BinaryPrimitives.WriteUInt64BigEndian(seq, sequenceNumber);

        var cmd = SpotifyPacketType.MercuryReq;

        var header = new Header
        {
            Uri = uri,
            Method = "GET"
        };


        Span<byte> headerSpan = header.ToByteArray();

        var payloadCount = 0;
        Memory<byte> packet = new byte[
            sizeof(ushort) // seq length
            + seq.Length // seq
            + sizeof(byte) // flags
            + sizeof(ushort) // part count
            + sizeof(ushort) //header length
            + headerSpan.Length // header
            + payloadCount * (sizeof(ushort) + 1) // part length
        ];

        BinaryPrimitives.WriteUInt16BigEndian(packet.Span, (ushort)seq.Length);
        seq.CopyTo(packet.Span.Slice(sizeof(ushort)));
        packet.Span[sizeof(ushort) + seq.Length] = 1; // flags: FINAL
        BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
                                                                + seq.Length + 1),
            (ushort)(1 + payloadCount)); // part count

        // header length
        BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
                                                                + seq.Length + 1 + sizeof(ushort)),
            (ushort)headerSpan.Length);

        // header
        headerSpan.CopyTo(packet.Span.Slice(sizeof(ushort)
                                            + seq.Length + 1 + sizeof(ushort) + sizeof(ushort)));

        for (var index = 0; index < payloadCount; index++)
        {
            //if we are in this loop, we can assume that the payload is not empty
            // var part = payload[index].Span;
            // BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
            //                                                         + seq.Length + 1 + sizeof(ushort)
            //                                                         + sizeof(ushort) + headerSpan.Length
            //                                                         + index * (sizeof(ushort) + 1)),
            //     (ushort)part.Length);
            // BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
            //                                                         + seq.Length + 1 + sizeof(ushort)
            //                                                         + sizeof(ushort) + headerSpan.Length
            //                                                         + index * (sizeof(ushort) + 1)
            //                                                         + sizeof(ushort)),
            //     (ushort)part.Length);
        }

        return (new BoxedSpotifyPackage(
            Type: cmd,
            Data: packet
            ), sequenceNumber);
    }

    public static ushort Count(ref ReadOnlySpan<byte> readOnlyMemory)
    {
        var c = BinaryPrimitives.ReadUInt16BigEndian(readOnlyMemory[..2]);
        readOnlyMemory = readOnlyMemory[2..];
        return c;
    }

    public static byte Flag(ref ReadOnlySpan<byte> data)
    {
        var d = data[..1];
        var l = d[0];
        data = data[1..];
        return l;
    }

    public static ReadOnlySpan<byte> ParsePart(ref ReadOnlySpan<byte> data)
    {
        var size = BinaryPrimitives.ReadUInt16BigEndian(data[..2]);
        data = data[2..];
        var body = data[..size];
        data = data[size..];
        return body;
    }

    public static ushort SeqLenRef(ref ReadOnlySpan<byte> data)
    {
        var d = data[..2];
        var l = BinaryPrimitives.ReadUInt16BigEndian(d);
        data = data[2..];
        return l;
    }

    public static ulong SeqRef(ref ReadOnlySpan<byte> data, int len)
    {
        //  return BinaryPrimitives.ReadUInt64BigEndian(data.Span[2..len]);
        var d = data[..len];
        var l = BinaryPrimitives.ReadUInt64BigEndian(d);
        data = data[len..];
        return l;
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
    public Dictionary<ulong, TaskCompletionSource<GetMercuryResponse>> MercuryWaiters { get; } = new();

    public void Dispose()
    {
        _tcpClient?.Dispose();
    }
}

public readonly record struct GetMercuryResponse(Header Header, ReadOnlyMemory<byte> Payload);