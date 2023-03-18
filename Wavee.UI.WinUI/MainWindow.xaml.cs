using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Wavee.UI.Models.Profiles;
using Wavee.UI.WinUI.Views.Login;
using Wavee.UI.WinUI.Views.Shell;
using WinUIEx;

namespace Wavee.UI.WinUI
{
    public sealed partial class MainWindow : WindowEx
    {
        public MainWindow()
        {
            this.InitializeComponent();
            var loginView = new LoginView();
            loginView.SignedIn += LoginView_OnSignedIn;
            this.ContentData.Content = loginView;
            this.SystemBackdrop = new MicaBackdrop();
            this.ExtendsContentIntoTitleBar = true;

            if (loginView.ViewModel.IsSignedIn)
            {
                LoginView_OnSignedIn(null, loginView.ViewModel.SignedInProfile!.Value);
            }
        }


        private void LoginView_OnSignedIn(object sender, Profile e)
        {
            if (this.ContentData.Content is LoginView v)
            {
                v.LoginView_OnUnloaded(null, null);
                v.SignedIn -= LoginView_OnSignedIn;
                this.ContentData.Content = null;
            }
            this.ContentData.Content = new ShellView(e);
        }
    }
}
