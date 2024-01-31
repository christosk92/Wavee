using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System.Windows.Input;
using Wavee.UI.ViewModels.NowPlaying;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Common
{
    public sealed partial class NumberOrPlayPauseButton : UserControl
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
        public static readonly DependencyProperty StateProperty = DependencyProperty.Register(nameof(State), typeof(NumberOrPlayButtonState), typeof(NumberOrPlayPauseButton), new PropertyMetadata(default(NumberOrPlayButtonState)));

        public NumberOrPlayPauseButton()
        {
            this.InitializeComponent();
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

        public NumberOrPlayButtonState State
        {
            get => (NumberOrPlayButtonState)GetValue(StateProperty);
            set => SetValue(StateProperty, value);
        }

        private void UpdateVisualState()
        {
            if (!IsHovered)
            {
                if (PlaybackState == WaveeUITrackPlaybackStateType.Playing)
                {
                    State = NumberOrPlayButtonState.NonHoveringPlaying;
                    // VisualStateManager.GoToState(this, "NonHoveringPlaying", true);
                }
                else if (PlaybackState == WaveeUITrackPlaybackStateType.Paused)
                {
                    State = NumberOrPlayButtonState.NonHoveringPaused;

                    //VisualStateManager.GoToState(this, "NonHoveringPaused", true);
                }
                else if (PlaybackState is WaveeUITrackPlaybackStateType.Loading)
                {
                    State = NumberOrPlayButtonState.Loading;

                    // VisualStateManager.GoToState(this, "Loading", true);
                }
                else
                {
                    State = NumberOrPlayButtonState.NonHoveringNotPlaying;

                    //VisualStateManager.GoToState(this, "NonHoveringNotPlaying", true);
                }
            }
            else
            {
                if (PlaybackState == WaveeUITrackPlaybackStateType.Playing)
                {
                    State = NumberOrPlayButtonState.HoveringPlaying;

                    // VisualStateManager.GoToState(this, "HoveringPlaying", true);
                }
                else if (PlaybackState is WaveeUITrackPlaybackStateType.Loading)
                {
                    State = NumberOrPlayButtonState.Loading;

                    //VisualStateManager.GoToState(this, "Loading", true);
                }
                else
                {
                    // This covers both Not Playing and Paused
                    State = NumberOrPlayButtonState.HoveringNotPlayingOrPaused;
                    // VisualStateManager.GoToState(this, "HoveringNotPlayingOrPaused", true);
                }
            }
        }

        public bool IsLoading(NumberOrPlayButtonState state)
        {
            return state is NumberOrPlayButtonState.Loading;
        }

        public bool ShouldShowButton(NumberOrPlayButtonState x)
        {
            switch (x)
            {
                case NumberOrPlayButtonState.NonHoveringPlaying:
                    return false;
                case NumberOrPlayButtonState.NonHoveringPaused:
                    return true;
                case NumberOrPlayButtonState.Loading:
                    return false;
                case NumberOrPlayButtonState.NonHoveringNotPlaying:
                    return false;
                case NumberOrPlayButtonState.HoveringPlaying:
                    return true;
                case NumberOrPlayButtonState.HoveringNotPlayingOrPaused:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(x), x, null);
            }
        }

        public bool ShouldShowIndexBlock(NumberOrPlayButtonState x)
        {
            switch (x)
            {
                case NumberOrPlayButtonState.NonHoveringPlaying:
                    return false;
                case NumberOrPlayButtonState.NonHoveringPaused:
                    return false;
                case NumberOrPlayButtonState.Loading:
                    return false;
                case NumberOrPlayButtonState.NonHoveringNotPlaying:
                    return true;
                case NumberOrPlayButtonState.HoveringPlaying:
                    return false;
                case NumberOrPlayButtonState.HoveringNotPlayingOrPaused:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(x), x, null);
            }
        }

        public bool ShouldShowAnimation(NumberOrPlayButtonState x)
        {
            switch (x)
            {
                case NumberOrPlayButtonState.NonHoveringPlaying:
                    return true;
                case NumberOrPlayButtonState.NonHoveringPaused:
                    return false;
                case NumberOrPlayButtonState.Loading:
                    return false;
                case NumberOrPlayButtonState.NonHoveringNotPlaying:
                    return false;
                case NumberOrPlayButtonState.HoveringPlaying:
                    return false;
                case NumberOrPlayButtonState.HoveringNotPlayingOrPaused:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(x), x, null);
            }
        }

        private async void PlayPauseButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            switch (PlaybackState)
            {
                case WaveeUITrackPlaybackStateType.NotPlaying:
                    PlayCommand.Execute(PlayCommandParameter);
                    break;
                case WaveeUITrackPlaybackStateType.Paused or WaveeUITrackPlaybackStateType.Playing:
                    await NowPlayingViewModel.Instance.PlayPauseCommand.ExecuteAsync(null);
                    break;
            }
        }

        public string GetGlyphFor(NumberOrPlayButtonState x)
        {
            //&#xF5B0;
            switch (x)
            {
                case NumberOrPlayButtonState.NonHoveringPlaying:
                    return "\uE62E"; // pause
                case NumberOrPlayButtonState.NonHoveringPaused:
                    return "\uF5B0"; //play
                case NumberOrPlayButtonState.Loading:
                    return "\uE62E"; // pause
                case NumberOrPlayButtonState.NonHoveringNotPlaying:
                    return "\uF5B0"; // play
                case NumberOrPlayButtonState.HoveringPlaying:
                    return "\uE62E"; // pause
                case NumberOrPlayButtonState.HoveringNotPlayingOrPaused:
                    return "\uF5B0"; // play
                default:
                    throw new ArgumentOutOfRangeException(nameof(x), x, null);
            }
        }
    }
}

public enum NumberOrPlayButtonState
{
    NonHoveringPlaying,
    NonHoveringPaused,
    Loading,
    NonHoveringNotPlaying,
    HoveringPlaying,
    HoveringNotPlayingOrPaused
}