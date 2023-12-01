using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Wavee.UI.WinUI.Controls;

public sealed class TrackControl : Control
{
    public TrackControl()
    {
        this.DefaultStyleKey = typeof(TrackControl);
    }

    public static DependencyProperty MainContentProperty =
        DependencyProperty.Register("MainContent", typeof(object), typeof(TrackControl), null);

    public static readonly DependencyProperty ShowImageProperty = DependencyProperty.Register(nameof(ShowImage), typeof(bool), typeof(TrackControl), new PropertyMetadata(default(bool)));
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

    public static readonly DependencyProperty AlternateColorsProperty = DependencyProperty.Register(nameof(AlternateColors), typeof(bool), typeof(TrackControl), new PropertyMetadata(default(bool)));

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
}