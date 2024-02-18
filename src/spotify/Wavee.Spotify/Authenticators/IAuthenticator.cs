using Google.Protobuf;
using Wavee.Spotify.Http.Interfaces;
using Wavee.Spotify.Models.Common;

namespace Wavee.Spotify.Authenticators;

public interface IAuthenticator
{
    Task Apply(string deviceId, IRequest request, IAPIConnector apiConnector);
    
    Task<string> GetToken(string deviceId, IAPIConnector apiConnector, CancellationToken cancel = default);

    protected internal Task<byte[]?> GetAudioKey(SpotifyId id, ByteString fileId, CancellationToken cancellationToken);
}