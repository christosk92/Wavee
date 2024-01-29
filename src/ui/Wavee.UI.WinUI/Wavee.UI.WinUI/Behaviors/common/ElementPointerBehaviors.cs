using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.Xaml.Interactivity;

namespace Wavee.UI.WinUI.Behaviors.common;

public sealed class ElementPointerBehaviors : Behavior<FrameworkElement>
{
    public static readonly DependencyProperty HasPointerProperty = DependencyProperty.Register(nameof(HasPointer), typeof(bool), typeof(ElementPointerBehaviors), new PropertyMetadata(default(bool)));

    protected override void OnAttached()
    {
        base.OnAttached();

        this.AssociatedObject.PointerEntered += AssociatedObjectOnPointerEntered;
        this.AssociatedObject.PointerExited += AssociatedObjectOnPointerExited;
    }

    protected override void OnDetaching()
    {
        this.AssociatedObject.PointerEntered -= AssociatedObjectOnPointerEntered;
        this.AssociatedObject.PointerExited -= AssociatedObjectOnPointerExited;

        HasPointer = false;
        base.OnDetaching();
    }

    private void AssociatedObjectOnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        HasPointer = false;
    }

    private void AssociatedObjectOnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        HasPointer = true;
    }
    public bool HasPointer
    {
        get => (bool)GetValue(HasPointerProperty);
        set => SetValue(HasPointerProperty, value);
    }
}
