using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModel.Playlist.Headers;

namespace Wavee.UI.WinUI.UI.TemplateSelectors;

public class PlaylistHeaderTemplateSelector : DataTemplateSelector
{
    public DataTemplate LoadingHeaderTemplate { get; set; }
    public DataTemplate RegularHeaderTemplate { get; set; }
    public DataTemplate BigHeaderTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            null or LoadingPlaylistHeader => LoadingHeaderTemplate,
            RegularPlaylistHeader => RegularHeaderTemplate,
            PlaylistBigHeader => BigHeaderTemplate,
        };
    }
}