using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using Wavee.Metadata.Artist;
using Wavee.Metadata.Common;
using Wavee.UI.WinUI.Models;


namespace Wavee.UI.WinUI.View.Artist.Views.Discography
{
    public sealed partial class ArtistDiscographyTrackListView : UserControl
    {
        public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(string), typeof(ArtistDiscographyTrackListView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ArtistDiscographyTrackListView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty ImagesProperty = DependencyProperty.Register(nameof(Images), typeof(ICoverImage[]), typeof(ArtistDiscographyTrackListView), new PropertyMetadata(default(ICoverImage[])));
        public static readonly DependencyProperty ReleaseDateProperty = DependencyProperty.Register(nameof(ReleaseDate), typeof(DiscographyReleaseDate), typeof(ArtistDiscographyTrackListView), new PropertyMetadata(default(DiscographyReleaseDate)));
        public static readonly DependencyProperty TrackCountProperty = DependencyProperty.Register(nameof(TrackCount), typeof(ushort), typeof(ArtistDiscographyTrackListView), new PropertyMetadata(default(ushort)));

        public ArtistDiscographyTrackListView()
        {
            this.InitializeComponent();
        }


        public string Id
        {
            get => (string)GetValue(IdProperty);
            set
            {
                SetValue(IdProperty, value);
                _ = LoadTracks(value);
            }
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public ICoverImage[] Images
        {
            get => (ICoverImage[])GetValue(ImagesProperty);
            set => SetValue(ImagesProperty, value);
        }

        public DiscographyReleaseDate ReleaseDate
        {
            get => (DiscographyReleaseDate)GetValue(ReleaseDateProperty);
            set => SetValue(ReleaseDateProperty, value);
        }

        public ushort TrackCount
        {
            get => (ushort)GetValue(TrackCountProperty);
            set
            {
                SetValue(TrackCountProperty, value);
                //create dummy tracks (shimmer)
                TracksS.ItemsSource = Enumerable.Range(0, value).Select(_ => new ShimmerTrackModel());
            }
        }
        private async Task LoadTracks(string id)
        {

        }
        public Uri? GetImage(ICoverImage[] images)
        {
            if (images is null) return null;
            if (images.Length > 0)
            {
                //get around 300 x 300 image
                const int targetSize = 300;
                var head = images
                    .OrderBy(x => Math.Abs(x.Height.IfNone(0) - targetSize))
                    .HeadOrNone()
                    .Map(x => x.Url);
                if (head.IsNone)
                    return null;
                return new Uri(head.IfNone(string.Empty));
            }

            return null;
        }
    }
}
