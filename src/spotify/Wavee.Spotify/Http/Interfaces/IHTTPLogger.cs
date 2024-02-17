namespace Wavee.Spotify.Http.Interfaces;

public interface IHttpLogger
{
    void OnRequest(IRequest request);
    void OnResponse(IResponse response);
}