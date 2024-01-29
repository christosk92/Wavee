using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace Wavee.UI.WinUI.Behaviors.Image;
public class ImageEventsBehavior : Behavior<Microsoft.UI.Xaml.Controls.Image>
{
    public static readonly DependencyProperty ImageOpenedProperty = DependencyProperty.Register(nameof(ImageOpened), typeof(bool), typeof(ImageEventsBehavior), new PropertyMetadata(default(bool)));

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ImageOpened += AssociatedObjectOnImageOpened;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.ImageOpened -= AssociatedObjectOnImageOpened;
    }
    private async void AssociatedObjectOnImageOpened(object sender, RoutedEventArgs e)
    {
        await Task.Delay(50);
        ImageOpened = true;
    }

    public bool ImageOpened
    {
        get => (bool)GetValue(ImageOpenedProperty);
        set => SetValue(ImageOpenedProperty, value);
    }
}
