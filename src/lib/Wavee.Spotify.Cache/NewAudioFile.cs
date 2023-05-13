using Spotify.Metadata;

namespace Wavee.Spotify.Cache;

public readonly record struct NewAudioFile(AudioFile File,
    ReadOnlyMemory<byte> Data,
    DateTimeOffset CreatedAt);