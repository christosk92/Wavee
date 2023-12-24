using Wavee.Spotify.Core.Models.Credentials;

namespace Wavee.Spotify.Interfaces;

internal interface ISpotifyTokenService
{
    ValueTask<SpotifyAccessToken> GetAccessTokenAsync(CancellationToken cancellationToken);
}