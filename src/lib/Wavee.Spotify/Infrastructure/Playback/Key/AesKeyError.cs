namespace Wavee.Spotify.Infrastructure.Playback.Key;

public readonly record struct AesKeyError(byte ErrorCode, byte ErrorType);