using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Wavee.UI.Features.Playback.ViewModels;
using Wavee.UI.Features.Shell.ViewModels;


namespace Wavee.UI.WinUI.Views.Playback;

public sealed partial class PlaybackPlayerControl : UserControl
{
    public static readonly DependencyProperty SelectedSidebarComponentProperty = DependencyProperty.Register(nameof(SelectedSidebarComponent), typeof(RightSidebarItemViewModel), typeof(PlaybackPlayerControl), new PropertyMetadata(default(RightSidebarItemViewModel)));

    public PlaybackPlayerControl()
    {
        this.InitializeComponent();
    }

    public PlaybackPlayerViewModel Player => (PlaybackPlayerViewModel)DataContext;

    public RightSidebarItemViewModel SelectedSidebarComponent
    {
        get => (RightSidebarItemViewModel)GetValue(SelectedSidebarComponentProperty);
        set => SetValue(SelectedSidebarComponentProperty, value);
    }

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

    public IEnumerable<MetadataItem> ConvertToMetadataItem((string, string)[] valueTuples)
    {
        if (valueTuples is null) yield break;
        foreach (var (item1, item2) in valueTuples)
        {
            yield return new MetadataItem
            {
                Label = item2,
                Command = Player.NavigationCommand,
                CommandParameter = item1
            };
        }
    }


    public bool IsLyrics(RightSidebarItemViewModel rightSidebarItemViewModel)
    {
        return rightSidebarItemViewModel is RightSidebarLyricsViewModel;
    }

    public void Setlyrics(bool? b)
    {
        if (b == true)
        {
            SelectedSidebarComponent = Player.Lyrics;
        }
        else
        {
            CheckHasNothingChecked();
        }
    }

    private void CheckHasNothingChecked()
    {
        if (LyricsButton.IsChecked is false)
        {
            SelectedSidebarComponent = null;
        }
    }
}