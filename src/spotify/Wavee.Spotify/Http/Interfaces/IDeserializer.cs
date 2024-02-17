namespace Wavee.Spotify.Http.Interfaces;

public interface IDeserializer
{
    void SerializeRequest(IRequest request);
    IApiResponse<T> DeserializeResponse<T>(IResponse response);
}