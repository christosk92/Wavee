using LanguageExt;
using ReactiveUI;
using Wavee.Core.Ids;

namespace Wavee.UI.ViewModels;

public sealed class ShellViewModel : ReactiveObject
{
    public ShellViewModel(Action<Seq<AudioId>> onLibraryItemAdded, Action<Seq<AudioId>> onLibraryItemRemoved)
    {
        
    }

    public List<PlaylistOrFolder> Playlists { get; }
}