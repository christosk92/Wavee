namespace Wavee.Spotify.Infrastructure.LegacyAuth;

internal readonly ref struct SpotifyUnencryptedPackage
{
    public required SpotifyPacketType Type { get; init; }
    public required ReadOnlySpan<byte> Payload { get; init; }
}