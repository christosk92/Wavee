using Mediator;
using Wavee.UI.Entities.Library;

namespace Wavee.UI.Features.Library.Queries;

public abstract class GetLibraryItemsQuery<T> : IQuery<TaskCompletionSource<LibraryItems<T>>>
{
    public required int Offset { get; init; }
    public required int Limit { get; init; }
    public required string? Search { get; init; }
    public required string SortField { get; init; }
    public required SortDirection SortDirection { get; init; }
}

public sealed class LibraryItems<T>
{
    public required IReadOnlyCollection<LibraryItem<T>> Items { get; init; }
    public required int Total { get; init; }
}

public enum SortDirection
{
    Ascending,
    Descending
}