using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace Wavee.UI.WinUI.Utils;
public static class ListViewProperties
{
    public static readonly DependencyProperty IsItemSelectedProperty =
        DependencyProperty.RegisterAttached("IsItemSelected", typeof(bool), typeof(ListViewProperties), new PropertyMetadata(false, OnIsItemSelectedChanged));

    public static bool GetIsItemSelected(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsItemSelectedProperty);
    }

    public static void SetIsItemSelected(DependencyObject obj, bool value)
    {
        obj.SetValue(IsItemSelectedProperty, value);
    }

    private static void OnIsItemSelectedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        var listView = obj as ListViewBase;
        if (listView != null)
        {
            if ((bool)e.NewValue)
            {
                listView.ContextFlyout = CreateMultipleItemsContextMenu();
            }
            else
            {
                listView.ContextFlyout = CreateSingleItemContextMenu();
            }
        }
    }

    private static MenuFlyout CreateSingleItemContextMenu()
    {
        var contextMenu = new MenuFlyout();

        var editMenuItem = new MenuFlyoutItem() { Text = "Edit" };
        var deleteMenuItem = new MenuFlyoutItem() { Text = "Delete" };
        contextMenu.Items.Add(editMenuItem);
        contextMenu.Items.Add(deleteMenuItem);
        return contextMenu;
    }

    private static MenuFlyout CreateMultipleItemsContextMenu()
    {
        var contextMenu = new MenuFlyout();
        var editSelectedMenuItem = new MenuFlyoutItem() { Text = "Edit Selected" };
        var deleteSelectedMenuItem = new MenuFlyoutItem() { Text = "Delete Selected" };
        contextMenu.Items.Add(editSelectedMenuItem);
        contextMenu.Items.Add(deleteSelectedMenuItem);
        return contextMenu;
    }
}

