using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Sidebar;

namespace Wavee.UI.WinUI.Views.Sidebar;

class SidebarItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate Regular { get; set; }
    public DataTemplate Counted { get; set; }
    public DataTemplate Header { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            HeaderSidebarItem => Header,
            CountedSidebarItem => Counted,
            RegularSidebarItem => Regular,
            _ => base.SelectTemplateCore(item, container)
        };
    }
}