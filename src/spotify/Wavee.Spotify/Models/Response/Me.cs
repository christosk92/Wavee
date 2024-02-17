namespace Wavee.Spotify.Models.Response;

public sealed class Me
{
    public required string Country { get; init; }
    public required string DisplayName { get; init; }
    public required string Email { get; init; }
}