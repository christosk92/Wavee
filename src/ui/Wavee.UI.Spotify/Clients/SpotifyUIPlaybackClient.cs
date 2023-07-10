using System.Reactive.Linq;
using System.Text;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using Spotify.Metadata;
using Wavee.Id;
using Wavee.Metadata.Common;
using Wavee.Remote;
using Wavee.UI.Client.Playback;

namespace Wavee.UI.Spotify.Clients;

internal sealed class SpotifyUIPlaybackClient : IWaveeUIPlaybackClient
{
    private readonly WeakReference<SpotifyClient> _spotifyClient;
    public SpotifyUIPlaybackClient(SpotifyClient spotifyClient)
    {
        _spotifyClient = new WeakReference<SpotifyClient>(spotifyClient);
        OurDeviceId = spotifyClient.DeviceId;
    }

    public IObservable<WaveeUIPlaybackState> PlaybackEvents => CreateListener();
    public Option<WaveeUIPlaybackState> CurrentPlayback { get; private set; }
    public string OurDeviceId { get; }

    private IObservable<WaveeUIPlaybackState> CreateListener()
    {
        if (!_spotifyClient.TryGetTarget(out var spotifyClient))
        {
            throw new InvalidOperationException("SpotifyClient is not available");
        }

        return spotifyClient
            .Remote
            .CreateListener()
            .SelectMany((x) => Task.Run(async () => await SpotifyRemotePlaybackEvent(x, new WeakReference<SpotifyClient>(spotifyClient))))
            .Select(f =>
            {
                CurrentPlayback = f;
                return f;
            })
            .ObserveOn(RxApp.TaskpoolScheduler);
    }

    private static async Task<WaveeUIPlaybackState> SpotifyRemotePlaybackEvent(SpotifyRemoteState spotifyRemoteState,
        WeakReference<SpotifyClient> weakReference)
    {
        if (spotifyRemoteState.TrackId.IsNone)
        {
            return WaveeUIPlaybackState.Empty;
        }

        if (!weakReference.TryGetTarget(out var spotifyClient))
        {
            throw new InvalidOperationException("SpotifyClient is not available");
        }

        var trackId = spotifyRemoteState.TrackId.IfNone(() => throw new InvalidOperationException("TrackId is not available"));
        var track = await spotifyClient
            .Metadata.GetTrack(trackId, CancellationToken.None)
            .ConfigureAwait(false);

        var images = GetCoverImages(track);

        return new WaveeUIPlaybackState(
            PlaybackState: spotifyRemoteState.Paused ? WaveeUIPlayerState.Paused :
            (spotifyRemoteState.HasPlayback ? WaveeUIPlayerState.Playing : WaveeUIPlayerState.NotPlayingAnything),
            Metadata: new WaveeItemMetadata(Id: trackId.ToString(),
                Uid: spotifyRemoteState.TrackUid,
                Title: new ItemWithId(
                    Id: SpotifyId.FromRaw(track.Album.Gid.Span, AudioItemType.Album, ServiceType.Spotify).ToString(),
                    Type: AudioItemType.Album,
                    Title: track.Name),
                Subtitles: track.Artist.Select(x => new ItemWithId(
                    Id: SpotifyId.FromRaw(x.Gid.Span, AudioItemType.Artist, ServiceType.Spotify).ToString(),
                    Type: AudioItemType.Artist,
                    Title: x.Name)).ToArray(),
                LargeImageUrl: images.OrderByDescending(x => x.Height.IfNone(0)).Head().Url,
                MediumImageUrl: images.OrderByDescending(x => x.Height.IfNone(0)).Skip(1).Head().Url,
                SmallImageUrl: images.OrderBy(x => x.Height.IfNone(0)).Head().Url, TimeSpan.FromMilliseconds(track.Duration),
                HasLyrics: track.HasHasLyrics && track.HasLyrics),
            Position: spotifyRemoteState.Position,
            Remote: CalculateRemoteDevice(spotifyClient.DeviceId, spotifyRemoteState),
            Devices: spotifyRemoteState.Devices.Select(CalculateRemoteDevice)
        );

    }

    private static RemoteDeviceInfo CalculateRemoteDevice(SpotifyRemoteDeviceInfo device)
    {
        return new RemoteDeviceInfo(
            Service: ServiceType.Spotify,
            DeviceId: device.Id,
            DeviceName: device.Name,
            Type: device.Type,
            CanControlVolume: device.CanControlVolume,
            VolumeFraction: device.Volume
        );
    }

    private static Option<RemoteDeviceInfo> CalculateRemoteDevice(string ourDeviceId, SpotifyRemoteState spotifyRemoteState)
    {
        if (spotifyRemoteState.Device.IsNone)
        {
            return Option<RemoteDeviceInfo>.None;
        }

        var device = spotifyRemoteState.Device.ValueUnsafe();
        if (device.Id == ourDeviceId)
        {
            return Option<RemoteDeviceInfo>.None;
        }

        return new RemoteDeviceInfo(
            Service: ServiceType.Spotify,
            DeviceId: device.Id,
            DeviceName: device.Name,
            Type: device.Type,
            CanControlVolume: device.CanControlVolume,
            VolumeFraction: device.Volume
        );
    }

    private static CoverImage[] GetCoverImages(Track track)
    {
        const string cdnUrlImage = "https://i.scdn.co/image/{0}";
        return track.Album.CoverGroup.Image
            .Select(x => new CoverImage(
                Url: CalculateUrl(x, cdnUrlImage),
                Width: x.HasWidth ? (ushort)x.Width : Option<ushort>.None,
                Height: x.HasHeight ? (ushort)x.Height : Option<ushort>.None
                ))
            .ToArray();
    }

    private static string CalculateUrl(Image image, string httpsIScdnCoImage)
    {
        //convert to hex id
        var sb = new StringBuilder();
        ReadOnlySpan<byte> span = image.FileId.Span;
        foreach (var b in span)
        {
            sb.Append(b.ToString("x2"));
        }

        return string.Format(httpsIScdnCoImage, sb.ToString());
    }
}