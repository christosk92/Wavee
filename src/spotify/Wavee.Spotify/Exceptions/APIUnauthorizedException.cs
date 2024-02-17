using System.Runtime.Serialization;
using Wavee.Spotify.Http.Interfaces;

namespace Wavee.Spotify.Exceptions;

[Serializable]
public class ApiUnauthorizedException : ApiException
{
    public ApiUnauthorizedException(IResponse response) : base(response)
    {
    }

    public ApiUnauthorizedException()
    {
    }

    public ApiUnauthorizedException(string message) : base(message)
    {
    }

    public ApiUnauthorizedException(string message, Exception innerException) : base(message, innerException)
    {
    }

#if NET8_0_OR_GREATER
    [Obsolete(
        "This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
#endif
    protected ApiUnauthorizedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}