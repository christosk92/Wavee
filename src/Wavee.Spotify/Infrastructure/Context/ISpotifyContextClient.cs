using Wavee.Spotify.Core.Models.Common;

namespace Wavee.Spotify.Infrastructure.Context;

internal interface ISpotifyContextClient
{
    Task<Eum.Spotify.context.Context> ResolveContext(string contextId, CancellationToken cancellationToken);
    Task<Eum.Spotify.context.Context> ResolveArtistContext(SpotifyId artistId, CancellationToken cancellationToken);
}