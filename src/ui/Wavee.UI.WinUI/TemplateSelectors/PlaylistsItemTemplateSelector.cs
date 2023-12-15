using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Features.Playlists.ViewModel;

namespace Wavee.UI.WinUI.TemplateSelectors;

public sealed class PlaylistsItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? PlaylistTemplate { get; set; }
    public DataTemplate? FolderTemplate { get; set; }
    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item switch
        {
            FolderSidebarItemViewModel _ => FolderTemplate,
            PlaylistSidebarItemViewModel _ => PlaylistTemplate,
        };
    }
}