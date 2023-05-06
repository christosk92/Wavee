using Eum.Spotify.storage;
using Google.Protobuf;

namespace Wavee.Spotify.Contracts.SpApi;

public interface ISpApi
{
    ValueTask<StorageResolveResponse> GetAudioStorage(ByteString fileId, CancellationToken ct = default);
}
