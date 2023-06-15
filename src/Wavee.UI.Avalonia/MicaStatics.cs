using System;
using System.Runtime.InteropServices;

namespace Wavee.UI.Avalonia;

public static class MicaStatics
{
    [Flags]
    public enum DWM_SYSTEMBACKDROP_TYPE
    {
        DWMSBT_MAINWINDOW = 2, // Mica
        DWMSBT_TRANSIENTWINDOW = 3, // Acrylic
        DWMSBT_TABBEDWINDOW = 4 // Tabbed
    }


    [Flags]
    public enum DWMWINDOWATTRIBUTE
    {
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        DWMWA_SYSTEMBACKDROP_TYPE = 38,
        /// <summary>
        /// Allows a window to either use the accent color, or dark, according to the user Color Mode preferences.
        /// </summary>
        DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19,
    }

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref int pvAttribute, int cbAttribute);


    public static int SetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, int parameter)
        => DwmSetWindowAttribute(hwnd, attribute, ref parameter, Marshal.SizeOf<int>());
}