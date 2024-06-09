using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Refit;
using Wavee.UI.Spotify.Responses;

namespace Wavee.UI.Spotify.Interfaces;

public interface ISpotifyAccountClient
{
    [Post(SpotifyUrls.Account.Token)]
    Task<AuthorizationCodeTokenResponse> GetCredentials(
        [Body(BodySerializationMethod.UrlEncoded)]
        Dictionary<string, string> formData,
        CancellationToken cancellationToken = default);
}