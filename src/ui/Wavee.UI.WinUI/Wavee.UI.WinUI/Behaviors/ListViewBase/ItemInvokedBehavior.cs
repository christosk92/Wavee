using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Wavee.UI.WinUI.Behaviors.ListViewBase;

public class ItemInvokedBehavior : Behavior<Microsoft.UI.Xaml.Controls.ItemsView>
{
    public static readonly DependencyProperty ItemInvokedCommandProperty =
        DependencyProperty.Register(
            "ItemInvokedCommand",
            typeof(ICommand),
            typeof(ItemInvokedBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ItemSelectedCommandProperty = DependencyProperty.Register(nameof(ItemSelectedCommand), typeof(ICommand), typeof(ItemInvokedBehavior), new PropertyMetadata(default(ICommand)));

    public ICommand ItemInvokedCommand
    {
        get { return (ICommand)GetValue(ItemInvokedCommandProperty); }
        set { SetValue(ItemInvokedCommandProperty, value); }
    }

    public ICommand ItemSelectedCommand
    {
        get => (ICommand)GetValue(ItemSelectedCommandProperty);
        set => SetValue(ItemSelectedCommandProperty, value);
    }


    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.SelectionChanged += AssociatedObjectOnSelectionChanged;
        AssociatedObject.IsItemInvokedEnabled = true;
        AssociatedObject.ItemInvoked += OnItemClick;
    }

    private void AssociatedObjectOnSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        var selectedItem = sender.SelectedItem;
        ItemSelectedCommand?.Execute(selectedItem);
    }

    private void OnItemClick(ItemsView sender, ItemsViewItemInvokedEventArgs args)
    {
        ItemInvokedCommand.Execute(args.InvokedItem);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.SelectionChanged -= AssociatedObjectOnSelectionChanged;
        AssociatedObject.ItemInvoked -= OnItemClick;
    }


}