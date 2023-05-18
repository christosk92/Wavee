using System.Net.Sockets;

namespace Wavee.Spotify.Infrastructure.Connection.Crypto;

public readonly record struct SpotifyConnectionRecord(
    ReadOnlyMemory<byte> SendKey,
    ReadOnlyMemory<byte> ReceiveKey,
    uint SendSequence,
    uint ReceiveSequence);