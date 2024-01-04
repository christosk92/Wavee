using ReactiveUI;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModels.Shell;
using Wavee.UI.ViewModels.SignIn;

namespace Wavee.UI.ViewModels.App;

public sealed class AppViewModel : ReactiveObject
{
    public AppViewModel(
        IShellViewModelFactory shellViewModelFactory,
        SignInViewModel signIn,
        INavigationContext coreWindowNavigationContext)
    {
        SignIn = signIn;

        signIn.ProfileAuthenticated += async (sender, context) =>
        {
            var shell = shellViewModelFactory.Create(context);
            await coreWindowNavigationContext.NavigateTo(shell);
        };
    }

    public SignInViewModel SignIn { get; } 
}