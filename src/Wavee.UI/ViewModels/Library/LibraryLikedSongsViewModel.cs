using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.UI.ViewModels.Library;

public sealed class LibraryLikedSongsViewModel : LibraryCategoryViewModel
{
    public LibraryLikedSongsViewModel(IObservable<IChangeSet<ILikedSong, string>> connect) : base("Liked songs",
        Icons.SegoeFluent("\uEB51"),
        Icons.SegoeFluent("\uEB52"))
    {
        connect
            .Transform(x => CreateLibraryLikedSongViewModel(x))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var items)
            .Subscribe();
        Items = items;
    }

    public ReadOnlyObservableCollection<object> Items { get; }


    private object CreateLibraryLikedSongViewModel(ILikedSong likedSong)
    {
        //TODO:
        return new object();
    }
}
