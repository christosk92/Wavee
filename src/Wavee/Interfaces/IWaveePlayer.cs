using Wavee.Core.Models;

namespace Wavee.Interfaces;

public interface IWaveePlayer
{
    event EventHandler<WaveePlaybackState> PlaybackChanged;
    double Volume { get; set; }
    TimeSpan Position { get; set; }
}