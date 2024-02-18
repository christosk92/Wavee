using Eum.Spotify.storage;
using Google.Protobuf;
using Wavee.Spotify.Playback;

namespace Wavee.Spotify.Http.Interfaces.Clients;

internal interface ICdnClient
{
    Task<HttpResponseMessage> StreamFromCdnAsync(string cdnUrl, long offset, long length);
    Task<StorageResolveResponse> GetAudioStorageAsync(ByteString fileId);
}