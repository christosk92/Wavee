using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Microsoft.UI.Xaml.Input;
using Wavee.UI.ViewModels.Track;

namespace Wavee.UI.WinUI.Controls
{
    public sealed class TrackControlContainer : ContentControl
    {
        private ContentPresenter _contentPresenter;

        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.Register(nameof(IsPlaying),
                typeof(bool),
                typeof(TrackControlContainer),
                new PropertyMetadata(false, OnIsPlayingPropertyChanged));

        public static readonly DependencyProperty IsPausedProperty =
            DependencyProperty.Register(nameof(IsPaused),
                typeof(bool),
                typeof(TrackControlContainer),
                new PropertyMetadata(true, OnIsPlayingPropertyChanged));


        // public static readonly DependencyProperty VisualStateProperty =
        //     DependencyProperty.Register(nameof(VisualState),
        //         typeof(TrackState),
        //         typeof(TrackControlContainer),
        //         new PropertyMetadata(TrackState.Nothing, OnVisualStateChanged));

        public static readonly DependencyProperty IndexStyleProperty =
            DependencyProperty.Register(nameof(IndexStyle), typeof(Style),
                typeof(TrackControlContainer),
                new PropertyMetadata(default(Style)));

        // public TrackState VisualState
        // {
        //     get => (TrackState)GetValue(VisualStateProperty);
        //     private set => SetValue(VisualStateProperty, value);
        // }
        public bool IsPlaying
        {
            get => (bool)GetValue(IsPlayingProperty);
            set => SetValue(IsPlayingProperty, value);
        }
        public bool IsPaused
        {
            get => (bool)GetValue(IsPausedProperty);
            set => SetValue(IsPausedProperty, value);
        }
        public Style IndexStyle
        {
            get => (Style)GetValue(IndexStyleProperty);
            set => SetValue(IndexStyleProperty, value);
        }

        public TrackViewModel Track
        {
            get => (TrackViewModel)GetValue(TrackProperty);
            set => SetValue(TrackProperty, value);
        }

        public TrackControlContainer()
        {
            this.DefaultStyleKey = typeof(TrackControlContainer);
        }
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _contentPresenter = GetTemplateChild("PART_ContentPresenter") as ContentPresenter;
            //UpdateVisualState(VisualState);
        }
        // private static void OnVisualStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        // {
        //     var control = d as TrackControlContainer;
        //     control?.UpdateVisualState((TrackState)e.NewValue);
        // }
        //OnIsPlayingPropertyChanged
        private static void OnIsPlayingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as TrackControlContainer;
            control?.UpdateState();
        }

        private void UpdateVisualState(TrackState state)
        {
            if (_contentPresenter?.Content is Control contentElement)
            {
                VisualStateManager.GoToState(contentElement, state.ToString(), true);
            }
        }

        private TrackState? _prevState;
        public static readonly DependencyProperty TrackProperty = DependencyProperty.Register(nameof(Track), typeof(TrackViewModel), typeof(TrackControlContainer), new PropertyMetadata(default(TrackViewModel)));

        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered(e);
            if (IsPlaying)
            {
                if (IsPaused)
                {
                    UpdateVisualState(TrackState.HoverPaused);
                }
                else
                {
                    UpdateVisualState(TrackState.HoverPlaying);
                }
            }
            else
            {
                UpdateVisualState(TrackState.Hover);
            }
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
            if (IsPlaying)
            {
                if (IsPaused)
                {
                    UpdateVisualState(TrackState.Paused);
                }
                else
                {
                    UpdateVisualState(TrackState.Playing);
                }
            }
            else
            {
                UpdateVisualState(TrackState.Nothing);
            }
        }

        private void UpdateState()
        {
            if (IsPlaying)
            {
                if (IsPaused)
                {
                    UpdateVisualState(TrackState.Paused);
                }
                else
                {
                    UpdateVisualState(TrackState.Playing);
                }
            }
            else
            {
                UpdateVisualState(TrackState.Nothing);
            }
        }
    }

    public enum TrackState
    {
        Nothing,
        Playing,
        Paused,
        Hover,
        HoverPlaying,
        HoverPaused
    }
}
