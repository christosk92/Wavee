using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Shell;

public sealed partial class Player : UserControl
{
    public Player()
    {
        this.InitializeComponent();
    }

    public PlayerViewModel ViewModel => DataContext as PlayerViewModel;
}