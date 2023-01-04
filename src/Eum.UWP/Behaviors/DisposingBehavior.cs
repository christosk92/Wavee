using System.Reactive.Disposables;
using Windows.UI.Xaml;
using Microsoft.Toolkit.Uwp.UI.Behaviors;

namespace Eum.UWP.Behaviors;

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
