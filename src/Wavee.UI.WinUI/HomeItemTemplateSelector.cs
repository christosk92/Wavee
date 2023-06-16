using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Core.Contracts.Common;

namespace Wavee.UI.WinUI;

public class HomeItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate Artist { get; set; }
    public DataTemplate Regular { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is CardItem cardItem)
        {
            return cardItem.Id.Type switch
            {
                AudioItemType.Artist => Artist,
                _ => Regular
            };
        }
        return base.SelectTemplateCore(item, container);
    }
}