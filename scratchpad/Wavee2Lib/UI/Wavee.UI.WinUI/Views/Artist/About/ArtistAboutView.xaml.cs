using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CommunityToolkit.Labs.WinUI;
using FontAwesome6;
using LanguageExt;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.UI.Client;
using Wavee.UI.WinUI.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Artist.About
{
    public sealed partial class ArtistAboutView : UserControl
    {
        public ArtistAboutView(string artistId)
        {
            this.InitializeComponent();

            Task.Run(async () =>
             {
                 const string fetch_uri = "hm://creatorabout/v0/artist-insights/{0}?format=json&locale={1}";
                 var url = string.Format(fetch_uri, artistId, "en");
                 var aff =
                     from mercuryClient in SpotifyView.Mercury

                     from response in mercuryClient.Get(url, CancellationToken.None).ToAff()
                     select response;
                 var result = await Task.Run(async () => await aff.Run());
                 var r = result.ThrowIfFail();

                 using var jsonDocument = JsonDocument.Parse(r.Payload);
                 var name = jsonDocument.RootElement.GetProperty("name").GetString();
                 var mainimageUrl = jsonDocument.RootElement.GetProperty("mainImageUrl").GetString();

                 string body = null;
                 Seq<ArtistLink> links = LanguageExt.Seq<ArtistLink>.Empty;
                 if (jsonDocument.RootElement.TryGetProperty("autobiography", out var autoBiography))
                 {
                     body = autoBiography.TryGetProperty("body", out var b) ? b.GetString() : null;
                     links = autoBiography.TryGetProperty("links", out var lk)
                         ? lk.EnumerateObject()
                             .Map(x => new ArtistLink(x.Name, x.Value.GetString())).ToArr().ToSeq()
                         : LanguageExt.Seq<ArtistLink>.Empty;
                 }

                 var biography = jsonDocument.RootElement.TryGetProperty("biography", out var bio)
                     ? bio.GetString()
                     : null;
                 var images = jsonDocument.RootElement.TryGetProperty("images", out var imgs)
                     ? (imgs.EnumerateArray()
                         .Map(x => new Artwork
                         {
                             Uri = x.GetProperty("uri").GetString(),
                             Width = x.GetProperty("width").GetInt32(),
                             Height = x.GetProperty("height").GetInt32()
                         }).ToArr().ToSeq())
                     : LanguageExt.Seq<Artwork>.Empty;
                 var globalChartPosition = jsonDocument.RootElement.GetProperty("globalChartPosition").GetInt32();
                 var monthlyListeners = jsonDocument.RootElement.GetProperty("monthlyListeners").GetUInt64();
                 var monthlyListenersDelta = jsonDocument.RootElement.GetProperty("monthlyListenersDelta").GetInt64();
                 var followers = jsonDocument.RootElement.GetProperty("followerCount").GetUInt64();
                 var followingCount = jsonDocument.RootElement.GetProperty("followingCount").GetUInt32();
                 var city = jsonDocument.RootElement.GetProperty("cities").EnumerateArray()
                     .Map((i, x) => new ArtistCity
                     {
                         Country = x.GetProperty("country")
                             .GetString(),
                         Region = x.GetProperty("region")
                             .GetString(),
                         City = x.GetProperty("city")
                             .GetString(),
                         Listeners = x.GetProperty("listeners")
                             .GetUInt64(),
                         Index = (uint)i
                     }).ToArr().ToSeq();

                 var info = new ArtistAbout
                 {
                     Images = images,
                     GlobalChartPosition = globalChartPosition,
                     MonthlyListeners = monthlyListeners,
                     MonthlyListenersDelta = monthlyListenersDelta,
                     Followers = followers,
                     FollowingCount = followingCount,
                     City = city,
                     Autobiography = body,
                     Links = links,
                     Biography = biography,
                     Name = name,
                     Image = mainimageUrl
                 };

                 this.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                 {
                     Info = info;
                     //(2)|(1/1), so we need 3 images
                     if (info.Images.Count > 2)
                     {
                         LeftImage.Background = new ImageBrush
                         {
                             ImageSource = new BitmapImage(new Uri(info.Images[0].Uri)),
                             Stretch = Stretch.UniformToFill
                         };
                         TopRightImage.Source = new BitmapImage(new Uri(info.Images[1].Uri));
                         BottomRightImage.Source = new BitmapImage(new Uri(info.Images[2].Uri));
                     }
                     else if (info.Images.Count > 1)
                     {
                         LeftImage.Background = new ImageBrush
                         {
                             ImageSource = new BitmapImage(new Uri(info.Images[0].Uri)),

                             Stretch = Stretch.UniformToFill
                         };
                         TopRightImage.Source = new BitmapImage(new Uri(info.Images[1].Uri));
                         //set rowspan to 2 and hide bottom right image
                         //also set the ratio of columns to 1:1
                         BottomRightImage.Visibility = Visibility.Collapsed;
                         Grid.SetRowSpan(TopRightImageGr, 2);

                         LeftGrid.Width = new GridLength(1, GridUnitType.Star);
                         RightGrid.Width = new GridLength(1, GridUnitType.Star);
                     }
                     else if (info.Images.Count > 0)
                     {
                         LeftImage.Background = new ImageBrush
                         {
                             ImageSource = new BitmapImage(new Uri(info.Images[0].Uri)),
                             Stretch = Stretch.UniformToFill
                         };
                         //hide top right and bottom right images
                         //also set the ratio of columns to 1 (fulL), so hide the right grid
                         TopRightImage.Visibility = Visibility.Collapsed;
                         BottomRightImage.Visibility = Visibility.Collapsed;
                         RightGrid.Width = new GridLength(0, GridUnitType.Pixel);
                     }
                     else
                     {
                         //no images. hide gallery
                         Gallery.Visibility = Visibility.Collapsed;
                     }

                     if (!links.Any())
                     {
                         LinksPanel.Visibility = Visibility.Collapsed;
                     }

                     if (!string.IsNullOrEmpty(info.Autobiography))
                     {
                         BiographySegments.Items.Add(new SegmentedItem
                         {
                             Content = "Autobiography",
                             Tag = "auto"
                         });
                     }

                     if (!string.IsNullOrEmpty(info.Biography))
                     {
                         BiographySegments.Items.Add(new SegmentedItem
                         {
                             Content = "Biography",
                             Tag = "biography"
                         });
                     }

                     if (BiographySegments.Items.Count == 1)
                     {
                         BiographySegments.Visibility = Visibility.Collapsed;
                     }

                     if (!BiographySegments.Items.Any())
                     {
                         Biographies.Visibility = Visibility.Collapsed;
                     }

                     PostedByPicture.ProfilePicture = new BitmapImage(new Uri(info.Image));
                     this.Bindings.Update();
                 });
             });
        }

        public void Clear()
        {
            this.Bindings.StopTracking();
            this.Content = new Border();
        }

        public ArtistAbout Info { get; set; }

        private void BiographySegments_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //BiographyContent
            var item = (SegmentedItem)e.AddedItems[0];
            if (item.Tag is "auto")
            {
                var html = new HtmlAgilityPack.HtmlDocument();
                html.LoadHtml(Info.Autobiography);
                BiographyContent.Text = HttpUtility.HtmlDecode(html.DocumentNode.InnerText);
                PostedByArtist.Visibility = Visibility.Visible;
            }
            else if (item.Tag is "biography")
            {
                var html = new HtmlAgilityPack.HtmlDocument();
                html.LoadHtml(Info.Biography);
                BiographyContent.Text = HttpUtility.HtmlDecode(html.DocumentNode.InnerText);
                PostedByArtist.Visibility = Visibility.Collapsed;
            }
        }

        public string FormatMonthlyListeners(ulong val)
        {
            return val.ToString("N0");
        }

        private void GalleryGrid_OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            (sender as UIElement).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Hand));
        }

        private void GalleryGrid_OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            (sender as UIElement).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
        }
    }

    public readonly struct ArtistAbout
    {
        public required string? Autobiography { get; init; }
        public required Seq<ArtistLink> Links { get; init; }
        public required string? Biography { get; init; }
        public required Seq<Artwork> Images { get; init; }
        public required Option<int> GlobalChartPosition { get; init; }
        public required ulong MonthlyListeners { get; init; }
        public required long MonthlyListenersDelta { get; init; }
        public required ulong Followers { get; init; }
        public required uint FollowingCount { get; init; }
        public required Seq<ArtistCity> City { get; init; }
        public required string Name { get; init; }
        public required string Image { get; init; }
    }

    public readonly record struct ArtistLink(string Key, string Ref)
    {
        public EFontAwesomeIcon GetIcon(string s)
        {
            return s switch
            {
                "facebook" => EFontAwesomeIcon.Brands_Facebook,
                "twitter" => EFontAwesomeIcon.Brands_Twitter,
                "instagram" => EFontAwesomeIcon.Brands_Instagram,
                "wikipedia" => EFontAwesomeIcon.Brands_WikipediaW,
                _ => EFontAwesomeIcon.Solid_Link
            };
        }
    }

    public readonly struct ArtistCity
    {
        public required string Country { get; init; }
        public required string Region { get; init; }
        public required string City { get; init; }
        public required ulong Listeners { get; init; }
        public required uint Index { get; init; }

        public string FormatIndex(uint index0)
        {
            return (index0 + 1).ToString("N0");
        }

        public string FormatDisplayName(string s, string s1)
        {
            return $"{s}, {s1}";
        }

        public string FormatListeners(ulong @ulong)
        {
            return @ulong.ToString("N0");
        }
    }

    public readonly struct Artwork
    {
        public required string Uri { get; init; }
        public required int Width { get; init; }
        public required int Height { get; init; }
    }
}
