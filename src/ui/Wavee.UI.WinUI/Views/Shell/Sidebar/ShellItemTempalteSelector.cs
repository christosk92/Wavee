using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Shell.Sidebar;

namespace Wavee.UI.WinUI.Views.Shell.Sidebar;

internal sealed class ShellItemTempalteSelector : DataTemplateSelector
{
    public DataTemplate GeneralSidebar { get; set; }
    public DataTemplate HeaderSidebar { get; set; }
    public DataTemplate CountedSidebar { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            GeneralSidebarItem => GeneralSidebar,
            HeaderSidebarItem => HeaderSidebar,
            CountedSidebarItem => CountedSidebar,
            _ => base.SelectTemplateCore(item, container)
        };
    }
}