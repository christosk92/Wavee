namespace Wavee.Spotify.Core.Models.Connection;

internal readonly ref struct SpotifyRefPackage
{
    public required SpotifyPacketType Type { get; init; }
    public required ReadOnlySpan<byte> Data { get; init; }
}

internal readonly struct SpotifyPackage
{
    public required SpotifyPacketType Type { get; init; }
    public required ReadOnlyMemory<byte> Data { get; init; }
}