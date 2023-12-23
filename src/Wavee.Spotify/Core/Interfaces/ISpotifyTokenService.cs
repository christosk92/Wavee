using Wavee.Spotify.Core.Models.Credentials;

namespace Wavee.Spotify.Core.Interfaces;

internal interface ISpotifyTokenService
{
    ValueTask<SpotifyAccessToken> GetAccessTokenAsync(CancellationToken cancellationToken);
}