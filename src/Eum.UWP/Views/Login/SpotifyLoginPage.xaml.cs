using Eum.UI.Spotify.ViewModels.Users;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Eum.UWP.Views.Login
{
    public sealed partial class SpotifyLoginPage : UserControl
    {
        public static readonly DependencyProperty GoBackCommandProperty = DependencyProperty.Register(nameof(GoBackCommand), typeof(ICommand), typeof(SpotifyLoginPage), new PropertyMetadata(default(ICommand)));

        public SpotifyLoginPage()
        {
            this.InitializeComponent();
        }

        public ICommand GoBackCommand
        {
            get => (ICommand)GetValue(GoBackCommandProperty);
            set => SetValue(GoBackCommandProperty, value);
        }

        public SignInToSpotifyViewModel ViewModel => (SignInToSpotifyViewModel)DataContext;
    }
}
