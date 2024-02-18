namespace Wavee.Core;

public interface IWaveePlayContext
{
    ValueTask<WaveeMediaSource?> GetAt(int index, CancellationToken cancellationToken = default);
}