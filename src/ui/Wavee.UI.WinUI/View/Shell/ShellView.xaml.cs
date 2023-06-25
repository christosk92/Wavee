using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using LanguageExt;
using Wavee.UI.ViewModel.Shell;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.UI;
using Microsoft.UI.Xaml.Shapes;
using Wavee.UI.WinUI.Navigation;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.View.Shell
{
    public sealed partial class ShellView : UserControl
    {
        public ShellView(ShellViewModel shellViewModel)
        {
            this.InitializeComponent();
            _ = new NavigationService(MainContent);

            ViewModel = shellViewModel;
        }
        public ShellViewModel ViewModel { get; set; }

        private void SidebarControl_Resized(object sender, Option<double> e)
        {

            SetDragRegionForCustomTitleBar(App.MainWindow.AppWindow);
        }

        private double leftinset;
        private void AppTitleBar_OnLoaded(object sender, RoutedEventArgs e)
        {
            switch (SidebarControl.DisplayMode)
            {
                case NavigationViewDisplayMode.Minimal:
                    leftinset = 48 + 48;
                    SearchBar.Margin = new Thickness(24, 0, 0, 0);
                    break;
                default:
                    leftinset = 48;
                    SearchBar.Margin = new Thickness(0);
                    break;
            }
            SetDragRegionForCustomTitleBar(App.MainWindow.AppWindow);
        }

        private void AppTitleBar_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetDragRegionForCustomTitleBar(App.MainWindow.AppWindow);
        }
        private void NavigationView_OnDisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            switch (SidebarControl.DisplayMode)
            {
                case NavigationViewDisplayMode.Minimal:
                    leftinset = 48 + 48;
                    SearchBar.Margin = new Thickness(24, 0, 0, 0);
                    break;
                default:
                    leftinset = 48;
                    SearchBar.Margin = new Thickness(0);
                    break;
            }
            SetDragRegionForCustomTitleBar(App.MainWindow.AppWindow);
        }

        private void NavigationView_OnPaneOpening(NavigationView sender, object args)
        {
            SetDragRegionForCustomTitleBar(App.MainWindow.AppWindow);
        }

        private void NavigationView_OnPaneClosing(NavigationView sender, NavigationViewPaneClosingEventArgs args)
        {
            SetDragRegionForCustomTitleBar(App.MainWindow.AppWindow);
        }


        [DllImport("Shcore.dll", SetLastError = true)]
        internal static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

        internal enum Monitor_DPI_Type : int
        {
            MDT_Effective_DPI = 0,
            MDT_Angular_DPI = 1,
            MDT_Raw_DPI = 2,
            MDT_Default = MDT_Effective_DPI
        }

        private double GetScaleAdjustment()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(App.MainWindow);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
            IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

            // Get DPI.
            int result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
            if (result != 0)
            {
                throw new Exception("Could not get DPI for monitor.");
            }

            uint scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
            return scaleFactorPercent / 100.0;
        }

        private void SetDragRegionForCustomTitleBar(AppWindow appWindow)
        {
            // Check to see if customization is supported.
            // The method returns true on Windows 10 since Windows App SDK 1.2, and on all versions of
            // Windows App SDK on Windows 11.
            if (AppWindowTitleBar.IsCustomizationSupported()
                && appWindow.TitleBar.ExtendsContentIntoTitleBar)
            {
                double scaleAdjustment = GetScaleAdjustment();

                var rightPadding = new GridLength(appWindow.TitleBar.RightInset * scaleAdjustment);

                LeftPaddingColumn.Width = new GridLength(appWindow.TitleBar.LeftInset * scaleAdjustment);
                RightPaddingColumn.Width = new GridLength(appWindow.TitleBar.RightInset / scaleAdjustment, GridUnitType.Pixel);


                List<RectInt32> dragRectsList = new();

                //Compute the drag rects and column definitions for the title bar

                //LeftPaddingColumn -> computed size
                //LeftDragColumn -> ?
                //SearchColumn -> ?
                //RightDragColumn -> ?
                //ProfileCardsColumn -> Fixed size
                //RightPaddingColumn -> computed size

                //if our pane is open
                //we need a drag region for the entire pane size
                if (SidebarControl.IsPaneOpen)
                {
                    //LeftPaddingColumn
                    //we need a drag rect for the entire pane size (after the left padding)
                    RectInt32 paneSizeRect;
                    paneSizeRect.X = (int)(leftinset * scaleAdjustment); // inset from left
                    paneSizeRect.Y = 0;

                    paneSizeRect.Width = (int)((SidebarControl.OpenPaneLength - leftinset) * scaleAdjustment); // minus the offset we just added
                    paneSizeRect.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
                    dragRectsList.Add(paneSizeRect);

                    LeftDragColumn.Width = new GridLength(SidebarControl.OpenPaneLength, GridUnitType.Pixel);

                    //Next we MIGHT need a drag rect before the search box.
                    //Since the searchBOX (not the column) has a maxwidth of 600,
                    //we need to check if there's a distance between the left padding and the search box
                    //if there is, we need a drag rect there
                    //if there isn't, we don't need a drag rect there
                    var needsDragRect = SearchColumn.ActualWidth > SearchBar.MaxWidth;
                    if (needsDragRect)
                    {
                        //the size needed is the difference of these two
                        var diff = SearchColumn.ActualWidth - SearchBar.MaxWidth;
                        //but this diff is wrong because this is from two sides (so you have ----searchbar-----)
                        diff /= 2;
                        RectInt32 dragRect;
                        dragRect.X = (int)(SidebarControl.OpenPaneLength * scaleAdjustment); // inset from left
                        dragRect.Y = 0;

                        dragRect.Width = (int)(diff * scaleAdjustment);
                        dragRect.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
                        dragRectsList.Add(dragRect);

                        //add one more drag rect for the right side of the search bar (extra space)

                        RectInt32 dragRectR;
                        dragRectR.X = (int)((SidebarControl.OpenPaneLength + diff + SearchBar.MaxWidth) * scaleAdjustment); // inset from left
                        dragRectR.Y = 0;
                        dragRectR.Width = (int)(diff * scaleAdjustment);
                        dragRectR.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
                        dragRectsList.Add(dragRectR);
                    }

                    //Add one after the search box and profile cards
                    //this is the right drag column
                    RectInt32 dragRectR2;
                    dragRectR2.X = (int)((SidebarControl.OpenPaneLength + SearchColumn.ActualWidth + ProfileCardsColumn.ActualWidth) * scaleAdjustment); // inset from left
                    dragRectR2.Y = 0;
                    dragRectR2.Width = (int)(RightPaddingColumn.ActualWidth * scaleAdjustment);
                    dragRectR2.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
                    dragRectsList.Add(dragRectR2);
                }
                else
                {
                    //LeftPaddingColumn
                    //we need a drag rect for until the search box
                    RectInt32 paneSizeRect;
                    paneSizeRect.X = (int)(leftinset * scaleAdjustment); // inset from left
                    paneSizeRect.Y = 0;

                    paneSizeRect.Width = (int)((LeftDragColumn.ActualWidth- leftinset) * scaleAdjustment); // minus the offset we just added
                    paneSizeRect.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
                    dragRectsList.Add(paneSizeRect);


                    //and one last one for the right side
                    RectInt32 dragRectR2;
                    dragRectR2.X = (int)((LeftDragColumn.ActualWidth + SearchColumn.ActualWidth + ProfileCardsColumn.ActualWidth) * scaleAdjustment); // inset from left
                    dragRectR2.Y = 0;
                    dragRectR2.Width = (int)(RightPaddingColumn.ActualWidth * scaleAdjustment);
                    dragRectR2.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);

                    dragRectsList.Add(dragRectR2);
                }



                // MimicAppTitleBar.Children.Clear();
                // //draw them on the title bar with a red background to see the areas
                // foreach (RectInt32 dragRect in dragRectsList)
                // {
                //     var w = Math.Max(0, dragRect.Width);
                //     var rect = new Rectangle();
                //     //create a random color
                //     var random = new Random();
                //     var color = Color.FromArgb(255, (byte)random.Next(0, 255), (byte)random.Next(0, 255), (byte)random.Next(0, 255));
                //
                //     rect.Fill = new SolidColorBrush(color);
                //     rect.Width = w / scaleAdjustment;
                //     rect.Height = dragRect.Height / scaleAdjustment;
                //
                //     //draw the rect at the right location
                //     Canvas.SetLeft(rect, dragRect.X / scaleAdjustment);
                //     Canvas.SetTop(rect, dragRect.Y / scaleAdjustment);
                //     MimicAppTitleBar.Children.Add(rect);
                // }


                RectInt32[] dragRects = dragRectsList.ToArray();

                appWindow.TitleBar.SetDragRectangles(dragRects);
            }
        }
    }
}
