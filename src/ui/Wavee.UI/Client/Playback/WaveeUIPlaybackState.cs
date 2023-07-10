using Eum.Spotify.connectstate;
using LanguageExt;
using Wavee.Id;

namespace Wavee.UI.Client.Playback;

public readonly record struct WaveeUIPlaybackState(WaveeUIPlayerState PlaybackState, Option<WaveeItemMetadata> Metadata,
    TimeSpan Position, Option<RemoteDeviceInfo> Remote, IEnumerable<RemoteDeviceInfo> Devices)
{
    public static WaveeUIPlaybackState Empty { get; } = new(WaveeUIPlayerState.NotPlayingAnything, Option<WaveeItemMetadata>.None, TimeSpan.Zero, Option<RemoteDeviceInfo>.None, Enumerable.Empty<RemoteDeviceInfo>());
}

public readonly record struct RemoteDeviceInfo(ServiceType Service, string DeviceId, string DeviceName, DeviceType Type, bool CanControlVolume, Option<double> VolumeFraction);

public readonly record struct WaveeItemMetadata(string Id, Option<string> Uid, ItemWithId Title, ItemWithId[] Subtitles,
    string LargeImageUrl, string MediumImageUrl, string SmallImageUrl, TimeSpan Duration, bool HasLyrics);

public readonly record struct ItemWithId(string Id, AudioItemType Type, string Title);

public enum WaveeUIPlayerState
{
    NotPlayingAnything,
    Playing,
    Paused
}