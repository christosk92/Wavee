namespace Wavee.Spotify.Contracts.AudioKeys;

public readonly record struct AesKeyError(byte ErrorCode, byte ErrorType);