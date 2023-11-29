using System.Collections.ObjectModel;
using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Common;
using Wavee.UI.Domain.Library;
using Wavee.UI.Entities.Artist;
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
    private int _total;
    private LibraryArtistViewModel? _selectedArtist;

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

    public LibraryArtistViewModel? SelectedArtist
    {
        get => _selectedArtist;
        set => SetProperty(ref _selectedArtist, value);
    }


    public async Task Initialize()
    {
        try
        {
            Artists.Clear();
            var library = await _mediator.Send(new GetLibraryArtistsQuery()
            {
                Offset = 0,
                Limit = 50,
                Search = _query,
                SortDirection = _sortDirection,
                SortField = _sortField
            });
            HandleResult(library);
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    private void HandleResult(LibraryItems<SimpleArtistEntity> artistsResult)
    {
        foreach (var artist in artistsResult.Items)
        {
            var bigImage = artist.Item.Images.MaxBy(z => z.Height ?? 0);
            var smallestImage = artist.Item.Images.MinBy(z => z.Height ?? 0);
            var mediumImage = artist.Item.Images.OrderBy(z => z.Height ?? 0).Skip(1).FirstOrDefault();
            Artists.Add(new LibraryArtistViewModel
            {
                Name = artist.Item.Name,
                Id = artist.Item.Id,
                BigImageUrl = bigImage.Url,
                MediumImageUrl = mediumImage.Url,
                SmallImageUrl = smallestImage.Url,
                AddedAt = artist.AddedAt
            });
        }
        Total = artistsResult.Total;
    }

    private void HandleError(Exception exception)
    {

    }
}