// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
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
        private readonly IEumUserViewModelManager _userManagerViewModel;
        public SidebarControl()
        {
            _userManagerViewModel = Ioc.Default.GetRequiredService<IEumUserViewModelManager>();
            this.InitializeComponent();
        }
        /// <summary>
        /// true if the user is currently resizing the sidebar
        /// </summary>
        private bool dragging;

        private double originalSize = 0;

        private bool lockFlag = false;

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(SidebarViewModel),
            typeof(SidebarControl), new PropertyMetadata(default(SidebarViewModel)));

        public static readonly DependencyProperty TabContentProperty = DependencyProperty.Register(nameof(TabContent), typeof(UIElement), typeof(SidebarControl), new PropertyMetadata(null));
        public static readonly DependencyProperty UserProperty = DependencyProperty.Register(nameof(User), typeof(EumUserViewModel),
            typeof(SidebarControl), new PropertyMetadata(default(EumUserViewModel), UserChanged));

        private static void UserChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sidebar = (d as SidebarControl);
            sidebar.ViewModel?.Deconstruct();
            if (e.NewValue is EumUserViewModel v)
            {
                sidebar.ViewModel = new SidebarViewModel(v, Ioc.Default.GetRequiredService<IEumUserPlaylistViewModelManager>());
                sidebar.OpenPaneLength = sidebar.NullOrSidebarWidth ?? sidebar.OpenPaneLength;
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

        private double? NullOrSidebarWidth
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

        private void Border_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var step = 1;
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            originalSize = IsPaneOpen ? (NullOrSidebarWidth ??= OpenPaneLength) : CompactPaneLength;

            if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
            {
                step = 5;
            }

            if (e.Key == VirtualKey.Space || e.Key == VirtualKey.Enter)
            {
                IsPaneOpen = !IsPaneOpen;
                return;
            }

            if (IsPaneOpen)
            {
                if (e.Key == VirtualKey.Left)
                {
                    SetSize(-step, true);
                    e.Handled = true;
                }
                else if (e.Key == VirtualKey.Right)
                {
                    SetSize(step, true);
                    e.Handled = true;
                }
            }
            else if (e.Key == VirtualKey.Right)
            {
                IsPaneOpen = !IsPaneOpen;
                return;
            }

            User.User.SidebarWidth = OpenPaneLength;
        }

        private void Border_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (DisplayMode == NavigationViewDisplayMode.Expanded)
            {
                SetSize(e.Cumulative.Translation.X);
            }
        }

        private void Border_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!dragging) // keep showing pressed event if currently resizing the sidebar
            {
                (sender as Grid).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
                VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerNormal", true);
            }
        }

        private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (DisplayMode == NavigationViewDisplayMode.Expanded)
            {
                (sender as Grid).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
                VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerPointerOver", true);
            }
        }

        private void SetSize(double val, bool closeImmediatleyOnOversize = false)
        {
            if (IsPaneOpen)
            {
                var newSize = originalSize + val;
                if (newSize <= MaximumSidebarWidth && newSize >= MinimumSidebarWidth)
                {
                    OpenPaneLength = newSize; // passing a negative value will cause an exception
                }

                if (newSize < MinimumSidebarWidth) // if the new size is below the minimum, check whether to toggle the pane
                {
                    if (MinimumSidebarWidth + val <= CompactPaneLength || closeImmediatleyOnOversize) // collapse the sidebar
                    {
                        IsPaneOpen = false;
                    }
                }
            }
            else
            {
                if (val >= MinimumSidebarWidth - CompactPaneLength || closeImmediatleyOnOversize)
                {
                    OpenPaneLength = MinimumSidebarWidth + (val + CompactPaneLength - MinimumSidebarWidth); // set open sidebar length to minimum value to keep it smooth
                    IsPaneOpen = true;
                }
            }

            User.User.SidebarWidth = OpenPaneLength;
        }

        private void ResizeElementBorder_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            (sender as Grid).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
            VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerNormal", true);
            User.User.SidebarWidth = OpenPaneLength;
            dragging = false;
        }

        private void ResizeElementBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            IsPaneOpen = !IsPaneOpen;
        }

        private void ResizeElementBorder_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (DisplayMode == NavigationViewDisplayMode.Expanded)
            {
                originalSize = IsPaneOpen ? (NullOrSidebarWidth ??= OpenPaneLength) : CompactPaneLength;
                (sender as Grid).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
                VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerPressed", true);
                dragging = true;
            }
        }
        public const double MinimumSidebarWidth = 1;

        public const double MaximumSidebarWidth = 500;

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
    }
}