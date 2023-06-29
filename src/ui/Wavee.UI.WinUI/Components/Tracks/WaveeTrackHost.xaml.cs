using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Xml.Linq;
using Microsoft.UI.Xaml.Markup;
using System;

namespace Wavee.UI.WinUI.Components.Tracks;

[ContentProperty(Name = "MContent")]
public sealed partial class WaveeTrackHost : UserControl
{
    public static readonly DependencyProperty MContentProperty = DependencyProperty.Register(nameof(MContent), typeof(object), typeof(WaveeTrackHost), new PropertyMetadata(default(object)));
    public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(nameof(Index), typeof(ushort), typeof(WaveeTrackHost), new PropertyMetadata(default(ushort), UIPropertyChanged));
    public static readonly DependencyProperty AlternateRowColorProperty = DependencyProperty.Register(nameof(AlternateRowColor), typeof(bool), typeof(WaveeTrackHost), new PropertyMetadata(default(bool), UIPropertyChanged));
    public static readonly DependencyProperty ShowImageProperty = DependencyProperty.Register(nameof(ShowImage), typeof(bool), typeof(WaveeTrackHost), new PropertyMetadata(default(bool), UIPropertyChanged));
    public WaveeTrackHost()
    {
        this.InitializeComponent();
    }

    public object MContent
    {
        get => (object)GetValue(MContentProperty);
        set => SetValue(MContentProperty, value);
    }

    public ushort Index
    {
        get => (ushort)GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }

    public bool AlternateRowColor
    {
        get => (bool)GetValue(AlternateRowColorProperty);
        set => SetValue(AlternateRowColorProperty, value);
    }

    public bool ShowImage
    {
        get => (bool)GetValue(ShowImageProperty);
        set => SetValue(ShowImageProperty, value);
    }

    private static void UIPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = (WaveeTrackHost)d;
        x.UpdateUI();
    }

    private void UpdateUI()
    {

    }

    public string FormatNumber(ushort x)
    {
        //1 -> 01.
        //10 -> 10.
        //100 -> 100.

        return $"{(x + 1):D2}.";
    }

    public Style GetStyleFor(int i)
    {
        //EvenBorderStyleGrid
        //OddBorderStyleGrid
        return
            !AlternateRowColor || (i % 2 == 0)
                ? (Style)Application.Current.Resources["OddBorderStyleGrid"]
                : (Style)Application.Current.Resources["EvenBorderStyleGrid"];
    }

    public string FormatIndex(ushort @ushort)
    {
        return $"{@ushort:D2}.";
    }

    private async void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
    {
        var p = (AnimatedVisualPlayer)sender;
        if (!p.IsPlaying)
        {
            await p.PlayAsync(0, 1, true);
        }
    }
}