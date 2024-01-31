using System.Windows.Input;
using LanguageExt;
using Wavee.UI.Providers;
using Wavee.UI.ViewModels.Artist;

namespace Wavee.UI.ViewModels.Library;
public sealed class LibraryItemViewModel : WaveePlayableItemViewModel
{
    public LibraryItemViewModel(IWaveeItem item, DateTimeOffset addedAt, ICommand playCommand,
        IWaveeUIAuthenticatedProfile profile) : base(item.Id, playCommand)
    {
        Item = item;
        AddedAt = addedAt;
        Profile = profile;
    }

    public DateTimeOffset AddedAt { get; }
    public IWaveeItem Item { get; }
    public IWaveeUIAuthenticatedProfile Profile { get; }
    public override string Name => Item.Name;

    public override bool Is(IWaveePlayableItem x, Option<string> uid, string contextId)
    {
        if (x is null) return false;

        return contextId == Item.Id || x.Descriptions.HeadOrNone().Match(
            None: () => false,
            Some: y => y.Id == Id
        );
    }
}
