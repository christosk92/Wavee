using CommunityToolkit.Mvvm.DependencyInjection;
using Windows.UI.Xaml.Controls;
using Eum.UI.ViewModels;
using Windows.ApplicationModel.Core;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Eum.UWP
{
    public sealed partial class WindowContentWrapper : UserControl
    {
        public WindowContentWrapper()
        {
            ViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
            this.InitializeComponent();
            this.DataContext = ViewModel;
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
        }

        public MainViewModel ViewModel { get; }
    }
}
