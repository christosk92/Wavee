using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Wavee.Core.Extensions;
using Wavee.Spotify.Authenticators;
using Wavee.Spotify.Http.Interfaces;

namespace Wavee.Spotify.Http.Serializers;

internal sealed partial class SystemTextJsonSerializer : IJsonSerializer
{
    private readonly JsonSerializerOptions _serializerSettings;

    public SystemTextJsonSerializer()
    {
        _serializerSettings = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault
            ? new DefaultJsonTypeInfoResolver()
            : MyContext.Default
        };
    }

    public void SerializeRequest(IRequest request)
    {
        Guard.NotNull(nameof(request), request);

        var val = request.Body?.Data;
        if (val is string || val is Stream || val is HttpContent || val is null)
        {
            return;
        }

        request.Body = (JsonSerializer.Serialize(request.Body, _serializerSettings), RequestContentType.Json);
    }

    public IApiResponse<T> DeserializeResponse<T>(IResponse response)
    {
        //Ensure.ArgumentNotNull(response, nameof(response));
        Guard.NotNull(nameof(response), response);

        if (response.ContentType?.Equals("application/json", StringComparison.Ordinal) is true ||
            response.ContentType == null)
        {
            if (response.Body is null)
            {
                return new ApiResponse<T>(response);
            }

            var body = JsonSerializer.Deserialize<T>(response.Body, _serializerSettings);
            return new ApiResponse<T>(response, body!);
        }

        return new ApiResponse<T>(response);
    }

    static JsonSerializerOptions CreateDefaultOptions()
    {
        return new()
        {
            TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault
                ? new DefaultJsonTypeInfoResolver()
                : MyContext.Default
        };
    }

    [JsonSerializable(typeof(BearerTokenResponse))]
    [JsonSerializable(typeof(AuthorizationCodeTokenResponse))]
    public partial class MyContext : JsonSerializerContext { }
}

public sealed class AuthorizationCodeTokenResponse
{
    [JsonPropertyName("access_token")] public required string AccessToken { get; init; }
    [JsonPropertyName("username")] public required string Username { get; init; }
}