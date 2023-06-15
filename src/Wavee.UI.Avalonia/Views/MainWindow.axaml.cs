using System;
using System.Runtime.InteropServices;
using System.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using FluentAvalonia.Interop;
using FluentAvalonia.Styling;
using FluentAvalonia.UI.Media;
using FluentAvalonia.UI.Windowing;
using Wavee.UI.Avalonia.Views.Shell;
using Wavee.UI.Core;
using Application = Avalonia.Application;

namespace Wavee.UI.Avalonia.Views;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDevTools();
#endif
        InitializeComponent();
        Application.Current.ActualThemeVariantChanged += OnActualThemeVariantChanged;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var thm = ActualThemeVariant;
        if (IsWindows11 && thm != FluentAvaloniaTheme.HighContrastTheme)
        {
            TryEnableMicaEffect();
        }


        TryEnableMicaEffect();

        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;
        
        SetLogInView();
    }

    private void SetLogInView()
    {
        var width = 600d;
        var height = 600;
      
        this.Width = width;
        this.Height = height;
        
        this.Position = new PixelPoint((int)((SystemParameters.VirtualScreenWidth - width) / 2), (int)((SystemParameters.VirtualScreenHeight - height) / 2));
        
        this.CanResize = false;
        
        // var x = (displayRegionWidth - width) / 2;
        // var y = (displayRegionHeight - height) / 2;
        //
        // appWindow.Move(new PointInt32((int)x, (int)y));
        //
        // AppWindow.Resize(new SizeInt32(
        //     _Width: (int)width,
        //     _Height: (int)height
        // ));
        //
        // var mainPresenter = (appWindow.Presenter as OverlappedPresenter);
        // mainPresenter.IsResizable = false;

        this.Content = new LoginView(SetShellView);
    }

    private void SetShellView(IAppState state)
    {
        var width = state.UserSettings.WindowWidth;
        var height = state.UserSettings.WindowHeight;
        
        this.Width = width;
        this.Height = height;
        
        this.Position = new PixelPoint((int)((SystemParameters.VirtualScreenWidth - width) / 2), (int)((SystemParameters.VirtualScreenHeight - height) / 2));
        
        this.CanResize = true;
        
        this.Content = new ShellView(state);
    }

    private void OnActualThemeVariantChanged(object sender, EventArgs e)
    {
        if (IsWindows11)
        {
            if (ActualThemeVariant != FluentAvaloniaTheme.HighContrastTheme)
            {
                TryEnableMicaEffect();
            }
            else
            {
                ClearValue(BackgroundProperty);
                ClearValue(TransparencyBackgroundFallbackProperty);
            }
        }
    }

    private void TryEnableMicaEffect()
    {
        var hwnd = this.TryGetPlatformHandle().Handle;
        //IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        
        if (ActualThemeVariant == ThemeVariant.Dark)
        {
            ApplyDarkMode(hwnd);
        }
        else
        {
            RemoveDarkMode(hwnd);
        }
        
        var dwmSystembackdropType = (int)MicaStatics.DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW;
        MicaStatics.DwmSetWindowAttribute(hwnd, MicaStatics.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
            ref dwmSystembackdropType,
            Marshal.SizeOf(typeof(int)));
    }

    private void RemoveDarkMode(IntPtr handle)
    {
        if (handle == IntPtr.Zero) { return; }

        var dwAttribute = MicaStatics.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;

        if (!OSVersionHelper.IsWindowsAtLeast(10, 0, 18985))
        {
            dwAttribute = MicaStatics.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD;
        }

        var _pvFalseAttribute = 0x00;
        MicaStatics.DwmSetWindowAttribute(handle, dwAttribute,
            ref _pvFalseAttribute,
            Marshal.SizeOf(typeof(int)));
    }

    private void ApplyDarkMode(IntPtr handle)
    {
        if (handle == IntPtr.Zero) return;

        var dwAttribute = MicaStatics.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;

        if (!OSVersionHelper.IsWindowsAtLeast(10, 0, 18985))
        {
            dwAttribute = MicaStatics.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD;
        }

        var _pvTrueAttribute = 0x01;
        MicaStatics.DwmSetWindowAttribute(handle, dwAttribute,
            ref _pvTrueAttribute,
            Marshal.SizeOf(typeof(int)));
    }
}