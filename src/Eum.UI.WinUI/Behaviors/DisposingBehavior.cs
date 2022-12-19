using System.Reactive.Disposables;
using CommunityToolkit.WinUI.UI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace Eum.UI.WinUI.Behaviors;

public abstract class DisposingBehavior<T> : BehaviorBase<T> where T : UIElement
{
	private CompositeDisposable? _disposables;

	protected override void OnAttached()
	{
		base.OnAttached();

		_disposables?.Dispose();

		_disposables = new CompositeDisposable();

		OnAttached(_disposables);
	}

	protected abstract void OnAttached(CompositeDisposable disposables);

	protected override void OnDetaching()
	{
		base.OnDetaching();

		_disposables?.Dispose();
	}
}
