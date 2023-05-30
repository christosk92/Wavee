using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Playlist;

namespace Wavee.UI.WinUI.Views.Sidebar;

internal sealed class PlaylistsItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate FolderTemplate { get; set; }
    public DataTemplate PlaylistTemplate { get; set; }
    public DataTemplate PlaylistTemplateWithMargin { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var explorerItem = (PlaylistViewModel)item;
        return explorerItem.IsFolder
            ? FolderTemplate
            : (explorerItem.IsInFolder ? PlaylistTemplate : PlaylistTemplateWithMargin);
    }
}