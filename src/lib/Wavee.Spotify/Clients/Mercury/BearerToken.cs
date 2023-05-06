using System.Text.Json;

namespace Wavee.Spotify.Clients.Mercury;

internal readonly record struct BearerToken(
    string AccessToken,
    TimeSpan ExpiresIn,
    string TokenType,
    string[] Scopes,
    DateTimeOffset Timestamp
)
{
    private const int EXPIRY_THRESHOLD_S = 20;

    public bool Expired => Timestamp + (ExpiresIn - TimeSpan.FromSeconds(EXPIRY_THRESHOLD_S))
                           < DateTimeOffset.Now;

    public const string MERCURY_TOKEN_TYPE = "mercury";

    public static BearerToken ParseFrom(ReadOnlyMemory<byte> bodySpan)
    {
        using var jsonDocument = JsonDocument.Parse(bodySpan);
        var root = jsonDocument.RootElement;

        var accessToken = root.GetProperty("accessToken").GetString();
        var expiresIn = root.GetProperty("expiresIn").GetInt32();
        var tokenType = root.GetProperty("tokenType").GetString();
        var scopes = root.GetProperty("scope").EnumerateArray().Select(x => x.GetString()).ToArray();
        var timestamp = DateTimeOffset.Now;

        return new BearerToken(accessToken, TimeSpan.FromSeconds(expiresIn), tokenType, scopes, timestamp);
    }
}