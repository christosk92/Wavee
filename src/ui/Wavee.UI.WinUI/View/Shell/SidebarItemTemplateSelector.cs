using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModel.Shell.Sidebar;

namespace Wavee.UI.WinUI.View.Shell;

internal sealed class SidebarItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate HeaderSidebarItemTemplate { get; set; }
    public DataTemplate RegularSidebarItemTemplate { get; set; }
    public DataTemplate CountedSidebarItemTemplate { get; set; }

    public DataTemplate PlaylistSidebarItemTemplate { get; set; }


    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            HeaderSidebarItem => HeaderSidebarItemTemplate,
            RegularSidebarItem => RegularSidebarItemTemplate,
            CountedSidebarItem => CountedSidebarItemTemplate,
            PlaylistSidebarItem => PlaylistSidebarItemTemplate,
            _ => base.SelectTemplateCore(item, container)
        };
    }
}