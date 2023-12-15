using System;
using System.Windows.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Spotify.Metadata;
using Wavee.Domain.Playback;
using Image = Microsoft.UI.Xaml.Controls.Image;

namespace Wavee.UI.WinUI.Controls;

public sealed class TrackControl : Control
{
    public TrackControl()
    {
        this.DefaultStyleKey = typeof(TrackControl);
    }

    public static DependencyProperty MainContentProperty =
        DependencyProperty.Register("MainContent", typeof(object), typeof(TrackControl), null);

    public static readonly DependencyProperty ShowImageProperty = DependencyProperty.Register(nameof(ShowImage), typeof(bool), typeof(TrackControl), new PropertyMetadata(default(bool), PropertyChangedCallback));
    public static readonly DependencyProperty NumberProperty = DependencyProperty.Register(nameof(Number), typeof(int), typeof(TrackControl), new PropertyMetadata(default(int), PropertyChangedCallback));

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        ReRender();
    }

    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        var correctState = PlaybackState switch
        {
            WaveeTrackPlaybackState.NotPlaying => "PointerNotPlaying",
            WaveeTrackPlaybackState.Playing => "PointerPlaying",
            WaveeTrackPlaybackState.Paused => "PointerPaused",
            _ => throw new ArgumentOutOfRangeException()
        };
        VisualStateManager.GoToState(this, correctState, true);
    }

    private void PlayButtonOnTapped(object sender, TappedRoutedEventArgs e)
    {
        PlayCommand?.Execute(PlayCommandParameter);
    }

    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        //Check if originalSource is not a child of this control
        var originalSource = e.OriginalSource as DependencyObject;
        if (originalSource is not null && !IsChildOf(originalSource))
        {
            var correctState = PlaybackState switch
            {
                WaveeTrackPlaybackState.NotPlaying => "NoPointerNotPlaying",
                WaveeTrackPlaybackState.Playing => "NoPointerPlaying",
                WaveeTrackPlaybackState.Paused => "NoPointerPaused",
                _ => throw new ArgumentOutOfRangeException()
            };

            VisualStateManager.GoToState(this, correctState, true);
        }
    }

    private bool IsChildOf(DependencyObject ob)
    {
        var hasChildOfThis = this.FindDescendant<DependencyObject>(x => x == ob);
        return hasChildOfThis is not null;
    }

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = d as TrackControl;
        x.ReRender();
    }

    private void ReRender()
    {
        if (AlternateColors)
        {
            var isOdd = Number % 2 == 1;
            if (isOdd)
            {
                VisualStateManager.GoToState(this, "Odd", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Even", true);
            }
        }
        else
        {
            VisualStateManager.GoToState(this, "Default", true);
        }

        VisualStateManager.GoToState(this, "NoPointerNotPlaying", true);

        var imageControl = GetTemplateChild("MainImage") as Image;
        if (ShowImage)
        {
            VisualStateManager.GoToState(this, "ShowImage", true);
            if (imageControl is not null && !string.IsNullOrEmpty(Image))
            {
                var bmp = new BitmapImage();
                bmp.DecodePixelHeight = 50;
                bmp.DecodePixelWidth = 50;
                bmp.UriSource = new Uri(Image);
                imageControl.Source = bmp;
            }
        }
        else
        {
            if (imageControl is not null)
            {
                imageControl.Source = null;
            }

            VisualStateManager.GoToState(this, "HideImage", true);
        }

        var correctState = PlaybackState switch
        {
            WaveeTrackPlaybackState.NotPlaying => "NoPointerNotPlaying",
            WaveeTrackPlaybackState.Playing => "NoPointerPlaying",
            WaveeTrackPlaybackState.Paused => "NoPointerPaused",
            _ => throw new ArgumentOutOfRangeException()
        };
        VisualStateManager.GoToState(this, correctState, true);

        NumberString = Number.ToString();
    }

    public static readonly DependencyProperty AlternateColorsProperty = DependencyProperty.Register(nameof(AlternateColors), typeof(bool), typeof(TrackControl), new PropertyMetadata(default(bool), PropertyChangedCallback));
    public static readonly DependencyProperty NumberStringProperty = DependencyProperty.Register(nameof(NumberString), typeof(string), typeof(TrackControl), new PropertyMetadata(default(string), PropertyChangedCallback));
    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(string), typeof(TrackControl), new PropertyMetadata(default(string?), PropertyChangedCallback));
    public static readonly DependencyProperty PlayCommandProperty = DependencyProperty.Register(nameof(PlayCommand), typeof(ICommand), typeof(TrackControl), new PropertyMetadata(default(ICommand)));
    public static readonly DependencyProperty PlayCommandParameterProperty = DependencyProperty.Register(nameof(PlayCommandParameter), typeof(object), typeof(TrackControl), new PropertyMetadata(default(object)));
    public static readonly DependencyProperty PlaybackStateProperty = DependencyProperty.Register(nameof(PlaybackState), typeof(WaveeTrackPlaybackState), typeof(TrackControl), new PropertyMetadata(default(WaveeTrackPlaybackState), PropertyChangedCallback));
    public static readonly DependencyProperty ImageBorderStyleProperty = DependencyProperty.Register(nameof(ImageBorderStyle), typeof(Style), typeof(TrackControl), new PropertyMetadata(default(Style?)));

    public object MainContent
    {
        get => GetValue(MainContentProperty);
        set => SetValue(MainContentProperty, value);
    }

    public bool ShowImage
    {
        get => (bool)GetValue(ShowImageProperty);
        set => SetValue(ShowImageProperty, value);
    }

    public int Number
    {
        get => (int)GetValue(NumberProperty);
        set => SetValue(NumberProperty, value);
    }

    public bool AlternateColors
    {
        get => (bool)GetValue(AlternateColorsProperty);
        set => SetValue(AlternateColorsProperty, value);
    }

    public string NumberString
    {
        get => (string)GetValue(NumberStringProperty);
        set => SetValue(NumberStringProperty, value);
    }

    public string? Image
    {
        get => (string?)GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public ICommand PlayCommand
    {
        get => (ICommand)GetValue(PlayCommandProperty);
        set => SetValue(PlayCommandProperty, value);
    }

    public object PlayCommandParameter
    {
        get => (object)GetValue(PlayCommandParameterProperty);
        set => SetValue(PlayCommandParameterProperty, value);
    }

    public WaveeTrackPlaybackState PlaybackState
    {
        get => (WaveeTrackPlaybackState)GetValue(PlaybackStateProperty);
        set => SetValue(PlaybackStateProperty, value);
    }

    public Style? ImageBorderStyle
    {
        get => (Style?)GetValue(ImageBorderStyleProperty);
        set => SetValue(ImageBorderStyleProperty, value);
    }
}