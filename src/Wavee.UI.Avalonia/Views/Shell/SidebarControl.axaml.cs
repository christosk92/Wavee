using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Wavee.UI.Core.Sys;

namespace Wavee.UI.Avalonia.Views.Shell;

public partial class SidebarControl : NavigationView
{
    private UserSettings _UserSettings;
    public static readonly DirectProperty<SidebarControl, UserSettings> UserSettingsProperty = AvaloniaProperty.RegisterDirect<SidebarControl, UserSettings>("UserSettings", o => o.UserSettings, (o, v) => o.UserSettings = v);

    public SidebarControl()
    {
        InitializeComponent();
    }

    public UserSettings UserSettings
    {
        get { return _UserSettings; }
        set { SetAndRaise(UserSettingsProperty, ref _UserSettings, value); }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void NavigationView_OnItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e)
    {
        
    }
}