using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Eum.UI.WinUI.Controls
{
    public class BlurredImageBrush : XamlCompositionBrushBase, IDisposable
    {
        public static readonly DependencyProperty BlurProperty = DependencyProperty.Register(
            nameof(Blur),
            typeof(double),
            typeof(BlurredImageBrush),
            new PropertyMetadata(default(double), OnPropertyChanged));
        /// <summary>
        ///     Identifies the <see cref="Source" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(ImageSource), // We use ImageSource type so XAML engine will automatically construct proper object from String.
            typeof(BlurredImageBrush),
            new PropertyMetadata(null, OnPropertyChanged));

        /// <summary>
        ///     Identifies the <see cref="Stretch" /> dependency property.
        ///     Requires 16299 or higher for modes other than None.
        /// </summary>
        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
            nameof(Stretch),
            typeof(Stretch),
            typeof(BlurredImageBrush),
            new PropertyMetadata(Stretch.None, OnPropertyChanged));

        private LoadedImageSurface _surface;
        private CompositionSurfaceBrush _surfaceBrush;

        /// <summary>
        ///     Gets or sets the <see cref="BitmapImage" /> source of the image to composite.
        /// </summary>
        public ImageSource Source
        {
            get => (ImageSource)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }


        /// <summary>
        ///     Gets or sets how to stretch the image within the brush.
        /// </summary>
        public Stretch Stretch
        {
            get => (Stretch)GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        public double Blur
        {
            get => (double)GetValue(BlurProperty);
            set => SetValue(BlurProperty, value);
        }
        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var brush = (BlurredImageBrush)d;
            brush.OnDisconnected();
            brush.OnConnected();
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
                _surfaceBrush.Stretch = CompositionStretchFromStretch(Stretch);


                var compositor = App.MWindow.Compositor;
                var blurEffect = new GaussianBlurEffect
                {
                    Name = "blur",
                    BlurAmount = (float)Blur,
                    BorderMode = EffectBorderMode.Hard,
                    Optimization = EffectOptimization.Balanced,
                    Source = new CompositionEffectSourceParameter("Source")
                };


                // var opacityMaskSurface = LoadedImageSurface.StartLoadFromUri(new Uri(Path.Combine(AppContext.BaseDirectory, "Assets/amb.png")));
                // var opacityBrush = compositor.CreateSurfaceBrush(opacityMaskSurface);
                // opacityBrush.Stretch = CompositionStretch.Fill;

                // var effect = new AlphaMaskEffect
                // {
                //     Source = blurEffect,
                //     AlphaMask = new CompositionEffectSourceParameter("Mask")
                // };
                var brush = compositor.CreateEffectFactory(
                        blurEffect,
                        new[] { "blur.BlurAmount" })
                    .CreateBrush();
                brush.SetSourceParameter("Source", compositor.CreateBackdropBrush());

                brush.SetSourceParameter("Source", _surfaceBrush);
                //brush.SetSourceParameter("Mask", opacityBrush);
                CompositionBrush = brush;
               // _surface.LoadCompleted += SurfaceOnLoadCompleted;
            }
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
                //_surface.LoadCompleted -= SurfaceOnLoadCompleted;
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

            GC.Collect();
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

        public void Dispose()
        {
            _surface?.Dispose();
            _surfaceBrush?.Dispose();
        }
    }
}
