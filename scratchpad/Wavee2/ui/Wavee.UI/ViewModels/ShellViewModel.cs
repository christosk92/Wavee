using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.UI.States;

namespace Wavee.UI.ViewModels;

public sealed class ShellViewModel : ReactiveObject
{
    public ShellViewModel(User user, 
        Action<Seq<AudioId>> onLibraryItemAdded,
        Action<Seq<AudioId>> onLibraryItemRemoved)
    {
        AppState.Instance.CurrentUser = user;
        User = user;
    }
    public User User { get; }
}