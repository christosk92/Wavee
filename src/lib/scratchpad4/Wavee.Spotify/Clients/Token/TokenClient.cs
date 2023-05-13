using System.Text.Json;
using System.Text.Json.Serialization;
using Wavee.Spotify.Clients.Mercury;

namespace Wavee.Spotify.Clients.Token;

internal readonly struct TokenClient : ITokenClient
{
    private static readonly AtomHashMap<Guid, MercuryTokenData> Tokens =
        LanguageExt.AtomHashMap<Guid, MercuryTokenData>.Empty;

    private readonly Guid _connectionId;
    private readonly IMercuryClient _mercuryClient;

    public TokenClient(Guid connectionId, IMercuryClient mercuryClient)
    {
        _connectionId = connectionId;
        _mercuryClient = mercuryClient;
    }

    public async ValueTask<string> GetToken(CancellationToken token = default)
    {
        IMercuryClient mercuryClient = _mercuryClient;
        Guid connectionId = _connectionId;
        var result = Tokens.Find(x => !x.Value.IsExpired)
            .MatchAsync(
                Some: t => t.Value.AccessToken,
                None: async () =>
                {
                    var uri = string.Format(KEYMASTER_URI, string.Join(",", scopes), SpotifyConstants.KEYMASTER_CLIENT_ID);
                    var response = await mercuryClient.Get(uri, token);
                    var tokenData = JsonSerializer.Deserialize<MercuryTokenData>(response.Body.Span);
                    Tokens.AddOrUpdate(connectionId, None: () => tokenData with
                    {
                        CreatedAt = DateTimeOffset.UtcNow
                    }, Some: _ => tokenData with
                    {
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                    return tokenData.AccessToken;
                }
            );
        
        return await result;
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
    private const string KEYMASTER_URI =
        "hm://keymaster/token/authenticated?scope={0}&client_id={1}&device_id=";
    private readonly record struct MercuryTokenData(
        [property: JsonPropertyName("accessToken")]
        string AccessToken,
        [property: JsonPropertyName("expiresIn")]
        ulong ExpiresIn,
        [property: JsonPropertyName("tokenType")]
        string TokenType,
        [property: JsonPropertyName("scope")] string[] Scope,
        [property: JsonPropertyName("permissions")]
        ushort[] Permissions)
    {
        internal DateTimeOffset CreatedAt { get; init; }
        public bool IsExpired => CreatedAt.AddSeconds(ExpiresIn) < DateTimeOffset.UtcNow;
    }
}