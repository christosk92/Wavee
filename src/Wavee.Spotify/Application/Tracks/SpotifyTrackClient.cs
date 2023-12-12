using Mediator;
using Spotify.Metadata;
using Wavee.Domain.Exceptions;
using Wavee.Spotify.Application.Tracks.Queries;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Exceptions;
using Wavee.Spotify.Domain.Tracks;

namespace Wavee.Spotify.Application.Tracks;

internal sealed class SpotifyTrackClient : ISpotifyTrackClient
{
    private readonly IMediator _mediator;

    public SpotifyTrackClient(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async ValueTask<Track> GetTrack(SpotifyId trackId, CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new SpotifyGetTrackQuery
        {
            TrackId = trackId
        }, cancellationToken);
        return response;
    }
}