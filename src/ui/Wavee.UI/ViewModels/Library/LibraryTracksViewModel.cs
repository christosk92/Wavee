using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Providers;
using Wavee.UI.Services;

namespace Wavee.UI.ViewModels.Library;

public sealed class LibraryTracksViewModel : ObservableObject, ILibraryComponentViewModel
{
    private LibraryComponentFilterRecord? _selectedSorting;
    private string? _filter;
    private bool _isBusy;
    private readonly IDispatcher _dispatcher;

    public LibraryTracksViewModel(IDispatcher dispatcherWrapper)
    {
        _dispatcher = dispatcherWrapper;
        AvailableSortingOptions =
        [
            new LibraryComponentFilterRecord(KnownLibraryComponentFilterType.Alphabetical, "Title", true),
            new LibraryComponentFilterRecord(KnownLibraryComponentFilterType.AlbumAlphabetical, "Album", true),
            new LibraryComponentFilterRecord(KnownLibraryComponentFilterType.ArtistAlphabetical, "Artist", true),
            new LibraryComponentFilterRecord(KnownLibraryComponentFilterType.DateAdded, "Date added", true),
            new LibraryComponentFilterRecord(KnownLibraryComponentFilterType.Duration, "Duration", true)
        ];

        IsBusy = true;
    }

    public void AddFromProfile(IWaveeUIAuthenticatedProfile profile)
    {

    }

    public void RemoveFromProfile(IWaveeUIAuthenticatedProfile profile)
    {

    }
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
    public string Filter
    {
        get => _filter;
        set => SetProperty(ref _filter, value);
    }
    public IReadOnlyCollection<LibraryComponentFilterRecord> AvailableSortingOptions { get; }

    public LibraryComponentFilterRecord? SelectedSorting
    {
        get => _selectedSorting;
        set => SetProperty(ref _selectedSorting, value);
    }
}