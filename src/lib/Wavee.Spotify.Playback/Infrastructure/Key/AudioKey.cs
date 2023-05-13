namespace Wavee.Spotify.Playback.Infrastructure.Key;

public readonly record struct AudioKey(ReadOnlyMemory<byte> Key);