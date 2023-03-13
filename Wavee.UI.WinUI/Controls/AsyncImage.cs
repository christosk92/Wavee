using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Org.BouncyCastle.Utilities;
using TagLib;
using Image = Microsoft.UI.Xaml.Controls.Image;

namespace Wavee.UI.WinUI.Controls;

public class AsyncImage : DependencyObject
{
    // private static readonly HttpClient httpClient = new HttpClient();
    //
    // public static readonly StyledProperty<string?> AsyncSourceProperty =
    //     AvaloniaProperty.Register<AsyncImage, string?>(nameof(AsyncSource));
    //
    // public static readonly StyledProperty<string?> FallbackSourceProperty =
    //     AvaloniaProperty.Register<AsyncImage, string?>(nameof(FallbackSource));
    //
    // public static readonly StyledProperty<bool> IsImageLoadedProperty =
    //     AvaloniaProperty.Register<AsyncImage, bool>(nameof(IsImageLoaded));
    //
    // public static readonly DirectProperty<AsyncImage, BitmapInterpolationMode> InterpolationModeProperty =
    //     AvaloniaProperty.RegisterDirect<AsyncImage, BitmapInterpolationMode>(nameof(InterpolationMode), i => i.interpolationMode, (i, v) => i.interpolationMode = v);


    public static readonly DependencyProperty AsyncSourceProperty =
        DependencyProperty.RegisterAttached(
            "AsyncSource",
            typeof(object),
            typeof(AsyncImage),
            new PropertyMetadata(null, PropertyChangedCallback)
        );

    public static readonly DependencyProperty DescaletoHeightProperty =
        DependencyProperty.RegisterAttached(
            "DescaletoHeight",
            typeof(int),
            typeof(AsyncImage),
            new PropertyMetadata(int.MaxValue, PropertyChangedCallback)
        );


    public static void SetAsyncSource(Image element, object value)
    {
        element.SetValue(AsyncSourceProperty, value);
    }

    public static object? GetAsyncSource(Image element)
    {
        return (object)element.GetValue(AsyncSourceProperty);
    }


    public static void SetDescaleToHeight(Image element, int value)
    {
        element.SetValue(DescaletoHeightProperty, value);
    }

    public static int GetDescaleToHeight(Image element)
    {
        return (int)element.GetValue(DescaletoHeightProperty);
    }

    private static async void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Image image)
        {
            if (e.NewValue != null)
            {
                if (GetAsyncSource(image) is { } img)
                {
                    image.Source = await GetImageFromSource(img, GetDescaleToHeight(image));
                }
            }
            else
            {
                image.Source = null;
            }
        }
    }

    // private BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.;
    // private string? currentSource;


    // public bool IsImageLoaded
    // {
    //     get => GetValue(IsImageLoadedProperty);
    //     set => SetValue(IsImageLoadedProperty, value);
    // }


    // protected override async void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
    // {
    //     base.OnPropertyChanged(change);
    //
    //     if (change.Property == SourceProperty)
    //     {
    //         IsImageLoaded = change.NewValue.HasValue && change.NewValue.Value is not null;
    //     }
    //     else if (change.Property == AsyncSourceProperty
    //         || change.Property == FallbackSourceProperty
    //         || change.Property == IsEnabledProperty
    //         || change.Property == InterpolationModeProperty)
    //     {
    //         Source = null;
    //
    //         if (!IsEnabled)
    //         {
    //             return;
    //         }
    //
    //         var height = Height;
    //
    //         var sourcePath = AsyncSource ?? FallbackSource;
    //         if (currentSource == sourcePath)
    //         {
    //             return;
    //         }
    //
    //         if (sourcePath == null)
    //         {
    //             Source = null;
    //             return;
    //         }
    //
    //         if (await GetImageFromSource(sourcePath, height) is IImage normalSource)
    //         {
    //             Source = normalSource;
    //             currentSource = sourcePath;
    //         }
    //         else if (FallbackSource != null && FallbackSource != sourcePath
    //             && await GetImageFromSource(FallbackSource, height) is IImage fallbackSource)
    //         {
    //             Source = fallbackSource;
    //             currentSource = FallbackSource;
    //         }
    //     }
    // }

    private static HttpClient httpClient = new HttpClient();

    private static async Task<ImageSource?> GetImageFromSource(object source, int rescaleToHeight = int.MaxValue)
    {
        Stream? imageStream = null;

        try
        {
            switch (source)
            {
                case string sourcePath when sourcePath.StartsWith("http"):
                    {
                        var response = await httpClient.GetAsync(sourcePath, HttpCompletionOption.ResponseContentRead);
                        imageStream = await response.Content.ReadAsStreamAsync();
                        break;
                    }
                case string sourcePath:
                    imageStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
                    break;
                // case ByteVector bytes:
                //     imageStream = ResizeImage(bytes.Data, 300, 300);
                //     imageStream.Position = 0;
                //     //await imageStream.WriteAsync(bytes.Data);
                //     //imageStream.Position = 0;
                //     break;
            }

            if (imageStream != null)
            {
                if (rescaleToHeight == int.MaxValue)
                {
                    var imageSource = new BitmapImage
                    {
                        DecodePixelWidth = 150,
                        DecodePixelHeight = 150
                    };
                    var rnd = imageStream.AsRandomAccessStream();
                    await imageSource.SetSourceAsync(rnd);
                    return imageSource;
                }
                else
                {
                    var imageSource = new BitmapImage
                    {
                        DecodePixelHeight = rescaleToHeight,
                        DecodePixelWidth = rescaleToHeight,
                    };
                    var rnd = imageStream.AsRandomAccessStream();
                    await imageSource.SetSourceAsync(rnd);
                    return imageSource;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            // if (imageStream != null) await imageStream.DisposeAsync();
            //Log.Logger.Warning(ex, "Image reading or resizing failed.");
            return null;
        }
    }


}