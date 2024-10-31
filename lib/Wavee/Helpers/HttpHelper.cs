using System.Text.Json;
using Google.Protobuf;

namespace Wavee.Helpers;

public static class HttpHelper
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = SpotifyClient.AppJsonSerializerContext.Default
    };

    private static readonly string[] JsonContentTypes = { "application/json", "text/json" };

    private static readonly string[] ProtobufContentTypes =
    {
        "application/x-protobuf", "application/octet-stream",
        "vnd.spotify/metadata-track"
    };

    private static readonly Uri SpClientUrl = new Uri("https://spclient.com/");

    public static async Task<T> DoGetProtobufRequest<T>(this HttpClient httpClient,
        string relativeUrl,
        MessageParser<T> parser,
        CancellationToken cancellationToken = default) where T : IMessage<T>
    {
        var finalUrl = new Uri(SpClientUrl, relativeUrl);
        var request = new HttpRequestMessage(HttpMethod.Get, finalUrl);

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType?.Split(';')[0]?.Trim();
        if (IsProtobufContentType(contentType))
        {
            if (!typeof(IMessage).IsAssignableFrom(typeof(T)))
            {
                throw new InvalidOperationException(
                    $"Type '{typeof(T).FullName}' must implement IMessage for Protobuf deserialization.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            var message = parser.ParseFrom(stream);
            return message;
        }
        else
        {
            // throw: "Unsupported content type: '{contentType}'." If you want to do a json request, use DoGetJsonRequest
            throw new NotSupportedException(
                $"Unsupported content type: '{contentType}'. If you want to do a json request, use DoGetJsonRequest.");
        }
    }

    public static async Task<T> DoGetJsonRequest<T>(this HttpClient httpClient, string relativeUrl,
        CancellationToken cancellationToken = default)
    {
        var finalUrl = new Uri(SpClientUrl, relativeUrl);
        var request = new HttpRequestMessage(HttpMethod.Get, finalUrl);

        var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType?.Split(';')[0]?.Trim();

        if (IsJsonContentType(contentType))
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        else if (IsProtobufContentType(contentType))
        {
            throw new NotSupportedException(
                $"Unsupported content type: '{contentType}'. If you want to do a protobuf request, use DoGetProtobufRequest.");
        }
        else
        {
            throw new NotSupportedException($"Unsupported content type: '{contentType}'.");
        }
    }

    private static bool IsJsonContentType(string contentType)
    {
        foreach (var jsonContentType in JsonContentTypes)
        {
            if (string.Equals(contentType, jsonContentType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsProtobufContentType(string contentType)
    {
        foreach (var protobufContentType in ProtobufContentTypes)
        {
            if (string.Equals(contentType, protobufContentType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}