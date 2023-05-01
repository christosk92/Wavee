namespace Wavee.Spotify.Infrastructure.Common.Mercury;

public readonly record struct MercuryResponse(
    ulong Seq,
    string Uri, int StatusCode,
    Seq<ReadOnlyMemory<byte>> Payload,
    int TotalLength);