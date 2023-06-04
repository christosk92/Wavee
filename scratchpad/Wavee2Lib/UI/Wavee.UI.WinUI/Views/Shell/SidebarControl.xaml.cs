using System;
using System.Collections.Generic;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using CommunityToolkit.WinUI.UI;
using Wavee.UI.WinUI.Helpers;
using Wavee.UI.Settings;
using Windows.System;
using Windows.UI.Core;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.UI.Models.Common;
using Wavee.UI.ViewModels;
using Microsoft.VisualBasic.ApplicationServices;

namespace Wavee.UI.WinUI.Views.Shell
{
    public sealed partial class SidebarControl : UserControl
    {

        /// <summary>
        /// true if the user is currently resizing the sidebar
        /// </summary>
        private bool dragging;

        private double originalSize = 0;

        private bool lockFlag = false;
        public static readonly DependencyProperty FixedItemsProperty = DependencyProperty.Register(nameof(FixedItems), typeof(IReadOnlyCollection<SidebarItem>), typeof(SidebarControl), new PropertyMetadata(default(IReadOnlyCollection<SidebarItem>)));
        public static readonly DependencyProperty PlaylistsProperty = DependencyProperty.Register(nameof(Playlists), typeof(List<PlaylistOrFolder>), typeof(SidebarControl), new PropertyMetadata(default(List<PlaylistOrFolder>)));
        public static readonly DependencyProperty UserSettingsProperty = DependencyProperty.Register(nameof(UserSettings),
            typeof(UserSettings), typeof(SidebarControl), new PropertyMetadata(default(UserSettings), PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SidebarControl;
            if (control == null)
                return;

            control.UserSettings = e.NewValue as UserSettings;
            control.SidebarSplitter.OpenPaneLength = control.UserSettings.SidebarWidth;
        }

        public static readonly DependencyProperty UserInfoProperty = DependencyProperty.Register(nameof(UserInfo), typeof(SpotifyUser), typeof(SidebarControl), new PropertyMetadata(default(SpotifyUser)));

        public SidebarControl()
        {
            this.InitializeComponent();
        }

        public IReadOnlyCollection<SidebarItem> FixedItems
        {
            get => (IReadOnlyCollection<SidebarItem>)GetValue(FixedItemsProperty);
            set => SetValue(FixedItemsProperty, value);
        }

        public List<PlaylistOrFolder> Playlists
        {
            get => (List<PlaylistOrFolder>)GetValue(PlaylistsProperty);
            set => SetValue(PlaylistsProperty, value);
        }

        public UserSettings UserSettings
        {
            get => (UserSettings)GetValue(UserSettingsProperty);
            set => SetValue(UserSettingsProperty, value);
        }

        public SpotifyUser UserInfo
        {
            get => (SpotifyUser)GetValue(UserInfoProperty);
            set => SetValue(UserInfoProperty, value);
        }

        private void Border_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (SidebarSplitter.DisplayMode is SplitViewDisplayMode.Inline)
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
            if (SidebarSplitter.DisplayMode is not SplitViewDisplayMode.Inline)
                return;

            var border = (Border)sender;
            border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
            VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerPointerOver", true);
        }
        private void ResizeElementBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            SidebarSplitter.IsPaneOpen = !SidebarSplitter.IsPaneOpen;
        }

        private void ResizeElementBorder_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var border = (Border)sender;
            border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
            VisualStateManager.GoToState(SidebarSplitter, "ResizerNormal", true);
            UserSettings.SidebarWidth = SidebarSplitter.OpenPaneLength;
            dragging = false;
        }
        private void ResizeElementBorder_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (SidebarSplitter.DisplayMode is not SplitViewDisplayMode.Inline)
                return;

            originalSize = SidebarSplitter.IsPaneOpen ? UserSettings.SidebarWidth : SidebarSplitter.CompactPaneLength;
            var border = (Border)sender;
            border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
            VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerPressed", true);
            dragging = true;
        }

        private void SetSize(double val, bool closeImmediatelyOnOversize = false)
        {
            if (SidebarSplitter.IsPaneOpen)
            {
                var newSize = originalSize + val;
                var isNewSizeGreaterThanMinimum = newSize >= UserSettings.MinimumSidebarWidth;
                if (newSize <= UserSettings.MaximumSidebarWidth && isNewSizeGreaterThanMinimum)
                    SidebarSplitter.OpenPaneLength = newSize; // passing a negative value will cause an exception

                // if the new size is below the minimum, check whether to toggle the pane collapse the sidebar
                SidebarSplitter.IsPaneOpen = !(!isNewSizeGreaterThanMinimum && (UserSettings.MinimumSidebarWidth + val <= SidebarSplitter.CompactPaneLength || closeImmediatelyOnOversize));
            }
            else
            {
                if (val < UserSettings.MinimumSidebarWidth - SidebarSplitter.CompactPaneLength &&
                    !closeImmediatelyOnOversize)
                    return;

                SidebarSplitter.OpenPaneLength = val + SidebarSplitter.CompactPaneLength; // set open sidebar length to minimum value to keep it smooth
                SidebarSplitter.IsPaneOpen = true;
            }
        }

        private void PaneRoot_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var contextMenu = FlyoutBase.GetAttachedFlyout(this);
            contextMenu.ShowAt(this, new FlyoutShowOptions() { Position = e.GetPosition(this) });

            e.Handled = true;
        }

        private void Border_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            var step = ctrl.HasFlag(CoreVirtualKeyStates.Down) ? 5 : 1;
            originalSize = SidebarSplitter.IsPaneOpen ? UserSettings.SidebarWidth : SidebarSplitter.CompactPaneLength;

            if (e.Key == VirtualKey.Space || e.Key == VirtualKey.Enter)
            {
                SidebarSplitter.IsPaneOpen = !SidebarSplitter.IsPaneOpen;
                return;
            }

            if (SidebarSplitter.IsPaneOpen)
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
                SidebarSplitter.IsPaneOpen = !SidebarSplitter.IsPaneOpen;
                return;
            }

            UserSettings.SidebarWidth = SidebarSplitter.OpenPaneLength;
        }

        private void Settings_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void AddPlaylistButton_OnTapped(object sender, TappedRoutedEventArgs e)
        {

        }

        private void PlaylistsListView_OnItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {

        }
        private void FixedSidebarItemsListView_OnItemClick(object sender, ItemClickEventArgs e)
        {
        }

        private void FixedSidebarItemsListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void FixedSidebarItemsListView_OnLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in FixedSidebarItemsListView.Items)
            {
                var container = (ListViewItem)FixedSidebarItemsListView.ContainerFromItem(item);
                var x = (SidebarItem)item;
                //if item is header, set it to not selectable
                if (x.IsAHeader)
                {
                    container.IsHitTestVisible = false;
                    container.IsEnabled = false;
                }
                if (!x.IsEnabled)
                {
                    container.IsHitTestVisible = false;
                    container.IsEnabled = false;
                }
            }
        }
        public Visibility HideIfNonZero(int i)
        {
            return i == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public ImageSource GetProfilePicture(SpotifyUser user)
        {
            if (user.ImageUrl.IsSome)
            {
                var imageId = user.ImageUrl.ValueUnsafe();
                var url = new Uri(imageId);
                return new BitmapImage(url);
            }

            return null;
        }
        public string GetDisplayName(SpotifyUser option)
        {
            return option.DisplayName;
        }

        public string GetInitials(SpotifyUser spotifyUser)
        {
            //display name or id
            return spotifyUser.DisplayName.Length > 0 ? spotifyUser.DisplayName[0].ToString() : spotifyUser.Id[0].ToString();
        }
    }
}
