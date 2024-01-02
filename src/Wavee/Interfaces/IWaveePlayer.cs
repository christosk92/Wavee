using Wavee.Core.Models;
using Wavee.Core.Playback;

namespace Wavee.Interfaces;

public interface IWaveePlayer
{
    event EventHandler<WaveePlaybackState> PlaybackChanged;
    double Volume { get; set; }
    TimeSpan Position { get; set; }
    ValueTask Play(WaveePlaybackList context);
    ValueTask Seek(TimeSpan timeSpan);
    ValueTask Stop();
}