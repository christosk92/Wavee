using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Wavee.UI.Core;
using Wavee.UI.ViewModel.Login;

namespace Wavee.UI.Avalonia.Views;

public partial class LoginView : UserControl
{
    public LoginView(Action<IAppState> done)
    {
        InitializeComponent();
        this.DataContext = new LoginViewModel(state =>
        {
            done(state);
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        this.FindControl<Control>("UsernameInput")!.Focus(NavigationMethod.Tab);
        // UsernameInput.Focus(NavigationMethod.Tab);
    }
}