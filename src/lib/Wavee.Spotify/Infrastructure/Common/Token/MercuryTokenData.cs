using System.Text.Json.Serialization;
namespace Wavee.Spotify.Infrastructure.Common.Token;

internal readonly record struct MercuryTokenData(
    [property: JsonPropertyName("accessToken")]
    string AccessToken, 
    [property: JsonPropertyName("expiresIn")]
    ulong ExpiresIn, 
    [property: JsonPropertyName("tokenType")]
    string TokenType, 
    [property: JsonPropertyName("scope")] 
    string[] Scope,
    [property: JsonPropertyName("permissions")]
    ushort[] Permissions);