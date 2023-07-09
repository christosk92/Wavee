using LanguageExt;

namespace Wavee.UI.Client.Playback;

public interface IWaveeUIPlaybackClient
{
    IObservable<WaveeUIPlaybackState> PlaybackEvents { get; }
    Option<WaveeUIPlaybackState> CurrentPlayback { get; }
    string OurDeviceId { get; }
}