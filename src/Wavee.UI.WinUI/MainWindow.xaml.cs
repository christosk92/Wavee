using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;


namespace Wavee.UI.WinUI;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        Instance = this;
        this.InitializeComponent();
        //this.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        this.SystemBackdrop = new MicaBackdrop();
    }

    public static MainWindow Instance { get; private set; }
}