using System;
using System.Windows.Forms;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

namespace Wavee.UI.WinUI.Views.Album;

public sealed partial class AlbumView : UserControl, INavigablePage
{
    public AlbumView()
    {
        ViewModel = new AlbumViewModel<WaveeUIRuntime>(App.Runtime);
        this.InitializeComponent();
    }

    public bool ShouldKeepInCache(int _)
    {
        //never keep in cache
        return false;
    }

    Option<INavigableViewModel> INavigablePage.ViewModel => ViewModel;

    public AlbumViewModel<WaveeUIRuntime> ViewModel { get; }

    private async void AlbumView_OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.AlbumFetched.Task;
        UpdateBindings();
    }

    private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        UpdateBindings();
    }

    private void UpdateBindings()
    {
        this.Bindings.Update();
        MainImage.Source = new BitmapImage(new Uri(ViewModel.Image));
        TotalDuration.Text = TotalSum().ToString(@"mm\:ss");
    }
    private TimeSpan TotalSum()
    {
        var totalSum = ViewModel.Discs.Sum(x =>
            x.Tracks.Sum(f => f.Duration.TotalMilliseconds));
        return TimeSpan.FromMilliseconds(totalSum);
    }
}