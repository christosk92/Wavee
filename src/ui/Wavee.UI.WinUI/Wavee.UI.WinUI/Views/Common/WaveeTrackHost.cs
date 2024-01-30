using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;
using Windows.UI;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;


namespace Wavee.UI.WinUI.Views.Common;

[ContentProperty(Name = "MainContent")]
public sealed class WaveeTrackHost : Control
{
    public static readonly DependencyProperty PlaybackStateProperty =
        DependencyProperty.Register(nameof(PlaybackState), typeof(WaveeUITrackPlaybackStateType),
            typeof(WaveeTrackHost), new PropertyMetadata(default(WaveeUITrackPlaybackStateType), PropertyChangedCallback));

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = (WaveeTrackHost)d;
        x.OnChanged((WaveeUITrackPlaybackStateType)e.NewValue);
    }

    public WaveeTrackHost()
    {
        this.DefaultStyleKey = typeof(WaveeTrackHost);
    }

    public WaveeUITrackPlaybackStateType PlaybackState
    {
        get => (WaveeUITrackPlaybackStateType)GetValue(PlaybackStateProperty);
        set => SetValue(PlaybackStateProperty, value);
    }



    public static DependencyProperty MainContentProperty =
        DependencyProperty.Register("MainContent", typeof(object), typeof(WaveeTrackHost), null);

    public object MainContent
    {
        get => GetValue(MainContentProperty);
        set => SetValue(MainContentProperty, value);
    }


    private void OnChanged(WaveeUITrackPlaybackStateType x)
    {
        switch (x)
        {
            case WaveeUITrackPlaybackStateType.NotPlaying:
                this.Foreground = (SolidColorBrush)Application.Current.Resources["ApplicationForegroundThemeBrush"];
                break;
            case WaveeUITrackPlaybackStateType.Playing:
                this.Foreground = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
                break;
            case WaveeUITrackPlaybackStateType.Paused:
                this.Foreground = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
                break;
            case WaveeUITrackPlaybackStateType.Loading:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(x), x, null);
        }
    }
}