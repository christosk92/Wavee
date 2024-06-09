using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.UI.ViewModels.Library;

public sealed class LibraryAlbumsViewModel : LibraryCategoryViewModel
{
    public LibraryAlbumsViewModel(IObservable<IChangeSet<ILikedAlbum, string>> connect) : base("Albums", Icons.SegoeFluent("\uE93C"), Icons.SegoeFluent("\uE93C"))
    {
        connect
            .Transform(album => CreateLibraryAlbumViewModel(album))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var albums)
            .Subscribe();
        Items = albums;
    }

    public ReadOnlyObservableCollection<object> Items { get; }

    private object CreateLibraryAlbumViewModel(ILikedAlbum album)
    {
        //TODO:
        return new();
    }
}