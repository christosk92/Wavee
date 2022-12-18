using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;
using ABI.Microsoft.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Helpers;
using Eum.Spotify.transfer;
using Eum.UI.Services;
using Eum.UI.Users;
using Eum.UI.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;
using Colors = Microsoft.UI.Colors;

namespace Eum.UI.WinUI;

[INotifyPropertyChanged]
public partial class WinUiUserThemeSelectorService : IThemeSelectorService
{
    [NotifyPropertyChangedFor(nameof(GlazeIsCustomColor))]
    [ObservableProperty]
    private string _glaze;
    private bool _initialized = false;
    private readonly EumUser _forUser;
    public WinUiUserThemeSelectorService(EumUser forUser)
    {
        _forUser = forUser;
        if (string.IsNullOrEmpty(forUser.Accent))
        {
            Glaze = Colors.Transparent.ToHex();
        }
        else
        {
            SetGlaze(forUser.Accent);
        }
        SetTheme(_forUser.AppTheme);

    }
    public bool GlazeIsCustomColor => Glaze.StartsWith("#");
    private ElementTheme AsElementTheme => Theme switch
    {
        AppTheme.Dark => ElementTheme.Dark,
        AppTheme.Light => ElementTheme.Light,
        AppTheme.SystemDefault => ElementTheme.Default,
        _ => throw new ArgumentOutOfRangeException()
    };

    public AppTheme Theme { get; set; } = AppTheme.SystemDefault;

     

    public void SetTheme(AppTheme theme)
    {
        Theme = theme;

        SetRequestedTheme();
        _forUser.AppTheme = theme;
    }

    private bool _showedAccentMessageAlready = false;

    public void SetGlaze(string colorCodeHex)
    {
        switch (colorCodeHex)
        {
            case "System Color":
                Glaze = Colors.Transparent.ToHex();
                _forUser.Accent = "System Color";
                break;
            case "Page Dependent":
                Glaze = "Page Dependent";
                _forUser.Accent = "Page Dependent";
                break;
            case "Playback Dependent":
                Glaze = "Playback Dependent";
                _forUser.Accent = "Playback Dependent";
                break;
            default:
                if (colorCodeHex is null)
                {
                    Glaze = Colors.Transparent.ToHex();
                }
                else
                {
                    var f = colorCodeHex.ToColor();
                    colorCodeHex = (Color.FromArgb(25, f.R, f.G, f.B)).ToHex();
                    Glaze = colorCodeHex;
                }

                _forUser.LastAccent = Glaze;
                _forUser.Accent = Glaze;
                break;
        }
        GlazeChanged?.Invoke(this, Glaze);
    }

    public event EventHandler<string> GlazeChanged;

    public void SetRequestedTheme()
    {
        if (App.MWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = AsElementTheme;

            TitleBarHelper.UpdateTitleBar(AsElementTheme);
        }
    }
}