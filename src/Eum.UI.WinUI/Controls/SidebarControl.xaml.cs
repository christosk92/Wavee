// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI;
using Eum.UI.Services.Playlists;
using Eum.UI.Services.Users;
using Eum.UI.Users;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Navigation;
using Eum.UI.ViewModels.Playlists;
using Eum.UI.ViewModels.Search;
using Eum.UI.ViewModels.Users;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Eum.Users;
using Eum.UI.ViewModels.Sidebar;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Controls
{
    public sealed partial class SidebarControl : NavigationView
    {
        private SearchRootViewModel? _searchRootViewModel;

        private readonly IEumUserViewModelManager _userManagerViewModel;
        public SidebarControl()
        {
            _userManagerViewModel = Ioc.Default.GetRequiredService<IEumUserViewModelManager>();
            this.InitializeComponent();
        }
        
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(SidebarViewModel),
            typeof(SidebarControl), new PropertyMetadata(default(SidebarViewModel)));

        public static readonly DependencyProperty TabContentProperty = DependencyProperty.Register(nameof(TabContent), typeof(UIElement), typeof(SidebarControl), new PropertyMetadata(null));
        public static readonly DependencyProperty UserProperty = DependencyProperty.Register(nameof(User), typeof(EumUserViewModel),
            typeof(SidebarControl), new PropertyMetadata(default(EumUserViewModel), UserChanged));

        public static readonly DependencyProperty SearchBarProperty = DependencyProperty.Register(nameof(SearchBar), typeof(SearchBarViewModel), typeof(SidebarControl), new PropertyMetadata(default(SearchBarViewModel)));

        private static void UserChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sidebar = (d as SidebarControl);
            sidebar.ViewModel?.Deconstruct();
            if (e.NewValue is EumUserViewModel v)
            {
                sidebar.ViewModel = new SidebarViewModel(v, Ioc.Default.GetRequiredService<IEumUserPlaylistViewModelManager>());
                sidebar.OpenPaneLength = sidebar.NullOrSidebarWidth ?? sidebar.OpenPaneLength;
                sidebar._searchRootViewModel = new SearchRootViewModel(sidebar.SearchBar);
            }

            GC.Collect();
        }


        public UIElement TabContent
        {
            get => (UIElement)GetValue(TabContentProperty);
            set => SetValue(TabContentProperty, value);
        }

        public EumUserViewModel User
        {
            get => (EumUserViewModel)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }
        public SidebarViewModel ViewModel
        {
            get => (SidebarViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }
        // private void PaneRoot_RightTapped(object sender, RightTappedRoutedEventArgs e)
        // {
        //     var contextMenu = FlyoutBase.GetAttachedFlyout(this);
        //     contextMenu.ShowAt(this, new FlyoutShowOptions() { Position = e.GetPosition(this) });
        //
        //     e.Handled = true;
        // }

        private void SidebarNavView_Loaded(object sender, RoutedEventArgs e)
        {
            (this.FindDescendant("TabContentBorder") as Border).Child = TabContent;
        }

        private void SidebarControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            IsPaneToggleButtonVisible = args.DisplayMode == NavigationViewDisplayMode.Minimal;
        }

        internal double? NullOrSidebarWidth
        {
            get
            {
                if (User?.User == null) return null;
                if (User.User.SidebarWidth == 0) return null;

                return User.User.SidebarWidth;
            }
            set
            {
                User.User.SidebarWidth = value.Value;
            }
        }

        public SearchBarViewModel SearchBar
        {
            get => (SearchBarViewModel) GetValue(SearchBarProperty);
            set => SetValue(SearchBarProperty, value);
        }


        private void SidebarControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.OpenPaneLength = NullOrSidebarWidth ?? OpenPaneLength;
        }

        private void AddPlaylist_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (NavigationService.Instance.Current is not CreatePlaylistViewModel)
            {
                NavigationService.Instance.To(new CreatePlaylistViewModel(Ioc.Default.GetRequiredService<IEumUserViewModelManager>()));
            }
        }

        private void UIElement_OnGotFocus(object sender, RoutedEventArgs e)
        {
            _searchRootViewModel.ForceShow(true);
        }
    }
}