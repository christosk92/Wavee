using Wavee.Spotify.Models.Common;
using Wavee.Spotify.Models.Response;

namespace Wavee.Spotify.Http.Interfaces.Clients;

public interface IEpisodesClient
{
    Task<SpotifyEpisodeInfo> Get(SpotifyId id, CancellationToken cancellationToken);
}