using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI.Common.ViewSelectors;

public sealed class PageViewSelector : DataTemplateSelector
{
    protected override DataTemplate SelectTemplateCore(object item)
    {
        return base.SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return base.SelectTemplateCore(item, container);
    }
}