using System;
using System.ComponentModel;
using System.IO;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Wavee.UI.Features.Library.ViewModels.Artist;
using Wavee.UI.Features.RightSidebar.ViewModels;
using Wavee.UI.Features.Shell.ViewModels;
using Wavee.UI.Test;
using Wavee.UI.WinUI.Contracts;

namespace Wavee.UI.WinUI.Views.Shell.RightSidebar;
public sealed partial class VideoRightSidebarPage : Page, INavigeablePage<RightSidebarVideoViewModel>
{
    public VideoRightSidebarPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is RightSidebarVideoViewModel vm)
        {
            this.Bindings.Initialize();
            DataContext = vm;
            ViewModel.IsActive = true;

            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }
    }

    private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RightSidebarVideoViewModel.CurrentTime))
        {
            if (ViewModel.CurrentTime >= TimeSpan.Zero)
            {
                MediaPlayerElement.MediaPlayer.PlaybackSession.Position = ViewModel.CurrentTime;
            }
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        ViewModel.IsActive = false;

        this.Bindings.StopTracking();

        ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
    }

    public void UpdateBindings()
    {
        this.Bindings.Update();
    }

    public RightSidebarVideoViewModel ViewModel
    {
        get
        {
            try
            {
                return DataContext is RightSidebarVideoViewModel vm ? vm : null;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
        }
    }

    public IMediaPlaybackSource ToStream(Stream stream)
    {
        if (stream == null)
        {
            return null;
        }

        return MediaSource.CreateFromStream(stream.AsRandomAccessStream(), "video/mp4");
    }

    private void MediaPlayerElement_OnLoaded(object sender, RoutedEventArgs e)
    {
        var mp = sender as MediaPlayerElement;
        mp.SetMediaPlayer(new MediaPlayer());
        var mediaPlayer = mp.MediaPlayer;
        mediaPlayer.MediaOpened += MediaPlayerOnMediaOpened;
    }

    private void MediaPlayerOnMediaOpened(MediaPlayer sender, object args)
    {
        //Set position to play from
        Constants.ServiceProvider.GetRequiredService<IUIDispatcher>().Invoke(() =>
        {
            sender.Position = ViewModel.Playback.ActivePlayer.Position + TimeSpan.FromMilliseconds(200);
            sender.Play();
            sender.Volume = 0;
        });
    }
}