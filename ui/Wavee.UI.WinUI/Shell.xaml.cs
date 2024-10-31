using Microsoft.UI.Xaml.Controls;
using Wavee.ViewModels.ViewModels;

namespace Wavee.UI.WinUI;

public sealed partial class Shell : UserControl
{
    public Shell()
    {
        this.InitializeComponent();
    }

    public MainViewModel ViewModel => (MainViewModel)DataContext;
}