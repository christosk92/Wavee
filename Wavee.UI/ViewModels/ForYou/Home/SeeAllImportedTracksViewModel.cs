using System.Collections.Immutable;
using CommunityToolkit.Common.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Wavee.UI.AudioImport;
using Wavee.UI.AudioImport.Database;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModels.Album;
using Wavee.UI.ViewModels.AudioItems;
using Wavee.UI.ViewModels.Playback;
using Wavee.UI.ViewModels.Playback.Contexts.Local;
using Wavee.UI.ViewModels.Playback.Impl;

namespace Wavee.UI.ViewModels.ForYou.Home
{
    public partial class SeeAllImportedTracksViewModel : ObservableObject, INavigatable
    {
        [ObservableProperty] private bool _albumsSourceGrouped;
        [ObservableProperty] private GroupedAlbumViewModels[]? _albumsSource;
        private string? _selectedSortOption;
        public string? SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (SetProperty(ref _selectedSortOption, value))
                {
                    UpdateViewSource();
                }
            }
        }

        public FilesSource Source
        {
            get;
        }

        private IAudioDb _audioDb;
        public SeeAllImportedTracksViewModel(IAudioDb audiodb)
        {
            _audioDb = audiodb;
            Source = new FilesSource();
            TracksCount = _audioDb.Count();
            SortAlbumOptions = new[]
            {
                "Import Date",
                "Artist",
                "A - Z",
                "Z - A",
                "Release Year",
            };
            SelectedSortOption = SortAlbumOptions.First();
        }
        private async Task UpdateViewSource(string? query = null)
        {
            var tolower = query?.ToLower();
            var albumsModels = await Task.Run(() =>
                _audioDb.AudioFiles
                    .Find(tolower != null ? file => file.Album.Contains(tolower) || file.Title.Contains(tolower) || file.Artists.Any(j => j.Contains(tolower)) : _ => true)
                    .OrderByDescending(file => file.CreatedAt)
                    .GroupBy(x => x.Album)
                    .Select(x => new LocalAlbum
                    {
                        Album = x.Key,
                        ServiceType = ServiceType.Local,
                        Image = x.FirstOrDefault(a => !string.IsNullOrEmpty(a.ImagePath))?.ImagePath,
                        Artists = x.SelectMany(a => a.Artists).ToArray(),
                        Tracks = x.Count(),
                        ReleaseYear = (ushort)x.First().Year
                    }).ToArray());
            var albums = albumsModels
                .Select((a, i) =>
                {
                    IPlayContext? context;
                    if (string.IsNullOrEmpty(tolower))
                    {
                        context =
                            LocalLibraryContext.Create(PlayOrderType.Imported, true, i, LibraryViewType.Albums);
                    }
                    else
                    {
                        context = new CustomContext(albumsModels.Cast<IPlayableItem>().Skip(i));

                    }

                    return new AlbumViewModel(a,
                        new AsyncRelayCommand<IPlayContext>(p => PlayerViewModel.Instance.PlayTask(p)),
                        context);
                });

            switch (SelectedSortOption)
            {
                case "Import Date":
                    AlbumsSource = new[]
                    {
                        new GroupedAlbumViewModels(false,
                            null,
                            albums)
                    };
                    AlbumsSourceGrouped = false;
                    break;
                case "Artist":
                    AlbumsSource = albums
                        .GroupBy(a => a.Album.Artists[0])
                        .Select(j => new GroupedAlbumViewModels(true,
                            j.Key,
                            j
                        ))
                        .ToArray();
                    AlbumsSourceGrouped = true;
                    break;
                case "A - Z":
                    AlbumsSource = albums
                        .GroupBy(a => a.Album.Name[0])
                        .OrderBy(a => a.Key)
                        .Select(j => new GroupedAlbumViewModels(true,
                            j.Key.ToString(),
                            j
                        )).ToArray(); AlbumsSourceGrouped = true;
                    break;
                case "Z - A":
                    AlbumsSource = albums
                        .GroupBy(a => a.Album.Name[0])
                        .OrderByDescending(a => a.Key)
                        .Select(j => new GroupedAlbumViewModels(true,
                            j.Key.ToString(),
                            j
                        )).ToArray();
                    AlbumsSourceGrouped = true;
                    break;
                case "Release Year":
                    AlbumsSource = albums
                        .GroupBy(a => a.Album.ReleaseYear)
                        .OrderByDescending(a => a.Key)
                        .Select(j => new GroupedAlbumViewModels(true,
                            j.Key.ToString(),
                            j
                        )).ToArray();
                    AlbumsSourceGrouped = true;
                    break;
            }
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

        public string[] SortAlbumOptions
        {
            get;
        }

        public void SearchForAlbum(string senderText)
        {
            UpdateViewSource(senderText);
        }
    }

    public class GroupedAlbumViewModels
    {
        public GroupedAlbumViewModels(bool isGrouped, string? groupKey, IEnumerable<AlbumViewModel> vms)
        {
            IsGrouped = isGrouped;
            GroupKey = groupKey;
            Items = vms;
        }
        public bool IsGrouped
        {
            get;
            init;
        }
        public string? GroupKey
        {
            get;
            init;
        }
        public IEnumerable<AlbumViewModel> Items
        {
            get;
        }

        public bool IsNotEmpty(string? s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            return true;
        }
    }
}


public class FilesSource : IIncrementalSource<TrackViewModel>
{
    private int _depth;

    public async Task<IEnumerable<TrackViewModel>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct = default)
    {
        var skip = pageIndex * pageSize;
        var take = pageSize;
        var items = Ioc.Default.GetRequiredService<LocalAudioManagerViewModel>()
            .GetLatestAudioFiles(a => a.CreatedAt, false, skip, take);
        await Task.Delay(10, ct);
        var itemsToReturn = items
            .Select((a, i) => new TrackViewModel(i + _depth, a))
            .ToImmutableArray();
        _depth += items.Count;
        return itemsToReturn;
    }
}
