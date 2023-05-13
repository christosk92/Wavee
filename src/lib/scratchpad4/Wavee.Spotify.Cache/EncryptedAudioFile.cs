namespace Wavee.Spotify.Cache;

public readonly record struct EncryptedAudioFile(ReadOnlyMemory<byte> Data, string FileId, int FormatType);