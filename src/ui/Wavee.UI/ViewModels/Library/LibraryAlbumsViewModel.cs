using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Providers;
using Wavee.UI.Services;

namespace Wavee.UI.ViewModels.Library;

public sealed class LibraryAlbumsViewModel : ObservableObject, ILibraryComponentViewModel
{
    private LibraryComponentFilterRecord? _selectedSorting;
    private string? _filter;
    private bool _isBusy;
    private readonly IDispatcher _dispatcher;
    public LibraryAlbumsViewModel(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        AvailableSortingOptions =
        [
            new LibraryComponentFilterRecord(KnownLibraryComponentFilterType.Alphabetical, "Alphabetical", true),
            new LibraryComponentFilterRecord(KnownLibraryComponentFilterType.DateAdded, "Date added", true),
            new LibraryComponentFilterRecord(KnownLibraryComponentFilterType.RecentlyPlayed, "Date Played", false),
            new LibraryComponentFilterRecord(KnownLibraryComponentFilterType.ArtistAlphabetical, "Artist", true),
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