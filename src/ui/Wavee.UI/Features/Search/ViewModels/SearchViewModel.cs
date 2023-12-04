using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mediator;
using Wavee.UI.Features.Search.Queries;

namespace Wavee.UI.Features.Search.ViewModels;

public sealed class SearchViewModel : ObservableObject
{
    private string? _query;
    private readonly IMediator _mediator;

    public SearchViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public ObservableCollection<SearchSuggestionViewModel> Suggestions { get; } = new();
    public ObservableCollection<SearchGroupViewModel> Results { get; } = new();
    public string? Query
    {
        get => _query;
        set => SetProperty(ref _query, value);
    }

    public async Task SearchSuggestions()
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            Suggestions.Clear();
            return;
        }

        var result = await _mediator.Send(new SearchAutocompleteQuery
        {
            Query = _query
        });
        Suggestions.Clear();
        foreach (var suggestion in result)
        {
            Suggestions.Add(suggestion);
        }
    }

    public async Task Search()
    {
        if (string.IsNullOrEmpty(Query))
        {
            Results.Clear();
            return;
        }

        var result = await _mediator.Send(new SearchQuery
        {
            Query = _query
        });
        Results.Clear();
        foreach (var group in result)
        {
            Results.Add(group);
        }
    }
}