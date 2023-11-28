using LiteDB;
using Wavee.UI.Entities.Artist;
using Wavee.UI.Entities.Library;
using Wavee.UI.Features.Library.Queries;

namespace Wavee.UI.Features.Library.DataAcces;

internal sealed class LibraryRepository : ILibraryRepository
{
    private readonly ILiteCollection<ArtistLibraryItemEntity> _artists;

    public LibraryRepository(ILiteDatabase db)
    {
        _artists = db.GetCollection<ArtistLibraryItemEntity>("artists");
    }

    public async Task<LibraryItems<SimpleArtistEntity>> GetArtists(
        string userId,
        string? querySearch, int offset, int limit,
        string sortField, SortDirection sortDirection)
    {
        var query = _artists.Query();
        query = query.Where(x => x.UserId == userId);
        if (!string.IsNullOrWhiteSpace(querySearch))
        {
            query = query.Where(x => x.Item.Name.Contains(querySearch));
        }

        var countTask = Task.Run(() => query.Count());
        var dataTask = Task.Run(() => query.OrderBy(sortField,
                sortDirection == SortDirection.Ascending ? Query.Ascending : Query.Descending)
            .Skip(offset)
            .Limit(limit)
            .ToList());
        await Task.WhenAll(countTask, dataTask);
        var count = countTask.Result;
        var artists = dataTask.Result;
        return new LibraryItems<SimpleArtistEntity>
        {
            Items = artists.Select(x => new LibraryItem<SimpleArtistEntity>
            {
                Item = x.Item,
                AddedAt = x.AddedAt
            }).ToList(),
            Total = count
        };
    }

    public bool ContainsAny(string user)
    {
        return _artists.Exists(x => x.UserId == user);
    }

    private sealed class ArtistLibraryItemEntity
    {
        public required SimpleArtistEntity Item { get; init; }
        public required DateTimeOffset AddedAt { get; init; }
        public required string UserId { get; set; }
    }
}

public interface ILibraryRepository
{
    Task<LibraryItems<SimpleArtistEntity>> GetArtists(
        string userId,
        string? querySearch, int offset, int limit, string sortField,
        SortDirection sortDirection);

    bool ContainsAny(string user);
}