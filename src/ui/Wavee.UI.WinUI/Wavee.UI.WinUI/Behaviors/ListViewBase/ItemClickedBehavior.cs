using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;
using System.Windows.Input;

namespace Wavee.UI.WinUI.Behaviors.ListViewBase;

public class ItemClickedBehavior : Behavior<Microsoft.UI.Xaml.Controls.ListViewBase>
{
    public static readonly DependencyProperty ItemClickedCommandProperty =
        DependencyProperty.Register(
            "ItemClickedCommand",
            typeof(ICommand),
            typeof(ItemClickedBehavior),
            new PropertyMetadata(null));

    public ICommand ItemClickedCommand
    {
        get { return (ICommand)GetValue(ItemClickedCommandProperty); }
        set { SetValue(ItemClickedCommandProperty, value); }
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ItemClick += OnItemClick;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.ItemClick -= OnItemClick;
    }

    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        ItemClickedCommand.Execute(e.ClickedItem);
    }
}