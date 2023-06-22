using LanguageExt;
using Spotify.Metadata;
using Wavee.Id;

namespace Wavee.AudioKey;

public interface ISpotifyAudioKeysClient
{
    Task<Option<byte[]>> GetAudioKey(SpotifyId id, AudioFile file, CancellationToken cancellationToken = default);
}