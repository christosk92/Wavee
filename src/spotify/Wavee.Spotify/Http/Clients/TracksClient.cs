using Spotify.Metadata;
using Wavee.Core.Extensions;
using Wavee.Spotify.Http.Interfaces;
using Wavee.Spotify.Http.Interfaces.Clients;
using Wavee.Spotify.Models.Common;
using Wavee.Spotify.Models.Mapping;
using Wavee.Spotify.Models.Response;

namespace Wavee.Spotify.Http.Clients;

public sealed class TracksClient : ApiClient, ITracksClient
{
    public TracksClient(IAPIConnector apiConnector) : base(apiConnector)
    {
    }

    public async Task<SpotifyTrackInfo> Get(SpotifyId id, CancellationToken cancellationToken)
    {
        if (id.Type is not AudioItemType.Track) throw new ArgumentException("Invalid id type", nameof(id));

        var url = new Uri(SpotifyUrls.Track.Get(id.ToBase16()));
        var track = await Api.Get<Track>(url, cancellationToken);
        return track.MapToDto(id);
    }
}