namespace Wavee.Spotify.Application.LegacyAuth.CommandHandlers;

public readonly record struct SpotifyEncryptionKeys(ReadOnlyMemory<byte> SendKey, ReadOnlyMemory<byte> ReceiveKey);