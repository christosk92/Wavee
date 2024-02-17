using System.Globalization;
using System.Runtime.Serialization;
using Wavee.Core.Extensions;
using Wavee.Spotify.Http.Interfaces;

namespace Wavee.Spotify.Exceptions;

[Serializable]
public class ApiTooManyRequestsException : ApiException
{
    public TimeSpan RetryAfter { get; }

    public ApiTooManyRequestsException(IResponse response) : base(response)
    {
        Guard.NotNull(nameof(response), response);
            
        if (response.Headers.TryGetValue("Retry-After", out string? retryAfter))
        {
            RetryAfter = TimeSpan.FromSeconds(int.Parse(retryAfter, CultureInfo.InvariantCulture));
        }
    }

    public ApiTooManyRequestsException() { }

    public ApiTooManyRequestsException(string message) : base(message) { }

    public ApiTooManyRequestsException(string message, Exception innerException) : base(message, innerException) { }

#if NET8_0_OR_GREATER
    [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
#endif
    protected ApiTooManyRequestsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}