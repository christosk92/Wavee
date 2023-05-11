namespace Wavee.Spotify.Infrastructure.Connection;

internal readonly record struct SpotifyPacket(SpotifyPacketType Command, ReadOnlyMemory<byte> Data);