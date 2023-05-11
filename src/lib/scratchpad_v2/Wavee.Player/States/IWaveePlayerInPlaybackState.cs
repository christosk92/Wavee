using NAudio.Wave;

namespace Wavee.Player.States;

public interface IWaveePlayerInPlaybackState : IWaveePlayerState
{
    string PlaybackId { get; }
    TimeSpan Position { get; }
    WaveStream Decoder { get; }
}