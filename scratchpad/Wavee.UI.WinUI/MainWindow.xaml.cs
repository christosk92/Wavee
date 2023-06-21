using Microsoft.UI.Xaml;
using Wavee.UI.WinUI.ViewModels;

namespace Wavee.UI.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        ViewModel = new MainWindowViewModel();
        this.InitializeComponent();
    }

    public MainWindowViewModel ViewModel { get; }
}