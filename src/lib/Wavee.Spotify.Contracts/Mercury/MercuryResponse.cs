using Eum.Spotify;

namespace Wavee.Spotify.Contracts.Mercury;

public readonly record struct MercuryResponse(Header Header, ReadOnlyMemory<byte> Body);