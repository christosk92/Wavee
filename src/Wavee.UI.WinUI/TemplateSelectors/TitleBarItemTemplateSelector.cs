using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.TemplateSelectors;

public sealed class TitleBarItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate Normal { get; set; }
    public DataTemplate Search { get; set; }
    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            TitleBarTabViewModel tab => tab.Id switch
            {
                { } when tab.Id == Constants.SearchTabId => Search,
                _ => Normal
            },
            _ => base.SelectTemplateCore(item, container)
        };
    }
}