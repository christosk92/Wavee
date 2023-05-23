using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Artist.Sections.List;

public partial class ArtistDiscographyLazyTracksView : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ArtistDiscographyLazyTracksView), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(string), typeof(ArtistDiscographyLazyTracksView), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty TracksCountProperty = DependencyProperty.Register(nameof(TracksCount), typeof(ushort), typeof(ArtistDiscographyLazyTracksView), new PropertyMetadata(default(ushort)));
    public static readonly DependencyProperty TracksProperty =
        DependencyProperty.Register(nameof
                (Tracks), typeof(ObservableCollection<ArtistDiscographyTrack>),
        typeof(ArtistDiscographyLazyTracksView),
        new PropertyMetadata(default(ObservableCollection<ArtistDiscographyTrack>)));
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

    public ObservableCollection<ArtistDiscographyTrack> Tracks
    {
        get
        {
            var tracks = (ObservableCollection<ArtistDiscographyTrack>)GetValue(TracksProperty);
            if (tracks.Any(x => !x.IsLoaded))
            {
                //setup a loading task
                var id = Id;
                var existingTracks = tracks;
                Task.Run(async () =>
                {
                    await LoadTracks(id, existingTracks, CancellationToken.None);
                });
            }
            return tracks;
        }
        set => SetValue(TracksProperty, value);
    }

    private async Task LoadTracks(AudioId id,
        ObservableCollection<ArtistDiscographyTrack> artistDiscographyTracks,
        CancellationToken ct)
    {
        const string fetch_uri = "hm://album/v1/album-app/album/{0}/android?country={1}";
        var aff =
            from countryCode in Spotify<WaveeUIRuntime>.CountryCode().Map(x => x.ValueUnsafe())
            let url = string.Format(fetch_uri, id.ToString(), countryCode)
            from mercuryClient in Spotify<WaveeUIRuntime>.Mercury().Map(x => x)
            from response in mercuryClient.Get(url, CancellationToken.None).ToAff()
            select response;
        var result = await aff.Run(runtime: App.Runtime);
        var r = result.ThrowIfFail();
        using var jsonDoc = JsonDocument.Parse(r.Payload);
        using var discs = jsonDoc.RootElement.GetProperty("discs").EnumerateArray();
        var uri = jsonDoc.RootElement.GetProperty("uri");
        if (uri.GetString() != id.ToString())
        {
            Debugger.Break();
        }
        Seq<ArtistDiscographyTrack> discsRes = LanguageExt.Seq<ArtistDiscographyTrack>.Empty;
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
                    Playcount = track.GetProperty("playcount") is { ValueKind: JsonValueKind.Number } p ? p.GetUInt64() : Option<ulong>.None,
                    Artists = artistsResults
                });
            }
        }

        //await Task.Delay(TimeSpan.FromMilliseconds(80), ct);
        this.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            artistDiscographyTracks.Clear();
            artistDiscographyTracks.AddRange(discsRes);

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
}