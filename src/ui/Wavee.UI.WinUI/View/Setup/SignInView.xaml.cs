using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.ViewModel.Setup;

namespace Wavee.UI.WinUI.View.Setup
{
    public sealed partial class SignInView : Page
    {
        private IdentityViewModel _viewModel;
        public SignInView(IdentityViewModel id)
        {
            ViewModel = id;
            this.InitializeComponent();
        }

        public SignInView()
        {
            this.InitializeComponent();
        }
        private async void OnIconLoaded(object sender, RoutedEventArgs e)
        {
            var player = (AnimatedVisualPlayer)sender;
            await player.PlayAsync(0, 0.5, false);
        }
        public IdentityViewModel ViewModel
        {
            get => _viewModel;
            set => SetField(ref _viewModel, value);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is IdentityViewModel vm)
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
    }
}
