using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Wavee.Core.Ids;
using Wavee.UI.ViewModels.Artist;
using Wavee.UI.WinUI.Navigation;
using ReactiveUI;
using Eum.Spotify.context;
using LanguageExt.UnsafeValueAccess;
using System.Windows.Input;
using Wavee.UI.ViewModels.Playback;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Artist
{
    public sealed partial class ArtistRootView : UserControl, ICacheablePage, INavigateablePage
    {
        public ArtistRootView()
        {
            this.InitializeComponent();
            ViewModel = new ArtistViewModel();
        }

        public ArtistViewModel ViewModel { get; }
        private void ArtistPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var newSize = (sender as FrameworkElement)?.ActualHeight ?? 0;
            //ratio is around 1:1, so 1/2
            if (!string.IsNullOrEmpty(HeaderImage.Source))
            {
                var topHeight = newSize * 0.5;
                topHeight = Math.Min(topHeight, 550);
                ImageT.Height = topHeight;
            }
            else
            {
                //else its only 1/4th
                var topHeight = newSize * 0.25;
                topHeight = Math.Min(topHeight, 550);
                ImageT.Height = topHeight;
            }
        }

        public bool ShouldKeepInCache(int currentDepth)
        {
            return currentDepth <= 1;
        }

        public void RemovedFromCache()
        {
            ViewModel.Destroy();
        }

        public async void NavigatedTo(object parameter)
        {
            if (parameter is not AudioId artistId)
            {
                return;
            }

            ViewModel.Create(artistId);

            var artist = await FetchArtist(artistId);
            ArtistNameBlock.Text = artist.Name;
            HeaderImage.Source = artist.HeaderImage;
            var r = artist.MonthlyListeners.ToString("N0");
            MonthlyListenersBlock.Text = $"{r} monthly listeners";
            MetadataPnale.Visibility = Visibility.Visible;
            ShowPanelAnim.Start();
            if (!string.IsNullOrEmpty(artist.ProfilePicture))
            {
                SecondPersonPicture.ProfilePicture = new BitmapImage(new Uri(artist.ProfilePicture));
            }
            else
            {
                SecondPersonPicture.DisplayName = artist.Name;
            }

            if (string.IsNullOrEmpty(artist.HeaderImage))
            {
                //show picture
                HeaderImage.Visibility = Visibility.Collapsed;
                AlternativeArtistImage.Visibility = Visibility.Visible;
                if (!string.IsNullOrEmpty(artist.ProfilePicture))
                {
                    AlternativeArtistImage.ProfilePicture = SecondPersonPicture.ProfilePicture;
                }
                else
                {
                    AlternativeArtistImage.DisplayName = SecondPersonPicture.DisplayName;
                }
            }

            //ArtistPage_OnSizeChanged
            this.ArtistPage_OnSizeChanged(this, null);
        }

        private async Task<ArtistView> FetchArtist(AudioId artistId)
        {
            const string fetch_uri = "hm://artist/v1/{0}/desktop?format=json&catalogue=premium&locale={1}&cat=1";
            var url = string.Format(fetch_uri, artistId.ToBase62(), "en");

            var aff =
                from mercuryClient in SpotifyView.Mercury
                from response in mercuryClient.Get(url, CancellationToken.None).ToAff()
                select response;
            var result = await aff.Run();
            var r = result.ThrowIfFail();
            using var jsonDoc = JsonDocument.Parse(r.Payload);
            var info = jsonDoc.RootElement.GetProperty("info");
            var name = info.GetProperty("name").GetString();
            var headerImage = jsonDoc.RootElement.TryGetProperty("header_image", out var hd)
                ? hd.GetProperty("image")
                    .GetString()
                : null;
            string profilePic = null;
            if (info.TryGetProperty("portraits", out var profil))
            {
                using var profilePics = profil.EnumerateArray();
                profilePic = profilePics.First().GetProperty("uri").GetString();
            }

            var monthlyListeners = jsonDoc.RootElement.TryGetProperty("monthly_listeners", out var mnl)
                ? (mnl.TryGetProperty("listener_count", out var lc) ? lc.GetUInt64() : 0)
                : 0;

            var topTracks = new List<ArtistTopTrackView>();
            if (jsonDoc.RootElement.GetProperty("top_tracks")
                .TryGetProperty("tracks", out var toptr))
            {
                using var topTracksArr = toptr.EnumerateArray();
                int index = 0;
                var playcommandFortoptracks = ReactiveCommand.Create<AudioId, Unit>(x =>
                {
                    var ctx = new PlayContextStruct(
                        ContextId: artistId.ToString(),
                        Index: topTracks.FindIndex(c => c.Id == x),
                        ContextUrl: $"context://{artistId.ToString()}",
                        TrackId: x,
                        NextPages: Option<IEnumerable<ContextPage>>.None,
                        PageIndex: Option<int>.None,
                        Metadata: LanguageExt.HashMap<string, string>.Empty
                    );
                    PlaybackViewModel.Instance.PlayCommand.Execute(ctx);
                    return default;
                });
                foreach (var topTrack in topTracksArr)
                {
                    var release = topTrack.GetProperty("release");
                    var releaseName = release.GetProperty("name").GetString();
                    var releaseUri = release.GetProperty("uri").GetString();
                    var releaseImage = release.GetProperty("cover").GetProperty("uri").GetString();
                    var track = new ArtistTopTrackView
                    {
                        Uri = topTrack.GetProperty("uri")
                            .GetString(),
                        Playcount = topTrack.GetProperty("playcount")
                            is
                        {
                            ValueKind: JsonValueKind.Number
                        } e
                            ? e.GetUInt64()
                            : Option<ulong>.None,
                        ReleaseName = releaseName,
                        ReleaseUri = releaseUri,
                        ReleaseImage = releaseImage,
                        Title = topTrack.GetProperty("name")
                            .GetString(),
                        Id = AudioId.FromUri(topTrack.GetProperty("uri")
                            .GetString()),
                        Index = index++,
                        PlayCommand = playcommandFortoptracks,
                    };
                    topTracks.Add(track);
                }
            }

            var releases = jsonDoc.RootElement.GetProperty("releases");

            static void GetView(JsonElement releases,
                string key,
                bool canSwitchViews,
                List<ArtistDiscographyGroupView> output,
                AudioId artistid)
            {
                var albums = releases.GetProperty(key);
                var totalAlbums = albums.GetProperty("total_count").GetInt32();
                if (totalAlbums > 0)
                {
                    var rl = albums.GetProperty("releases");
                    using var albumReleases = rl.EnumerateArray();
                    var albumsView = new List<ArtistDiscographyView>(rl.GetArrayLength());

                    foreach (var release in albumReleases)
                    {
                        var releaseUri = release.GetProperty("uri").GetString();
                        var releaseName = release.GetProperty("name").GetString();
                        var releaseImage = release.GetProperty("cover").GetProperty("uri").GetString();
                        var year = release.GetProperty("year").GetUInt16();

                        var tracks = new List<ArtistDiscographyTrack>();
                        var playCommandForContext = ReactiveCommand.Create<AudioId, Unit>(x =>
                        {
                            //pages are for artists are like:
                            //hm://artistplaycontext/v1/page/spotify/album/{albumId}/km
                            var currentId = AudioId.FromUri(releaseUri);
                            // var pageUrl = $"hm://artistplaycontext/v1/page/spotify/album/{currentId}/km";
                            // //next pages:
                            // var nextPages = new RepeatedField<ContextPage>
                            // {
                            //     new ContextPage
                            //     {
                            //         PageUrl = pageUrl
                            //     }
                            // };
                            var nextPages =
                                output.SelectMany(y => y.Views)
                                    .SkipWhile(z => z.Id != currentId).Select(albumView =>
                                        $"hm://artistplaycontext/v1/page/spotify/album/{albumView.Id.ToBase62()}/km")
                                    .Select(nextPageUrl => new ContextPage { PageUrl = nextPageUrl });

                            var index = tracks.FindIndex(c => c.Id == x);
                            PlaybackViewModel.Instance.PlayCommand.Execute(new PlayContextStruct(
                                ContextId: artistid.ToString(),
                                Index: index,
                                TrackId: x,
                                ContextUrl: Option<string>.None,
                                NextPages: Option<IEnumerable<ContextPage>>.Some(nextPages),
                                PageIndex: 0,
                                Metadata: HashMap.empty<string, string>()));
                            return default;
                        });

                        if (release.TryGetProperty("discs", out var discs))
                        {
                            using var discsArr = discs.EnumerateArray();
                            foreach (var disc in discsArr)
                            {
                                using var tracksInDisc = disc.GetProperty("tracks").EnumerateArray();
                                foreach (var track in tracksInDisc)
                                {
                                    tracks.Add(new ArtistDiscographyTrack
                                    {
                                        PlayCommand = playCommandForContext,
                                        Playcount = track.GetProperty("playcount")
                                            is
                                        {
                                            ValueKind: JsonValueKind.Number
                                        } e
                                            ? e.GetUInt64()
                                            : Option<ulong>.None,
                                        Title = track.GetProperty("name")
                                            .GetString(),
                                        Number = track.GetProperty("number")
                                            .GetUInt16(),
                                        Id = AudioId.FromUri(track.GetProperty("uri").GetString()),
                                        Duration = TimeSpan.FromMilliseconds(track.GetProperty("duration").GetUInt32()),
                                        IsExplicit = track.GetProperty("explicit").GetBoolean()
                                    });
                                }
                            }
                        }
                        else
                        {
                            var tracksCount = release.GetProperty("track_count").GetUInt16();
                            tracks.AddRange(Enumerable.Range(0, tracksCount)
                                .Select(c => new ArtistDiscographyTrack
                                {
                                    PlayCommand = playCommandForContext,
                                    Playcount = Option<ulong>.None,
                                    Title = null,
                                    Number = (ushort)(c + 1),
                                    Id = default,
                                    Duration = default,
                                    IsExplicit = false
                                }));
                        }

                        var pluralModifier = tracks.Count > 1 ? "tracks" : "track";
                        albumsView.Add(new ArtistDiscographyView
                        {
                            Id = AudioId.FromUri(releaseUri),
                            Title = releaseName,
                            Image = releaseImage,
                            Tracks = new ArtistDiscographyTracksHolder
                            {
                                Tracks = tracks,
                                AlbumId = AudioId.FromUri(releaseUri)
                            },
                            ReleaseDateAsStr = $"{year.ToString()} - {tracks.Count} {pluralModifier}"
                        });
                    }

                    var newGroup = new ArtistDiscographyGroupView
                    {
                        GroupName = FirstCharToUpper(key),
                        Views = albumsView,
                        CanSwitchTemplates = canSwitchViews
                    };

                    output.Add(newGroup);
                }
            }


            var res = new List<ArtistDiscographyGroupView>(3);
            GetView(releases, "albums", true, res, artistId);
            GetView(releases, "singles", true, res, artistId);
            GetView(releases, "compilations", false, res, artistId);



            return new ArtistView(
                Name: name,
                ProfilePicture: profilePic,
                HeaderImage: headerImage,
                MonthlyListeners: monthlyListeners,
                TopTracks: topTracks,
                Discography: res
            );
        }

        private static string FirstCharToUpper(string key)
        {
            ReadOnlySpan<char> sliced = key;
            return $"{char.ToUpper(sliced[0])}{sliced[1..]}";
        }

        private readonly record struct ArtistView(string Name, string ProfilePicture, string HeaderImage, ulong MonthlyListeners,
            IReadOnlyCollection<ArtistTopTrackView> TopTracks,
            IReadOnlyCollection<ArtistDiscographyGroupView> Discography);

        private async void ScrollViewer_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            var frac = ((ScrollViewer)sender).VerticalOffset / ImageT.Height;
            var progress = Math.Clamp(frac, 0, 1);
            HeaderImage.BlurValue = progress * 20;

            var exponential = Math.Pow(progress, 2);
            var opacity = 1 - exponential;
            HeaderImage.Opacity = opacity;

            //at around 75%, we should start transforming the header into a floating one
            const double threshold = 0.75;
            if (progress >= 0.75)
            {
                BaseTrans.Source = MetadataPnale;
                BaseTrans.Target = SecondMetadataPanel;
                await BaseTrans.StartAsync();
            }
            else
            {
                BaseTrans.Source = SecondMetadataPanel;
                BaseTrans.Target = MetadataPnale;
                await BaseTrans.StartAsync();
            }
        }

        private void ImageT_OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {

        }

        public object FollowingToContent(bool b)
        {
            var stckp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };
            if (b)
            {
                stckp.Children.Add(new FontIcon
                {
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                    Glyph = "\uE8F8"
                });
                stckp.Children.Add(new TextBlock
                {
                    Text = "Unfollow"
                });
            }
            else
            {
                stckp.Children.Add(new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"), Glyph = "\uE8FA" });
                stckp.Children.Add(new TextBlock { Text = "Follow" });
            }

            return stckp;
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }

    public class ArtistDiscographyGroupView
    {
        public required string GroupName { get; set; }
        public required List<ArtistDiscographyView> Views { get; set; }
        public required bool CanSwitchTemplates { get; set; }
    }
    public class ArtistDiscographyView
    {
        public string Title { get; set; }
        public string Image { get; set; }
        public AudioId Id { get; set; }
        public ArtistDiscographyTracksHolder Tracks { get; set; }
        public string ReleaseDateAsStr { get; set; }
    }

    public class ArtistDiscographyTracksHolder
    {
        public List<ArtistDiscographyTrack> Tracks { get; set; }
        public AudioId AlbumId { get; set; }
    }
    public class ArtistDiscographyTrack
    {
        public Option<ulong> Playcount { get; set; }
        public string Title { get; set; }
        public ushort Number { get; set; }
        public List<SpotifyAlbumArtistView> Artists { get; set; }
        public bool IsLoaded => !string.IsNullOrEmpty(Title);
        public AudioId Id { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsExplicit { get; set; }
        public required ICommand PlayCommand { get; set; }

        public ushort MinusOne(ushort v)
        {
            return (ushort)(v - 1);
        }

        public bool Negate(bool b)
        {
            return !b;
        }

        public string FormatPlaycount(Option<ulong> playcount)
        {
            return playcount.IsSome
                ? playcount.ValueUnsafe().ToString("N0")
                : "< 1,000";
        }

        public string FormatTimestamp(TimeSpan timeSpan)
        {
            return timeSpan.ToString(@"mm\:ss");
        }
    }

    public class ArtistTopTrackView
    {
        public required string Uri { get; set; }
        public required Option<ulong> Playcount { get; set; }
        public required string ReleaseImage { get; set; }
        public required string ReleaseName { get; set; }
        public required string ReleaseUri { get; set; }
        public required string Title { get; set; }
        public required AudioId Id { get; set; }
        public required int Index { get; set; }
        public required ICommand PlayCommand { get; set; }

        public string FormatPlaycount(Option<ulong> playcount)
        {
            return playcount.IsSome
                ? playcount.ValueUnsafe().ToString("N0")
                : "< 1,000";
        }
    }
}
public class SpotifyAlbumArtistView
{
    public string Name { get; set; }
    public AudioId Id { get; set; }
    public string Image { get; set; }
}