using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Wavee.UI.WinUI.TemplateSelectors;

class ComboBoxSelectedItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SelectedItemTemplate { get; set; }
    public DataTemplateSelector? SelectedItemTemplateSelector { get; set; }
    public DataTemplate? DropdownItemsTemplate { get; set; }
    public DataTemplateSelector? DropdownItemsTemplateSelector { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        var itemToCheck = container;

        // Search up the visual tree, stopping at either a ComboBox or
        // a ComboBoxItem (or null). This will determine which template to use
        while (itemToCheck is not null
               and not ComboBox
               and not ComboBoxItem)
            itemToCheck = VisualTreeHelper.GetParent(itemToCheck);

        // If you stopped at a ComboBoxItem, you're in the dropdown
        var inDropDown = itemToCheck is ComboBoxItem;

        return inDropDown
            ? DropdownItemsTemplate ?? DropdownItemsTemplateSelector?.SelectTemplate(item, container)
            : SelectedItemTemplate  ?? SelectedItemTemplateSelector?.SelectTemplate(item, container);
    }
}