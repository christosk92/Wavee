using Wavee.Spotify.Http.Interfaces;
using Wavee.Spotify.Http.Interfaces.Clients;
using Wavee.Spotify.Models.Response;

namespace Wavee.Spotify.Http.Clients;

public class UserProfileClient : ApiClient, IUserProfileClient
{
    public UserProfileClient(IAPIConnector apiConnector) : base(apiConnector)
    {
    }

    public Task<Me> Current(CancellationToken cancel = default)
    {
        return Api.Get<Me>(new Uri(SpotifyUrls.Public.Me), cancel);
    }
}