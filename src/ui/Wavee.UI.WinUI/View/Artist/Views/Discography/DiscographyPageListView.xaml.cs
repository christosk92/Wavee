using System;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.Metadata.Artist;
using Wavee.UI.Client.Artist;
using CommunityToolkit.Common.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LanguageExt;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.Metadata.Common;
using Wavee.UI.Client.Album;
using Wavee.UI.ViewModel.Shell;

namespace Wavee.UI.WinUI.View.Artist.Views.Discography;

public sealed partial class DiscographyPageListView : UserControl
{
    public DiscographyPageListView(GetReleases getReleasesFunc)
    {
        this.InitializeComponent();
        Releases = new IncrementalLoadingCollection<DiscographyReleasesVmSource, ArtistDiscographyReleaseViewModel>(
            new DiscographyReleasesVmSource(getReleasesFunc), itemsPerPage: 50);
        _ = Releases.LoadMoreItemsAsync(2);
        this.InitializeComponent();
    }

    public IncrementalLoadingCollection<DiscographyReleasesVmSource, ArtistDiscographyReleaseViewModel> Releases { get; }

    private async void Scroller_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (!e.IsIntermediate)
        {
            var scroller = (ScrollViewer)sender;
            var distanceToEnd = scroller.ExtentHeight - (scroller.VerticalOffset + scroller.ViewportHeight);

            if (distanceToEnd <= 3.0 * scroller.ViewportHeight
                && Releases.HasMoreItems && !Releases.IsLoading)
            {
                await Releases.LoadMoreItemsAsync(2);
            }
        }
    }

    private void DiscographyPageGridView_OnLoaded(object sender, RoutedEventArgs e)
    {
        var scroller = this.FindAscendant<ScrollViewer>();
        scroller.ViewChanged += Scroller_ViewChanged;
    }

    private async void ItemsRepeater_OnElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        try
        {
            //var element = args.Index;
            var uiElementinView = args.Element;
            if (uiElementinView is FrameworkElement ff &&
                ff.Tag is ArtistDiscographyReleaseViewModel itemInview)
            {
                //try to access ArtistDiscographyReleaseViewModel
                await itemInview.LoadTracks();
            }
        }
        catch (COMException x)
        {

        }
        catch (Exception ex)
        {

        }
    }
}

public class ArtistDiscographyReleaseViewModel
{
    private ObservableCollection<LoadingWaveeUIAlbumTrack> _items;
    public IArtistDiscographyRelease Release { get; set; }

    public ObservableCollection<LoadingWaveeUIAlbumTrack> Items
    {
        get => _items;
        set => _items = value;
    }

    public async Task LoadTracks()
    {
        var id = Release.Id.ToString();
        var response = await Task.Run(async () => await ShellViewModel.Instance.User.Client.Album.GetAlbumTracks(id));
        var allTracks = response.SelectMany(f => f.Tracks);
        //= allTracks;
        //check if release is still the same

        if (Release.Id.ToString() == id)
        {
            Items.Clear();
            foreach (var track in allTracks)
            {
                var index = track.TrackNumber - 1;
                //Items[index].Track = track;
                Items.Add(new LoadingWaveeUIAlbumTrack { Track = track });
            }
        }
    }
    public ImageSource? GetImage(ICoverImage[] images)
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
            var imgsource = new BitmapImage
            {
                DecodePixelHeight = targetSize,
                DecodePixelWidth = targetSize,
                UriSource = new Uri(head.IfNone(string.Empty))
            };
            return imgsource;
        }

        return null;
    }

    // public async void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
    // {
    //     if (_items is not null && _items.Any(f => f.Loading))
    //     {
    //         await LoadTracks(Release.Id.ToString());
    //     }
    // }

    public async void Loaded(object sender, RoutedEventArgs e)
    {
        await LoadTracks();
    }
}

public class LoadingWaveeUIAlbumTrack : ObservableObject
{
    private IWaveeUIAlbumTrack _track;

    public IWaveeUIAlbumTrack? Track
    {
        get => _track;
        set
        {
            if (SetProperty(ref _track, value))
            {
                this.OnPropertyChanged(nameof(Loading));
            }
        }
    }
    public bool Loading => Track is null;

    public bool Negate(bool b)
    {
        return !b;
    }
}

public sealed class DiscographyReleasesVmSource : IIncrementalSource<ArtistDiscographyReleaseViewModel>
{
    private readonly GetReleases _getReleasesFunc;
    public DiscographyReleasesVmSource(GetReleases getReleasesFunc)
    {
        _getReleasesFunc = getReleasesFunc;
    }

    public Task<IEnumerable<ArtistDiscographyReleaseViewModel>> GetPagedItemsAsync(int pageIndex,
        int pageSize, CancellationToken cancellationToken = new CancellationToken())
    {
        var offset = pageIndex * pageSize;
        var limit = pageSize;
        return _getReleasesFunc(offset, limit, cancellationToken)
            .Map(x => x.Select(y => new ArtistDiscographyReleaseViewModel
            {
                Release = y,
                Items = new ObservableCollection<LoadingWaveeUIAlbumTrack>(Enumerable.Range(0, y.TotalTracks)
                    .Select(_ => new LoadingWaveeUIAlbumTrack()))

            }));
    }
}