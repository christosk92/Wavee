namespace Wavee.Spotify.Infrastructure.AudioKey;

public readonly record struct AesKeyError(byte ErrorCode, byte ErrorType);