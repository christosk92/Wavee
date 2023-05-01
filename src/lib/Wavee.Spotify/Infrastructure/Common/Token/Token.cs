namespace Wavee.Spotify.Infrastructure.Common.Token;

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
}