using System;
using Microsoft.UI.Xaml;
using Wavee.UI.ViewModels.Artist;

namespace Wavee.UI.WinUI.Converters;

public static class FunctionBindings
{
    public static Visibility IsNullThenVisible(object? x)
    {
        return x is null ? Visibility.Visible : Visibility.Collapsed;
    }
    public static Visibility IsNullThenCollapsed(object? x)
    {
        return x is null ? Visibility.Collapsed : Visibility.Visible;
    }

    public static Visibility TrueThenVisible(bool o)
    {
        return o ? Visibility.Visible : Visibility.Collapsed;
    }
    public static Visibility TrueThenCollapsed(bool o)
    {
        return o ? Visibility.Collapsed : Visibility.Visible;
    }

    public static bool Negate(bool b) => !b;

    public static Style AlbumTrackStyleSelector(int i)
    {
        var minusOne = i - 1;
        var isEven = minusOne % 2 == 0;
        if (isEven) return (Style)Application.Current.Resources["EvenStyle"];
        return (Style)Application.Current.Resources["OddStyle"];
    }

    public static string FormatTrackNumber(int i)
    {
        var x = $"{i:D2}.";
        return x;
    }

    public static string FormatPlaycount(long l)
    {
        return $"{l:#,##0}";
    }

    public static string FormatDuration(TimeSpan timeSpan) => MilliSecondsToTimestampConverter.ConvertTo(timeSpan);

    public static Visibility IsPlayingThenVisible(WaveeUITrackPlaybackStateType waveeUiTrackPlaybackStateType)
    {
        return waveeUiTrackPlaybackStateType is WaveeUITrackPlaybackStateType.Paused
            or WaveeUITrackPlaybackStateType.Playing
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public static string IsPlayingThenPlayOrElsePausedIcon(WaveeUITrackPlaybackStateType waveeUiTrackPlaybackStateType)
    {
        if (waveeUiTrackPlaybackStateType is WaveeUITrackPlaybackStateType.Paused or WaveeUITrackPlaybackStateType.NotPlaying or WaveeUITrackPlaybackStateType.Loading) return "\uF5B0";
        return "\uE62E";
    }

    public static bool IsPlayingThenTrue(WaveeUITrackPlaybackStateType waveeUiTrackPlaybackStateType)
    {
        return waveeUiTrackPlaybackStateType is WaveeUITrackPlaybackStateType.Playing
            or WaveeUITrackPlaybackStateType.Paused;
    }

    public static Visibility IsONLYPlayingOrNonHoveredThenVisible(WaveeUITrackPlaybackStateType waveeUiTrackPlaybackStateType, bool hovering)
    {
        if (!hovering)
        {
            //if not hoverng, and playing; show
            if (waveeUiTrackPlaybackStateType is WaveeUITrackPlaybackStateType.Playing)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        //if hovernig, collapse
        return Visibility.Collapsed;
    }

    public static Visibility IsOnlyPausedOrHoveredThenVisible(WaveeUITrackPlaybackStateType waveeUiTrackPlaybackStateType, bool hovering)
    {
        if (hovering) return Visibility.Visible;

        if (waveeUiTrackPlaybackStateType is WaveeUITrackPlaybackStateType.Paused) return Visibility.Visible;

        return Visibility.Collapsed;
    }

    public static Visibility IsPlayingOrHovered(WaveeUITrackPlaybackStateType waveeUiTrackPlaybackStateType, bool hovering)
    {
        if (waveeUiTrackPlaybackStateType is WaveeUITrackPlaybackStateType.Playing
            or WaveeUITrackPlaybackStateType.Paused) return Visibility.Visible;
        return hovering ? Visibility.Visible : Visibility.Collapsed;
    }
}