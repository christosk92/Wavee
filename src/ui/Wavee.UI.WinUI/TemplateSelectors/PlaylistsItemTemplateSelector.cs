using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Playlists;

namespace Wavee.UI.WinUI.TemplateSelectors;

internal sealed class PlaylistsItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate FolderTemplate { get; set; }
    public DataTemplate PlaylistTemplate { get; set; }
    public DataTemplate PlaylistTemplateWithMargin { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var explorerItem = (PlaylistSubscription)item;
        return explorerItem.IsFolder
            ? FolderTemplate
            : (explorerItem.IsInFolder ? PlaylistTemplate : PlaylistTemplateWithMargin);
    }
}