using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Eum.UI.WinUI.Controls
{
    partial class SidebarControlStyles : ResourceDictionary
    {        /// <summary>
             /// true if the user is currently resizing the sidebar
             /// </summary>
        private bool dragging;

        private double originalSize = 0;

        private bool lockFlag = false;

        public SidebarControlStyles()
        {
            InitializeComponent();
        }

        private SidebarControl? _root;


        // private void Second_ItemsREpeaterLoaded(object sender, RoutedEventArgs e)
        // {
        //
        //     var itemsRepeater = (sender as ItemsRepeater);
        //     var ascendant = itemsRepeater.FindAscendant<SidebarControl>();
        //     itemsRepeater.ItemsSource = ascendant.ViewModel.Playlists;
        // }

        private void Border_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var step = 1;
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            originalSize = _root.IsPaneOpen ? (_root.NullOrSidebarWidth ??= _root.OpenPaneLength) : _root.CompactPaneLength;

            if (ctrl.HasFlag(CoreVirtualKeyStates.Down))
            {
                step = 5;
            }

            if (e.Key == VirtualKey.Space || e.Key == VirtualKey.Enter)
            {
                _root.IsPaneOpen = !_root.IsPaneOpen;
                return;
            }

            if (_root.IsPaneOpen)
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
                _root.IsPaneOpen = !_root.IsPaneOpen;
                return;
            }

            _root.User.User.SidebarWidth = _root.OpenPaneLength;
        }

        private void Border_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (_root.DisplayMode == NavigationViewDisplayMode.Expanded)
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
            if (_root.DisplayMode == NavigationViewDisplayMode.Expanded)
            {
                (sender as Grid).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
                VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerPointerOver", true);
            }
        }

        private void SetSize(double val, bool closeImmediatleyOnOversize = false)
        {
            if (_root.IsPaneOpen)
            {
                var newSize = originalSize + val;
                if (newSize <= MaximumSidebarWidth && newSize >= MinimumSidebarWidth)
                {
                    _root.OpenPaneLength = newSize; // passing a negative value will cause an exception
                }

                if (newSize < MinimumSidebarWidth) // if the new size is below the minimum, check whether to toggle the pane
                {
                    if (MinimumSidebarWidth + val <= _root.CompactPaneLength || closeImmediatleyOnOversize) // collapse the sidebar
                    {
                        _root.IsPaneOpen = false;
                    }
                }
            }
            else
            {
                if (val >= MinimumSidebarWidth - _root.CompactPaneLength || closeImmediatleyOnOversize)
                {
                    _root.OpenPaneLength = MinimumSidebarWidth + (val + _root.CompactPaneLength - MinimumSidebarWidth); // set open sidebar length to minimum value to keep it smooth
                    _root.IsPaneOpen = true;
                }
            }

            _root.User.User.SidebarWidth = _root.OpenPaneLength;
        }

        private void ResizeElementBorder_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            (sender as Grid).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
            VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerNormal", true);
            _root.User.User.SidebarWidth = _root.OpenPaneLength;
            dragging = false;
        }

        private void ResizeElementBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            _root.IsPaneOpen = !_root.IsPaneOpen;
        }

        private void ResizeElementBorder_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (_root.DisplayMode == NavigationViewDisplayMode.Expanded)
            {
                originalSize =_root.IsPaneOpen ? (_root.NullOrSidebarWidth ??= _root.OpenPaneLength) : _root.CompactPaneLength;
                (sender as Grid).ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
                VisualStateManager.GoToState((sender as Grid).FindAscendant<SplitView>(), "ResizerPressed", true);
                dragging = true;
            }
        }
        public const double MinimumSidebarWidth = 1;

        public const double MaximumSidebarWidth = 500;

        private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
        {
            _root ??= (sender as FrameworkElement).FindAscendant<SidebarControl>();
        }

        private void FrameworkElement_OnUnloaded(object sender, RoutedEventArgs e)
        {
            _root = null;
        }
    }
}