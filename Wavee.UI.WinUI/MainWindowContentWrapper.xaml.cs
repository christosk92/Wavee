using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModels.Identity;

namespace Wavee.UI.WinUI;

public sealed partial class MainWindowContentWrapper : UserControl
{
    public MainWindowContentWrapper()
    {
        this.InitializeComponent();
        NavigationService.SetRoot<SignInViewModel>();
    }

    public NavigationService NavigationService => NavigationService.Instance;
}