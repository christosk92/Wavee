// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Forms;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Login;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Profile
{
    public sealed partial class CurrentUserProfileCardView : UserControl
    {
        public CurrentUserProfileCardView()
        {
            ProfilesViewModel = Ioc.Default.GetRequiredService<ProfilesViewModel>();
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }
        public ProfilesViewModel ProfilesViewModel { get; }

        public UserManagerViewModel ViewModel => Ioc.Default.GetRequiredService<UserManagerViewModel>();
    }
}
