using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Xml.Linq;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.Input;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using NAudio.Wave;
using Wavee.UI.Client.Playback;
using Wavee.UI.ViewModel.Playback;
using Wavee.UI.ViewModel.Shell;

namespace Wavee.UI.WinUI.Components.Tracks;

[ContentProperty(Name = "MContent")]
public sealed partial class WaveeTrackHost : UserControl
{
    private static IPlayParameter? _invokedWith;

    public static readonly DependencyProperty MContentProperty = DependencyProperty.Register(nameof(MContent), typeof(object), typeof(WaveeTrackHost), new PropertyMetadata(default(object)));
    public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(nameof(Index), typeof(ushort), typeof(WaveeTrackHost), new PropertyMetadata(default(ushort), UIPropertyChanged));
    public static readonly DependencyProperty AlternateRowColorProperty = DependencyProperty.Register(nameof(AlternateRowColor), typeof(bool), typeof(WaveeTrackHost), new PropertyMetadata(default(bool), UIPropertyChanged));
    public static readonly DependencyProperty ShowImageProperty = DependencyProperty.Register(nameof(ShowImage), typeof(bool), typeof(WaveeTrackHost), new PropertyMetadata(default(bool), UIPropertyChanged));
    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(string), typeof(WaveeTrackHost), new PropertyMetadata(default(string?), UIPropertyChanged));
    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(string), typeof(WaveeTrackHost), new PropertyMetadata(default(string), UIPropertyChanged));
    public static readonly DependencyProperty WithCheckboxProperty = DependencyProperty.Register(nameof(WithCheckbox), typeof(bool), typeof(WaveeTrackHost), new PropertyMetadata(default(bool)));
    public static readonly DependencyProperty PlaybackStateProperty = DependencyProperty.Register(nameof(PlaybackState), typeof(TrackPlaybackState), typeof(WaveeTrackHost), new PropertyMetadata(default(TrackPlaybackState), PlaybackStateChanged));
    private IDisposable? _subscrption;
    public static readonly DependencyProperty UidProperty = DependencyProperty.Register(nameof(Uid), typeof(Option<string>), typeof(WaveeTrackHost), new PropertyMetadata(default(Option<string>), UIPropertyChanged));
    public static readonly DependencyProperty PlaycommandProperty = DependencyProperty.Register(nameof(Playcommand), typeof(AsyncRelayCommand<IPlayParameter>), typeof(WaveeTrackHost), new PropertyMetadata(default(AsyncRelayCommand<IPlayParameter>)));


    public static readonly DependencyProperty PlayparameterProperty = DependencyProperty.Register(nameof(Playparameter), typeof(IPlayParameter), typeof(WaveeTrackHost), new PropertyMetadata(default(IPlayParameter)));

    public WaveeTrackHost()
    {
        this.InitializeComponent();
    }

    public TrackPlaybackState PlaybackState
    {
        get => (TrackPlaybackState)GetValue(PlaybackStateProperty);
        set => SetValue(PlaybackStateProperty, value);
    }
    public object MContent
    {
        get => (object)GetValue(MContentProperty);
        set => SetValue(MContentProperty, value);
    }

    public ushort Index
    {
        get => (ushort)GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }

    public bool AlternateRowColor
    {
        get => (bool)GetValue(AlternateRowColorProperty);
        set => SetValue(AlternateRowColorProperty, value);
    }

    public bool ShowImage
    {
        get => (bool)GetValue(ShowImageProperty);
        set => SetValue(ShowImageProperty, value);
    }

    public string? Image
    {
        get => (string?)GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public string Id
    {
        get => (string)GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }

    public bool WithCheckbox
    {
        get => (bool)GetValue(WithCheckboxProperty);
        set => SetValue(WithCheckboxProperty, value);
    }

    public Option<string> Uid
    {
        get => (Option<string>)GetValue(UidProperty);
        set => SetValue(UidProperty, value);
    }

    public AsyncRelayCommand<IPlayParameter> Playcommand
    {
        get => (AsyncRelayCommand<IPlayParameter>)GetValue(PlaycommandProperty);
        set => SetValue(PlaycommandProperty, value);
    }

    public IPlayParameter Playparameter
    {
        get => (IPlayParameter)GetValue(PlayparameterProperty);
        set => SetValue(PlayparameterProperty, value);
    }


    private static void UIPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = (WaveeTrackHost)d;
        x.UpdateUI();
    }
    private void ChangePlaybackState(TrackPlaybackState state)
    {
        switch (state)
        {
            case TrackPlaybackState.None:
                if (Playcommand is
                    {
                        IsRunning: true
                    } && _invokedWith?.Equals(Playparameter) is true)
                {
                    VisualStateManager.GoToState(this, "LoadingState", true);
                }
                else
                {
                    VisualStateManager.GoToState(this, "NoPlayback", true);
                }

                break;
            case TrackPlaybackState.Playing:
                VisualStateManager.GoToState(this, "PlayingPlayback", true);
                // PlaybackViewContent.Content = new AnimatedVisualPlayer
                // {
                //     Source = new Equaliser(),
                //     AutoPlay = true,
                //     PlaybackRate = 2.0
                // };
                break;
            case TrackPlaybackState.Paused:
                VisualStateManager.GoToState(this, "PausedPlayback", true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void UpdateUI()
    {
        if (ShowImage)
        {
            var buttonsPanel = this.FindName("ImageBorder") as FrameworkElement;
            if (buttonsPanel != null)
            {
                buttonsPanel.Visibility = Visibility.Visible;
                buttonsPanel.Width = 28;
            }

            if (!string.IsNullOrEmpty(Image))
            {
                var bitmapImage = new BitmapImage();
                AlbumImage.Source = bitmapImage;
                bitmapImage.DecodePixelHeight = 32;
                bitmapImage.DecodePixelWidth = 32;
                bitmapImage.UriSource = new System.Uri(Image, UriKind.RelativeOrAbsolute);

                RelativePanel.SetRightOf(MainContent, "ImageBorder");
            }
        }
        else
        {
            //   var buttonsPanel = this.FindName("ButtonsPanel") as UIElement;
            if (ImageBorder != null)
            {
                ImageBorder.Visibility = Visibility.Collapsed;
                AlbumImage.Source = null;
                Microsoft.UI.Xaml.Markup.XamlMarkupHelper.UnloadObject(ImageBorder);
                RelativePanel.SetRightOf(MainContent, "SavedButton");
            }
        }

        if (!string.IsNullOrEmpty(Id))
        {
            _subscrption?.Dispose();
            _subscrption = null;
            _subscrption = ShellViewModel.Instance.Playback.CreateListener()
                .StartWith(ShellViewModel.Instance.Playback._lastReceivedState)
                .Subscribe(PlaybackChanged);
        }
    }

    public void PlaybackChanged(WaveeUIPlaybackState state)
    {
        if ((Uid.IsSome && ShellViewModel.Instance.Playback.Uid.IsSome
                                        && ShellViewModel.Instance.Playback.Uid.ValueUnsafe().Equals(Uid.ValueUnsafe()))
            || ShellViewModel.Instance.Playback.ItemId.Equals(Id))
        {
            PlaybackState = ShellViewModel.Instance.Playback.Paused
                ? TrackPlaybackState.Paused
                : TrackPlaybackState.Playing;
        }
        else
        {
            PlaybackState = TrackPlaybackState.None;
        }
    }

    public string FormatNumber(ushort x)
    {
        //1 -> 01.
        //10 -> 10.
        //100 -> 100.

        return $"{(x + 1):D2}.";
    }

    public Style GetStyleFor(ushort i)
    {
        //EvenBorderStyleGrid
        //OddBorderStyleGrid
        return
            !AlternateRowColor || (i % 2 == 0)
                ? (Style)Application.Current.Resources["EvenBorderStyleGrid"]
                : (Style)Application.Current.Resources["OddBorderStyleGrid"];
    }

    public string FormatIndex(ushort @ushort)
    {
        return $"{(@ushort + 1):D2}.";
    }

    private async void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
    {
        var p = (AnimatedVisualPlayer)sender;
        if (!p.IsPlaying)
        {
            await p.PlayAsync(0, 1, true);
        }
    }
    private static void PlaybackStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = (WaveeTrackHost)d;

        if (e.NewValue is TrackPlaybackState tr && e.NewValue != e.OldValue)
        {
            x.ChangePlaybackState(tr);
        }
    }

    private void WaveeTrackHost_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType is PointerDeviceType.Touch)
        {
            return;
        }
        switch (PlaybackState)
        {
            case TrackPlaybackState.None:
                if (Playcommand is
                    {
                        IsRunning: true
                    } && _invokedWith?.Equals(Playparameter) is true)
                {
                    VisualStateManager.GoToState(this, "LoadingState", true);
                }
                else
                {
                    VisualStateManager.GoToState(this, "NoPlaybackHover", true);
                }
                break;
            case TrackPlaybackState.Playing:
                VisualStateManager.GoToState(this, "PlayingPlaybackHover", true);
                break;
            case TrackPlaybackState.Paused:
                VisualStateManager.GoToState(this, "PausedPlaybackHover", true);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void WaveeTrackHost_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType is PointerDeviceType.Touch)
        {
            return;
        }
        switch (PlaybackState)
        {
            case TrackPlaybackState.None:
                if (Playcommand is
                    {
                        IsRunning: true
                    } && _invokedWith?.Equals(Playparameter) is true)
                {
                    VisualStateManager.GoToState(this, "LoadingState", true);
                }
                else
                {
                    VisualStateManager.GoToState(this, "NoPlayback", true);
                }
                break;
            case TrackPlaybackState.Playing:
                // PlaybackViewContent.Content = new AnimatedVisualPlayer
                // {
                //     Source = new Equaliser(),
                //     AutoPlay = true,
                //     PlaybackRate = 2.0
                // };
                VisualStateManager.GoToState(this, "PlayingPlayback", true);
                break;
            case TrackPlaybackState.Paused:
                VisualStateManager.GoToState(this, "PausedPlayback", true);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void WaveeTrackHost_OnUnloaded(object sender, RoutedEventArgs e)
    {
        _subscrption?.Dispose();
        _subscrption = null;
    }

    private void WaveeTrackHost_OnLoading(FrameworkElement sender, object args)
    {
        if (!string.IsNullOrEmpty(Id))
        {
            _subscrption?.Dispose();
            _subscrption = null;
            UpdateUI();
        }
    }

    public Visibility TrueToCollapsed(bool b)
    {
        return b ? Visibility.Collapsed : Visibility.Visible;
    }

    private void PlaButton_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "LoadingState", true);
        _invokedWith = Playparameter;
    }
}
public enum TrackPlaybackState
{
    None,
    Playing,
    Paused
}