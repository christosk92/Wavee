using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Wavee.Core.Extensions;
using Wavee.Spotify.Extensions;
using Wavee.Spotify.Http.Interfaces;

namespace Wavee.Spotify.Http;

public class NetHttpClient : IHttpClient
{
    private readonly HttpMessageHandler? _httpMessageHandler;
    private readonly HttpClient _httpClient;

    public NetHttpClient()
    {
        _httpClient = new HttpClient();
    }

    public NetHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IResponse> DoRequest(IRequest request, CancellationToken cancel)
    {
        Guard.NotNull(nameof(request), request);
        using var requestMsg = BuildRequestMessage(request);
        var responseMsg = await _httpClient
            .SendAsync(requestMsg, HttpCompletionOption.ResponseContentRead, cancel)
            .ConfigureAwait(false);

        return await BuildResponse(responseMsg, cancel).ConfigureAwait(false);
    }

    private static async Task<IResponse> BuildResponse(HttpResponseMessage responseMsg, CancellationToken cancel)
    {
        Guard.NotNull(nameof(responseMsg), responseMsg);

        var content = responseMsg.Content;
        var headers = responseMsg.Headers.ToDictionary(header => header.Key, header => header.Value.First());
        var body = await responseMsg.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false);
        var contentType = content.Headers?.ContentType?.MediaType;

        return new Response(headers)
        {
            ContentType = contentType,
            StatusCode = responseMsg.StatusCode,
            Body = body
        };
    }

    private static HttpRequestMessage BuildRequestMessage(IRequest request)
    {
        //Ensure.ArgumentNotNull(request, nameof(request));
        Guard.NotNull(nameof(request), request);
        var fullUri = request.Endpoint.ApplyParameters(request.Parameters);
        if (fullUri.AbsoluteUri.StartsWith("https://spclient.com"))
        {
            //gae2-spclient.spotify.com
            fullUri = new Uri(fullUri.AbsoluteUri.Replace("https://spclient.com",
                "https://gae2-spclient.spotify.com"));
        }

        var requestMsg = new HttpRequestMessage(request.Method, fullUri);
        foreach (var header in request.Headers)
        {
            requestMsg.Headers.Add(header.Key, header.Value);
        }

        switch (request.Body?.Data)
        {
            case HttpContent body:
                requestMsg.Content = body;
                switch (request.Body.Value.Item2)
                {
                    case RequestContentType.Json:
                        requestMsg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        break;
                    case RequestContentType.Protobuf:
                        requestMsg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
                        break;
                    case RequestContentType.FormUrlEncoded:
                        requestMsg.Content.Headers.ContentType =
                            new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;
            case string body:
                requestMsg.Content = new StringContent(body, Encoding.UTF8, "application/json");
                break;
            case Stream body:
                requestMsg.Content = new StreamContent(body);
                switch (request.Body.Value.Item2)
                {
                    case RequestContentType.Json:
                        requestMsg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        break;
                    case RequestContentType.Protobuf:
                        requestMsg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
                        break;
                    case RequestContentType.FormUrlEncoded:
                        requestMsg.Content.Headers.ContentType =
                            new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;
        }

        return requestMsg;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
            _httpMessageHandler?.Dispose();
        }
    }

    public void SetRequestTimeout(TimeSpan timeout)
    {
        _httpClient.Timeout = timeout;
    }

    public Task<HttpResponseMessage> SendRaw(HttpRequestMessage request, CancellationToken cancel)
    {
        return _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel);
    }
}

public class Response : IResponse
{
    public Response(IDictionary<string, string> headers)
    {
        Guard.NotNull(nameof(headers), headers);

        Headers = new ReadOnlyDictionary<string, string>(headers);
    }

    public Stream? Body { get; set; }

    public IReadOnlyDictionary<string, string> Headers { get; set; }

    public HttpStatusCode StatusCode { get; set; }

    public string? ContentType { get; set; }

    public void Dispose()
    {
        Body?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (Body != null) await Body.DisposeAsync();
    }
}