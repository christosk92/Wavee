using Mediator;
using Wavee.UI.Domain.Library;

namespace Wavee.UI.Features.Library.Queries;

public abstract class GetLibraryItemsQuery<T, TSorting> : IQuery<LibraryItems<T>> where TSorting : struct, Enum
{
    // public required int Offset { get; init; }
    // public required int Limit { get; init; }
    public required IReadOnlyCollection<string> Search { get; init; }
    public required TSorting SortField { get; init; }
    public required bool SortDescending { get; init; }
}

public sealed class LibraryItems<T>
{
    public required IReadOnlyCollection<LibraryItem<T>> Items { get; init; }
    public required int Total { get; init; }
}
