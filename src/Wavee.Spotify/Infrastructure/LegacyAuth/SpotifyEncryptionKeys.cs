namespace Wavee.Spotify.Infrastructure.LegacyAuth;

public readonly record struct SpotifyEncryptionKeys(ReadOnlyMemory<byte> SendKey, ReadOnlyMemory<byte> ReceiveKey);