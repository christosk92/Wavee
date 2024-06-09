using System;
using System.Collections.ObjectModel;
using DynamicData;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.UI.WinUI;

namespace Wavee.UI.ViewModels.Library;

public abstract partial class LibraryCategoryViewModel : ReactiveObject
{
    [AutoNotify] private int _order;
    [AutoNotify] private bool _isSelected;
    protected LibraryCategoryViewModel(string name,
        IconElement icon,
        IconElement selectedIcon)
    {
        Name = name;
        Icon = icon;
        SelectedIcon = selectedIcon;
    }

    public string Name { get; }
    public IconElement Icon { get; }
    public IconElement SelectedIcon { get; }
    public ReadOnlyObservableCollection<object>? SubItems { get; protected set; }
    public bool HasSubItems => SubItems is not null;

    public static LibraryCategoryViewModel Pins(IObservable<IChangeSet<IPinnableItem, string>> pins) => new LibraryPinsViewModel(pins);
    public static LibraryCategoryViewModel Playlists(IObservable<IChangeSet<IPlaylist, string>> playlists) => new LibraryPlaylistsViewModel(playlists);
    public static LibraryCategoryViewModel Albums(IObservable<IChangeSet<ILikedAlbum, string>> connect) => new LibraryAlbumsViewModel(connect);
    public static LibraryCategoryViewModel Artists(IObservable<IChangeSet<ILikedArtist, string>> connect) => new LibraryArtistsViewModel(connect);
    public static LibraryCategoryViewModel Folders(IObservable<IChangeSet<IFolder, string>> connect) => new LibraryFoldersViewModel(connect);
    public static LibraryCategoryViewModel LikedSongs(IObservable<IChangeSet<ILikedSong, string>> connect) => new LibraryLikedSongsViewModel(connect);

    public IconElement PickIcon(bool b)
    {
        if (b) return SelectedIcon;
        return Icon;
    }
}