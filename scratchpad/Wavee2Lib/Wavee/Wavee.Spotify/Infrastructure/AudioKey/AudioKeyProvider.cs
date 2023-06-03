using System.Buffers.Binary;
using System.Diagnostics;
using System.Threading.Channels;
using Google.Protobuf;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Mercury;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Infrastructure.AudioKey;

internal readonly struct AudioKeyProvider : IAudioKeyProvider
{
    private readonly record struct AudioKey(AudioId TrackId, AudioFile Format, ReadOnlyMemory<byte> Key);

    private static readonly Dictionary<string, uint> SendSequences = new Dictionary<string, uint>();

    private readonly SendPackage _onPackageSend;
    private readonly Func<PackageReceiveCondition, Channel<BoxedSpotifyPacket>> _onPackageReceive;
    private readonly string _username;
    private static readonly object SendLock = new object();

    public AudioKeyProvider(
        string username,
        SendPackage onPackageSend,
        Func<PackageReceiveCondition, Channel<BoxedSpotifyPacket>> onPackageReceive)
    {
        _onPackageSend = onPackageSend;
        _onPackageReceive = onPackageReceive;
        _username = username;
        _onPackageSend = onPackageSend;
        _onPackageReceive = onPackageReceive;
        lock (SendSequences)
        {
            SendSequences.TryAdd(username, 0);
        }
    }

    public async Task<Either<AesKeyError, ReadOnlyMemory<byte>>> GetAudioKey(AudioId id, AudioFile file,
        CancellationToken ct = default)
    {
        string username = _username;
        uint seq = 0;
        lock (SendLock)
        {
            seq = SendSequences[username];
            SendSequences[username] = seq + 1;
        }

        // var seq = SendSequences.Swap(x => x.AddOrUpdate(username,
        //     None: () => 0,
        //     Some: y => y + 1
        // )).ValueUnsafe()[username];
        Debug.WriteLine($"Sending audiokey seq {seq} for {id}");
        var reader = _onPackageReceive((ref SpotifyUnencryptedPackage check) => Condition(ref check, seq));

        SendInternal(seq, id, file.FileId);

        await foreach (var aespacket in reader.Reader.ReadAllAsync(ct))
        {
            reader.Writer.Complete();
            switch (aespacket.Type)
            {
                case SpotifyPacketType.AesKey:
                    var key = aespacket.Data.Slice(4, 16);
                    return key;
                //    return Right(new AudioKey(key));
                case SpotifyPacketType.AesKeyError:
                    var errorCode = aespacket.Data.Span[4];
                    var errorType = aespacket.Data.Span[5];
                    return Left(new AesKeyError(errorCode, errorType));
            }
        }
        
        return Left(new AesKeyError(0, 0));
    }

    private void SendInternal(uint seq, AudioId id, ByteString fileId)
    {
        var pckg = AesPacketBuilder.BuildRequest(id, fileId, seq);
        _onPackageSend(pckg);
    }

    private static bool Condition(ref SpotifyUnencryptedPackage packagetocheck, uint seq)
    {
        if (packagetocheck.Type is
            SpotifyPacketType.AesKey or SpotifyPacketType.AesKeyError)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(packagetocheck.Payload.Slice(0, 4)) == seq;
        }

        return false;
    }
}