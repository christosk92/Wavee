using Mediator;
using Wavee.Spotify.Domain.Common;

namespace Wavee.Spotify.Application.Search.Queries;

public sealed class SpotifySearchQuery : IQuery<SpotifySearchResult>
{
    public required string Query { get; init; }
    public required int Offset { get; init; }
    public required int Limit { get; init; }
    public required int NumberOfTopResults { get; init; }
}

public sealed class SpotifySearchResult
{
    public IReadOnlyCollection<SpotifySearchCategoryResult> Items { get; init; }
}

public sealed class SpotifySearchCategoryResult
{
    public required int Order { get; init; }
    public required ulong Total { get; init; }
    public required IReadOnlyCollection<ISpotifyItem> Items { get; init; }
    public required string Key { get; init; }
}