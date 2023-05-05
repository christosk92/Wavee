using Google.Protobuf;
using Wavee.Spotify.Common;
using Wavee.Spotify.Infrastructure.Sys;

namespace Wavee.Spotify.Clients.AudioKeys;

public interface IAudioKeys
{
    Task<Either<AesKeyError, ReadOnlyMemory<byte>>> GetAudioKey(SpotifyId itemId, ByteString fileId,
        CancellationToken ct = default);
}

internal readonly struct AudioKeysClientImpl : IAudioKeys
{
    private readonly Guid _connectionId;
    private readonly Ref<Option<uint>> _nextAudioKeySequence;

    public AudioKeysClientImpl(Guid connectionId, Ref<Option<uint>> nextAudioKeySequence)
    {
        _connectionId = connectionId;
        _nextAudioKeySequence = nextAudioKeySequence;
    }

    public async Task<Either<AesKeyError, ReadOnlyMemory<byte>>> GetAudioKey(SpotifyId itemId, ByteString fileId,
        CancellationToken ct = default)
    {
        var listenerResult = SpotifyRuntime.GetChannelReader(_connectionId);
        var getWriter = SpotifyRuntime.GetSender(_connectionId);

        var response =
            await AudioKeyRuntime.Request(itemId, fileId, _nextAudioKeySequence, getWriter, listenerResult);

        var run = SpotifyRuntime.RemoveListener(_connectionId, listenerResult);

        return response;
    }
}