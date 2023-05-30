using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views;

public sealed partial class MainContentView : UserControl
{
    public MainContentView()
    {
        this.InitializeComponent();
        this.Content = new SignInView(OnSignInAction);
    }

    private void OnSignInAction(Option<User> user)
    {
        if (user.IsSome)
        {
            this.Content = new ShellView(user.ValueUnsafe());
        }
    }
}