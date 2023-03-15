using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using System.Windows.Media;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Wavee.UI.WinUI.Controls;
using System.Windows.Controls;
using Border = Microsoft.UI.Xaml.Controls.Border;
using System.ComponentModel.Design;
using UWPToWinAppSDKUpgradeHelpers;

namespace Wavee.UI.WinUI.Themes;

partial class NavViewStyles : ResourceDictionary
{
    public const double MinimumSidebarWidth = 180;

    public const double MaximumSidebarWidth = 500;

    public NavViewStyles()
    {
        this.InitializeComponent();
    }
    /// <summary>
    /// true if the user is currently resizing the sidebar
    /// </summary>
    private bool dragging;
    private double originalSize = 0;

    private void ResizeElementBorder_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        var border = (Border)sender;
        var sidebarControl = border.FindAscendant<SidebarControl>();

        border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
        VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerNormal", true);
        sidebarControl.ViewModel.UserViewModel.User.UserData.SidebarWidth = sidebarControl.OpenPaneLength;
        dragging = false;
    }
    private void ResizeElementBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        var sidebarControl = (sender as Border).FindAscendant<SidebarControl>();

        sidebarControl.IsPaneOpen = !sidebarControl.IsPaneOpen;
    }

    private void Border_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        var sidebarControl = (sender as Border).FindAscendant<SidebarControl>();

        if (sidebarControl.DisplayMode == NavigationViewDisplayMode.Expanded)
            SetSize(sidebarControl, e.Cumulative.Translation.X);
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
        var sidebarControl = (sender as Border).FindAscendant<SidebarControl>();

        if (sidebarControl.DisplayMode != NavigationViewDisplayMode.Expanded)
            return;

        var border = (Border)sender;
        border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
        VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerPointerOver", true);
    }

    private void SetSize(SidebarControl sb, double val, bool closeImmediatelyOnOversize = false)
    {
        if (sb.IsPaneOpen)
        {
            var newSize = originalSize + val;
            var isNewSizeGreaterThanMinimum = newSize >= MinimumSidebarWidth;
            if (newSize <= MaximumSidebarWidth && isNewSizeGreaterThanMinimum)
                sb.OpenPaneLength = newSize; // passing a negative value will cause an exception

            // if the new size is below the minimum, check whether to toggle the pane collapse the sidebar
            sb.IsPaneOpen = !(!isNewSizeGreaterThanMinimum && (MinimumSidebarWidth + val
                <= sb.CompactPaneLength || closeImmediatelyOnOversize));
        }
        else
        {
            if (val < MinimumSidebarWidth - sb.CompactPaneLength &&
                !closeImmediatelyOnOversize)
                return;

            sb.OpenPaneLength = val + sb.CompactPaneLength; // set open sidebar length to minimum value to keep it smooth
            sb.IsPaneOpen = true;
        }
    }

    private void ResizeElementBorder_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        var sidebarControl = (sender as Border).FindAscendant<SidebarControl>();

        if (sidebarControl.DisplayMode != NavigationViewDisplayMode.Expanded)
            return;

        originalSize = sidebarControl.IsPaneOpen ? sidebarControl.ViewModel.UserViewModel.User.UserData.SidebarWidth : sidebarControl.CompactPaneLength;
        var border = (Border)sender;
        border.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
        VisualStateManager.GoToState(border.FindAscendant<SplitView>(), "ResizerPressed", true);
        dragging = true;
    }
}
