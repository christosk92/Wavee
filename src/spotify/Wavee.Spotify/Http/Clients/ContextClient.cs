using Eum.Spotify.context;
using Wavee.Core.Extensions;
using Wavee.Spotify.Http.Interfaces;
using Wavee.Spotify.Http.Interfaces.Clients;

namespace Wavee.Spotify.Http.Clients;

internal sealed class ContextClient : ApiClient, IContextClient
{
    public ContextClient(IAPIConnector apiConnector) : base(apiConnector)
    {
    }

    public Task<Context> ResolveContext(string contextId, CancellationToken cancellationToken)
    {
        Guard.NotNullOrEmptyOrWhitespace(contextId, nameof(contextId));
        var url = SpotifyUrls.Context.Resolve(contextId);
        var uri = new Uri(url);

        return Api.Get<Context>(uri, cancellationToken);
    }

    public Task<Context> ResolveContextRaw(string contextUrl, CancellationToken cancellationToken)
    {
        Guard.NotNullOrEmptyOrWhitespace(contextUrl, nameof(contextUrl));
        var uri = new Uri("https://spclient.com/" + contextUrl);

        return Api.Get<Context>(uri, cancellationToken);
    }
}