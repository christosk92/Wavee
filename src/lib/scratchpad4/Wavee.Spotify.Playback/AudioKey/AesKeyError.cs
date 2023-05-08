namespace Wavee.Spotify.Sys.AudioKey;

public readonly record struct AesKeyError(byte ErrorCode, byte ErrorType);