using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Common.Collections;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic.Logging;
using Wavee.UI.ViewModel.Playlist;
using Wavee.UI.ViewModel.Playlist.Headers;
using Wavee.UI.ViewModel.Shell;
using Wavee.UI.WinUI.Controls;
using Wavee.UI.WinUI.Navigation;
using Log = Serilog.Log;

namespace Wavee.UI.WinUI.View.Playlist;

public sealed partial class PlaylistView : UserControl, INavigable, ICacheablePage
{
    private readonly CancellationTokenSource _cts = new();
    public PlaylistView()
    {
        ViewModel = new PlaylistViewModel(ShellViewModel.Instance.User);
        this.InitializeComponent();
        IncrementalLoadingCollection = new IncrementalLoadingCollection<PlaylistTrackSource, PlaylistTrackViewModel>(new PlaylistTrackSource(ViewModel));
    }

    public PlaylistViewModel ViewModel { get; }
    public IncrementalLoadingCollection<PlaylistTrackSource, PlaylistTrackViewModel> IncrementalLoadingCollection { get; }

    public async void NavigatedTo(object parameter)
    {
        if (parameter is string id)
        {
            try
            {
                await ViewModel.Initialize(id, _cts.Token);
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException disposed)
            {

            }
            catch (Exception e)
            {
                Log.Error(e, "An error occurred while initializing the playlist.");
            }
            finally
            {
                _cts.Dispose();
            }
        }
    }

    public void NavigatedFrom(NavigationMode mode)
    {
        try
        {
            _cts.Cancel();
        }
        catch (Exception e)
        {

        }
    }

    public bool ShouldKeepInCache(int currentDepth)
    {
        return currentDepth <= 4;
    }

    public void RemovedFromCache()
    {
    }

    private void PlaylistView_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        //BigHeaderImage
        if (ViewModel.Header is PlaylistBigHeader)
        {
            double wantHeight = 0;
            var bigHeader = this.FindDescendant<ImageTransitionControl>(x => x.Name is "BigHeaderImage");
            if (bigHeader is not null)
            {
                //2:1 ratio of height 
                var thisSize = this.ActualSize;
                var height = thisSize.Y;
                wantHeight = height * .75;
                bigHeader.Height = wantHeight;
            }

            var bigHeaderGrid = this.FindDescendant<Grid>(x => x.Name is "BigHeaderGrid");
            if ((bigHeaderGrid is not null))
            {
                //pull it up by wantHeight
                double pullUp = wantHeight * .3;
                bigHeaderGrid.Margin = new Thickness(0, 0, 0, -pullUp);
            }

            var bigHeaderMetadataPanel = this.FindDescendant<StackPanel>(x => x.Name is "BigHeaderMetadataPanel");
            if (((bigHeaderMetadataPanel is not null)))
            {
                double pullUp = (wantHeight / 2) * 0.5;
                bigHeaderMetadataPanel.Margin = new Thickness(0, -pullUp, 0, 0);
            }
        }
    }

    private void BigHeaderImage_OnLoaded(object sender, RoutedEventArgs e)
    {
        PlaylistView_OnSizeChanged(this, null);
    }

    private void AlbumImage_OnImageOpened(object sender, RoutedEventArgs e)
    {
        (ViewModel.Header as RegularPlaylistHeader)!.MozaicCreated = true;
    }
}

public class PlaylistTrackSource : IIncrementalSource<PlaylistTrackViewModel>
{
    private readonly PlaylistViewModel _viewModel;
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public PlaylistTrackSource(PlaylistViewModel viewModel)
    {
        _viewModel = viewModel;
    }

    public async Task<IEnumerable<PlaylistTrackViewModel>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            await _viewModel.WaitForTracks.Task;
            var items = _viewModel.Generate(offset: pageIndex * pageSize, limit: pageSize);
            //10 ms delay to trick the UI into thinking it's loading
            await Task.Delay(10, cancellationToken);
            //start a task to fill the next page
            _ = Task.Run(async () =>
            {
                await _viewModel.FetchAndSetTracks(items
                        .ToDictionary(x=> x.Uid, x=> x),
                    (action) => _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () => action()),
                    cancellationToken);
            }, cancellationToken);
            return items;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to fetch page");
            return Enumerable.Empty<PlaylistTrackViewModel>();
        }
    }
}