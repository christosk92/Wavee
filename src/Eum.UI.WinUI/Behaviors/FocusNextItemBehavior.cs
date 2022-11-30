using System.Reactive.Disposables;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Eum.UI.WinUI.Behaviors;

internal class FocusNextItemBehavior : DisposingBehavior<Control>
{
    public static readonly DependencyProperty IsFocusedProperty = DependencyProperty.Register(nameof(IsFocused), typeof(bool), typeof(FocusNextItemBehavior), new PropertyMetadata(default(bool), IsFocusedChanged));

    private static void IsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = d as FocusNextItemBehavior;
        if (e.NewValue is false)
        {
            var associatedObject = behavior.AssociatedObject;
            var parentControl = associatedObject.FindAscendant<ItemsControl>();

            if (parentControl is { })
            {
                foreach (var item in parentControl.FindDescendants())
                {
                    var nextToFocus = item.FindDescendant<TextBox>();
                    if (nextToFocus != null)
                    {
                        if (nextToFocus.IsEnabled)
                        {
                            nextToFocus.Focus(FocusState.Programmatic);
                            return;
                        }
                    }
                }

                parentControl.Focus(FocusState.Programmatic);
            }
        }
    }
    // public static readonly StyledProperty<bool> IsFocusedProperty =
	// 	AvaloniaProperty.Register<FocusNextItemBehavior, bool>(nameof(IsFocused), true);

	// public bool IsFocused
	// {
	// 	get => GetValue(IsFocusedProperty);
	// 	set => SetValue(IsFocusedProperty, value);
	// }

    public bool IsFocused
    {
        get => (bool) GetValue(IsFocusedProperty);
        set => SetValue(IsFocusedProperty, value);
    }

    protected override void OnAttached(CompositeDisposable disposables)
    {
    }
}
