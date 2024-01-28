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
}