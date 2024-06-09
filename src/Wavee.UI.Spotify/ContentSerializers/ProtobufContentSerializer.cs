using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Refit;

namespace Wavee.UI.Spotify.ContentSerializers;

public sealed class ProtobufContentSerializer : IHttpContentSerializer
{
    public async Task<T> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default)
    {
        ReadOnlyMemory<byte> data = await content.ReadAsByteArrayAsync(cancellationToken);
        var message = Activator.CreateInstance<T>();
        if (message is IMessage protobufMessage)
        {
            protobufMessage.MergeFrom(data.Span);
            return (T)protobufMessage;
        }
        throw new InvalidOperationException("Response content is not a protobuf message.");
    }

 
    public HttpContent ToHttpContent<T>(T item)
    {
        if (item is IMessage protobufMessage)
        {
            var content = new ByteArrayContent(protobufMessage.ToByteArray());
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-protobuf");
            return content;
        }
        throw new InvalidOperationException("Request content is not a protobuf message.");
    }

    public string GetFieldNameForProperty(PropertyInfo propertyInfo)
    {
        return propertyInfo.Name;
    }
}