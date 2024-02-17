using Wavee.Spotify.Http.Interfaces;

namespace Wavee.Spotify.Authenticators;

public interface IAuthenticator
{
    Task Apply(string deviceId, IRequest request, IAPIConnector apiConnector);
    
    Task<string> GetToken(string deviceId, IAPIConnector apiConnector, CancellationToken cancel = default);
}