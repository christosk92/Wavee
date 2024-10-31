using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Wavee.UI.WinUI.Helpers
{
    internal static class WindowInterop
    {
        [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetDpiForWindow(IntPtr hwnd);

        // Constants for window messages
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MINIMIZE = 0xF020;
        public const int SC_MAXIMIZE = 0xF030;
        public const int SC_RESTORE = 0xF120;
        public const int WM_SIZE = 0x0005;
        public const int WM_CLOSE = 0x0010;

        // Constants for window styles
        public const int GWL_STYLE = -16;
        public const int GWL_WNDPROC = -4;
        public const int WS_MINIMIZE = 0x20000000;
        public const int WS_MAXIMIZE = 0x01000000;

        // WINDOWPLACEMENT structure
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        // POINT structure
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        // RECT structure
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // P/Invoke for GetWindowPlacement
        [DllImport("user32.dll")]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        // P/Invoke for SetForegroundWindow
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        // P/Invoke for GetWindowLongPtr (64-bit)
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        // P/Invoke for GetWindowLongPtr (32-bit)
        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }

        // P/Invoke for SetWindowLongPtr (64-bit)
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        // P/Invoke for SetWindowLongPtr (32-bit)
        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
        }

        // Constants for window messages
        public const int WM_GETMINMAXINFO = 0x0024;

        // MINMAXINFO structure
        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        // Delegate for window procedure
        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // Dictionary to store window data
        private static Dictionary<IntPtr, WindowData> _windowData = new Dictionary<IntPtr, WindowData>();

        internal class WindowData
        {
            public double MinWidth { get; set; }
            public double MinHeight { get; set; }
            public WndProc NewWndProc { get; set; }
            public IntPtr OldWndProc { get; set; }
        }

        // P/Invoke for CallWindowProc
        [DllImport("user32.dll")]
        public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // P/Invoke for DefWindowProc
        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);


        public static double GetMinWidth(IntPtr hWnd)
        {
            if (_windowData.TryGetValue(hWnd, out var value))
            {
                return value.MinWidth;
            }
            else
            {
                return 766;
            }
        }

        public static void SetMinWidth(IntPtr hWnd, double minWidth)
        {
            SetMinSize(hWnd, minWidth, null);
        }


        public static double GetMinHeight(IntPtr hWnd)
        {
            if (_windowData.ContainsKey(hWnd))
            {
                return _windowData[hWnd].MinHeight;
            }
            else
            {
                return 766;
            }
        }

        public static void SetMinHeight(IntPtr hWnd, double minHeight)
        {
            SetMinSize(hWnd, null, minHeight);
        }
        private static void SetMinSize(IntPtr hWnd, double? minWidth, double? minHeight)
        {
            if (!_windowData.ContainsKey(hWnd))
            {
                // Subclass the window
                var data = new WindowData();
                data.MinWidth = minWidth ?? 0;
                data.MinHeight = minHeight ?? 0;

                // Create new WndProc delegate
                data.NewWndProc = new WndProc(NewWindowProc);

                // Set the new window procedure
                data.OldWndProc = SetWindowLongPtr(hWnd, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(data.NewWndProc));

                // Add to dictionary
                _windowData[hWnd] = data;
            }
            else
            {
                // Update the minWidth and minHeight
                if (minWidth.HasValue)
                    _windowData[hWnd].MinWidth = minWidth.Value;
                if (minHeight.HasValue)
                    _windowData[hWnd].MinHeight = minHeight.Value;
            }
        }

        public static IntPtr NewWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                // Handle WM_GETMINMAXINFO
                // lParam is a pointer to MINMAXINFO structure
                var minmaxinfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);

                // Get the data for this window
                if (_windowData.TryGetValue(hWnd, out var data))
                {
                    int dpi = GetDpiForWindow(hWnd);
                    double scaleFactor = dpi / 96.0;

                    if (data.MinWidth > 0)
                    {
                        // Set the minimum track width
                        minmaxinfo.ptMinTrackSize.X = (int)(data.MinWidth * scaleFactor);
                    }

                    if (data.MinHeight > 0)
                    {
                        // Set the minimum track height
                        minmaxinfo.ptMinTrackSize.Y = (int)(data.MinHeight * scaleFactor);
                    }

                    // Marshal the structure back to the pointer
                    Marshal.StructureToPtr(minmaxinfo, lParam, true);

                    // Return zero to indicate we've processed this message
                    return IntPtr.Zero;
                }
            }

            // For other messages, call the original window procedure
            if (_windowData.TryGetValue(hWnd, out var data2))
            {
                return CallWindowProc(data2.OldWndProc, hWnd, msg, wParam, lParam);
            }
            else
            {
                return DefWindowProc(hWnd, msg, wParam, lParam);
            }
        }

    }
}