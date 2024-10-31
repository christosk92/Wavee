using System.Reactive.Linq;
using ReactiveUI;
using Wavee.ViewModels.Enums;
using Wavee.ViewModels.Infrastructure;
using Wavee.ViewModels.State;

namespace Wavee.ViewModels.Models.UI;

[AppLifetime]
public partial class ApplicationSettings : ReactiveObject, IApplicationSettings
{
    private const int ThrottleTime = 500;

    private readonly string _persistentConfigFilePath;
    private readonly PersistentConfig _startupConfig;
    private readonly UiConfig _uiConfig;

    [AutoNotify] private WindowState _windowState;
    [AutoNotify] private bool _oobe;

    // General
    [AutoNotify] private bool _darkModeEnabled;
    [AutoNotify] private bool _runOnSystemStartup;
    [AutoNotify] private bool _hideOnClose;

    public ApplicationSettings(
        string persistentConfigFilePath, 
        PersistentConfig persistentConfig,
        UiConfig uiConfig)
    {
        _persistentConfigFilePath = persistentConfigFilePath;

        _uiConfig = uiConfig;

        // General
        _darkModeEnabled = _uiConfig.DarkModeEnabled;
        
        _runOnSystemStartup = _uiConfig.RunOnSystemStartup;
        _hideOnClose = _uiConfig.HideOnClose;

        _oobe = _uiConfig.Oobe;

        _windowState = (WindowState)Enum.Parse(typeof(WindowState), _uiConfig.WindowState);

        // Save UiConfig on change
        this.WhenAnyValue(
                x => x.DarkModeEnabled,
                x => x.RunOnSystemStartup,
                x => x.HideOnClose,
                x => x.Oobe,
                x => x.WindowState)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(ThrottleTime))
            .Do(_ => ApplyUiConfigChanges())
            .Subscribe();
    }

    private void ApplyUiConfigChanges()
    {
        _uiConfig.DarkModeEnabled = DarkModeEnabled;
        _uiConfig.RunOnSystemStartup = RunOnSystemStartup;
        _uiConfig.HideOnClose = HideOnClose;
        _uiConfig.Oobe = Oobe;
        _uiConfig.WindowState = WindowState.ToString();
    }

}