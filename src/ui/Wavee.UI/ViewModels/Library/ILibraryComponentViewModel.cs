namespace Wavee.UI.ViewModels.Library;

public interface ILibraryComponentViewModel : IHasProfileViewModel
{
    bool IsBusy { get; set; }
    string Filter { get; set; }
    IReadOnlyCollection<LibraryComponentFilterRecord> AvailableSortingOptions { get; }
    LibraryComponentFilterRecord? SelectedSorting { get; set; }
}

public record LibraryComponentFilterRecord(KnownLibraryComponentFilterType Key, string Title, bool CanSortBiDirectional);

public enum KnownLibraryComponentFilterType
{
    Alphabetical,
    DateAdded,
    RecentlyPlayed,
    AlbumAlphabetical,
    ArtistAlphabetical,
    Duration
}