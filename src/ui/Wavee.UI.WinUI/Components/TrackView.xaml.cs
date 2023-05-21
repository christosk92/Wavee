using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace Wavee.UI.WinUI.Components;

public sealed partial class TrackView : UserControl
{
    public static readonly DependencyProperty ViewProperty = DependencyProperty.Register(nameof(View), typeof(object), typeof(TrackView), new PropertyMetadata(default(object)));
    public static readonly DependencyProperty IndexProperty = DependencyProperty.Register(nameof(Index), typeof(int), typeof(TrackView), new PropertyMetadata(default(int)));
    public static readonly DependencyProperty ShowImageProperty = DependencyProperty.Register(nameof(ShowImage), typeof(bool), typeof(TrackView), new PropertyMetadata(default(bool)));
    public static readonly DependencyProperty ImageUrlProperty =
        DependencyProperty.Register(nameof(ImageUrl),
            typeof(string), typeof(TrackView),
            new PropertyMetadata(default(string?)));

    public static readonly DependencyProperty AlternatingRowColorProperty = DependencyProperty.Register(nameof(AlternatingRowColor), typeof(bool), typeof(TrackView), new PropertyMetadata(default(bool)));

    public TrackView()
    {
        this.InitializeComponent();
    }

    public object View
    {
        get => (object)GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }

    public int Index
    {
        get => (int)GetValue(IndexProperty);
        set => SetValue(IndexProperty, value);
    }

    public bool ShowImage
    {
        get => (bool)GetValue(ShowImageProperty);
        set => SetValue(ShowImageProperty, value);
    }

    public string ImageUrl
    {
        get => (string)GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public bool AlternatingRowColor
    {
        get => (bool)GetValue(AlternatingRowColorProperty);
        set => SetValue(AlternatingRowColorProperty, value);
    }

    public string FormatIndex(int i)
    {
        //if we have 1, we want 01.
        //2 should be 02.
        //3 should be 03.
        var index = i + 1;
        var str = index.ToString("D2");
        return $"{str}.";
    }

    public bool HasImage(string img, bool ifTrue)
    {
        return !string.IsNullOrEmpty(img) ? ifTrue : !ifTrue;
    }

    public Style GetStyleFor(int i)
    {
        return
            !AlternatingRowColor || (i % 2 == 0)
                ? (Style)Application.Current.Resources["EvenBorderStyle"]
                : (Style)Application.Current.Resources["OddBorderStyle"];
    }
}