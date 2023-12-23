using Wavee.Spotify.Core.Models.Credentials;

namespace Wavee.Spotify.Core.Interfaces.Clients;

public interface ISpotifyTokenClient
{
    ValueTask<SpotifyAccessToken> GetAccessToken(CancellationToken cancellationToken = default);
}