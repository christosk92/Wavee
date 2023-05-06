using Google.Protobuf;
using LanguageExt;
using Wavee.Spotify.Contracts.Common;

namespace Wavee.Spotify.Contracts.AudioKeys;

public interface IAudioKeys
{
    Task<Either<AesKeyError, ReadOnlyMemory<byte>>> GetAudioKey(SpotifyId itemId, ByteString fileId,
        CancellationToken ct = default);
}