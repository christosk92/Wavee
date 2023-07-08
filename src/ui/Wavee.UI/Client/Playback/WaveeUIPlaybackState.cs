using Eum.Spotify.connectstate;
using LanguageExt;
using Wavee.Id;

namespace Wavee.UI.Client.Playback;

public readonly record struct WaveeUIPlaybackState(WaveeUIPlayerState PlaybackState, Option<WaveeItemMetadata> Metadata,
    TimeSpan Position, Option<RemoteState> Remote)
{
    public static WaveeUIPlaybackState Empty { get; } = new(WaveeUIPlayerState.NotPlayingAnything, Option<WaveeItemMetadata>.None, TimeSpan.Zero, Option<RemoteState>.None);
}

public readonly record struct RemoteState(ServiceType Service, string DeviceId, string DeviceName, DeviceType Type, bool CanControlVolume, Option<double> VolumeFraction);

public readonly record struct WaveeItemMetadata(string Id, Option<string> Uid, ItemWithId Title, ItemWithId[] Subtitles,
    string LargeImageUrl, string SmallImageUrl, TimeSpan Duration, bool HasLyrics);
public readonly record struct ItemWithId(string Id, AudioItemType Type, string Title);

public enum WaveeUIPlayerState
{
    NotPlayingAnything,
    Playing,
    Paused
}