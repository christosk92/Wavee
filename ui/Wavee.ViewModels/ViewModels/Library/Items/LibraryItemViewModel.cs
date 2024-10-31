using ReactiveUI;
using Wavee.Models.Common;
using Wavee.ViewModels.Models;
using Wavee.ViewModels.ViewModels.Common;

namespace Wavee.ViewModels.ViewModels.Library.Items;

public abstract partial class LibraryItemViewModel : ReactiveObject
{
    protected LibraryItemViewModel(LibraryItem newLibraryItem, AbsWaveeItemViewModel viewModel)
    {
        ViewModel = viewModel;
        Id = newLibraryItem.Id;
        AddedAt = newLibraryItem.AddedAt;
    }

    public SpotifyId Id { get; }
    public DateTimeOffset AddedAt { get; }
    public AbsWaveeItemViewModel ViewModel { get; }

    public abstract void Update(LibraryItem item);
}