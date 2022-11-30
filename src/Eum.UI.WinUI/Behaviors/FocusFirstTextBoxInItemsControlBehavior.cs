using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace Eum.UI.WinUI.Behaviors;

internal class FocusFirstTextBoxInItemsControlBehavior : Behavior<ItemsControl>
{
	protected override void OnAttached()
	{
		base.OnAttached();

		AssociatedObject!.LayoutUpdated += OnLayoutUpdated;
	}

	protected override void OnDetaching()
	{
		base.OnDetaching();

		AssociatedObject!.LayoutUpdated -= OnLayoutUpdated;
	}

	private void OnLayoutUpdated(object? sender, object e)
	{
		AssociatedObject!.LayoutUpdated -= OnLayoutUpdated;
		AssociatedObject.FindDescendant<TextBox>()?.Focus(FocusState.Programmatic);
	}
}
