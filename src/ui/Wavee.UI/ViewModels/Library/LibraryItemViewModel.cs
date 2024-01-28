using Wavee.UI.Providers;

namespace Wavee.UI.ViewModels.Library;

public sealed class LibraryItemViewModel
{
    public LibraryItemViewModel(IWaveeItem item, DateTimeOffset addedAt, IWaveeUIAuthenticatedProfile profile)
    {
        Item = item;
        AddedAt = addedAt;
        Profile = profile;
    }
    public DateTimeOffset AddedAt { get; }
    public IWaveeItem Item { get; }
    public IWaveeUIAuthenticatedProfile Profile { get; }
}
