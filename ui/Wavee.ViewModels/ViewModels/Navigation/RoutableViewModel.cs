using System.Reactive.Disposables;
using System.Windows.Input;
using ReactiveUI;
using Wavee.ViewModels.ViewModels.Dialogs.Base;
using Wavee.ViewModels.ViewModels.NavBar;

namespace Wavee.ViewModels.ViewModels.Navigation;

public abstract partial class RoutableViewModel : ViewModelBase, INavigatable
{
    private CompositeDisposable? _currentDisposable;

    [AutoNotify] private bool _isBusy;
    [AutoNotify] private bool _enableBack;
    [AutoNotify] private bool _enableCancel;
    [AutoNotify] private bool _enableCancelOnPressed;
    [AutoNotify] private bool _enableCancelOnEscape;
    [AutoNotify] private bool _isActive;

    protected RoutableViewModel()
    {
        BackCommand = ReactiveCommand.Create(() => Navigate().Back(), this.WhenAnyValue(model => model.IsBusy, b => !b));
        CancelCommand = ReactiveCommand.Create(() => Navigate().Clear());
    }

    public abstract string Title { get; protected set; }
    public NavigationTarget CurrentTarget { get; internal set; }

    public virtual NavigationTarget DefaultTarget => NavigationTarget.HomeScreen;
    public ICommand BackCommand { get; protected set; }
    public ICommand? NextCommand { get; protected set; }
    public ICommand? SkipCommand { get; protected set; }
    public ICommand CancelCommand { get; protected set; }
    private void DoNavigateTo(bool isInHistory)
    {
        if (_currentDisposable is { })
        {
            throw new Exception("Can't navigate to something that has already been navigated to.");
        }

        _currentDisposable = new CompositeDisposable();

        OnNavigatedTo(isInHistory, _currentDisposable);
    }
    public INavigationStack<RoutableViewModel> Navigate()
    {
        var currentTarget = CurrentTarget == NavigationTarget.Default ? DefaultTarget : CurrentTarget;

        return Navigate(currentTarget);
    }

    public INavigationStack<RoutableViewModel> Navigate(NavigationTarget currentTarget)
    {
        return UiContext.Navigate(currentTarget);
    }
    public void OnNavigatedTo(bool isInHistory)
    {
        DoNavigateTo(isInHistory);
    }

    protected virtual void OnNavigatedTo(bool isInHistory, CompositeDisposable disposables)
    {
    }

    private void DoNavigateFrom(bool isInHistory)
    {
        OnNavigatedFrom(isInHistory);

        _currentDisposable?.Dispose();
        _currentDisposable = null;
    }

    void INavigatable.OnNavigatedFrom(bool isInHistory)
    {
        DoNavigateFrom(isInHistory);
    }

    protected virtual void OnNavigatedFrom(bool isInHistory)
    {
    }
    public async Task<DialogResult<TResult>> NavigateDialogAsync<TResult>(DialogViewModelBase<TResult> dialog, NavigationTarget target, NavigationMode navigationMode = NavigationMode.Normal)
    {
        target = NavigationExtensions.GetTarget(this, target);

        return await UiContext.Navigate(target).NavigateDialogAsync(dialog, navigationMode);
    }

    protected void SetupCancel(bool enableCancel, bool enableCancelOnEscape, bool enableCancelOnPressed, bool escapeGoesBack = false)
    {
        EnableCancel = enableCancel;
        EnableCancelOnEscape = enableCancelOnEscape && !escapeGoesBack;
        EnableCancelOnPressed = enableCancelOnPressed;
    }
}