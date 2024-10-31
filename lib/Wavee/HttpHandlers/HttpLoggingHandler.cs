using Microsoft.Extensions.Logging;

namespace Wavee.HttpHandlers;

public class HttpLoggingHandler : DelegatingHandler
{
    private readonly ILogger<HttpLoggingHandler> _logger;

    public HttpLoggingHandler(ILogger<HttpLoggingHandler> logger)
    {
        _logger = logger;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Sending {Method} request to {Url}", request.Method, request.RequestUri);
        var response = await base.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotModified)
        {
            return response;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var statusCode = response.StatusCode;
        _logger.LogError("Received {StatusCode} response from {Url}: {Content}", statusCode, request.RequestUri, content);
        return response;
    }
}