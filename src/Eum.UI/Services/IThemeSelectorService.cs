using Eum.UI.ViewModels.Settings;

namespace Eum.UI.Services;

public interface IThemeSelectorService
{
    AppTheme Theme
    {
        get;
    }
    ValueTask SetThemeAsync(AppTheme theme);

    ValueTask SetRequestedThemeAsync();
}
