using System.Buffers.Binary;
using System.Threading.Channels;
using Google.Protobuf;
using LanguageExt.UnsafeValueAccess;
using Wavee.Spotify.Common;
using Wavee.Spotify.Infrastructure.Connection;

namespace Wavee.Spotify.Infrastructure.Sys;

internal static class AudioKeyRuntime
{
    internal static async ValueTask<Either<AesKeyError, ReadOnlyMemory<byte>>> Request(
        SpotifyId itemId,
        ByteString fileId,
        Ref<Option<uint>> sequence,
        ChannelWriter<SpotifyPacket> sender,
        ChannelReader<Either<Error, SpotifyPacket>> reader)
    {
        var nextSequence = GetNextSequenceId(sequence);
        var resultTask = Task.Run(async () =>
        {
            await foreach (var packetOrError in reader.ReadAllAsync())
            {
                if (packetOrError.IsLeft)
                {
                    throw packetOrError.Match(Left: r => r, Right: _ => throw new Exception("Should not happen"));
                }

                var packet = packetOrError.Match(Left: _ => throw new Exception("Should not happen"), Right: r => r);

                // we are only interested in aes packets here that have the same sequence id as the request   
                if (packet.Command is SpotifyPacketType.AesKey or SpotifyPacketType.AesKeyError)
                {
                    var seq = BinaryPrimitives.ReadUInt32BigEndian(packet.Data.Slice(0, 4).Span);
                    if (seq != nextSequence)
                    {
                        //not our packet. ignore...
                        continue;
                    }

                    switch (packet.Command)
                    {
                        case SpotifyPacketType.AesKey:
                            var key = packet.Data.Slice(4, 16);
                            return Either<AesKeyError, ReadOnlyMemory<byte>>.Right(key);
                        case SpotifyPacketType.AesKeyError:
                            var errorCode = packet.Data.Span[4];
                            var errorType = packet.Data.Span[5];
                            return Either<AesKeyError, ReadOnlyMemory<byte>>.Left(new AesKeyError(errorCode,
                                errorType));
                    }
                }
            }

            return default;
        });

        //now that our listeners are setup, we can send the request
        var buildPacket = AesPacketBuilder.BuildRequest(itemId, fileId, nextSequence);
        var wrote = sender.TryWrite(buildPacket);

        var response = await resultTask;
        return response;
    }

    private static uint GetNextSequenceId(Ref<Option<uint>> sequenceId)
    {
        //if no item exists, add 0 and return 0
        //if item exists, add 1 and return +1 (so if it was 0, return 0 + 1)
        return atomic(() => sequenceId.Swap(x => x.Match(
            Some: y => y + 1,
            None: () => (uint)0))).ValueUnsafe();
    }
}

public readonly record struct AesKeyError(byte ErrorCode, byte ErrorType);