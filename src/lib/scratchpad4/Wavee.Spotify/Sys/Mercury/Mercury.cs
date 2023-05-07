using System.Buffers.Binary;
using Eum.Spotify;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Sys.Connection;
using Wavee.Spotify.Sys.Connection.Contracts;

namespace Wavee.Spotify.Sys.Mercury;

public static class Mercury
{
    public static async ValueTask<MercuryResponse> Get(this SpotifyConnectionInfo connection, string uri,
        Option<string> contentType ,
        CancellationToken ct = default)
    {
        var sequenceMaybe = await MercuryClient<WaveeRuntime>.Send(connection.ConnectionId,
            MercuryMethod.Get, uri, contentType, ct).Run(WaveeCore.Runtime);
        var seq = sequenceMaybe.Match(
            Succ: r => r,
            Fail: e => throw e
        );

        Seq<ReadOnlyMemory<byte>> partials = Seq<ReadOnlyMemory<byte>>();
        while (!ct.IsCancellationRequested)
        {
            var response = await ConnectionListener<WaveeRuntime>.ConsumePacket(connection.ConnectionId,
                    p =>
                    {
                        if (p.Command is not SpotifyPacketType.MercuryEvent and not SpotifyPacketType.MercuryReq
                            and not SpotifyPacketType.MercurySub and not SpotifyPacketType.MercuryUnsub)
                            return false;

                        var data = p.Data;
                        var seqLen = SeqLen(ref data);
                        var foundSeq = Seq(ref data, seqLen);

                        if (foundSeq != seq)
                        {
                            //not our packet. ignore...
                            return false;
                        }

                        return true;
                    },
                    static () => true, ct)
                .Run(WaveeCore.Runtime);

            var package = response.Match(
                Succ: p => p,
                Fail: e => throw e
            );

            var data = package.Data;
            var seqLen = SeqLen(ref data);
            var foundSeq = Seq(ref data, seqLen);
            var flags = Flag(ref data);
            var count = Count(ref data);

            for (int i = 0; i < count; i++)
            {
                var part = ParsePart(ref data);
                partials = partials.Add(part);
            }

            if (flags != 1) continue;
            var header = Header.Parser.ParseFrom(partials[0].Span);
            var bodyLength = partials.Skip(1).Fold(0, (acc, x) => acc + x.Length);
            Memory<byte> body = new byte[bodyLength];
            var offset = 0;
            foreach (var part in partials.Skip(1))
            {
                part.CopyTo(body.Slice(offset));
                offset += part.Length;
            }

            return new MercuryResponse(header, body);
        }

        throw new OperationCanceledException();
    }

    private static ReadOnlyMemory<byte> ParsePart(ref ReadOnlyMemory<byte> data)
    {
        var size = BinaryPrimitives.ReadUInt16BigEndian(data.Span[..2]);
        data = data[2..];
        var body = data[..size];
        data = data[size..];
        return body;
    }

    private static ushort SeqLen(ref ReadOnlyMemory<byte> data)
    {
        var d = data.Span[..2];
        var l = BinaryPrimitives.ReadUInt16BigEndian(d);
        data = data[2..];
        return l;
    }

    private static ulong Seq(ref ReadOnlyMemory<byte> data, int len)
    {
        //  return BinaryPrimitives.ReadUInt64BigEndian(data.Span[2..len]);
        var d = data.Span[..len];
        var l = BinaryPrimitives.ReadUInt64BigEndian(d);
        data = data[len..];
        return l;
    }

    private static ushort Count(ref ReadOnlyMemory<byte> readOnlyMemory)
    {
        var c = BinaryPrimitives.ReadUInt16BigEndian(readOnlyMemory.Span[..2]);
        readOnlyMemory = readOnlyMemory[2..];
        return c;
    }

    private static byte Flag(ref ReadOnlyMemory<byte> data)
    {
        var d = data.Span[..1];
        var l = d[0];
        data = data[1..];
        return l;
    }
}

internal static class MercuryClient<RT> where RT : struct, HasTCP<RT>, HasHttp<RT>
{
    private static readonly AtomHashMap<Guid, ulong> SequenceNumbers =
        LanguageExt.AtomHashMap<Guid, ulong>.Empty;

    public static Aff<RT, ulong> Send(
        Guid connectionId,
        MercuryMethod method, string uri, Option<string> contentType, CancellationToken ct = default) =>
        Aff<RT, ulong>(async (rt) =>
        {
            SequenceNumbers
                .AddOrUpdate(connectionId, None: () => 0UL, Some: existing => existing + 1);
            var seq = SequenceNumbers.Find(connectionId).IfNone(0UL);
            // var seq = SequenceNumbers.Find(connectionId).IfNone(0UL);
            // SequenceNumbers.AddOrUpdate(connectionId, seq + 1);
            var packet = MercuryPacketBuilder.BuildRequest(method, uri, seq, contentType);
            var channelMaybe = SpotifyConnection<RT>.ConnectionProducer.Value.Find(connectionId);
            if (channelMaybe.IsNone)
            {
                return seq;
            }

            var channel = channelMaybe.ValueUnsafe();
            await channel.WriteAsync(packet, ct);
            return seq;
        });
}

internal enum MercuryMethod
{
    Get,
    Sub,
    Unsub,
    Send
}

public readonly record struct MercuryResponse(Header Header, ReadOnlyMemory<byte> Body);