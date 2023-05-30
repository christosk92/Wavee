using System.ComponentModel;
using System.Reactive.Linq;
using Newtonsoft.Json;
using ReactiveUI;
using Wavee.UI.Bases;
using System.Reactive;
using System.Reactive.Linq;
using Eum.Spotify.connectstate;
using ReactiveUI;
using Wavee.Helpers;
using Unit = System.Reactive.Unit;

namespace Wavee.UI.Config;

[JsonObject(MemberSerialization.OptIn)]
public class AppConfig : ConfigBase
{
    private AppTheme _theme;
    private double? _sidebarWidth;
    private double? _windowWidth;
    private double? _windowHeight;
    private string? _spotifyDeviceName;
    private DeviceType _spotifyDeviceType;
    private string _metadataCachePath;
    private string _audioFilesCachePath;
    public AppConfig() : base()
    {
    }

    public AppConfig(string filePath) : base(filePath)
    {
        this.WhenAnyValue(
                x => x.Theme,
                x => x.SpotifyDeviceName,
                x => x.SpotifyDeviceType,
                x => x.MetadataCachePath,
                x => x.AudioFilesCachePath,
                (_, device, deviceType, metadacachePath, audioFileCachePath) =>
                {
                    //TODO: Update Spotify config here in AppState
                    return Unit.Default;
                })
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ToFile());

        this.WhenAnyValue(
                x => x.WindowWidth,
                x => x.WindowHeight,
                x => x.SidebarWidth)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(_ => ToFile());
    }

    [DefaultValue("::env")]
    [JsonProperty(PropertyName = "MetadataCachePath")]
    public string MetadataCachePath
    {
        get => _metadataCachePath;
        set => this.RaiseAndSetIfChanged(ref _metadataCachePath, value);
    }

    [DefaultValue("::env")]
    [JsonProperty(PropertyName = "AudioFilesCachePath")]
    public string AudioFilesCachePath
    {
        get => _audioFilesCachePath;
        set => this.RaiseAndSetIfChanged(ref _audioFilesCachePath, value);
    }

    [DefaultValue("Wavee in WinUI")]
    [JsonProperty(PropertyName = "SpotifyDeviceName")]
    public string? SpotifyDeviceName
    {
        get => _spotifyDeviceName;
        set => this.RaiseAndSetIfChanged(ref _spotifyDeviceName, value);
    }

    [DefaultValue(DeviceType.Computer)]
    [JsonProperty(PropertyName = "SpotifyDeviceType")]
    public DeviceType SpotifyDeviceType
    {
        get => _spotifyDeviceType;
        set => this.RaiseAndSetIfChanged(ref _spotifyDeviceType, value);
    }

    [DefaultValue(AppTheme.System)]
    [JsonProperty(PropertyName = "AppTheme")]
    public AppTheme Theme
    {
        get => _theme;
        set => this.RaiseAndSetIfChanged(ref _theme, value);
    }

    [JsonProperty(PropertyName = "WindowWidth")]
    public double? WindowWidth
    {
        get => _windowWidth;
        set => this.RaiseAndSetIfChanged(ref _windowWidth, value);
    }

    [JsonProperty(PropertyName = "WindowHeight")]
    public double? WindowHeight
    {
        get => _windowHeight;
        set => this.RaiseAndSetIfChanged(ref _windowHeight, value);
    }

    const double DefaultSidebarWidth = 200;
    [DefaultValue(DefaultSidebarWidth)]
    [JsonProperty(PropertyName = "SidebarWidth")]
    public double SidebarWidth
    {
        get => _sidebarWidth ?? DefaultSidebarWidth;
        set => this.RaiseAndSetIfChanged(ref _sidebarWidth, value);
    }
}