using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Wavee.Metadata.Common;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.View.Artist.Views
{
    public sealed partial class SpotifyPreReleaseControl : UserControl
    {
        public static readonly DependencyProperty ImagesProperty = DependencyProperty.Register(nameof(Images), typeof(ICoverImage[]), typeof(SpotifyPreReleaseControl), new PropertyMetadata(default(ICoverImage[])));
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(SpotifyPreReleaseControl), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty ReleaseDateProperty = DependencyProperty.Register(nameof(ReleaseDate), typeof(DateTimeOffset), typeof(SpotifyPreReleaseControl), new PropertyMetadata(default(DateTimeOffset)));

        public SpotifyPreReleaseControl()
        {
            this.InitializeComponent();
        }

        public ICoverImage[] Images
        {
            get => (ICoverImage[])GetValue(ImagesProperty);
            set => SetValue(ImagesProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public DateTimeOffset ReleaseDate
        {
            get => (DateTimeOffset)GetValue(ReleaseDateProperty);
            set => SetValue(ReleaseDateProperty, value);
        }

        public string GetRealReleaseString(DateTimeOffset dateTimeOffset)
        {
            var toLocalDate = dateTimeOffset.ToLocalTime();
            return $"{toLocalDate.Day} {toLocalDate:MMMM} at {toLocalDate:t} (Local time)";
        }

        public string GetImage(ICoverImage[] coverImages)
        {
            if(coverImages is null) return string.Empty;
            if (coverImages.Length > 0)
            {
                return coverImages[0].Url;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
