using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI.TemplateSelectors;

public sealed class HomeCardTemplateSelector : DataTemplateSelector
{
    public DataTemplate Artist { get; set; }
    public DataTemplate Playlist { get; set; }
    public DataTemplate Album { get; set; }
    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            ISimpleArtist => Artist,
            ISimplePlaylist => Playlist,
            ISimpleAlbum => Album,
            _ => base.SelectTemplateCore(item, container)
        };
    }
}