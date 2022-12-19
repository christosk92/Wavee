using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Windows.System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Eum.UI.WinUI.Behaviors;

internal class SwallowEnterKeyBehavior : AttachedToVisualTreeBehavior<AutoSuggestBox>
{
	protected override void OnAttachedToVisualTree(CompositeDisposable disposable)
	{
		if (AssociatedObject is null)
		{
			return;
		}

		Observable
			.FromEventPattern<KeyRoutedEventArgs>(AssociatedObject, nameof(AutoSuggestBox.KeyDown))
			.Where(args => args.EventArgs.Key == VirtualKey.Enter)
			.Subscribe(pattern =>
            {
                pattern.EventArgs.Handled = true;
            })
			.DisposeWith(disposable);
	}
}