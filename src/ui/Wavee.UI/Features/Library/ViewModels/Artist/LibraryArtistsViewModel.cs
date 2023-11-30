using System.Collections.ObjectModel;
using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Domain.Common;
using Wavee.UI.Domain.Library;
using Wavee.UI.Entities.Artist;
using Wavee.UI.Features.Album.ViewModels;
using Wavee.UI.Features.Artist.Queries;
using Wavee.UI.Features.Library.Queries;
using Wavee.UI.Features.Navigation.ViewModels;

namespace Wavee.UI.Features.Library.ViewModels.Artist;

public sealed class LibraryArtistsViewModel : NavigationItemViewModel
{
    private readonly IMediator _mediator;

    private string? _query;
    private bool _isLoading;
    private string _sortField;
    private int _total;
    private LibraryArtistViewModel? _selectedArtist;


    public LibraryArtistsViewModel(IMediator mediator)
    {
        _mediator = mediator;
        _sortField = nameof(LibraryItem<SimpleArtistEntity>.AddedAt);

        SortFields = new[]
        {
            nameof(LibraryItem<SimpleArtistEntity>.AddedAt),
            nameof(LibraryItem<SimpleArtistEntity>.Item.Name),
            "Recents"
        };
    }
    public ObservableCollection<LibraryArtistViewModel> Artists { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }


    public string? Query
    {
        get => _query;
        set => SetProperty(ref _query, value);
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

    public string SortField
    {
        get => _sortField;
        set => SetProperty(ref _sortField, value);
    }

    public IReadOnlyCollection<string> SortFields { get; }

    public async Task Initialize()
    {
        try
        {
            Artists.Clear();
            var library = await _mediator.Send(new GetLibraryArtistsQuery()
            {
                Offset = 0,
                Limit = 20,
                Search = _query,
                SortField = _sortField
            });
            HandleResult(library);
        }
        catch (Exception e)
        {
            HandleError(e);
        }
    }

    public async Task FetchPage(int offset, CancellationToken cancellationToken)
    {
        try
        {
            var library = await _mediator.Send(new GetLibraryArtistsQuery()
            {
                Offset = offset,
                Limit = 20,
                Search = _query,
                SortField = _sortField
            }, cancellationToken);
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
            var vm = new LibraryArtistViewModel
            {
                Name = artist.Item.Name,
                Id = artist.Item.Id,
                BigImageUrl = bigImage.Url,
                MediumImageUrl = mediumImage.Url,
                SmallImageUrl = smallestImage.Url,
                AddedAt = artist.AddedAt,
                TotalAlbums = null
            };
            vm.FetchArtistAlbumsFunc = (i, i1, arg3) => FetchArtistAlbums(vm, i, i1, arg3);
            Artists.Add(vm);
        }
        Total = artistsResult.Total;
    }

    private void HandleError(Exception exception)
    {

    }

    public async Task FetchArtistAlbums(LibraryArtistViewModel artist,
        int offset, int limit, CancellationToken cancellationToken)
    {
        var results = await _mediator.Send(new GetAlbumsForArtistQuery
        {
            Id = artist.Id,
            Offset = (uint)offset,
            Limit = (uint)limit,
        }, cancellationToken);

        artist.TotalAlbums = results.Total;
        foreach (var album in results.Albums)
        {
            var bigImage = album.Images.MaxBy(z => z.Height ?? 0);
            //   var smallestImage = album.Images.MinBy(z => z.Height ?? 0);
            var mediumImage = album.Images.OrderBy(z => z.Height ?? 0).Skip(1).FirstOrDefault();
            artist.Albums.Add(new AlbumViewModel
            {
                Name = album.Name,
                Id = album.Id,
                BigImageUrl = bigImage.Url,
                TotalSongs = (uint)album.Tracks.Length,
                Duration = TimeSpan.FromMilliseconds(album.Tracks.Sum(x => x.Duration.TotalMilliseconds)),
                Year = album.Year,
                Type = album.Type,
                Tracks = album.Tracks.Select((x, i) => new AlbumTrackViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Duration = x.Duration,
                    Number = i + 1,
                })
                    .ToArray(),
                MediumImageUrl = mediumImage.Url,
            });
        }
    }
}