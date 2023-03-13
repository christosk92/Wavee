using System.Collections.Immutable;
using CommunityToolkit.Common.Collections;
using Wavee.UI.AudioImport;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModels.AudioItems;

namespace Wavee.UI.ViewModels.ForYou.Home
{
    public class SeeAllImportedTracksViewModel : INavigatable
    {
        public FilesSource Source
        {
            get;
        }
        public AlbumViewModel[] AlbumsSource
        {
            get;
        }
        private readonly LocalAudioManagerViewModel _vm;
        public SeeAllImportedTracksViewModel(LocalAudioManagerViewModel audioManagerViewModel)
        {
            _vm = audioManagerViewModel;
            Source = new FilesSource(audioManagerViewModel);
            AlbumsSource = audioManagerViewModel
                .GetLatestAlbums(a => a.CreatedAt, false, 0, int.MaxValue)
                .Select(a => new AlbumViewModel(a))
                .ToArray();
            TracksCount = audioManagerViewModel.Count();
        }
        public void OnNavigatedTo(object parameter)
        {

        }

        public void OnNavigatedFrom()
        {

        }

        public int MaxDepth
        {
            get;
        }

        public int TracksCount
        {
            get;
        }
    }
}

// public class AlbumsSource : IIncrementalSource<AlbumViewModel>
// {
//     private int _depth;
//     private readonly LocalAudioManagerViewModel _localAudioManagerViewModel;
//     public AlbumsSource(LocalAudioManagerViewModel localAudioManagerViewModel)
//     {
//         _localAudioManagerViewModel = localAudioManagerViewModel;
//     }
//
//     public async Task<IEnumerable<AlbumViewModel>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct = default)
//     {
//         var skip = pageIndex * pageSize;
//         var take = pageSize;
//
//         var items = _localAudioManagerViewModel
//             .GetLatestAlbums(a => a.CreatedAt, false, skip, take);
//         await Task.Delay(10, ct);
//         var itemsToReturn = items
//             .Select((a, i) => new AlbumViewModel(a))
//             .ToImmutableArray();
//         _depth += itemsToReturn.Length;
//         return itemsToReturn;
//     }
// }
public class FilesSource : IIncrementalSource<TrackViewModel>
{
    private int _depth;
    private readonly LocalAudioManagerViewModel _localAudioManagerViewModel;
    public FilesSource(LocalAudioManagerViewModel localAudioManagerViewModel)
    {
        _localAudioManagerViewModel = localAudioManagerViewModel;
    }

    public async Task<IEnumerable<TrackViewModel>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct = default)
    {
        var skip = pageIndex * pageSize;
        var take = pageSize;

        var items = _localAudioManagerViewModel
            .GetLatestAudioFiles(a => a.CreatedAt, false, skip, take);
        await Task.Delay(10, ct);
        var itemsToReturn = items
            .Select((a, i) => new TrackViewModel(i + _depth, a))
            .ToImmutableArray();
        _depth += items.Count;
        return itemsToReturn;
    }
}
