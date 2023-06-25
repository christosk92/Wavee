using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.ViewModel.Setup;
using System;
using CommunityToolkit.WinUI.UI;

namespace Wavee.UI.WinUI.View.Setup
{
    public sealed partial class OptInView : Page, INotifyPropertyChanged
    {
        private OptInViewModel _viewModel;

        public OptInView()
        {
            this.InitializeComponent();
        }

        public OptInViewModel ViewModel
        {
            get => _viewModel;
            set => SetField(ref _viewModel, value);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is OptInViewModel vm)
            {
                ViewModel = vm;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }


        private async void OnIconLoaded(object sender, RoutedEventArgs e)
        {
            var player = (AnimatedVisualPlayer)sender;
            await player.PlayAsync(0, 0.5, false);
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var idx = ((ComboBox)sender).SelectedIndex;
            var theme = (AppTheme)idx;
            ViewModel.Settings.AppTheme = theme;
        }

        public int CastTo(AppTheme appTheme)
        {
            ((FrameworkElement)App.MainWindow.Content).RequestedTheme = appTheme switch
            {
                AppTheme.Dark => ElementTheme.Dark,
                AppTheme.Light => ElementTheme.Light,
                AppTheme.System => ElementTheme.Default,
            };

            //find dialog
            this.FindParent<ContentDialog>().RequestedTheme = appTheme switch
            {
                AppTheme.Dark => ElementTheme.Dark,
                AppTheme.Light => ElementTheme.Light,
                AppTheme.System => ElementTheme.Default,
            };

            return (int)appTheme;
        }
    }
}
