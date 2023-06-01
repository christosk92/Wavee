
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.ViewModels;
using Wavee.UI.ViewModels.Artist;
using static LanguageExt.Prelude;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;

namespace Wavee.UI.WinUI.Views.Artist.Sections.List;

public partial class ArtistDiscographyLazyTracksView : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ArtistDiscographyLazyTracksView), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(string), typeof(ArtistDiscographyLazyTracksView), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty TracksCountProperty = DependencyProperty.Register(nameof(TracksCount), typeof(ushort), typeof(ArtistDiscographyLazyTracksView), new PropertyMetadata(default(ushort)));
    public static readonly DependencyProperty TracksProperty =
        DependencyProperty.Register(nameof
                (Tracks), typeof(ArtistDiscographyTracksHolder),
        typeof(ArtistDiscographyLazyTracksView),
        new PropertyMetadata(default(ArtistDiscographyTracksHolder)));
    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(AudioId), typeof(ArtistDiscographyLazyTracksView), new PropertyMetadata(default(AudioId)));

    public ArtistDiscographyLazyTracksView()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Image
    {
        get => (string)GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public ushort TracksCount
    {
        get => (ushort)GetValue(TracksCountProperty);
        set => SetValue(TracksCountProperty, value);
    }

    public ArtistDiscographyTracksHolder Tracks
    {
        get
        {
            var tracks = (ArtistDiscographyTracksHolder)GetValue(TracksProperty);
            if (tracks.Tracks.Any(x => !x.IsLoaded))
            {
                //setup a loading task
                Task.Run(async () =>
                {
                    await LoadTracks(tracks, CancellationToken.None);
                });
            }
            return tracks;
        }
        set => SetValue(TracksProperty, value);
    }

    private async Task LoadTracks(ArtistDiscographyTracksHolder toFetch,
        CancellationToken ct)
    {
        const string fetch_uri = "hm://album/v1/album-app/album/{0}/android?country={1}";
        var aff =
            from cached in Spotify<WaveeUIRuntime>.Cache()
                .Map(x => x.GetRawEntity(toFetch.AlbumId)
                    .BiMap(Some: r => SuccessAff(r),
                        None: () => from countryCode in Spotify<WaveeUIRuntime>.CountryCode().Map(x => x.ValueUnsafe())
                                    let url = string.Format(fetch_uri, toFetch.AlbumId.ToString(), countryCode)
                                    from mercuryClient in Spotify<WaveeUIRuntime>.Mercury().Map(x => x)
                                    from response in mercuryClient.Get(url, CancellationToken.None).ToAff()
                                    from _ in Spotify<WaveeUIRuntime>.Cache().Map(x => x.SaveRawEntity(toFetch.AlbumId,
                                        toFetch.AlbumId.ToString(),
                                        response.Payload, DateTimeOffset.UtcNow.AddDays(1)))
                                    select response.Payload
                    )
                )
            from response in cached.ValueUnsafe()
            select response;
        var result = await aff.Run(runtime: App.Runtime);
        var r = result.ThrowIfFail();
        using var jsonDoc = JsonDocument.Parse(r);
        using var discs = jsonDoc.RootElement.GetProperty("discs").EnumerateArray();
        LanguageExt.Seq<ArtistDiscographyTrack> discsRes = LanguageExt.Seq<ArtistDiscographyTrack>.Empty;
        foreach (var disc in discs)
        {
            using var tracks = disc.GetProperty("tracks").EnumerateArray();
            foreach (var track in tracks)
            {
                var arr = track.GetProperty("artists");
                using var artistssInTrack = arr.EnumerateArray();
                var artistsResults = artistssInTrack.Select(artistInTracki => new SpotifyAlbumArtistView
                {
                    Name = artistInTracki.GetProperty("name").GetString(),
                    Id = AudioId.FromUri(artistInTracki.GetProperty("uri").GetString()),
                    Image = artistInTracki.TryGetProperty("image", out var img)
                        ? img.GetProperty("uri").GetString()
                        : null
                }).ToList();
                discsRes = discsRes.Add(new ArtistDiscographyTrack
                {
                    Title = track.GetProperty("name")
                        .GetString(),
                    Id = AudioId.FromUri(track.GetProperty("uri")
                        .GetString()),
                    Duration = TimeSpan.FromMilliseconds(track.GetProperty("duration")
                        .GetUInt32()),
                    Number = track.GetProperty("number")
                        .GetUInt16(),
                    IsExplicit = track.GetProperty("explicit")
                        .GetBoolean(),
                    Playcount = track.GetProperty("playcount") is
                    {
                        ValueKind: JsonValueKind.Number
                    } p
                        ? p.GetUInt64()
                        : LanguageExt.Option<ulong>.None,
                    Artists = artistsResults,
                    PlayCommand = null
                });
            }
        }

        //await Task.Delay(TimeSpan.FromMilliseconds(80), ct);
        this.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
       {
           //artistDiscographyTracks.Clear();
           //artistDiscographyTracks.AddRange(discsRes);
           for (var index = 0; index < discsRes.Count; index++)
           {
               var track = discsRes[index];
               var oldTrack = toFetch.Tracks[index];

               oldTrack.Title = track.Title;
               oldTrack.Id = track.Id;
               oldTrack.Duration = track.Duration;
               oldTrack.Number = track.Number;
               oldTrack.IsExplicit = track.IsExplicit;
               oldTrack.Playcount = track.Playcount;
               oldTrack.Artists = track.Artists;
           }

           this.Bindings.Update();
       });
        // return artistDiscographyTracks
        //     .Select(c => new ArtistDiscographyTrack
        //     {
        //         Number = c.Number,
        //         Playcount = (ulong)Random.Shared.Next(0,
        //             int.MaxValue - 1),
        //         Title = $"Track {c.Number}",
        //         Id = default,
        //         Duration = default,
        //         IsExplicit = false,
        //     }).ToList();
    }

    public AudioId Id
    {
        get => (AudioId)GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }


    private void ShimmerListView_OnChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
    {

    }

    private void ShimmerListView_OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {

    }

    private async void Track_DragStarted(UIElement sender, DragStartingEventArgs args)
    {
        var itemsRepeater = sender.FindAscendant<ItemsRepeater>();

        var selecteditems =
            itemsRepeater.FindDescendant<ListViewItem>(x => x.IsSelected);

        var deferral = args.GetDeferral();
        // args.Data.SetData(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text,
        //     JsonSerializer.Serialize(selecteditems.Select(c => c.Id)));
        // args.Data.Properties.Title = "Tracks";
        // args.Data.Properties.Description = "Tracks";
        // args.Data.Properties.ApplicationName = "Wavee";
        // var v = args.Data.GetView();
        //
        // args.Data.RequestedOperation = DataPackageOperation.Copy;
        // //  args.Data.SetText("Add tracks");
        //
        // //var bitmap = this.Img.Source as BitmapImage;
        // args.DragUI.SetContentFromDataPackage();
        //args.DragUI.SetContentFromBitmapImage(bitmap);
        deferral.Complete();
    }
}