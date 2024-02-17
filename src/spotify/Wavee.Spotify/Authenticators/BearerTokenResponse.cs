namespace Wavee.Spotify.Authenticators;

public sealed class BearerTokenResponse : IUserToken
{
    public string AccessToken { get; set; } = default!;
    public string TokenType { get; set; } = default!;
    public int ExpiresIn { get; set; }
    public string Scope { get; set; } = default!;

    /// <summary>
    ///   Auto-Initalized to UTC Now
    /// </summary>
    /// <value></value>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsExpired
    {
        get => CreatedAt.AddSeconds(ExpiresIn) <= DateTime.UtcNow;
    }
}
public interface IUserToken : IToken
{
    /// <summary>
    /// Comma-Seperated list of scopes
    /// </summary>
    public string Scope { get; set; }
}

/// <summary>
/// A token to access the Spotify API
/// </summary>
public interface IToken
{
    /// <summary>
    /// Access token string
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// Type of this token (eg. Bearer)
    /// </summary>
    public string TokenType { get; set; }

    /// <summary>
    /// Auto-Initalized to UTC Now
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Is the token still valid?
    /// </summary>
    public bool IsExpired { get; }
}