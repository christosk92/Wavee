using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;


namespace Wavee.UI.WinUI.Views.Common;

public sealed class NumberOrPlayPauseButton : Control
{
    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = (NumberOrPlayPauseButton)d;
        x.OnPropertyChanged(e);
    }
    public static readonly DependencyProperty PlaybackStateProperty = DependencyProperty.Register(nameof(PlaybackState), typeof(WaveeUITrackPlaybackStateType), typeof(NumberOrPlayPauseButton),
        new PropertyMetadata(default(WaveeUITrackPlaybackStateType), PropertyChangedCallback));

    public static readonly DependencyProperty NumberBlockStyleProperty = DependencyProperty.Register(nameof(NumberBlockStyle), typeof(Style),
        typeof(NumberOrPlayPauseButton), new PropertyMetadata(default(Style), PropertyChangedCallback));
    public static readonly DependencyProperty NumberTextProperty = DependencyProperty.Register(nameof(NumberText), typeof(string),
        typeof(NumberOrPlayPauseButton), new PropertyMetadata(default(string), PropertyChangedCallback));
    public static readonly DependencyProperty IsHoveredProperty = DependencyProperty.Register(nameof(IsHovered), typeof(bool),
        typeof(NumberOrPlayPauseButton), new PropertyMetadata(false, PropertyChangedCallback));

    public static readonly DependencyProperty PlayCommandProperty = DependencyProperty.Register(nameof(PlayCommand), typeof(ICommand), typeof(NumberOrPlayPauseButton), new PropertyMetadata(default(ICommand)));
    public static readonly DependencyProperty PlayCommandParameterProperty = DependencyProperty.Register(nameof(PlayCommandParameter), typeof(object), typeof(NumberOrPlayPauseButton), new PropertyMetadata(default(object)));

    public NumberOrPlayPauseButton()
    {
        this.DefaultStyleKey = typeof(NumberOrPlayPauseButton);
    }

    private ContentControl _x;
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _x = (ContentControl)this.GetTemplateChild("ContentPresenter");
        UpdateVisualState();
    }
    public bool IsHovered
    {
        get => (bool)GetValue(IsHoveredProperty);
        set => SetValue(IsHoveredProperty, value);
    }

    private void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        UpdateVisualState();
    }
    public WaveeUITrackPlaybackStateType PlaybackState
    {
        get => (WaveeUITrackPlaybackStateType)GetValue(PlaybackStateProperty);
        set => SetValue(PlaybackStateProperty, value);
    }

    public Style NumberBlockStyle
    {
        get => (Style)GetValue(NumberBlockStyleProperty);
        set => SetValue(NumberBlockStyleProperty, value);
    }

    public string NumberText
    {
        get => (string)GetValue(NumberTextProperty);
        set => SetValue(NumberTextProperty, value);
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


    private Button? _playpauseButton;
    private void UpdateVisualState()
    {
        if (!IsHovered)
        {
            if (PlaybackState == WaveeUITrackPlaybackStateType.Playing)
            {
                VisualStateManager.GoToState(this, "NonHoveringPlaying", true);
            }
            else if (PlaybackState == WaveeUITrackPlaybackStateType.Paused)
            {
                VisualStateManager.GoToState(this, "NonHoveringPaused", true);
            }
            else if (PlaybackState is WaveeUITrackPlaybackStateType.Loading)
            {
                VisualStateManager.GoToState(this, "Loading", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "NonHoveringNotPlaying", true);
            }
        }
        else
        {
            if (PlaybackState == WaveeUITrackPlaybackStateType.Playing)
            {
                VisualStateManager.GoToState(this, "HoveringPlaying", true);
            }
            else if (PlaybackState is WaveeUITrackPlaybackStateType.Loading)
            {
                VisualStateManager.GoToState(this, "Loading", true);
            }
            else
            {
                // This covers both Not Playing and Paused
                VisualStateManager.GoToState(this, "HoveringNotPlayingOrPaused", true);
            }
        }

        if (_x?.Content is Button y)
        {
            _playpauseButton = y;
            _playpauseButton.CommandParameter = PlayCommandParameter;
            _playpauseButton.Command = PlayCommand;
        }
    }

}