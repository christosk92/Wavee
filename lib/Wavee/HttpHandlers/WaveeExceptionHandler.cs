using Microsoft.Extensions.Logging;
using Wavee.Exceptions;

namespace Wavee.HttpHandlers;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

internal sealed class WaveeExceptionHandler : DelegatingHandler
{
    private readonly ILogger<WaveeExceptionHandler> _logger;

    public WaveeExceptionHandler(ILogger<WaveeExceptionHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            //Log: Sending [method] request to [url]
            var response = await base.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotModified)
            {
                return response;
            }
            else
            {
                string content = await response.Content.ReadAsStringAsync(cancellationToken);
                var statusCode = response.StatusCode;
                _logger.LogError("Received {StatusCode} response from {Url}: {Content}", statusCode, request.RequestUri,
                    content);
                var httpRequestException = new HttpRequestException(
                    $"Response status code does not indicate success: {(int)statusCode} ({statusCode}).",
                    null,
                    statusCode);

                if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
                {
                    throw new WaveeCouldNotAuthenticateException(
                        $"Authentication failed with status code {statusCode}: {content}",
                        content,
                        statusCode,
                        httpRequestException);
                }
                else
                {
                    throw new WaveeNetworkException(
                        $"HTTP request failed with status code {statusCode}: {content}",
                        content,
                        statusCode,
                        httpRequestException);
                }
            }
        }
        catch (Exception ex)
        {
            throw new WaveeUnknownException("An unknown error occurred while processing the request.", ex);
        }
    }
}