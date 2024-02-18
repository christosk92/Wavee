namespace Wavee.Spotify.Http.Interfaces;

public interface IHttpClient : IDisposable
{
    Task<IResponse> DoRequest(IRequest request, CancellationToken cancel = default);
    void SetRequestTimeout(TimeSpan timeout);
    Task<HttpResponseMessage> SendRaw(HttpRequestMessage request, CancellationToken cancel);
}