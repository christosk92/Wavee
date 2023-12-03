using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.Spotify.Common;
using Wavee.UI.Features.Search.ViewModels;

namespace Wavee.UI.WinUI.TemplateSelectors;

public sealed class SearchSuggestionsItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate ArtistTemplate { get; set; } = null!;
    public DataTemplate GenericTemplate { get; set; } = null!;
    public DataTemplate QueryTemplate { get; set; } = null!;

    override protected DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            SearchSuggestionEntityViewModel v => v.Type switch
            {
                SpotifyItemType.Artist => ArtistTemplate,
                SpotifyItemType.Unknown => ArtistTemplate,
                _ => GenericTemplate
            },
            SearchSuggestionQueryViewModel => QueryTemplate,
            _ => QueryTemplate
        };
    }
}