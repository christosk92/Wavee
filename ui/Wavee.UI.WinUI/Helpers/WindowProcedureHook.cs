// File: Helpers/WindowProcedureHook.cs

using System;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Wavee.UI.WinUI.Helpers
{
    internal class WindowProcedureHook : IDisposable
    {
        private readonly IntPtr _hWnd;
        private readonly WndProcDelegate _newWndProc;
        private readonly IntPtr _oldWndProc;

        // Delegate for the new window procedure
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // Import necessary functions from user32.dll
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate newProc);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // Constants for window messages
        public const int WM_CLOSE = 0x0010;
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_CLOSE = 0xF060;

        public event EventHandler? Closing;
        public event EventHandler? Closed;

        public WindowProcedureHook(IntPtr hWnd)
        {
            _hWnd = hWnd;
            _newWndProc = CustomWndProc;
            _oldWndProc = SetWindowLongPtr(_hWnd, -4, _newWndProc); // GWL_WNDPROC = -4
            if (_oldWndProc == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to set window procedure.");
            }
        }

        private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_SYSCOMMAND:
                    {
                        int command = wParam.ToInt32() & 0xFFF0;
                        if (command == SC_CLOSE)
                        {
                            Closing?.Invoke(this, EventArgs.Empty);
                            // Optionally, prevent the window from closing
                            // return IntPtr.Zero;
                        }
                        break;
                    }
                case WM_CLOSE:
                    {
                        Closing?.Invoke(this, EventArgs.Empty);
                        // Optionally, perform actions before closing
                        break;
                    }
            }

            // Call the original window procedure for default processing
            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        public void Dispose()
        {
            // Restore the original window procedure
            SetWindowLongPtr(_hWnd, -4, null);
        }
    }
}
