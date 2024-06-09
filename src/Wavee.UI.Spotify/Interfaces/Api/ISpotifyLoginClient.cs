using System.Threading;
using System.Threading.Tasks;
using Eum.Spotify.login5v3;
using Refit;

namespace Wavee.UI.Spotify.Interfaces;

public interface ISpotifyLoginClient
{
    [Post(SpotifyUrls.Login.V3)]
    Task<LoginResponse> ExchangeToken([Body(BodySerializationMethod.Serialized)] LoginRequest request,
        CancellationToken cancellationToken = default);
}