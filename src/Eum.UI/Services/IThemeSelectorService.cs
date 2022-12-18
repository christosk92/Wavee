using Eum.UI.ViewModels.Settings;

namespace Eum.UI.Services;

public interface IThemeSelectorService
{
    AppTheme Theme
    {
        get;
    }
    string Glaze { get; set; }
    bool GlazeIsCustomColor { get; }
    void SetTheme(AppTheme theme);
    void SetGlaze(string colorCodeHex);

    event EventHandler<string> GlazeChanged;
}
