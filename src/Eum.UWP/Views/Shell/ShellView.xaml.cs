using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.Services.Users;
using Eum.UI.ViewModels.Navigation;
using Eum.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Eum.UWP.Views.Shell
{
    public sealed partial class ShellView : UserControl
    {
        public ShellView()
        {
            UserManager = Ioc.Default.GetRequiredService<IEumUserViewModelManager>();
            ViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        public IEumUserViewModelManager UserManager { get; }
        public MainViewModel ViewModel { get; }

        private void NavigationView_OnItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer.DataContext is INavigatable routable)
            {
                NavigationService.Instance.To(routable);
            }
        }
    }
}
