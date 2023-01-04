using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Search;
using ReactiveUI;


namespace Eum.UWP.Behaviors;
public class ShowAttachedFlyoutWhenFocusedBehavior : AttachedToVisualTreeBehavior<AutoSuggestBox>
{
    public static readonly DependencyProperty IsSearchPageVisibleProperty = DependencyProperty.Register(nameof(IsSearchPageVisible), typeof(bool), typeof(ShowAttachedFlyoutWhenFocusedBehavior), new PropertyMetadata(default(bool)));

    protected override void OnAttachedToVisualTree(CompositeDisposable disposable)
    {
        FocusBasedFlyoutOpener(AssociatedObject).DisposeWith(disposable);

        var controller = new SearchRootViewModel(Ioc.Default.GetRequiredService<MainViewModel>().SearchBar);
        IsOpenPropertySynchronizer(controller).DisposeWith(disposable);


        // // EDGE CASES
        // // Edge case when the Visual Root becomes active and the Associated object is focused.
        // ActivateOpener(AssociatedObject, visualRoot, controller).DisposeWith(disposable);
        // DeactivateCloser(visualRoot, controller).DisposeWith(disposable);
    }
    private IDisposable IsOpenPropertySynchronizer(SearchRootViewModel controller)
    {
        return this
            .WhenAnyValue(x => x.IsSearchPageVisible)
            .Do(controller.ForceShow)
            .Subscribe();
    }

    public bool IsSearchPageVisible
    {
        get => (bool)GetValue(IsSearchPageVisibleProperty);
        set => SetValue(IsSearchPageVisibleProperty, value);
    }
    private IDisposable FocusBasedFlyoutOpener(
        AutoSuggestBox associatedObject)
    {
        RoutedEventHandler? _s = default;
        var associatedObjectGotFocus = Observable.FromEventPattern<RoutedEventArgs>(
            add =>
            {
                _s = new RoutedEventHandler(add);
                AssociatedObject.GotFocus += _s;
            },
            remove =>
            {
                AssociatedObject.GotFocus -= _s;
            });

        // var isAssociatedObjectFocused =
        //     new AvaloniaPropertyObservable<FocusState>(associatedObject, AutoSuggestBox.IsFocusEngagedProperty);

        var weAreFocused = associatedObjectGotFocus
            .Throttle(TimeSpan.FromSeconds(0.1))
            .DistinctUntilChanged();

        return weAreFocused
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(isOpen =>
            {
                if (IsSearchPageVisible)
                {
                    IsSearchPageVisible = false;
                }

                IsSearchPageVisible = true;
            })
            .Subscribe();
    }

}
