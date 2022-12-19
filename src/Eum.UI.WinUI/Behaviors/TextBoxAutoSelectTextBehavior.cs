using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.Foundation;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ReactiveUI;

namespace Eum.UI.WinUI.Behaviors;

internal class TextBoxAutoSelectTextBehavior : AttachedToVisualTreeBehavior<AutoSuggestBox>
{
	protected override void OnAttachedToVisualTree(CompositeDisposable disposable)
	{
		if (AssociatedObject is null)
		{
			return;
		}
        RoutedEventHandler? _s = default;
        var gotFocus = Observable.FromEventPattern<RoutedEventArgs>(
            add =>
            {
                _s = new RoutedEventHandler(add);
                AssociatedObject.GotFocus += _s;
            },
            remove =>
            {
                AssociatedObject.GotFocus -= _s;
            });
        RoutedEventHandler? _t = default;
        //var gotFocus = AssociatedObject.OnEvent<GettingFocusEventArgs>(UIElement.GettingFocusEvent);
        var lostFocus = Observable.FromEventPattern<RoutedEventArgs>(
            add =>
            {
                _t = new RoutedEventHandler(add);
                AssociatedObject.LostFocus += _t;
            },
            remove =>
            {
                AssociatedObject.LostFocus -= _t;
            });
        var isFocused = gotFocus.Select(_ => true).Merge(lostFocus.Select(_ => false));

		isFocused
			.Throttle(TimeSpan.FromSeconds(0.1))
			.DistinctUntilChanged()
			.ObserveOn(RxApp.MainThreadScheduler)
			.Where(focused => focused)
			.Do(_ =>
            {
                var item = AssociatedObject.FindDescendant<TextBox>();
                item.SelectAll();
            })
			.Subscribe()
			.DisposeWith(disposable);
	}

    private void AssociatedObjectOnLostFocus(object sender, RoutedEventArgs e)
    {
      
    }

    private void AssociatedObjectOnGotFocus(object sender, RoutedEventArgs e)
    {
        
    }
}
