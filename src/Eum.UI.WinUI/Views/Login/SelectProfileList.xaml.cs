// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Eum.UI.ViewModels.Users;
using Eum.UI.ViewModels.Login;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Login
{
    public sealed partial class SelectProfileList : UserControl
    {
        public SelectProfileList()
        {
            this.InitializeComponent();
        }
        public UserViewModelBase SelectedItem
        {
            get => (UserViewModelBase)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public ProfilesViewModel ViewModel => (ProfilesViewModel)DataContext;

        private void MenuFlyout_Opening(object sender, object e)
        {
            var fl = sender as MenuFlyout;
            var cont = UsersLv.ItemFromContainer(fl.Target);

            if (cont == null)
                fl.Hide();
            else { SelectedItem = (UserViewModelBase)cont; }
        }

        /// <summary>
        /// A property that stores the page's selected item.
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object),
                typeof(UserList), new PropertyMetadata(null));

        private void Back_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.ShowProfiles = true;
        }
    }
}
