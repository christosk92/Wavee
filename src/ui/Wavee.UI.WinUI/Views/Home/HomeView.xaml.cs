using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Contracts;
using Wavee.UI.Home;
using Wavee.UI.WinUI.Navigation;

namespace Wavee.UI.WinUI.Views.Home
{
    public sealed partial class HomeView : UserControl, INavigablePage
    {
        public HomeView()
        {
            ViewModel = new HomeViewModel();
            this.InitializeComponent();
        }

        public bool ShouldKeepInCache(int depth)
        {
            return depth <= 3;
        }

        INavigableViewModel INavigablePage.ViewModel => ViewModel;

        public HomeViewModel ViewModel { get; }
    }
}
