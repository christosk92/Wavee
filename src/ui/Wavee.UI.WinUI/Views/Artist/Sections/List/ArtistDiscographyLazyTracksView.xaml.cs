using System;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Artist.Sections.List;

public partial class ArtistDiscographyLazyTracksView : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ArtistDiscographyLazyTracksView), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(string), typeof(ArtistDiscographyLazyTracksView), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty TracksCountProperty = DependencyProperty.Register(nameof(TracksCount), typeof(ushort), typeof(ArtistDiscographyLazyTracksView), new PropertyMetadata(default(ushort)));
    public static readonly DependencyProperty TracksProperty = DependencyProperty.Register(nameof(Tracks), typeof(Seq<ArtistDiscographyTrack>), typeof(ArtistDiscographyLazyTracksView), new PropertyMetadata(default(Seq<ArtistDiscographyTrack>)));
    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(nameof(Id), typeof(string), typeof(ArtistDiscographyLazyTracksView), new PropertyMetadata(default(string)));

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

    public Seq<ArtistDiscographyTrack> Tracks
    {
        get
        {
            var tracks = (Seq<ArtistDiscographyTrack>)GetValue(TracksProperty);
            if (tracks.Any(x => !x.IsLoaded))
            {
                //setup a loading task
                var id = Id;
                var existingTracks = tracks;
                Task.Run(async () =>
                {
                    var tracks = await LoadTracks(id, existingTracks, CancellationToken.None);
                    this.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                    {
                        this.Tracks = tracks;
                    });
                });
            }
            return tracks;
        }
        set => SetValue(TracksProperty, value);
    }

    private async Task<Seq<ArtistDiscographyTrack>> LoadTracks(string id,
        Seq<ArtistDiscographyTrack> artistDiscographyTracks,
        CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(80), ct);
        return artistDiscographyTracks
            .Select(c => new ArtistDiscographyTrack
            {
                Number = c.Number,
                Playcount = (ulong)Random.Shared.Next(0, int.MaxValue - 1),
                Title = $"Track {c.Number}",
            });
    }

    public string Id
    {
        get => (string)GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }


    private void ShimmerListView_OnChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
    {

    }

    private void ShimmerListView_OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {

    }
}