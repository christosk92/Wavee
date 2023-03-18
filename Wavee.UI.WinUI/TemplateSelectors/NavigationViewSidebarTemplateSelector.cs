using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Shell.Sidebar;

namespace Wavee.UI.WinUI.TemplateSelectors
{
    internal sealed class NavigationViewSidebarTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Header { get; set; }
        public DataTemplate Generic { get; set; }
        public DataTemplate Counted { get; set; }
        public DataTemplate PlaylistHeader { get; set; }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is not ISidebarItem s) return base.SelectTemplateCore(item, container);

            return s switch
            {
                SidebarItemHeader => Header,
                GenericSidebarItem => Generic,
                CountedSidebarItem => Counted,
                CreatePlaylistButtonSidebarItem => PlaylistHeader,
                _ => base.SelectTemplateCore(item, container)
            };
        }
    }
}
