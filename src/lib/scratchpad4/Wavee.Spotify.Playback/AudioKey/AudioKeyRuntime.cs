using System.Buffers.Binary;
using System.Threading.Channels;
using Google.Protobuf;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Sys.Common;
using Wavee.Spotify.Sys.Connection;
using Wavee.Spotify.Sys.Connection.Contracts;

namespace Wavee.Spotify.Sys.AudioKey;

internal static class AudioKeyRuntime
{
    private static Atom<HashMap<Guid, uint>> _audioKeySequence = Atom(LanguageExt.HashMap<Guid, uint>.Empty);

    public static Aff<RT, Either<AesKeyError, ReadOnlyMemory<byte>>> GetAudioKey<RT>(
        this SpotifyConnectionInfo connectionInfo,
        SpotifyId itemId,
        ByteString fileId,
        CancellationToken ct = default)
        where RT : struct, HasTCP<RT>, HasHttp<RT>
    {
        var connectionId = connectionInfo.ConnectionId;
        var nextSeqMap = atomic(() => _audioKeySequence.Swap(x => x.AddOrUpdate(connectionId,
            Some: x => x + 1,
            None: () => 0
        )));
        var nextSeq = nextSeqMap.ValueUnsafe().Find(connectionId)
            .IfNoneUnsafe(() => throw new Exception("Should not happen"));

        var dispatcher = SpotifyConnection<RT>.ConnectionProducer.Value.Find(connectionId);
        if (dispatcher.IsNone)
        {
            return FailEff<RT, Either<AesKeyError, ReadOnlyMemory<byte>>>(new Exception("Connection not found"));
        }

        var sender = dispatcher.IfNoneUnsafe(() => throw new Exception("Should not happen"));

        return Aff<RT, Either<AesKeyError, ReadOnlyMemory<byte>>>(async rt =>
        {
            var result = await Request(rt, connectionId, nextSeq, itemId, fileId, sender);
            return result;
        });
    }

    internal static async ValueTask<Either<AesKeyError, ReadOnlyMemory<byte>>> Request<RT>(
        RT runtime,
        Guid connectionId,
        uint sequence,
        SpotifyId itemId,
        ByteString fileId,
        ChannelWriter<SpotifyPacket> sender) where RT : struct, HasTCP<RT>, HasHttp<RT>
    {
        var resultTask = Task.Run(async () =>
        {
            var packet = await ConnectionListener<RT>.ConsumePacket(connectionId, packet =>
            {
                // we are only interested in aes packets here that have the same sequence id as the request   
                if (packet.Command is SpotifyPacketType.AesKey or SpotifyPacketType.AesKeyError)
                {
                    var seq = BinaryPrimitives.ReadUInt32BigEndian(packet.Data.Slice(0, 4).Span);
                    if (seq != sequence)
                    {
                        //not our packet. ignore...
                        return false;
                    }

                    return true;
                }

                return false;
            }, true).Run(runtime);

            return packet.Match(
                Succ: f => f,
                Fail: e => throw e
            );
        });

        //now that our listeners are setup, we can send the request
        var buildPacket = AesPacketBuilder.BuildRequest(itemId, fileId, sequence);
        var wrote = sender.TryWrite(buildPacket);

        var response = await resultTask;
        switch (response.Command)
        {
            case SpotifyPacketType.AesKey:
                var key = response.Data.Slice(4, 16);
                return Either<AesKeyError, ReadOnlyMemory<byte>>.Right(key);
            case SpotifyPacketType.AesKeyError:
                var errorCode = response.Data.Span[4];
                var errorType = response.Data.Span[5];
                return Either<AesKeyError, ReadOnlyMemory<byte>>.Left(new AesKeyError(errorCode,
                    errorType));
        }

        throw new Exception("Should not happen");
    }
}