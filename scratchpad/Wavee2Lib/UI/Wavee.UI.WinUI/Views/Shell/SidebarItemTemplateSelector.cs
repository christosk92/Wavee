using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI.Views.Shell;

class SidebarItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate Regular { get; set; }
    public DataTemplate Counted { get; set; }
    public DataTemplate Header { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        var sidebar = (SidebarItem)item;
        if(sidebar.IsAHeader) return Header;
        return sidebar.IsCountable ? Counted : Regular;
    }
}