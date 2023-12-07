using System;
using System.Numerics;
using CommunityToolkit.WinUI.UI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Wavee.UI.WinUI.Controls;

public sealed class PlayableContentControl : Control
{
    public static readonly DependencyProperty ViewTypeProperty = DependencyProperty.Register(nameof(ViewType), typeof(PlayableContentViewType), typeof(PlayableContentControl), new PropertyMetadata(default(PlayableContentViewType)));
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(PlayableContentControl), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(PlayableContentControl), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(string), typeof(PlayableContentControl), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(string), typeof(PlayableContentControl), new PropertyMetadata(default(string), ImagedChanged));

    private static void ImagedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = (PlayableContentControl)d;
        x.OnImageChanged(e.NewValue is string s ? s : null);
    }

    private void OnImageChanged(string? newImage)
    {
        switch (ViewType)
        {
            case PlayableContentViewType.AlbumSquare:
                {
                    if (!string.IsNullOrEmpty(newImage))
                    {
                        var albumSquare = (Image)GetTemplateChild("AlbumImageBoxImage");
                        var bmpg = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                        bmpg.UriSource = new Uri(newImage);
                        albumSquare.Source = bmpg;
                    }

                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public PlayableContentControl()
    {
        this.DefaultStyleKey = typeof(PlayableContentControl);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        Render();
    }

    private void Render()
    {
        switch (ViewType)
        {
            case PlayableContentViewType.AlbumSquare:
                {
                    VisualStateManager.GoToState(this, "AlbumSquare", false);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override async void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);

        switch (ViewType)
        {
            case PlayableContentViewType.AlbumSquare:
                {
                    var albumSquare = (UIElement)GetTemplateChild("AlbumImageBox");
                    await AnimationBuilder.Create()
                        .Scale(to: new Vector2(1.05f, 1.05f), from: new Vector2(1f, 1f),
                            duration: TimeSpan.FromMilliseconds(200))
                        .StartAsync(albumSquare);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override async void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);

        switch (ViewType)
        {
            case PlayableContentViewType.AlbumSquare:
                {
                    var albumSquare = (UIElement)GetTemplateChild("AlbumImageBox");
                    await AnimationBuilder.Create()
                        .Scale(to: new Vector2(1f, 1f), from: new Vector2(1.05f, 1.05f),
                            duration: TimeSpan.FromMilliseconds(200))
                        .StartAsync(albumSquare);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }


    public PlayableContentViewType ViewType
    {
        get => (PlayableContentViewType)GetValue(ViewTypeProperty);
        set => SetValue(ViewTypeProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string Id
    {
        get => (string)GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }

    public string Image
    {
        get => (string)GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }
}

public enum PlayableContentViewType
{
    AlbumSquare
}