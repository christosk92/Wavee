using Wavee.Spotify.Infrastructure.PublicApi.Me;

namespace Wavee.Spotify.Infrastructure.PublicApi;

public interface ISpotifyPublicApi
{
    Task<PrivateSpotifyUser> GetMe(CancellationToken ct = default);
    Task<HttpResponseMessage> GetDesktopHome(string types, int offset, int limit, int contentLimit, int contentOffset, CancellationToken ct = default); 
}