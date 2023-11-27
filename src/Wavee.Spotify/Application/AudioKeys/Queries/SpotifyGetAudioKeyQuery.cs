using Google.Protobuf;
using Mediator;
using Wavee.Spotify.Common;

namespace Wavee.Spotify.Application.AudioKeys.Queries;

public sealed class SpotifyGetAudioKeyQuery : IQuery<byte[]>
{
    public required SpotifyId ItemId { get; init; }
    public required ByteString FileId { get; init; }
}