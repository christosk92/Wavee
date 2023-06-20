using Eum.Spotify;

namespace Wavee.Spotify.Mercury;

public readonly record struct MercuryResponse(Header Header, ReadOnlyMemory<byte> Payload);