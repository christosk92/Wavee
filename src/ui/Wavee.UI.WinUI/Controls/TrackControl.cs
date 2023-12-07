using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Imaging;
using Spotify.Metadata;
using Image = Microsoft.UI.Xaml.Controls.Image;

namespace Wavee.UI.WinUI.Controls;

public sealed class TrackControl : Control
{
    public TrackControl()
    {
        this.DefaultStyleKey = typeof(TrackControl);
    }

    public static DependencyProperty MainContentProperty =
        DependencyProperty.Register("MainContent", typeof(object), typeof(TrackControl), null);

    public static readonly DependencyProperty ShowImageProperty = DependencyProperty.Register(nameof(ShowImage), typeof(bool), typeof(TrackControl), new PropertyMetadata(default(bool), PropertyChangedCallback));
    public static readonly DependencyProperty NumberProperty = DependencyProperty.Register(nameof(Number), typeof(int), typeof(TrackControl), new PropertyMetadata(default(int), PropertyChangedCallback));

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        ReRender();
    }

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = d as TrackControl;
        x.ReRender();
    }

    private void ReRender()
    {
        if (AlternateColors)
        {
            var isOdd = Number % 2 == 1;
            if (isOdd)
            {
                VisualStateManager.GoToState(this, "Odd", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Even", true);
            }
        }
        else
        {
            VisualStateManager.GoToState(this, "Default", true);
        }

        var imageControl = GetTemplateChild("MainImage") as Image;
        if (ShowImage)
        {
            VisualStateManager.GoToState(this, "ShowImage", true);
            if (imageControl is not null && !string.IsNullOrEmpty(Image))
            {
                var bmp = new BitmapImage();
                bmp.DecodePixelHeight = 50;
                bmp.DecodePixelWidth = 50;
                bmp.UriSource = new Uri(Image);
                imageControl.Source = bmp;
            }
        }
        else
        {
            if (imageControl is not null)
            {
                imageControl.Source = null;
            }

            VisualStateManager.GoToState(this, "HideImage", true);
        }

        NumberString = Number.ToString();
    }

    public static readonly DependencyProperty AlternateColorsProperty = DependencyProperty.Register(nameof(AlternateColors), typeof(bool), typeof(TrackControl), new PropertyMetadata(default(bool), PropertyChangedCallback));
    public static readonly DependencyProperty NumberStringProperty = DependencyProperty.Register(nameof(NumberString), typeof(string), typeof(TrackControl), new PropertyMetadata(default(string), PropertyChangedCallback));
    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(string), typeof(TrackControl), new PropertyMetadata(default(string?), PropertyChangedCallback));

    public object MainContent
    {
        get => GetValue(MainContentProperty);
        set => SetValue(MainContentProperty, value);
    }

    public bool ShowImage
    {
        get => (bool)GetValue(ShowImageProperty);
        set => SetValue(ShowImageProperty, value);
    }

    public int Number
    {
        get => (int)GetValue(NumberProperty);
        set => SetValue(NumberProperty, value);
    }

    public bool AlternateColors
    {
        get => (bool)GetValue(AlternateColorsProperty);
        set => SetValue(AlternateColorsProperty, value);
    }

    public string NumberString
    {
        get => (string)GetValue(NumberStringProperty);
        set => SetValue(NumberStringProperty, value);
    }

    public string? Image
    {
        get => (string?)GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }
}