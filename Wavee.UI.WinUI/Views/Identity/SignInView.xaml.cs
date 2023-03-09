using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using Wavee.UI.ViewModels.Identity;
namespace Wavee.UI.WinUI.Views.Identity;

public sealed partial class SignInView : UserControl
{
    public SignInViewModel ViewModel { get; }

    public SignInView(SignInViewModel viewmodel)
    {
        ViewModel = viewmodel;
        this.InitializeComponent();
    }

    private void LetsGetStartedBlock_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ProgressBorder.Width = (sender as FrameworkElement)!.ActualWidth * 1.5;
    }

    public bool NullToBool(string? o, bool ifNull)
    {
        return string.IsNullOrEmpty(o) ? ifNull : !ifNull;
    }

    public SpotifyCredentialsViewModel? CastToOrNull(AbsCredentialsViewModel absCredentialsViewModel)
    {
        if (absCredentialsViewModel is SpotifyCredentialsViewModel c) return c;
        return null;
    }
}
