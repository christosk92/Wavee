using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Wavee.UI.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        this.SystemBackdrop = new MicaBackdrop();
        this.ExtendsContentIntoTitleBar = true;
    }
}