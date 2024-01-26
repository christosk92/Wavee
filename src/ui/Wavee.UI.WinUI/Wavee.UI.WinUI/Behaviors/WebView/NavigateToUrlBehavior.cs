using System;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace Wavee.UI.WinUI.Behaviors.WebView;

public sealed class NavigateToUrlBehavior : Behavior<WebView2>
{
    public static readonly DependencyProperty UrlProperty = DependencyProperty.Register(nameof(Url),
        typeof(string),
        typeof(NavigateToUrlBehavior), new PropertyMetadata(default(string?), PropertyChangedCallback));

    public string? Url
    {
        get => (string?)GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = d as NavigateToUrlBehavior;
        if (e.NewValue is string y && !string.IsNullOrEmpty(y))
        {
            x!.AssociatedObject.Source = new Uri(y);
        }
        else
        {
            x!.AssociatedObject.Source = null;
        }
    }
}