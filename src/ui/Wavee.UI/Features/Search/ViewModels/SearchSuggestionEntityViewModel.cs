using Wavee.Spotify.Common;

namespace Wavee.UI.Features.Search.ViewModels;

public sealed class SearchSuggestionEntityViewModel : SearchSuggestionViewModel
{
    public string Id { get; init; }
    public SpotifyItemType Type { get; init; }
    public string Name { get; init; }
    public string? Subtitle { get; init; }
    public string? ImageUrl { get; init; }
}