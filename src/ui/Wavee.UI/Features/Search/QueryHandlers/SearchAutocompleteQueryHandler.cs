using Mediator;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;
using Wavee.UI.Features.Search.Queries;
using Wavee.UI.Features.Search.ViewModels;

namespace Wavee.UI.Features.Search.QueryHandlers;

public sealed class SearchAutocompleteQueryHandler : IQueryHandler<SearchAutocompleteQuery, IReadOnlyCollection<SearchSuggestionViewModel>>
{
    private readonly ISpotifyClient _spotifyClient;

    public SearchAutocompleteQueryHandler(ISpotifyClient spotifyClient)
    {
        _spotifyClient = spotifyClient;
    }

    public async ValueTask<IReadOnlyCollection<SearchSuggestionViewModel>> Handle(SearchAutocompleteQuery query, CancellationToken cancellationToken)
    {
        var items = await _spotifyClient.Search.Autocomplete(query.Query, cancellationToken);
        var suggestions = new List<SearchSuggestionViewModel>();
        foreach (var suggestion in items.Queries)
        {
            suggestions.Add(new SearchSuggestionQueryViewModel
            {
                Terms = suggestion.Segments,
                Query = suggestion.Query
            });
        }

        foreach (var suggestion in items.Hits)
        {
            suggestions.Add(new SearchSuggestionEntityViewModel
            {
                Id = suggestion.Id.ToString(),
                Type = suggestion.Id.Type,
                Name = suggestion.Name,
                Subtitle = suggestion.Id.Type.ToString(),
                ImageUrl = ConstructUrl(suggestion.ImageUrl)
            });
        }

        return suggestions;
    }

    private string? ConstructUrl(string suggestionImageUrl)
    {
        if (suggestionImageUrl.StartsWith("spotify:image:"))
        {
            var id = suggestionImageUrl.Substring("spotify:image:".Length);
            return $"https://i.scdn.co/image/{id}";
        }

        return suggestionImageUrl;
    }
}