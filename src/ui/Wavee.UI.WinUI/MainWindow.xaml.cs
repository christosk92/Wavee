using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Wavee.UI.Features.MainWindow;

namespace Wavee.UI.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        Instance = this;
        this.SystemBackdrop = new MicaBackdrop();
        this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

        var m_TitleBar = this.AppWindow.TitleBar;

        m_TitleBar.BackgroundColor = Colors.Transparent;
        m_TitleBar.ButtonBackgroundColor = Colors.Transparent;

        m_TitleBar.InactiveBackgroundColor = Colors.Transparent;
        m_TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
    }

    public MainWindowViewModel ViewModel { get; private set; }

    public static MainWindow Instance { get; private set; }

    public void SetViewModel(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        this.Bindings.Update();
    }
}