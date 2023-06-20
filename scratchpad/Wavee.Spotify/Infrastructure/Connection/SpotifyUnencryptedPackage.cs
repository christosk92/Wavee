namespace Wavee.Spotify.Infrastructure.Connection;

internal readonly ref struct SpotifyUnencryptedPackage
{
    public readonly SpotifyPacketType Type;
    public readonly ReadOnlySpan<byte> Payload;

    public SpotifyUnencryptedPackage(SpotifyPacketType type, ReadOnlySpan<byte> payload)
    {
        Type = type;
        Payload = payload;
    }
}

internal class BoxedSpotifyPackage
{
    public readonly SpotifyPacketType Type;
    public readonly ReadOnlyMemory<byte> Payload;

    public BoxedSpotifyPackage(SpotifyPacketType type, ReadOnlyMemory<byte> payload)
    {
        Type = type;
        Payload = payload;
    }
}