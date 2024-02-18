using Google.Protobuf;
using Wavee.Spotify.Models.Common;

namespace Wavee.Spotify.Http.Interfaces.Clients;

internal interface IAudioKeyClient
{
    ValueTask<byte[]?> GetAudioKey(SpotifyId trackId, ByteString fileId, CancellationToken cancellationToken);
}