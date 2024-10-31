using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Wavee.ViewModels.Enums;
using Wavee.ViewModels.Interfaces;
using Wavee.ViewModels.Models.EventArgs;
using Wavee.ViewModels.Models.UI;
using WinRT.Interop;
using Wavee.UI.WinUI.Helpers;
using Windows.Graphics;
using Microsoft.Graphics.Display;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WindowClosingEventArgs = Wavee.ViewModels.Models.EventArgs.WindowClosingEventArgs;

namespace Wavee.UI.WinUI
{
    public sealed partial class MainWindow : Window, IMainWindow, INotifyPropertyChanged
    {
        private WaveeRect _bounds;
        private WindowState _windowState;
        private AppWindow _appWindow;
        private WindowStateManager _windowStateManager;
        private float _dpiScale;
        private WindowProcedureHook? _wndProcHook;

        // Events
        public event EventHandler<WindowClosingEventArgs>? Closing;
        public event EventHandler? Closed;
        public event PropertyChangedEventHandler? PropertyChanged;

        // Constructor
        public MainWindow()
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.SystemBackdrop = App.Instance.RequestedTheme switch
            {
                ApplicationTheme.Light => new MicaBackdrop
                {
                    Kind= MicaKind.BaseAlt
                },
                ApplicationTheme.Dark => new MicaBackdrop(),
                _ => throw new ArgumentOutOfRangeException()
            };


            // Initialize AppWindow
            var windowHandle = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            // Initialize DisplayInformation
            _dpiScale = WindowInterop.GetDpiForWindow(windowHandle) / 100f;
            // _displayInformation.DpiChanged += OnDpiChanged;

            // Initialize WindowStateManager
            _windowStateManager = new WindowStateManager(this);
            _windowStateManager.PropertyChanged += OnWindowStateChanged;


            // Initialize WindowProcedureHook
            try
            {
                _wndProcHook = new WindowProcedureHook(windowHandle);
                _wndProcHook.Closing += OnWindowClosing;
                _wndProcHook.Closed += OnWindowClosed; // If implemented in WindowProcedureHook
            }
            catch (Exception ex)
            {
                // Handle exceptions related to setting the window procedure
                // For example, log the error or notify the user
                Console.WriteLine($"Failed to set window procedure: {ex.Message}");
            }


            // Subscribe to other window events
            this.Closed += OnClosedInternal;
            this.SizeChanged += OnSizeChanged;

            // Initialize WindowState
            _windowState = _windowStateManager.CurrentState;
            OnPropertyChanged(nameof(WindowState));
        }

        /// <summary>
        /// Brings the window to the front, restores it if minimized, and activates it.
        /// </summary>
        public void BringToFront()
        {
            // Check and restore if minimized
            if (_windowState == WindowState.Minimized)
            {
                RestoreWindow();
            }

            // Activate the window
            this.Activate();

            // Bring to foreground using Windows API
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            WindowInterop.SetForegroundWindow(hwnd);
        }

        /// <summary>
        /// Shows the window by activating it.
        /// </summary>
        public void Show()
        {
            this.Activate();
        }

        /// <summary>
        /// Gets or sets the bounds of the window.
        /// </summary>
        public new WaveeRect Bounds
        {
            get => _bounds;
            set => SetField(ref _bounds, value);
        }

        /// <summary>
        /// Gets or sets the state of the window.
        /// </summary>
        public WindowState WindowState
        {
            get => _windowState;
            set => SetField(ref _windowState, value);
        }

        // Other properties with DPI handling
        public double Width
        {
            get => PhysicalToLogical(_appWindow.Size.Width);
            set
            {
                int physicalWidth = LogicalToPhysical(value);
                if (Math.Abs(_appWindow.Size.Height - physicalWidth) > 0.01)
                {
                    var size = new SizeInt32((int)physicalWidth, _appWindow.Size.Height);
                    _appWindow.Resize(size);
                    OnPropertyChanged();
                }
            }
        }

        public double Height
        {
            get => PhysicalToLogical(_appWindow.Size.Height);
            set
            {
                int physicalHeight = LogicalToPhysical(value);
                if (_appWindow.Size.Height != physicalHeight)
                {
                    var size = new SizeInt32(_appWindow.Size.Width, physicalHeight);
                    _appWindow.Resize(size);
                    OnPropertyChanged();
                }
            }
        }

        public double MinWidth
        {
            get => WindowInterop.GetMinWidth(WindowNative.GetWindowHandle(this));
            set
            {
                if (Math.Abs(WindowInterop.GetMinWidth(WindowNative.GetWindowHandle(this)) - value) > 0.01)
                {
                    // var minSize = new SizeInt32(value, _appWindow.MinSize.Height);
                    // _appWindow.SetMinSize(minSize);
                    WindowInterop.SetMinWidth(WindowNative.GetWindowHandle(this), value);
                    OnPropertyChanged();
                }
            }
        }

        public double MinHeight
        {
            get => WindowInterop.GetMinHeight(WindowNative.GetWindowHandle(this));
            set
            {
                if (Math.Abs(WindowInterop.GetMinHeight(WindowNative.GetWindowHandle(this)) - value) > 0.01)
                {
                    // var minSize = new SizeInt32(value, _appWindow.MinSize.Height);
                    WindowInterop.SetMinHeight(WindowNative.GetWindowHandle(this), value);
                    OnPropertyChanged();
                }
            }
        }


        public object DataContext
        {
            get => this.RootGrid.DataContext;
            set => this.RootGrid.DataContext = value;
        }


        /// <summary>
        /// Event handler invoked when the window is attempting to close.
        /// Raises the Closing event with appropriate arguments.
        /// </summary>
        private void OnWindowClosing(object? sender, EventArgs e)
        {
            var closingArgs = new WindowClosingEventArgs(WindowCloseReason.ApplicationShutdown, false);

            Closing?.Invoke(this, closingArgs);

            if (closingArgs.Cancel)
            {

            }
            else
            {
                // Proceed with closing
                // The window will close naturally after this method
            }
        }

        /// <summary>
        /// Event handler invoked after the window has closed.
        /// Raises the Closed event with appropriate arguments.
        /// </summary>
        private void OnWindowClosed(object? sender, EventArgs e)
        {
            // Optionally, perform actions after the window has closed
            Console.WriteLine("Window has been closed.");
        }

        /// <summary>
        /// Event handler for Closed event.
        /// Raises the Closed event for external subscribers.
        /// </summary>
        private void OnClosedInternal(object sender, EventArgs args)
        {
            // Unsubscribe from DPI changes
            //_displayInformation.DpiChanged -= OnDpiChanged;

            // Dispose WindowStateManager
            _windowStateManager.PropertyChanged -= OnWindowStateChanged;
            _windowStateManager.Dispose();

            Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Event handler for SizeChanged event.
        /// Updates the Bounds, Width, Height, and WindowState.
        /// </summary>
        private void OnSizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            this.Bounds = new WaveeRect(new WaveeSize(args.Size.Width, args.Size.Height));
            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(Height));

            // Update WindowState is handled by WindowStateManager
        }

        /// <summary>
        /// Event handler for window state changes.
        /// Updates the custom WindowState enum accordingly.
        /// </summary>
        private void OnWindowStateChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WindowStateManager.CurrentState))
            {
                WindowState = _windowStateManager.CurrentState;
                // Optionally, raise Closing or other events based on state changes
            }
        }

        // /// <summary>
        // /// Event handler for DpiChanged event.
        // /// Updates the DPI scale and adjusts window size accordingly.
        // /// </summary>
        // private void OnDpiChanged(DisplayInformation sender, object args)
        // {
        //     // Update the DPI scale
        //     _dpiScale = sender.RawPixelsPerViewPixel;
        //
        //     // Adjust window size to maintain the same logical size
        //     int newPhysicalWidth = LogicalToPhysical(this.Width);
        //     int newPhysicalHeight = LogicalToPhysical(this.Height);
        //     _appWindow.Resize(new SizeInt32(newPhysicalWidth, newPhysicalHeight));
        //
        //     // Update Bounds if necessary
        //     this.Bounds = new WaveeRect(new WaveeSize(this.Width, this.Height));
        //     OnPropertyChanged(nameof(Width));
        //     OnPropertyChanged(nameof(Height));
        // }

        /// <summary>
        /// Restores the window from a minimized state.
        /// </summary>
        private void RestoreWindow()
        {
            if (_windowState == WindowState.Minimized)
            {
                // Restore the window using AppWindow
                //_appWindow.Restore();
                _appWindow.Show();
                // Alternatively, you can use ShowWindow with SW_RESTORE
                // but AppWindow.Restore() is more aligned with the Windows App SDK

                // Update WindowState
                WindowState = WindowState.Normal;
            }
        }

        /// <summary>
        /// Converts logical pixels to physical pixels based on the current DPI scale.
        /// </summary>
        /// <param name="logical">The size in logical pixels.</param>
        /// <returns>The size in physical pixels.</returns>
        private int LogicalToPhysical(double logical)
        {
            return (int)(logical * _dpiScale);
        }

        /// <summary>
        /// Converts physical pixels to logical pixels based on the current DPI scale.
        /// </summary>
        /// <param name="physical">The size in physical pixels.</param>
        /// <returns>The size in logical pixels.</returns>
        private double PhysicalToLogical(int physical)
        {
            return physical / _dpiScale;
        }

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the field and raises PropertyChanged event if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">Reference to the field.</param>
        /// <param name="value">New value to set.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>True if the value was changed; otherwise, false.</returns>
        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Imports the SetForegroundWindow function from user32.dll to bring the window to the foreground.
        /// </summary>
        /// <param name="hWnd">Handle to the window.</param>
        /// <returns>True if the window was brought to the foreground; otherwise, false.</returns>
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
