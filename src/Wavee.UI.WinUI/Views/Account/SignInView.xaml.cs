using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Account;

public sealed partial class SignInView : UserControl
{
    public SignInView()
    {
        this.InitializeComponent();
    }

    public AccountViewModel ViewModel => (AccountViewModel)DataContext;
}