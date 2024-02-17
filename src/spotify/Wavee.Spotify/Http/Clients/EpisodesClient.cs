using Wavee.Spotify.Http.Interfaces;
using Wavee.Spotify.Http.Interfaces.Clients;
using Wavee.Spotify.Models.Common;
using Wavee.Spotify.Models.Response;

namespace Wavee.Spotify.Http.Clients;

public sealed class EpisodesClient : ApiClient, IEpisodesClient
{
    public EpisodesClient(IAPIConnector apiConnector) : base(apiConnector)
    {
    }

    public Task<SpotifyEpisodeInfo> Get(SpotifyId id, CancellationToken cancellationToken)
    {
        if (id.Type is not AudioItemType.PodcastEpisode) throw new ArgumentException("Invalid id type", nameof(id));

        var url = new Uri(SpotifyUrls.Episode.Get(id.ToBase16()));
        return Api.Get<SpotifyEpisodeInfo>(url, cancellationToken);
    }
}