using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;
using Microsoft.UI.Xaml.Markup;


namespace Wavee.UI.WinUI.Views.Common;

[ContentProperty(Name = "MainContent")]
public sealed class WaveeTrackHost : Control
{
    public static readonly DependencyProperty PlaybackStateProperty = DependencyProperty.Register(nameof(PlaybackState), typeof(WaveeUITrackPlaybackStateType), typeof(WaveeTrackHost), new PropertyMetadata(default(WaveeUITrackPlaybackStateType)));
    public static readonly DependencyProperty TrackIdProperty = DependencyProperty.Register(nameof(TrackId), typeof(ComposedKey), typeof(WaveeTrackHost), new PropertyMetadata(default(ComposedKey)));
    public WaveeTrackHost()
    {
        this.DefaultStyleKey = typeof(WaveeTrackHost);
    }

    public WaveeUITrackPlaybackStateType PlaybackState
    {
        get => (WaveeUITrackPlaybackStateType)GetValue(PlaybackStateProperty);
        set => SetValue(PlaybackStateProperty, value);
    }

    public ComposedKey TrackId
    {
        get => (ComposedKey)GetValue(TrackIdProperty);
        set => SetValue(TrackIdProperty, value);
    }


    public static DependencyProperty MainContentProperty =
        DependencyProperty.Register("MainContent", typeof(object), typeof(WaveeTrackHost), null);

    public object MainContent
    {
        get => GetValue(MainContentProperty);
        set => SetValue(MainContentProperty, value);
    }
}