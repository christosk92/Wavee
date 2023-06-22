using LanguageExt;
using Spotify.Metadata;
using Wavee.Id;
using Wavee.Infrastructure.AudioKey;

namespace Wavee.AudioKey.Live;

internal readonly struct LiveSpotifyAudioKeysClient : ISpotifyAudioKeysClient
{
    private readonly Guid _connectionId;

    public LiveSpotifyAudioKeysClient(Guid connectionId)
    {
        _connectionId = connectionId;
    }

    public Task<Option<byte[]>> GetAudioKey(SpotifyId id, AudioFile file, CancellationToken cancellationToken = default)
    {
        return SpotifyAudioKeysHandler.RequestKey(_connectionId, id, file, cancellationToken);
    }
}