using Wavee.Core.Contracts;

namespace Wavee.Player.States;

public interface IWaveeInPlaybackState : IWaveePlaybackState
{
    internal IAudioStream Stream { get; init; }
}