using Newtonsoft.Json;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Wavee.UI.Bases;
using Wavee.UI.ViewModels;

namespace Wavee.UI;

[JsonObject(MemberSerialization.OptIn)]
public class UiConfig : ConfigBase
{
    private string? _lastSelectedUserId;
    private bool _runOnSystemStartup;
    private double? _windowWidth;
    private double? _windowHeight;
    private Theme _theme;
    private double? _sidebarWidth;
    private PlaylistSortProperty _playlistSortProperty = PlaylistSortProperty.Custom;

    public UiConfig() : base()
    {
    }

    public UiConfig(string filePath) : base(filePath)
    {
        this.WhenAnyValue(
                x => x.Theme,
                x => x.RunOnSystemStartup,
                x => x.SidebarWidth,
                x => x.PlaylistSortProperty,
                (_, _, _, _) => Unit.Default)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ToFile());


        this.WhenAnyValue(
                x => x.WindowWidth,
                x => x.WindowHeight)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(_ => ToFile());
    }


    [DefaultValue(Theme.System)]
    [JsonProperty(PropertyName = "Theme", DefaultValueHandling = DefaultValueHandling.Populate)]
    public Theme Theme
    {
        get => _theme;
        set => RaiseAndSetIfChanged(ref _theme, value);
    }

    [DefaultValue(null)]
    [JsonProperty(PropertyName = "LastSelectedUser", DefaultValueHandling = DefaultValueHandling.Populate)]
    public string? LastSelectedUser
    {
        get => _lastSelectedUserId;
        set => RaiseAndSetIfChanged(ref _lastSelectedUserId, value);
    }

    [DefaultValue(false)]
    [JsonProperty(PropertyName = "RunOnSystemStartup", DefaultValueHandling = DefaultValueHandling.Populate)]
    public bool RunOnSystemStartup
    {
        get => _runOnSystemStartup;
        set => RaiseAndSetIfChanged(ref _runOnSystemStartup, value);
    }


    [JsonProperty(PropertyName = "WindowWidth")]
    public double? WindowWidth
    {
        get => _windowWidth;
        set => RaiseAndSetIfChanged(ref _windowWidth, value);
    }

    [JsonProperty(PropertyName = "WindowHeight")]
    public double? WindowHeight
    {
        get => _windowHeight;
        set => RaiseAndSetIfChanged(ref _windowHeight, value);
    }

    [JsonProperty(PropertyName = "SidebarWidth")]
    public double? SidebarWidth
    {
        get => _sidebarWidth;
        set => RaiseAndSetIfChanged(ref _sidebarWidth, value);
    }

    [DefaultValue(PlaylistSortProperty.Custom)]
    [JsonProperty(PropertyName = "PlaylistSortProperty", DefaultValueHandling = DefaultValueHandling.Populate)]
    public PlaylistSortProperty PlaylistSortProperty
    {
        get => _playlistSortProperty;
        set => RaiseAndSetIfChanged(ref _playlistSortProperty, value);
    }
}

public enum Theme
{
    System,
    Light,
    Dark
}