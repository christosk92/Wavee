using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Playlists;

namespace Wavee.UI.WinUI.Views.Shell;

internal sealed class PlaylistsItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate FolderTemplate { get; set; }
    public DataTemplate PlaylistTemplate { get; set; }
    public DataTemplate PlaylistTemplateWithMargin { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var explorerItem = (PlaylistOrFolder)item;
        return explorerItem.Value.Match(
            Left: _ => FolderTemplate,
            Right: x => x.IsInFolder ? PlaylistTemplateWithMargin : PlaylistTemplate);

    }
}