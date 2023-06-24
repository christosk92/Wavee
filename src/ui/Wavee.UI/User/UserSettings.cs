using Newtonsoft.Json;
using ReactiveUI;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
                (_, _, _, _) => Unit.Default)
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

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
