using Mediator;
using Wavee.Spotify.Application.Authentication.Queries;
using Wavee.Spotify.Common.Contracts;

namespace Wavee.Spotify;

internal sealed class SpotifyClient : ISpotifyClient
{
    private readonly IMediator _mediator;

    public SpotifyClient(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<string> Test()
    {
        var token = await _mediator.Send(new GetSpotifyTokenQuery(), new CancellationToken());
        return token;
    }
}