namespace Wavee.Player.States;

public interface IWaveePlayerInPlaybackState : IWaveePlayerState
{
    string PlaybackId { get; }
    TimeSpan Position { get; }
    Stream Decoder { get; }
}