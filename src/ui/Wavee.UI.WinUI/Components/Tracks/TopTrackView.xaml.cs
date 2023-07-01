using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Wavee.Metadata.Artist;
using Duration = LanguageExt.Duration;
using System;
using System.Linq;
using Wavee.Metadata.Common;

namespace Wavee.UI.WinUI.Components.Tracks
{
    public sealed partial class TopTrackView : UserControl
    {
        public static readonly DependencyProperty NumberProperty = DependencyProperty.Register(nameof(Number), typeof(ushort), typeof(AlbumTrackView), new PropertyMetadata(default(ushort)));
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(AlbumTrackView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty PlaycountProperty = DependencyProperty.Register(nameof(Playcount), typeof(Option<ulong>), typeof(AlbumTrackView), new PropertyMetadata(default(Option<ulong>)));
        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(nameof(Duration), typeof(TimeSpan), typeof(AlbumTrackView), new PropertyMetadata(default(TimeSpan)));
        public static readonly DependencyProperty ArtistsProperty = DependencyProperty.Register(nameof(Artists), typeof(ITrackArtist[]), typeof(TopTrackView), new PropertyMetadata(default(ITrackArtist[])));
        public static readonly DependencyProperty ImagesProperty = DependencyProperty.Register(nameof(Images), typeof(ICoverImage[]), typeof(TopTrackView), new PropertyMetadata(default(ICoverImage[])));
        public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(string), typeof(TopTrackView), new PropertyMetadata(default(string)));

        public TopTrackView()
        {
            this.InitializeComponent();
        }
        public ushort Number
        {
            get => (ushort)GetValue(NumberProperty);
            set => SetValue(NumberProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public Option<ulong> Playcount
        {
            get => (Option<ulong>)GetValue(PlaycountProperty);
            set => SetValue(PlaycountProperty, value);
        }

        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public ITrackArtist[] Artists
        {
            get => (ITrackArtist[])GetValue(ArtistsProperty);
            set => SetValue(ArtistsProperty, value);
        }

        public ICoverImage[] Images
        {
            get => (ICoverImage[])GetValue(ImagesProperty);
            set => SetValue(ImagesProperty, value);
        }

        public string Id
        {
            get => (string)GetValue(IdProperty);
            set => SetValue(IdProperty, value);
        }


        public string FormatPlaycount(Option<ulong> ulongs)
        {
            if (ulongs.IsNone) return "< 1,000";
            var x = ulongs.IfNone(0);
            if (x < 1000) return x.ToString();

            //1 million -> 1,000,000
            //1 billion -> 1,000,000,000
            return x.ToString("N0");
        }

        public string FormatDuration(TimeSpan timeSpan)
        {
            return timeSpan.ToString(@"mm\:ss");
        }

        public string? GetSmallestImage(ICoverImage[] coverImages)
        {
            if (coverImages is null) return null;
            if (coverImages.Length == 0) return null;

            return coverImages[0].Url;
        }
    }
}
