namespace Wavee.UI.Navigation;

internal sealed class NavigationContext : INavigationContext
{
    private readonly IViewFactory _viewFactory;
    private readonly IFrameFacade _frameFacade;
    private readonly IDialogFacade _dialogFacade;

    public NavigationContext(
        IViewFactory viewFactory,
        IFrameFacade frameFacade,
        IDialogFacade dialogFacade)
    {
        _viewFactory = viewFactory;
        _frameFacade = frameFacade;
        _dialogFacade = dialogFacade;

        _frameFacade.NavigatedTo += (sender, args) =>
        {
            object? vmUnwrapped = null;
            args.ViewModel.Match(
                actionWhenNone: () => { },
                actionWhenSome: x => { vmUnwrapped = x; }
            );

            NavigatedTo?.Invoke(sender, vmUnwrapped);
        };
    }

    public event EventHandler<object?> NavigatedTo;

    public ValueTask<bool> NavigateTo<T>(T viewModel,
        NavigationAnimationType animationType = NavigationAnimationType.None)
    {
        var view = _viewFactory.ViewType<T>();
        bool done = false;
        ValueTask<bool> result = new ValueTask<bool>(false);
        view.Match(
            actionWhenNone: () => { },
            actionWhenSome: t =>
            {
                switch (t.DisplayType)
                {
                    case ViewType.Page:
                        _frameFacade.NavigateTo(t.ViewType, viewModel, animationType);
                        result = new ValueTask<bool>(true);
                        break;
                    case ViewType.Dialog:
                        result = new ValueTask<bool>(_dialogFacade.OpenDialog(t.ViewType, viewModel)
                            .ContinueWith(_ => true));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        );

        return result;
    }
}

public interface INavigationContext
{
    ValueTask<bool> NavigateTo<T>(T viewModel, NavigationAnimationType animationType = NavigationAnimationType.None);
    event EventHandler<object?> NavigatedTo;
}