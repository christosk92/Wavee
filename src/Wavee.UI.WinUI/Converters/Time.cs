using System;

namespace Wavee.UI.WinUI.Converters;

public static class Time
{
    public static string FormatTime(TimeSpan time, string? format = null)
    {
        format ??= @"mm\:ss";
        return time.ToString(format);
    }
}