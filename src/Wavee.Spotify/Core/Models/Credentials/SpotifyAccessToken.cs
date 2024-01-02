namespace Wavee.Spotify.Core.Models.Credentials;

public sealed class SpotifyAccessToken
{
    private static readonly TimeSpan _tokenExpirationBuffer = TimeSpan.FromSeconds(30);

    public required string Username { get; init; }
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt - _tokenExpirationBuffer;


    public override string ToString() => AccessToken;
}