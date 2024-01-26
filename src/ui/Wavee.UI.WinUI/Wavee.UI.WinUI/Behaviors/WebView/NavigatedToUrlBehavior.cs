using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace Wavee.UI.WinUI.Behaviors.WebView;

public sealed class NavigatedToUrlBehavior : BehaviorBase<WebView2>
{
    public static readonly DependencyProperty OnNavigatedProperty = DependencyProperty.Register(nameof(OnNavigated), typeof(IRelayCommand<string>), typeof(NavigatedToUrlBehavior), new PropertyMetadata(default(IRelayCommand<string>)));

    protected override void OnAssociatedObjectLoaded()
    {
        base.OnAssociatedObjectLoaded();

        this.AssociatedObject.NavigationCompleted += AssociatedObjectOnNavigationCompleted;
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        this.AssociatedObject.NavigationCompleted += AssociatedObjectOnNavigationCompleted;
    }

    protected override void OnDetaching()
    {
        this.AssociatedObject.NavigationCompleted -= AssociatedObjectOnNavigationCompleted;

        base.OnDetaching();
    }



    private void AssociatedObjectOnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        OnNavigated?.Execute(sender.Source.ToString());
    }

    public IRelayCommand<string> OnNavigated
    {
        get => (IRelayCommand<string>)GetValue(OnNavigatedProperty);
        set => SetValue(OnNavigatedProperty, value);
    }
}