// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
using Eum.UI.ViewModels.Login;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.Globalization;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Login
{
    public sealed partial class ProfilesView : UserControl
    {
        public ProfilesView()
        {
            this.InitializeComponent();
        }

        public ProfilesViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<ProfilesViewModel>();


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
