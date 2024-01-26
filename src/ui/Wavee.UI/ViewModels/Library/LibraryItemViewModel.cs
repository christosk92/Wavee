namespace Wavee.UI.ViewModels.Library;

public sealed class LibraryItemViewModel
{
    public LibraryItemViewModel(IWaveeItem item, DateTimeOffset addedAt)
    {
        Item = item;
        AddedAt = addedAt;
    }
    public DateTimeOffset AddedAt { get; }

    public IWaveeItem Item { get; }
}

public sealed class ItemViewModel
{
}