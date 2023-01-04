using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.Services.Users;
using Eum.UI.ViewModels.Navigation;
using Eum.UI.ViewModels.Settings;
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
    public sealed partial class CurrentUserProfileCardView : UserControl
    {
        public CurrentUserProfileCardView()
        {
            UserViewModel = Ioc.Default.GetRequiredService<IEumUserViewModelManager>();
            this.InitializeComponent();
            this.DataContext = UserViewModel;
        }
        public IEumUserViewModelManager UserViewModel { get; }
        private void Settings_tapped(object sender, TappedRoutedEventArgs e)
        {
            NavigationService.Instance.To(new SettingsViewModel(UserViewModel.CurrentUser.User));
        }
    }
}
