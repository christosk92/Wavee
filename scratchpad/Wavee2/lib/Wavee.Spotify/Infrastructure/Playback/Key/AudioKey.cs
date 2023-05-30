namespace Wavee.Spotify.Infrastructure.Playback.Key;

public readonly record struct AudioKey(ReadOnlyMemory<byte> Key);