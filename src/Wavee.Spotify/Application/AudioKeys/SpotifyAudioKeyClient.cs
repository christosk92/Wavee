using Google.Protobuf;
using Mediator;
using Wavee.Spotify.Application.AudioKeys.Queries;
using Wavee.Spotify.Common;

namespace Wavee.Spotify.Application.AudioKeys;

internal sealed class SpotifyAudioKeyClient : ISpotifyAudioKeyClient
{
    private readonly IMediator _mediator;

    public SpotifyAudioKeyClient(IMediator mediator)
    {
        _mediator = mediator;
    }

    public ValueTask<byte[]> GetAudioKey(SpotifyId itemId, ByteString fileId,
        CancellationToken cancellationToken = default) =>
        _mediator.Send(new SpotifyGetAudioKeyQuery
        {
            ItemId = itemId,
            FileId = fileId
        }, cancellationToken);
}

public interface ISpotifyAudioKeyClient
{
    ValueTask<byte[]> GetAudioKey(SpotifyId itemId, ByteString fileId,
        CancellationToken cancellationToken = default);
}