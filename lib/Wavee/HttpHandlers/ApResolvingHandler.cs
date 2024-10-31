using NeoSmart.AsyncLock;
using Wavee.Services;

namespace Wavee.HttpHandlers;

internal sealed class ApResolvingHandler : DelegatingHandler
{
    private readonly ApResolver _apResolver;

    public ApResolvingHandler(ApResolver apResolver)
    {
        _apResolver = apResolver;
    }

    // The point of this handler is to resolve the AP address for the request
    // If the URL contains http://spclient.com
    // we must resolve the AP address and replace the host with the resolved AP address
    // This is done by calling the ApResolver.ResolveAp method

    private static (string, int)? _cachedAp;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var uri = request.RequestUri;
        if (uri.Host == "spclient.com")
        {
            if (_cachedAp is null)
            {
                using (await _lock.LockAsync())
                {
                    if (_cachedAp == null)
                    {
                        _cachedAp = await _apResolver.ResolveAsync("spclient", cancellationToken);
                    }
                }
            }

            var resolvedUri = new UriBuilder(uri) { Host = _cachedAp.Value.Item1, Port = _cachedAp.Value.Item2 };
            request.RequestUri = resolvedUri.Uri;
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private AsyncLock _lock = new AsyncLock();
}