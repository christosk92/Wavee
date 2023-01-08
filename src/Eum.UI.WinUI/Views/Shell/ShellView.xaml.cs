// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.ViewModels.Navigation;
using Eum.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Composition;
using Eum.UI.Services.Users;
using Eum.UI.Users;
using WinRT;
using ColorCode.Compilation.Languages;
using Eum.Connections.Spotify.Models.Views;
using Microsoft.UI.Xaml.Shapes;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Shell
{
    public sealed partial class ShellView : UserControl
    {
        public ShellView()
        {
            UserManager = Ioc.Default.GetRequiredService<IEumUserViewModelManager>();
            ViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }

        public IEumUserViewModelManager UserManager { get; }
        public MainViewModel ViewModel { get; }

        private void NavigationView_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer.DataContext is INavigatable routable)
            {
                NavigationService.Instance.To(routable);
            }
        }

        private void Sidepanel_CloseRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            ViewModel.SidePanelView = null;
            ViewModel.ShouldShowSidePanel = false;
            ViewModel.LyricsViewModel = null;
        }

        public bool IsSidePanelView(string s, string s1)
        {
            return s == s1;
        }

        public object GetSelectedItem(string s)
        {
            switch (s)
            {
                case "queue":
                    return SidebarNavView.MenuItems[1];
                case "lyrics":
                    return SidebarNavView.MenuItems[0];
                    
            }

            return default;
        }

        public string GetTitle(string s)
        {
            switch (s)
            {
                case "queue":
                    return "Up next";
                case "lyrics":
                    return "Lyrics";
            }

            return string.Empty;
        }

        private void SidebarNavView_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            ViewModel.SidePanelView = args.InvokedItemContainer.Tag?.ToString();
        }
    }
}
