// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.Mvvm.Input;
using Eum.UI.Spotify.ViewModels.Users;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Login
{
    public sealed partial class SpotifyLoginPage : UserControl
    {
        public static readonly DependencyProperty GoBackCommandProperty = DependencyProperty.Register(nameof(GoBackCommand), typeof(ICommand), typeof(SpotifyLoginPage), new PropertyMetadata(default(ICommand)));

        public SpotifyLoginPage()
        {
            this.InitializeComponent();
        }

        public ICommand GoBackCommand
        {
            get => (ICommand) GetValue(GoBackCommandProperty);
            set => SetValue(GoBackCommandProperty, value);
        }

        public SignInToSpotifyViewModel ViewModel => (SignInToSpotifyViewModel) DataContext;
    }
}
