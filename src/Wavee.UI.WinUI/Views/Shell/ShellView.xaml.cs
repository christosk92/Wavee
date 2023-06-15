using Microsoft.UI.Xaml.Controls;
using System.Runtime;
using System;
using System.Collections.Generic;
using Wavee.UI.Core;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.Spotify.Infrastructure.Remote.Contracts;
using Wavee.UI.Navigation;
using Wavee.UI.WinUI.Navigation;
using Wavee.UI.WinUI.Views.Home;
using WinRT.Interop;
using Wavee.UI.Core.Sys.Mock;
using Wavee.UI.ViewModel;

namespace Wavee.UI.WinUI.Views.Shell
{
    public sealed partial class ShellView : UserControl
    {
        public ShellView(IAppState appState)
        {
            this.InitializeComponent();
            Titlebar.Loaded += TitlebarOnLoaded;
            Titlebar.SizeChanged += TitlebarOnSizeChanged;
            MainWindow.Instance.SetTitleBar(Titlebar);
            NavigationService = new NavigationService(MainFrame);
            NavigationService.Navigated += NavigationServiceOnNavigating;
            NavigationService.Navigate(typeof(HomePage));
            AppState = appState;
            ViewModel = new ShellViewModel(appState);
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        public ShellViewModel ViewModel { get; }

        private void TitlebarOnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetDragRegionForCustomTitleBar(MainWindow.Instance.AppWindow);
        }

        private void TitlebarOnLoaded(object sender, RoutedEventArgs e)
        {
            SetDragRegionForCustomTitleBar(MainWindow.Instance.AppWindow);
        }

        private void NavigationServiceOnNavigating(object sender, (Type Tp, object Prm) e)
        {
            SidebarControl.IsBackEnabled = NavigationService.CanGoBack;
            var item = e.Tp;
            if (item == typeof(HomePage))
            {
                SidebarControl.SelectedItem = SidebarControl.MenuItems[1];
            }
        }

        public IAppState AppState { get; }

        public static NavigationService NavigationService { get; private set; }

        public NavigationService NavService => NavigationService;

        private void HomeLoaded(object sender, RoutedEventArgs e)
        {
            var item = (sender as NavigationViewItem);
            item.Tag = new NavigateToObject(
                To: typeof(HomePage)
            );
        }

        public string GetUserDescription(UserProfile userProfile)
        {
            return "SPOTIFY PREMIUM";
        }

        public string GetUserName(UserProfile userProfile)
        {
            if (userProfile is null) return string.Empty;
            return userProfile.Name ?? userProfile.Id;
        }

        public ImageSource GetUserPicture(UserProfile userProfile)
        {
            if (userProfile is null) return null;
            if (userProfile.ImageUrl.IsSome)
            {
                var imageUrl = userProfile.ImageUrl.ValueUnsafe();
                var image = new BitmapImage(new Uri(imageUrl));
                return image;
            }

            return null;
        }

        public string GetUserInitials(UserProfile userProfile)
        {
            if (userProfile is null) return string.Empty;
            var nameCorrect = userProfile.Name ?? userProfile.Id;
            var initials = nameCorrect.Split(' ');
            if (initials.Length > 1)
            {
                return initials[0][0].ToString() + initials[1][0].ToString();
            }
            else
            {
                return initials[0][0].ToString();
            }
        }

        private void SidebarControl_OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
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

                // RightPaddingColumn.Width = new GridLength(appWindow.TitleBar.RightInset / scaleAdjustment);
                //LeftPaddingColumn.Width = new GridLength(appWindow.TitleBar.LeftInset / scaleAdjustment);


                List<Windows.Graphics.RectInt32> dragRectsList = new();

                Windows.Graphics.RectInt32 dragRectL;
                dragRectL.X = (int)((80 + 80) * scaleAdjustment);
                dragRectL.Y = 0;
                dragRectL.Height = (int)(Titlebar.ActualHeight * scaleAdjustment);
                dragRectL.Width = (int)((Titlebar.ActualWidth - dragRectL.X - 200) * scaleAdjustment);
                dragRectsList.Add(dragRectL);
                Windows.Graphics.RectInt32[] dragRects = dragRectsList.ToArray();

                appWindow.TitleBar.SetDragRectangles(dragRects);
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
            IntPtr hWnd = WindowNative.GetWindowHandle(MainWindow.Instance);
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


        public Visibility IsOurDevice(SpotifyRemoteDeviceInfo spotifyRemoteDeviceInfo)
        {
            //hide our device
            return spotifyRemoteDeviceInfo.DeviceId ==
                   Global.AppState.DeviceId ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
