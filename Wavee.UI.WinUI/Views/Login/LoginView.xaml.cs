using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Models.Profiles;
using Wavee.UI.ViewModels.Login;

namespace Wavee.UI.WinUI.Views.Login
{
    public sealed partial class LoginView : UserControl
    {
        public LoginView()
        {
            ViewModel = Ioc.Default.GetRequiredService<LoginViewModel>();
            this.InitializeComponent();
            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ViewModel.IsSignedIn)
            {
                SignedIn?.Invoke(this, ViewModel.SignedInProfile!.Value);
                ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            }
        }

        public LoginViewModel ViewModel { get; }

        public event EventHandler<Profile>? SignedIn;

        public bool NullToBool(string s, bool b)
        {
            return string.IsNullOrEmpty(s) ? b : !b;
        }

        public Visibility IsNullToVisibility(object? obj, bool visibleIfNull)
        {
            if (obj is null)
            {
                return visibleIfNull ? Visibility.Visible : Visibility.Collapsed;
            }
            return visibleIfNull ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LetsGetStartedBlock_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ProgressBorder.Width = (sender as FrameworkElement)!.ActualWidth * 1.5;
        }

        public void LoginView_OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            SignedIn = null;
            this.Bindings.StopTracking();
        }
    }
}