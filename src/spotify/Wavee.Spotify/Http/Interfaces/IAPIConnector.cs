namespace Wavee.Spotify.Http.Interfaces;

public interface IAPIConnector
{
    event EventHandler<IResponse>? ResponseReceived;

    Task<T> Put<T>(Uri uri, Dictionary<string, string>? headers, object? body, RequestContentType? bodyType,
        CancellationToken cancel = default);

    Task<T> Get<T>(Uri uri, CancellationToken cancel = default);

    Task<T> Post<T>(Uri uri, IDictionary<string, string>? parameters, object? body,
        RequestContentType? bodyType,
        Dictionary<string, string>? headers, CancellationToken cancel = default);

    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken none);
}