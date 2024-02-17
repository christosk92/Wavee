using System.Runtime.Serialization;
using System.Text.Json;
using Wavee.Core.Extensions;
using Wavee.Spotify.Http.Interfaces;

namespace Wavee.Spotify.Exceptions;

[Serializable]
public class ApiException : Exception
{
    public IResponse? Response { get; set; }

    public ApiException(IResponse response) : base(ParseAPIErrorMessage(response))
    {
        Guard.NotNull(nameof(response), response);
        Response = response;
    }

    public ApiException()
    {
    }

    public ApiException(string message) : base(message)
    {
    }

    public ApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

#if NET8_0_OR_GREATER
    [Obsolete(
        "This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
#endif
    protected ApiException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        Response = info.GetValue("ApiException.Response", typeof(IResponse)) as IResponse;
    }

    private static string? ParseAPIErrorMessage(IResponse response)
    {
        using var bodyStr = response.Body;
        if (bodyStr == null)
        {
            return null;
        }

        using var jsondoc = JsonDocument.Parse(bodyStr);

        return jsondoc.RootElement.ToString();
    }


#if NET8_0_OR_GREATER
    [Obsolete(
        "This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
#endif
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue("ApiException.Response", Response);
    }
}