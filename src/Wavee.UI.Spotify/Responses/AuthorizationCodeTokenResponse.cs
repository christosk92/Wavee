using System.Text.Json.Serialization;

namespace Wavee.UI.Spotify.Responses;

public sealed class AuthorizationCodeTokenResponse
{
    [JsonPropertyName("access_token")] public required string AccessToken { get; init; }
    [JsonPropertyName("username")] public required string Username { get; init; }
}