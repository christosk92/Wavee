namespace Wavee.Spotify.Clients.Mercury.Key;

public readonly record struct AesKeyError(byte ErrorCode, byte ErrorType);