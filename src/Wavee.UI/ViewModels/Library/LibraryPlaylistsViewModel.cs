using System;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.UI.ViewModels.Library;

public sealed class LibraryPlaylistsViewModel : LibraryCategoryViewModel
{
    public LibraryPlaylistsViewModel(IObservable<IChangeSet<IPlaylist, string>> playlists) : base("Playlists",
        Icons.MediaPlayer("\uE93F"),
        Icons.MediaPlayer("\uE93F"))
    {
        playlists
            .Transform(x => CreatePlaylistViewModel(x))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var items)
            .Subscribe();
        SubItems = items;
    }

    private object CreatePlaylistViewModel(IPlaylist playlist)
    {
        //TODO:
        return new object();
    }
}
