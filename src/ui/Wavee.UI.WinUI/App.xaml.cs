using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ReactiveUI;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using Serilog;
using Wavee.UI.Daemon;
using System.Threading.Tasks;
using Windows.ApplicationModel.Wallet;
using Windows.Foundation;
using Windows.Graphics;
using Microsoft.UI.Windowing;
using Wavee.UI.Helpers;
using Wavee.UI.WinUI.Helpers;
using Wavee.UI.WinUI.Views.Shell;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Microsoft.UI;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace Wavee.UI.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public Config Config { get; }
    public Global? Global { get; private set; }


    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        Config = new Config(LoadOrCreateConfigs(), Array.Empty<string>());

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(Path.Combine(Config.DataDir, "Logs", "log.txt"), rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();

        Global = CreateGlobal();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            Log.Error(ex, "An application-crash error occurred.");

            RxApp.MainThreadScheduler.Schedule(() => throw new ApplicationException("Exception has been thrown in unobserved ThrownExceptions", ex));
        });


        UiConfig uiConfig = LoadOrCreateUiConfig(Config.DataDir);
        Services.Initialize(Global!, uiConfig);

        await StartupHelper.ModifyStartupSettingAsync(uiConfig.RunOnSystemStartup).ConfigureAwait(false);

        var configWidth = Services.UiConfig.WindowWidth;
        var configHeight = Services.UiConfig.WindowHeight;

        MWindow = new Window
        {
            SystemBackdrop = new MicaBackdrop(),
            ExtendsContentIntoTitleBar = true,
            Content = new ShellView(),
        };
        //move to last saved position
        MWindow.AppWindow.Move(new PointInt32());

        // if (configWidth is not null && configHeight is not null)
        // {
        //     MWindow.AppWindow.Resize(new SizeInt32((int)configWidth.Value, (int)configHeight.Value));
        // }

        double scaleAdjustment = GetScaleAdjustment();

        var actualWidth = (configWidth ?? 800) * scaleAdjustment;
        var actualHeight = (configHeight ?? 600) * scaleAdjustment;

        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(MWindow);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        Microsoft.UI.Windowing.DisplayArea displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId,
            Microsoft.UI.Windowing.DisplayAreaFallback.Primary);

        double displayRegionWidth = displayArea.WorkArea.Width;
        double displayRegionHeight = displayArea.WorkArea.Height;

        var x = (displayRegionWidth - actualWidth) / 2;
        var y = (displayRegionHeight - actualHeight) / 2;

        appWindow.Move(new PointInt32((int)x, (int)y));


        appWindow.Resize(new SizeInt32((int)(actualWidth),
            (int)(actualHeight)));
        MWindow.SizeChanged += MWindow_SizeChanged;

        ThemeHelper.ApplyTheme(uiConfig.Theme);

        MWindow.Activate();
    }

    private void MWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        Services.UiConfig.WindowWidth = args.Size.Width;
        Services.UiConfig.WindowHeight = args.Size.Height;
    }

    private static UiConfig LoadOrCreateUiConfig(string dataDir)
    {
        Directory.CreateDirectory(dataDir);

        UiConfig uiConfig = new(Path.Combine(dataDir, "UiConfig.json"));
        uiConfig.LoadFile(createIfMissing: true);

        return uiConfig;
    }
    private Global CreateGlobal()
    {
        return new Global(Config.DataDir, Config);
    }

    private PersistentConfig LoadOrCreateConfigs()
    {
        Directory.CreateDirectory(Config.DataDir);

        PersistentConfig persistentConfig = new(Path.Combine(Config.DataDir, "Config.json"));
        persistentConfig.LoadFile(createIfMissing: true);


        return persistentConfig;
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


    public static Window MWindow { get; private set; }
}