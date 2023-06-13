using System.Reactive.Linq;
using Newtonsoft.Json;
using ReactiveUI;
using Wavee.UI.Core.Contracts;
using Wavee.UI.Helpers;

namespace Wavee.UI.Core.Sys;

public sealed class UserSettings : ConfigBase
{
    private double _windowHeight = 600;
    private double _windowWidth = 800;
    private double _sidebarWidth = 300;
    private AppTheme _appTheme;
    public const double MaximumSidebarWidth = 500;
    public const double MinimumSidebarWidth = 100;

    public UserSettings(string username) : base(BuildAppPathFor(username))
    {
        this.WhenAnyValue(
                x => x.WindowWidth,
                x => x.WindowHeight,
                x => x.SidebarWidth)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Skip(1) // Won't save on creation.
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(_ => ToFile());

        this.LoadOrCreateDefaultFile();
    }
    [JsonProperty(PropertyName = "WindowWidth")]
    public double WindowWidth
    {
        get => _windowWidth;
        set => this.RaiseAndSetIfChanged(ref _windowWidth, value);
    }
    [JsonProperty(PropertyName = "WindowHeight")]
    public double WindowHeight
    {
        get => _windowHeight;
        set => this.RaiseAndSetIfChanged(ref _windowHeight, value);
    }

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

    [JsonProperty(PropertyName = "AppTheme")]
    public AppTheme AppTheme
    {
        get => _appTheme;
        set => this.RaiseAndSetIfChanged(ref _appTheme, value);
    }

    static string BuildAppPathFor(string username)
    {
        var persistentStoragePath = Global.GetPersistentStoragePath!();
        var userSettingsPath = Path.Combine(persistentStoragePath, "UserSettings");
        var finalPath = Path.Combine(userSettingsPath, $"{username}.json");
        IoHelpers.EnsureContainingDirectoryExists(finalPath);
        return finalPath;
    }
}

public enum AppTheme
{
    System,
    Light,
    Dark
}