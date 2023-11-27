using Google.Protobuf;
using Mediator;

namespace Wavee.Spotify.Application.Playback.Queries;

public sealed class SpotifyGetChunkQuery : IQuery<(byte[] Data, long TotalSize)>
{
    public required ByteString FileId { get; init; }
    public required string? CdnUrl { get; init; }
    public required int Index { get; init; }
}