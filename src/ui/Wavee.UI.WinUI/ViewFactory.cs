using Microsoft.UI.Xaml;
using System;
using Wavee.UI.ViewModel.Home;
using Wavee.UI.ViewModel.Playlist;
using Wavee.UI.ViewModel.Setup;
using Wavee.UI.ViewModel.Shell;
using Wavee.UI.ViewModel.Wizard;
using Wavee.UI.WinUI.View;
using Wavee.UI.WinUI.View.Home;
using Wavee.UI.WinUI.View.Playlist;
using Wavee.UI.WinUI.View.Setup;
using Wavee.UI.WinUI.View.Shell;

namespace Wavee.UI.WinUI;
public static class ViewFactory
{
    public static UIElement ConstructFromViewModel(object viewModel)
    {
        return (UIElement)(viewModel switch
        {
            IdentityViewModel id => new SignInView(id),
            ShellViewModel shell => new ShellView(shell),
        });
    }

    internal static Type GetTypeFromViewModel(object vm)
    {
        return vm switch
        {
            WelcomeViewModel => typeof(WelcomePage),
            IdentityViewModel => typeof(SignInView),
            SettingEverythingUpViewModel => typeof(SettingEverythingUpView),
            OptInViewModel => typeof(OptInView),
            YouAreGoodToGoViewModel => typeof(YoureGoodToGoView),
        };
    }

    internal static Type GetTypeFromViewModel(Type vm)
    {
        return vm switch
        {
            _ when vm == typeof(HomeViewModel) => typeof(HomeView),
            _ when vm == typeof(PlaylistViewModel) => typeof(PlaylistView),
        };
    }
}
