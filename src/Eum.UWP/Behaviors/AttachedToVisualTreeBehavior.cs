using System.Reactive.Disposables;
using Windows.UI.Xaml;

namespace Eum.UWP.Behaviors;

public abstract class AttachedToVisualTreeBehavior<T> : DisposingBehavior<T> where T : UIElement
{
	private CompositeDisposable? _disposables;

	protected override void OnAttached(CompositeDisposable disposables)
	{
		_disposables = disposables;
	}

    protected override void OnAssociatedObjectLoaded()
    {
        OnAttachedToVisualTree(_disposables!);
    }


	protected abstract void OnAttachedToVisualTree(CompositeDisposable disposable);
}
