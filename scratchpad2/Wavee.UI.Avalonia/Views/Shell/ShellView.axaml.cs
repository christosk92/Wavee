using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Wavee.UI.Core;
using Wavee.UI.ViewModel;

namespace Wavee.UI.Avalonia.Views.Shell;

public partial class ShellView : UserControl
{
    public ShellView(IAppState appState)
    {
        InitializeComponent();
        Global.AppState = appState;
        this.DataContext = new ShellViewModel(appState);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void HomeLoaded(object? sender, RoutedEventArgs e)
    {
        
    }

    private void SidebarControl_OnBackRequested(object? sender, NavigationViewBackRequestedEventArgs e)
    {
        
    }
}