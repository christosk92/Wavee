namespace Wavee.UI.Navigation;

internal sealed class NavigationContextFactory : INavigationContextFactory
{
    public INavigationContext Create(IViewFactory viewFactory, IFrameFacade frameFacade, IDialogFacade dialogFacade)
    {
        return new NavigationContext(
            viewFactory: viewFactory,
            frameFacade: frameFacade,
            dialogFacade: dialogFacade
        );
    }
}

public interface INavigationContextFactory
{
    INavigationContext Create(IViewFactory viewFactory,
        IFrameFacade frameFacade,
        IDialogFacade dialogFacade
    );
}