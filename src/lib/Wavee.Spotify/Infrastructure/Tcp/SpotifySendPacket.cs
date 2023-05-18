using Wavee.Spotify.Infrastructure.Connection;

namespace Wavee.Spotify.Infrastructure.Tcp;

internal readonly record struct SpotifySendPacket(SpotifyPacketType Command, ReadOnlyMemory<byte> Data);