using System.Text.Json;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;

namespace Wavee.Spotify.Infrastructure.Mercury.Token;

public readonly struct TokenClient
{
    private static AtomHashMap<Guid, MercuryTokenData> _tokenCache =
        LanguageExt.AtomHashMap<Guid, MercuryTokenData>.Empty;

    private readonly Guid _connectionId;
    private readonly MercuryClient _mercuryClient;

    public TokenClient(Guid connectionId, MercuryClient mercuryClient)
    {
        _connectionId = connectionId;
        _mercuryClient = mercuryClient;
    }

    public async ValueTask<string> GetToken(CancellationToken ct = default)
    {
        var potentialToken = _tokenCache.Find(_connectionId)
            .Bind(x => x.IsExpired ? None : Some(x.AccessToken));
        if (potentialToken.IsSome)
        {
            return potentialToken.ValueUnsafe();
        }

        const string KEYMASTER_URI = "hm://keymaster/token/authenticated?scope={0}&client_id={1}&device_id=";
        var uri = string.Format(KEYMASTER_URI, string.Join(",", scopes), SpotifyConstants.KEYMASTER_CLIENT_ID);

        var response = await _mercuryClient.Get(uri, ct);
        var tokenData = JsonSerializer.Deserialize<MercuryTokenData>(response.Payload.Span);
        _tokenCache.AddOrUpdate(_connectionId,
            Some: _ => tokenData with
            {
                CreatedAt = DateTimeOffset.Now.Subtract(TimeSpan.FromMinutes(1))
            },
            None: tokenData);
        
        return tokenData.AccessToken;
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