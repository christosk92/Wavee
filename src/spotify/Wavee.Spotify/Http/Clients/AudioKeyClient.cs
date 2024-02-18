using Google.Protobuf;
using Wavee.Core.Extensions;
using Wavee.Spotify.Authenticators;
using Wavee.Spotify.Http.Interfaces;
using Wavee.Spotify.Http.Interfaces.Clients;
using Wavee.Spotify.Models.Common;

namespace Wavee.Spotify.Http.Clients;

internal sealed class AudioKeyClient : IAudioKeyClient
{
    private readonly ISpotifyCache _cache;
    private readonly IAuthenticator _authenticator;

    public AudioKeyClient(IAuthenticator authenticator, ISpotifyCache cache)
    {
        _authenticator = authenticator;
        _cache = cache;
    }

    public async ValueTask<byte[]?> GetAudioKey(SpotifyId trackId, ByteString fileId, CancellationToken cancellationToken)
    {
        var cacheKey = $"audio-key-{trackId}-{fileId.ToBase64()}";

        var result = await _cache.TryGetOrFetch(cacheKey, async (_, ct) =>
        {
            var key = await _authenticator.GetAudioKey(trackId, fileId, ct);
            return key;
        }, cancellationToken);
        return result;
    }
}