using System.Net;

namespace Wavee.Spotify.Http.Interfaces;

public interface IResponse : IDisposable, IAsyncDisposable
{
    Stream? Body { get; }

    IReadOnlyDictionary<string, string> Headers { get; }

    HttpStatusCode StatusCode { get; }

    string? ContentType { get; }
}