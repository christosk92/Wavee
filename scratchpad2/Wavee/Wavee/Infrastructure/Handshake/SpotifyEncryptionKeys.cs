namespace Wavee.Spotify.Infrastructure.Handshake;

public readonly record struct SpotifyEncryptionKeys(ReadOnlyMemory<byte> SendKey, ReadOnlyMemory<byte> ReceiveKey);