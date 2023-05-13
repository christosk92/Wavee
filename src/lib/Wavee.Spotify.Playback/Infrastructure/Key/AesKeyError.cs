namespace Wavee.Spotify.Playback.Infrastructure.Key;

public readonly record struct AesKeyError(byte ErrorCode, byte ErrorType);