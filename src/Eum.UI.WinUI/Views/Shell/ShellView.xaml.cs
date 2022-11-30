// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Navigation;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Shell
{
    public sealed partial class ShellView : UserControl
    {
        public ShellView()
        {
            UserManager = Ioc.Default.GetRequiredService<UserManagerViewModel>();
            ViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        public UserManagerViewModel UserManager { get; }
        public MainViewModel ViewModel { get; }

        private void NavigationView_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer.DataContext is RoutableViewModel routable)
            {
                NavigationState.Instance.HomeScreenNavigation.To(routable);
            }
        }
    }
}
