using OneOf;
using Wavee.Contracts.Common;

namespace Wavee.Contracts.Interfaces.Contracts;

public interface IPlaybackDevice
{
    IObservable<OneOf<StateError, IPlaybackState>> State { get; }

    IObservable<PlaybackConnectionStatusType> ConnectionStatus { get; }
}