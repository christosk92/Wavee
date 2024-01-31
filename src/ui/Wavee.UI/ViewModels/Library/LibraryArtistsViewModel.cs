﻿using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wavee.UI.Providers;
using Wavee.UI.Services;
using Wavee.UI.ViewModels.Artist;
using Wavee.UI.ViewModels.NowPlaying;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Wavee.UI.ViewModels.Library;

public sealed class LibraryArtistsViewModel : ObservableObject, ILibraryComponentViewModel
{
    private readonly IDispatcher _dispatcherWrapper;
    private LibraryComponentFilterRecord? _selectedSorting;
    private string? _filter;
    private bool _isBusy;
    private bool _sortAscending;
    private readonly List<IWaveeUIAuthenticatedProfile> _profiles = [];
    private WaveeArtistViewModel? _selectedItem;
    public WaveeArtistViewModel? _changingTo;

    public LibraryArtistsViewModel(IDispatcher dispatcherWrapper)
    {
        Errors = new ObservableCollection<ExceptionForProfile>();
        Items = new ObservableCollection<LibraryItemViewModel>();
        _dispatcherWrapper = dispatcherWrapper;
        AvailableSortingOptions =
        [
            new LibraryComponentFilterRecord(KnownLibraryComponentFilterType.Alphabetical, "Alphabetical", true),
            new LibraryComponentFilterRecord(KnownLibraryComponentFilterType.DateAdded, "Date added", true),
            new LibraryComponentFilterRecord(KnownLibraryComponentFilterType.RecentlyPlayed, "Date Played", false)
        ];
        OnItemInvoked = new RelayCommand<object>((x) =>
        {

        });

        OnItemSelectedCommand = new RelayCommand<object>(x =>
        {
            if (x is null)
            {
                return;
            }
            var item = (LibraryItemViewModel)x;
            SelectedItem = WaveeArtistViewModel.GetOrCreate(item.Item.Id, item.Item, profile: item.Profile, dispatcher: _dispatcherWrapper,
                playCommand: NowPlayingViewModel.Instance.PlayNewItemCommand);
        });
        IsBusy = true;
    }

    public ObservableCollection<ExceptionForProfile> Errors { get; }
    public ObservableCollection<LibraryItemViewModel> Items { get; }

    public WaveeArtistViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            _changingTo = value;
            this.SetProperty(ref _selectedItem, value);
        }
    }

    public void AddFromProfile(IWaveeUIAuthenticatedProfile profile)
    {
        _profiles.Add(profile);

        Task.Run(async () => await FetchData(profile, false));
    }

    public void RemoveFromProfile(IWaveeUIAuthenticatedProfile profile)
    {
        _profiles.Remove(profile);
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

    public bool SortAscending
    {
        get => _sortAscending;
        set => SetProperty(ref _sortAscending, value);
    }

    public bool HasErrors => Errors.Count > 0;
    public RelayCommand<object> OnItemInvoked { get; }
    public RelayCommand<object> OnItemSelectedCommand { get; }


    private async Task FetchData(IWaveeUIAuthenticatedProfile profile, bool isRetrying)
    {
        _dispatcherWrapper.Dispatch(() =>
        {
            IsBusy = true;
            ClearErrorsFor(profile);
        }, highPriority: true);

        if (isRetrying)
        {
            await Task.Delay(500);
        }
        var sort = _selectedSorting ?? AvailableSortingOptions.ElementAt(1);
        var sortAscending = _sortAscending;
        try
        {
            var artists = await profile.GetLibraryArtists(sort:
                sort.Key,
                sortAscending: sortAscending,
                _filter,
                PlayCommand,
                CancellationToken.None);
            _dispatcherWrapper.Dispatch(() =>
            {
                IsBusy = false;
                foreach (var item in artists)
                {
                    Items.Add(item);
                }
            }, highPriority: false);
        }
        catch (Exception x)
        {
            _dispatcherWrapper.Dispatch(() =>
            {
                AddError(profile, x, () => Task.Run(async () => await FetchData(profile, true)));
                IsBusy = false;
            }, highPriority: false);
        }
    }

    public IAsyncRelayCommand<WaveePlayableItemViewModel> PlayCommand =>
        NowPlayingViewModel.Instance.PlayNewItemCommand;

    private void AddError(IWaveeUIAuthenticatedProfile profile, Exception err, Action? retry)
    {
        Errors.Add(new ExceptionForProfile(err, profile, retry));

        this.OnPropertyChanged(nameof(HasErrors));
    }
    private void ClearErrorsFor(IWaveeUIAuthenticatedProfile profile)
    {
        var errors = Errors.Where(x => x.Profile == profile).ToArray();
        foreach (var error in errors)
        {
            Errors.Remove(error);
        }

        this.OnPropertyChanged(nameof(HasErrors));
    }
}