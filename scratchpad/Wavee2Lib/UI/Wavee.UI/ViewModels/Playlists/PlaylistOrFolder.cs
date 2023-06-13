using System.Collections.ObjectModel;
using LanguageExt;
using ReactiveUI;

namespace Wavee.UI.ViewModels.Playlists;

public sealed class PlaylistOrFolder : ReactiveObject
{
    private int _originalIndex;

    public PlaylistOrFolder(Either<PlaylistFolderSidebarItem, PlaylistSidebarItem> value)
    {
        Value = value;
        //setup a listener for when the name changes
    }

    public Either<PlaylistFolderSidebarItem, PlaylistSidebarItem> Value { get; }

    public string Name => Value.Match(
        folder => folder.Name,
        playlist => playlist.Name
    );

    public ObservableCollection<PlaylistSidebarItem> ItemsIfFolder => Value.Match(
        folder => folder.Items,
        playlist => throw new InvalidOperationException("This is not a folder")
    );


    public int OriginalIndex
    {
        get => _originalIndex;
        set => this.RaiseAndSetIfChanged(ref _originalIndex, value);
    }

    public string Id { get; set; }
}