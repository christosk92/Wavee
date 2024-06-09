using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.UI.ViewModels.Library;

public sealed class LibraryArtistsViewModel : LibraryCategoryViewModel
{
    public LibraryArtistsViewModel(IObservable<IChangeSet<ILikedArtist, string>> connect) : base("Artists",
        Icons.SegoeFluent("\uEBDA"),
        Icons.SegoeFluent("\uEBDA"))
    {
        connect
            .Transform(artist => CreateLibraryArtistViewModel(artist))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var artists)
            .Subscribe();

        Items = artists;
    }

    private object CreateLibraryArtistViewModel(ILikedArtist artist)
    {
        //TODO:
        return new();
    }

    public ReadOnlyObservableCollection<object> Items { get; }
}