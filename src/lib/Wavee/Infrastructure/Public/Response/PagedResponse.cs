namespace Wavee.Infrastructure.Public.Response;

public sealed class PagedResponse<T>
{
    public required IReadOnlyCollection<T> Items { get; init; }
    public required int Total { get; init; }
    public required int Offset { get; init; }
    public required int Limit { get; init; }
    public required bool HasNextPage { get; init; }
}