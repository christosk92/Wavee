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
using System.Windows.Forms;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Web.WebView2.Core;
using Wavee.UI.Features.Identity.ViewModels;
using Wavee.Spotify.Application.Authentication.Modules;
using Windows.UI.WebUI;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Identity
{
    public sealed partial class SignInView : UserControl
    {
        public SignInView()
        {
            this.InitializeComponent();
        }

        public IdentityViewModel ViewModel => (IdentityViewModel)DataContext;

        private void MainWebView_OnNavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            if (args.Uri.ToString().StartsWith("http://127.0.0.1"))
            {
                ViewModel.OnRedirect(args.Uri);
            }
        }

        public GridLength IsLoadingToGridColumnWidth(bool isLoading)
        {
            return !isLoading ? new GridLength(1, GridUnitType.Star) : new GridLength(0, GridUnitType.Pixel);
        }
    }
}
