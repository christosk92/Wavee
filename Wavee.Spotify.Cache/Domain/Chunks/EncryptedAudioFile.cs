namespace Wavee.Spotify.Cache.Domain.Chunks;

public readonly record struct EncryptedAudioFile(ReadOnlyMemory<byte> Data, string FileId, int FormatType);