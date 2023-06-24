using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.ViewModel.Setup;

namespace Wavee.UI.WinUI.View.Setup;

public sealed partial class SettingEverythingUpView : Page, INotifyPropertyChanged
{
    private SettingEverythingUpViewModel _viewModel;

    public SettingEverythingUpView()
    {
        this.InitializeComponent();
    }
    public SettingEverythingUpViewModel ViewModel
    {
        get => _viewModel;
        set => SetField(ref _viewModel, value);
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is SettingEverythingUpViewModel vm)
        {
            ViewModel = vm;
            await vm.Submit(1);
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

    public double ToDegrees(double d)
    {
        var frac = d / 100;
        return frac * 360;
    }

    public string FormatString(double d)
    {
        var rounded = (int)d;
        return $"{rounded}%";
    }
}