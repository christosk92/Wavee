using System.Buffers.Binary;
using System.Threading.Channels;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Tcp;

namespace Wavee.Spotify.Infrastructure.Playback.Key;

public readonly struct AudioKeyClient
{
    private readonly Guid _connectionId;

    private readonly Func<Option<SpotifySendPacket>, Func<SpotifySendPacket, bool>, ChannelReader<SpotifySendPacket>>
        _subscribe;

    private readonly Action<ChannelReader<SpotifySendPacket>> _removePackageListener;

    private static Atom<HashMap<Guid, uint>> _seq = Atom(LanguageExt.HashMap<Guid, uint>.Empty);

    internal AudioKeyClient(
        Guid connectionId,
        Func<
            Option<SpotifySendPacket>,
            Func<SpotifySendPacket, bool>, ChannelReader<SpotifySendPacket>> subscribe,
        Action<ChannelReader<SpotifySendPacket>> removePackageListener)
    {
        _connectionId = connectionId;
        _subscribe = subscribe;
        _removePackageListener = removePackageListener;
    }

    public async ValueTask<Either<AesKeyError, AudioKey>> GetAudioKey(AudioId trackId, ByteString fileId,
        CancellationToken ct = default)
    {
        var connectionId = _connectionId;
        var nextSequences = atomic(() =>
        {
            return _seq
                .Swap(x => x.AddOrUpdate(connectionId,
                    Some: s => s + 1,
                    None: () => 0))
                .ValueUnsafe();
        });
        var nextSequence = nextSequences.Find(connectionId).ValueUnsafe();


        var packet = AesPacketBuilder.BuildRequest(trackId, fileId, nextSequence);
        var reader = _subscribe(packet, p => IsOurPackage(p, nextSequence));
        await foreach (var aespacket in reader.ReadAllAsync(ct))
        {
            _removePackageListener(reader);
            switch (aespacket.Command)
            {
                case SpotifyPacketType.AesKey:
                    var key = aespacket.Data.Slice(4, 16);
                    return Right(new AudioKey(key));
                case SpotifyPacketType.AesKeyError:
                    var errorCode = aespacket.Data.Span[4];
                    var errorType = aespacket.Data.Span[5];
                    return Left(new AesKeyError(errorCode, errorType));
            }
        }

        throw new Exception("Should not happen");
    }

    private static bool IsOurPackage(SpotifySendPacket spotifySendPacket, uint nextSequence)
    {
        if (spotifySendPacket.Command is SpotifyPacketType.AesKey or SpotifyPacketType.AesKeyError)
        {
            //realistically we only fetch one key at a time, so we can just check if the seq is the same
            return BinaryPrimitives.ReadUInt32BigEndian(spotifySendPacket.Data.Slice(0, 4).Span) == nextSequence;
        }

        return false;
    }
}