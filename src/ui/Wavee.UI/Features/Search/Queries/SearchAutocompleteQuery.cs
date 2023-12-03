using Mediator;
using Wavee.UI.Features.Search.ViewModels;

namespace Wavee.UI.Features.Search.Queries;

public sealed class SearchAutocompleteQuery : IQuery<IReadOnlyCollection<SearchSuggestionViewModel>>
{
    public string? Query { get; init; }
}