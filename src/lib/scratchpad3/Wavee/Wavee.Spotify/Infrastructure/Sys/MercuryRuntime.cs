using System.Buffers.Binary;
using System.Threading.Channels;
using Eum.Spotify;
using LanguageExt.UnsafeValueAccess;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Infrastructure.Connection;

namespace Wavee.Spotify.Infrastructure.Sys;

internal static class MercuryRuntime
{
    internal static async ValueTask<MercuryResponse> Send(
        MercuryMethod method, string uri,
        Option<string> contentType,
        Ref<Option<ulong>> sequence,
        ChannelWriter<SpotifyPacket> sender,
        ChannelReader<Either<Error, SpotifyPacket>> reader)
    {
        var nextSequence = GetNextSequenceId(sequence);
        var resultTask = Task.Run(async () =>
        {
            Seq<ReadOnlyMemory<byte>> partials = Seq<ReadOnlyMemory<byte>>();

            await foreach (var packetOrError in reader.ReadAllAsync())
            {
                if (packetOrError.IsLeft)
                {
                    throw packetOrError.Match(Left: r => r, Right: _ => throw new Exception("Should not happen"));
                }

                var packet = packetOrError.Match(Left: _ => throw new Exception("Should not happen"), Right: r => r);

                // we are only interested in mercury packets here that have the same sequence id as the request   
                if (packet.Command is SpotifyPacketType.MercuryReq)
                {
                    var data = packet.Data;
                    var seqLen = SeqLen(ref data);
                    var seq = Seq(ref data, seqLen);
                    if (seq != nextSequence)
                    {
                        //not our packet. ignore...
                        continue;
                    }

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
            }

            return new MercuryResponse();
        });

        //now that our listeners are setup, we can send the request
        var buildPacket = MercuryPacketBuilder.BuildRequest(method, uri, nextSequence, contentType);
        var wrote = sender.TryWrite(buildPacket);

        var response = await resultTask;
        return response;
    }

    internal static ValueTask<MercuryResponse> Get(string url,
        Ref<Option<ulong>> sequence,
        ChannelWriter<SpotifyPacket> sender,
        ChannelReader<Either<Error, SpotifyPacket>> reader)
    {
        return Send(MercuryMethod.Get, url, None, sequence, sender, reader);
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

    private static ulong GetNextSequenceId(Ref<Option<ulong>> sequenceId)
    {
        //if no item exists, add 0 and return 0
        //if item exists, add 1 and return +1 (so if it was 0, return 0 + 1)
        return atomic(() => sequenceId.Swap(x => x.Match(
            Some: y => y + 1,
            None: () => (ulong)0))).ValueUnsafe();
    }
}

public readonly record struct MercuryResponse(Header Header, ReadOnlyMemory<byte> Body);