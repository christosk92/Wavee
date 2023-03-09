using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.Shell;

namespace Wavee.UI.WinUI.TemplateSelectors
{
    internal class SidebarTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Header { get; set; }
        public DataTemplate Library { get; set; }
        public DataTemplate Playlist { get; set; }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return item switch
            {
                SidebarHeader => Header,
                LibraryViewModelFactory => Library,
                SidebarItemViewModel => Playlist,
                _ => base.SelectTemplateCore(item, container)
            };
        }
    }
}
