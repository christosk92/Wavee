using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Wavee.UI.WinUI.View.Setup;

public sealed partial class YoureGoodToGoView : Page
{
    public YoureGoodToGoView()
    {
        this.InitializeComponent();
    }
    private async void OnIconLoaded(object sender, RoutedEventArgs e)
    {
        var player = (AnimatedVisualPlayer)sender;
        await player.PlayAsync(0, 0.5, false);
    }
}