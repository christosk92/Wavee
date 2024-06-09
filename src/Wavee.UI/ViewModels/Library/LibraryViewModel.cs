using System;
using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System.Reactive.Linq;

namespace Wavee.UI.ViewModels.Library;

public sealed class LibraryViewModel : ReactiveObject
{
    private ReadOnlyObservableCollection<LibraryCategoryViewModel> _categories;
    private LibraryCategoryViewModel? _selectedCategory;

    public LibraryViewModel(AccountLibrary library, IObservable<TitleBarTabViewModel> activeTabObservable)
    {
        var pins = LibraryCategoryViewModel.Pins(library.Pins);
        pins.Order = 0;

        var playlists = LibraryCategoryViewModel.Playlists(library.Playlists);
        playlists.Order = 1;


        var likedsongs = LibraryCategoryViewModel.LikedSongs(library.LikedSongs);
        likedsongs.Order = 2;

        var albums = LibraryCategoryViewModel.Albums(library.Albums);
        albums.Order = 3;

        var artists = LibraryCategoryViewModel.Artists(library.Artists);
        artists.Order = 4;

        var folders = LibraryCategoryViewModel.Folders(library.Folders);
        folders.Order = 5;

        var sourceCache = new SourceList<LibraryCategoryViewModel>();
        sourceCache.Add(pins);
        sourceCache.Add(playlists);
        sourceCache.Add(likedsongs);
        sourceCache.Add(albums);
        sourceCache.Add(artists);
        sourceCache.Add(folders);


        sourceCache.Connect()
            .Sort(SortExpressionComparer<LibraryCategoryViewModel>.Ascending(vm => vm.Order))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _categories)
            .Subscribe();

        activeTabObservable.Subscribe(x =>
        {
            if (x.Id == Constants.LibraryTabId)
            {
                if (x.ViewModel is LibraryCategoryViewModel y)
                    SelectedCategory = y;
                else
                    SelectedCategory = null;
            }
            else
            {
                SelectedCategory = null;
            }
        });
    }

    public ReadOnlyObservableCollection<LibraryCategoryViewModel> Categories => _categories;

    public LibraryCategoryViewModel SelectedCategory
    {
        get => _selectedCategory;
        private set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }
}