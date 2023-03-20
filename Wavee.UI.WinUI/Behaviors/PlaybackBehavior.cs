using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Wavee.Interfaces.Models;
using Wavee.UI.ViewModels.Playback;
using Wavee.UI.ViewModels.Track;
using Wavee.UI.WinUI.Controls;

namespace Wavee.UI.WinUI.Behaviors;

public static class PlaybackBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(PlaybackBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEnabledProperty, value);
    }


    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(d is ListView listView)) return;

        if ((bool)e.NewValue)
        {
            listView.Loaded += ListView_Loaded;
            listView.Unloaded += ListView_Unloaded;
            listView.ContainerContentChanging += ListView_ContainerContentChanging;
        }
        else
        {
            listView.Loaded -= ListView_Loaded;
            listView.Unloaded -= ListView_Unloaded;
            listView.ContainerContentChanging -= ListView_ContainerContentChanging;
        }
    }

    private static void ListView_Loaded(object sender, RoutedEventArgs e)
    {
        var listView = (ListView)sender;
        if (PlaybackViewModel.Instance != null)
        {
            PlaybackViewModel.Instance.PlayingItemChanged += PlaybackViewModel_PlaybackEvent;
        }
    }

    private static void ListView_Unloaded(object sender, RoutedEventArgs e)
    {
        var listView = (ListView)sender;
        if (PlaybackViewModel.Instance != null)
        {
            PlaybackViewModel.Instance.PlayingItemChanged -= PlaybackViewModel_PlaybackEvent;
        }
    }

    private static void PlaybackViewModel_PlaybackEvent(object sender, IPlayableItem e)
    {
    }

    private static void ListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (!args.InRecycleQueue && args.ItemContainer != null)
        {
            var listView = (ListView)sender;

            if (PlaybackViewModel.Instance != null)
            {
                var trackControlContainer = FindVisualChild<TrackControlContainer>(args.ItemContainer);
                if (trackControlContainer != null)
                {
                    var trackViewModel = (TrackViewModel)args.Item;
                    trackControlContainer.IsPlaying = trackViewModel.Track.Equals(PlaybackViewModel.Instance.PlayingItem);
                }
            }
        }
    }



    private static T FindVisualChild<T>(DependencyObject depObj) where T : DependencyObject
    {
        if (depObj == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            var child = VisualTreeHelper.GetChild(depObj, i);
            if (child != null && child is T)
            {
                return (T)child;
            }

            T childItem = FindVisualChild<T>(child);
            if (childItem != null) return childItem;
        }
        return null;
    }
}
