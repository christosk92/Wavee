using System.Buffers.Binary;
using Google.Protobuf;
using LanguageExt;
using Serilog;
using Spotify.Metadata;
using Wavee.Id;
using Wavee.Infrastructure.Connection;
using Wavee.Spotify.Infrastructure.Connection;

namespace Wavee.Infrastructure.AudioKey;

internal static class SpotifyAudioKeysHandler
{
    private static Dictionary<Guid, uint> _sequences = new Dictionary<Guid, uint>();
    private static object _lock = new object();

    public static async Task<Option<byte[]>> RequestKey(Guid connectionId, SpotifyId id, AudioFile file,
        CancellationToken ct = default)
    {
        uint seq = 0;
        lock (_lock)
        {
            if (_sequences.TryGetValue(connectionId, out var value))
            {
                seq = (uint)value;
                _sequences[connectionId] = value + 1;
            }
            else
            {
                _sequences.Add(connectionId, 0);
                seq = 0;
            }
        }

        Log.Information("Requesting audio key for {id} {file} {seq}", id, file, seq);
        var (reader, onComplete) =
            connectionId.CreateListener(((ref SpotifyUnencryptedPackage check) => Condition(ref check, seq)));
        SendInternal(connectionId, seq, id, file.FileId);
        await foreach (var aespacket in reader.ReadAllAsync(ct))
        {
            onComplete();
            switch (aespacket.Type)
            {
                case SpotifyPacketType.AesKey:
                    var key = aespacket.Payload.Slice(4, 16);
                    return key.ToArray();
                //    return Right(new AudioKey(key));
                case SpotifyPacketType.AesKeyError:
                    var errorCode = aespacket.Payload.Span[4];
                    var errorType = aespacket.Payload.Span[5];
                    throw new AesKeyError(errorCode, errorType);
                //  return Left(new AesKeyError(errorCode, errorType));
            }
        }

        throw new Exception("No response from Spotify");
    }

    private static void SendInternal(Guid connectionId, uint seq, SpotifyId id, ByteString fileId)
    {
        var pckg = AesPacketBuilder.BuildRequest(id, fileId, seq);
        SpotifyConnection.Send(connectionId, new BoxedSpotifyPackage(
            type: pckg.Type,
            payload: pckg.Payload.ToArray()
        ));
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