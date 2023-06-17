using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualBasic;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Wavee.UI.Core.Sys;
using Wavee.UI.WinUI.Extensions;
using Wavee.UI.WinUI.Navigation;
using Wavee.UI.WinUI.Views.Shell;


namespace Wavee.UI.WinUI.Components
{
    public sealed partial class SidebarControl : NavigationView
    {
        public static readonly DependencyProperty UserSettingsProperty = DependencyProperty.Register(nameof(UserSettings), typeof(UserSettings), typeof(SidebarControl), new PropertyMetadata(default(UserSettings)));
        /// <summary>
        /// true if the user is currently resizing the sidebar
        /// </summary>
        private bool dragging;

        private double originalSize = 0;

        private bool lockFlag = false;
        public SidebarControl()
        {
            this.InitializeComponent();
        }

        public UserSettings UserSettings
        {
            get => (UserSettings)GetValue(UserSettingsProperty);
            set => SetValue(UserSettingsProperty, value);
        }

        private void Border_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            var step = ctrl.HasFlag(CoreVirtualKeyStates.Down) ? 5 : 1;
            originalSize = IsPaneOpen ? UserSettings.SidebarWidth : CompactPaneLength;

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

            UserSettings.SidebarWidth = OpenPaneLength;
        }

        private void Border_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (DisplayMode == NavigationViewDisplayMode.Expanded)
                SetSize(e.Cumulative.Translation.X);
        }

        private void Border_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (dragging)
                return; // keep showing pressed event if currently resizing the sidebar

            var border = (Border)sender;
            border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
            VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerNormal", true);
        }

        private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (DisplayMode != NavigationViewDisplayMode.Expanded)
                return;

            var border = (Border)sender;
            border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
            VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerPointerOver", true);
        }

        private void SetSize(double val, bool closeImmediatelyOnOversize = false)
        {
            if (IsPaneOpen)
            {
                var newSize = originalSize + val;
                var isNewSizeGreaterThanMinimum = newSize >= UserSettings.MinimumSidebarWidth;
                if (newSize <= UserSettings.MaximumSidebarWidth && isNewSizeGreaterThanMinimum)
                    OpenPaneLength = newSize; // passing a negative value will cause an exception

                // if the new size is below the minimum, check whether to toggle the pane collapse the sidebar
                IsPaneOpen = !(!isNewSizeGreaterThanMinimum && (UserSettings.MinimumSidebarWidth + val <= CompactPaneLength || closeImmediatelyOnOversize));
            }
            else
            {
                if (val < UserSettings.MinimumSidebarWidth - CompactPaneLength &&
                    !closeImmediatelyOnOversize)
                    return;

                OpenPaneLength = val + CompactPaneLength; // set open sidebar length to minimum value to keep it smooth
                IsPaneOpen = true;
            }
        }

        private void ResizeElementBorder_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var border = (Border)sender;
            border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
            VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerNormal", true);
            UserSettings.SidebarWidth = OpenPaneLength;
            dragging = false;
        }

        private void ResizeElementBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            IsPaneOpen = !IsPaneOpen;
        }

        private void ResizeElementBorder_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (DisplayMode != NavigationViewDisplayMode.Expanded)
                return;

            originalSize = IsPaneOpen ? UserSettings.SidebarWidth : CompactPaneLength;
            var border = (Border)sender;
            border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
            VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerPressed", true);
            dragging = true;
        }

        public static GridLength GetSidebarCompactSize()
        {
            return App.Current.Resources.TryGetValue("NavigationViewCompactPaneLength", out object paneLength) && paneLength is double paneLengthDouble
                ? new GridLength(paneLengthDouble)
                : new GridLength(200);
        }

        private void Sidebar_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if ( args.InvokedItem is null || args.InvokedItemContainer is null)
            {
                return;
            }

            var navigationPath = args.InvokedItemContainer.Tag;
            if (navigationPath is NavigateToObject navigateTo)
            {
                ShellView.NavigationService.Navigate(navigateTo.To, navigateTo.Parameter);
            }
        }

        private void SidebarControl_OnDisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            if (args.DisplayMode == NavigationViewDisplayMode.Expanded)
            {
               this.IsPaneToggleButtonVisible = false;
               this.OpenPaneLength = UserSettings.SidebarWidth;
            }
            else
            {
                this.IsPaneToggleButtonVisible = true;
                this.OpenPaneLength = 250;
            }
        }

        private void SidebarControl_OnPaneOpening(NavigationView sender, object args)
        {
            //PaneRoot
            if (this.DisplayMode is NavigationViewDisplayMode.Minimal or NavigationViewDisplayMode.Compact)
            {
                var bg = this.FindDescendant<Grid>(x => x.Name is "PaneRoot");
                //AcrylicInAppFillColorDefaultBrush
                bg.Background = (Brush)Application.Current.Resources["AcrylicInAppFillColorDefaultBrush"];
                //bg.Background = new SolidColorBrush(Colors.White);
            }
        }

        private void SidebarControl_OnPaneClosing(NavigationView sender, NavigationViewPaneClosingEventArgs args)
        {
            var bg = this.FindDescendant<Grid>(x => x.Name is "PaneRoot");
            //AcrylicInAppFillColorDefaultBrush
            bg.Background = null;
        }
    }
}
