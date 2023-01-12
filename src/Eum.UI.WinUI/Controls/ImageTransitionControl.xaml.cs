using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using CommunityToolkit.WinUI.UI.Animations;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Eum.UI.WinUI.Controls;

public sealed partial class ImageTransitionControl : UserControl
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        "Source", typeof(string), typeof(ImageTransitionControl),
        new PropertyMetadata(default(string), OnSourcePropertyChanged));

    //public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
    //    "Stretch", typeof(Stretch), typeof(ImageTransitionControl),
    //    new PropertyMetadata(default(Stretch), OnStretchPropertyChanged));

    // Using a DependencyProperty as the backing store for TransitionDuration.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TransitionDurationProperty =
        DependencyProperty.Register("TransitionDuration", typeof(TimeSpan), typeof(ImageTransitionControl),
            new PropertyMetadata(TimeSpan.FromSeconds(0.15),
                (d, e) =>
                {
                    var control = (ImageTransitionControl) d;
                    var newValue = (TimeSpan) e.NewValue;

                    var fadeInAnim = (Storyboard) control.Resources["FadeInAnim"];
                    fadeInAnim.Children[0].Duration = newValue;

                    var fadeOutAnim = (Storyboard) control.Resources["FadeOutAnim"];
                    fadeOutAnim.Children[0].Duration = newValue;
                }));

    public static readonly DependencyProperty BlurValueProperty =
        DependencyProperty.Register("BlurValue", typeof(double),
            typeof(ImageTransitionControl), new PropertyMetadata(default(double), OnBlurValueChanged));

    public ImageTransitionControl()
    {
        InitializeComponent();
    }

    public string Source
    {
        get => (string) GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /*
            public Stretch Stretch
            {
                get => (Stretch)GetValue(StretchProperty);
                set => SetValue(StretchProperty, value);
            }
    */

    public TimeSpan TransitionDuration
    {
        get => (TimeSpan) GetValue(TransitionDurationProperty);
        set => SetValue(TransitionDurationProperty, value);
    }

    public double BlurValue
    {
        get => (double) GetValue(BlurValueProperty);
        set => SetValue(BlurValueProperty, value);
    }

    private static void OnBlurValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ImageTransitionControl) d;

        var newValue = (double) e.NewValue;
        control.Br.Blur = newValue;
    }

    private static async void OnSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var newImage = e.NewValue as string;
        var oldImage = e.OldValue as string;

        if (newImage == oldImage)
            return;
        var control = (ImageTransitionControl) d;
        if (string.IsNullOrEmpty(newImage))
        {
            await control.FadeOut();
            return;
        }

        if (control.Br.Source != null)
        {
            await control.FadeOut();
            await control.FadeIn();
        }
        else
        {
            control.Br.Source = new BitmapImage(new Uri(newImage));
            //control.FadeIn();
        }
    }

    private static void OnStretchPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ImageTransitionControl) d;
        control.Image.Stretch = (Stretch) e.NewValue;
    }

    private void Image_OnImageOpened(object sender, RoutedEventArgs e)
    {
        FadeIn();
    }

    private Task FadeOut()
    {
        var s = (Storyboard) Resources["FadeOutAnim"];
        return s.BeginAsync();
    }

    private Task FadeIn()
    {
        var s = (Storyboard) Resources["FadeInAnim"];
        return s.BeginAsync();
    }

    private void ImageFadeOutAnim_OnCompleted(object sender, object e)
    {
        if (string.IsNullOrEmpty(Source)) return;
        Br.Source = new BitmapImage(new Uri(Source));

        //if it's a local image, fade in immediately
        FadeIn();
    }

    private void Br_OnOpened(ImageOpacityBrush sender, EventArgs args)
    {
        FadeIn();
    }
}

public class ImageOpacityBrush : XamlCompositionBrushBase, IDisposable
{
    /// <summary>
    ///     Identifies the <see cref="Source" /> dependency property.
    /// </summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(ImageSource), // We use ImageSource type so XAML engine will automatically construct proper object from String.
        typeof(ImageOpacityBrush),
        new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>
    ///     Identifies the <see cref="Stretch" /> dependency property.
    ///     Requires 16299 or higher for modes other than None.
    /// </summary>
    public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
        nameof(Stretch),
        typeof(Stretch),
        typeof(ImageOpacityBrush),
        new PropertyMetadata(Stretch.None, OnPropertyChanged));

    public static readonly DependencyProperty BlurProperty = DependencyProperty.Register(
        nameof(Blur),
        typeof(double),
        typeof(ImageOpacityBrush),
        new PropertyMetadata(default(double), OnBlurPropertyChanged));


    private LoadedImageSurface _surface;
    private CompositionSurfaceBrush _surfaceBrush;

    /// <summary>
    ///     Gets or sets the <see cref="BitmapImage" /> source of the image to composite.
    /// </summary>
    public ImageSource Source
    {
        get => (ImageSource) GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }


    /// <summary>
    ///     Gets or sets how to stretch the image within the brush.
    /// </summary>
    public Stretch Stretch
    {
        get => (Stretch) GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public double Blur
    {
        get => (double) GetValue(BlurProperty);
        set => SetValue(BlurProperty, value);
    }

    public void Dispose()
    {
        // newSurface?.Dispose();
    }

    public event TypedEventHandler<ImageOpacityBrush, EventArgs> Opened;

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var brush = (ImageOpacityBrush) d;
        brush.OnDisconnected();
        brush.OnConnected();
    }

    private static void OnBlurPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var brush = (ImageOpacityBrush) d;

        brush.CompositionBrush?.Properties
            .InsertScalar("blur.BlurAmount", (float) (double) e.NewValue);
    }

    /// <inheritdoc />
    protected override void OnConnected()
    {
        // Delay creating composition resources until they're required.
        if (CompositionBrush == null && Source is BitmapImage bitmap)
        {
            if (bitmap.UriSource == null) return;
            // Use LoadedImageSurface API to get ICompositionSurface from image uri provided
            // If UriSource is invalid, StartLoadFromUri will return a blank texture.
            _surface = LoadedImageSurface.StartLoadFromUri(bitmap.UriSource);

            // Load Surface onto SurfaceBrush
            _surfaceBrush = App.MWindow.Compositor.CreateSurfaceBrush(_surface);
            _surfaceBrush.Stretch = CompositionStretch.UniformToFill;


            var compositor = App.MWindow.Compositor;
            var blurEffect = new GaussianBlurEffect
            {
                Name = "blur",
                BlurAmount = (float) Blur,
                BorderMode = EffectBorderMode.Hard,
                Optimization = EffectOptimization.Balanced,
                Source = new CompositionEffectSourceParameter("Source")
            };


            var opacityMaskSurface = LoadedImageSurface.StartLoadFromUri(new Uri(Path.Combine(AppContext.BaseDirectory, "Assets/amb.png")));
            var opacityBrush = compositor.CreateSurfaceBrush(opacityMaskSurface);
            opacityBrush.Stretch = CompositionStretch.Fill;

            var effect = new AlphaMaskEffect
            {
                Source = blurEffect,
                AlphaMask = new CompositionEffectSourceParameter("Mask")
            };
            var brush = compositor.CreateEffectFactory(
                    effect,
                    new[] {"blur.BlurAmount"})
                .CreateBrush();
            brush.SetSourceParameter("Source", compositor.CreateBackdropBrush());

            brush.SetSourceParameter("Source", _surfaceBrush);
            brush.SetSourceParameter("Mask", opacityBrush);
            CompositionBrush = brush;
            _surface.LoadCompleted += SurfaceOnLoadCompleted;
        }
    }

    private void SurfaceOnLoadCompleted(LoadedImageSurface sender, LoadedImageSourceLoadCompletedEventArgs args)
    {
        Opened?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    protected override void OnDisconnected()
    {
        // Dispose of composition resources when no longer in use.
        if (CompositionBrush != null)
        {
            CompositionBrush.Dispose();
            CompositionBrush = null;
        }

        if (_surfaceBrush != null)
        {
            _surfaceBrush.Dispose();
            _surfaceBrush = null;
        }

        if (_surface != null)
        {
            _surface.LoadCompleted -= SurfaceOnLoadCompleted;
            try
            {
                _surface.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }

            _surface = null;
        }

        //Source = null;

        //GC.Collect();
    }

    //// Helper to allow XAML developer to use XAML stretch property rather than another enum.
    private static CompositionStretch CompositionStretchFromStretch(Stretch value)
    {
        switch (value)
        {
            case Stretch.None:
                return CompositionStretch.None;
            case Stretch.Fill:
                return CompositionStretch.Fill;
            case Stretch.Uniform:
                return CompositionStretch.Uniform;
            case Stretch.UniformToFill:
                return CompositionStretch.UniformToFill;
        }

        return CompositionStretch.None;
    }
}