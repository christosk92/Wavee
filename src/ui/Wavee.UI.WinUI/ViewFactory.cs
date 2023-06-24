using Microsoft.UI.Xaml;
using System;
using Wavee.UI.ViewModel.Identity;
using Wavee.UI.ViewModel.Setup;
using Wavee.UI.ViewModel.Wizard;
using Wavee.UI.WinUI.View;
using Wavee.UI.WinUI.View.Setup;

namespace Wavee.UI.WinUI;
public static class ViewFactory
{
    public static UIElement ConstructFromViewModel(object viewModel)
    {
        return (UIElement)(viewModel switch
        {
            IdentityViewModel id => new SignInView(id)
        });
    }

    internal static Type GetTypeFromViewModel(object vm)
    {
        return vm switch
        {
            WelcomeViewModel => typeof(WelcomePage),
        };
    }
}
