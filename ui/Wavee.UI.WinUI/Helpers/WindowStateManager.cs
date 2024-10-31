using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Wavee.ViewModels.Enums;
using Wavee.UI.WinUI.Helpers;
using Microsoft.UI.Dispatching;
using static Wavee.UI.WinUI.Helpers.WindowInterop;
using WinRT.Interop;

namespace Wavee.UI.WinUI.Helpers
{
    internal class WindowStateManager : INotifyPropertyChanged, IDisposable
    {
        private readonly Window _window;
        private readonly IntPtr _hWnd;
        private readonly DispatcherTimer _timer;
        private WindowState _currentState;

        public event PropertyChangedEventHandler? PropertyChanged;

        public WindowState CurrentState
        {
            get => _currentState;
            private set
            {
                if (_currentState != value)
                {
                    _currentState = value;
                    OnPropertyChanged();
                }
            }
        }

        public WindowStateManager(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _hWnd = WindowNative.GetWindowHandle(_window);

            // Initialize current state
            var currState = GetWindowState();
            if (currState.HasValue) CurrentState = currState.Value;
            // Set up a timer to poll window state every 500 milliseconds
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        private void OnTimerTick(object? sender, object e)
        {
            var newState = GetWindowState();
            if (newState != CurrentState && newState.HasValue)
            {
                CurrentState = newState.Value;
            }
        }

        private WindowState? GetWindowState()
        {
            var placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            bool success = WindowInterop.GetWindowPlacement(_hWnd, ref placement);
            if (!success)
            {
                return null;
            }

            switch (placement.showCmd)
            {
                case 1: // SW_SHOWNORMAL
                case 9: // SW_RESTORE
                    return WindowState.Normal;
                case 2: // SW_SHOWMINIMIZED
                    return WindowState.Minimized;
                case 3: // SW_SHOWMAXIMIZED
                    return WindowState.Maximized;
                default:
                    return WindowState.Normal;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
        }
    }
}
