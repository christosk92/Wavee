using Wavee.Spotify.Application.Search.Queries;

namespace Wavee.UI.Features.Search.ViewModels;

public sealed class SearchSuggestionQueryViewModel : SearchSuggestionViewModel
{
    public IReadOnlyCollection<SpotifyAutocompleteQuerySegment> Terms { get; init; }
    public string Query { get; init; }
}