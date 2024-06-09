using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.Library.List;

namespace Wavee.UI.WinUI.TemplateSelectors;

public sealed class LibraryCategoryItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate Category { get; set; }
    public DataTemplate Pinned { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            LibraryCategoryViewModel _ => Category,
            PinnedItemViewModel _ => Pinned,
        };
    }
}