using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.ViewModels.Users;
using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace Eum.UWP.Views.Login
{
    public sealed partial class LoginView : UserControl
    {
        public LoginView()
        {
            ViewModel = Ioc.Default.GetRequiredService<LoginViewModel>();
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        public LoginViewModel ViewModel { get; }

        public double Multiply(double d, string factor)
        {
            //we input a string because on some machines, 1.5 gets parsed as 15...
            var f = float.Parse(factor, CultureInfo.InvariantCulture);
            return d * f;
        }

        private void LetsGetStartedBlock_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ProgressBorder.Width = Multiply((sender as FrameworkElement)!.ActualWidth, "1.5");
        }
    }
}
