using System;
using System.Threading.Tasks;
using Eum.UI.Services;
using Eum.UI.Users;
using Eum.UI.ViewModels.Settings;
using Microsoft.UI.Xaml;

namespace Eum.UI.WinUI;

public class WinUiUserThemeSelectorService : IThemeSelectorService
{
    private bool _initialized = false;
    private readonly EumUser _forUser;
    public WinUiUserThemeSelectorService(EumUser forUser)
    {
        _forUser = forUser;
    }

    private ElementTheme AsElementTheme => Theme switch
    {
        AppTheme.Dark => ElementTheme.Dark,
        AppTheme.Light => ElementTheme.Light,
        AppTheme.SystemDefault => ElementTheme.Default,
        _ => throw new ArgumentOutOfRangeException()
    };

    public AppTheme Theme { get; set; } = AppTheme.SystemDefault;

    public async ValueTask SetThemeAsync(AppTheme theme)
    {
        Theme = theme;

        await SetRequestedThemeAsync();
        _forUser.AppTheme = theme;
    }

    public ValueTask SetRequestedThemeAsync()
    {
        if (App.MWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = AsElementTheme;

            TitleBarHelper.UpdateTitleBar(AsElementTheme);
        }
        return ValueTask.CompletedTask;
    }
}