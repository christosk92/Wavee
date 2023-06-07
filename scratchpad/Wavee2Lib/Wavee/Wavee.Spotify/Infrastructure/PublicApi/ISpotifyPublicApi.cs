using Wavee.Spotify.Infrastructure.PublicApi.Me;

namespace Wavee.Spotify.Infrastructure.PublicApi;

public interface ISpotifyPublicApi
{
    Task<PrivateSpotifyUser> GetMe(CancellationToken ct = default);
}