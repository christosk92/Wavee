using System;
using Microsoft.UI;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Documents;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Core;
using Wavee.UI.ViewModel.Login;
using WinRT.Interop;
using Windows.UI.WindowManagement;
using AppWindow = Microsoft.UI.Windowing.AppWindow;
using AppWindowTitleBar = Microsoft.UI.Windowing.AppWindowTitleBar;

namespace Wavee.UI.WinUI.Views.Login
{
    public sealed partial class LoginView : UserControl
    {
        private Action<IAppState> _done;
        public LoginView(Action<IAppState> done)
        {
            _done = done;
            this.InitializeComponent();
            ViewModel = new LoginViewModel(state =>
            {
                done(state);
                _done = null;
            });
        }

        public LoginViewModel ViewModel { get; }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            App.MainWindow.SetTitleBar(titleBar: TitleBar);
            var titlebar = App.MainWindow.AppWindow.TitleBar;
            titlebar.ExtendsContentIntoTitleBar = true;
            App.MainWindow.AppWindow.TitleBar.BackgroundColor = Colors.Transparent;

            await ViewModel.AttemptLoginStored(CancellationToken.None);
            UsernameInput.Focus(FocusState.Keyboard);
        }

        private void TitleBar_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Check to see if customization is supported.
            // The method returns true on Windows 10 since Windows App SDK 1.2, and on all versions of
            // Windows App SDK on Windows 11.
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                // Update drag region if the size of the title bar changes.
                SetDragRegionForCustomTitleBar(App.MainWindow.AppWindow);
            }
        }

        private void TitleBar_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                SetDragRegionForCustomTitleBar(App.MainWindow.AppWindow);
            }
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
                && App.MainWindow.ExtendsContentIntoTitleBar)
            {
                double scaleAdjustment = GetScaleAdjustment();


                List<Windows.Graphics.RectInt32> dragRectsList = new();

                Windows.Graphics.RectInt32 dragRectL;
                dragRectL.X = 0;
                dragRectL.Y = 0;
                dragRectL.Height = (int)(this.TitleBar.ActualHeight * scaleAdjustment);
                dragRectL.Width = (int)((this.TitleBar.ActualWidth) * scaleAdjustment);
                dragRectsList.Add(dragRectL);

                Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

                appWindow.TitleBar.SetDragRectangles(dragRects);
            }
        }

        public bool Negate(bool b)
        {
            return !b;
        }
    }
}
