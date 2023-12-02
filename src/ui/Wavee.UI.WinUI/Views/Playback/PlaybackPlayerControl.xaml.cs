using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Features.Playback.ViewModels;


namespace Wavee.UI.WinUI.Views.Playback;

public sealed partial class PlaybackPlayerControl : UserControl
{
    public PlaybackPlayerControl()
    {
        this.InitializeComponent();
    }

    public PlaybackPlayerViewModel Player => (PlaybackPlayerViewModel)DataContext;

    private void PlaybackPlayerControl_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is PlaybackPlayerViewModel player)
        {
            player.AddPositionCallback(500, Callback);
        }
    }

    private void Callback(TimeSpan obj)
    {
        PositionText.Text = obj.ToString(@"mm\:ss");
        PositionSlider.Value = obj.TotalSeconds;
    }

    public string Format(TimeSpan timeSpan)
    {
        return timeSpan.ToString(@"mm\:ss");
    }
}