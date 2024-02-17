using Wavee.Core.Extensions;
using Wavee.Spotify.Http.Interfaces;

namespace Wavee.Spotify.Http;

public sealed class ApiResponse<T> : IApiResponse<T>
{
    public ApiResponse(IResponse response, T? body = default)
    {
        Guard.NotNull(nameof(response), response);

        Body = body;
        Response = response;
    }

    public T? Body { get; set; }

    public IResponse Response { get; set; }
}