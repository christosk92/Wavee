using Windows.Graphics;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;
using System.Runtime.InteropServices;
using Wavee.UI.Core;
using Wavee.UI.WinUI.Views.Login;
using Wavee.UI.WinUI.Views.Shell;
using WinRT.Interop;
using Windows.UI.WindowManagement;
using WinUIEx;

namespace Wavee.UI.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        public MainWindow()
        {
            this.InitializeComponent();
            Instance = this;
            this.ExtendsContentIntoTitleBar = true;
            this.SystemBackdrop = new MicaBackdrop();


            SetLogInView();

            this.SizeChanged += (sender, args) =>
            {
                if (this.Content is ShellView { AppState: { } } v)
                {
                    v.AppState.UserSettings.WindowWidth = args.Size.Width;
                    v.AppState.UserSettings.WindowHeight = args.Size.Height;
                }
            };
        }
        private void SetShellView(IAppState state)
        {
            var windowWidth = state.UserSettings.WindowWidth;
            var windowHeight = state.UserSettings.WindowHeight;

            var scaleAdjustment = GetScaleAdjustment();
            var width = windowWidth * scaleAdjustment;
            var height = windowHeight * scaleAdjustment;

            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var displayArea = DisplayArea.GetFromWindowId(windowId,
                DisplayAreaFallback.Primary);

            double displayRegionWidth = displayArea.WorkArea.Width;
            double displayRegionHeight = displayArea.WorkArea.Height;

            width = Math.Min(width, displayRegionWidth);
            height = Math.Min(height, displayRegionHeight);

            var mainPresenter = (AppWindow.Presenter as OverlappedPresenter);
            mainPresenter.IsResizable = true;

            var x = (displayRegionWidth - width) / 2;
            var y = (displayRegionHeight - height) / 2;

            AppWindow.Move(new PointInt32((int)x, (int)y));

            AppWindow.Resize(new SizeInt32(
                _Width: (int)width,
                _Height: (int)height
            ));

            this.MinWidth = 640;
            this.MinHeight = 600;
            Global.AppState = state;
            this.Content = new ShellView(state);
        }

        private void SetLogInView()
        {
            //login view is characterized by being centered, not resizable and fixed size
            var appWindow = this.AppWindow;

            var scaleAdjustment = GetScaleAdjustment();

            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var displayArea = DisplayArea.GetFromWindowId(windowId,
                DisplayAreaFallback.Primary);

            double displayRegionWidth = displayArea.WorkArea.Width;
            double displayRegionHeight = displayArea.WorkArea.Height;

            var width = 600d * scaleAdjustment;
            var height = 600d * scaleAdjustment;

            width = Math.Min(width, displayRegionWidth);
            height = Math.Min(height, displayRegionHeight);

            var x = (displayRegionWidth - width) / 2;
            var y = (displayRegionHeight - height) / 2;

            appWindow.Move(new PointInt32((int)x, (int)y));

            AppWindow.Resize(new SizeInt32(
                _Width: (int)width,
                _Height: (int)height
            ));

            var mainPresenter = (appWindow.Presenter as OverlappedPresenter);
            mainPresenter.IsResizable = false;

            this.Content = new LoginView(SetShellView);
        }

        private double GetScaleAdjustment()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
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

        [DllImport("Shcore.dll", SetLastError = true)]
        internal static extern int
            GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

        internal enum Monitor_DPI_Type : int
        {
            MDT_Effective_DPI = 0,
            MDT_Angular_DPI = 1,
            MDT_Raw_DPI = 2,
            MDT_Default = MDT_Effective_DPI
        }

        public static MainWindow Instance { get; private set; }
    }
}
