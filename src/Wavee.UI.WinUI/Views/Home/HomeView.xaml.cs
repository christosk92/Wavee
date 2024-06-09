using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels;
using HomeViewModel = Wavee.UI.ViewModels.Home.HomeViewModel;


namespace Wavee.UI.WinUI.Views.Home;

public sealed partial class HomeView : UserControl
{
    public HomeView()
    {
        this.InitializeComponent();
    }

    public HomeViewModel ViewModel => (HomeViewModel)DataContext;
}