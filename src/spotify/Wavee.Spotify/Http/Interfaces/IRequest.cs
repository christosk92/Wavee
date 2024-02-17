namespace Wavee.Spotify.Http.Interfaces;

public interface IRequest
{
    Uri Endpoint { get; }
    IDictionary<string, string> Headers { get; }
    IDictionary<string, string> Parameters { get; }
    HttpMethod Method { get; }
    (object Data, RequestContentType)? Body { get; set; }
    
}

public enum RequestContentType
{
    Json,
    Protobuf,
    FormUrlEncoded
}