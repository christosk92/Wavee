using Wavee.Core.Contracts;

namespace Wavee.Player;

public interface IAudioStream
{
    ITrack Track { get; }
}