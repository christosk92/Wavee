using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.Contracts.Interfaces.Playback;

public interface IMediaSource
{
    IPlayableItem Item { get; }
    Task<Stream> CreateStream(CancellationToken cancellationToken);
}