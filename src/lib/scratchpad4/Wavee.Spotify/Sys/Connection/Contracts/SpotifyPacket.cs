namespace Wavee.Spotify.Sys.Connection.Contracts;

internal readonly record struct SpotifyPacket(SpotifyPacketType Command, ReadOnlyMemory<byte> Data);