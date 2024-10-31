using Wavee.Enums;

namespace Wavee.Models.Token;

public readonly record struct SpotifyToken(string Value, SpotifyTokenType Type, DateTimeOffset AbsoluteExpiration)
{
    private static TimeSpan Offset = TimeSpan.FromSeconds(30);

    public bool IsExpired => DateTimeOffset.UtcNow >= AbsoluteExpiration - Offset;
}