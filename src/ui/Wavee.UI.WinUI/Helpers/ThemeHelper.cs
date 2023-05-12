using System;
using Microsoft.UI.Xaml;

namespace Wavee.UI.WinUI.Helpers;

public static class ThemeHelper
{
    public static Theme CurrentTheme { get; private set; }

    public static void ApplyTheme(Theme theme)
    {
        if (App.MWindow.Content is FrameworkElement f)
        {
            f.RequestedTheme = theme switch
            {
                Theme.Light => ElementTheme.Light,
                Theme.Dark => ElementTheme.Dark,
                Theme.System => ElementTheme.Default,
                _ => throw new NotSupportedException($"Theme {theme} is not supported.")
            };
        }
    }
}