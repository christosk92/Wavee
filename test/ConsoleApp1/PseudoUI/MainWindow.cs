using System.ComponentModel;
using System.Runtime.InteropServices;
using Wavee.ViewModels.Enums;
using Wavee.ViewModels.Interfaces;
using Wavee.ViewModels.Models.EventArgs;
using Wavee.ViewModels.Models.UI;
using Wavee.ViewModels.ViewModels;

namespace ConsoleApp1.PseudoUI;

public sealed class MainWindow : IMainWindow
{
    private void Do(MainViewModel mainViewModel)
    {
        
    }
    
    
    
    
    
    
    private object _dataContext;

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_HIDE = 0;
    const int SW_SHOW = 5;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void BringToFront()
    {
        var handle = GetConsoleWindow();
        ShowWindow(handle, SW_HIDE);
    }

    public void Close()
    {
        throw new NotImplementedException();
    }

    public WaveeRect Bounds { get; }
    public WindowState WindowState { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double MinWidth { get; set; }
    public double MinHeight { get; set; }

    public object DataContext
    {
        get => _dataContext;
        set
        {
            if (value is MainViewModel mainViewModel)
            {
                Do(mainViewModel);
            }
            _dataContext = value;
        }
    }


    public event EventHandler<WindowClosingEventArgs>? Closing;
    public event EventHandler? Closed;

    public void Show()
    {
        var handle = GetConsoleWindow();
        ShowWindow(handle, SW_HIDE);
    }
}