using System.Net.Http.Headers;
using Google.Protobuf;
using Wavee.Core.Extensions;
using Wavee.Spotify.Http.Interfaces;

namespace Wavee.Spotify.Http.Serializers;

internal sealed class ProtobufDeserializer : IProtobufDeserializer
{
    public void SerializeRequest(IRequest request)
    {
        Guard.NotNull(nameof(request), request);

        var val = request.Body?.Data;
        if (val is string || val is Stream || val is HttpContent || val is null)
        {
            return;
        }

        var dt = request.Body.Value.Data;
        if (dt is null)
        {
            return;
        }

        if (dt is IMessage msg)
        {
            var bd = new ByteArrayContent(msg.ToByteArray());
            bd.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
            request.Body = (bd, RequestContentType.Protobuf);
        }
    }

    public IApiResponse<T> DeserializeResponse<T>(IResponse response)
    {
        Guard.NotNull(nameof(response), response);

        var message = Activator.CreateInstance<T>();
        if (message is IMessage msg)
        {
            msg.MergeFrom(response.Body);
            return new ApiResponse<T>(response, (T)msg);
        }
        
        return new ApiResponse<T>(response);
    }
}