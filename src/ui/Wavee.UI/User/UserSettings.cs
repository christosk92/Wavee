using Newtonsoft.Json;
using ReactiveUI;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Eum.Spotify.connectstate;
using Wavee.UI.Bases;

namespace Wavee.UI.User;
public sealed class UserSettings : ConfigBase, IDisposable
{
    private bool _sidebarExpanded = true;
    private bool _imageExpanded = false;
    private double _sidebarWidth;
    private double? _windowWidth;
    private double? _windowHeight;
    private AppTheme _appTheme;
    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    private string? _deviceName;
    private DeviceType _deviceType;
    private PreferedQuality _preferedQuality;
    private int _crossfadeSeconds;

    public UserSettings() : base()
    {
    }

    public UserSettings(string filePath) : base(filePath)
    {
        this.WhenAnyValue(
                x => x.SidebarExpanded,
                x => x.SidebarWidth,
                x => x.ImageExpanded,
                x => x.AppTheme,
                x => x.DeviceName,
                x => x.DeviceType,
                x => x.PreferedQuality,
                x => x.CrossfadeSeconds,
                (_, _, _, _, _, _, _,_) => Unit.Default)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ToFile())
            .DisposeWith(_disposables);

        this.WhenAnyValue(
                x => x.WindowWidth,
                x => x.WindowHeight)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(_ => ToFile())
            .DisposeWith(_disposables);
    }

    [JsonProperty(PropertyName = "AppTheme")]
    public AppTheme AppTheme
    {
        get => _appTheme;
        set => this.RaiseAndSetIfChanged(ref _appTheme, value);
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


    [DefaultValue(250)]
    [JsonProperty(PropertyName = "SidebarWidth", DefaultValueHandling = DefaultValueHandling.Populate)]
    public double SidebarWidth
    {
        get => _sidebarWidth;
        set => this.RaiseAndSetIfChanged(ref _sidebarWidth, value);
    }


    [DefaultValue(true)]
    [JsonProperty(PropertyName = "SidebarExpanded", DefaultValueHandling = DefaultValueHandling.Populate)]
    public bool SidebarExpanded
    {
        get => _sidebarExpanded;
        set => this.RaiseAndSetIfChanged(ref _sidebarExpanded, value);
    }

    [DefaultValue(true)]
    [JsonProperty(PropertyName = "ImageExpanded", DefaultValueHandling = DefaultValueHandling.Populate)]
    public bool ImageExpanded
    {
        get => _imageExpanded;
        set => this.RaiseAndSetIfChanged(ref _imageExpanded, value);
    }

    [DefaultValue("Wavee")]
    [JsonProperty(PropertyName = "DeviceName", DefaultValueHandling = DefaultValueHandling.Populate)]
    public string DeviceName
    {
        get => _deviceName;
        set => this.RaiseAndSetIfChanged(ref _deviceName, value);
    }

    [DefaultValue(DeviceType.Computer)]
    [JsonProperty(PropertyName = "DeviceType", DefaultValueHandling = DefaultValueHandling.Populate)]
    public DeviceType DeviceType
    {
        get => _deviceType;
        set => this.RaiseAndSetIfChanged(ref _deviceType, value);
    }


    [DefaultValue(PreferedQuality.Normal)]
    [JsonProperty(PropertyName = "PreferedQuality", DefaultValueHandling = DefaultValueHandling.Populate)]
    public PreferedQuality PreferedQuality
    {
        get => _preferedQuality;
        set => this.RaiseAndSetIfChanged(ref _preferedQuality, value);
    }

    [DefaultValue(0)]
    [JsonProperty(PropertyName = "CrossfadeSeconds", DefaultValueHandling = DefaultValueHandling.Populate)]
    public int CrossfadeSeconds
    {
        get => _crossfadeSeconds;
        set => this.RaiseAndSetIfChanged(ref _crossfadeSeconds, value);
    }
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
