using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Models.Common;

namespace Wavee.UI.WinUI.TemplateSelectors;

public sealed class HomeItemTemplateSelector : DataTemplateSelector
{
    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            CardViewItem v => v.Id.Type switch
            {
                AudioItemType.Artist => Artist,
                _ => Normal
            },
            _ => Normal
        };
    }
    public DataTemplate Artist { get; set; }
    public DataTemplate Normal { get; set; }
}