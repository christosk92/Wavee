using System.ComponentModel;
using System.Reactive.Linq;
using Eum.Spotify.connectstate;
using LanguageExt;
using Newtonsoft.Json;
using ReactiveUI;
using Wavee.Spotify;
using Wavee.UI.Enums;

namespace Wavee.UI.Settings;

public sealed class UserSettings : ConfigBase
{
    public const double DefaultSidebarWidth = 200.0;
    public const double MaximumSidebarWidth = 400.0;
    public const double MinimumSidebarWidth = 100.0;

    private readonly string _path;
    private double _sidebarWidth = DefaultSidebarWidth;
    private AppTheme _appTheme;
    private TimeSpan _crossfadeDuration = TimeSpan.Zero;
    private PreferredQualityType _preferedQuality = PreferredQualityType.Normal;
    private string _deviceName = "Wavee";
    private DeviceType _deviceType = DeviceType.Computer;

    public UserSettings(string path) : base(path)
    {
        _path = path;

        this.WhenAnyValue(
                x => x.SidebarWidth,
                x => x.AppTheme)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(_ => ToFile());

        this.WhenAnyValue(
            x => x.CrossfadeDuration,
            x => x.PreferedQuality,
            x => x.DeviceName,
            x => x.DeviceType,
            (_, _, _, _) => Unit.Default)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ToFile());
    }

    [DefaultValue(DefaultSidebarWidth)]
    [JsonProperty(PropertyName = "SidebarWidth")]
    public double SidebarWidth
    {
        get => _sidebarWidth;
        set
        {
            value = Math.Clamp(value, MinimumSidebarWidth, MaximumSidebarWidth);
            this.RaiseAndSetIfChanged(ref _sidebarWidth, value);
        }
    }

    [DefaultValue(Enums.AppTheme.System)]
    [JsonProperty(PropertyName = "AppTheme")]
    public AppTheme AppTheme
    {
        get => _appTheme;
        set => this.RaiseAndSetIfChanged(ref _appTheme, value);
    }

    [JsonProperty(PropertyName = "CrossfadeDuration")]
    public TimeSpan CrossfadeDuration
    {
        get => _crossfadeDuration;
        set => this.RaiseAndSetIfChanged(ref _crossfadeDuration, value);
    }

    [DefaultValue(PreferredQualityType.Normal)]
    [JsonProperty(PropertyName = "PreferedQuality")]
    public PreferredQualityType PreferedQuality
    {
        get => _preferedQuality;
        set => this.RaiseAndSetIfChanged(ref _preferedQuality, value);
    }

    [DefaultValue("Wavee")]
    [JsonProperty(PropertyName = "DeviceName")]
    public string DeviceName
    {
        get => _deviceName;
        set => this.RaiseAndSetIfChanged(ref _deviceName, value);
    }

    [DefaultValue(Eum.Spotify.connectstate.DeviceType.Computer)]
    [JsonProperty(PropertyName = "DeviceType")]
    public DeviceType DeviceType
    {
        get => _deviceType;
        set => this.RaiseAndSetIfChanged(ref _deviceType, value);
    }
}