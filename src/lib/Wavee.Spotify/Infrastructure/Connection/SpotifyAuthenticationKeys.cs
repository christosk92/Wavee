namespace Wavee.Spotify.Infrastructure.Connection;

public readonly record struct SpotifyAuthenticationKeys(
    ReadOnlyMemory<byte> SendKey,
    ReadOnlyMemory<byte> ReceiveKey,
    ReadOnlyMemory<byte> Challenge);