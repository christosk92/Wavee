using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Identity;

namespace Wavee.UI.WinUI.Views.Identity;

public sealed partial class SpotifyCredentialsView : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), 
            typeof(SpotifyCredentialsViewModel), 
            typeof(SpotifyCredentialsView), 
            new PropertyMetadata(default(SpotifyCredentialsViewModel), PropertyChangedCallback));

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SpotifyCredentialsView v)
        {
            v.DataContext = e.NewValue;
        }
    }

    public SpotifyCredentialsView()
    {
        this.InitializeComponent();
    }

    public SpotifyCredentialsViewModel ViewModel
    {
        get => (SpotifyCredentialsViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
}