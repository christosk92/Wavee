using LanguageExt;
using Spotify.Metadata;
using Wavee.Core.Ids;

namespace Wavee.Spotify.Infrastructure.AudioKey;

public interface IAudioKeyProvider
{
    Task<Either<AesKeyError, ReadOnlyMemory<byte>>> GetAudioKey(AudioId id, AudioFile file, CancellationToken ct = default);
}