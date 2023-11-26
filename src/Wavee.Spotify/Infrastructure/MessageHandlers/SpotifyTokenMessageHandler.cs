using System.Net.Http.Headers;
using Mediator;
using Wavee.Spotify.Application.Authentication.Queries;

namespace Wavee.Spotify.Infrastructure.MessageHandlers;

internal sealed class SpotifyTokenMessageHandler : DelegatingHandler
{
    private readonly IMediator _mediator;
    
    public SpotifyTokenMessageHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _mediator.Send(new GetSpotifyTokenQuery
        {
            Username = null
        }, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}

// internal sealed class GzipMessageHandler : DelegatingHandler
// {
//     public GzipMessageHandler(SpotifyTokenMessageHandler inner) : base(inner)
//     {
//     }
//     protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
//         CancellationToken cancellationToken)
//     {
//         request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
//         return await base.SendAsync(request, cancellationToken);
//     }
// }