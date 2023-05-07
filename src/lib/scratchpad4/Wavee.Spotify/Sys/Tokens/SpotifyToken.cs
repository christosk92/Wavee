using System.Text.Json;
using System.Text.Json.Serialization;
using LanguageExt.UnsafeValueAccess;
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Traits;
using Wavee.Spotify.Sys.Mercury;

namespace Wavee.Spotify.Sys.Tokens;

public static class SpotifyToken
{
    public static async ValueTask<string> FetchAccessToken(this SpotifyConnectionInfo connectionInfo)
    {
        var tokenMaybe = await SpotifyTokenClient<WaveeRuntime>.GetToken(connectionInfo).Run(WaveeCore.Runtime);

        return tokenMaybe.Match(
            Succ: t => t.AccessToken,
            Fail: e => throw e
        );
    }
}

internal static class SpotifyTokenClient<RT> where RT : struct, HasTCP<RT>
{
    private static readonly AtomHashMap<Guid, MercuryToken> _tokens = LanguageExt.AtomHashMap<Guid, MercuryToken>.Empty;

    public static Aff<RT, MercuryToken> GetToken(SpotifyConnectionInfo connectionInfo)
    {
        return Aff<RT, MercuryToken>(async rt =>
        {
            var token = _tokens.Find(connectionInfo.ConnectionId);
            if (token.IsSome && !token.ValueUnsafe().Expired)
            {
                return token.ValueUnsafe();
            }

            var newTokenMaybe = await GetNewToken(connectionInfo).Run(rt);
            var newToken = newTokenMaybe.Match(
                Succ: r => r,
                Fail: e => throw e
            );
            _tokens.AddOrUpdate(connectionInfo.ConnectionId, newToken);
            return newToken;
        });
    }

    private static Aff<RT, MercuryToken> GetNewToken(
        SpotifyConnectionInfo connectionInfo,
        CancellationToken ct = default)
    {
        const string url = "hm://keymaster/token/authenticated?scope={0}&client_id={1}&device_id=";
        var scope = string.Join(",", scopes);
        var finalUrl = string.Format(url, scope, SpotifyConstants.KEYMASTER_CLIENT_ID);

        return connectionInfo.Get(finalUrl, Option<string>.None, ct)
            .Map(f => JsonSerializer.Deserialize<MercuryToken>(f.Body.Span)).ToAff();
    }

    private static string[] scopes = new[]
    {
        "user-read-private",
        "user-read-email",
        "playlist-modify-public",
        "ugc-image-upload",
        "playlist-read-private",
        "playlist-read-collaborative",
        "playlist-read"
    };
}

internal struct MercuryToken
{
    private const int TokenExpireThreshold = 10;

    [JsonConstructor]
    public MercuryToken(string accessToken, int expiresIn) =>
        (AccessToken, ExpiresIn, CreatedAt) = (accessToken, expiresIn, DateTime.UtcNow);

    [JsonPropertyName("accessToken")] public string AccessToken { get; internal set; }
    [JsonPropertyName("expiresIn")] public int ExpiresIn { get; }
    [JsonIgnore] public DateTime CreatedAt { get; }

    [JsonIgnore]
    public TimeSpan RemainingTime => CreatedAt.AddSeconds(ExpiresIn)
                                     - DateTime.UtcNow;

    public override string ToString() => RemainingTime.TotalMilliseconds > 0 ? AccessToken : "Expired";

    public bool Expired
        => !(RemainingTime.TotalMilliseconds > 0);
}