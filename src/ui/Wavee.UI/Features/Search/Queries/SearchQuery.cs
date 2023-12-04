using Mediator;
using Wavee.UI.Features.Search.ViewModels;

namespace Wavee.UI.Features.Search.Queries;

public sealed class SearchQuery : IQuery<IReadOnlyCollection<SearchGroupViewModel>>
{
    public string? Query { get; init; }
}