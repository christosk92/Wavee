// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Windows.Input;
using Eum.UI.Items;
using Eum.UI.Users;
using Eum.UI.ViewModels.Users;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Login
{
    public sealed partial class ProfilesView : UserControl
    {
        public static readonly DependencyProperty LoginViewModelProperty = DependencyProperty.Register(nameof(LoginViewModel), typeof(LoginViewModel), typeof(ProfilesView), new PropertyMetadata(default(LoginViewModel)));

        public ProfilesView()
        {
            this.InitializeComponent();
        }


        public LoginViewModel LoginViewModel
        {
            get => (LoginViewModel)GetValue(LoginViewModelProperty);
            set => SetValue(LoginViewModelProperty, value);
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            LoginViewModel.AddUserCommand.Execute(ServiceType.Spotify);
        }

        private async void ListViewBase_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as EumUserViewModel;
            await LoginViewModel.Login(item, CancellationToken.None);
        }
    }
}
