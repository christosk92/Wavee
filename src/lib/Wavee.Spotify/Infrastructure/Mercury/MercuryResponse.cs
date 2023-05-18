using Eum.Spotify;
using LanguageExt;

namespace Wavee.Spotify.Infrastructure.Mercury;

public readonly record struct MercuryResponse(
    Header Header,
    ReadOnlyMemory<byte> Payload);