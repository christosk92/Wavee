using System.Net.Http.Headers;
using Mediator;
using Wavee.Spotify.Application.Authentication.Queries;
using Wavee.Spotify.Application.Common.Queries;

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

internal sealed class SpotifyPrependSpClientUrlHandler : DelegatingHandler
{
    private readonly IMediator _mediator;

    public SpotifyPrependSpClientUrlHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var spclient = await _mediator.Send(new SpotifyGetAdaptiveApiUrlQuery
        {
            Type = SpotifyApiUrlType.SpClient,
            DontReturnThese = null
        }, cancellationToken);

        var url = spclient.Url(true, false);
        //https://spclient.com
        var newUrl = request.RequestUri.ToString()
            .Replace("https://spclient.com", url);
        request.RequestUri = new Uri(newUrl);
        return await base.SendAsync(request, cancellationToken);
    }
}