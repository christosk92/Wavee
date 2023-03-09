using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.UI.ViewModels.Identity.User;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Identity
{
    public sealed partial class UserProfileCard : UserControl
    {
        public UserProfileCard()
        {
            var UVM = Ioc.Default.GetRequiredService<UserManagerViewModel>();
            this.InitializeComponent();

            if (UVM.CurrentUserVal != null)
            {
                Name.Text = UVM.CurrentUserVal.DisplayName;
                if (!string.IsNullOrEmpty(UVM.CurrentUserVal.User.UserData.ProfilePicture))
                    Prf.ProfilePicture = new BitmapImage(new Uri(UVM.CurrentUserVal.User.UserData.ProfilePicture));
                else
                {
                    Prf.DisplayName = UVM.CurrentUserVal.DisplayName;
                }

                Product.Text = UVM.CurrentUserVal.User.UserData.Metadata["product"].ToUpper();
            }
        }
    }
}
