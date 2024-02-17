namespace Wavee.Core;

public interface IWaveePlayContext
{
    ValueTask<IWaveeMediaSource?> GetAt(int index, CancellationToken cancellationToken = default);
}