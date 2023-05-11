using Spotify.Metadata;

namespace Wavee.Spotify.Cache.Domain.Chunks;

public readonly record struct NewAudioFile(AudioFile File,
    ReadOnlyMemory<byte> Data,
    DateTimeOffset CreatedAt);