using System.Collections.ObjectModel;
using LanguageExt;

namespace Wavee.UI.ViewModels;

public sealed class PlaylistOrFolder
{
    public PlaylistOrFolder(Either<PlaylistFolderViewModel, PlaylistViewModel> value)
    {
        Value = value;
        //setup a listener for when the name changes
    }

    public Either<PlaylistFolderViewModel, PlaylistViewModel> Value { get; }

    public string Name => Value.Match(
        folder => folder.Name,
        playlist => playlist.Name
    );

    public ObservableCollection<PlaylistViewModel> ItemsIfFolder => Value.Match(
        folder => folder.Items,
        playlist => throw new InvalidOperationException("This is not a folder")
    );
}