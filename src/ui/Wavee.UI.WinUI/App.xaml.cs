using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Windows.Graphics;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ReactiveUI;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.Infrastructure.Sys;
using TimeSpan = System.TimeSpan;
using Unit = LanguageExt.Unit;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Wavee.UI.WinUI.Views;
using WinRT.Interop;

namespace Wavee.UI.WinUI;

public partial class App : Application, INotifyPropertyChanged
{
    private int _width;
    private int _height;

    static App()
    {
        Runtime = WaveeUIRuntime.New(string.Empty);
        var home = Environment<WaveeUIRuntime>.getEnvironmentVariable("APPDATA").Run(Runtime)
            .ThrowIfFail();
        if (home.IsNone)
        {
            throw new System.Exception("APPDATA environment variable not set");
        }

        const string appName = "WaveeUI";
        Runtime = Runtime.WithPath(Path.Combine(home.ValueUnsafe(), appName));
    }

    public App()
    {
        this.InitializeComponent();
    }

    public static WaveeUIRuntime Runtime { get; }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _ = await UiConfig<WaveeUIRuntime>.CreateDefaultIfNotExists.Run(Runtime);

        var window = new Window
        {
            SystemBackdrop = new MicaBackdrop(),
            ExtendsContentIntoTitleBar = true,
            Content = new ShellView()
        };
        MWindow = window;
        
        
        var scaleAdjustment = GetScaleAdjustment();
        var width = (await UiConfig<WaveeUIRuntime>.WindowWidth.Run(Runtime)).IfFail(800) * scaleAdjustment;
        var height = (await UiConfig<WaveeUIRuntime>.WindowHeight.Run(Runtime)).IfFail(600) * scaleAdjustment;
        
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(MWindow);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        Microsoft.UI.Windowing.DisplayArea displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId,
            Microsoft.UI.Windowing.DisplayAreaFallback.Primary);

        double displayRegionWidth = displayArea.WorkArea.Width;
        double displayRegionHeight = displayArea.WorkArea.Height;

        var x = (displayRegionWidth - width) / 2;
        var y = (displayRegionHeight - height) / 2;

        appWindow.Move(new PointInt32((int)x, (int)y));
        
        window.AppWindow.Resize(new SizeInt32(
            _Width: (int)width,
            _Height: (int)height
        ));
        _width = (int)width;
        _height = (int)height;

        this.WhenAnyValue(
                x => x.Height,
                x => x.Width)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(async (___) =>
            {
                var aff =
                    from _ in UiConfig<WaveeUIRuntime>.SetWindowWidth((uint)Width)
                    from __ in UiConfig<WaveeUIRuntime>.SetWindowHeight((uint)Height)
                    select Unit.Default;

                var run = await aff.Run(Runtime);
            })
            .Subscribe();
        window.SizeChanged += (sender, eventArgs) =>
        {
            Width = (int)eventArgs.Size._width;
            Height = (int)eventArgs.Size._height;
        };
        window.Activate();
    }

    public static Window MWindow { get; private set; }
    public int Width
    {
        get => _width;
        set => SetField(ref _width, value);
    }

    public int Height
    {
        get => _height;
        set => SetField(ref _height, value);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
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
        IntPtr hWnd = WindowNative.GetWindowHandle(MWindow);
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

}