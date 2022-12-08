using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Eum.UI.ViewModels.Playlists;
using Eum.UI.ViewModels.Sidebar;

namespace Eum.UWP.TemplateSelectors
{
    public class NavItemDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Header { get; set; }
        public DataTemplate Item { get; set; }
        public DataTemplate PlaylistHeader { get; set; }
        public DataTemplate Playlist { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return item switch
            {
                PlaylistViewModel => Playlist,
                SidebarPlaylistHeader => PlaylistHeader,
                SidebarItemHeader _ => Header,
                SidebarItemViewModel _=> Item,
                _ => base.SelectTemplateCore(item, container)
            };
        }
    }
}
