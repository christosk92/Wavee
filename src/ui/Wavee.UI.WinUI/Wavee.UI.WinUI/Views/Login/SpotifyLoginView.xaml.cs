using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Login;

namespace Wavee.UI.WinUI.Views.Login;

public sealed partial class SpotifyLoginView : UserControl
{
    public SpotifyLoginView(SpotifyLoginViewModel login)
    {
        ViewModel = login;
        this.InitializeComponent();
    }
    public SpotifyLoginViewModel ViewModel { get; }

    public Visibility HideOrShowWebView(string? navigateTo, bool isBusy)
    {
        if (isBusy) return Visibility.Collapsed;

        return string.IsNullOrEmpty(navigateTo) ? Visibility.Collapsed : Visibility.Visible;
    }
}