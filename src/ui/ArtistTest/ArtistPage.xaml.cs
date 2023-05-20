using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Labs.WinUI;
using CommunityToolkit.WinUI.UI.Animations;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using static LanguageExt.Prelude;
namespace ArtistTest;

public partial class ArtistPage
{
    public ArtistPage()
    {
        InitializeComponent();

        using var jsonDoc = JsonDocument.Parse(Artists.STARSET);
        var info = jsonDoc.RootElement.GetProperty("info");
        var name = info.GetProperty("name").GetString();
        var headerImage = jsonDoc.RootElement.GetProperty("header_image")
            .GetProperty("image")
            .GetString();

        var monthlyListeners = jsonDoc.RootElement.GetProperty("monthly_listeners")
            .GetProperty("listener_count")
            .GetUInt64();

        using var topTracksArr = jsonDoc.RootElement.GetProperty("top_tracks")
            .GetProperty("tracks").EnumerateArray();
        var topTracks = LanguageExt.Seq<ArtistTopTrackView>.Empty;
        foreach (var topTrack in topTracksArr)
        {
            var release = topTrack.GetProperty("release");
            var releaseName = release.GetProperty("name").GetString();
            var releaseUri = release.GetProperty("uri").GetString();
            var releaseImage = release.GetProperty("cover").GetProperty("uri").GetString();
            var track = new ArtistTopTrackView
            {
                Uri = topTrack.GetProperty("uri").GetString(),
                Playcount = topTrack.GetProperty("playcount")
                    is { ValueKind: JsonValueKind.Number } e
                ? e.GetUInt64() : Option<ulong>.None,
                ReleaseName = releaseName,
                ReleaseUri = releaseUri,
                ReleaseImage = releaseImage,
                Title = topTrack.GetProperty("name").GetString(),
            };
            topTracks = topTracks.Add(track);
        }

        var releases = jsonDoc.RootElement.GetProperty("releases");

        static void GetView(JsonElement releases,
            string key,
            bool canSwitchViews, List<ArtistDiscographyGroupView> output)
        {
            var albums = releases.GetProperty(key);
            var totalAlbums = albums.GetProperty("total_count").GetInt32();
            if (totalAlbums > 0)
            {
                using var albumReleases = albums.GetProperty("releases").EnumerateArray();
                var albumsView = LanguageExt.Seq<ArtistDiscographyView>.Empty;

                foreach (var release in albumReleases)
                {
                    var releaseUri = release.GetProperty("uri").GetString();
                    var releaseName = release.GetProperty("name").GetString();
                    var releaseImage = release.GetProperty("cover").GetProperty("uri").GetString();
                    var year = release.GetProperty("year").GetUInt16();

                    Seq<ArtistDiscographyTrack> tracks = LanguageExt.Seq<ArtistDiscographyTrack>.Empty;
                    if (release.TryGetProperty("discs", out var discs))
                    {
                        using var discsArr = discs.EnumerateArray();
                        foreach (var disc in discsArr)
                        {
                            using var tracksInDisc = disc.GetProperty("tracks").EnumerateArray();
                            foreach (var track in tracksInDisc)
                            {
                                tracks = tracks.Add(new ArtistDiscographyTrack
                                {
                                    Playcount = track.GetProperty("playcount")
                                        is { ValueKind: JsonValueKind.Number } e
                                        ? e.GetUInt64()
                                        : Option<ulong>.None,
                                    Title = track.GetProperty("name").GetString(),
                                    Number = track.GetProperty("number")
                                        .GetUInt16()
                                });
                            }
                        }
                    }
                    else
                    {
                        var tracksCount = release.GetProperty("track_count").GetUInt16();
                        tracks = Enumerable.Range(0, tracksCount)
                            .Select(c => new ArtistDiscographyTrack
                            {
                                Playcount = Option<ulong>.None,
                                Title = null,
                                Number = (ushort)(c + 1)
                            }).ToSeq();
                    }

                    var pluralModifier = tracks.Length > 1 ? "tracks" : "track";
                    albumsView = albumsView.Add(new ArtistDiscographyView
                    {
                        Id = releaseUri,
                        Title = releaseName,
                        Image = releaseImage,
                        Tracks = tracks,
                        ReleaseDateAsStr = $"{year.ToString()} - {tracks.Length} {pluralModifier}"
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


        var result = new List<ArtistDiscographyGroupView>(3);
        GetView(releases, "albums", true, result);
        GetView(releases, "singles", true, result);
        GetView(releases, "compilations", false, result);


        _artist = new ArtistView(
            name: name,
            headerImage: headerImage,
            monthlyListeners: monthlyListeners,
            topTracks: topTracks,
            result.ToSeq()
        );
        MetadataPnale.Visibility = Visibility.Visible;
        _artistFetched.SetResult();
    }

    private static string FirstCharToUpper(string key)
    {
        ReadOnlySpan<char> sliced = key;
        return $"{char.ToUpper(sliced[0])}{sliced[1..]}";
    }

    public ArtistView Artist => _artist;

    private void ArtistPage_OnLoaded(object sender, RoutedEventArgs e)
    {

    }

    private void ArtistPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        //ratio is around 1:1, so 1/2
        var topHeight = e.NewSize.Height * 0.5;
        ImageT.Height = topHeight;
    }

    private async void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        MetadataPnale.Visibility = Visibility.Collapsed;
        await Img.FadeIn();
        await Task.Delay(10);
        MetadataPnale.Visibility = Visibility.Visible;
    }

    private void Img_OnLoaded(object sender, RoutedEventArgs e)
    {
    }

    public string FormatListeners(ulong @ulong)
    {
        //we want the result like 1,212,212;
        var r = @ulong.ToString("N0");
        return $"{r} monthly listeners";
    }

    private ArtistOverview? _overview;
    private ArtistConcerts? _concerts;
    private ArtistAbout? _about;

    private TaskCompletionSource _artistFetched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    private ArtistView _artist;

    private async void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        await _artistFetched.Task;
        var selectedItems = e.AddedItems;
        if (selectedItems.Count > 0)
        {
            var item = (SegmentedItem)selectedItems[0];
            var content = item.Tag switch
            {
                "overview" => _overview ??= new ArtistOverview(ref _artist),
                "concerts" => _concerts ??= new ArtistConcerts(ref _artist),
                "about" => (_about ??= new ArtistAbout(ref _artist)) as UIElement,
                _ => throw new ArgumentOutOfRangeException()
            };
            MainContent.Content = content;
        }
    }

    private void ScrollViewer_OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        var frac = ((ScrollViewer)sender).VerticalOffset / ImageT.Height;
        var progress = Math.Clamp(frac, 0, 1);
        Img.BlurValue = progress * 20;

        var exponential = Math.Pow(progress, 2);
        var opacity = 1 - exponential;
        Img.Opacity = opacity;

        //at around 75%, we should start transforming the header into a floating one
        const double threshold = 0.75;
        if (progress >= 0.75)
        {

        }
        else
        {

        }
    }
}

public readonly struct ArtistView
{
    public string Name { get; }
    public string HeaderImage { get; }
    public ulong MonthlyListeners { get; }
    public Seq<ArtistTopTrackView> TopTracks { get; }
    public Seq<ArtistDiscographyGroupView> Discography { get; }

    public ArtistView(string name, string headerImage, ulong monthlyListeners, Seq<ArtistTopTrackView> topTracks, Seq<ArtistDiscographyGroupView> discography)
    {
        Name = name;
        HeaderImage = headerImage;
        MonthlyListeners = monthlyListeners;
        TopTracks = topTracks;
        Discography = discography;
    }
}

public readonly struct ArtistDiscographyGroupView
{
    public required string GroupName { get; init; }
    public required Seq<ArtistDiscographyView> Views { get; init; }
    public required bool CanSwitchTemplates { get; init; }
}

public readonly struct ArtistDiscographyView
{
    public required string Title { get; init; }
    public required string Image { get; init; }
    public required string Id { get; init; }
    public Seq<ArtistDiscographyTrack> Tracks { get; init; }
    public required string ReleaseDateAsStr { get; init; }
}
public readonly struct ArtistDiscographyTrack
{
    public required Option<ulong> Playcount { get; init; }
    public required string Title { get; init; }
    public required ushort Number { get; init; }
    public bool IsLoaded => !string.IsNullOrEmpty(Title);
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
            ? playcount.ValueUnsafe().ToString("N")
            : "< 1,000";
    }
}

public readonly struct ArtistTopTrackView
{
    public required string Uri { get; init; }
    public required Option<ulong> Playcount { get; init; }
    public required string ReleaseImage { get; init; }
    public required string ReleaseName { get; init; }
    public required string ReleaseUri { get; init; }
    public required string Title { get; init; }

    public string FormatPlaycount(Option<ulong> playcount)
    {
        return playcount.IsSome
            ? playcount.ValueUnsafe().ToString("N")
            : "< 1,000";
    }
}