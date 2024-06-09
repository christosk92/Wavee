using Wavee.Contracts.Common;
using Wavee.Contracts.Enums;

namespace Wavee.Contracts.Interfaces.Contracts;

public interface IPlaybackState
{
    IPlayableItem? CurrentItem { get; }
    RealTimePosition Position { get; }
    RemotePlaybackStateType State { get; }
}