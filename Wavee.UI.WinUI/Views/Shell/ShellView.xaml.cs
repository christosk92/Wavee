using System;
using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using System.Numerics;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Models.Profiles;
using Wavee.UI.ViewModels.Shell;
using Wavee.UI.ViewModels.User;
using Wavee.UI.WinUI.Interfaces.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Shell
{
    public sealed partial class ShellView : UserControl
    {
        public ShellView(Profile profile)
        {
            ViewModel = new ShellViewModel(new UserViewModel(profile), Ioc.Default.GetRequiredService<IStringLocalizer>());
            NavigationViewService = Ioc.Default.GetRequiredService<INavigationViewService>();
            this.InitializeComponent();
            NavigationViewService.Initialize(navigationView: SidebarNavView);
            Ioc.Default.GetRequiredService<INavigationService>().SetFrame(NavigationFrame);
        }

        public INavigationViewService NavigationViewService { get; }
        public ShellViewModel ViewModel { get; }
        public bool IsDebug => Debugger.IsAttached;

        public Vector3 GetVectorFromHeight(double d)
        {
            return new Vector3(0, (float)-d, 0);
        }

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            GC.Collect();
        }

        private void GCTApped(object sender, TappedRoutedEventArgs e)
        {
            GC.Collect();
        }
    }
}
