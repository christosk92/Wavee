using System.Collections.ObjectModel;
using Mediator;
using Wavee.Spotify.Common;
using Wavee.UI.Entities.Artist;
using Wavee.UI.Entities.Library;
using Wavee.UI.Features.Library.Queries;
using Wavee.UI.Features.Navigation.ViewModels;

namespace Wavee.UI.Features.Library.ViewModels.Artist;

public sealed class LibraryArtistsViewModel : NavigationItemViewModel
{
    private readonly IMediator _mediator;

    private string? _query;
    private bool _isLoading;
    private SortDirection _sortDirection;
    private string _sortField;
    private int _offset;
    private const int Count = 20;
    private int _total;

    public LibraryArtistsViewModel(IMediator mediator)
    {
        _mediator = mediator;
        _sortField = nameof(LibraryItem<SimpleArtistEntity>.AddedAt);
        _sortDirection = SortDirection.Descending;
    }
    public ObservableCollection<LibraryArtistViewModel> Artists { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public int Offset
    {
        get => _offset;
        private set => SetProperty(ref _offset, value);
    }

    public SortDirection SortDirection
    {
        get => _sortDirection;
        private set => SetProperty(ref _sortDirection, value);
    }

    public string? Query
    {
        get => _query;
        private set => SetProperty(ref _query, value);
    }

    public int Total
    {
        get => _total;
        private set => SetProperty(ref _total, value);
    }



    public async Task Initialize()
    {
        try
        {
            var library = await _mediator.Send(new GetLibraryArtistsQuery()
            {
                Offset = _offset,
                Limit = Count,
                Search = _query,
                SortDirection = _sortDirection,
                SortField = _sortField
            });
            if (!library.Task.IsCompleted)
            {
                IsLoading = true;
                var result = await library.Task;
                HandleResult(result);
                IsLoading = false;
            }
            else
            {
                var result = await library.Task;
                HandleResult(result);
            }
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    private void HandleResult(LibraryItems<SimpleArtistEntity> artistsResult)
    {

    }

    private void HandleError(Exception exception)
    {

    }
}