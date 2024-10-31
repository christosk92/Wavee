using System.ComponentModel;
using Wavee.ViewModels.Enums;
using Wavee.ViewModels.Models.EventArgs;
using Wavee.ViewModels.Models.UI;

namespace Wavee.ViewModels.Interfaces;

public interface IMainWindow : INotifyPropertyChanged
{
    void BringToFront();
    void Close();
    
    WaveeRect Bounds { get; }
    WindowState WindowState { get; set; }
    double Width { get; set; }
    double Height { get; set; }
    double MinWidth { get; set; }
    double MinHeight { get; set; }
    object DataContext { get; set; }
    
    event EventHandler<WindowClosingEventArgs>? Closing; 
    event EventHandler? Closed;
    void Show();
}