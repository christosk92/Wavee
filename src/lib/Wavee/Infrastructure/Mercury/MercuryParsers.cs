using System.Buffers.Binary;
using System.Diagnostics;
using Eum.Spotify;
using Google.Protobuf;
using Serilog;
using Wavee.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Token.Live;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Wavee.Infrastructure.Mercury;

internal static class MercuryParsers
{
    private static Dictionary<Guid, ulong> _sequences = new();
    private static readonly object _sequencesLock = new();

    //The last remaining mercury endpoint
    //once spotify replaces login/v3 with login/v4?, this will be removed
    public static async Task<MercuryResponse> GetAsync(Guid connId, string endpoint, CancellationToken cancellationToken = default)
    {
        bool done = false;

        while (!done)
        {
            try
            {
                var seq = GetSequence(connId);

                var (listener, onDone) =
                    connId.CreateListener(((ref SpotifyUnencryptedPackage y) => Condition(ref y, seq)));

                Log.Debug("Sending mercury request to {endpoint}", endpoint);
                var partials = new List<ReadOnlyMemory<byte>>();
                var sw = Stopwatch.StartNew();
                var completionTask = Task.Run(async () =>
                {
                    await foreach (var package in listener.ReadAllAsync(cancellationToken))
                    {
                        var data = package.Payload;
                        var seqLen = SeqLenRef(ref data);
                        var foundSeq = SeqRef(ref data, seqLen);
                        var flags = Flag(ref data);
                        var count = Count(ref data);
                        for (int i = 0; i < count; i++)
                        {
                            var part = ParsePart(ref data);
                            partials.Add(part);
                        }

                        if (flags != 1)
                            continue;
                        var header = Header.Parser.ParseFrom(partials[0].Span);
                        var bodyLength = partials.Skip(1).Sum(x => x.Length);
                        Memory<byte> body = new byte[bodyLength];
                        var offset = 0;
                        foreach (var part in partials.Skip(1))
                        {
                            part.CopyTo(body.Slice(offset));
                            offset += part.Length;
                        }

                        sw.Stop();
                        Log.Debug("MercuryClient.Get {endpoint} took {elapsed}ms", endpoint, sw.ElapsedMilliseconds);
                        var response = new MercuryResponse(header, body);
                        onDone();
                        return response;
                    }

                    throw new TimeoutException();
                }, cancellationToken);
                SendInternal(connId, seq, endpoint, MercuryMethod.Get, null);

                done = true;
                return await completionTask;
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    done = true;
                    Log.Debug("MercuryClient.Get was canceled");
                    Debug.WriteLine("MercuryClient.Get was canceled");
                    throw;
                }
            }
            catch (Exception x)
            {
                // ignored
                Debug.WriteLine(x);
                done = false;
                Log.Error(x, "MercuryClient.Get failed, retrying...");
                await Task.Delay(1000, cancellationToken);
            }
        }

        throw new OperationCanceledException();
    }

    private static ulong GetSequence(Guid connId)
    {
        lock (_sequencesLock)
        {
            if (_sequences.TryGetValue(connId, out var seq))
            {
                _sequences[connId] = seq + 1;
                return seq;
            }

            _sequences[connId] = 1;
            return 0;
        }
    }

    private static void SendInternal(Guid connectionId, ulong seq, string uri, MercuryMethod method, string? contentType)
    {
        var toSend = MercuryRequests.Build(
            seq,
            MercuryMethod.Get,
            uri,
            null,
            Array.Empty<ReadOnlyMemory<byte>>());

        SpotifyConnection.Send(connectionId, toSend);
    }


    private static bool Condition(ref SpotifyUnencryptedPackage packagetocheck, ulong seq)
    {
        if (packagetocheck.Type is SpotifyPacketType.MercuryEvent
            or SpotifyPacketType.MercuryReq
            or SpotifyPacketType.MercurySub
            or SpotifyPacketType.MercuryUnsub
            or SpotifyPacketType.Unknown0xb6)
        {
            var seqLength = BinaryPrimitives.ReadUInt16BigEndian(packagetocheck.Payload.Slice(0, 2));
            var calculatedSeq = BinaryPrimitives.ReadUInt64BigEndian(packagetocheck.Payload.Slice(2, seqLength));
            if (calculatedSeq != seq)
                return false;
            return true;
        }

        return false;
    }

    public static ReadOnlyMemory<byte> ParsePart(ref ReadOnlyMemory<byte> data)
    {
        var size = BinaryPrimitives.ReadUInt16BigEndian(data.Span[..2]);
        data = data[2..];
        var body = data[..size];
        data = data[size..];
        return body;
    }

    public static ushort SeqLenRef(ref ReadOnlyMemory<byte> data)
    {
        var d = data.Span[..2];
        var l = BinaryPrimitives.ReadUInt16BigEndian(d);
        data = data[2..];
        return l;
    }

    public static ulong SeqRef(ref ReadOnlyMemory<byte> data, int len)
    {
        //  return BinaryPrimitives.ReadUInt64BigEndian(data.Span[2..len]);
        var d = data.Span[..len];
        var l = BinaryPrimitives.ReadUInt64BigEndian(d);
        data = data[len..];
        return l;
    }

    public static ushort SeqLen(ReadOnlyMemory<byte> data)
    {
        var d = data.Span[..2];
        var l = BinaryPrimitives.ReadUInt16BigEndian(d);
        return l;
    }

    public static ulong Seq(ReadOnlyMemory<byte> data, int len)
    {
        //  return BinaryPrimitives.ReadUInt64BigEndian(data.Span[2..len]);
        var d = data.Span[..len];
        var l = BinaryPrimitives.ReadUInt64BigEndian(d);
        return l;
    }

    public static ushort Count(ref ReadOnlyMemory<byte> readOnlyMemory)
    {
        var c = BinaryPrimitives.ReadUInt16BigEndian(readOnlyMemory.Span[..2]);
        readOnlyMemory = readOnlyMemory[2..];
        return c;
    }

    public static byte Flag(ref ReadOnlyMemory<byte> data)
    {
        var d = data.Span[..1];
        var l = d[0];
        data = data[1..];
        return l;
    }

    private enum MercuryMethod
    {
        Get,
        Sub,
        Unsub,
        Send
    }

    private static class MercuryRequests
    {
        public static BoxedSpotifyPackage Build(
            ulong sequenceNumber,
            MercuryMethod method,
            string uri,
            string? contentType,
            ReadOnlyMemory<byte>[] payload)
        {
            Span<byte> seq = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64BigEndian(seq, sequenceNumber);

            var cmd = method switch
            {
                MercuryMethod.Get => SpotifyPacketType.MercuryReq,
                MercuryMethod.Sub => SpotifyPacketType.MercurySub,
                MercuryMethod.Unsub => SpotifyPacketType.MercuryUnsub,
                MercuryMethod.Send => SpotifyPacketType.MercuryReq,
                _ => throw new ArgumentOutOfRangeException()
            };

            var header = new Header
            {
                Uri = uri,
                Method = method.ToString().ToUpper()
            };

            if (contentType != null) header.ContentType = contentType;

            Span<byte> headerSpan = header.ToByteArray();

            var payloadCount = payload.Count();
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
                var part = payload[index].Span;
                BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
                                                                        + seq.Length + 1 + sizeof(ushort)
                                                                        + sizeof(ushort) + headerSpan.Length
                                                                        + index * (sizeof(ushort) + 1)),
                    (ushort)part.Length);
                BinaryPrimitives.WriteUInt16BigEndian(packet.Span.Slice(sizeof(ushort)
                                                                        + seq.Length + 1 + sizeof(ushort)
                                                                        + sizeof(ushort) + headerSpan.Length
                                                                        + index * (sizeof(ushort) + 1)
                                                                        + sizeof(ushort)),
                    (ushort)part.Length);
            }

            return new BoxedSpotifyPackage(cmd, packet);
        }
    }

    public static void Reset(Guid connection)
    {
        lock (_sequencesLock)
        {
            _sequences[connection] = 0;
        }
    }
}